using System;
using System.Diagnostics;
using TerraFX.Interop;
using Voltium.Common;
using Voltium.Common.Debugging;
using Voltium.Core.GpuResources;
using Voltium.Core.Managers;
using Voltium.Core.Memory.GpuResources;
using Voltium.Core.Memory.GpuResources.ResourceViews;
using Voltium.Core.Pipeline;
using Voltium.TextureLoading;
using static TerraFX.Interop.D3D_PRIMITIVE_TOPOLOGY;
using Buffer = Voltium.Core.Memory.GpuResources.Buffer;

namespace Voltium.Core
{
    /// <summary>
    /// Represents a context on which GPU commands can be recorded
    /// </summary>
    public unsafe partial struct GraphicsContext : IDisposable
    {
        private ComPtr<ID3D12GraphicsCommandList> _list;
        private ComPtr<ID3D12CommandAllocator> _allocator;


        internal ComPtr<ID3D12GraphicsCommandList> GetAndReleaseList() => _list.Move();
        internal ComPtr<ID3D12CommandAllocator> GetAndReleaseAllocator() => _allocator.Move();

        internal GraphicsContext Move()
        {
            var copy = this;
            copy._list = _list.Move();
            copy._allocator = _allocator.Move();
            return copy;
        }

        internal GraphicsContext Copy()
        {
            var copy = this;
            copy._list = _list.Copy();
            copy._allocator = _allocator.Copy();
            return copy;
        }

        internal GraphicsContext(ComPtr<ID3D12GraphicsCommandList> list, ComPtr<ID3D12CommandAllocator> allocator)
        {
            Debug.Assert(list.Exists);
            Debug.Assert(allocator.Exists);

            _list = list.Move();
            _allocator = allocator.Move();
        }

        private static bool AreCopyable(GpuResource source, GpuResource destination)
        {
            D3D12_RESOURCE_DESC srcDesc = source.UnderlyingResource->GetDesc();
            D3D12_RESOURCE_DESC destDesc = destination.UnderlyingResource->GetDesc();

            return srcDesc.Width == destDesc.Width
                   && srcDesc.Height == destDesc.Height
                   && srcDesc.DepthOrArraySize == destDesc.DepthOrArraySize
                   && srcDesc.Dimension == destDesc.Dimension;
        }

        /// <summary>
        /// Sets the viewport and scissor rectangle to encompass the entire screen
        /// </summary>
        /// <param name="fullscreen">The <see cref="ScreenData"/> representing the screen</param>
        public void SetViewportAndScissor(in ScreenData fullscreen)
        {
            SetViewports(new Viewport(0, 0, fullscreen.Width, fullscreen.Height, 0, 1));
            SetScissorRectangles(new Rectangle(0, 0, (int)fullscreen.Width, (int)fullscreen.Height));
        }

        /// <summary>
        /// Sets the blend factor for the pipeline <see cref="BlendDesc"/>
        /// </summary>
        /// <param name="value">The value of the blend factor</param>
        public void SetBlendFactor(RgbaColor value)
        {
            _list.Get()->OMSetBlendFactor(&value.R);
        }

        /// <summary>
        /// Sets the stencil ref for the pipeline <see cref="BlendDesc"/>
        /// </summary>
        /// <param name="value">The value of the stencil ref</param>
        public void SetStencilRef(uint value)
        {
            _list.Get()->OMSetStencilRef(value);
        }

        /// <summary>
        /// Copy an entire resource
        /// </summary>
        /// <param name="source">The resource to copy from</param>
        /// <param name="destination">The resource to copy to</param>
        public void CopyResource(Buffer source, Buffer destination)
        {
            _list.Get()->CopyResource(destination.Resource.UnderlyingResource, source.Resource.UnderlyingResource);
        }

        /// <summary>
        /// Copy an entire resource
        /// </summary>
        /// <param name="source">The resource to copy from</param>
        /// <param name="destination">The resource to copy to</param>
        public void CopyResource(Texture source, Texture destination)
        {
            _list.Get()->CopyResource(source.Resource.UnderlyingResource, destination.Resource.UnderlyingResource);
        }

        /// <summary>
        /// Uploads a buffer from the CPU to the GPU
        /// </summary>
        /// <param name="allocator"></param>
        /// <param name="buffer"></param>
        /// <param name="destination"></param>
        public void UploadResource(GpuAllocator allocator, ReadOnlySpan<byte> buffer, Buffer destination)
        {
            var upload = allocator.AllocateBuffer(buffer.Length, MemoryAccess.CpuUpload, ResourceState.GenericRead);
            upload.WriteData(buffer);

            CopyResource(upload, destination);
        }

