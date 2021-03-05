using System;
using System.Buffers;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using TerraFX.Interop;
using Voltium.Common;
using Voltium.Core.CommandBuffer;
using Voltium.Core.Contexts;
using Voltium.Core.Memory;
using Voltium.Core.Pipeline;
using Voltium.Core.Queries;
using static TerraFX.Interop.Windows;
using static TerraFX.Interop.D3D12_FEATURE;
using System.Runtime.InteropServices;
using System.Collections.Immutable;

namespace Voltium.Core.Devices
{
    public unsafe class D3D12NativeDevice : INativeDevice
    {
        private const uint ShaderVisibleDescriptorCount = 1024 * 512;

        private UniqueComPtr<ID3D12Device5> _device;
        private GenerationalHandleMapper _mapper;
        private ValueList<FreeBlock> _freeList;
        private int _resDescriptorSize, _rtvDescriptorSize, _dsvDescriptorSize;
        private UniqueComPtr<ID3D12DescriptorHeap> _shaderDescriptors;
        private D3D12_CPU_DESCRIPTOR_HANDLE _firstShaderDescriptor;
        private D3D12_GPU_DESCRIPTOR_HANDLE _firstShaderDescriptorGpu;
        private uint _numShaderDescriptors;

        public D3D12NativeDevice(D3D_FEATURE_LEVEL fl)
        {
            _mapper = new(false);

            {
                using UniqueComPtr<ID3D12Debug> debug = default;
                Windows.D3D12GetDebugInterface(debug.Iid, (void**)&debug);
                debug.Ptr->EnableDebugLayer();
            }

            UniqueComPtr<ID3D12Device5> device = default;

            ThrowIfFailed(D3D12CreateDevice(
                null,
                fl,
                device.Iid,
                (void**)&device
            ));

            _device = device;

            _resDescriptorSize = (int)device.Ptr->GetDescriptorHandleIncrementSize(D3D12_DESCRIPTOR_HEAP_TYPE.D3D12_DESCRIPTOR_HEAP_TYPE_CBV_SRV_UAV);
            _rtvDescriptorSize = (int)device.Ptr->GetDescriptorHandleIncrementSize(D3D12_DESCRIPTOR_HEAP_TYPE.D3D12_DESCRIPTOR_HEAP_TYPE_RTV);
            _dsvDescriptorSize = (int)device.Ptr->GetDescriptorHandleIncrementSize(D3D12_DESCRIPTOR_HEAP_TYPE.D3D12_DESCRIPTOR_HEAP_TYPE_DSV);

            UniqueComPtr<ID3D12DescriptorHeap> shaderVisible = default;
            var desc = new D3D12_DESCRIPTOR_HEAP_DESC
            {
                NumDescriptors = ShaderVisibleDescriptorCount,
                Type = D3D12_DESCRIPTOR_HEAP_TYPE.D3D12_DESCRIPTOR_HEAP_TYPE_CBV_SRV_UAV,
                Flags = D3D12_DESCRIPTOR_HEAP_FLAGS.D3D12_DESCRIPTOR_HEAP_FLAG_SHADER_VISIBLE
            };

            ThrowIfFailed(device.Ptr->CreateDescriptorHeap(&desc, shaderVisible.Iid, (void**)&shaderVisible));

            _shaderDescriptors = shaderVisible;
            _firstShaderDescriptor = shaderVisible.Ptr->GetCPUDescriptorHandleForHeapStart();
            _firstShaderDescriptorGpu = shaderVisible.Ptr->GetGPUDescriptorHandleForHeapStart();
            _numShaderDescriptors = desc.NumDescriptors;

            _freeList = new(1, ArrayPool<FreeBlock>.Shared);


            CheckFeatureSupport(D3D12_FEATURE_D3D12_OPTIONS, out D3D12_FEATURE_DATA_D3D12_OPTIONS opts);
            CheckFeatureSupport(D3D12_FEATURE_ARCHITECTURE1, out D3D12_FEATURE_DATA_ARCHITECTURE1 arch1);
            CheckFeatureSupport(D3D12_FEATURE_GPU_VIRTUAL_ADDRESS_SUPPORT, out D3D12_FEATURE_DATA_GPU_VIRTUAL_ADDRESS_SUPPORT va);
            Info = new()
            {
                IsCacheCoherent = Helpers.Int32ToBool(arch1.CacheCoherentUMA) || !Helpers.Int32ToBool(arch1.UMA),
                IsUma = Helpers.Int32ToBool(arch1.UMA),
                VirtualAddressRange = 2UL << (int)va.MaxGPUVirtualAddressBitsPerProcess,
                MergedHeapSupport = opts.ResourceHeapTier == D3D12_RESOURCE_HEAP_TIER.D3D12_RESOURCE_HEAP_TIER_2
            };
        }

        public DeviceInfo Info { get; private set; }

        private struct FreeBlock
        {
            public uint Offset, Length;
        }

        internal ref GenerationalHandleMapper GetMapperRef() => ref _mapper;
        internal ID3D12Device5* GetDevice() => _device.Ptr;
        internal D3D12Fence GetFence(FenceHandle fence) => _mapper.GetInfo(fence);

        public INativeQueue CreateQueue(DeviceContext context) => new D3D12NativeQueue(this, context);

        public FenceHandle CreateFromPreexisting(ID3D12Fence* pFence)
        {
            var fence = new D3D12Fence
            {
                Fence = pFence,
                Flags = ComPtr.TryQueryInterface(pFence, out ID3D12Fence1* pFence1) ? pFence1->GetCreationFlags() : 0
            };

            return _mapper.Create(fence);
        }

