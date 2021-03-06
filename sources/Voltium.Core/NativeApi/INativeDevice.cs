using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;
using TerraFX.Interop;
using Voltium.Common;
using Voltium.Common.Pix;
using Voltium.Core.Memory;
using Voltium.Core.NativeApi;
using Voltium.Core.Pipeline;
using Voltium.Core.Queries;
using static TerraFX.Interop.Windows;
using Buffer = Voltium.Core.Memory.Buffer;

namespace Voltium.Core.Devices
{
    public readonly struct DeviceInfo
    {
        public bool IsUma { get; init; }
        public bool IsCacheCoherent { get; init; }

        public bool IsCacheCoherentUma => IsCacheCoherent && IsUma;

        public bool MergedHeapSupport { get; init; }
        public ulong VirtualAddressRange { get; init; }
    }





    public enum WaitMode
    {
        WaitForAll,
        WaitForAny
    }

    public readonly unsafe struct OSEvent
    {
        private readonly IntPtr _hEvent; // HANDLE on windows. eventfd on linux

        public OSEvent(IntPtr hEvent) => _hEvent = hEvent;

        public bool IsCompleted
        {
            get
            {
                if (OperatingSystem.IsWindows())
                {
                    return WaitForSingleObject(_hEvent, 0) == WAIT_OBJECT_0;
                }
                else
                {
                    return ThrowHelper.ThrowPlatformNotSupportedException<bool>("Unknown OS");
                }
            }
        }

        public void WaitSync()
        {
            if (_hEvent == default)
            {
                return;
            }

            if (OperatingSystem.IsWindows())
            {
                WaitForSingleObject(_hEvent, INFINITE);
            }
            else if (OperatingSystem.IsLinux())
            {
                fd_set set;
                FD_SET((int)_hEvent, &set);
                Libc.select(1, &set, null, null, null);


                static void FD_SET(int n, fd_set* p)
                {
                    nint mask = (nint)(1u << (n % 32));
                    p->fds_bits[(int)((uint)n / 32)] |= mask;
                }
            }
            else if (OperatingSystem.IsLinux())
            {
                ThrowHelper.ThrowPlatformNotSupportedException("TODO");
            }
            else
            {
                ThrowHelper.ThrowPlatformNotSupportedException("Unknown OS");
            }
        }

        private struct CallbackData
        {
            public delegate*<object?, void> FnPtr;
            public IntPtr ObjectHandle;
            public IntPtr Event;
            public IntPtr WaitHandle;
        }

        public void RegisterCallback<T>(T state, delegate*<T, void> callback) where T : class?
        {
            if (_hEvent == default)
            {
                return;
            }


            if (IsCompleted)
            {
                callback(state);
                return;
            }

            if (OperatingSystem.IsWindows())
            {
                var gcHandle = GCHandle.Alloc(state);

                // see below, we store the managed object handle and fnptr target in this little block
                var context = Helpers.Alloc<CallbackData>();
                IntPtr newHandle;

                int err = RegisterWaitForSingleObject(
                    &newHandle,
                    _hEvent,
                    &CallbackWrapper,
                    context,
                    INFINITE,
                    0
                );

                if (err == 0)
                {
                    ThrowHelper.ThrowWin32Exception("RegisterWaitForSingleObject failed");
                }

                context->FnPtr = (delegate*<object?, void>)callback;
                context->ObjectHandle = GCHandle.ToIntPtr(gcHandle);
                context->Event = _hEvent;
                context->WaitHandle = newHandle;
            }
            else if (OperatingSystem.IsLinux())
            {
                ThrowHelper.ThrowPlatformNotSupportedException("TODO");
            }
            else
            {
                ThrowHelper.ThrowPlatformNotSupportedException("Unknown OS");
            }
        }

        [UnmanagedCallersOnly]
        private static void CallbackWrapper(void* pContext, byte _)
        {
            var context = (CallbackData*)pContext;

            PIXMethods.NotifyWakeFromFenceSignal(context->Event);

            // we know it takes a T which is a ref type. provided no one does something weird and hacky to invoke this method, we can safely assume it is a T
            delegate*<object?, void> fn = context->FnPtr;
            var val = GCHandle.FromIntPtr(context->ObjectHandle);

            // the user specified callback
            fn(val.Target);

            val.Free();
            Helpers.Free(context);

            // is this ok ???
            UnregisterWait(context->WaitHandle);
        }
    }

    public interface INativeDevice : IDisposable
    {
        DeviceInfo Info { get; }

        (ulong Alignment, ulong Length) GetTextureAllocationInfo(in TextureDesc desc);


        public INativeQueue CreateQueue(DeviceContext context);

        public ulong GetCompletedValue(FenceHandle fence);
        public void Wait(ReadOnlySpan<FenceHandle> fences, ReadOnlySpan<ulong> values, WaitMode mode);
        public OSEvent GetEventForWait(ReadOnlySpan<FenceHandle> fences, ReadOnlySpan<ulong> values, WaitMode mode);


