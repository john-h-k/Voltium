using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using TerraFX.Interop;
using Voltium.Common;
using Voltium.Core.NativeApi;
using Voltium.Core.Pipeline;
using Voltium.Core.Queries;

namespace Voltium.Core.NativeApi
{
    /// <summary>
    /// The pipeline bind point
    /// </summary>
    public enum BindPoint : ulong
    {
        /// <summary>
        /// The graphics pipeline
        /// </summary>
        Graphics,

        /// <summary>
        /// The compute pipeline
        /// </summary>
        Compute
    }

    /// <summary>
    /// Provides information for tooling about commands
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class CommandInfoAttribute : Attribute
    {
        /// <summary>
        /// Create a new instance of <see cref="CommandInfoAttribute"/>
        /// </summary>
        /// <param name="type">The <see cref="RequiredQueueType"/> for this command</param>
        /// <param name="flags">Any <see cref="CommandFlags"/> about this command</param>
        public CommandInfoAttribute(RequiredQueueType type, CommandFlags flags = CommandFlags.None)
        {

        }
    }

    /// <summary>
    /// The queue type required for a command
    /// </summary>
    public enum RequiredQueueType
    {
        /// <summary>
        /// A copy queue is required
        /// </summary>
        Copy = 1,

        /// <summary>
        /// A compute queue is required
        /// </summary>
        Compute = 2,

        /// <summary>
        /// A graphics queue is required
        /// </summary>
        Graphics = 4
    }

    /// <summary>
    /// Flags about a command
    /// </summary>
    public enum CommandFlags
    {
        /// <summary>
        /// No flags
        /// </summary>
        None = 0,

        /// <summary>
        /// This command is not allowed in pre-processed command buffer
        /// </summary>
        ForbiddenInBundle = 1,

        /// <summary>
        /// This command is not allowed within a render pass
        /// </summary>
        ForbiddenInRenderPass = 2
    }

    /// <summary>
    /// The type of a command buffer command
    /// </summary>
    public enum CommandType : ulong
    {
        // Debug/profiling

        /// <summary>
        /// Inserts a metadata marker into the command buffer. This is for tooling only
        /// </summary>
        [CommandInfo(RequiredQueueType.Copy)]
        InsertMarker,

        /// <summary>
        /// Begins a metadata event. This is for tooling only
        /// </summary>
        [CommandInfo(RequiredQueueType.Copy)]
        BeginEvent,

        /// <summary>
        /// Ends a metadata event. This is for tooling only
        /// </summary>
        [CommandInfo(RequiredQueueType.Copy)]
        EndEvent,

        // Barriers

        /// <summary>
        /// Transitions a resource from one state to another. This might involve cache flushes,
        /// cache invalidations, or layout changes
        /// </summary>
        [CommandInfo(RequiredQueueType.Copy, CommandFlags.ForbiddenInBundle)]
        Transition,

        /// <summary>
        /// Ensures all prior UAV writes are finished before any later commands execute
        /// </summary>
        [CommandInfo(RequiredQueueType.Copy, CommandFlags.ForbiddenInBundle)]
        WriteBarrier,

        /// <summary>
        /// Indicates that a resource may have been aliased
        /// </summary>
        [CommandInfo(RequiredQueueType.Copy, CommandFlags.ForbiddenInBundle)]
        AliasingBarrier,

        /// <summary>
        /// Sets the currently bound pipeline state
        /// </summary>
        [CommandInfo(RequiredQueueType.Compute)]
        SetPipeline,

        /// <summary>
        /// Sets the currently bound index buffer
        /// </summary>
        [CommandInfo(RequiredQueueType.Graphics)]
        SetIndexBuffer,

        /// <summary>
        /// Sets a contigous range of the currently bound vertex buffers
        /// </summary>
        [CommandInfo(RequiredQueueType.Graphics)]
        SetVertexBuffer,

        /// <summary>
        /// Sets the viewports for the draw
        /// </summary>
        [CommandInfo(RequiredQueueType.Graphics, CommandFlags.ForbiddenInBundle)]
        SetViewports,

        /// <summary>
        /// Sets the scissors rectangles for the draw
        /// </summary>
        [CommandInfo(RequiredQueueType.Graphics, CommandFlags.ForbiddenInBundle)]
        SetScissorRectangles,

        /// <summary>
        /// Sets the shading rate and shading rate combiners for the draw
        /// </summary>
        [CommandInfo(RequiredQueueType.Graphics)]
        SetShadingRate,

        /// <summary>
        /// Sets the shading rate image for the draw
        /// </summary>
        [CommandInfo(RequiredQueueType.Graphics)]
        SetShadingRateImage,

        /// <summary>
        /// Sets the stencil reference value for the draw
        /// </summary>
        [CommandInfo(RequiredQueueType.Graphics)]
        SetStencilRef,

