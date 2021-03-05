//using System;
//using System.Buffers;
//using System.Collections.Generic;
//using System.Diagnostics;
//using System.Diagnostics.CodeAnalysis;
//using System.IO;
//using System.IO.MemoryMappedFiles;
//using System.Reflection;
//using System.Runtime.CompilerServices;
//using System.Runtime.InteropServices;
//using Microsoft.Toolkit.HighPerformance.Extensions;
//using TerraFX.Interop;
//using Voltium.Common;
//using Voltium.Common.Strings;
//using Voltium.Core.Devices.Shaders;
//using Voltium.Core.Pipeline;
//using static TerraFX.Interop.Windows;

//namespace Voltium.Core.Devices
//{
//    // TODO: allow serializing to file sonehow
//    /// <summary>
//    /// In charge of creation, storing, and retrieving pipeline state objects (PSOs)
//    /// </summary>
//    [ThreadSafe]
//    public unsafe class PipelineManager
//    {
//        private ComputeDevice _device;
//        private UniqueComPtr<ID3D12PipelineLibrary1> _psoLibrary;

//        private static bool DisableCache => true;


//        /// <summary>
//        /// Creates a new <see cref="PipelineManager"/> for a device
//        /// </summary>
//        /// <param name="device">The <see cref="ComputeDevice"/> to create the manager for</param>
//        public PipelineManager(ComputeDevice device)
//        {
//            _device = device;

//            if (device.QueryFeatureSupport<D3D12_FEATURE_DATA_SHADER_CACHE>(D3D12_FEATURE.D3D12_FEATURE_SHADER_CACHE)
//                    .SupportFlags.HasFlag(D3D12_SHADER_CACHE_SUPPORT_FLAGS.D3D12_SHADER_CACHE_SUPPORT_LIBRARY))
//            {
//                return;
//            }

//            Span<byte> cache = GetCache();

//            fixed (byte* pCache = cache)
//            {
//                using UniqueComPtr<ID3D12PipelineLibrary1> psoLibrary = default;

//                int hr = E_FAIL;
//                try
//                {
//                    hr = _device.As<ID3D12Device2>()->CreatePipelineLibrary(pCache, (uint)cache.Length, psoLibrary.Iid, (void**)&psoLibrary);
//                }
//                // Debug layer may throw
//                catch (SEHException)
//                {

//                }

//                if (hr is E_INVALIDARG or D3D12_ERROR_DRIVER_VERSION_MISMATCH or D3D12_ERROR_ADAPTER_NOT_FOUND)
//                {
//                    // cache invalidated
//                    Guard.ThrowIfFailed(_device.As<ID3D12Device2>()->CreatePipelineLibrary(null, 0, psoLibrary.Iid, (void**)&psoLibrary));
//                    DeleteCache();
//                }
//                else if (FAILED(hr))
//                {
//                    Guard.ThrowIfFailed(hr, "_device.DevicePointerAs<ID3D12Device2>()->CreatePipelineLibrary(pCache, (uint)cache.Length, psoLibrary.Iid, (void**)&psoLibrary);");
//                }

//                _psoLibrary = psoLibrary.Move();
//            }

//            AppDomain.CurrentDomain.ProcessExit += (_, _) => Cache(dispose: true);
//        }

//        private static Span<byte> GetCache()
//        {
//            if (!DisableCache && File.Exists(CachePsoLibLocation))
//            {
//                // handle race condition where file is deleted between the call to 'Exists' and the read
//                try
//                {
//                    return File.ReadAllBytes(CachePsoLibLocation);
//                }
//                catch (FileNotFoundException)
//                {
//                    return Span<byte>.Empty;
//                }
//            }

//            return Span<byte>.Empty;
//        }

//        private static void DeleteCache() => File.Delete(CachePsoLibLocation);

//        ///// <summary>
//        ///// Creates a new named pipeline state object and registers it in the library for retrieval with
//        ///// <see cref="RetrievePso(string, in GraphicsPipelineDesc)"/>
//        ///// </summary>
//        ///// <param name="desc">The descriptor for the pipeline state</param>
//        ///// <param name="name">The name of the pipeline state</param>
//        //public PipelineStateObject CreatePipelineStateObject<TShaderInput, TPipelineStream>(in TPipelineStream desc, string name) where TPipelineStream : unmanaged, IPipelineStreamType
//        //{
//        //    // Prevent readonly copy being made. These fields are all private
//        //    Unsafe.AsRef(in desc)._Initialize();

//        //    fixed (void* p = &desc)
//        //    {
//        //        var pso = new D3D12_PIPELINE_STATE_STREAM_DESC
//        //        {
//        //            pPipelineStateSubobjectStream = p,
//        //            SizeInBytes = (nuint)sizeof(TPipelineStream)
//        //        };