        FenceHandle CreateFence(ulong initialValue, FenceFlags flags = FenceFlags.None);
        void DisposeFence(FenceHandle fence);


        HeapHandle CreateHeap(ulong size, in HeapInfo info);
        void DisposeHeap(HeapHandle handle);


        unsafe void* Map(BufferHandle handle);
        void Unmap(BufferHandle handle);

        BufferHandle AllocateBuffer(in BufferDesc desc, MemoryAccess access);
        BufferHandle AllocateBuffer(in BufferDesc desc, HeapHandle heap, ulong offset);
        void DisposeBuffer(BufferHandle handle);

        TextureHandle AllocateTexture(in TextureDesc desc, ResourceState initial);
        TextureHandle AllocateTexture(in TextureDesc desc, ResourceState initial, HeapHandle heap, ulong offset);
        void DisposeTexture(TextureHandle handle);

        RaytracingAccelerationStructureHandle AllocateRaytracingAccelerationStructure(ulong length);
        RaytracingAccelerationStructureHandle AllocateRaytracingAccelerationStructure(ulong length, HeapHandle heap, ulong offset);
        void DisposeRaytracingAccelerationStructure(RaytracingAccelerationStructureHandle handle);

        QuerySetHandle CreateQuerySet(QuerySetType type, uint length);
        void DisposeQuerySet(QuerySetHandle handle);

        RootSignatureHandle CreateRootSignature(ReadOnlySpan<RootParameter> rootParams, ReadOnlySpan<StaticSampler> samplers, RootSignatureFlags flags);
        void DisposeRootSignature(RootSignatureHandle sig);

        PipelineHandle CreatePipeline(in RootSignatureHandle rootSignature, in ComputePipelineDesc desc);
        PipelineHandle CreatePipeline(in RootSignatureHandle rootSignature, in GraphicsPipelineDesc desc);
        //PipelineHandle CreatePipeline(in RaytracingPipelineDesc desc);
        PipelineHandle CreatePipeline(in RootSignatureHandle rootSignature, in MeshPipelineDesc desc);
        void DisposePipeline(PipelineHandle handle);


        IndirectCommandHandle CreateIndirectCommand(in RootSignature rootSig, ReadOnlySpan<IndirectArgument> arguments, uint byteStride);
        IndirectCommandHandle CreateIndirectCommand(in IndirectArgument arguments, uint byteStride);


        DescriptorSetHandle CreateDescriptorSet(DescriptorType type, uint count);
        void DisposeDescriptorSet(DescriptorSetHandle handle);

        void UpdateDescriptors(ViewSetHandle views, uint firstView, DescriptorSetHandle descriptors, uint firstDescriptor, uint count);
        void CopyDescriptors(DescriptorSetHandle source, uint firstSource, DescriptorSetHandle dest, uint firstDest, uint count);

        ViewSetHandle CreateViewSet(uint viewCount);
        void DisposeViewSet(ViewSetHandle handle);

        ViewHandle CreateView(ViewSetHandle viewHeap, uint index, BufferHandle handle);
        ViewHandle CreateView(ViewSetHandle viewHeap, uint index, BufferHandle handle, in BufferViewDesc desc);
        ViewHandle CreateView(ViewSetHandle viewHeap, uint index, TextureHandle handle);
        ViewHandle CreateView(ViewSetHandle viewHeap, uint index, TextureHandle handle, in TextureViewDesc desc);
        ViewHandle CreateView(ViewSetHandle viewHeap, uint index, RaytracingAccelerationStructureHandle handle);

    }

    public enum DescriptorType
    {
        ConstantBuffer,
        DynamicConstantBuffer,

        StructuredBuffer,
        WritableStructuredBuffer,

        DynamicStructuredBuffer,
        DynamicWritableStructuredBuffer,

        TypedBuffer,
        WritableTypedBuffer,

        Texture,
        WritableTexture,

        Sampler,
        RaytracingAccelerationStructure
    }

    public enum BufferViewUsage
    {

    }

    public struct BufferViewDesc
    {
        public DataFormat Format;
        public uint Offset;
        public uint Length;
        public bool IsRaw;
    }

    public struct TextureViewDesc
    {
        public DataFormat Format;
        public ShaderComponentMapping X, Y, Z, W;
        public uint FirstMip, MipCount;
        public uint FirstArrayOrDepthSlice, ArrayOrDepthSliceCount;
        public TextureAspect Aspect;
        public float MinSampleLevelOfDetail;
    }

    public enum TextureAspect
    {
        Color,
        Depth,
        Stencil
    }

    public enum ResourceType
    {
        Texture,
        RenderTargetOrDepthStencilTexture,
        Buffer
    }

    public enum Alignment : ulong
    {
        _4KB = 4 * 1024,
        _64KB = 64 * 1024,
        _4MB = 4 * 1024 * 1024
    }

    public struct HeapInfo
    {
        public Alignment Alignment;
        public MemoryAccess Access;
        public ResourceType Type;
    }
}