        /// <summary>
        /// Sets the blend factor value for the draw
        /// </summary>
        [CommandInfo(RequiredQueueType.Graphics)]
        SetBlendFactor,

        /// <summary>
        /// Sets the depth bounds values for the draw
        /// </summary>
        [CommandInfo(RequiredQueueType.Graphics)]
        SetDepthBounds,

        /// <summary>
        /// Sets the sample positions for the draw
        /// </summary>
        [CommandInfo(RequiredQueueType.Graphics)]
        SetSamplePositions,

        /// <summary>
        /// Sets the view instancing mask for the draw
        /// </summary>
        [CommandInfo(RequiredQueueType.Graphics)]
        SetViewInstanceMask,


        // Shader resources

        /// <summary>
        /// Binds a dynamic descriptor to a shader resource
        /// </summary>
        [CommandInfo(RequiredQueueType.Compute)]
        BindDynamicBufferDescriptor,

        /// <summary>
        /// Binds a dynamic descriptor to a shader resource
        /// </summary>
        [CommandInfo(RequiredQueueType.Compute)]
        BindDynamicRaytracingAccelerationStructureDescriptor,

        /// <summary>
        /// Binds a set of descriptors to shader resources
        /// </summary>
        [CommandInfo(RequiredQueueType.Compute)]
        BindDescriptors,

        /// <summary>
        /// Binds 32-bit constants to shader resources
        /// </summary>
        [CommandInfo(RequiredQueueType.Compute)]
        Bind32BitConstants,

        // Render passes

        /// <summary>
        /// Begins a render pass. Note that nested render passes are illegal
        /// </summary>
        [CommandInfo(RequiredQueueType.Graphics, CommandFlags.ForbiddenInBundle | CommandFlags.ForbiddenInRenderPass)]
        BeginRenderPass,

        /// <summary>
        /// Ends a render pass. Note that nested render passes are illegal
        /// </summary>
        [CommandInfo(RequiredQueueType.Graphics, CommandFlags.ForbiddenInBundle | CommandFlags.ForbiddenInRenderPass)]
        EndRenderPass,

        // Timestamps and queries

        /// <summary>
        /// Reads the tick count of the queue's timestamp into a query set
        /// </summary>
        [CommandInfo(RequiredQueueType.Copy)]
        ReadTimestamp,

        /// <summary>
        /// Begins a query
        /// </summary>
        [CommandInfo(RequiredQueueType.Graphics)]
        BeginQuery,

        /// <summary>
        /// Ends a query
        /// </summary>
        [CommandInfo(RequiredQueueType.Graphics)]
        EndQuery,

        /// <summary>
        /// Resolves opaque query data to an application-understood format
        /// </summary>
        [CommandInfo(RequiredQueueType.Copy, CommandFlags.ForbiddenInBundle | CommandFlags.ForbiddenInRenderPass)]
        ResolveQuery,

        // Conditional rendering/predication

        /// <summary>
        /// Begins conditional rendering
        /// </summary>
        [CommandInfo(RequiredQueueType.Copy, CommandFlags.ForbiddenInBundle)]
        BeginConditionalRendering,

        /// <summary>
        /// Ends conditional rendering
        /// </summary>
        [CommandInfo(RequiredQueueType.Copy, CommandFlags.ForbiddenInBundle)]
        EndConditionalRendering,

        // Copies

        /// <summary>
        /// Copies a region of a buffer
        /// </summary>
        [CommandInfo(RequiredQueueType.Copy, CommandFlags.ForbiddenInBundle | CommandFlags.ForbiddenInRenderPass)]
        BufferCopy,

        /// <summary>
        /// Copies a region of a texture
        /// </summary>
        [CommandInfo(RequiredQueueType.Copy, CommandFlags.ForbiddenInBundle | CommandFlags.ForbiddenInRenderPass)]
        TextureCopy,

        /// <summary>
        /// Copies a region of a buffer to a texture
        /// </summary>
        [CommandInfo(RequiredQueueType.Copy, CommandFlags.ForbiddenInBundle | CommandFlags.ForbiddenInRenderPass)]
        BufferToTextureCopy,

        /// <summary>
        /// Copies a region of a texture to a buffer
        /// </summary>
        [CommandInfo(RequiredQueueType.Copy, CommandFlags.ForbiddenInBundle | CommandFlags.ForbiddenInRenderPass)]
        TextureToBufferCopy,

        /// <summary>
        /// Writes 32-bit constants to a given address
        /// </summary>
        [CommandInfo(RequiredQueueType.Copy, CommandFlags.ForbiddenInBundle | CommandFlags.ForbiddenInRenderPass)]
        WriteConstants,

        // Clears


        /// <summary>
        /// Clears a buffer to a set of floating-point values
        /// </summary>
        [CommandInfo(RequiredQueueType.Compute, CommandFlags.ForbiddenInBundle | CommandFlags.ForbiddenInRenderPass)]
        ClearBuffer,