        /// <summary>
        /// Uploads a buffer from the CPU to the GPU
        /// </summary>
        /// <param name="allocator"></param>
        /// <param name="texture"></param>
        /// <param name="subresources"></param>
        /// <param name="destination"></param>
        public void UploadResource(GpuAllocator allocator, ReadOnlySpan<byte> texture, ReadOnlySpan<SubresourceData> subresources, Texture destination)
        {
            var upload = allocator.AllocateBuffer(
                (long)Windows.GetRequiredIntermediateSize(destination.Resource.UnderlyingResource, 0, (uint)subresources.Length),
                MemoryAccess.CpuUpload,
                ResourceState.GenericRead
            );

            fixed (byte* pTextureData = texture)
            fixed (SubresourceData* pSubresources = subresources)
            {
                // D3D12_SUBRESOURCE_DATA and SubresourceData are blittable, just SubresourceData contains an offset past the pointer
                // Fix that here
                for (var i = 0; i < subresources.Length; i++)
                {
                    ((D3D12_SUBRESOURCE_DATA*)&pSubresources[i])->pData = pTextureData + pSubresources[i].DataOffset;
                }

                _ = Windows.UpdateSubresources(
                    _list.Get(),
                    destination.Resource.UnderlyingResource,
                    upload.Resource.UnderlyingResource,
                    0,
                    0,
                    (uint)subresources.Length,
                    (D3D12_SUBRESOURCE_DATA*)pSubresources
                );
            }
        }

        /// <summary>
        /// Sets a directly-bound constant buffer view descriptor to the graphics pipeline
        /// </summary>
        /// <param name="paramIndex">The index in the <see cref="RootSignature"/> which this view represents</param>
        /// <param name="cbuffer">The <see cref="Buffer"/> containing the buffer to add</param>
        /// <param name="offset"></param>
        public void SetGraphicsConstantBufferDescriptor(uint paramIndex, Buffer cbuffer, uint offset = 0)
        {
            _list.Get()->SetGraphicsRootConstantBufferView(paramIndex, cbuffer.GpuAddress + offset);
        }

        /// <summary>
        /// Set the graphics root signature for the command list
        /// </summary>
        /// <param name="signature">The signature to set to</param>
        public void SetGraphicsRootSignature(RootSignature signature)
        {
            _list.Get()->SetGraphicsRootSignature(signature.Value);
        }

        /// <summary>
        /// Mark a resource barrier on the command list
        /// </summary>
        /// <param name="resource">The resource to transition</param>
        /// <param name="transition">The transition</param>
        public void ResourceTransition<T>(Buffer resource, ResourceTransition transition) where T : unmanaged
            => ResourceTransition(resource.Resource, transition);

        /// <summary>
        /// Mark a resource barrier on the command list
        /// </summary>
        /// <param name="resource">The resource to transition</param>
        /// <param name="transition">The transition</param>
        public void ResourceTransition(Texture resource, ResourceTransition transition)
            => ResourceTransition(resource.Resource, transition);

        private void ResourceTransition(GpuResource resource, ResourceTransition transition)
        {
            // don't do unnecessary work
            if (resource.State == transition.NewState)
            {
                return;
            }

            var barrier = new D3D12_RESOURCE_BARRIER
            {
                Type = D3D12_RESOURCE_BARRIER_TYPE.D3D12_RESOURCE_BARRIER_TYPE_TRANSITION,
                Flags = D3D12_RESOURCE_BARRIER_FLAGS.D3D12_RESOURCE_BARRIER_FLAG_NONE,
                Anonymous = new D3D12_RESOURCE_BARRIER._Anonymous_e__Union
                {
                    Transition = CreateD3D12Transition(resource, transition)
                }
            };

            resource.State = transition.NewState;
            _list.Get()->ResourceBarrier(1, &barrier);
        }

        private D3D12_RESOURCE_TRANSITION_BARRIER CreateD3D12Transition(GpuResource resource, ResourceTransition transition)
        {
            return new D3D12_RESOURCE_TRANSITION_BARRIER
            {
                pResource = resource.UnderlyingResource,
                StateBefore = (D3D12_RESOURCE_STATES)resource.State,
                StateAfter = (D3D12_RESOURCE_STATES)transition.NewState,
                Subresource = transition.Subresource
            };
        }