        public TextureHandle CreateFromPreexisting(ID3D12Resource* pTexture)
        {
            var desc = pTexture->GetDesc();
            var texture = new D3D12Texture
            {
                Texture = pTexture,
                Width = (uint)desc.Width,
                Height = desc.Height,
                DepthOrArraySize = desc.DepthOrArraySize,
                Format = desc.Format,
                Flags = desc.Flags
            };

            return _mapper.Create(texture);
        }

        public (ulong Alignment, ulong Length) GetTextureAllocationInfo(in TextureDesc desc)
        {
            GetResourceDesc(desc, out var native, out _);

            var info = _device.Ptr->GetResourceAllocationInfo(0, 1, &native);

            return (info.Alignment, info.SizeInBytes);
        }


        public ulong GetCompletedValue(FenceHandle fence) => _mapper.GetInfo(fence).Fence->GetCompletedValue();
        public void Wait(ReadOnlySpan<FenceHandle> fences, ReadOnlySpan<ulong> values, WaitMode mode)
            => SetEvent(default, fences, values, mode);

        public IntPtr GetEventForWait(ReadOnlySpan<FenceHandle> fences, ReadOnlySpan<ulong> values, WaitMode mode)
        {
            var @event = CreateEventW(null, FALSE, FALSE, null);

            SetEvent(@event, fences, values, mode);

            return @event;
        }

        private void SetEvent(IntPtr hEvent, ReadOnlySpan<FenceHandle> fences, ReadOnlySpan<ulong> values, WaitMode mode)
        {
            if (fences.Length == 1)
            {
                var fence = fences[0];
                var value = values[0];

                ThrowIfFailed(_mapper.GetInfo(fence).Fence->SetEventOnCompletion(value, hEvent));
            }

            var nativeFences = ArrayPool<IntPtr>.Shared.Rent(fences.Length);

            int i = 0;
            foreach (var fence in fences)
            {
                nativeFences[i++] = (IntPtr)_mapper.GetInfo(fence).Fence;
            }

            fixed (ulong* pValues = values)
            fixed (void* pFences = nativeFences)
            {
                ThrowIfFailed(_device.Ptr->SetEventOnMultipleFenceCompletion(
                    (ID3D12Fence**)pFences,
                    pValues,
                    (uint)fences.Length,
                    mode switch
                    {
                        WaitMode.WaitForAll => D3D12_MULTIPLE_FENCE_WAIT_FLAGS.D3D12_MULTIPLE_FENCE_WAIT_FLAG_ALL,
                        WaitMode.WaitForAny => D3D12_MULTIPLE_FENCE_WAIT_FLAGS.D3D12_MULTIPLE_FENCE_WAIT_FLAG_ANY,
                        _ => 0,
                    },
                    hEvent
                ));
            }

            ArrayPool<IntPtr>.Shared.Return(nativeFences);
        }

        public FenceHandle CreateFence(ulong initialValue, FenceFlags flags = FenceFlags.None)
        {
            UniqueComPtr<ID3D12Fence> res = default;

            ThrowIfFailed(_device.Ptr->CreateFence(initialValue, (D3D12_FENCE_FLAGS)flags, res.Iid, (void**)&res));

            var fence = new D3D12Fence
            {
                Fence = res.Ptr,
                Flags = (D3D12_FENCE_FLAGS)flags
            };

            return _mapper.Create(fence);
        }

        public void DisposeFence(FenceHandle fence) => _mapper.GetAndFree(fence).Fence->Release();


        public void* Map(BufferHandle buffer) => _mapper.GetInfo(buffer).CpuAddress;
        public void Unmap(BufferHandle buffer) { }

        public BufferHandle AllocateBuffer(in BufferDesc desc, MemoryAccess access)
        {
            UniqueComPtr<ID3D12Resource> res = default;

            var props = GetAccessProperties(access);
            var nativeDesc = D3D12_RESOURCE_DESC.Buffer(desc.Length, (D3D12_RESOURCE_FLAGS)desc.ResourceFlags);

            ThrowIfFailed(_device.Ptr->CreateCommittedResource(
                &props,
                D3D12_HEAP_FLAGS.D3D12_HEAP_FLAG_NONE,
                &nativeDesc,
                DefaultBufferState(props),
                null,
                res.Iid,
                (void**)&res
            ));


            void* cpu = null;

            if (access != MemoryAccess.GpuOnly)
            {
                ThrowIfFailed(res.Ptr->Map(0, null, &cpu));
            }

            var buffer = new D3D12Buffer
            {
                Buffer = res.Ptr,
                GpuAddress = res.Ptr->GetGPUVirtualAddress(),
                CpuAddress = cpu,
                Length = desc.Length
            };

            return _mapper.Create(buffer);
        }

        public BufferHandle AllocateBuffer(in BufferDesc desc, HeapHandle heap, ulong offset)
        {
            UniqueComPtr<ID3D12Resource> res = default;

            var nativeHeap = _mapper.GetInfo(heap);
            var nativeDesc = D3D12_RESOURCE_DESC.Buffer(desc.Length, (D3D12_RESOURCE_FLAGS)desc.ResourceFlags);

            ThrowIfFailed(_device.Ptr->CreatePlacedResource(
                nativeHeap.Heap,
                offset,
                &nativeDesc,
                DefaultBufferState(nativeHeap.Properties),
                null,
                res.Iid,
                (void**)&res
            ));


            void* cpu = null;

            if (nativeHeap.Properties.IsCPUAccessible)
            {
                ThrowIfFailed(res.Ptr->Map(0, null, &cpu));
            }

            var buffer = new D3D12Buffer
            {
                Buffer = res.Ptr,
                Flags = nativeDesc.Flags,
                GpuAddress = res.Ptr->GetGPUVirtualAddress(),
                CpuAddress = cpu,
                Length = desc.Length
            };

            return _mapper.Create(buffer);
        }