        /// <summary>
        /// Clears a buffer to a set of integer values
        /// </summary>
        [CommandInfo(RequiredQueueType.Compute, CommandFlags.ForbiddenInBundle | CommandFlags.ForbiddenInRenderPass)]
        ClearBufferInteger,

        /// <summary>
        /// Clears a texture to a set of floating-point values
        /// </summary>
        [CommandInfo(RequiredQueueType.Compute, CommandFlags.ForbiddenInBundle | CommandFlags.ForbiddenInRenderPass)]
        ClearTexture,

        /// <summary>
        /// Clears a texture to a set of integer values
        /// </summary>
        [CommandInfo(RequiredQueueType.Compute, CommandFlags.ForbiddenInBundle | CommandFlags.ForbiddenInRenderPass)]
        ClearTextureInteger,

        /// <summary>
        /// Clears a depth-stencil to a depth and stencil value
        /// </summary>
        [CommandInfo(RequiredQueueType.Graphics, CommandFlags.ForbiddenInBundle | CommandFlags.ForbiddenInRenderPass)]
        ClearDepthStencil,

        // Raytracing acceleration structures
        BuildTopLevelAccelerationStructure,
        BuildBottomLevelAccelerationStructure,
        CopyAccelerationStructure,
        SerializeAccelerationStructure,
        DeserializeAccelerationStructure,

        // Execute

        /// <summary>
        /// Performs an indirect command with arguments provided by the GPU
        /// </summary>
        [CommandInfo(RequiredQueueType.Graphics)]
        ExecuteIndirect,


        /// <summary>
        /// Perform a draw
        /// </summary>
        [CommandInfo(RequiredQueueType.Graphics)]
        Draw,

        /// <summary>
        /// Perform an indexed draw
        /// </summary>
        [CommandInfo(RequiredQueueType.Graphics)]
        DrawIndexed,

        /// <summary>
        /// Perform a compute dispatch
        /// </summary>
        [CommandInfo(RequiredQueueType.Compute, CommandFlags.ForbiddenInRenderPass)]
        Dispatch,

        /// <summary>
        /// Perform a ray trace operation
        /// </summary>
        [CommandInfo(RequiredQueueType.Compute, CommandFlags.ForbiddenInRenderPass)]
        RayTrace,

        /// <summary>
        /// Perform a mesh dispatch operation
        /// </summary>
        [CommandInfo(RequiredQueueType.Graphics)]
        MeshDispatch,
    }

    internal interface ICommand
    {
        CommandType Type { get; }
    }

    [StructLayout(LayoutKind.Explicit)]
    internal struct Command
    {

        public const int Alignment = sizeof(CommandType);

        [FieldOffset(0)]
        public CommandType Type;

        [FieldOffset(sizeof(CommandType))]
        public byte Arguments;

        #region Shader resources

        [FieldOffset(sizeof(CommandType))]
        public CommandBind32BitConstants Bind32BitConstants;

        [FieldOffset(sizeof(CommandType))]
        public CommandBindDescriptors BindDescriptors;

        [FieldOffset(sizeof(CommandType))]
        public CommandBindDynamicBufferDescriptor BindDynamicBufferDescriptor;

        [FieldOffset(sizeof(CommandType))]
        public CommandBindDynamicRaytracingAccelerationStructureDescriptor BindDynamicRaytracingAccelerationStructureDescriptor;

        #endregion

        #region Barriers

        [FieldOffset(sizeof(CommandType))]
        public CommandTransitions Transitions;

        [FieldOffset(sizeof(CommandType))]
        public CommandWriteBarrier WriteBarriers;

        [FieldOffset(sizeof(CommandType))]
        public CommandAliasingBarrier AliasingBarriers;

        #endregion

        #region Debug/profiling


        [FieldOffset(sizeof(CommandType))]
        public CommandInsertMarker InsertMarker;

        [FieldOffset(sizeof(CommandType))]
        public CommandBeginEvent BeginEvent;

        #endregion

        #region Render passes

        [FieldOffset(sizeof(CommandType))]
        public CommandBeginRenderPass BeginRenderPass;

        #endregion

        #region Timestamps and queries

        [FieldOffset(sizeof(CommandType))]
        public CommandReadTimestamp ReadTimestamp;

        [FieldOffset(sizeof(CommandType))]
        public CommandBeginQuery BeginQuery;

        [FieldOffset(sizeof(CommandType))]
        public CommandEndQuery EndQuery;

        [FieldOffset(sizeof(CommandType))]
        public CommandResolveQuery ResolveQuery;

        #endregion

        #region Conditional rendering/predication

        [FieldOffset(sizeof(CommandType))]
        public CommandBeginConditionalRendering BeginConditionalRendering;

        // EndConditionalRendering has no arguments

        #endregion

        #region Draw State

        [FieldOffset(sizeof(CommandType))]
        public CommandSetIndexBuffer SetIndexBuffer;

