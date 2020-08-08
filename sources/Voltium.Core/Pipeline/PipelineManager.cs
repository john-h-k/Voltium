using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using TerraFX.Interop;
using Voltium.Common;
using Voltium.Common.Strings;
using Voltium.Core.Devices.Shaders;
using Voltium.Core.Pipeline;
using static Voltium.Core.Devices.PipelineTranslationLayer;

namespace Voltium.Core.Devices
{
    // TODO: allow serializing to file sonehow
    /// <summary>
    /// In charge of creation, storing, and retrieving pipeline state objects (PSOs)
    /// </summary>
    [ThreadSafe]
    public unsafe class PipelineManager
    {
        private ComputeDevice _device;
        private ComPtr<ID3D12PipelineLibrary> _psoLibrary;
        private Dictionary<ComPtr<ID3D12PipelineState>, PipelineStateObject> _psoMap = new();

        [MemberNotNullWhen(true, nameof(_legacyGraphicsMap))]
        [MemberNotNullWhen(true, nameof(_legacyComputeMap))]
#pragma warning disable CS8775 // Member must have a non-null value when exiting in some condition.
        private bool IsLegacy { get; set; }
#pragma warning restore CS8775 // Member must have a non-null value when exiting in some condition.

        private Dictionary<(string Name, GraphicsPipelineDesc Desc), GraphicsPipelineStateObject>? _legacyGraphicsMap;
        private Dictionary<(string Name, ComputePipelineDesc Desc), ComputePipelineStateObject>? _legacyComputeMap;

        /// <summary>
        /// Creates a new <see cref="PipelineManager"/> for a device
        /// </summary>
        /// <param name="device">The <see cref="ComputeDevice"/> to create the manager for</param>
        public PipelineManager(ComputeDevice device)
        {
            _device = device;
            if (device.DeviceLevel < ComputeDevice.SupportedDevice.Device1)
            {
                IsLegacy = true;
                _legacyGraphicsMap = new();
                _legacyComputeMap = new();
            }

            Reset();
        }

        /// <summary>
        /// Creates a new named pipeline state object and registers it in the library for retrieval with
        /// <see cref="RetrievePso(string, in GraphicsPipelineDesc)"/>
        /// </summary>
        /// <param name="name">The name of the pipeline state</param>
        /// <param name="graphicsDesc">The descriptor for the pipeline state</param>
        public GraphicsPipelineStateObject CreatePipelineStateObject<TShaderInput>(string name, in GraphicsPipelineDesc graphicsDesc) where TShaderInput : unmanaged, IBindableShaderType
        {
            try
            {
                var copy = graphicsDesc;
                copy.Inputs = default(TShaderInput).GetShaderInputs();


                return CreatePipelineStateObject(name, copy);
            }
            catch (Exception e)
            {
#if REFLECTION
                // if this happens when ShaderInputAttribute is applied, our generator is bugged
                bool hasGenAttr = typeof(TShaderInput).GetCustomAttribute<ShaderInputAttribute>() is object;

                const string hasGenAttrMessage = "This appears to be a failure with the " +
                    "IA input type generator ('Voltium.Analyzers.IAInputDescGenerator'). Please file a bug";

                const string noGenAttrMessage = "You appear to have manually implemented the IA input methods. Ensure they do not throw when called on a defaulted struct" +
                    "('default(TShaderInput).GetShaderInputs()')";

                ThrowHelper.ThrowArgumentException(
                    $"IA input type '{typeof(TShaderInput).Name}' threw an exception of type '{e.GetType().Name}'. " +
                    $"Inspect InnerException to view this exception. {(hasGenAttr ? hasGenAttrMessage : noGenAttrMessage)}", e);
#else
                ThrowHelper.ThrowArgumentException(
                    $"IA input type '{nameof(TShaderInput)}' threw an exception. " +
                    $"Inspect InnerException to view this exception. Reflection is disabled so no further information could be gathered", e);

                return default!;
#endif
            }
        }