        public RaytracingAccelerationStructureHandle AllocateRaytracingAccelerationStructure(ulong length)
        {
            UniqueComPtr<ID3D12Resource> res = default;

            var props = GetRaytracingAccelerationStructureHeapProperties();
            var nativeDesc = D3D12_RESOURCE_DESC.Buffer(length, D3D12_RESOURCE_FLAGS.D3D12_RESOURCE_FLAG_ALLOW_UNORDERED_ACCESS);

            ThrowIfFailed(_device.Ptr->CreateCommittedResource(
                &props,
                D3D12_HEAP_FLAGS.D3D12_HEAP_FLAG_NONE,
                &nativeDesc,
                DefaultBufferState(props),
                null,
                res.Iid,
                (void**)&res
            ));


            var buffer = new D3D12RaytracingAccelerationStructure
            {
                RaytracingAccelerationStructure = res.Ptr,
                Address = res.Ptr->GetGPUVirtualAddress(),
                Length = length
            };

            return _mapper.Create(buffer);
        }

        public RaytracingAccelerationStructureHandle AllocateRaytracingAccelerationStructure(ulong length, HeapHandle heap, ulong offset)
        {
            UniqueComPtr<ID3D12Resource> res = default;

            var nativeHeap = _mapper.GetInfo(heap);
            var nativeDesc = D3D12_RESOURCE_DESC.Buffer(length, D3D12_RESOURCE_FLAGS.D3D12_RESOURCE_FLAG_ALLOW_UNORDERED_ACCESS);

            ThrowIfFailed(_device.Ptr->CreatePlacedResource(
                nativeHeap.Heap,
                offset,
                &nativeDesc,
                DefaultBufferState(nativeHeap.Properties),
                null,
                res.Iid,
                (void**)&res
            ));


            var buffer = new D3D12RaytracingAccelerationStructure
            {
                RaytracingAccelerationStructure = res.Ptr,
                Address = res.Ptr->GetGPUVirtualAddress(),
                Length = length
            };

            return _mapper.Create(buffer);
        }


        public TextureHandle AllocateTexture(in TextureDesc desc, ResourceState initial)
        {
            GetResourceDesc(desc, out var nativeDesc, out var clearValue);

            UniqueComPtr<ID3D12Resource> res = default;

            var props = GetRaytracingAccelerationStructureHeapProperties();

            ThrowIfFailed(_device.Ptr->CreateCommittedResource(
                &props,
                D3D12_HEAP_FLAGS.D3D12_HEAP_FLAG_NONE,
                &nativeDesc,
                (D3D12_RESOURCE_STATES)initial,
                &clearValue,
                res.Iid,
                (void**)&res
            ));

            var texture = new D3D12Texture
            {
                Texture = res.Ptr,
                Width = (uint)nativeDesc.Width,
                Height = nativeDesc.Height,
                DepthOrArraySize = nativeDesc.DepthOrArraySize,
                Format = nativeDesc.Format,
                Flags = nativeDesc.Flags
            };

            return _mapper.Create(texture);
        }

        public TextureHandle AllocateTexture(in TextureDesc desc, ResourceState initial, HeapHandle heap, ulong offset)
        {
            GetResourceDesc(desc, out var nativeDesc, out var clearValue);

            UniqueComPtr<ID3D12Resource> res = default;

            var nativeHeap = _mapper.GetInfo(heap);

            ThrowIfFailed(_device.Ptr->CreatePlacedResource(
                nativeHeap.Heap,
                offset,
                &nativeDesc,
                (D3D12_RESOURCE_STATES)initial,
                &clearValue,
                res.Iid,
                (void**)&res
            ));

            var texture = new D3D12Texture
            {
                Texture = res.Ptr,
                Width = (uint)nativeDesc.Width,
                Height = nativeDesc.Height,
                DepthOrArraySize = nativeDesc.DepthOrArraySize,
                Format = nativeDesc.Format,
                Flags = nativeDesc.Flags
            };

            return _mapper.Create(texture);
        }

        public HeapHandle CreateHeap(ulong size, in HeapInfo info)
        {
            UniqueComPtr<ID3D12Heap> res = default;

            var desc = new D3D12_HEAP_DESC
            {
                Alignment = (ulong)info.Alignment,
                Properties = GetAccessProperties(info.Access),
                SizeInBytes = size
            };

            ThrowIfFailed(_device.Ptr->CreateHeap(&desc, res.Iid, (void**)&res));

            var heap = new D3D12Heap
            {
                Heap = res.Ptr,
                Alignment = desc.Alignment,
                Properties = desc.Properties,
                Length = size
            };

            return _mapper.Create(heap);
        }

        public DescriptorSetHandle CreateDescriptorSet(DescriptorType type, uint count)
        {
            for (var i = 0; i < _freeList.Length; i++)
            {
                ref var block = ref _freeList.RefIndex(i);

                if (block.Length >= count)
                {
                    var offset = block.Offset;
                    block.Offset += count;
                    block.Length -= count;

                    var (gen, id) = Helpers.Pack2x24_16To2x32(offset, count, (ushort)type);
                    return new DescriptorSetHandle(new GenerationalHandle(gen, id));
                }
            }

            ThrowHelper.ThrowInsufficientMemoryException("Too many descriptors! Or overly fragmented heap :(");
            return default;
        }

