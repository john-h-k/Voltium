using System;
using System.Collections.Generic;
using System.Reflection;
using TerraFX.Interop;
using Voltium.Common;
using Voltium.Common.Strings;
using Voltium.Core.Managers.Shaders;
using Voltium.Core.Pipeline;
using static Voltium.Core.Managers.PipelineTranslationLayer;

namespace Voltium.Core.Managers
{
    // TODO: allow serializing to file sonehow
    /// <summary>
    /// In charge of creation, storing, and retrieving pipeline state objects (PSOs)
    /// </summary>
    [ThreadSafe]
    public unsafe static class PipelineManager
    {
        //private ComPtr<ID3D12PipelineLibrary> _psoLibrary;
        private static Dictionary<string, PipelineStateObject> _psos = new(16, new FastStringComparer());

        /// <summary>
        /// Creates a new named pipeline state object and registers it in the library for retrieval with
        /// <see cref="RetrievePso(string)"/>
        /// </summary>
        /// <param name="device">The device to use when creating the pipeline state</param>
        /// <param name="name">The name of the pipeline state</param>
        /// <param name="graphicsDesc">The descriptor for the pipeline state</param>
        public static GraphicsPso CreatePso<TShaderInput>(GraphicsDevice device, string name, GraphicsPipelineDesc graphicsDesc) where TShaderInput : unmanaged, IBindableShaderType
        {
            try
            {
                graphicsDesc.Inputs = default(TShaderInput).GetShaderInputs();
            }
            catch (Exception e)
            {
#if REFLECTION
                // if this happens when ShaderInputAttribute is applied, our generator is bugged
                bool hasGenAttr = typeof(TShaderInput).GetCustomAttribute<ShaderInputAttribute>() is object;

                const string hasGenAttrMessage = "This appears to be a failure with the" +
                    "IA input type generator ('Voltium.Analyzers.IAInputDescGenerator'. Please file a bug";

                const string noGenAttrMessage = "You appear to have manually implemented the IA input methods. Ensure they do not throw when called on a defaulted struct" +
                    "('default(TShaderInput).GetShaderInputs()')";

                ThrowHelper.ThrowArgumentException(
                    $"IA input type '{typeof(TShaderInput).Name}' threw an exception of type '{e.GetType().Name}'. " +
                    $"Inspect InnerException to view this exception. {(hasGenAttr ? hasGenAttrMessage : noGenAttrMessage)}", e);
#else
                ThrowHelper.ThrowArgumentException(
                    $"IA input type '{nameof(TShaderInput)}' threw an exception. " +
                    $"Inspect InnerException to view this exception. Reflection is disabled so no further information could be gathered", e);
#endif
            }

            return CreatePso(device, name, graphicsDesc);
        }


        /// <summary>
        /// Creates a new named pipeline state object and registers it in the library for retrieval with
        /// <see cref="RetrievePso(string)"/>
        /// </summary>
        /// <param name="device">The device to use when creating the pipeline state</param>
        /// <param name="name">The name of the pipeline state</param>
        /// <param name="graphicsDesc">The descriptor for the pipeline state</param>
        public static GraphicsPso CreatePso(GraphicsDevice device, string name, in GraphicsPipelineDesc graphicsDesc)
        {
            TranslateGraphicsPipelineDescriptionWithoutShadersOrShaderInputLayoutElements(graphicsDesc, out D3D12_GRAPHICS_PIPELINE_STATE_DESC desc);

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

                desc.VS = new D3D12_SHADER_BYTECODE(vs, (uint)graphicsDesc.VertexShader.Length);
                desc.PS = new D3D12_SHADER_BYTECODE(ps, (uint)graphicsDesc.PixelShader.Length);
                desc.GS = new D3D12_SHADER_BYTECODE(gs, (uint)graphicsDesc.GeometryShader.Length);
                desc.DS = new D3D12_SHADER_BYTECODE(ds, (uint)graphicsDesc.DomainShader.Length);
                desc.HS = new D3D12_SHADER_BYTECODE(hs, (uint)graphicsDesc.HullShader.Length);
                desc.InputLayout = new D3D12_INPUT_LAYOUT_DESC { NumElements = (uint)graphicsDesc.Inputs.Length, pInputElementDescs = pDesc };

                using ComPtr<ID3D12PipelineState> pso = default;
                Guard.ThrowIfFailed(device.Device->CreateGraphicsPipelineState(
                    &desc,
                    pso.Guid,
                    ComPtr.GetVoidAddressOf(&pso)
                ));

                GC.KeepAlive(strBuff);

                DirectXHelpers.SetObjectName(pso.Get(), $"Graphics pipeline state object '{name}'");

                return new GraphicsPso(pso.Move(), graphicsDesc);
            }
        }


        /// <summary>
        /// Creates a new named pipeline state object and registers it in the library for retrieval with
        /// <see cref="RetrievePso(string)"/>
        /// </summary>
        /// <param name="device">The device to use when creating the pipeline state</param>
        /// <param name="name">The name of the pipeline state</param>
        /// <param name="computeDesc">The descriptor for the pipeline state</param>
        public static ComputePso CreatePso(GraphicsDevice device, string name, in ComputePipelineDesc computeDesc)
        {
            fixed (byte* vs = computeDesc.ComputeShader)
            {
                D3D12_COMPUTE_PIPELINE_STATE_DESC desc = new()
                {
                    CS = new D3D12_SHADER_BYTECODE(vs, (uint)computeDesc.ComputeShader.Length),
                    pRootSignature = computeDesc.ShaderSignature.Value
                };

                using ComPtr<ID3D12PipelineState> pso = default;
                Guard.ThrowIfFailed(device.Device->CreateComputePipelineState(
                    &desc,
                    pso.Guid,
                    ComPtr.GetVoidAddressOf(&pso)
                ));

                DirectXHelpers.SetObjectName(pso.Get(), $"Compute pipeline state object '{name}'");

                return new ComputePso(pso.Move(), computeDesc);
            }
        }

        /// <summary>
        /// Retrives a pipeline state object by name
        /// </summary>
        /// <param name="name">The name of the PSO to retrieve</param>
        /// <returns>The PSO stored with the name</returns>
        public static PipelineStateObject RetrievePso(string name)
        {
            return _psos[name];
        }

        /// <summary>
        /// Store a pipeline state object with an associated name
        /// </summary>
        /// <param name="name"></param>
        /// <param name="pso"></param>
        /// <param name="overwrite"></param>
        public static void StorePso(string name, PipelineStateObject pso, bool overwrite = false)
        {
            if (overwrite)
            {
                _psos[name] = pso;
            }
            else
            {
                if (!_psos.TryAdd(name, pso))
                {
                    ThrowHelper.ThrowInvalidOperationException($"PSO with name '{name}' was already present, and the " +
                        $"overwrite parameter was set to false");
                }
            }
        }

        /// <inheritdoc/>
        public static void Dispose()
        {
            lock (_psos)
            {
                foreach (var value in _psos.Values)
                {
                    value.Dispose();
                }
            }
        }
    }
}