        /// <summary>
        /// Sets a range of non-continuous render targets
        /// </summary>
        /// <param name="renderTargets">A span of <see cref="CpuHandle"/>s representing each render target</param>
        /// <param name="depthStencilHandle">The handle to the depth stencil descriptor</param>
        public void SetRenderTarget(Span<CpuHandle> renderTargets, CpuHandle? depthStencilHandle = null)
        {
            fixed (CpuHandle* p = renderTargets)
            {
                var depthStencil = depthStencilHandle.GetValueOrDefault();
                _list.Get()->OMSetRenderTargets(
                    (uint)renderTargets.Length,
                    &p->Value,
                    Windows.FALSE,
                    depthStencilHandle is null ? null : &depthStencil.Value
                );
            }
        }

        /// <summary>
        /// Sets a range of continuous render targets
        /// </summary>
        /// <param name="renderTargetHandle">The handle to the start of the continuous array of render targets</param>
        /// <param name="renderTargetCount">The number of render targets pointed to be <paramref name="renderTargetHandle"/></param>
        /// <param name="depthStencilHandle">The handle to the depth stencil descriptor</param>
        public void SetRenderTarget(CpuHandle renderTargetHandle, uint renderTargetCount, CpuHandle? depthStencilHandle = null)
        {
            var depthStencil = depthStencilHandle.GetValueOrDefault();
            _list.Get()->OMSetRenderTargets(
                renderTargetCount,
                &renderTargetHandle.Value,
                Windows.TRUE,
                depthStencilHandle is null ? null : &depthStencil.Value
            );
        }

        /// <summary>
        /// Sets the primitive toplogy for geometry
        /// </summary>
        /// <param name="topology">The <see cref="D3D_PRIMITIVE_TOPOLOGY"/> to use</param>
        public void SetTopology(Topology topology)
        {
            _list.Get()->IASetPrimitiveTopology((D3D_PRIMITIVE_TOPOLOGY)topology);
        }

        /// <summary>
        /// Sets the number of points in a control point patch
        /// </summary>
        /// <param name="count">The number of points per patch></param>
        public void SetControlPatchPointCount(byte count)
        {
            Guard.InRangeInclusive(1, 32, count);
            _list.Get()->IASetPrimitiveTopology(D3D_PRIMITIVE_TOPOLOGY_1_CONTROL_POINT_PATCHLIST + (count - 1));
        }

        /// <summary>
        /// Clear the render target
        /// </summary>
        /// <param name="rtv">The render target to clear</param>
        /// <param name="color">The RGBA color to clear it to</param>
        /// <param name="rect">The rectangle representing the section to clear</param>
        public void ClearRenderTarget(DescriptorHandle rtv, RgbaColor color, Rectangle rect)
        {
            _list.Get()->ClearRenderTargetView(rtv.CpuHandle.Value, &color.R, 1, (RECT*)&rect);
        }

        /// <summary>
        /// Clear the render target
        /// </summary>
        /// <param name="rtv">The render target to clear</param>
        /// <param name="color">The RGBA color to clear it to</param>
        /// <param name="rect">The rectangles representing the sections to clear. By default, this will clear the entire resource</param>
        public void ClearRenderTarget(DescriptorHandle rtv, RgbaColor color, ReadOnlySpan<Rectangle> rect = default)
        {
            fixed (Rectangle* p = rect)
            {
                _list.Get()->ClearRenderTargetView(rtv.CpuHandle.Value, &color.R, (uint)rect.Length, (RECT*)p);
            }
        }

        /// <summary>
        /// Clear the depth element of the depth stencil
        /// </summary>
        /// <param name="dsv">The depth stencil target to clear</param>
        /// <param name="depth">The <see cref="float"/> value to set the depth resource to. By default, this is <c>1</c></param>
        /// <param name="rect">The rectangles representing the sections to clear. By default, this will clear the entire resource</param>
        public void ClearDepth(DescriptorHandle dsv, float depth = 1, ReadOnlySpan<Rectangle> rect = default)
        {
            fixed (Rectangle* p = rect)
            {
                _list.Get()->ClearDepthStencilView(dsv.CpuHandle.Value, D3D12_CLEAR_FLAGS.D3D12_CLEAR_FLAG_DEPTH, depth, 0, (uint)rect.Length, (RECT*)p);
            }
        }

        /// <summary>
        /// Clear the render target
        /// </summary>
        /// <param name="dsv">The depth stencil target to clear</param>
        /// <param name="stencil">The <see cref="byte"/> value to set the stencil resource to. By default, this is <c>0</c></param>
        /// <param name="rect">The rectangles representing the sections to clear. By default, this will clear the entire resource</param>
        public void ClearStencil(DescriptorHandle dsv, byte stencil = 0, ReadOnlySpan<Rectangle> rect = default)
        {
            fixed (Rectangle* p = rect)
            {
                _list.Get()->ClearDepthStencilView(dsv.CpuHandle.Value, D3D12_CLEAR_FLAGS.D3D12_CLEAR_FLAG_STENCIL, 0, stencil, (uint)rect.Length, (RECT*)p);
            }
        }