        public void DisposeDescriptorSet(DescriptorSetHandle handle)
        {
            var (offset, count, _) = Helpers.Unpack2x32To2x24_16(handle.Generational.Generation, handle.Generational.Id);

            var block = new FreeBlock { Offset = offset, Length = count };

            _freeList.Add(block);
        }

        public void UpdateDescriptors(ViewSetHandle viewSet, uint firstView, DescriptorSetHandle descriptors, uint firstDescriptor, uint count)
        {
            var views = _mapper.GetInfo(viewSet);

            var (offset, length, type) = Helpers.Unpack2x32To2x24_16(descriptors.Generational.Generation, descriptors.Generational.Id);

            var dest = GetShaderDescriptorForCpu(offset + firstDescriptor);

            D3D12_CPU_DESCRIPTOR_HANDLE src = default;

            switch ((DescriptorType)type)
            {
                case DescriptorType.ConstantBuffer:
                    src = GetCbv(views.FirstShaderResources, firstView, views.Length);
                    break;

                case DescriptorType.StructuredBuffer:
                case DescriptorType.RaytracingAccelerationStructure:
                case DescriptorType.TypedBuffer:
                case DescriptorType.Texture:
                    src = GetSrv(views.FirstShaderResources, firstView, views.Length);
                    break;

                case DescriptorType.WritableStructuredBuffer:
                case DescriptorType.WritableTypedBuffer:
                case DescriptorType.WritableTexture:
                    src = GetUav(views.FirstShaderResources, firstView, views.Length);
                    break;

                case DescriptorType.Sampler:
                    break;

                case DescriptorType.DynamicConstantBuffer:
                case DescriptorType.DynamicWritableStructuredBuffer:
                case DescriptorType.DynamicStructuredBuffer:
                    break;
            }

            _device.Ptr->CopyDescriptorsSimple(count, dest, GetCbv(views.FirstShaderResources, firstView, views.Length), D3D12_DESCRIPTOR_HEAP_TYPE.D3D12_DESCRIPTOR_HEAP_TYPE_CBV_SRV_UAV);
        }

        public void CopyDescriptors(DescriptorSetHandle source, uint firstSource, DescriptorSetHandle dest, uint firstDest, uint count) => throw new NotImplementedException();

        public ViewSetHandle CreateViewSet(uint viewCount)
        {
            var desc = new D3D12_DESCRIPTOR_HEAP_DESC
            {
                NumDescriptors = viewCount
            };

            UniqueComPtr<ID3D12DescriptorHeap> renderTarget = default, depthStencil = default, shaderResources = default;

            desc.Type = D3D12_DESCRIPTOR_HEAP_TYPE.D3D12_DESCRIPTOR_HEAP_TYPE_RTV;
            ThrowIfFailed(_device.Ptr->CreateDescriptorHeap(&desc, renderTarget.Iid, (void**)&renderTarget));

            desc.Type = D3D12_DESCRIPTOR_HEAP_TYPE.D3D12_DESCRIPTOR_HEAP_TYPE_DSV;
            ThrowIfFailed(_device.Ptr->CreateDescriptorHeap(&desc, depthStencil.Iid, (void**)&depthStencil));

            desc.Type = D3D12_DESCRIPTOR_HEAP_TYPE.D3D12_DESCRIPTOR_HEAP_TYPE_CBV_SRV_UAV;
            desc.NumDescriptors *= 3;
            ThrowIfFailed(_device.Ptr->CreateDescriptorHeap(&desc, shaderResources.Iid, (void**)&shaderResources));

            var viewSet = new D3D12ViewSet
            {
                RenderTarget = renderTarget.Ptr,
                FirstRenderTarget = renderTarget.Ptr->GetCPUDescriptorHandleForHeapStart(),

                DepthStencil = depthStencil.Ptr,
                FirstDepthStencil = depthStencil.Ptr->GetCPUDescriptorHandleForHeapStart(),

                ShaderResources = shaderResources.Ptr,
                FirstShaderResources = shaderResources.Ptr->GetCPUDescriptorHandleForHeapStart(),

                Length = viewCount
            };

            return _mapper.Create(viewSet);
        }


        public void DisposeViewSet(ViewSetHandle handle)
        {
            var info = _mapper.GetAndFree(handle);

            if (info.RenderTarget != null)
            {
                info.RenderTarget->Release();
            }
            if (info.DepthStencil != null)
            {
                info.DepthStencil->Release();
            }
            if (info.ShaderResources != null)
            {
                info.ShaderResources->Release();
            }
        }

        public ViewHandle CreateView(ViewSetHandle viewHeap, uint index, BufferHandle handle)
        {
            var views = _mapper.GetInfo(viewHeap);
            var buffer = _mapper.GetInfo(handle);

            var srv = GetSrv(views.FirstShaderResources, index, views.Length);
            var uav = GetUav(views.FirstShaderResources, index, views.Length);
            var cbv = GetCbv(views.FirstShaderResources, index, views.Length);

            if (buffer.Flags.HasFlag(D3D12_RESOURCE_FLAGS.D3D12_RESOURCE_FLAG_ALLOW_UNORDERED_ACCESS))
            {
                _device.Ptr->CreateUnorderedAccessView(buffer.Buffer, null, null, uav);
            }

            _device.Ptr->CreateShaderResourceView(buffer.Buffer, null, srv);

            var desc = new D3D12_CONSTANT_BUFFER_VIEW_DESC
            {
                BufferLocation = buffer.GpuAddress,
                SizeInBytes = (uint)buffer.Length
            };

            _device.Ptr->CreateConstantBufferView(&desc, cbv);

            var view = new D3D12View
            {
                Resource = buffer.Buffer,
                Format = DXGI_FORMAT.DXGI_FORMAT_UNKNOWN,
                ShaderResource = srv,
                ConstantBuffer = cbv,
                UnorderedAccess = uav,
                Length = buffer.Length
            };

            return _mapper.Create(view);
        }

