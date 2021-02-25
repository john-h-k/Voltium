using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using TerraFX.Interop;
using Voltium.Core.Memory;
using Voltium.Core.Pipeline;
using Voltium.Core.Queries;

namespace Voltium.Core.CommandBuffer
{
    public readonly struct GenerationalHandle
    {
        public readonly uint Generation;
        public readonly uint Id;

        public ulong AsUInt64() => Generation | (Id >> 32);

        public GenerationalHandle(uint generation, uint handle)
        {
            Generation = generation;
            Id = handle;
        }
    }

    public interface IHandle<THandle> where THandle : struct, IHandle<THandle>
    {   
        public GenerationalHandle Generational { get; }

        public THandle FromGenerationHandle(GenerationalHandle handle);
    }

    public struct View
    {
        internal ViewHandle Handle;
        internal ViewSetHandle Set;
        internal uint Index;
        private Disposal<ViewHandle> _dispose;

        public void Dispose() => _dispose.Dispose(ref Handle);
    }

    public struct RenderPass { }


    public enum BindPoint { Graphics, Compute }

    [AttributeUsage(AttributeTargets.Field)]
    public sealed class CommandInfoAttribute : Attribute
    {
        public CommandInfoAttribute(RequiredQueueType type, CommandFlags flags)
        {

        }
    }

    public enum RequiredQueueType
    {
        Copy = 1,
        Compute = 2,
        Graphics = 4
    }

    public enum CommandFlags
    {
        AllowedInRenderPass = 1,
        AllowedInBundle = 2,
        DispatchesWork = 4
    }

    public enum CommandType : ulong
    {
        // Debug/profiling
        [CommandInfo(RequiredQueueType.Copy, CommandFlags.AllowedInBundle | CommandFlags.AllowedInRenderPass)]
        InsertMarker,

        [CommandInfo(RequiredQueueType.Copy, CommandFlags.AllowedInBundle | CommandFlags.AllowedInRenderPass)]
        BeginEvent,

        [CommandInfo(RequiredQueueType.Copy, CommandFlags.AllowedInBundle | CommandFlags.AllowedInRenderPass)]
        EndEvent,

        // Barriers
        [CommandInfo(RequiredQueueType.Copy, CommandFlags.AllowedInBundle | CommandFlags.AllowedInRenderPass | CommandFlags.DispatchesWork)]
        Transition,

        [CommandInfo(RequiredQueueType.Copy, CommandFlags.AllowedInBundle | CommandFlags.AllowedInRenderPass | CommandFlags.DispatchesWork)]
        WriteBarrier,

        [CommandInfo(RequiredQueueType.Copy, CommandFlags.AllowedInBundle | CommandFlags.AllowedInRenderPass | CommandFlags.DispatchesWork)]
        AliasingBarrier,

        // Pipeline
        [CommandInfo(RequiredQueueType.Compute, CommandFlags.AllowedInBundle | CommandFlags.AllowedInRenderPass | CommandFlags.DispatchesWork)]
        SetPipeline,

        // Draw state
        [CommandInfo(RequiredQueueType.Graphics, CommandFlags.AllowedInBundle | CommandFlags.AllowedInRenderPass | CommandFlags.DispatchesWork)]
        SetIndexBuffer,

        [CommandInfo(RequiredQueueType.Graphics, CommandFlags.AllowedInBundle | CommandFlags.AllowedInRenderPass | CommandFlags.DispatchesWork)]
        SetVertexBuffer,

        [CommandInfo(RequiredQueueType.Graphics, CommandFlags.AllowedInBundle | CommandFlags.AllowedInRenderPass | CommandFlags.DispatchesWork)]
        SetViewports,

        [CommandInfo(RequiredQueueType.Graphics, CommandFlags.AllowedInBundle | CommandFlags.AllowedInRenderPass | CommandFlags.DispatchesWork)]
        SetScissorRectangles,

        [CommandInfo(RequiredQueueType.Graphics, CommandFlags.AllowedInBundle | CommandFlags.AllowedInRenderPass | CommandFlags.DispatchesWork)]
        SetShadingRate,