//        //        using UniqueComPtr<ID3D12PipelineState> state = default;
//        //        _device.ThrowIfFailed(_device.DevicePointerAs<ID3D12Device2>()->CreatePipelineState(&pso, state.Iid, (void**)&state));
//        //    }

//        //    return null!;
//        //}

//        /// <summary>
//        /// Creates a new named pipeline state object and registers it in the library for retrieval
//        /// </summary>
//        /// <param name="desc">The descriptor for the pipeline state</param>
//        /// <param name="name">The name of the pipeline state</param>
//        ///
//        public GraphicsPipelineStateObject CreatePipelineStateObject(GraphicsPipelineDesc desc, string name)
//        {
//            desc.SetMarkers(_device);

//            fixed (void* p = desc)
//            fixed (char* pName = name)
//            {
//                var pso = new D3D12_PIPELINE_STATE_STREAM_DESC
//                {
//                    pPipelineStateSubobjectStream = p,
//                    SizeInBytes = desc.DescSize
//                };

//                using UniqueComPtr<ID3D12PipelineState> state = GetOrCreatePso(&pso, pName);

//                if (ComIdentity.TryGetManagedObject(state.Ptr, out GraphicsPipelineStateObject obj))
//                {
//                    return obj;
//                }

//                return new GraphicsPipelineStateObject(state.Move(), desc);
//            }
//        }

//        /// <summary>
//        /// Creates a new named pipeline state object and registers it in the library for retrieval
//        /// </summary>
//        /// <param name="desc">The descriptor for the pipeline state</param>
//        /// <param name="name">The name of the pipeline state</param>
//        ///
//        public PipelineStateObject CreatePipelineStateObject(ComputePipelineDesc desc, string name)
//        {
//            fixed (void* p = desc)
//            fixed (char* pName = name)
//            {
//                var pso = new D3D12_PIPELINE_STATE_STREAM_DESC
//                {
//                    pPipelineStateSubobjectStream = p,
//                    SizeInBytes = desc.DescSize
//                };

//                using UniqueComPtr<ID3D12PipelineState> state = GetOrCreatePso(&pso, pName);

//                if (ComIdentity.TryGetManagedObject(state.Ptr, out ComputePipelineStateObject obj))
//                {
//                    return obj;
//                }

//                return new ComputePipelineStateObject(state.Move(), desc);
//            }
//        }

//        /// <summary>
//        /// Creates a new named pipeline state object and registers it in the library for retrieval
//        /// </summary>
//        /// <param name="desc">The descriptor for the pipeline state</param>
//        /// <param name="name">The name of the pipeline state</param>
//        ///
//        public PipelineStateObject CreatePipelineStateObject(MeshPipelineDesc desc, string name)
//        {
//            fixed (void* p = desc)
//            fixed (char* pName = name)
//            {
//                var pso = new D3D12_PIPELINE_STATE_STREAM_DESC
//                {
//                    pPipelineStateSubobjectStream = p,
//                    SizeInBytes = desc.DescSize
//                };

//                using UniqueComPtr<ID3D12PipelineState> state = GetOrCreatePso(&pso, pName);

//                if (ComIdentity.TryGetManagedObject(state.Ptr, out MeshPipelineStateObject obj))
//                {
//                    return obj;
//                }

//                return new MeshPipelineStateObject(state.Move(), desc);
//            }
//        }


//        /// <summary>
//        /// Creates a new named pipeline state object and registers it in the library for retrieval
//        /// </summary>
//        /// <param name="desc">The descriptor for the pipeline state</param>
//        /// <param name="name">The name of the pipeline state</param>
//        ///
//        public PipelineStateObject CreatePipelineStateObject(RaytracingPipelineDesc desc, string name)
//        {
//            fixed (char* pName = name)
//            {
//                var serialized = desc.Serialize();
//                var psoDesc = serialized.Desc;

//                using UniqueComPtr<ID3D12StateObject> state = default;
//                _device.ThrowIfFailed(_device.As<ID3D12Device5>()->CreateStateObject(&psoDesc, state.Iid, (void**)&state));

//                return new RaytracingPipelineStateObject(state.Move(), desc);
//            }
//        }

//        private UniqueComPtr<ID3D12PipelineState> GetOrCreatePso(D3D12_PIPELINE_STATE_STREAM_DESC* pso, char* pName)
//        {
//            using UniqueComPtr<ID3D12PipelineState> state = default;