        public ViewHandle CreateView(ViewSetHandle viewHeap, uint index, BufferHandle handle, in BufferViewDesc desc) => throw new NotImplementedException();

        public ViewHandle CreateView(ViewSetHandle viewHeap, uint index, TextureHandle handle)
        {
            var views = _mapper.GetInfo(viewHeap);
            var texture = _mapper.GetInfo(handle);

            var srv = GetSrv(views.FirstShaderResources, index, views.Length);
            var uav = GetUav(views.FirstShaderResources, index, views.Length);
            var rtv = views.FirstRenderTarget.Offset(_rtvDescriptorSize, index);
            var dsv = views.FirstDepthStencil.Offset(_dsvDescriptorSize, index);

            if (texture.Flags.HasFlag(D3D12_RESOURCE_FLAGS.D3D12_RESOURCE_FLAG_ALLOW_UNORDERED_ACCESS))
            {
                _device.Ptr->CreateUnorderedAccessView(texture.Texture, null, null, uav);
            }
            if (texture.Flags.HasFlag(D3D12_RESOURCE_FLAGS.D3D12_RESOURCE_FLAG_ALLOW_DEPTH_STENCIL))
            {
                _device.Ptr->CreateDepthStencilView(texture.Texture, null, dsv);
            }
            if (texture.Flags.HasFlag(D3D12_RESOURCE_FLAGS.D3D12_RESOURCE_FLAG_ALLOW_RENDER_TARGET))
            {
                _device.Ptr->CreateRenderTargetView(texture.Texture, null, rtv);
            }
            if (!texture.Flags.HasFlag(D3D12_RESOURCE_FLAGS.D3D12_RESOURCE_FLAG_DENY_SHADER_RESOURCE)
                && !texture.Flags.HasFlag(D3D12_RESOURCE_FLAGS.D3D12_RESOURCE_FLAG_ALLOW_DEPTH_STENCIL))
            {
                // depth stencils have no default view
                _device.Ptr->CreateShaderResourceView(texture.Texture, null, srv);
            }

            var view = new D3D12View
            {
                Resource = texture.Texture,
                Format = DXGI_FORMAT.DXGI_FORMAT_UNKNOWN,
                ShaderResource = srv,
                UnorderedAccess = uav,
                DepthStencil = dsv,
                RenderTarget = rtv,
                Width = texture.Width,
                Height = texture.Height,
                DepthOrArraySize = texture.DepthOrArraySize
            };

            return _mapper.Create(view);
        }

        public ViewHandle CreateView(ViewSetHandle viewHeap, uint index, TextureHandle handle, in TextureViewDesc desc) => throw new NotImplementedException();
        public ViewHandle CreateView(ViewSetHandle viewHeap, uint index, RaytracingAccelerationStructureHandle handle)
        {
            var views = _mapper.GetInfo(viewHeap);
            var accelerationStructure = _mapper.GetInfo(handle);

            var srv = GetSrv(views.FirstShaderResources, index, views.Length);

            D3D12_SHADER_RESOURCE_VIEW_DESC desc;
            _ = &desc;

            desc.Format = DXGI_FORMAT.DXGI_FORMAT_UNKNOWN;
            desc.ViewDimension = D3D12_SRV_DIMENSION.D3D12_SRV_DIMENSION_RAYTRACING_ACCELERATION_STRUCTURE;
            desc.RaytracingAccelerationStructure.Location = accelerationStructure.Address;

            _device.Ptr->CreateShaderResourceView(null, &desc, srv);

            var view = new D3D12View
            {
                Resource = accelerationStructure.RaytracingAccelerationStructure,
                Format = DXGI_FORMAT.DXGI_FORMAT_UNKNOWN,
                ShaderResource = srv
            };

            return _mapper.Create(view);
        }


        public PipelineHandle CreatePipeline(in ComputePipelineDesc desc) => throw new NotImplementedException();


        public PipelineHandle CreatePipeline(in GraphicsPipelineDesc desc)
        {
            desc.SetMarkers(null);
            nuint a = (nuint)desc.Desc.RootSig.Pointer;
            desc.Desc.RootSig.Pointer = _mapper.GetInfo(Unsafe.As<nuint, RootSignatureHandle>(ref a)).RootSignature;

            fixed (void* p = desc)
            {
                UniqueComPtr<ID3D12PipelineState> pso = default;
                var psoDesc = new D3D12_PIPELINE_STATE_STREAM_DESC
                {
                    pPipelineStateSubobjectStream = p,
                    SizeInBytes = desc.DescSize
                };

                ThrowIfFailed(_device.Ptr->CreatePipelineState(
                    &psoDesc,
                    pso.Iid,
                    (void**)&pso
                ));

                var pipelineState = new D3D12PipelineState
                {
                    BindPoint = BindPoint.Graphics,
                    PipelineState = (ID3D12Object*)pso.Ptr,
                    RootSignature = desc.Desc.RootSig.Pointer,
                    RootParameters = _mapper.GetInfo(Unsafe.As<nuint, RootSignatureHandle>(ref a)).RootParameters
                };

                return _mapper.Create(pipelineState);
            }
        }

