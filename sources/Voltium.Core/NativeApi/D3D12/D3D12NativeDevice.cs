using System;
using System.Buffers;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using TerraFX.Interop;
using Voltium.Common;
using Voltium.Core.Contexts;
using Voltium.Core.Memory;
using Voltium.Core.Pipeline;
using Voltium.Core.Queries;
using TerraFX.Interop.Windows;
using TerraFX.Interop.DirectX;
using static TerraFX.Interop.Windows.Windows;
using static TerraFX.Interop.DirectX.DirectX;
using static TerraFX.Interop.DirectX.DXGI;
using static TerraFX.Interop.DirectX.D3D12;
using static TerraFX.Interop.DirectX.D3D12_FEATURE;
using static TerraFX.Interop.DirectX.D3D12_PIPELINE_STATE_SUBOBJECT_TYPE;
using System.Runtime.InteropServices;
using System.Collections.Immutable;
using Voltium.Core.NativeApi;
using Voltium.Core.NativeApi.D3D12;
using Voltium.Allocators;
using System.Runtime.Versioning;

namespace Voltium.Core.Devices
{
    /// <summary>
    /// The native device type used to interface with a GPU
    /// </summary>
    [SupportedOSPlatform("windows10.0.17763.0")]
    public unsafe class D3D12NativeDevice : INativeDevice
    {
        private const uint ShaderVisibleDescriptorCount = 1024 * 512, ShaderVisibleSamplerCount = 1024;

        private UniqueComPtr<ID3D12Device5> _device;
        private D3D12HandleMapper _mapper;
        private ValueList<FreeBlock> _freeList;
        private int _resDescriptorSize, _rtvDescriptorSize, _dsvDescriptorSize;
        private UniqueComPtr<ID3D12DescriptorHeap> _shaderDescriptors;
        private UniqueComPtr<ID3D12DescriptorHeap> _samplerDescriptors;
        private D3D12_CPU_DESCRIPTOR_HANDLE _firstShaderDescriptor;
        private D3D12_GPU_DESCRIPTOR_HANDLE _firstShaderDescriptorGpu;
        private D3D12_CPU_DESCRIPTOR_HANDLE _firstSamplerDescriptor;
        private D3D12_GPU_DESCRIPTOR_HANDLE _firstSamplerDescriptorGpu;
        private uint _numShaderDescriptors;
        private uint _numSamplerDescriptors;

        internal void DefaultDescriptorHeaps(out ID3D12DescriptorHeap* resources, out ID3D12DescriptorHeap* samplers)
        {
            resources = _shaderDescriptors.Ptr;
            samplers = _samplerDescriptors.Ptr;
        }