        [FieldOffset(sizeof(CommandType))]
        public CommandSetVertexBuffers SetVertexBuffers;

        [FieldOffset(sizeof(CommandType))]
        public CommandSetViewports SetViewports;

        [FieldOffset(sizeof(CommandType))]
        public CommandSetScissorRectangles SetScissorRectangles;

        [FieldOffset(sizeof(CommandType))]
        public CommandSetPipeline SetPipeline;

        [FieldOffset(sizeof(CommandType))]
        public CommandSetShadingRate SetShadingRate;

        [FieldOffset(sizeof(CommandType))]
        public CommandSetShadingRateImage SetShadingRateImage;

        [FieldOffset(sizeof(CommandType))]
        public CommandSetStencilRef SetStencilRef;

        [FieldOffset(sizeof(CommandType))]
        public CommandSetBlendFactor SetBlendFactor;

        [FieldOffset(sizeof(CommandType))]
        public CommandSetDepthBounds SetDepthBounds;

        [FieldOffset(sizeof(CommandType))]
        public CommandSetSamplePositions SetSamplePositions;

        [FieldOffset(sizeof(CommandType))]
        public CommandSetViewInstanceMask SetViewInstanceMask;

        #endregion

        #region Copies

        [FieldOffset(sizeof(CommandType))]
        public CommandBufferCopy BufferCopy;

        [FieldOffset(sizeof(CommandType))]
        public CommandBufferToTextureCopy BufferToTextureCopy;

        [FieldOffset(sizeof(CommandType))]
        public CommandTextureCopy TextureCopy;

        [FieldOffset(sizeof(CommandType))]
        public CommandWriteConstants WriteConstants;

        #endregion

        #region Clears

        [FieldOffset(sizeof(CommandType))]
        public CommandClearBuffer ClearBuffer;

        [FieldOffset(sizeof(CommandType))]
        public CommandClearBufferInteger ClearBufferInteger;

        [FieldOffset(sizeof(CommandType))]
        public CommandClearTexture ClearTexture;

        [FieldOffset(sizeof(CommandType))]
        public CommandClearTextureInteger ClearTextureInteger;

        [FieldOffset(sizeof(CommandType))]
        public CommandClearDepthStencil ClearDepthStencil;

        #endregion

        #region Raytracing acceleration structures

        [FieldOffset(sizeof(CommandType))]
        public CommandBuildTopLevelAccelerationStructure BuildTopLevelAccelerationStructure;

        [FieldOffset(sizeof(CommandType))]
        public CommandBuildBottomLevelAccelerationStructure BuildBottomLevelAccelerationStructure;

        [FieldOffset(sizeof(CommandType))]
        public CommandCopyAccelerationStructure CopyAccelerationStructure;

        [FieldOffset(sizeof(CommandType))]
        public CommandSerializeAccelerationStructure SerializeAccelerationStructure;
        
        [FieldOffset(sizeof(CommandType))]
        public CommandDeserializeAccelerationStructure DeserializeAccelerationStructure;


        #endregion

        #region Execute

        [FieldOffset(sizeof(CommandType))]
        public CommandExecuteIndirect ExecuteIndirect;

        [FieldOffset(sizeof(CommandType))]
        public CommandDraw Draw;

        [FieldOffset(sizeof(CommandType))]
        public CommandDrawIndexed DrawIndexed;

        [FieldOffset(sizeof(CommandType))]
        public CommandDispatch Dispatch;

        [FieldOffset(sizeof(CommandType))]
        public CommandDispatchMesh MeshDispatch;

        [FieldOffset(sizeof(CommandType))]
        public CommandRayTrace RayTrace;

        #endregion
    }

    /// <summary>
    /// The load operation for a given resource
    /// </summary>
    public enum LoadOperation
    {
        /// <summary>
        /// Discard the previous results of the resource, as it will not be read from
        /// </summary>
        Discard = D3D12_RENDER_PASS_BEGINNING_ACCESS_TYPE.D3D12_RENDER_PASS_BEGINNING_ACCESS_TYPE_DISCARD,

        /// <summary>
        /// Clear the resource, so all reads from it return a fixed value
        /// </summary>
        Clear = D3D12_RENDER_PASS_BEGINNING_ACCESS_TYPE.D3D12_RENDER_PASS_BEGINNING_ACCESS_TYPE_CLEAR,

        /// <summary>
        /// Preserves the previous values of the resource, so all reads return the value from the resource
        /// </summary>
        Preserve = D3D12_RENDER_PASS_BEGINNING_ACCESS_TYPE.D3D12_RENDER_PASS_BEGINNING_ACCESS_TYPE_PRESERVE,

        /// <summary>
        /// The resource is not read from, nor written to
        /// </summary>
        NoAccess = D3D12_RENDER_PASS_BEGINNING_ACCESS_TYPE.D3D12_RENDER_PASS_BEGINNING_ACCESS_TYPE_NO_ACCESS,
    }