        //public PipelineHandle CreatePipeline(in RaytracingPipelineDesc desc) => throw new NotImplementedException();
        public PipelineHandle CreatePipeline(in MeshPipelineDesc desc) => throw new NotImplementedException();

        public QuerySetHandle CreateQuerySet(QuerySetType type, uint length)
        {
            var desc = new D3D12_QUERY_HEAP_DESC
            {
                Type = (D3D12_QUERY_HEAP_TYPE)type,
                Count = length
            };

            UniqueComPtr<ID3D12QueryHeap> res = default;

            ThrowIfFailed(_device.Ptr->CreateQueryHeap(&desc, res.Iid, (void**)&res));

            var queryHeap = new D3D12QueryHeap
            {
                QueryHeap = res.Ptr,
                Length = length
            };

            return _mapper.Create(queryHeap);
        }

        public RootSignatureHandle CreateRootSignature(ReadOnlySpan<RootParameter> rootParameters, ReadOnlySpan<StaticSampler> staticSamplers, RootSignatureFlags flags)
        {
            using var rootParams = RentedArray<D3D12_ROOT_PARAMETER1>.Create(rootParameters.Length);
            using var samplers = RentedArray<D3D12_STATIC_SAMPLER_DESC>.Create(staticSamplers.Length);

            TranslateRootParameters(rootParameters, rootParams.Value);
            TranslateStaticSamplers(staticSamplers, samplers.Value);

            fixed (D3D12_ROOT_PARAMETER1* pRootParams = rootParams.Value)
            fixed (D3D12_STATIC_SAMPLER_DESC* pSamplerDesc = samplers.Value)
            {
                var desc = new D3D12_ROOT_SIGNATURE_DESC1
                {
                    NumParameters = (uint)rootParameters.Length,
                    pParameters = pRootParams,
                    NumStaticSamplers = (uint)staticSamplers.Length,
                    pStaticSamplers = pSamplerDesc,
                    Flags = (D3D12_ROOT_SIGNATURE_FLAGS)flags
                };

                var versionedDesc = new D3D12_VERSIONED_ROOT_SIGNATURE_DESC
                {
                    Version = D3D_ROOT_SIGNATURE_VERSION.D3D_ROOT_SIGNATURE_VERSION_1_1,
                    Desc_1_1 = desc
                };

                ID3DBlob* pBlob = default;
                ID3DBlob* pError = default;
                int hr = Windows.D3D12SerializeVersionedRootSignature(
                    &versionedDesc,
                    &pBlob,
                    &pError
                );

                if (FAILED(hr))
                {
                    var message = pError is null ? string.Empty : pError->AsDxcBlob()->GetString(Encoding.ASCII);
                    ThrowHelper.ThrowExternalException(hr, message);
                }

                UniqueComPtr<ID3D12RootSignature> pRootSig = default;
                ThrowIfFailed(_device.Ptr->CreateRootSignature(
                    0 /* TODO: MULTI-GPU */,
                    pBlob->GetBufferPointer(),
                    (uint)pBlob->GetBufferSize(),
                    pRootSig.Iid,
                    (void**)&pRootSig
                ));

                var rootSig = new D3D12RootSignature
                {
                    RootSignature = pRootSig.Ptr,
                    RootParameters = ImmutableArray.Create(rootParameters.ToArray())
                };

                return _mapper.Create(rootSig);
            }
        }

        private static void TranslateRootParameters(ReadOnlySpan<RootParameter> rootParameters, Span<D3D12_ROOT_PARAMETER1> outRootParams)
        {
            for (var i = 0; i < rootParameters.Length; i++)
            {
                var inRootParam = rootParameters[i];
                D3D12_ROOT_PARAMETER1 outRootParam = new D3D12_ROOT_PARAMETER1
                {
                    ParameterType = (D3D12_ROOT_PARAMETER_TYPE)inRootParam.Type,
                    ShaderVisibility = (D3D12_SHADER_VISIBILITY)inRootParam.Visibility
                };
                switch (inRootParam.Type)
                {
                    case RootParameterType.DescriptorTable:
                        outRootParam.DescriptorTable = new D3D12_ROOT_DESCRIPTOR_TABLE1
                        {
                            NumDescriptorRanges = (uint)inRootParam.DescriptorTable!.Length,
                            // IMPORTANT: we *know* this is pinned, because it can only come from RootParameter.CreateDescriptorTable, which strictly makes sure it is pinned
                            pDescriptorRanges = (D3D12_DESCRIPTOR_RANGE1*)Unsafe.AsPointer(
                                ref MemoryMarshal.GetArrayDataReference(inRootParam.DescriptorTable)
                            )
                        };
                        break;

                    case RootParameterType.DwordConstants:
                        outRootParam.Constants = inRootParam.Constants;
                        break;

                    case RootParameterType.ConstantBufferView:
                    case RootParameterType.ShaderResourceView:
                    case RootParameterType.UnorderedAccessView:
                        outRootParam.Descriptor = inRootParam.Descriptor;
                        break;
                }

                outRootParams[i] = outRootParam;
            }
        }