        /// <summary>
        /// Create a new D3D12 device
        /// </summary>
        /// <param name="fl">The <see cref="D3D_FEATURE_LEVEL"/> this device requires</param>
        public D3D12NativeDevice(D3D_FEATURE_LEVEL fl)
        {
            _mapper = new(false);

            {
                using UniqueComPtr<ID3D12Debug> debug = default;
                DirectX.D3D12GetDebugInterface(debug.Iid, (void**)&debug);
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

            UniqueComPtr<ID3D12DescriptorHeap> shaderVisibleSamplers = default;
            desc = new D3D12_DESCRIPTOR_HEAP_DESC
            {
                NumDescriptors = 2048 /* max */,
                Type = D3D12_DESCRIPTOR_HEAP_TYPE.D3D12_DESCRIPTOR_HEAP_TYPE_SAMPLER,
                Flags = D3D12_DESCRIPTOR_HEAP_FLAGS.D3D12_DESCRIPTOR_HEAP_FLAG_SHADER_VISIBLE
            };

            ThrowIfFailed(device.Ptr->CreateDescriptorHeap(&desc, shaderVisibleSamplers.Iid, (void**)&shaderVisibleSamplers));

            _samplerDescriptors = shaderVisibleSamplers;
            _firstSamplerDescriptor = shaderVisible.Ptr->GetCPUDescriptorHandleForHeapStart();
            _firstSamplerDescriptorGpu = shaderVisible.Ptr->GetGPUDescriptorHandleForHeapStart();
            _numSamplerDescriptors = desc.NumDescriptors;

            _freeList = new(1, ArrayPool<FreeBlock>.Shared);
            _freeList.Add(new FreeBlock { Offset = 0, Length = ShaderVisibleDescriptorCount });

            CheckFeatureSupport(D3D12_FEATURE_D3D12_OPTIONS, out D3D12_FEATURE_DATA_D3D12_OPTIONS opts);
            CheckFeatureSupport(D3D12_FEATURE_ARCHITECTURE1, out D3D12_FEATURE_DATA_ARCHITECTURE1 arch1);
            CheckFeatureSupport(D3D12_FEATURE_GPU_VIRTUAL_ADDRESS_SUPPORT, out D3D12_FEATURE_DATA_GPU_VIRTUAL_ADDRESS_SUPPORT va);
            Info = new()
            {
                IsCacheCoherent = Helpers.Int32ToBool(arch1.CacheCoherentUMA) || !Helpers.Int32ToBool(arch1.UMA),
                IsUma = Helpers.Int32ToBool(arch1.UMA),
                VirtualAddressRange = 2UL << (int)va.MaxGPUVirtualAddressBitsPerProcess,
                MergedHeapSupport = opts.ResourceHeapTier == D3D12_RESOURCE_HEAP_TIER.D3D12_RESOURCE_HEAP_TIER_2,
                RaytracingShaderIdentifierSize = D3D12_SHADER_IDENTIFIER_SIZE_IN_BYTES
            };
        }

        /// <inheritdoc />
        public DeviceInfo Info { get; private set; }

        private struct FreeBlock
        {
            public uint Offset, Length;
        }

        internal ref D3D12HandleMapper GetMapperRef() => ref _mapper;
        internal ID3D12Device5* GetDevice() => _device.Ptr;
        internal D3D12Fence GetFence(FenceHandle fence) => _mapper.GetInfo(fence);

        /// <inheritdoc />
        public INativeQueue CreateQueue(ExecutionEngine context) => new D3D12NativeQueue(this, context);

        /// <inheritdoc />
        public FenceHandle CreateFromPreexisting(ID3D12Fence* pFence)
        {
            var fence = new D3D12Fence
            {
                Fence = pFence,
                Flags = ComPtr.TryQueryInterface(pFence, out ID3D12Fence1* pFence1) ? pFence1->GetCreationFlags() : 0
            };

            return _mapper.Create(fence);
        }

        /// <inheritdoc />
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

        /// <inheritdoc />
        public (ulong Alignment, ulong Length) GetTextureAllocationInfo(in TextureDesc desc)
        {
            GetResourceDesc(desc, out var native, out _);

            var info = _device.Ptr->GetResourceAllocationInfo(0, 1, &native);

            return (info.Alignment, info.SizeInBytes);
        }

        /// <inheritdoc />

        public ulong GetCompletedValue(FenceHandle fence) => _mapper.GetInfo(fence).Fence->GetCompletedValue();

        /// <inheritdoc />
        public void Wait(ReadOnlySpan<FenceHandle> fences, ReadOnlySpan<ulong> values, WaitMode mode)
            => SetEvent(default, fences, values, mode);

        /// <inheritdoc />
        public OSEvent GetEventForWait(ReadOnlySpan<FenceHandle> fences, ReadOnlySpan<ulong> values, WaitMode mode)
        {
            var @event = CreateEventW(null, FALSE, FALSE, null);

            SetEvent(@event, fences, values, mode);

            return OSEvent.FromWin32Handle(@event);
        }

        private void SetEvent(HANDLE hEvent, ReadOnlySpan<FenceHandle> fences, ReadOnlySpan<ulong> values, WaitMode mode)
        {
            if (fences.Length == 1)
            {
                var fence = fences[0];
                var value = values[0];

                ThrowIfFailed(_mapper.GetInfo(fence).Fence->SetEventOnCompletion(value, hEvent));

                return;
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

        /// <inheritdoc />
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

        /// <inheritdoc />
        public void DisposeFence(FenceHandle fence) => _mapper.GetAndFree(fence).Fence->Release();


        /// <inheritdoc />
        public void* Map(BufferHandle buffer) => _mapper.GetInfo(buffer).CpuAddress;
        /// <inheritdoc />
        public void Unmap(BufferHandle buffer) { }

        /// <inheritdoc />
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

            if (props.IsCPUAccessible)
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

        /// <inheritdoc />
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

        /// <inheritdoc />
        public RaytracingAccelerationStructureHandle AllocateRaytracingAccelerationStructure(ulong length)
        {
            UniqueComPtr<ID3D12Resource> res = default;

            var props = GetRaytracingAccelerationStructureHeapProperties();
            var nativeDesc = D3D12_RESOURCE_DESC.Buffer(length, D3D12_RESOURCE_FLAGS.D3D12_RESOURCE_FLAG_ALLOW_UNORDERED_ACCESS);

            ThrowIfFailed(_device.Ptr->CreateCommittedResource(
                &props,
                D3D12_HEAP_FLAGS.D3D12_HEAP_FLAG_NONE,
                &nativeDesc,
                D3D12_RESOURCE_STATES.D3D12_RESOURCE_STATE_RAYTRACING_ACCELERATION_STRUCTURE,
                null,
                res.Iid,
                (void**)&res
            ));


            var buffer = new D3D12RaytracingAccelerationStructure
            {
                RaytracingAccelerationStructure = res.Ptr,
                GpuAddress = res.Ptr->GetGPUVirtualAddress(),
                Length = length
            };

            return _mapper.Create(buffer);
        }

        /// <inheritdoc />
        public RaytracingAccelerationStructureHandle AllocateRaytracingAccelerationStructure(ulong length, HeapHandle heap, ulong offset)
        {
            UniqueComPtr<ID3D12Resource> res = default;

            var nativeHeap = _mapper.GetInfo(heap);
            var nativeDesc = D3D12_RESOURCE_DESC.Buffer(length, D3D12_RESOURCE_FLAGS.D3D12_RESOURCE_FLAG_ALLOW_UNORDERED_ACCESS);

            ThrowIfFailed(_device.Ptr->CreatePlacedResource(
                nativeHeap.Heap,
                offset,
                &nativeDesc,
                D3D12_RESOURCE_STATES.D3D12_RESOURCE_STATE_RAYTRACING_ACCELERATION_STRUCTURE,
                null,
                res.Iid,
                (void**)&res
            ));


            var buffer = new D3D12RaytracingAccelerationStructure
            {
                RaytracingAccelerationStructure = res.Ptr,
                GpuAddress = res.Ptr->GetGPUVirtualAddress(),
                Length = length
            };

            return _mapper.Create(buffer);
        }


        /// <inheritdoc />
        public TextureHandle AllocateTexture(in TextureDesc desc, ResourceState initial)
        {
            GetResourceDesc(desc, out var nativeDesc, out var clearValue);

            UniqueComPtr<ID3D12Resource> res = default;

            var props = GetRaytracingAccelerationStructureHeapProperties();

            static bool IsRtOrDs(ResourceFlags flags) => (flags & (ResourceFlags.AllowRenderTarget | ResourceFlags.AllowDepthStencil)) != 0;

            ThrowIfFailed(_device.Ptr->CreateCommittedResource(
                &props,
                D3D12_HEAP_FLAGS.D3D12_HEAP_FLAG_NONE,
                &nativeDesc,
                (D3D12_RESOURCE_STATES)initial,
                IsRtOrDs(desc.ResourceFlags) ? &clearValue  : null,
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

        /// <inheritdoc />
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

        /// <inheritdoc />
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

        /// <inheritdoc />
        public DynamicBufferDescriptorHandle CreateDynamicDescriptor(BufferHandle buffer)
        {
            var info = new D3D12DynamicBufferDescriptor
            {
                GpuAddress = _mapper.GetInfo(buffer).GpuAddress
            };

            return _mapper.Create(info);
        }

        /// <inheritdoc />
        public DynamicRaytracingAccelerationStructureDescriptorHandle CreateDynamicDescriptor(RaytracingAccelerationStructureHandle buffer)
        {
            var info = new D3D12DynamicRaytracingAccelerationStructureDescriptor
            {
                GpuAddress = _mapper.GetInfo(buffer).GpuAddress
            };

            return _mapper.Create(info);
        }

        /// <inheritdoc />
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

        /// <inheritdoc />
        public void DisposeDescriptorSet(DescriptorSetHandle handle)
        {
            var (offset, count, _) = Helpers.Unpack2x32To2x24_16(handle.Generational.Generation, handle.Generational.Id);

            var block = new FreeBlock { Offset = offset, Length = count };

            _freeList.Add(block);
        }

        /// <inheritdoc />
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

            _device.Ptr->CopyDescriptorsSimple(count, dest, src, D3D12_DESCRIPTOR_HEAP_TYPE.D3D12_DESCRIPTOR_HEAP_TYPE_CBV_SRV_UAV);
        }

        /// <inheritdoc />
        public void CopyDescriptors(DescriptorSetHandle source, uint firstSource, DescriptorSetHandle dest, uint firstDest, uint count) => throw new NotImplementedException();

        /// <inheritdoc />
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


        /// <inheritdoc />
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

        /// <inheritdoc />
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

        /// <inheritdoc />
        public ViewHandle CreateView(ViewSetHandle viewHeap, uint index, BufferHandle handle, in BufferViewDesc desc) => throw new NotImplementedException();

        /// <inheritdoc />
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

        /// <inheritdoc />
        public ViewHandle CreateView(ViewSetHandle viewHeap, uint index, TextureHandle handle, in TextureViewDesc desc) => throw new NotImplementedException();
        /// <inheritdoc />
        public ViewHandle CreateView(ViewSetHandle viewHeap, uint index, RaytracingAccelerationStructureHandle handle)
        {
            var views = _mapper.GetInfo(viewHeap);
            var accelerationStructure = _mapper.GetInfo(handle);

            var srv = GetSrv(views.FirstShaderResources, index, views.Length);

            D3D12_SHADER_RESOURCE_VIEW_DESC desc;
            _ = &desc;

            desc.Shader4ComponentMapping = D3D12_DEFAULT_SHADER_4_COMPONENT_MAPPING;
            desc.Format = DXGI_FORMAT.DXGI_FORMAT_UNKNOWN;
            desc.ViewDimension = D3D12_SRV_DIMENSION.D3D12_SRV_DIMENSION_RAYTRACING_ACCELERATION_STRUCTURE;
            desc.RaytracingAccelerationStructure.Location = accelerationStructure.GpuAddress;

            _device.Ptr->CreateShaderResourceView(null, &desc, srv);

            var view = new D3D12View
            {
                Resource = accelerationStructure.RaytracingAccelerationStructure,
                Format = DXGI_FORMAT.DXGI_FORMAT_UNKNOWN,
                ShaderResource = srv
            };

            return _mapper.Create(view);
        }

        private struct InitialSubObjects
        {
            public D3D12_NODE_MASK NodeMask;
            public D3D12_RAYTRACING_PIPELINE_CONFIG PipelineConfig;
            public D3D12_RAYTRACING_SHADER_CONFIG ShaderConfig;
            public D3D12_GLOBAL_ROOT_SIGNATURE GlobalRootSig;
        }

        /// <inheritdoc />
        public PipelineHandle CreatePipeline(in NativeRaytracingPipelineDesc pipelineDesc)
        {
            var buff = new ValueList<byte>(ArrayPool<byte>.Shared);

            var rootSigCount = pipelineDesc.LocalRootSignatures.Length;
            var libraryCount = pipelineDesc.Libraries.Length;
            var triangleHitGroupCount = pipelineDesc.TriangleHitGroups.Length;
            var proceduralHitGroupCount = pipelineDesc.ProceduralPrimitiveHitGroups.Length;

            var exportLength = 0;
            foreach (ref readonly var library in pipelineDesc.Libraries.Span)
            {
                exportLength += library.Exports.Length;
            }

            var associationCount = 0;
            var associationSubobjectCount = 0;
            foreach (ref readonly var localRootSig in pipelineDesc.LocalRootSignatures.Span)
            {
                if (!localRootSig.Associations.IsEmpty)
                {
                    associationCount += localRootSig.Associations.Length;
                    associationSubobjectCount++;
                }
            }

            var subObjectCount = 4 + rootSigCount + associationSubobjectCount + libraryCount + exportLength + triangleHitGroupCount + proceduralHitGroupCount;

            var subObjectSizes =
                sizeof(D3D12_NODE_MASK) +
                sizeof(D3D12_RAYTRACING_PIPELINE_CONFIG) +
                sizeof(D3D12_RAYTRACING_SHADER_CONFIG) +
                sizeof(D3D12_GLOBAL_ROOT_SIGNATURE) +

                (sizeof(D3D12_LOCAL_ROOT_SIGNATURE) * rootSigCount) +
                (sizeof(D3D12_SUBOBJECT_TO_EXPORTS_ASSOCIATION) * associationSubobjectCount) +
                (sizeof(char*) * associationCount) +
                (sizeof(D3D12_DXIL_LIBRARY_DESC) * libraryCount) +
                (sizeof(D3D12_EXPORT_DESC) * exportLength) +
                (sizeof(D3D12_HIT_GROUP_DESC) * triangleHitGroupCount) +
                (sizeof(D3D12_HIT_GROUP_DESC) * proceduralHitGroupCount);

            int
            MaxStringsPerAssociation = 1,
            MaxStringsPerExport = 2,
            MaxStringsPerTriangleHitGroup = 3,
            MaxStringsPerProceduralPrimitiveHitGroup = 4;

            var requiredHandles =
                   (exportLength * MaxStringsPerExport) +
                   (associationCount * MaxStringsPerAssociation) +
                   (triangleHitGroupCount * MaxStringsPerTriangleHitGroup) +
                   (proceduralHitGroupCount * MaxStringsPerProceduralPrimitiveHitGroup);

            using var objects = RentedArray<D3D12_STATE_SUBOBJECT>.Create(subObjectCount);
            using var buffer = RentedArray<byte>.Create(subObjectSizes);
            using var handles = RentedArray<MemoryHandle>.Create(requiredHandles);

            var objSpan = objects.AsSpan();
            var bufferSpan = buffer.AsSpan();
            var handleSpan = handles.AsSpan();

            var rootSigInfo = _mapper.GetInfo(pipelineDesc.RootSignature);

            var initial = new InitialSubObjects
            {
                NodeMask = new D3D12_NODE_MASK { NodeMask = pipelineDesc.NodeMask },
                GlobalRootSig = new D3D12_GLOBAL_ROOT_SIGNATURE { pGlobalRootSignature = rootSigInfo.RootSignature },
                PipelineConfig = new D3D12_RAYTRACING_PIPELINE_CONFIG { MaxTraceRecursionDepth = pipelineDesc.MaxRecursionDepth },
                ShaderConfig = new D3D12_RAYTRACING_SHADER_CONFIG { MaxAttributeSizeInBytes = pipelineDesc.MaxAttributeSize, MaxPayloadSizeInBytes = pipelineDesc.MaxPayloadSize }
            };

            SerializeInitialSubObjects(initial, ref objSpan, ref bufferSpan, ref handleSpan);
            SerializeLibraries(pipelineDesc.Libraries.Span, ref objSpan, ref bufferSpan, ref handleSpan);
            SerializeLocalRootSigs(pipelineDesc.LocalRootSignatures.Span, ref objSpan, ref bufferSpan, ref handleSpan);
            SerializeHitGroups(pipelineDesc.TriangleHitGroups.Span, pipelineDesc.ProceduralPrimitiveHitGroups.Span, ref objSpan, ref bufferSpan, ref handleSpan);

            fixed (D3D12_STATE_SUBOBJECT* pSubobjects = objects)
            {
                var desc = new D3D12_STATE_OBJECT_DESC
                {
                    pSubobjects = pSubobjects,
                    NumSubobjects = (uint)objects.Length,
                    Type = D3D12_STATE_OBJECT_TYPE.D3D12_STATE_OBJECT_TYPE_RAYTRACING_PIPELINE
                };

                UniqueComPtr<ID3D12StateObject> pso = default;
                ThrowIfFailed(_device.Ptr->CreateStateObject(&desc, pso.Iid, (void**)&pso));


                return _mapper.Create(new D3D12PipelineState
                {
                    BindPoint = BindPoint.Compute,
                    IsRaytracing = true,
                    PipelineState = (ID3D12Object*)pso.Ptr,
                    Properties = pso.QueryInterface<ID3D12StateObjectProperties>().Ptr,
                    RootParameters = rootSigInfo.RootParameters,
                    RootSignature = rootSigInfo.RootSignature,
                    Topology = 0
                });
            }



            void SerializeInitialSubObjects(in InitialSubObjects o, ref Span<D3D12_STATE_SUBOBJECT> objects, ref Span<byte> buff, ref Span<MemoryHandle> handles)
            {
                var pStart = WriteAndAdvance(ref buff, o);
                WriteAndAdvanceSubObjects(ref objects, new D3D12_STATE_SUBOBJECT { Type = D3D12_STATE_SUBOBJECT_TYPE.D3D12_STATE_SUBOBJECT_TYPE_NODE_MASK, pDesc = &pStart->NodeMask });
                WriteAndAdvanceSubObjects(ref objects, new D3D12_STATE_SUBOBJECT { Type = D3D12_STATE_SUBOBJECT_TYPE.D3D12_STATE_SUBOBJECT_TYPE_RAYTRACING_PIPELINE_CONFIG, pDesc = &pStart->PipelineConfig });
                WriteAndAdvanceSubObjects(ref objects, new D3D12_STATE_SUBOBJECT { Type = D3D12_STATE_SUBOBJECT_TYPE.D3D12_STATE_SUBOBJECT_TYPE_RAYTRACING_SHADER_CONFIG, pDesc = &pStart->ShaderConfig });
                WriteAndAdvanceSubObjects(ref objects, new D3D12_STATE_SUBOBJECT { Type = D3D12_STATE_SUBOBJECT_TYPE.D3D12_STATE_SUBOBJECT_TYPE_GLOBAL_ROOT_SIGNATURE, pDesc = &pStart->GlobalRootSig });
            }

            void SerializeLibraries(ReadOnlySpan<ShaderLibrary> libraries, ref Span<D3D12_STATE_SUBOBJECT> objects, ref Span<byte> buff, ref Span<MemoryHandle> handles)
            {
                foreach (ref readonly var library in libraries)
                {
                    var pExports = (D3D12_EXPORT_DESC*)Helpers.AddressOf(buff);

                    foreach (ref readonly var export in library.Exports.Span)
                    {
                        var exportDesc = new D3D12_EXPORT_DESC();

                        var name = AddHandle(ref handles, export.Name);
                        exportDesc.Name = (ushort*)name.Pointer;

                        if (export.ExportRename?.Length != 0)
                        {
                            var rename = AddHandle(ref handles, export.ExportRename);
                            exportDesc.Name = (ushort*)rename.Pointer;
                        }

                        WriteAndAdvance(ref buff, exportDesc);
                    }

                    var desc = new D3D12_DXIL_LIBRARY_DESC
                    {
                        DXILLibrary = new D3D12_SHADER_BYTECODE { pShaderBytecode = library.Library.Pointer, BytecodeLength = library.Library.Length },
                        NumExports = (uint)library.Exports.Length,
                        pExports = library.Exports.IsEmpty ? null : pExports
                    };


                    var subObject = new D3D12_STATE_SUBOBJECT
                    {
                        Type = D3D12_STATE_SUBOBJECT_TYPE.D3D12_STATE_SUBOBJECT_TYPE_DXIL_LIBRARY,
                        pDesc = Helpers.AddressOf(buff)
                    };

                    WriteAndAdvance(ref buff, desc);
                    WriteAndAdvanceSubObjects(ref objects, subObject);
                }
            }

            void SerializeLocalRootSigs(ReadOnlySpan<LocalRootSignatureAssociation> localRootSigs, ref Span<D3D12_STATE_SUBOBJECT> objects, ref Span<byte> buff, ref Span<MemoryHandle> handles)
            {
                foreach (ref readonly var sig in localRootSigs)
                {
                    var desc = new D3D12_LOCAL_ROOT_SIGNATURE
                    {
                        pLocalRootSignature = _mapper.GetInfo(sig.RootSignature).RootSignature
                    };

                    var subObject = new D3D12_STATE_SUBOBJECT
                    {
                        Type = D3D12_STATE_SUBOBJECT_TYPE.D3D12_STATE_SUBOBJECT_TYPE_LOCAL_ROOT_SIGNATURE,
                        pDesc = Helpers.AddressOf(buff)
                    };

                    WriteAndAdvance(ref buff, desc);
                    var pSubObject = WriteAndAdvanceSubObjects(ref objects, subObject);

                    if (sig.Associations.IsEmpty)
                    {
                        return;
                    }

                    char** pAssociations = (char**)Helpers.AddressOf(buff);
                    foreach (var associationName in sig.Associations.Span)
                    {
                        var handle = AddHandle(ref handles, associationName);

                        var p = (nuint)handle.Pointer;
                        WriteAndAdvance(ref buff, p);
                    }

                    var association = new D3D12_SUBOBJECT_TO_EXPORTS_ASSOCIATION
                    {
                        pSubobjectToAssociate = pSubObject,
                        NumExports = (uint)sig.Associations.Length,
                        pExports = (ushort**)pAssociations
                    };

                    var pAssociationDesc = WriteAndAdvance(ref buff, association);

                    var subObjectAssociation = new D3D12_STATE_SUBOBJECT
                    {
                        Type = D3D12_STATE_SUBOBJECT_TYPE.D3D12_STATE_SUBOBJECT_TYPE_SUBOBJECT_TO_EXPORTS_ASSOCIATION,
                        pDesc = pAssociationDesc
                    };

                    WriteAndAdvanceSubObjects(ref objects, subObjectAssociation);
                }
            }


            void SerializeHitGroups(ReadOnlySpan<TriangleHitGroup> triangles, ReadOnlySpan<ProceduralPrimitiveHitGroup> prims, ref Span<D3D12_STATE_SUBOBJECT> objects, ref Span<byte> buff, ref Span<MemoryHandle> handles)
            {
                foreach (ref readonly var triangle in triangles)
                {
                    var name = AddHandle(ref handles, triangle.Name);
                    var closestHit = AddHandle(ref handles, triangle.ClosestHitShader);
                    var anyHit = AddHandle(ref handles, triangle.AnyHitShader);

                    var group = new D3D12_HIT_GROUP_DESC
                    {
                        Type = D3D12_HIT_GROUP_TYPE.D3D12_HIT_GROUP_TYPE_TRIANGLES,
                        HitGroupExport = (ushort*)name.Pointer,
                        ClosestHitShaderImport = (ushort*)closestHit.Pointer,
                        AnyHitShaderImport = (ushort*)anyHit.Pointer,
                    };

                    AddSingleHitGroup(ref objects, ref buff, group);
                }

                foreach (ref readonly var primitive in prims)
                {
                    var name = AddHandle(ref handles, primitive.Name);
                    var closestHit = AddHandle(ref handles, primitive.ClosestHitShader);
                    var anyHit = AddHandle(ref handles, primitive.AnyHitShader);
                    var intersection = AddHandle(ref handles, primitive.IntersectionShader);

                    var group = new D3D12_HIT_GROUP_DESC
                    {
                        Type = D3D12_HIT_GROUP_TYPE.D3D12_HIT_GROUP_TYPE_PROCEDURAL_PRIMITIVE,
                        HitGroupExport = (ushort*)name.Pointer,
                        ClosestHitShaderImport = (ushort*)closestHit.Pointer,
                        AnyHitShaderImport = (ushort*)anyHit.Pointer,
                        IntersectionShaderImport = (ushort*)intersection.Pointer,
                    };

                    AddSingleHitGroup(ref objects, ref buff, group);
                }
            }

            void AddSingleHitGroup(ref Span<D3D12_STATE_SUBOBJECT> objects, ref Span<byte> buff, in D3D12_HIT_GROUP_DESC hitGroup)
            {
                var pBuff = WriteAndAdvance(ref buff, hitGroup);

                var obj = new D3D12_STATE_SUBOBJECT
                {
                    Type = D3D12_STATE_SUBOBJECT_TYPE.D3D12_STATE_SUBOBJECT_TYPE_HIT_GROUP,
                    pDesc = pBuff
                };

                WriteAndAdvanceSubObjects(ref objects, obj);
            }

            MemoryHandle AddHandle(ref Span<MemoryHandle> handles, string? memory)
            {
                var handle = memory.AsMemory().Pin();
                handles[0] = handle;
                handles = handles[1..];

                return handle;
            }

            T* WriteAndAdvance<T>(ref Span<byte> buff, in T val) where T : unmanaged
            {
                var addr = Helpers.AddressOf(buff);
                MemoryMarshal.Write(buff, ref Unsafe.AsRef(in val));
                buff = buff[sizeof(T)..];
                return (T*)addr;
            }

            D3D12_STATE_SUBOBJECT* WriteAndAdvanceSubObjects(ref Span<D3D12_STATE_SUBOBJECT> objects, in D3D12_STATE_SUBOBJECT obj)
            {
                var addr = Helpers.AddressOf(objects);
                objects[0] = obj;
                objects = objects[1..];
                return addr;
            }

        }

        /// <inheritdoc />
        public PipelineHandle CreatePipeline(in NativeComputePipelineDesc pipelineDesc)
        {
            var rootSig = _mapper.GetInfo(pipelineDesc.RootSignature);

            var desc = new D3D12_COMPUTE_PIPELINE_STATE_DESC
            {
                CS = new D3D12_SHADER_BYTECODE(pipelineDesc.ComputeShader.Pointer, pipelineDesc.ComputeShader.Length),
                pRootSignature = rootSig.RootSignature,
                NodeMask = pipelineDesc.NodeMask
            };


            UniqueComPtr<ID3D12PipelineState> pso = default;
            ThrowIfFailed(_device.Ptr->CreateComputePipelineState(&desc, pso.Iid, (void**)&pso));

            var info = new D3D12PipelineState
            {
                BindPoint = BindPoint.Compute,
                IsRaytracing = false,
                PipelineState = (ID3D12Object*)pso.Ptr,
                RootSignature = rootSig.RootSignature,
                RootParameters = rootSig.RootParameters
            };

            return _mapper.Create(info);
        }

        private struct PipelineStreamBuilder
        {
            private ValueList<byte> _buffer;
            public ReadOnlySpan<byte> PipelineStream => _buffer.AsSpan();

            public static PipelineStreamBuilder Create() => new() { _buffer = new(1) };

            public void AddShader(in CompiledShader shader)
                => AddShader(new() { pShaderBytecode = shader.Pointer, BytecodeLength = shader.Length }, shader.Type);

            public void AddShader(in D3D12_SHADER_BYTECODE bytecode, ShaderType type)
            {
                _buffer.AddRange(AsBytes(
                    type switch
                    {
                        ShaderType.Vertex => D3D12_PIPELINE_STATE_SUBOBJECT_TYPE_VS,
                        ShaderType.Pixel => D3D12_PIPELINE_STATE_SUBOBJECT_TYPE_PS,
                        ShaderType.Domain => D3D12_PIPELINE_STATE_SUBOBJECT_TYPE_DS,
                        ShaderType.Hull => D3D12_PIPELINE_STATE_SUBOBJECT_TYPE_HS,
                        ShaderType.Geometry => D3D12_PIPELINE_STATE_SUBOBJECT_TYPE_GS,
                        ShaderType.Compute => D3D12_PIPELINE_STATE_SUBOBJECT_TYPE_CS,
                        ShaderType.Mesh => D3D12_PIPELINE_STATE_SUBOBJECT_TYPE_MS,
                        ShaderType.Amplification => D3D12_PIPELINE_STATE_SUBOBJECT_TYPE_AS,
                        ShaderType.Unspecified or _ => throw new InvalidOperationException("Unspecified shader type is not valid in a pipeline desc"),
                    }
                ));


                _buffer.AddRange(sizeof(void*) - sizeof(D3D12_PIPELINE_STATE_SUBOBJECT_TYPE));

                _buffer.AddRange(AsBytes(bytecode));
            }

            public void Add<T>(in T val) where T : unmanaged
            {
                var (type, subobjectAlignment) = val switch
                {
                    D3D12_GLOBAL_ROOT_SIGNATURE => (D3D12_PIPELINE_STATE_SUBOBJECT_TYPE_ROOT_SIGNATURE, sizeof(void*)),
                    D3D12_BLEND_DESC => (D3D12_PIPELINE_STATE_SUBOBJECT_TYPE_BLEND, 4),
                    uint => (D3D12_PIPELINE_STATE_SUBOBJECT_TYPE_SAMPLE_MASK, 4),
                    D3D12_RASTERIZER_DESC => (D3D12_PIPELINE_STATE_SUBOBJECT_TYPE_RASTERIZER, 4),
                    D3D12_DEPTH_STENCIL_DESC => (D3D12_PIPELINE_STATE_SUBOBJECT_TYPE_DEPTH_STENCIL, 4),
                    D3D12_INPUT_LAYOUT_DESC => (D3D12_PIPELINE_STATE_SUBOBJECT_TYPE_INPUT_LAYOUT, sizeof(void*)),
                    D3D12_INDEX_BUFFER_STRIP_CUT_VALUE => (D3D12_PIPELINE_STATE_SUBOBJECT_TYPE_IB_STRIP_CUT_VALUE, 4),
                    D3D12_PRIMITIVE_TOPOLOGY_TYPE => (D3D12_PIPELINE_STATE_SUBOBJECT_TYPE_PRIMITIVE_TOPOLOGY, 4),
                    D3D12_RT_FORMAT_ARRAY => (D3D12_PIPELINE_STATE_SUBOBJECT_TYPE_RENDER_TARGET_FORMATS, 4),
                    DXGI_FORMAT => (D3D12_PIPELINE_STATE_SUBOBJECT_TYPE_DEPTH_STENCIL_FORMAT, 4),
                    DXGI_SAMPLE_DESC => (D3D12_PIPELINE_STATE_SUBOBJECT_TYPE_SAMPLE_DESC, 4),
                    D3D12_NODE_MASK => (D3D12_PIPELINE_STATE_SUBOBJECT_TYPE_NODE_MASK, 4),
                    D3D12_CACHED_PIPELINE_STATE => (D3D12_PIPELINE_STATE_SUBOBJECT_TYPE_CACHED_PSO, sizeof(void*)),
                    D3D12_DEPTH_STENCIL_DESC1 => (D3D12_PIPELINE_STATE_SUBOBJECT_TYPE_DEPTH_STENCIL1, 4),
                    D3D12_VIEW_INSTANCING_DESC => (D3D12_PIPELINE_STATE_SUBOBJECT_TYPE_VIEW_INSTANCING, sizeof(void*)),
                    _ => (ThrowHelper.ThrowInvalidOperationException<D3D12_PIPELINE_STATE_SUBOBJECT_TYPE>("unrecognised subobject"), 0)
                };

                _buffer.AddRange(AsBytes(type));

                // Align the subobject (some need void* alignment so need padding after enum on 64 bit platforms)
                _buffer.AddRange(subobjectAlignment - sizeof(D3D12_PIPELINE_STATE_SUBOBJECT_TYPE));
                _buffer.AddRange(AsBytes(val));

                var pad = (sizeof(D3D12_PIPELINE_STATE_SUBOBJECT_TYPE)
                    + (subobjectAlignment == sizeof(D3D12_PIPELINE_STATE_SUBOBJECT_TYPE) ? 0 : subobjectAlignment - sizeof(D3D12_PIPELINE_STATE_SUBOBJECT_TYPE)) + sizeof(T)) % sizeof(void*);
                _buffer.AddRange(pad);
            }


            private static ReadOnlySpan<byte> AsBytes<T>(in T val)
                    => MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<T, byte>(ref Unsafe.AsRef(in val)), Unsafe.SizeOf<T>());
        }


        /// <inheritdoc />
        public PipelineHandle CreatePipeline(in NativeGraphicsPipelineDesc desc)
        {
            var builder = PipelineStreamBuilder.Create();

            var rootSig = _mapper.GetInfo(desc.RootSignature);

            builder.Add(new D3D12_NODE_MASK { NodeMask = desc.NodeMask });

            builder.Add(new D3D12_GLOBAL_ROOT_SIGNATURE { pGlobalRootSignature = rootSig.RootSignature });

            builder.AddShader(desc.VertexShader);
            if (desc.HullShader.Length > 0)
            {
                builder.AddShader(desc.HullShader);
                builder.AddShader(desc.DomainShader);
            }
            if (desc.GeometryShader.Length > 0)
            {
                builder.AddShader(desc.GeometryShader);
            }
            if (desc.PixelShader.Length > 0)
            {
                builder.AddShader(desc.PixelShader);
            }

            ref readonly var depthStencil = ref desc.DepthStencil;

            builder.Add(new D3D12_DEPTH_STENCIL_DESC1
            {
                DepthEnable = Helpers.BoolToInt32(depthStencil.EnableDepthTesting),
                DepthWriteMask = (D3D12_DEPTH_WRITE_MASK)depthStencil.DepthWriteMask,
                DepthFunc = (D3D12_COMPARISON_FUNC)depthStencil.DepthComparison,
                StencilEnable = Helpers.BoolToInt32(depthStencil.EnableStencilTesting),
                StencilReadMask = depthStencil.StencilReadMask,
                StencilWriteMask = depthStencil.StencilWriteMask,
                FrontFace = new D3D12_DEPTH_STENCILOP_DESC
                {
                    StencilFailOp = (D3D12_STENCIL_OP)depthStencil.FrontFace.StencilTestFailOp,
                    StencilDepthFailOp = (D3D12_STENCIL_OP)depthStencil.FrontFace.StencilTestDepthTestFailOp,
                    StencilPassOp = (D3D12_STENCIL_OP)depthStencil.FrontFace.StencilPasslOp,
                    StencilFunc = (D3D12_COMPARISON_FUNC)depthStencil.FrontFace.ExistingDataOp,
                },
                BackFace = new D3D12_DEPTH_STENCILOP_DESC
                {
                    StencilFailOp = (D3D12_STENCIL_OP)depthStencil.BackFace.StencilTestFailOp,
                    StencilDepthFailOp = (D3D12_STENCIL_OP)depthStencil.BackFace.StencilTestDepthTestFailOp,
                    StencilPassOp = (D3D12_STENCIL_OP)depthStencil.BackFace.StencilPasslOp,
                    StencilFunc = (D3D12_COMPARISON_FUNC)depthStencil.BackFace.ExistingDataOp,
                },
                DepthBoundsTestEnable = Helpers.BoolToInt32(depthStencil.EnableDepthBoundsTesting)
            });

            ref readonly var rasterizer = ref desc.Rasterizer;

            builder.Add(new D3D12_RASTERIZER_DESC
            {
                FillMode = rasterizer.EnableWireframe ? D3D12_FILL_MODE.D3D12_FILL_MODE_WIREFRAME : D3D12_FILL_MODE.D3D12_FILL_MODE_SOLID,
                CullMode = (D3D12_CULL_MODE)rasterizer.FaceCullMode,
                FrontCounterClockwise = Helpers.BoolToInt32(rasterizer.FrontFaceType == FaceType.Anticlockwise),
                DepthBias = rasterizer.DepthBias,
                DepthBiasClamp = rasterizer.MaxDepthBias,
                SlopeScaledDepthBias = rasterizer.SlopeScaledDepthBias,
                DepthClipEnable = Helpers.BoolToInt32(rasterizer.EnableDepthClip),
                MultisampleEnable = Helpers.BoolToInt32(rasterizer.LineRenderAlgorithm == LineRenderAlgorithm.Quadrilateral),
                AntialiasedLineEnable = Helpers.BoolToInt32(rasterizer.LineRenderAlgorithm == LineRenderAlgorithm.AlphaAntiAliased),
                ForcedSampleCount = rasterizer.ForcedSampleCount,
                ConservativeRaster = rasterizer.EnableConservativerRasterization ? D3D12_CONSERVATIVE_RASTERIZATION_MODE.D3D12_CONSERVATIVE_RASTERIZATION_MODE_ON : D3D12_CONSERVATIVE_RASTERIZATION_MODE.D3D12_CONSERVATIVE_RASTERIZATION_MODE_OFF,
            });

            ref readonly var blend = ref desc.Blend;

            Unsafe.SkipInit(out D3D12_BLEND_DESC nativeBlendDesc);
            nativeBlendDesc.AlphaToCoverageEnable = Helpers.BoolToInt32(blend.UseAlphaToCoverage);
            nativeBlendDesc.IndependentBlendEnable = Helpers.BoolToInt32(blend.UseIndependentBlend);

            var blendDescCount = blend.UseIndependentBlend ? 1 : desc.RenderTargetFormats.Count;
            for (var i = 0; i < blendDescCount; i++)
            {
                ref readonly var renderTargetBlendDesc = ref blend.RenderTargets[i];
                nativeBlendDesc.RenderTarget[i] = new D3D12_RENDER_TARGET_BLEND_DESC
                {
                    BlendEnable = Helpers.BoolToInt32(renderTargetBlendDesc.EnableBlendOp),
                    LogicOpEnable = Helpers.BoolToInt32(renderTargetBlendDesc.EnableLogicOp),
                    SrcBlend = (D3D12_BLEND)renderTargetBlendDesc.SrcBlend,
                    DestBlend = (D3D12_BLEND)renderTargetBlendDesc.SrcBlend,
                    BlendOp = (D3D12_BLEND_OP)renderTargetBlendDesc.BlendOp,
                    SrcBlendAlpha = (D3D12_BLEND)renderTargetBlendDesc.SrcBlendAlpha,
                    DestBlendAlpha = (D3D12_BLEND)renderTargetBlendDesc.DestBlendAlpha,
                    BlendOpAlpha = (D3D12_BLEND_OP)renderTargetBlendDesc.AlphaBlendOp,
                    LogicOp = (D3D12_LOGIC_OP)renderTargetBlendDesc.SrcBlend,
                    RenderTargetWriteMask = (byte)renderTargetBlendDesc.RenderTargetWriteMask,
                };
            }

            builder.Add(nativeBlendDesc);

            ref readonly var msaa = ref desc.Msaa;

            builder.Add(new DXGI_SAMPLE_DESC
            {
                Count = msaa.SampleCount,
                Quality = msaa.QualityLevel
            });

            builder.Add((DXGI_FORMAT)desc.DepthStencilFormat);

            ref readonly var renderTargets = ref desc.RenderTargetFormats;
            builder.Add(new D3D12_RT_FORMAT_ARRAY
            {
                NumRenderTargets = renderTargets.Count,
                RTFormats = new()
                {
                    e0 = (DXGI_FORMAT)renderTargets[0],
                    e1 = (DXGI_FORMAT)renderTargets[1],
                    e2 = (DXGI_FORMAT)renderTargets[2],
                    e3 = (DXGI_FORMAT)renderTargets[3],
                    e4 = (DXGI_FORMAT)renderTargets[4],
                    e5 = (DXGI_FORMAT)renderTargets[5],
                    e6 = (DXGI_FORMAT)renderTargets[6],
                    e7 = (DXGI_FORMAT)renderTargets[7],
                }
            });

            builder.Add(new D3D12_NODE_MASK { NodeMask = desc.NodeMask });

            builder.Add(desc.Topology switch
            {
                Topology.PointList => D3D12_PRIMITIVE_TOPOLOGY_TYPE.D3D12_PRIMITIVE_TOPOLOGY_TYPE_UNDEFINED,
                Topology.LineList or Topology.LineStrip => D3D12_PRIMITIVE_TOPOLOGY_TYPE.D3D12_PRIMITIVE_TOPOLOGY_TYPE_LINE,
                Topology.TriangleList or Topology.TriangleStrip => D3D12_PRIMITIVE_TOPOLOGY_TYPE.D3D12_PRIMITIVE_TOPOLOGY_TYPE_TRIANGLE,
                Topology.Unknown or _ => D3D12_PRIMITIVE_TOPOLOGY_TYPE.D3D12_PRIMITIVE_TOPOLOGY_TYPE_UNDEFINED,
            });

            var pInputs = stackalloc D3D12_INPUT_ELEMENT_DESC[32]; // max
            var pNames = stackalloc sbyte*[32]; // max


            ref readonly var inputLayout = ref desc.Inputs;
            var span = inputLayout.ShaderInputs.Span;
            StackSentinel.StackAssert(span.Length <= 32);
            for (var i = 0; i < span.Length; i++)
            {
                ref readonly var input = ref span[i];

                var name = pNames[i] = StringHelpers.MarshalToUnmanagedAscii(input.Name);
                pInputs[i] = new D3D12_INPUT_ELEMENT_DESC
                {
                    SemanticName = name,
                    SemanticIndex = input.NameIndex,
                    Format = (DXGI_FORMAT)input.Type,
                    InputSlot = input.Channel,
                    AlignedByteOffset = input.Offset,
                    InputSlotClass = (D3D12_INPUT_CLASSIFICATION)input.InputClass,
                    InstanceDataStepRate = input.VerticesPerInstanceChange
                };
            }

            builder.Add(new D3D12_INPUT_LAYOUT_DESC
            {
                pInputElementDescs = pInputs,
                NumElements = (uint)span.Length
            });

            fixed (void* pDesc = builder.PipelineStream)
            {

                var nativeDesc = new D3D12_PIPELINE_STATE_STREAM_DESC
                {
                    pPipelineStateSubobjectStream = pDesc,
                    SizeInBytes = (uint)builder.PipelineStream.Length
                };

                UniqueComPtr<ID3D12PipelineState> pso = default;
                ThrowIfFailed(_device.Ptr->CreatePipelineState(&nativeDesc, pso.Iid, (void**)&pso));

                var info = new D3D12PipelineState
                {
                    BindPoint = BindPoint.Graphics,
                    IsRaytracing = false,
                    PipelineState = (ID3D12Object*)pso.Ptr,
                    RootSignature = rootSig.RootSignature,
                    Topology = (D3D_PRIMITIVE_TOPOLOGY)desc.Topology,
                    RootParameters = rootSig.RootParameters
                };

                return _mapper.Create(info);
            }
        }

        /// <inheritdoc />
        public PipelineHandle CreatePipeline(in NativeMeshPipelineDesc desc) => throw new NotImplementedException();

        /// <inheritdoc />
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

        /// <inheritdoc />
        public LocalRootSignatureHandle CreateLocalRootSignature(ReadOnlySpan<RootParameter> rootParameters, ReadOnlySpan<StaticSampler> staticSamplers, RootSignatureFlags flags)
            => _mapper.Create(new D3D12LocalRootSignature
            {
                RootSignature = InternalCreateRootSignature(rootParameters, staticSamplers, flags, local: true),
                RootParameters = ImmutableArray.Create(rootParameters.ToArray())
            });

        /// <inheritdoc />
        public RootSignatureHandle CreateRootSignature(ReadOnlySpan<RootParameter> rootParameters, ReadOnlySpan<StaticSampler> staticSamplers, RootSignatureFlags flags)
            => _mapper.Create(new D3D12RootSignature
            {
                RootSignature = InternalCreateRootSignature(rootParameters, staticSamplers, flags, local: false),
                RootParameters = ImmutableArray.Create(rootParameters.ToArray())
            });


        private ID3D12RootSignature* InternalCreateRootSignature(ReadOnlySpan<RootParameter> rootParameters, ReadOnlySpan<StaticSampler> staticSamplers, RootSignatureFlags flags, bool local)
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
                    Flags = ((D3D12_ROOT_SIGNATURE_FLAGS)flags) | (local ? D3D12_ROOT_SIGNATURE_FLAGS.D3D12_ROOT_SIGNATURE_FLAG_LOCAL_ROOT_SIGNATURE : 0)
                };

                var versionedDesc = new D3D12_VERSIONED_ROOT_SIGNATURE_DESC
                {
                    Version = D3D_ROOT_SIGNATURE_VERSION.D3D_ROOT_SIGNATURE_VERSION_1_1,
                    Desc_1_1 = desc
                };

                ID3DBlob* pBlob = default;
                ID3DBlob* pError = default;
                int hr = D3D12SerializeVersionedRootSignature(
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

                return pRootSig.Ptr;
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

        public void GetRaytracingShaderIdentifier(PipelineHandle raytracingPipeline, ReadOnlySpan<char> shaderName, Span<byte> identifier)
        {
            if (identifier.Length < Info.RaytracingShaderIdentifierSize)
            {
                ThrowHelper.ThrowArgumentException("Span too small for identifier");
            }

            fixed (char* pName = shaderName)
            {
                void* pIdentifier = _mapper.GetInfo(raytracingPipeline).Properties->GetShaderIdentifier((ushort*)pName);

                if (pIdentifier is null)
                {
                    ThrowHelper.ThrowArgumentException("Invalid shader name");
                }    

                new Span<byte>(pIdentifier, D3D12_SHADER_IDENTIFIER_SIZE_IN_BYTES).CopyTo(identifier);
            }
        }

        /// <inheritdoc />
        public void DisposeDynamicDescriptor(DynamicRaytracingAccelerationStructureDescriptorHandle handle) => _mapper.GetAndFree(handle);

        /// <inheritdoc />
        public void DisposeDynamicDescriptor(DynamicBufferDescriptorHandle handle) => _mapper.GetAndFree(handle);

        /// <inheritdoc />
        public void DisposeBuffer(BufferHandle handle) => _mapper.GetAndFree(handle).Buffer->Release();
        /// <inheritdoc />
        public void DisposeHeap(HeapHandle handle) => _mapper.GetAndFree(handle).Heap->Release();
        /// <inheritdoc />
        public void DisposePipeline(PipelineHandle handle) => _mapper.GetAndFree(handle).PipelineState->Release();
        /// <inheritdoc />
        public void DisposeQuerySet(QuerySetHandle handle) => _mapper.GetAndFree(handle).QueryHeap->Release();
        /// <inheritdoc />
        public void DisposeRaytracingAccelerationStructure(RaytracingAccelerationStructureHandle handle) => _mapper.GetAndFree(handle).RaytracingAccelerationStructure->Release();
        /// <inheritdoc />
        public void DisposeRootSignature(RootSignatureHandle handle) => _mapper.GetAndFree(handle).RootSignature->Release();
        /// <inheritdoc />
        public void DisposeLocalRootSignature(LocalRootSignatureHandle handle) => _mapper.GetAndFree(handle).RootSignature->Release();
        /// <inheritdoc />
        public void DisposeTexture(TextureHandle handle) => _mapper.GetAndFree(handle).Texture->Release();
        /// <inheritdoc />
        public void DisposeView(ViewHandle handle) => _mapper.GetAndFree(handle);

        /// <inheritdoc />
        public IndirectCommandHandle CreateIndirectCommand(RootSignatureHandle rootSig, ReadOnlySpan<IndirectArgument> arguments, uint byteStride) => throw new NotImplementedException();
        /// <inheritdoc />
        public IndirectCommandHandle CreateIndirectCommand(in IndirectArgument arguments, uint byteStride) => throw new NotImplementedException();
        /// <inheritdoc />
        public void DisposeIndirectCommand(IndirectCommandHandle handle) => throw new NotImplementedException();

        /// <inheritdoc />
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
        private D3D12_CPU_DESCRIPTOR_HANDLE GetUav(D3D12_CPU_DESCRIPTOR_HANDLE handle, uint index, uint count) => handle.Offset(_resDescriptorSize, count + index);
        private D3D12_CPU_DESCRIPTOR_HANDLE GetCbv(D3D12_CPU_DESCRIPTOR_HANDLE handle, uint index, uint count) => handle.Offset(_resDescriptorSize, (count * 2) + index);



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

        /// <inheritdoc/>
        public (ulong DestSize, ulong ScratchSize, ulong UpdateSize) GetBottomLevelAccelerationStructureBuildInfo(ReadOnlySpan<GeometryDesc> geometry, BuildAccelerationStructureFlags flags)
        {
            fixed (void* pGeometryDescs = geometry)
            {
                var inputs = new D3D12_BUILD_RAYTRACING_ACCELERATION_STRUCTURE_INPUTS
                {
                    Type = D3D12_RAYTRACING_ACCELERATION_STRUCTURE_TYPE.D3D12_RAYTRACING_ACCELERATION_STRUCTURE_TYPE_BOTTOM_LEVEL,
                    Flags = (D3D12_RAYTRACING_ACCELERATION_STRUCTURE_BUILD_FLAGS)flags,
                    DescsLayout = D3D12_ELEMENTS_LAYOUT.D3D12_ELEMENTS_LAYOUT_ARRAY,
                    NumDescs = (uint)geometry.Length,
                    pGeometryDescs = (D3D12_RAYTRACING_GEOMETRY_DESC*)pGeometryDescs
                };

                D3D12_RAYTRACING_ACCELERATION_STRUCTURE_PREBUILD_INFO info;

                _device.Ptr->GetRaytracingAccelerationStructurePrebuildInfo(&inputs, &info);

                return (info.ResultDataMaxSizeInBytes, info.ScratchDataSizeInBytes, info.UpdateScratchDataSizeInBytes);
            }
        }

        /// <inheritdoc/>
        public (ulong DestSize, ulong ScratchSize, ulong UpdateSize) GetTopLevelAccelerationStructureBuildInfo(uint numInstances, BuildAccelerationStructureFlags flags)
        {
            var inputs = new D3D12_BUILD_RAYTRACING_ACCELERATION_STRUCTURE_INPUTS
            {
                Type = D3D12_RAYTRACING_ACCELERATION_STRUCTURE_TYPE.D3D12_RAYTRACING_ACCELERATION_STRUCTURE_TYPE_TOP_LEVEL,
                Flags = (D3D12_RAYTRACING_ACCELERATION_STRUCTURE_BUILD_FLAGS)flags,
                DescsLayout = D3D12_ELEMENTS_LAYOUT.D3D12_ELEMENTS_LAYOUT_ARRAY,
                NumDescs = numInstances,
                InstanceDescs = /* arbitrary non-null value */ 1
            };

            D3D12_RAYTRACING_ACCELERATION_STRUCTURE_PREBUILD_INFO info;

            _device.Ptr->GetRaytracingAccelerationStructurePrebuildInfo(&inputs, &info);

            return (info.ResultDataMaxSizeInBytes, info.ScratchDataSizeInBytes, info.UpdateScratchDataSizeInBytes);
        }

        public ulong GetDeviceVirtualAddress(BufferHandle handle) => _mapper.GetInfo(handle).GpuAddress;
        public ulong GetDeviceVirtualAddress(RaytracingAccelerationStructureHandle handle) => _mapper.GetInfo(handle).GpuAddress;
    }
}