    /// <summary>
    /// The store operation for a given resource
    /// </summary>
    public enum StoreOperation
    {
        /// <summary>
        /// Discard the results of the render, as it will not be written to
        /// </summary>
        Discard = D3D12_RENDER_PASS_ENDING_ACCESS_TYPE.D3D12_RENDER_PASS_ENDING_ACCESS_TYPE_DISCARD,

        /// <summary>
        /// Resolve the resource from a multisampled resource to a non-multisampled resource
        /// </summary>
        Resolve = D3D12_RENDER_PASS_ENDING_ACCESS_TYPE.D3D12_RENDER_PASS_ENDING_ACCESS_TYPE_RESOLVE,
        
        /// <summary>
        /// Preserve the resource, so all writes to it can be read afterwards
        /// </summary>
        Preserve = D3D12_RENDER_PASS_ENDING_ACCESS_TYPE.D3D12_RENDER_PASS_ENDING_ACCESS_TYPE_PRESERVE,

        /// <summary>
        /// The resource is not read from, nor written to
        /// </summary>
        NoAccess = D3D12_RENDER_PASS_ENDING_ACCESS_TYPE.D3D12_RENDER_PASS_ENDING_ACCESS_TYPE_NO_ACCESS,
    }

    internal unsafe struct CommandExecuteIndirect : ICommand
    {
        public CommandType Type => CommandType.ExecuteIndirect;

        public IndirectCommandHandle IndirectCommand;
        public BufferHandle Arguments;
        public uint Offset;
        public uint Count;
        public bool HasCountSpecifier;
        public CountSpecifier* CountSpecifier => (CountSpecifier*)((uint*)Unsafe.AsPointer(ref HasCountSpecifier) + 1);
    }

    internal struct CountSpecifier
    {
        public BufferHandle CountBuffer;
        public uint Offset;
    }
    internal unsafe struct CommandBuildTopLevelAccelerationStructure : ICommand
    {
        public CommandType Type => CommandType.BuildTopLevelAccelerationStructure;

        public BufferHandle Scratch;
        public RaytracingAccelerationStructureHandle Dest;

        public BuildAccelerationStructureFlags Flags;
        public LayoutType Layout;

        public uint InstanceCount;
        public BufferHandle Instances;
        public uint Offset;
    }

    internal unsafe struct CommandBuildBottomLevelAccelerationStructure : ICommand
    {
        public CommandType Type => CommandType.BuildBottomLevelAccelerationStructure;

        public BufferHandle Scratch;
        public RaytracingAccelerationStructureHandle Dest;

        public BuildAccelerationStructureFlags Flags;

        public uint GeometryCount;
        private uint _pad;
        public GeometryDesc* GeometryDescs => (GeometryDesc*)((uint*)Unsafe.AsPointer(ref _pad) + 1);
    }

    internal unsafe struct CommandCopyAccelerationStructure : ICommand
    {
        public CommandType Type => CommandType.CopyAccelerationStructure;

        public RaytracingAccelerationStructureHandle Source;
        public RaytracingAccelerationStructureHandle Dest;
        public bool Compact;
    }

    internal unsafe struct CommandSerializeAccelerationStructure : ICommand
    {
        public CommandType Type => CommandType.SerializeAccelerationStructure;

        public RaytracingAccelerationStructureHandle Source;
        public BufferHandle Dest;
    }

    internal unsafe struct CommandDeserializeAccelerationStructure : ICommand
    {
        public CommandType Type => CommandType.DeserializeAccelerationStructure;

        public BufferHandle Source;
        public RaytracingAccelerationStructureHandle Dest;
    }

    internal unsafe struct CommandSetIndexBuffer : ICommand
    {
        public CommandType Type => CommandType.SetIndexBuffer;

        public BufferHandle Buffer;
        public IndexFormat Format;
        public uint Length;
    }

    internal unsafe struct CommandSetVertexBuffers : ICommand
    {
        public CommandType Type => CommandType.SetVertexBuffer;

        public uint FirstBufferIndex;
        public uint Count;
        public VertexBuffer* Buffers => (VertexBuffer*)((uint*)Unsafe.AsPointer(ref Count) + 1);
    }

    internal unsafe struct VertexBuffer
    {
        public BufferHandle Buffer;
        public uint Stride;
        public uint Length;
    }

    internal unsafe struct CommandSetViewports : ICommand
    {
        public CommandType Type => CommandType.SetViewports;

        public uint Count;
        public Viewport* Viewports => (Viewport*)((uint*)Unsafe.AsPointer(ref Count) + 1);
    }

    internal enum ResourceHandleType
    {
        Buffer, Texture, RaytracingAccelerationStructure
    }

    [StructLayout(LayoutKind.Explicit)]
    internal unsafe struct ResourceHandle : IEquatable<ResourceHandle>
    {
        [FieldOffset(0)]
        public ResourceHandleType Type;