        private static void TranslateStaticSamplers(ReadOnlySpan<StaticSampler> staticSamplers, Span<D3D12_STATIC_SAMPLER_DESC> samplers)
        {
            for (var i = 0; i < staticSamplers.Length; i++)
            {
                var staticSampler = staticSamplers[i];

                ref readonly var desc = ref staticSampler.Sampler.Desc;

                D3D12_STATIC_BORDER_COLOR staticBorderColor;
                var borderColor = Rgba128.FromRef(ref Unsafe.AsRef(in desc.BorderColor[0]));

                if (borderColor == StaticSampler.OpaqueBlack)
                {
                    staticBorderColor = D3D12_STATIC_BORDER_COLOR.D3D12_STATIC_BORDER_COLOR_OPAQUE_BLACK;
                }
                else if (borderColor == StaticSampler.OpaqueWhite)
                {
                    staticBorderColor = D3D12_STATIC_BORDER_COLOR.D3D12_STATIC_BORDER_COLOR_OPAQUE_WHITE;
                }
                else if (borderColor == StaticSampler.TransparentBlack)
                {
                    staticBorderColor = D3D12_STATIC_BORDER_COLOR.D3D12_STATIC_BORDER_COLOR_TRANSPARENT_BLACK;
                }
                else
                {
                    ThrowHelper.ThrowArgumentException("Static sampler must have opaque black, opaque white, or transparent black border color");
                    staticBorderColor = default;
                }

                var sampler = new D3D12_STATIC_SAMPLER_DESC
                {
                    AddressU = desc.AddressU,
                    AddressW = desc.AddressW,
                    AddressV = desc.AddressV,
                    ComparisonFunc = desc.ComparisonFunc,
                    BorderColor = staticBorderColor,
                    Filter = desc.Filter,
                    MaxAnisotropy = desc.MaxAnisotropy,
                    MaxLOD = desc.MaxLOD,
                    MinLOD = desc.MinLOD,
                    MipLODBias = desc.MipLODBias,
                    RegisterSpace = staticSampler.RegisterSpace,
                    ShaderRegister = staticSampler.ShaderRegister,
                    ShaderVisibility = (D3D12_SHADER_VISIBILITY)staticSampler.Visibility
                };

                samplers[i] = sampler;
            }
        }

        public void DisposeBuffer(BufferHandle handle) => _mapper.GetAndFree(handle).Buffer->Release();
        public void DisposeHeap(HeapHandle handle) => _mapper.GetAndFree(handle).Heap->Release();
        public void DisposePipeline(PipelineHandle handle) => _mapper.GetAndFree(handle).PipelineState->Release();
        public void DisposeQuerySet(QuerySetHandle handle) => _mapper.GetAndFree(handle).QueryHeap->Release();
        public void DisposeRaytracingAccelerationStructure(RaytracingAccelerationStructureHandle handle) => _mapper.GetAndFree(handle).RaytracingAccelerationStructure->Release();
        public void DisposeRootSignature(RootSignatureHandle handle) => _mapper.GetAndFree(handle).RootSignature->Release();
        public void DisposeTexture(TextureHandle handle) => _mapper.GetAndFree(handle).Texture->Release();
        public void DisposeView(ViewHandle handle) => _mapper.GetAndFree(handle);

        public IndirectCommandHandle CreateIndirectCommand(in RootSignature rootSig, ReadOnlySpan<IndirectArgument> arguments, uint byteStride) => throw new NotImplementedException();
        public IndirectCommandHandle CreateIndirectCommand(in IndirectArgument arguments, uint byteStride) => throw new NotImplementedException();

        public void Dispose()
        {
            _device.Dispose();
        }


        private void GetResourceDesc(
            in TextureDesc desc,
            out D3D12_RESOURCE_DESC nativeDesc,
            out D3D12_CLEAR_VALUE clearVal
        )
        {
            DXGI_SAMPLE_DESC sample = new DXGI_SAMPLE_DESC(desc.Msaa.SampleCount, desc.Msaa.QualityLevel);

            // Normalize default
            if (desc.Msaa.SampleCount == 0)
            {
                sample.Count = 1;
            }

            nativeDesc = new D3D12_RESOURCE_DESC
            {
                Dimension = (D3D12_RESOURCE_DIMENSION)desc.Dimension,
                Alignment = 0,
                Width = desc.Width,
                Height = Math.Max(1, desc.Height),
                DepthOrArraySize = Math.Max((ushort)1, desc.DepthOrArraySize),
                MipLevels = desc.MipCount,
                Format = (DXGI_FORMAT)desc.Format,
                Flags = (D3D12_RESOURCE_FLAGS)desc.ResourceFlags,
                SampleDesc = sample,
                Layout = (D3D12_TEXTURE_LAYOUT)desc.Layout
            };

            clearVal = new D3D12_CLEAR_VALUE { Format = nativeDesc.Format };

            var val = desc.ClearValue.GetValueOrDefault();
            if (desc.ResourceFlags.HasFlag(ResourceFlags.AllowRenderTarget))
            {
                Unsafe.As<float, Rgba128>(ref clearVal.Anonymous.Color[0]) = val.Color;
            }
            else if (desc.ResourceFlags.HasFlag(ResourceFlags.AllowDepthStencil))
            {
                clearVal.Anonymous.DepthStencil.Depth = val.Depth;
                clearVal.Anonymous.DepthStencil.Stencil = val.Stencil;
            }
        }

        private D3D12_CPU_DESCRIPTOR_HANDLE GetSrv(D3D12_CPU_DESCRIPTOR_HANDLE handle, uint index, uint count) => handle.Offset(_resDescriptorSize, index);
        private D3D12_CPU_DESCRIPTOR_HANDLE GetUav(D3D12_CPU_DESCRIPTOR_HANDLE handle, uint index, uint count) => handle.Offset(_resDescriptorSize, (count / 3) + index);
        private D3D12_CPU_DESCRIPTOR_HANDLE GetCbv(D3D12_CPU_DESCRIPTOR_HANDLE handle, uint index, uint count) => handle.Offset(_resDescriptorSize, ((count / 3) * 2) + index);



