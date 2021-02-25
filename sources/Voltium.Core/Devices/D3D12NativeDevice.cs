using System;
using System.Buffers;
using System.Collections.Generic;
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

namespace Voltium.Core.Devices
{
    public unsafe struct D3D12NativeDevice : INativeDevice
    {
        private const uint ShaderVisibleDescriptorCount = 1024 * 1024;

        private UniqueComPtr<ID3D12Device3> _device;
        private GenerationalHandleMapper _mapper;
        private ValueList<FreeBlock> _freeList;
        private int _resDescriptorSize, _rtvDescriptorSize, _dsvDescriptorSize;
        private UniqueComPtr<ID3D12DescriptorHeap> _shaderDescriptors;
        private D3D12_CPU_DESCRIPTOR_HANDLE _firstShaderDescriptor;
        private uint _numShaderDescriptors;

        private struct FreeBlock
        {
            public uint Offset, Length;
        }

        public D3D12NativeDevice(D3D_FEATURE_LEVEL fl) : this()
        {
            UniqueComPtr<ID3D12Device> device = default;

            ThrowIfFailed(Windows.D3D12CreateDevice(
                null,
                fl,
                device.Iid,
                (void**)&device
            ));

            _device = device;
            _mapper = new();

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
            _numShaderDescriptors = desc.NumDescriptors;

            _freeList = new(1, ArrayPool<FreeBlock>.Shared);
        }

        public DeviceInfo Info { get; private set; }

        public (ulong Alignment, ulong Length) GetTextureAllocationInfo(in TextureDesc desc)
        {
            GetResourceDesc(desc, out var native, out _);

            var info = _device.Ptr->GetResourceAllocationInfo(0, 1, &native);

            return (info.Alignment, info.SizeInBytes);
        }

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


            var buffer = new D3D12Buffer
            {
                Buffer = res.Ptr,
                Address = res.Ptr->GetGPUVirtualAddress(),
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


            var buffer = new D3D12Buffer
            {
                Buffer = res.Ptr,
                Flags = nativeDesc.Flags,
                Address = res.Ptr->GetGPUVirtualAddress(),
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
                Texture = res.Ptr
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
                Texture = res.Ptr
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
                    return new DescriptorSetHandle(new CommandBuffer.GenerationalHandle(gen, id));
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

            var dest = GetShaderDescriptor(offset + firstDescriptor);

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
                BufferLocation = buffer.Address,
                SizeInBytes = (uint)buffer.Length
            };

            _device.Ptr->CreateConstantBufferView(&desc, cbv);

            var view = new D3D12View
            {
                Resource = buffer.Buffer,
                Format = DXGI_FORMAT.DXGI_FORMAT_UNKNOWN,
                ShaderResource = srv,
                ConstantBuffer = cbv,
                UnorderedAccess = uav
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
            if (!texture.Flags.HasFlag(D3D12_RESOURCE_FLAGS.D3D12_RESOURCE_FLAG_DENY_SHADER_RESOURCE))
            {
                _device.Ptr->CreateShaderResourceView(texture.Texture, null, srv);
            }

            var view = new D3D12View
            {
                Resource = texture.Texture,
                Format = DXGI_FORMAT.DXGI_FORMAT_UNKNOWN,
                ShaderResource = srv,
                UnorderedAccess = uav,
                DepthStencil = dsv,
                RenderTarget = rtv
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
                    BindPoint = CommandBuffer.BindPoint.Graphics,
                    PipelineState = (ID3D12Object*)pso.Ptr,
                    RootSignature = desc.Desc.RootSig.Pointer,
                    RootParameters = _mapper.GetInfo(desc.Sig).RootParameters
                };

                return _mapper.Create(pipelineState);
            }
        }

        public PipelineHandle CreatePipeline(in RaytracingPipelineDesc desc) => throw new NotImplementedException();
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

        public RootSignatureHandle CreateRootSignature(ReadOnlySpan<RootParameter> rootParams, ReadOnlySpan<StaticSampler> samplers, RootSignatureFlags flags) => throw new NotImplementedException();

        public void DisposeBuffer(BufferHandle handle) => _mapper.GetAndFree(handle).Buffer->Release();
        public void DisposeHeap(HeapHandle handle) => _mapper.GetAndFree(handle).Heap->Release();
        public void DisposePipeline(PipelineHandle handle) => _mapper.GetAndFree(handle).PipelineState->Release();
        public void CreateQuerySet(QuerySetHandle handle) => _mapper.GetAndFree(handle).QueryHeap->Release();
        public void DisposeRaytracingAccelerationStructure(RaytracingAccelerationStructureHandle handle) => _mapper.GetAndFree(handle).RaytracingAccelerationStructure->Release();
        public void DisposeRootSignature(RootSignatureHandle handle) => _mapper.GetAndFree(handle).RootSignature->Release();
        public void DisposeTexture(TextureHandle handle) => _mapper.GetAndFree(handle).Texture->Release();
        public void DisposeView(ViewHandle handle) => _mapper.GetAndFree(handle);

        public GpuTask Execute(ReadOnlySpan<ReadOnlyMemory<byte>> cmds) => throw new NotImplementedException();

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



        private D3D12_CPU_DESCRIPTOR_HANDLE GetShaderDescriptor(uint index)
        {
            var handle = _firstShaderDescriptor;
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
        private void ThrowIfFailed(int hr) => throw new NotImplementedException();
    }
}
