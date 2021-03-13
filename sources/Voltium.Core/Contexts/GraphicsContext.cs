using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using TerraFX.Interop;
using Voltium.Common;
using Voltium.Core.CommandBuffer;
using Voltium.Core.Contexts;
using Voltium.Core.Devices;
using Voltium.Core.Exceptions;
using Voltium.Core.Memory;
using Voltium.Core.Pipeline;
using Voltium.TextureLoading;
using static TerraFX.Interop.D3D_PRIMITIVE_TOPOLOGY;
using Buffer = Voltium.Core.Memory.Buffer;

namespace Voltium.Core
{
    public enum ShadingRate
    {
        Shade1x1 = D3D12_SHADING_RATE.D3D12_SHADING_RATE_1X1,
        Shade1x2 = D3D12_SHADING_RATE.D3D12_SHADING_RATE_1X2,
        Shade2x1 = D3D12_SHADING_RATE.D3D12_SHADING_RATE_2X1,
        Shade2x2 = D3D12_SHADING_RATE.D3D12_SHADING_RATE_2X2,
        Shade2x4 = D3D12_SHADING_RATE.D3D12_SHADING_RATE_2X4,
        Shade4x2 = D3D12_SHADING_RATE.D3D12_SHADING_RATE_4X2,
        Shade4x4 = D3D12_SHADING_RATE.D3D12_SHADING_RATE_4X4
    }

    /// <summary>
    /// 
    /// </summary>
    public enum Combiner
    {
        Passthrough = D3D12_SHADING_RATE_COMBINER.D3D12_SHADING_RATE_COMBINER_PASSTHROUGH,
        Override = D3D12_SHADING_RATE_COMBINER.D3D12_SHADING_RATE_COMBINER_OVERRIDE,
        Min = D3D12_SHADING_RATE_COMBINER.D3D12_SHADING_RATE_COMBINER_MIN,
        Max = D3D12_SHADING_RATE_COMBINER.D3D12_SHADING_RATE_COMBINER_MAX,
        Sum = D3D12_SHADING_RATE_COMBINER.D3D12_SHADING_RATE_COMBINER_SUM,
    }


    public unsafe struct DepthStencil
    {
        public View Resource;
        public LoadOperation DepthLoad;
        public StoreOperation DepthStore;
        public LoadOperation StencilLoad;
        public StoreOperation StencilStore;
        public float DepthClear;
        public byte StencilClear;
    }

    public unsafe struct RenderTarget
    {
        public View Resource;
        public LoadOperation Load;
        public StoreOperation Store;
        public Rgba128 ColorClear;
    }
    /// <summary>
    /// Represents a context on which GPU commands can be recorded
    /// </summary>
    public unsafe partial class GraphicsContext : ComputeContext
    {
        public GraphicsContext(bool closed = false) : base()
        {
            if (closed)
            {
                Close();
            }
        }

        public readonly struct EndRenderPassMarker : IDisposable
        {
            private readonly GraphicsContext _context;

            internal EndRenderPassMarker(GraphicsContext context) => _context = context;

            public void Dispose() => _context.EndRenderPass();
        }


        public EndRenderPassMarker ScopedRenderPass(in RenderTarget renderTarget)
        {
            BeginRenderPass(renderTarget);
            return new(this);
        }
        public EndRenderPassMarker ScopedRenderPass(in DepthStencil depthStencil)
        {
            BeginRenderPass(depthStencil);
            return new(this);
        }
        public EndRenderPassMarker ScopedRenderPass(in RenderTarget renderTarget, in DepthStencil depthStencil)
        {
            BeginRenderPass(renderTarget, depthStencil);
            return new(this);
        }
        public EndRenderPassMarker ScopedRenderPass(ReadOnlySpan<RenderTarget> renderTargets)
        {
            BeginRenderPass(renderTargets);
            return new(this);
        }
        public EndRenderPassMarker ScopedRenderPass(ReadOnlySpan<RenderTarget> renderTargets, in DepthStencil depthStencil)
        {
            BeginRenderPass(renderTargets, depthStencil);
            return new(this);
        }

        public void BeginRenderPass(in RenderTarget renderTarget)
        {
            var command = new CommandBeginRenderPass
            {
                RenderTargetCount = 1,
            };

            RenderPassRenderTarget rp;
            EncodeRenderTarget(renderTarget, &rp);

            _encoder.EmitVariable(&command, &rp, 1);
        }

        public void BeginRenderPass(in DepthStencil depthStencil)
        {
            var command = new CommandBeginRenderPass
            {
                RenderTargetCount = 0,
                HasDepthStencil = true
            };


            EncodeDepthStencil(depthStencil, &command.DepthStencil);

            _encoder.Emit(&command);
        }