        [CommandInfo(RequiredQueueType.Graphics, CommandFlags.AllowedInBundle | CommandFlags.AllowedInRenderPass | CommandFlags.DispatchesWork)]
        SetShadingRateImage,

        [CommandInfo(RequiredQueueType.Graphics, CommandFlags.AllowedInBundle | CommandFlags.AllowedInRenderPass | CommandFlags.DispatchesWork)]
        SetTopology,

        [CommandInfo(RequiredQueueType.Graphics, CommandFlags.AllowedInBundle | CommandFlags.AllowedInRenderPass | CommandFlags.DispatchesWork)]
        SetStencilRef,

        [CommandInfo(RequiredQueueType.Graphics, CommandFlags.AllowedInBundle | CommandFlags.AllowedInRenderPass | CommandFlags.DispatchesWork)]
        SetBlendFactor,

        [CommandInfo(RequiredQueueType.Graphics, CommandFlags.AllowedInBundle | CommandFlags.AllowedInRenderPass | CommandFlags.DispatchesWork)]
        SetDepthBounds,

        [CommandInfo(RequiredQueueType.Graphics, CommandFlags.AllowedInBundle | CommandFlags.AllowedInRenderPass | CommandFlags.DispatchesWork)]
        SetSamplePositions,

        [CommandInfo(RequiredQueueType.Graphics, CommandFlags.AllowedInBundle | CommandFlags.AllowedInRenderPass | CommandFlags.DispatchesWork)]
        SetViewInstanceMask,


        // Shader resources
        BindVirtualAddress,
        BindDescriptors,
        Bind32BitConstants,

        // Render passes
        BeginRenderPass,
        EndRenderPass,

        // Timestamps and queries
        ReadTimestamp,
        BeginQuery,
        EndQuery,
        ResolveQuery,

        // Conditional rendering/predication
        BeginConditionalRendering,
        EndConditionalRendering,

        // Copies
        BufferCopy,
        TextureCopy,
        BufferToTextureCopy,
        TextureToBufferCopy,
        WriteConstants,

        // Clears
        ClearBuffer,
        ClearBufferInteger,
        ClearTexture,
        ClearTextureInteger,
        ClearDepthStencil,

        // Raytracing acceleration structuresS
        BuildAccelerationStructure,
        CopyAccelerationStructure,
        CompactAccelerationStructure,
        SerializeAccelerationStructure,
        DeserializeAccelerationStructure,

        // Execute
        [CommandInfo(RequiredQueueType.Graphics, CommandFlags.AllowedInBundle | CommandFlags.AllowedInRenderPass | CommandFlags.DispatchesWork)]
        ExecuteIndirect,

        [CommandInfo(RequiredQueueType.Graphics, CommandFlags.AllowedInBundle | CommandFlags.AllowedInRenderPass | CommandFlags.DispatchesWork)]
        Draw,

        [CommandInfo(RequiredQueueType.Graphics, CommandFlags.AllowedInBundle | CommandFlags.AllowedInRenderPass | CommandFlags.DispatchesWork)]
        DrawIndexed,

        [CommandInfo(RequiredQueueType.Compute, CommandFlags.AllowedInBundle | CommandFlags.DispatchesWork)]
        Dispatch,

        [CommandInfo(RequiredQueueType.Compute, CommandFlags.AllowedInBundle | CommandFlags.DispatchesWork)]
        RayTrace,

        [CommandInfo(RequiredQueueType.Graphics, CommandFlags.AllowedInBundle | CommandFlags.AllowedInRenderPass | CommandFlags.DispatchesWork)]
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
        public CommandBindVirtualAddress BindVirtualAddress;

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
        public CommandSetTopology SetTopology;

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