        [FieldOffset(sizeof(ResourceHandleType))]
        public BufferHandle Buffer;
        [FieldOffset(sizeof(ResourceHandleType))]
        public TextureHandle Texture;
        [FieldOffset(sizeof(ResourceHandleType))]
        public RaytracingAccelerationStructureHandle RaytracingAccelerationStructure;

        public static implicit operator ResourceHandle(BufferHandle h) => new(h);
        public static implicit operator ResourceHandle(TextureHandle h) => new(h);
        public static implicit operator ResourceHandle(RaytracingAccelerationStructureHandle h) => new(h);

        public ResourceHandle(BufferHandle buff)
        {
            Unsafe.SkipInit(out this);
            Type = ResourceHandleType.Buffer;
            Buffer = buff;
        }
        public ResourceHandle(TextureHandle tex)
        {
            Unsafe.SkipInit(out this);
            Type = ResourceHandleType.Texture;
            Texture = tex;
        }
        public ResourceHandle(RaytracingAccelerationStructureHandle accelerationStructure)
        {
            Unsafe.SkipInit(out this);
            Type = ResourceHandleType.RaytracingAccelerationStructure;
            RaytracingAccelerationStructure = accelerationStructure;
        }

        public bool Equals(ResourceHandle other) => Type == other.Type && Buffer.Generational == other.Buffer.Generational;
    }

    internal unsafe struct CommandSetScissorRectangles : ICommand
    {
        public CommandType Type => CommandType.SetScissorRectangles;

        public uint Count;
        public Rectangle* Rectangles => (Rectangle*)((uint*)Unsafe.AsPointer(ref Count) + 1);
    }

    internal unsafe struct CommandWriteBarrier : ICommand
    {
        public CommandType Type => CommandType.WriteBarrier;

        public uint Count;
        public ResourceHandle* Resources => (ResourceHandle*)((uint*)Unsafe.AsPointer(ref Count) + 1);
    }

    internal unsafe struct CommandTransitions : ICommand
    {
        public CommandType Type => CommandType.Transition;

        public uint Count;
        public ResourceTransitionBarrier* Transitions
            => (ResourceTransitionBarrier*)((uint*) Unsafe.AsPointer(ref Count) + 1);
    }

    internal unsafe struct CommandAliasingBarrier : ICommand
    {
        public CommandType Type => CommandType.AliasingBarrier;

        public uint Count;
        public ResourceAlias* Resources => (ResourceAlias*)((uint*)Unsafe.AsPointer(ref Count) + 1);
    }

    internal struct ResourceAlias
    {
        public ResourceHandle Before, After;
    }

    internal unsafe struct CommandInsertMarker : ICommand
    {
        public CommandType Type => CommandType.InsertMarker;

        public uint Length;
        public byte* Data => (byte*)Unsafe.AsPointer(ref Length) + 1;
    }

    internal unsafe struct CommandBeginEvent : ICommand
    {
        public CommandType Type => CommandType.BeginEvent;

        public uint Length;
        public byte* Data => (byte*)Unsafe.AsPointer(ref Length) + 1;
    }



    internal unsafe struct CommandBeginRenderPass : ICommand
#pragma warning restore CS1633 // Unrecognized #pragma directive
    {
        public CommandType Type => CommandType.BeginRenderPass;

        public RenderPassHandle RenderPass;
        public bool HasDepthStencil;
        public RenderPassDepthStencil DepthStencil;
        public bool AllowTextureWrites;
        public uint RenderTargetCount;
        public RenderPassRenderTarget* RenderTargets => (RenderPassRenderTarget*)((uint*)Unsafe.AsPointer(ref RenderTargetCount) + 1);
    }

    internal unsafe struct RenderPassDepthStencil
    {
        public ViewHandle View;
        public LoadOperation DepthLoad;
        public StoreOperation DepthStore;
        public LoadOperation StencilLoad;
        public StoreOperation StencilStore;
        public float Depth;
        public byte Stencil;
    }

    internal unsafe struct RenderPassRenderTarget
    {
        public ViewHandle View;
        public LoadOperation Load;
        public StoreOperation Store;
        public fixed float ClearValue[4];
    }

    internal unsafe struct CommandWriteConstants : ICommand
    {
        public CommandType Type => CommandType.WriteConstants;

        public /* alignment */ ulong Count;
        public WriteConstantParameters* Parameters => (WriteConstantParameters*)((uint*)Unsafe.AsPointer(ref Count) + 1);
        public WriteBufferImmediateMode* Modes => (WriteBufferImmediateMode*)(Parameters + Count);
    }

    internal struct WriteConstantParameters
    {
        public ulong Address;
        public uint Value;
    }

    internal unsafe struct CommandReadTimestamp : ICommand
    {
        public CommandType Type => CommandType.ReadTimestamp;