        public void BeginRenderPass(in RenderTarget renderTarget, in DepthStencil depthStencil)
        {
            var command = new CommandBeginRenderPass
            {
                RenderTargetCount = 1,
                HasDepthStencil = true
            };

            RenderPassRenderTarget rp;
            EncodeRenderTarget(renderTarget, &rp);
            EncodeDepthStencil(depthStencil, &command.DepthStencil);

            _encoder.EmitVariable(&command, &rp, 1);
        }


        public void BeginRenderPass(ReadOnlySpan<RenderTarget> renderTargets)
        {
            if (renderTargets.Length > 8)
            {
                ThrowGraphicsException("Too many render targets!!");
            }

            var command = new CommandBeginRenderPass
            {
                RenderTargetCount = (uint)renderTargets.Length,
            };

            var rps = stackalloc RenderPassRenderTarget[8];

            uint i = 0;
            foreach (ref readonly var renderTarget in renderTargets)
            {
                EncodeRenderTarget(renderTarget, &rps[i++]);
            }

            _encoder.EmitVariable(&command, rps, i);
        }

        public void BeginRenderPass(ReadOnlySpan<RenderTarget> renderTargets, in DepthStencil depthStencil)
        {
            var command = new CommandBeginRenderPass
            {
                RenderTargetCount = (uint)renderTargets.Length,
                HasDepthStencil = true
            };

            EncodeDepthStencil(depthStencil, &command.DepthStencil);
            var rps = stackalloc RenderPassRenderTarget[8];

            uint i = 0;
            foreach (ref readonly var renderTarget in renderTargets)
            {
                EncodeRenderTarget(renderTarget, &rps[i++]);
            }

            _encoder.EmitVariable(&command, rps, i);
        }

        private void EncodeDepthStencil(in DepthStencil pDepth, RenderPassDepthStencil* pRp)
        {
            // Currently identical formats
            pRp->View = pDepth.Resource.Handle;
            pRp->DepthLoad = pDepth.DepthLoad;
            pRp->DepthStore = pDepth.DepthStore;
            pRp->StencilLoad = pDepth.StencilLoad;
            pRp->StencilStore = pDepth.StencilStore;
            pRp->Depth = pDepth.DepthClear;
            pRp->Stencil = pDepth.StencilClear;
        }
        private void EncodeRenderTarget(in RenderTarget pDepth, RenderPassRenderTarget* pRp)
        {
            pRp->View = pDepth.Resource.Handle;
            pRp->Load = pDepth.Load;
            pRp->Store = pDepth.Store;
            Unsafe.Write(pRp->ClearValue, pDepth.ColorClear);
        }

        public void ClearDepth(in View depth, float value = 1)
        {
            var command = new CommandClearDepthStencil
            {
                Depth = value,
                Flags = DepthStencilClearFlags.ClearDepth,
                View = depth.Handle
            };

            _encoder.Emit(&command);
        }

        public void EndRenderPass() => _encoder.EmitEmpty(CommandType.EndRenderPass);

        /// <summary>
        /// If depth bounds testing is enabled, sets the depth bounds
        /// </summary>
        /// <param name="min">The <see cref="float"/> which indicates the minimum depth value which won't be discarded</param>
        /// <param name="max">The <see cref="float"/> which indicates the maximum depth value which won't be discarded</param>
        public void SetDepthsBounds(float min, float max)
        {
            var command = new CommandSetDepthBounds
            {
                Min = min,
                Max = max
            };

            _encoder.Emit(&command);
        }

        ///// <summary>
        ///// Executes a <see cref="GraphicsContext"/>
        ///// </summary>
        ///// <param name="bundle"></param>
        //[ /* no nested bundles */ IllegalBundleMethod]
        //public void ExecuteBundle(GraphicsContext bundle)
        //{
        //    List->ExecuteBundle(bundle.GetListPointer());
        //}

        public void SetShadingRate(ShadingRate defaultShadingRate, Combiner defaultRateAndPerPrimitiveRateCombiner, Combiner shadingRateTextureCombiner)
        {
            var command = new CommandSetShadingRate
            {
                BaseRate = defaultShadingRate,
                ShaderCombiner = defaultRateAndPerPrimitiveRateCombiner,
                ImageCombiner = shadingRateTextureCombiner
            };

            _encoder.Emit(&command);
        }

        public void SetShadingRateTexture([RequiresResourceState(ResourceState.VariableShadeRateSource)] in Texture tex)
        {
            var command = new CommandSetShadingRateImage
            {
                ShadingRateImage = tex.Handle
            };

            _encoder.Emit(&command);
        }

