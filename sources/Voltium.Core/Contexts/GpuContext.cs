using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using TerraFX.Interop;
using Voltium.Common;
using Voltium.Core.Devices;
using Voltium.Core.Memory;
using Voltium.Core.Pool;
using Buffer = Voltium.Core.Memory.Buffer;

namespace Voltium.Core
{
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class IllegalRenderPassMethodAttribute : Attribute { }
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class IllegalBundleMethodAttribute : Attribute { }

    internal enum BindPoint { Graphics, Compute }


    [GenerateEquality] internal partial struct QueryHeapHandle { public ulong Value; }
    [GenerateEquality] internal partial struct PipelineHandle { public ulong Value; }

    [GenerateEquality] internal partial struct ResourceHandle { public ulong Value; }

    [GenerateEquality] internal partial struct ViewHandle { public ulong Value; }
    [GenerateEquality] internal partial struct RenderPassHandle { public ulong Value; }

    internal enum CommandType
    {
        // Pipeline
        SetPipeline,

        // Draw state
        SetShadingRate,
        SetShadingRateImage,
        SetTopology,
        SetStencilRef,
        SetBlendFactor,
        SetDepthBounds,
        SetSamplePositions,
        SetViewInstanceMask,

        // Shader resources
        BindDescriptors,
        Bind32BitConstants,

        // Render passes
        BeginRenderPass,
        EndRenderPass,

        // Timestamps and queries
        ReadTimestamp,
        BeginQuery,
        EndQuery,

        // Conditional rendering/predication
        BeginConditionalRendering,
        EndConditionalRendering,

        // Copies
        BufferCopy,
        TextureCopy,
        BufferToTextureCopy,
        TextureToBufferCopy,

        // Clears
        ClearBuffer,
        ClearBufferInteger,
        ClearTexture,
        ClearTextureInteger,
        ClearDepthStencil,

        // Raytracing acceleration structures
        BuildAccelerationStructure,
        CopyAccelerationStructure,
        CompactAccelerationStructure,
        SerializeAccelerationStructure,
        DeserializeAccelerationStructure,

        // Execute
        ExecuteIndirect,
        Draw,
        DrawIndexed,
        Dispatch,
        RayTrace,
        MeshDispatch,
    }

    [StructLayout(LayoutKind.Explicit)]
    internal struct Command
    {
        [FieldOffset(0)]
        public CommandType Type;

        [FieldOffset(sizeof(CommandType))]
        public byte Arguments;

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

        #endregion

        #region Conditional rendering/predication

        [FieldOffset(sizeof(CommandType))]
        public CommandBeginConditionalRendering BeginConditionalRendering;

        // EndConditionalRendering has no arguments

        #endregion

        #region Draw State

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
        public CommandDraw Draw;

        [FieldOffset(sizeof(CommandType))]
        public CommandDrawIndexed DrawIndexed;

        [FieldOffset(sizeof(CommandType))]
        public CommandDispatch Dispatch;

        [FieldOffset(sizeof(CommandType))]
        public CommandDispatchMesh MeshDispatch;

        [FieldOffset(sizeof(CommandType))]
        public CommandRayTrace RayTrace;

        [FieldOffset(sizeof(CommandType))]
        public CommandBind32BitConstants Bind32BitConstants;

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

    internal unsafe struct RenderPassInfo
    {
        public LoadOperation DepthLoad;
        public StoreOperation DepthStore;
        public LoadOperation StencilLoad;
        public StoreOperation StencilStore;

        public uint RenderTargetCount;

        public LoadOperationBuffer8 RenderTargetLoad;
        public StoreOperationBuffer8 RenderTargetStore;

        [FixedBufferType(typeof(LoadOperation), 8)]
        public struct LoadOperationBuffer8 { }
        [FixedBufferType(typeof(StoreOperation), 8)]
        public struct StoreOperationBuffer8 { }
    }

    internal unsafe struct CommandBeginRenderPass
    {
        public RenderPassHandle RenderPass;
        public DepthStencil DepthStencil;
        public bool AllowTextureWrites;
        public uint RenderTargetCount;
        public RenderTarget* RenderTargets => (RenderTarget*)((uint*)Unsafe.AsPointer(ref RenderTargetCount) + 1);
    }

    internal unsafe struct DepthStencil
    {
        public ViewHandle Resource;
        public float Depth;
        public byte Stencil;
    }

    internal unsafe struct RenderTarget
    {
        public ViewHandle Resource;
        public fixed float ClearValue[4];
    }

    

    internal unsafe struct CommandReadTimestamp
    {
        internal QueryHeapHandle QueryHeap;
        internal uint Index;
    }

    internal unsafe struct CommandBeginQuery
    {
        internal QueryHeapHandle QueryHeap;
        internal QueryType Type;
        internal uint Index;
    }

    internal unsafe struct CommandEndQuery
    {
        internal QueryHeapHandle QueryHeap;
        internal QueryType Type;
        internal uint Index;
    }

    internal unsafe struct CommandSetPipeline
    {
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

    internal unsafe struct CommandSetShadingRate
    {
        public ShadingRate BaseRate;
        public Combiner ShaderCombiner;
        public Combiner ImageCombiner;
    }