        /// <summary>
        /// Creates a new named pipeline state object and registers it in the library for retrieval with
        /// <see cref="RetrievePso(string, in GraphicsPipelineDesc)"/>
        /// </summary>
        /// <param name="name">The name of the pipeline state</param>
        /// <param name="graphicsDesc">The descriptor for the pipeline state</param>
        public GraphicsPipelineStateObject CreatePipelineStateObject(string name, in GraphicsPipelineDesc graphicsDesc)
        {
            TranslateGraphicsPipelineDescriptionWithoutShadersOrShaderInputLayoutElements(_device, ref Unsafe.AsRef(in graphicsDesc), out D3D12_GRAPHICS_PIPELINE_STATE_DESC desc);

            // TODO use pinned pool
            using var buff = RentedArray<D3D12_INPUT_ELEMENT_DESC>.Create(graphicsDesc.Inputs.Length);

            fixed (D3D12_INPUT_ELEMENT_DESC* pDesc = buff.Value)
            fixed (byte* vs = graphicsDesc.VertexShader)
            fixed (byte* ps = graphicsDesc.PixelShader)
            fixed (byte* gs = graphicsDesc.GeometryShader)
            fixed (byte* ds = graphicsDesc.DomainShader)
            fixed (byte* hs = graphicsDesc.HullShader)
            {
                // we must keep this alive until the end of the scope
                var strBuff = TranslateLayouts(graphicsDesc.Inputs, pDesc);

                TranslateShadersMustBePinned(graphicsDesc, ref desc);

                desc.InputLayout = new D3D12_INPUT_LAYOUT_DESC { NumElements = (uint)graphicsDesc.Inputs.Length, pInputElementDescs = pDesc };

                using ComPtr<ID3D12PipelineState> pso = default;
                _device.ThrowIfFailed(_device.DevicePointer->CreateGraphicsPipelineState(
                    &desc,
                    pso.Iid,
                    (void**)&pso
                ));

                // Prevent GC disposing it while translation occurs etc
                GC.KeepAlive(strBuff);

                DebugHelpers.SetName(pso.Get(), $"Graphics pipeline state object '{name}'");


                var pipeline = new GraphicsPipelineStateObject(pso.Move(), graphicsDesc);
                _psoMap[pso] = pipeline;


                if (IsLegacy)
                {
                    _legacyGraphicsMap[(name, graphicsDesc)] = pipeline;
                }
                else
                {
                    fixed (char* pName = name)
                    {
                        _psoLibrary.Get()->StorePipeline((ushort*)pName, pipeline.GetPso());
                    }
                }

                return pipeline;
            }
        }

        /// <summary>
        /// Resets the manager, clearing all pipelines
        /// </summary>
        public void Reset()
        {
            _psoLibrary.Dispose();

            // TODO pipeline library caching
            if (!IsLegacy)
            {
                using ComPtr<ID3D12PipelineLibrary> psoLibrary = default;
                int hr = _device.DevicePointerAs<ID3D12Device1>()->CreatePipelineLibrary(null, 0, psoLibrary.Iid, (void**)&psoLibrary);
                _psoLibrary = psoLibrary.Move();

                if (hr == Windows.DXGI_ERROR_UNSUPPORTED)
                {
                    IsLegacy = true;
                    _legacyComputeMap = new();
                    _legacyGraphicsMap = new();
                }
            }
            else
            {
                _legacyComputeMap.Clear();
                _legacyGraphicsMap.Clear();
            }
        }

        /// <summary>
        /// Creates a new named pipeline state object and registers it in the library for retrieval with
        /// <see cref="RetrievePso(string, in ComputePipelineDesc)"/>
        /// </summary>
        /// <param name="name">The name of the pipeline state</param>
        /// <param name="computeDesc">The descriptor for the pipeline state</param>
        public ComputePipelineStateObject CreatePipelineStateObject(string name, in ComputePipelineDesc computeDesc)
        {
            fixed (byte* vs = computeDesc.ComputeShader)
            {
                D3D12_COMPUTE_PIPELINE_STATE_DESC desc = new()
                {
                    CS = new D3D12_SHADER_BYTECODE(vs, (uint)computeDesc.ComputeShader.Length),
                    pRootSignature = computeDesc.RootSignature.Value
                };

                using ComPtr<ID3D12PipelineState> pso = default;
                _device.ThrowIfFailed(_device.DevicePointer->CreateComputePipelineState(
                    &desc,
                    pso.Iid,
                    (void**)&pso
                ));

                DebugHelpers.SetName(pso.Get(), $"Compute pipeline state object '{name}'");

                var pipeline = new ComputePipelineStateObject(pso.Move(), computeDesc);
                _psoMap[pso] = pipeline;

                if (IsLegacy)
                {
                    _legacyComputeMap[(name, computeDesc)] = pipeline;
                }
                else
                {
                    fixed (char* pName = name)
                    {
                        _psoLibrary.Get()->StorePipeline((ushort*)pName, pipeline.GetPso());
                    }
                }

                return pipeline;
            }
        }