        /// <summary>
        /// Clear the render target
        /// </summary>
        /// <param name="dsv">The depth stencil target to clear</param>
        /// <param name="depth">The <see cref="float"/> value to set the depth resource to. By default, this is <c>1</c></param>
        /// <param name="stencil">The <see cref="byte"/> value to set the stencil resource to. By default, this is <c>0</c></param>
        /// <param name="rect">The rectangles representing the sections to clear. By default, this will clear the entire resource</param>
        public void ClearDepthStencil(DescriptorHandle dsv, float depth = 1, byte stencil = 0, ReadOnlySpan<Rectangle> rect = default)
        {
            fixed (Rectangle* p = rect)
            {
                _list.Get()->ClearDepthStencilView(dsv.CpuHandle.Value, D3D12_CLEAR_FLAGS.D3D12_CLEAR_FLAG_STENCIL | D3D12_CLEAR_FLAGS.D3D12_CLEAR_FLAG_DEPTH, depth, stencil, (uint)rect.Length, (RECT*)p);
            }
        }

        /// <summary>
        /// Set the viewports
        /// </summary>
        /// <param name="viewport">The viewport to set</param>
        public void SetViewports(Viewport viewport)
        {
            _list.Get()->RSSetViewports(1, (D3D12_VIEWPORT*)&viewport);
        }

        /// <summary>
        /// Set the scissor rectangles
        /// </summary>
        /// <param name="rectangles">The rectangles to set</param>
        public void SetScissorRectangles(ReadOnlySpan<Rectangle> rectangles)
        {
            fixed (Rectangle* pRects = rectangles)
            {
                _list.Get()->RSSetScissorRects((uint)rectangles.Length, (RECT*)pRects);
            }
        }

        /// <summary>
        /// Set the scissor rectangles
        /// </summary>
        /// <param name="rectangle">The rectangle to set</param>
        public void SetScissorRectangles(Rectangle rectangle)
        {
            _list.Get()->RSSetScissorRects(1, (RECT*)&rectangle);
        }

        /// <summary>
        /// Set the viewports
        /// </summary>
        /// <param name="viewports">The viewports to set</param>
        public void SetViewports(ReadOnlySpan<Viewport> viewports)
        {
            fixed (Viewport* pViewports = viewports)
            {
                _list.Get()->RSSetViewports((uint)viewports.Length, (D3D12_VIEWPORT*)pViewports);
            }
        }

        /// <summary>
        /// Set the vertex buffers
        /// </summary>
        /// <param name="vertexResource">The vertex buffer to set</param>
        /// <param name="startSlot">The slot on the device array to start setting vertex buffers to</param>
        /// <typeparam name="T">The type of the vertex in <see cref="Buffer"/></typeparam>
        public void SetVertexBuffers<T>(Buffer vertexResource, uint startSlot = 0)
            where T : unmanaged
        {
            var desc = CreateVertexBufferView<T>(vertexResource);
            _list.Get()->IASetVertexBuffers(startSlot, 1, &desc);
        }

        /// <summary>
        /// Set the vertex buffers
        /// </summary>
        /// <param name="vertexBuffers">The vertex buffers to set</param>
        /// <param name="startSlot">The slot on the device array to start setting vertex buffers to</param>
        /// <typeparam name="T">The type of the vertex in <see cref="Buffer"/></typeparam>
        public void SetVertexBuffers<T>(ReadOnlySpan<Buffer> vertexBuffers, uint startSlot = 0)
            where T : unmanaged
        {
            Debug.Assert(StackSentinel.SafeToStackalloc<D3D12_VERTEX_BUFFER_VIEW>(vertexBuffers.Length));

            D3D12_VERTEX_BUFFER_VIEW* views = stackalloc D3D12_VERTEX_BUFFER_VIEW[vertexBuffers.Length];
            for (int i = 0; i < vertexBuffers.Length; i++)
            {
                views[i] = CreateVertexBufferView<T>(vertexBuffers[i]);
            }

            _list.Get()->IASetVertexBuffers(startSlot, (uint)vertexBuffers.Length, views);
        }

