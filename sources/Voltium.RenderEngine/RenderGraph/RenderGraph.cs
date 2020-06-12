using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TerraFX.Interop;
using Voltium.Common;
using Voltium.Core.GpuResources;
using Voltium.Core.Managers;
using Voltium.Core.Pipeline;
using Voltium.RenderEngine.Passes;
using Voltium.RenderEngine.RenderGraph;
using static TerraFX.Interop.D3D12_RESOURCE_STATES;
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace Voltium.Core
{
    public unsafe class RenderGraph
    {
        private List<ResourceData> _resources = new(8);
        private List<RenderPassBuilder> _passes = new(4);
        private TexHandle _backBuffer = new TexHandle(0xFFFFFFFF);

        public void AddPass<T>(T val) where T : RenderPass
        {
            var pass = CreatePassBuilder();
            val.Register(ref pass);
        }

        public void SetPresentPass<T>(T val) where T : PresentPass
        {
            var pass = CreatePassBuilder();
            val.Register(ref pass, _backBuffer);
        }

        private RenderPassBuilder CreatePassBuilder()
        {
            var pass = new RenderPassBuilder((uint)_passes.Count, this);
            _passes.Add(pass);
            return pass;
        }

        internal TexHandle RegisterTexture(ResourceData data)
        {
            Debug.Assert(data.Dimension is not null);
            _resources.Add(data);
            return new TexHandle((uint)_resources.Count - 1);
        }

        internal BufferHandle RegisterBuffer(ResourceData data)
        {
            Debug.Assert(data.Dimension is null);
            _resources.Add(data);
            return new BufferHandle((uint)_resources.Count - 1);
        }

        internal ResourceData GetResource(TexHandle handle) => _resources[(int)handle.Index];
        internal void SetResource(TexHandle handle, ResourceData value) => _resources[(int)handle.Index] = value;
    }

    public struct ComponentResolver
    {
        public RenderComponents Components { get; private set; }
        public Texture Resolve(TexHandle handle) => throw new NotImplementedException();
        public Buffer Resolve(BufferHandle handle) => throw new NotImplementedException();
    }


    public struct Texture
    {
    }

    public struct Buffer
    {

    }

    public readonly struct TexHandle
    {
        // index into the graph's list of resources
        internal readonly uint Index;

        internal TexHandle(uint index)
        {
            Index = index;
        }
    }

    public readonly struct BufferHandle
    {
        // index into the graph's list of resources
        internal readonly uint Index;

        internal BufferHandle(uint index)
        {
            Index = index;
        }
    }

    internal struct ResourceData
    {
        internal double? SwapChainMultiplier;
        internal TextureDimension? Dimension; // null == buffer
        internal nuint Width, Height, Depth;
        internal ResourceState InitialState;
        internal ResourceFlags Flags;

        internal uint CreationPass;
        internal List<(uint PassIndex, ResourceReadFlags Flags)>? ReadPasses;
        internal List<(uint PassIndex, ResourceWriteFlags Flags)>? WritePasses;
    }

    public struct RenderPassBuilder
    {
        private uint _passIndex;
        public RenderComponents Components { get; }
        private RenderGraph _graph;

        internal RenderPassBuilder(uint passIndex, RenderGraph graph)
        {
            _passIndex = passIndex;
            _graph = graph;
            Components = RenderComponents.Create();
        }

        public TexHandle CreateRenderTarget()
        {
            var res = new ResourceData
            {
                SwapChainMultiplier = 1,
                Flags = ResourceFlags.AllowRenderTarget,
                Dimension = TextureDimension.Tex2D
            };

            return RegisterGraphTexture(res);
        }

        public TexHandle CreateDepthStencil(bool shaderVisible)
        {
            var res = new ResourceData
            {
                SwapChainMultiplier = 1,
                Flags = ResourceFlags.AllowDepthStencil | (shaderVisible ? 0 : ResourceFlags.DenyShaderResource),
                CreationPass = _passIndex,
                Dimension = TextureDimension.Tex2D
            };

            return RegisterGraphTexture(res);
        }

        public BufferHandle CreateVertexBuffer(nuint size)
        {
            var res = new ResourceData { Width = size };

            return RegisterGraphBuffer(res);
        }

        private TexHandle RegisterGraphTexture(ResourceData res)
        {
            res.CreationPass = _passIndex;
            return _graph.RegisterTexture(res);
        }

        private BufferHandle RegisterGraphBuffer(ResourceData res)
        {
            res.CreationPass = _passIndex;
            return _graph.RegisterBuffer(res);
        }

        public void Write(TexHandle tex, ResourceWriteFlags flags)
        {
            var res = _graph.GetResource(tex);

            res.WritePasses ??= new();

            res.WritePasses.Add((_passIndex, flags));
        }

        public void Read(TexHandle tex, ResourceReadFlags flags)
        {
            var res = _graph.GetResource(tex);

            res.ReadPasses ??= new();

            res.ReadPasses.Add((_passIndex, flags));
        }
    }

    // The valid combinations of these flags is quite finicky so we expose them as bools
    internal enum ResourceFlags : uint
    {
        AllowDepthStencil = D3D12_RESOURCE_FLAGS.D3D12_RESOURCE_FLAG_ALLOW_DEPTH_STENCIL,
        AllowRenderTarget = D3D12_RESOURCE_FLAGS.D3D12_RESOURCE_FLAG_ALLOW_RENDER_TARGET,
        AllowUnorderedAccess = D3D12_RESOURCE_FLAGS.D3D12_RESOURCE_FLAG_ALLOW_UNORDERED_ACCESS,
        DenyShaderResource = D3D12_RESOURCE_FLAGS.D3D12_RESOURCE_FLAG_DENY_SHADER_RESOURCE,
    }

    public enum ResourceWriteFlags : uint
    {
        /// <summary>
        /// The resource is being used as an unordered access resource. This state is read/write
        /// </summary>
        UnorderedAccess = D3D12_RESOURCE_STATE_UNORDERED_ACCESS,

        /// <summary>
        /// The resource is being used as a render target. This state is write-only
        /// </summary>
        RenderTarget = D3D12_RESOURCE_STATE_RENDER_TARGET,

        /// <summary>
        /// The resource is being written to as a depth buffer, e.g when <see cref="DepthStencilDesc.EnableDepthTesting"/> is <see langword="true"/>.
        /// </summary>
        DepthWrite = D3D12_RESOURCE_STATE_DEPTH_WRITE,
        
        /// <summary>
        /// The resource is being used as a stream-out destination. This is a write-only state
        /// </summary>
        StreamOut = D3D12_RESOURCE_STATE_STREAM_OUT,

        /// <summary>
        /// The resource is being used as the destination of a GPU copy operation
        /// </summary>
        CopyDestination = D3D12_RESOURCE_STATE_COPY_DEST,

        /// <summary>
        /// The resource is being used as the destination of a MSAA resolve operation
        /// </summary>
        ResolveDestination = D3D12_RESOURCE_STATE_RESOLVE_DEST
    }
    public enum ResourceReadFlags : uint
    {
        /// <summary>
        /// The resource is being used as a vertex buffer. This state is read-only
        /// </summary>
        VertexBuffer = D3D12_RESOURCE_STATE_VERTEX_AND_CONSTANT_BUFFER,

        /// <summary>
        /// The resource is being used as a constant buffer. This state is read-only
        /// </summary>
        ConstantBuffer = D3D12_RESOURCE_STATE_VERTEX_AND_CONSTANT_BUFFER,

        /// <summary>
        /// The resource is being used as a vertex or constant buffer. This state is read-only
        /// </summary>
        VertexOrConstantBuffer = D3D12_RESOURCE_STATE_VERTEX_AND_CONSTANT_BUFFER,

        /// <summary>
        /// The resource is being used as an index buffer. This state is read-only
        /// </summary>
        IndexBuffer = D3D12_RESOURCE_STATE_INDEX_BUFFER,

        /// <summary>
        /// The resource is being read from as a depth buffer/
        /// This state is read-only and cannot be combined with
        /// </summary>
        DepthRead = D3D12_RESOURCE_STATE_DEPTH_READ,

        /// <summary>
        /// The resource is being used by a shader other than <see cref="ShaderType.Pixel"/>
        /// </summary>
        NonPixelShaderResource = D3D12_RESOURCE_STATE_NON_PIXEL_SHADER_RESOURCE,

        /// <summary>
        /// The resource is being used by a <see cref="ShaderType.Pixel"/> shader
        /// </summary>
        PixelShaderResource = D3D12_RESOURCE_STATE_PIXEL_SHADER_RESOURCE,

        /// <summary>
        /// The resource is being used as an indirect argument. This is a read-only state
        /// </summary>
        IndirectArgument = D3D12_RESOURCE_STATE_INDIRECT_ARGUMENT,

        /// <summary>
        /// The resource is being used as the source of a GPU copy operation
        /// </summary>
        CopySource = D3D12_RESOURCE_STATE_COPY_SOURCE,

        /// <summary>
        /// The resource can be read as several other <see cref="ResourceState"/>s. This is the starting
        /// state for a <see cref="GpuMemoryType.CpuUpload"/> resource
        /// </summary>
        GenericRead = D3D12_RESOURCE_STATE_GENERIC_READ,

        /// <summary>
        /// The resource is being used as the source of a MSAA resolve operation
        /// </summary>
        ResolveSource = D3D12_RESOURCE_STATE_RESOLVE_SOURCE
    }
}