        /// <summary>
        /// Sets the viewport and scissor rectangle
        /// </summary>
        /// <param name="width">The width, in pixels</param>
        /// <param name="height">The height, in pixels</param>
        [IllegalBundleMethod]
        public void SetViewportAndScissor(uint width, uint height)
        {
            SetViewports(new Viewport(0, 0, width, height, 0, Windows.D3D12_MAX_DEPTH));
            SetScissorRectangles(new Rectangle(0, 0, (int)width, (int)height));
        }

        /// <summary>
        /// Sets the viewport and scissor rectangle
        /// </summary>
        /// <param name="size">The size, in pixels</param>
        [IllegalBundleMethod]
        public void SetViewportAndScissor(Size size)
            => SetViewportAndScissor((uint)size.Width, (uint)size.Height);

        /// <summary>
        /// Sets the blend factor for the pipeline <see cref="BlendDesc"/>
        /// </summary>
        /// <param name="value">The value of the blend factor</param>
        public void SetBlendFactor(Rgba128 value)
        {
            var command = new CommandSetBlendFactor();
            Unsafe.As<float, Rgba128>(ref command.BlendFactor[0]) = value;

            _encoder.Emit(&command);
        }

        /// <summary>
        /// Sets the stencil ref for the pipeline <see cref="BlendDesc"/>
        /// </summary>
        /// <param name="value">The value of the stencil ref</param>
        public void SetStencilRef(uint value)
        {
            var command = new CommandSetStencilRef
            {
                StencilRef = value
            };

            _encoder.Emit(&command);
        }

        /// <summary>
        /// Sets the primitive toplogy for geometry
        /// </summary>
        /// <param name="topology">The <see cref="D3D_PRIMITIVE_TOPOLOGY"/> to use</param>
        public void SetTopology(Topology topology)
        {
            var command = new CommandSetTopology
            {
                Topology = topology,
            };

            _encoder.Emit(&command);
        }

        /// <summary>
        /// Sets the number of points in a control point patch
        /// </summary>
        /// <param name="count">The number of points per patch></param>
        public void SetControlPatchPointCount(byte count)
        {
            Guard.InRangeInclusive(1, 32, count);

            var command = new CommandSetPatchListCount
            {
                PatchListCount = count,
            };

            _encoder.Emit(&command);
        }
        /// <summary>
        /// Set the viewports
        /// </summary>
        /// <param name="viewport">The viewport to set</param>
        [IllegalBundleMethod]
        public void SetViewports(in Viewport viewport) => SetViewports(MemoryMarshal.CreateReadOnlySpan(ref Unsafe.AsRef(in viewport), 1));

        /// <summary>
        /// Set the scissor rectangles
        /// </summary>
        /// <param name="rectangles">The rectangles to set</param>
        [IllegalBundleMethod]
        public void SetScissorRectangles(ReadOnlySpan<Rectangle> rectangles)
        {
            fixed (Rectangle* pRectangles = rectangles)
            {
                var command = new CommandSetScissorRectangles
                {
                    Count = (uint)rectangles.Length
                };

                _encoder.EmitVariable(&command, pRectangles, (uint)rectangles.Length);
            }
        }


        /// <summary>
        /// Set the scissor rectangles
        /// </summary>
        /// <param name="rectangle">The rectangle to set</param>
        [IllegalBundleMethod]
        public void SetScissorRectangles(Size rectangle) => SetScissorRectangles(new Rectangle(0, 0, rectangle.Width, rectangle.Height));

        /// <summary>
        /// Set the scissor rectangles
        /// </summary>
        /// <param name="rectangle">The rectangle to set</param>
        [IllegalBundleMethod]
        public void SetScissorRectangles(Rectangle rectangle)
        {
            var command = new CommandSetScissorRectangles
            {
                Count = 1
            };

            _encoder.EmitVariable(&command, &rectangle, 1);
        }

        /// <summary>
        /// Set the viewports
        /// </summary>
        /// <param name="viewports">The viewports to set</param>
        [IllegalBundleMethod]
        public void SetViewports(ReadOnlySpan<Viewport> viewports)
        {
            fixed (Viewport* pViewports = viewports)
            {
                var command = new CommandSetViewports
                {
                    Count = (uint)viewports.Length
                };

                _encoder.EmitVariable(&command, pViewports, (uint)viewports.Length);
            }
        }