        private static D3D12_VERTEX_BUFFER_VIEW CreateVertexBufferView<T>(Buffer buffer)
            where T : unmanaged
        {
            return new D3D12_VERTEX_BUFFER_VIEW
            {
                BufferLocation = buffer.GpuAddress,
                SizeInBytes = buffer.Length,
                StrideInBytes = (uint)sizeof(T)
            };
        }

        /// <summary>
        /// Set the index buffer
        /// </summary>
        /// <param name="indexResource">The index buffer to set</param>
        /// <typeparam name="T">The type of the index in <see cref="Buffer"/></typeparam>
        public void SetIndexBuffer<T>(Buffer indexResource)
            where T : unmanaged
        {
            var desc = CreateIndexBufferView(indexResource);
            _list.Get()->IASetIndexBuffer(&desc);

            static D3D12_INDEX_BUFFER_VIEW CreateIndexBufferView(Buffer buffer)
            {
                return new D3D12_INDEX_BUFFER_VIEW
                {
                    BufferLocation = buffer.GpuAddress,
                    SizeInBytes = buffer.Length,
                    Format = GetDxgiIndexType()
                };

                static DXGI_FORMAT GetDxgiIndexType()
                {
                    if (typeof(T) == typeof(int))
                    {
                        return DXGI_FORMAT.DXGI_FORMAT_R32_SINT;
                    }
                    else if (typeof(T) == typeof(uint))
                    {
                        return DXGI_FORMAT.DXGI_FORMAT_R32_UINT;
                    }
                    else if (typeof(T) == typeof(short))
                    {
                        return DXGI_FORMAT.DXGI_FORMAT_R16_SINT;
                    }
                    else if (typeof(T) == typeof(ushort))
                    {
                        return DXGI_FORMAT.DXGI_FORMAT_R16_UINT;
                    }

                    ThrowHelper.ThrowNotSupportedException("Unsupported index type, must be UInt32/Int32/UInt16/Int16");
                    return default;
                }
            }
        }

        /// <summary>
        /// Submits a draw call
        /// </summary>
        public void Draw(uint vertexCountPerInstance, uint startVertexLocation = 0)
            => DrawInstanced(vertexCountPerInstance, 1, startVertexLocation, 0);

        /// <summary>
        /// Submits an indexed draw call
        /// </summary>
        public void DrawIndexed(uint indexCountPerInstance, uint startIndexLocation = 0, int baseVertexLocation = 0)
            => DrawIndexedInstanced(indexCountPerInstance, 1, startIndexLocation, baseVertexLocation, 0);

        /// <summary>
        /// Submits an instanced draw call
        /// </summary>
        public void DrawInstanced(uint vertexCountPerInstance, uint instanceCount, uint startVertexLocation, uint startInstanceLocation)
        {
            _list.Get()->DrawInstanced(
                vertexCountPerInstance,
                instanceCount,
                startVertexLocation,
                startInstanceLocation
            );
        }

        /// <summary>
        /// Submits an indexed and instanced draw call
        /// </summary>
        public void DrawIndexedInstanced(uint indexCountPerInstance, uint instanceCount, uint startIndexLocation, int baseVertexLocation, uint startInstanceLocation)
        {
            _list.Get()->DrawIndexedInstanced(
                indexCountPerInstance,
                instanceCount,
                startIndexLocation,
                baseVertexLocation,
                startInstanceLocation
            );
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            _list.Dispose();
            _allocator.Dispose();
        }
    }

    /// <summary>
    /// Represents the parameters used for a call to <see cref="ID3D12GraphicsCommandList.DrawInstanced"/>
    /// </summary>
    public readonly struct DrawArgs
    {
        /// <summary>
        /// Number of indices read from the vertex buffer for each instance
        /// </summary>
        public readonly uint VertexCountPerInstance;

        /// <summary>
        /// Number of instances to draw
        /// </summary>
        public readonly uint InstanceCount;

        /// <summary>
        /// The location of the first vertex read by the GPU from the vertex buffer
        /// </summary>
        public readonly uint StartVertexLocation;

        /// <summary>
        /// A value added to each vertex before reading per-instance data from a vertex buffer
        /// </summary>
        public readonly uint StartInstanceLocation;

        /// <summary>
        /// Creates a new instance of <see cref="IndexedDraw"/>
        /// </summary>
        public DrawArgs(
            uint vertexCountPerInstance,
            uint instanceCount,
            uint startVertexLocation,
            uint startInstanceLocation
        )
        {
            VertexCountPerInstance = vertexCountPerInstance;
            InstanceCount = instanceCount;
            StartVertexLocation = startVertexLocation;
            StartInstanceLocation = startInstanceLocation;
        }
    }
}