    internal unsafe struct CommandSetShadingRateImage
    {
        public ResourceHandle ShadingRateImage;
    }

    internal unsafe struct CommandSetTopology
    {
        public Topology Topology;
    }

    internal unsafe struct CommandSetStencilRef
    {
        public uint StencilRef;
    }

    internal unsafe struct CommandBeginConditionalRendering
    {
        public ResourceHandle Buffer;
        public ulong Offset;
        public bool Predicate;
    }

    internal struct SamplePosition { public sbyte X, Y; };

    internal unsafe struct CommandSetBlendFactor
    {
        public fixed float BlendFactor[4];
    }

    internal unsafe struct CommandSetDepthBounds
    {
        public float Max;
        public float Min;
    }

    internal unsafe struct CommandSetSamplePositions
    {
        public uint SamplesPerPixel;
        public uint PixelCount;

        public SamplePosition* SamplePositions => (SamplePosition*)((uint*)Unsafe.AsPointer(ref PixelCount) + 1);
    }

    internal unsafe struct CommandSetViewInstanceMask
    {
        public uint Mask;
    }

    internal unsafe struct CommandBindDescriptors
    {
        internal BindPoint BindPoint;
        internal uint ParameterIndex;
    }

    internal unsafe struct CommandBind32BitConstants
    {
        internal BindPoint BindPoint;
        internal uint ParameterIndex;
        internal uint OffsetIn32BitValues;
        internal uint Num32BitValues;

        internal uint* Values => (uint*)Unsafe.AsPointer(ref Num32BitValues) + 1;
    }



    internal struct CommandBufferCopy
    {
        public ResourceHandle Source, Dest;
        public ulong SourceOffset, DestOffset;
        public ulong Length;
    }

    internal struct CommandTextureCopy
    {
        public ResourceHandle Source, Dest;
        public ulong SourceOffset, DestOffset;
        public ulong Length;
    }

    [Flags]
    internal enum DepthStencilClearFlags : byte
    {
        ClearDepth = 1,
        ClearStencil = 1 << 2,
        ClearAll = ClearDepth | ClearStencil
    }

    internal unsafe struct CommandClearBuffer
    {
        public ViewHandle View;
        public fixed float ClearValue[4];
    }

    internal unsafe struct CommandClearBufferInteger
    {
        public ViewHandle View;
        public fixed uint ClearValue[4];
    }

    internal unsafe struct CommandClearDepthStencil
    {
        public ViewHandle View;
        public float Depth;
        public byte Stencil;
        public DepthStencilClearFlags Flags;

        public uint RectangleCount;
        internal Rectangle* Rectangles => (Rectangle*)((uint*)Unsafe.AsPointer(ref RectangleCount) + 1);
    }



    internal unsafe struct CommandClearTextureInteger
    {
        public ViewHandle View;
        public fixed uint ClearValue[4];

        public uint RectangleCount;
        internal Rectangle* Rectangles => (Rectangle*)((uint*)Unsafe.AsPointer(ref RectangleCount) + 1);
    }

    internal unsafe struct CommandClearTexture
    {
        public ViewHandle View;
        public fixed float ClearValue[4];

        public uint RectangleCount;
        internal Rectangle* Rectangles => (Rectangle*)((uint*)Unsafe.AsPointer(ref RectangleCount) + 1);
    }

    internal struct CommandDraw
    {
        public uint VertexCountPerInstance;
        public uint InstanceCount;
        public uint StartVertexLocation;
        public uint StartInstanceLocation;
    }


    public struct CommandDrawIndexed
    {
        public uint IndexCountPerInstance;
        public uint InstanceCount;
        public uint StartIndexLocation;
        public int BaseVertexLocation;
        public uint StartInstanceLocation;
    }

    public struct CommandDispatch
    {
        public uint X, Y, Z;
    }

    public struct CommandDispatchMesh
    {
        public uint X, Y, Z;
    }

    public struct CommandVertexBufferViewArguments
    {
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

    /// <summary>
    /// Represents a generic Gpu context
    /// </summary>
    public unsafe partial class GpuContext : IDisposable
    {
        internal ContextParams Params;
        internal List<IDisposable?> AttachedResources { get; private set; } = new();

        internal ComputeDevice Device => Params.Device;

        internal ExecutionContext Context => Params.Context;

        private bool _closed;

        private byte[] _cmdBuff;

        internal ReadOnlySpan<byte> CommandBuffer => _cmdBuff;


        internal GpuContext(in ContextParams @params)
        {
            Params = @params;
            _cmdBuff = new();
            // We can't read past this many buffers as we skip init'ing them

            // Don't bother zero'ing expensive buffer
        }

        [VariadicGeneric("Attach(%t); %t = default!;", minNumberArgs: 1)]
        public void Attach<T0>(ref T0 t0)
            where T0 : IDisposable
        {
            Attach(t0);
            t0 = default!;
            VariadicGenericAttribute.InsertExpressionsHere();
        }

        private void Attach(IDisposable resource)
        {
            AttachedResources.Add(resource);
        }

        /// <summary>
        /// Submits this context to the device
        /// </summary>
        public virtual void Close() => Dispose();

        /// <summary>
        /// Submits this context to the device
        /// </summary>
        public virtual void Dispose()
        {
            _closed = true;
        }
    }
}