        internal QuerySetHandle QueryHeap;
        internal uint Index;
    }

    internal unsafe struct CommandBeginQuery : ICommand
    {
        public CommandType Type => CommandType.BeginQuery;

        internal QuerySetHandle QueryHeap;
        internal QueryType QueryType;
        internal uint Index;
    }

    internal unsafe struct CommandEndQuery : ICommand
    {
        public CommandType Type => CommandType.EndQuery;

        internal QuerySetHandle QueryHeap;
        internal QueryType QueryType;
        internal uint Index;
    }

    internal unsafe struct CommandSetPipeline : ICommand
    {
        public CommandType Type => CommandType.SetPipeline;

        internal PipelineHandle Pipeline;
    }

    // SetShadingRate,
    // SetShadingRateImage,
    // SetTopology,
    // SetStencilRef,
    // SetBlendFactor,
    // SetDepthBounds,
    // SetSamplePositions,
    // SetViewInstanceMask,

    internal unsafe struct CommandSetShadingRate : ICommand
    {
        public CommandType Type => CommandType.SetShadingRate;

        public ShadingRate BaseRate;
        public Combiner ShaderCombiner;
        public Combiner ImageCombiner;
    }

    internal unsafe struct CommandSetShadingRateImage : ICommand
    {
        public CommandType Type => CommandType.SetShadingRateImage;

        public TextureHandle ShadingRateImage;
    }

    internal unsafe struct CommandResolveQuery : ICommand
    {
        public CommandType Type => CommandType.ResolveQuery;

        public QuerySetHandle QueryHeap;
        public QueryType QueryType;
        public Range Queries;
        public BufferHandle Dest;
        public ulong Offset;
    }

    internal unsafe struct CommandSetStencilRef : ICommand
    {
        public CommandType Type => CommandType.SetStencilRef;

        public uint StencilRef;
    }

    internal unsafe struct CommandBeginConditionalRendering : ICommand
    {
        public CommandType Type => CommandType.BeginConditionalRendering;

        public BufferHandle Buffer;
        public ulong Offset;
        public bool Predicate;
    }

    internal struct SamplePosition { public sbyte X, Y; };

    internal unsafe struct CommandSetBlendFactor : ICommand
    {
        public CommandType Type => CommandType.SetBlendFactor;

        public fixed float BlendFactor[4];
    }

    internal unsafe struct CommandSetDepthBounds : ICommand
    {
        public CommandType Type => CommandType.SetDepthBounds;

        public float Max;
        public float Min;
    }

    internal unsafe struct CommandSetSamplePositions : ICommand
    {
        public CommandType Type => CommandType.SetSamplePositions;

        public uint SamplesPerPixel;
        public uint PixelCount;

        public SamplePosition* SamplePositions => (SamplePosition*)((uint*)Unsafe.AsPointer(ref PixelCount) + 1);
    }

    internal unsafe struct CommandSetViewInstanceMask : ICommand
    {
        public CommandType Type => CommandType.SetViewInstanceMask;

        public uint Mask;
    }

    internal unsafe struct CommandBindDynamicBufferDescriptor : ICommand
    {
        public CommandType Type => CommandType.BindDynamicBufferDescriptor;

        internal BindPoint BindPoint;
        internal uint ParamIndex;
        internal DynamicBufferDescriptorHandle DynamicDescriptor;
        internal uint OffsetInBytes;
    }

    internal unsafe struct CommandBindDynamicRaytracingAccelerationStructureDescriptor : ICommand
    {
        public CommandType Type => CommandType.BindDynamicRaytracingAccelerationStructureDescriptor;

        internal BindPoint BindPoint;
        internal uint ParamIndex;
        internal DynamicRaytracingAccelerationStructureDescriptorHandle DynamicDescriptor;
    }

    internal unsafe struct CommandBindDescriptors : ICommand
    {
        public CommandType Type => CommandType.BindDescriptors;

        internal BindPoint BindPoint;
        internal uint FirstSetIndex;
        internal uint SetCount;
        internal DescriptorSetHandle* Sets => (DescriptorSetHandle*)((uint*)Unsafe.AsPointer(ref SetCount) + 1);
    }

    internal unsafe struct CommandBind32BitConstants : ICommand
    {
        public CommandType Type => CommandType.Bind32BitConstants;

        internal BindPoint BindPoint;
        internal uint ParameterIndex;
        internal uint OffsetIn32BitValues;
        internal uint Num32BitValues;

        internal uint* Values => (uint*)Unsafe.AsPointer(ref Num32BitValues) + 1;
    }

    internal struct Box
    {
        public uint Left, Top, Front, Right, Bottom, Back;
    }

    internal unsafe struct CommandBufferToTextureCopy : ICommand
    {
        public CommandType Type => CommandType.BufferToTextureCopy;