        internal D3D12_CPU_DESCRIPTOR_HANDLE GetShaderDescriptorForCpu(uint index)
        {
            var handle = _firstShaderDescriptor;
            return handle.Offset(_resDescriptorSize, index);
        }
        internal D3D12_GPU_DESCRIPTOR_HANDLE GetShaderDescriptorForGpu(uint index)
        {
            var handle = _firstShaderDescriptorGpu;
            return handle.Offset(_resDescriptorSize, index);
        }

        private D3D12_HEAP_PROPERTIES GetRaytracingAccelerationStructureHeapProperties() => new D3D12_HEAP_PROPERTIES(D3D12_HEAP_TYPE.D3D12_HEAP_TYPE_DEFAULT);
        private D3D12_HEAP_PROPERTIES GetAccessProperties(MemoryAccess access) => new D3D12_HEAP_PROPERTIES((D3D12_HEAP_TYPE)access);
        private D3D12_RESOURCE_STATES DefaultBufferState(in D3D12_HEAP_PROPERTIES props) => props.Type switch
        {
            D3D12_HEAP_TYPE.D3D12_HEAP_TYPE_UPLOAD => D3D12_RESOURCE_STATES.D3D12_RESOURCE_STATE_GENERIC_READ,
            D3D12_HEAP_TYPE.D3D12_HEAP_TYPE_READBACK => D3D12_RESOURCE_STATES.D3D12_RESOURCE_STATE_COPY_DEST,
            _ => D3D12_RESOURCE_STATES.D3D12_RESOURCE_STATE_COMMON
        };


        private void CheckFeatureSupport<TFeature>(D3D12_FEATURE feature, out TFeature p) where TFeature : unmanaged
        {
            fixed (TFeature* pp = &p)
            {
                CheckFeatureSupport(feature, pp);
            }
        }
        private void CheckFeatureSupport<TFeature>(D3D12_FEATURE feature, TFeature* p) where TFeature : unmanaged
        {
            ThrowIfFailed(_device.Ptr->CheckFeatureSupport(feature, p, (uint)sizeof(TFeature)));
        }

        private static void GetMajorMinorForShaderModel(D3D_SHADER_MODEL model, out int major, out int minor)
            => (major, minor) = model switch
            {
                D3D_SHADER_MODEL.D3D_SHADER_MODEL_5_1 => (5, 1),
                D3D_SHADER_MODEL.D3D_SHADER_MODEL_6_0 => (6, 0),
                D3D_SHADER_MODEL.D3D_SHADER_MODEL_6_1 => (6, 1),
                D3D_SHADER_MODEL.D3D_SHADER_MODEL_6_2 => (6, 2),
                D3D_SHADER_MODEL.D3D_SHADER_MODEL_6_3 => (6, 3),
                D3D_SHADER_MODEL.D3D_SHADER_MODEL_6_4 => (6, 4),
                D3D_SHADER_MODEL.D3D_SHADER_MODEL_6_5 => (6, 5),
                D3D_SHADER_MODEL.D3D_SHADER_MODEL_6_6 => (6, 6),
                _ => throw new ArgumentException()
            };

        /// <summary>
        /// Throws if a given HR is a fail code. Also properly handles device-removed error codes, unlike Guard.ThrowIfFailed
        /// </summary>
        [MethodImpl(MethodTypes.Validates)]
        internal void ThrowIfFailed(
            int hr,
            [CallerArgumentExpression("hr")] string? expression = null
#if DEBUG || EXTENDED_ERROR_INFORMATION
            ,
            [CallerFilePath] string? filepath = default,
            [CallerMemberName] string? memberName = default,
            [CallerLineNumber] int lineNumber = default
#endif
        )
        {
            // invert branch so JIT assumes the HR is S_OK
            if (SUCCEEDED(hr))
            {
                return;
            }

            HrIsFail(this, hr
#if DEBUG || EXTENDED_ERROR_INFORMATION
                , expression, filepath, memberName, lineNumber
#endif
                );

            [MethodImpl(MethodImplOptions.NoInlining)]
            static void HrIsFail(D3D12NativeDevice device, int hr

#if DEBUG || EXTENDED_ERROR_INFORMATION
                                , string? expression, string? filepath, string? memberName, int lineNumber
#endif
)
            {
                if (hr is DXGI_ERROR_DEVICE_REMOVED or DXGI_ERROR_DEVICE_RESET or DXGI_ERROR_DEVICE_HUNG)
                {
                    throw new DeviceDisconnectedException(/* TODO */ null!, TranslateReason(device._device.Ptr->GetDeviceRemovedReason()));
                }

                static DeviceDisconnectReason TranslateReason(int hr) => hr switch
                {
                    DXGI_ERROR_DEVICE_REMOVED => DeviceDisconnectReason.Removed,
                    DXGI_ERROR_DEVICE_HUNG => DeviceDisconnectReason.Hung,
                    DXGI_ERROR_DEVICE_RESET => DeviceDisconnectReason.Reset,
                    DXGI_ERROR_DRIVER_INTERNAL_ERROR => DeviceDisconnectReason.InternalDriverError,
                    _ => DeviceDisconnectReason.Unknown
                };

                Guard.ThrowForHr(hr
#if DEBUG || EXTENDED_ERROR_INFORMATION
                    ,
                    expression, filepath, memberName, lineNumber
#endif
                    );
            }
        }
    }
}