//            int hr = E_INVALIDARG;
//            if (_psoLibrary.Exists)
//            {
//                // will return E_INVALIDARG if pipeline does not exist
//                hr = _psoLibrary.Ptr->LoadPipeline((ushort*)pName, pso, state.Iid, (void**)&state);
//            }
//            if (hr == E_INVALIDARG)
//            {
//                _device.ThrowIfFailed(_device.As<ID3D12Device2>()->CreatePipelineState(pso, state.Iid, (void**)&state));
//                _device.ThrowIfFailed(_psoLibrary.Ptr->StorePipeline((ushort*)pName, state.Ptr));
//            }

//            return state.Move();
//        }

//        //        /// <summary>
//        //        /// Creates a new named pipeline state object and registers it in the library for retrieval with
//        //        /// <see cref="RetrievePso(string, in GraphicsPipelineDesc)"/>
//        //        /// </summary>
//        //        /// <param name="graphicsDesc">The descriptor for the pipeline state</param>
//        //        /// <param name="name">The name of the pipeline state</param>
//        //        public GraphicsPipelineStateObject CreatePipelineStateObject<TShaderInput>(in GraphicsPipelineDesc graphicsDesc, string name) where TShaderInput : unmanaged, IBindableShaderType
//        //        {
//        //            try
//        //            {
//        //                var copy = graphicsDesc;
//        //                copy.Inputs = new InputLayout(default(TShaderInput).GetShaderInputs().Span);


//        //                return CreatePipelineStateObject(name, copy);
//        //            }
//        //            catch (Exception e)
//        //            {
//        //#if REFLECTION
//        //                // if this happens when ShaderInputAttribute is applied, our generator is bugged
//        //                bool hasGenAttr = typeof(TShaderInput).GetCustomAttribute<ShaderInputAttribute>() is not null;

//        //                const string hasGenAttrMessage = "This appears to be a failure with the " +
//        //                    "IA input type generator ('Voltium.Analyzers.IAInputDescGenerator'). Please file a bug";

//        //                const string noGenAttrMessage = "You appear to have manually implemented the IA input methods. Ensure they do not throw when called on a defaulted struct" +
//        //                    "('default(TShaderInput).GetShaderInputs()')";

//        //                ThrowHelper.ThrowArgumentException(
//        //                    $"IA input type '{typeof(TShaderInput).Name}' threw an exception of type '{e.GetType().Name}'. " +
//        //                    $"Inspect InnerException to view this exception. {(hasGenAttr ? hasGenAttrMessage : noGenAttrMessage)}", e);
//        //#else
//        //                ThrowHelper.ThrowArgumentException(
//        //                    $"IA input type '{nameof(TShaderInput)}' threw an exception. " +
//        //                    $"Inspect InnerException to view this exception. Reflection is disabled so no further information could be gathered", e);

//        //                return default!;
//        //#endif
//        //            }
//        //        }

//        private const string CacheExtension = ".cpsolib";
//        private static readonly string CachePsoLibLocation = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), Assembly.GetEntryAssembly()!.GetName().Name!, "Voltium.Core.PipelineManagerCache") + CacheExtension;

//        private void Cache(bool dispose)
//        {
//            if (DisableCache || !_psoLibrary.Exists)
//            {
//                return;
//            }

//            try
//            {
//                var size = (long)_psoLibrary.Ptr->GetSerializedSize();

//                Directory.CreateDirectory(Path.GetDirectoryName(CachePsoLibLocation)!);

//                using var mmf = MemoryMappedFile.CreateFromFile(CachePsoLibLocation, FileMode.Create, null, size);
//                using var accessor = mmf.CreateViewAccessor(0, size);


//                using var buff = RentedArray<byte>.Create(checked((int)size));

//                try
//                {
//                    byte* pBuff = null;
//                    accessor.SafeMemoryMappedViewHandle.AcquirePointer(ref pBuff);

//                    fixed (byte* pIntermediate = buff)
//                    {
//                        try
//                        {
//                            _device.ThrowIfFailed(_psoLibrary.Ptr->Serialize(pIntermediate, (nuint)size));
//                        }
//                        catch (SEHException)
//                        {
//                            // debug layer throws
//                        }
//                        catch (DeviceDisconnectedException e) when (e.Reason == DeviceDisconnectReason.InternalDriverError)
//                        {
//                            // buggy 460 nvidia driver
//                        }
//                    }
//                    accessor.WriteArray(0, buff.Value, 0, checked((int)size));
//                    //_device.ThrowIfFailed(_psoLibrary.Ptr->Serialize(pBuff, (nuint)size));
//                }
//                finally
//                {
//                    accessor.SafeMemoryMappedViewHandle.ReleasePointer();
//                }
//            }
//            finally
//            {

//                if (dispose)
//                {
//                    _psoLibrary.Dispose();
//                }
//            }
//        }

//        /// <inheritdoc/>
//        public void Dispose()
//        {
//        }
//    }
//}
