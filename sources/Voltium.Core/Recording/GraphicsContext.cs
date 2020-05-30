using System;
using System.Diagnostics;
using TerraFX.Interop;
using Voltium.Common;
using Voltium.Common.Debugging;
using Voltium.Core.GpuResources;
using Voltium.Core.Pipeline;

namespace Voltium.Core
{
    /// <summary>
    /// Represents a context on which GPU commands can be recorded
    /// </summary>
    public unsafe struct GraphicsContext : IDisposable
    {
        private ComPtr<ID3D12GraphicsCommandList> _list;
        private ComPtr<ID3D12CommandAllocator> _allocator;

        internal ComPtr<ID3D12GraphicsCommandList> GetAndReleaseList() => _list.Move();
        internal ComPtr<ID3D12CommandAllocator> GetAndReleaseAllocator() => _allocator.Move();

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public GraphicsContext Move()
        {
            var copy = this;
            copy._list = _list.Move();
            copy._allocator = _allocator.Move();
            return copy;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public GraphicsContext Copy()
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
        public void CopyResource(GpuResource source, GpuResource destination)
        {
            Debug.Assert(AreCopyable(source, destination)); // this is just a "most of the time scenario"
            // line below can error even if assertion isn't hit

            _list.Get()->CopyResource(source.UnderlyingResource, destination.UnderlyingResource);
        }

        /// <summary>
        /// Sets a directly-bound constant buffer view descriptor to the graphics pipeline
        /// </summary>
        /// <param name="paramIndex">The index in the <see cref="RootSignature"/> which this view represents</param>
        /// <param name="resource">The GPU address of the resource</param>
        public void SetGraphicsConstantBufferDescriptor(uint paramIndex, GpuResource resource)
        {
            _list.Get()->SetGraphicsRootConstantBufferView(paramIndex, resource.GpuAddress);
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
        public void ResourceTransition(GpuResource resource, ResourceTransition transition)
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
                StateBefore = resource.State,
                StateAfter = transition.NewState,
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
        public void SetPrimitiveTopology(D3D_PRIMITIVE_TOPOLOGY topology)
        {
            _list.Get()->IASetPrimitiveTopology(topology);
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
        /// <typeparam name="T">The type of the vertex in <see cref="VertexBuffer{TVertex}"/></typeparam>
        public void SetVertexBuffers<T>(VertexBuffer<T> vertexResource, uint startSlot = 0)
            where T : unmanaged
        {
            var desc = vertexResource.BufferView;
            _list.Get()->IASetVertexBuffers(startSlot, 1, &desc);
        }

        /// <summary>
        /// Set the vertex buffers
        /// </summary>
        /// <param name="vertexBuffers">The vertex buffers to set</param>
        /// <param name="startSlot">The slot on the device array to start setting vertex buffers to</param>
        /// <typeparam name="T">The type of the vertex in <see cref="VertexBuffer{TVertex}"/></typeparam>
        public void SetVertexBuffers<T>(ReadOnlySpan<VertexBuffer<T>> vertexBuffers, uint startSlot = 0)
            where T : unmanaged
        {
            Debug.Assert(StackSentinel.SafeToStackalloc<D3D12_VERTEX_BUFFER_VIEW>(vertexBuffers.Length));

            D3D12_VERTEX_BUFFER_VIEW* views = stackalloc D3D12_VERTEX_BUFFER_VIEW[vertexBuffers.Length];
            for (int i = 0; i < vertexBuffers.Length; i++)
            {
                views[i] = vertexBuffers[i].BufferView;
            }

            _list.Get()->IASetVertexBuffers(startSlot, (uint)vertexBuffers.Length, views);
        }

        /// <summary>
        /// Set the index buffer
        /// </summary>
        /// <param name="indexResource">The index buffer to set</param>
        /// <typeparam name="T">The type of the index in <see cref="IndexBuffer{TIndex}"/></typeparam>
        public void SetIndexBuffer<T>(IndexBuffer<T> indexResource)
            where T : unmanaged
        {
            var desc = indexResource.BufferView;
            _list.Get()->IASetIndexBuffer(&desc);
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
            Logger.LogInformation("CommandContext disposed");
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