        /// <summary>
        /// Retrives a pipeline state object by name
        /// </summary>
        /// <param name="name">The name of the PSO to retrieve</param>
        /// <param name="graphicsDesc">The <see cref="GraphicsPipelineDesc"/> for the PSO to retrieve</param>
        /// <returns>The PSO stored with the name</returns>
        public GraphicsPipelineStateObject RetrievePso(string name, in GraphicsPipelineDesc graphicsDesc)
        {
            if (IsLegacy)
            {
                return _legacyGraphicsMap[(name, graphicsDesc)];
            }

            TranslateGraphicsPipelineDescriptionWithoutShadersOrShaderInputLayoutElements(_device, ref Unsafe.AsRef(in graphicsDesc), out D3D12_GRAPHICS_PIPELINE_STATE_DESC desc);

            // TODO use pinned pool
            using var buff = RentedArray<D3D12_INPUT_ELEMENT_DESC>.Create(graphicsDesc.Inputs.Length);

            fixed (D3D12_INPUT_ELEMENT_DESC* pDesc = buff.Value)
            fixed (byte* vs = graphicsDesc.VertexShader)
            fixed (byte* ps = graphicsDesc.PixelShader)
            fixed (byte* gs = graphicsDesc.GeometryShader)
            fixed (byte* ds = graphicsDesc.DomainShader)
            fixed (byte* hs = graphicsDesc.HullShader)
            {
                // we must keep this alive until the end of the scope
                var strBuff = TranslateLayouts(graphicsDesc.Inputs, pDesc);

                TranslateShadersMustBePinned(graphicsDesc, ref desc);

                desc.InputLayout = new D3D12_INPUT_LAYOUT_DESC { NumElements = (uint)graphicsDesc.Inputs.Length, pInputElementDescs = pDesc };


                fixed (char* pName = name)
                {
                    using ComPtr<ID3D12PipelineState> pso = default;
                    _psoLibrary.Get()->LoadGraphicsPipeline((ushort*)pName, &desc, pso.Iid, (void**)&pso);

                    // Prevent GC disposing it while translation occurs etc
                    GC.KeepAlive(strBuff);

                    return (GraphicsPipelineStateObject)_psoMap[pso];
                }

            }
        }

        /// <summary>
        /// Retrives a pipeline state object by name
        /// </summary>
        /// <param name="name">The name of the PSO to retrieve</param>
        /// <param name="computeDesc">The <see cref="ComputePipelineDesc"/> for the PSO to retrieve</param>
        /// <returns>The PSO stored with the name</returns>
        public ComputePipelineStateObject RetrievePso(string name, in ComputePipelineDesc computeDesc)
        {
            if (IsLegacy)
            {
                return _legacyComputeMap[(name, computeDesc)];
            }

            fixed (byte* vs = computeDesc.ComputeShader)
            {
                D3D12_COMPUTE_PIPELINE_STATE_DESC desc = new()
                {
                    CS = new D3D12_SHADER_BYTECODE(vs, (uint)computeDesc.ComputeShader.Length),
                    pRootSignature = computeDesc.RootSignature.Value
                };

                fixed (char* pName = name)
                {
                    using ComPtr<ID3D12PipelineState> pso = default;
                    _psoLibrary.Get()->LoadComputePipeline((ushort*)pName, &desc, pso.Iid, (void**)&pso);

                    return (ComputePipelineStateObject)_psoMap[pso];
                }
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            _psoLibrary.Dispose();
        }
    }
}