        public BufferHandle Source;
        public TextureHandle Dest;
        public ulong SourceOffset;
        public DataFormat SourceFormat;
        public uint DestSubresource;
        public uint SourceWidth, SourceHeight, SourceDepth, SourceRowPitch;
        public uint DestX, DestY, DestZ;
        public bool HasBox;
        public Box* Box => (Box*)((uint*)Unsafe.AsPointer(ref HasBox) + 1);
    }

    internal struct CommandBufferCopy : ICommand
    {
        public CommandType Type => CommandType.BufferCopy;

        public BufferHandle Source, Dest;
        public ulong SourceOffset, DestOffset;
        public ulong Length;
    }

    internal unsafe struct CommandTextureCopy : ICommand
    {
        public CommandType Type => CommandType.TextureCopy;

        public TextureHandle Source, Dest;
        public uint SourceSubresource, DestSubresource;
        public uint DestX, DestY, DestZ;
        public bool HasBox;
        public Box* Box => (Box*)((uint*)Unsafe.AsPointer(ref HasBox) + 1);
    }

    [Flags]
    internal enum DepthStencilClearFlags : byte
    {
        ClearDepth = 1,
        ClearStencil = 1 << 2,
        ClearAll = ClearDepth | ClearStencil
    }

    internal unsafe struct CommandClearBuffer : ICommand
    {
        public CommandType Type => CommandType.ClearBuffer;

        public ViewHandle View;
        public DescriptorHandle Descriptor;
        public fixed float ClearValue[4];
    }

    internal unsafe struct CommandClearBufferInteger : ICommand
    {
        public CommandType Type => CommandType.ClearBufferInteger;

        public ViewHandle View;
        public DescriptorHandle Descriptor;
        public fixed uint ClearValue[4];
    }

    internal unsafe struct CommandClearDepthStencil : ICommand
    {
        public CommandType Type => CommandType.ClearDepthStencil;

        public ViewHandle View;
        public float Depth;
        public byte Stencil;
        public DepthStencilClearFlags Flags;

        public uint RectangleCount;
        internal Rectangle* Rectangles => (Rectangle*)((uint*)Unsafe.AsPointer(ref RectangleCount) + 1);
    }



    internal unsafe struct CommandClearTextureInteger : ICommand
    {
        public CommandType Type => CommandType.ClearTextureInteger;

        public ViewHandle View;
        public DescriptorHandle Descriptor;
        public fixed uint ClearValue[4];

        public uint RectangleCount;
        internal Rectangle* Rectangles => (Rectangle*)((uint*)Unsafe.AsPointer(ref RectangleCount) + 1);
    }

    internal unsafe struct CommandClearTexture : ICommand
    {
        public CommandType Type => CommandType.ClearTexture;

        public ViewHandle View;
        public DescriptorHandle Descriptor;
        public fixed float ClearValue[4];

        public uint RectangleCount;
        internal Rectangle* Rectangles => (Rectangle*)((uint*)Unsafe.AsPointer(ref RectangleCount) + 1);
    }

    internal struct CommandDraw : ICommand
    {
        public CommandType Type => CommandType.Draw;

        public uint VertexCountPerInstance;
        public uint InstanceCount;
        public uint StartVertexLocation;
        public uint StartInstanceLocation;
    }


    internal struct CommandDrawIndexed : ICommand
    {
        public CommandType Type => CommandType.DrawIndexed;

        public uint IndexCountPerInstance;
        public uint InstanceCount;
        public uint StartIndexLocation;
        public int BaseVertexLocation;
        public uint StartInstanceLocation;
    }

    internal struct CommandDispatch : ICommand
    {
        public CommandType Type => CommandType.Dispatch;

        public uint X, Y, Z;
    }

    internal struct CommandDispatchMesh : ICommand
    {
        public CommandType Type => CommandType.MeshDispatch;

        public uint X, Y, Z;
    }

    internal struct CommandVertexBufferViewArguments : ICommand
    {
        public CommandType Type => CommandType.InsertMarker;

        public ulong BufferLocation;
        public uint SizeInBytes;
        public uint StrideInBytes;
    }

    internal struct CommandIndexBufferViewArguments
    {
        public ulong BufferLocation;
        public uint SizeInBytes;
        public IndexFormat Format;
    }

    // TODO
    internal struct CommandRayTrace : ICommand
    {
        public CommandType Type => CommandType.RayTrace;

        public uint Width, Height, Depth;

        public ShaderRecord RayGeneration;
        public ShaderRecordArray HitGroup;
        public ShaderRecordArray MissShader;
        public ShaderRecordArray Callable;
    }

    public struct ShaderRecord
    {
        public BufferHandle Buffer;
        public uint Offset;
        public uint Length;
    }

    public struct ShaderRecordArray
    {
        public ShaderRecord Record;
        public uint RecordCount;
    }

    internal struct ResourceTransitionBarrier
    {
        public ResourceHandle Resource;
        public ResourceState Before;
        public ResourceState After;
        public uint Subresource;
    }
}