    public enum LoadOperation
    {
        Discard = D3D12_RENDER_PASS_BEGINNING_ACCESS_TYPE.D3D12_RENDER_PASS_BEGINNING_ACCESS_TYPE_DISCARD,
        Clear = D3D12_RENDER_PASS_BEGINNING_ACCESS_TYPE.D3D12_RENDER_PASS_BEGINNING_ACCESS_TYPE_CLEAR,
        Preserve = D3D12_RENDER_PASS_BEGINNING_ACCESS_TYPE.D3D12_RENDER_PASS_BEGINNING_ACCESS_TYPE_PRESERVE,
        NoAccess = D3D12_RENDER_PASS_BEGINNING_ACCESS_TYPE.D3D12_RENDER_PASS_BEGINNING_ACCESS_TYPE_NO_ACCESS,
    }
    public enum StoreOperation
    {
        Discard = D3D12_RENDER_PASS_ENDING_ACCESS_TYPE.D3D12_RENDER_PASS_ENDING_ACCESS_TYPE_DISCARD,
        Resolve = D3D12_RENDER_PASS_ENDING_ACCESS_TYPE.D3D12_RENDER_PASS_ENDING_ACCESS_TYPE_RESOLVE,
        Preserve = D3D12_RENDER_PASS_ENDING_ACCESS_TYPE.D3D12_RENDER_PASS_ENDING_ACCESS_TYPE_PRESERVE,
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

    public struct CountSpecifier
    {
        public BufferHandle CountBuffer;
        public uint Offset;
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
        public CommandType Type => CommandType.SetIndexBuffer;

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
    internal unsafe struct ResourceHandle
    {
        [FieldOffset(0)]
        public ResourceHandleType Type;

        [FieldOffset(sizeof(ResourceHandleType))]
        public BufferHandle Buffer;
        [FieldOffset(sizeof(ResourceHandleType))]
        public TextureHandle Texture;
        [FieldOffset(sizeof(ResourceHandleType))]
        public RaytracingAccelerationStructureHandle RaytracingAccelerationStructure;
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
        public (ResourceHandle Resource, ResourceState Before, ResourceState After, uint Subresource) * Transitions
            => ((ResourceHandle, ResourceState, ResourceState, uint) *)((uint*)Unsafe.AsPointer(ref Count) + 1);
    }

    internal unsafe struct CommandAliasingBarrier : ICommand
    {
        public CommandType Type => CommandType.AliasingBarrier;

        public uint Count;
        public (ResourceHandle Before, ResourceHandle After) * Resources => ((ResourceHandle, ResourceHandle) *)((uint*)Unsafe.AsPointer(ref Count) + 1);
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

    public unsafe struct RenderPassDepthStencil
    {
        public ViewHandle View;
        public LoadOperation DepthLoad;
        public StoreOperation DepthStore;
        public LoadOperation StencilLoad;
        public StoreOperation StencilStore;
        public float Depth;
        public byte Stencil;
    }

    public unsafe struct RenderPassRenderTarget
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

    internal unsafe struct CommandSetTopology : ICommand
    {
        public CommandType Type => CommandType.SetTopology;

        public Topology Topology;
    }



    internal unsafe struct CommandSetPatchListCount : ICommand
    {
        public CommandType Type => CommandType.SetTopology;

        public byte PatchListCount;
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

    internal unsafe struct CommandBindVirtualAddress : ICommand
    {
        public CommandType Type => CommandType.BindVirtualAddress;

        internal BindPoint BindPoint;
        internal uint ParamIndex;
        internal ulong VirtualAddress;
    }


    internal unsafe struct CommandBindDescriptors : ICommand
    {
        public CommandType Type => CommandType.BindDescriptors;

        internal BindPoint BindPoint;
        internal uint FirstSetIndex;
        internal uint SetCount;
        internal DescriptorHandle* Sets => (DescriptorHandle*)((uint*)Unsafe.AsPointer(ref SetCount) + 1);
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

    public struct Box
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

    public struct CommandVertexBufferViewArguments : ICommand
    {
        public CommandType Type => CommandType.InsertMarker;

        public ulong BufferLocation;
        public uint SizeInBytes;
        public uint StrideInBytes;
    }

    public struct CommandIndexBufferViewArguments
    {
        public ulong BufferLocation;
        public uint SizeInBytes;
        public IndexFormat Format;
    }

    // TODO
    public struct CommandRayTrace
    {
        //private RayDispatchDesc Desc;
    }
}