        /// <summary>
        /// Set the vertex buffers
        /// </summary>
        /// <param name="vertexResource">The vertex buffer to set</param>
        /// <param name="startSlot">The slot on the device array to set the vertex buffer to</param>
        /// <typeparam name="T">The type of the vertex in <see cref="Buffer"/></typeparam>
        public void SetVertexBuffers<T>([RequiresResourceState(ResourceState.VertexBuffer)] in Buffer vertexResource, uint startSlot = 0)
            where T : unmanaged
        => SetVertexBuffers<T>(vertexResource, (uint)vertexResource.Length / (uint)sizeof(T), startSlot);

        /// <summary>
        /// Set the vertex buffers
        /// </summary>
        /// <param name="vertexResource">The vertex buffer to set</param>
        /// <param name="numVertices">The number of vertices in the buffer</param>
        /// <param name="startSlot">The slot on the device array to set the vertex buffer to</param>
        /// <typeparam name="T">The type of the vertex in <see cref="Buffer"/></typeparam>
        public void SetVertexBuffers<T>([RequiresResourceState(ResourceState.VertexBuffer)] in Buffer vertexResource, uint numVertices, uint startSlot = 0)
            where T : unmanaged
        {
            var command = new CommandSetVertexBuffers
            {
                FirstBufferIndex = startSlot,
                Count = 1
            };

            var view = new VertexBuffer
            {
                Buffer = vertexResource.Handle,
                Length = numVertices * (uint)sizeof(T),
                Stride = (uint)sizeof(T)
            };

            _encoder.EmitVariable(&command, &view, 1);
        }

        /// <summary>
        /// Set the index buffer
        /// </summary>
        /// <param name="indexResource">The index buffer to set</param>
        /// <param name="numIndices">The number of indices to bind</param>
        /// <typeparam name="T">The type of the index in <see cref="Buffer"/></typeparam>
        public void SetIndexBuffer<T>([RequiresResourceState(ResourceState.IndexBuffer)] in Buffer indexResource, uint numIndices = uint.MaxValue)
            where T : unmanaged
        {
            var command = new CommandSetIndexBuffer
            {
                Buffer = indexResource.Handle,
                Format = IndexFormatForT<T>(),
                Length = (uint)(Math.Min(numIndices, indexResource.Length))
            };

            _encoder.Emit(&command);
        }

        private static IndexFormat IndexFormatForT<T>()
             => default(T) switch { uint => IndexFormat.R32UInt, ushort => IndexFormat.R16UInt, _ => IndexFormat.NoIndexBuffer };

        /// <summary>
        /// Submits a draw call
        /// </summary>
        public void Draw(int vertexCountPerInstance, int startVertexLocation = 0)
            => DrawInstanced((uint)vertexCountPerInstance, 1, (uint)startVertexLocation, 0);

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
        /// Submits an indexed draw call
        /// </summary>
        public void DrawIndexed(int indexCountPerInstance, int startIndexLocation = 0, int baseVertexLocation = 0)
            => DrawIndexedInstanced((uint)indexCountPerInstance, 1, (uint)startIndexLocation, baseVertexLocation, 0);

        /// <summary>
        /// Submits an instanced draw call
        /// </summary>
        public void DrawInstanced(uint vertexCountPerInstance, uint instanceCount, uint startVertexLocation, uint startInstanceLocation)
        {
            var command = new CommandDraw
            {
                VertexCountPerInstance = vertexCountPerInstance,
                InstanceCount = instanceCount,
                StartVertexLocation = startVertexLocation,
                StartInstanceLocation = startInstanceLocation
            };

            _encoder.Emit(&command);
        }

        /// <summary>
        /// Submits an indexed and instanced draw call
        /// </summary>
        public void DrawIndexedInstanced(uint indexCountPerInstance, uint instanceCount, uint startIndexLocation, int baseVertexLocation, uint startInstanceLocation)
        {
            var command = new CommandDrawIndexed
            {
                IndexCountPerInstance = indexCountPerInstance,
                InstanceCount = instanceCount,
                StartIndexLocation = startIndexLocation,
                BaseVertexLocation = baseVertexLocation,
                StartInstanceLocation = startInstanceLocation,
            };

            _encoder.Emit(&command);
        }

        /// <summary>
        /// Dispatches a mesh or amplification shader
        /// </summary>
        /// <param name="x">The number of thread groups to execute in the x direction</param>
        /// <param name="y">The number of thread groups to execute in the y direction</param>
        /// <param name="z">The number of thread groups to execute in the z direction</param>
        public void DispatchMeshes(uint x, uint y, uint z)
        {
            var command = new CommandDispatchMesh
            {
                X = x,
                Y = y,
                Z = z
            };

            _encoder.Emit(&command);
        }

        private void ThrowGraphicsException(string s) => new GraphicsException(null!, s);
    }
}
