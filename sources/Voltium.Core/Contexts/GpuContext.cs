using System;
using System.Buffers;
using System.Runtime.InteropServices;
using Voltium.Common;
using Voltium.Core.CommandBuffer;
using Voltium.Core.Memory;
using Buffer = Voltium.Core.Memory.Buffer;


namespace Voltium.Core.Contexts
{
    /// <summary>
    /// Represents a generic Gpu context
    /// </summary>
    public unsafe partial class GpuContext
    {
        private ValueList<ResourceHandle> _attachedResources;
        private bool _closed;
        private ReadOnlyMemory<byte> _closedMemory;
        private protected ContextEncoder<ValueResettableArrayBufferWriter<byte>> _encoder;
        private ContextEncoder<ValueResettableArrayBufferWriter<byte>> _closedForReset;
        private PipelineHandle? _firstPipeline;

        internal PipelineHandle? FirstPipeline => _firstPipeline;
        internal ReadOnlyValueList<ResourceHandle> AttachedResources => new(_attachedResources);
        internal ReadOnlyMemory<byte> Commands
            => _closed
                ? _closedMemory
                : ThrowHelper.ThrowInvalidOperationException<ReadOnlyMemory<byte>>("GpuContext was not closed (accessing buffer is invalid)");

        public GpuContext()
        {
            _attachedResources = new(1, ArrayPool<ResourceHandle>.Shared);
            _encoder = ContextEncoder.Create(new ValueResettableArrayBufferWriter<byte>(64));
            _closedMemory = ReadOnlyMemory<byte>.Empty;
            _firstPipeline = default;
        }

        public struct DisposableResource
        {
            internal ResourceHandle Handle;
            private DisposableResource(in ResourceHandle handle) => Handle = handle;

            public static implicit operator DisposableResource(in Buffer buff) => new(new(buff.Handle));
            public static implicit operator DisposableResource(in Texture tex) => new(new(tex.Handle));
            public static implicit operator DisposableResource(in RaytracingAccelerationStructure accelerationStructure) => new(new(accelerationStructure.Handle));
        }

        public void Attach(ReadOnlySpan<DisposableResource> arr)
        {
            _attachedResources.AddRange(MemoryMarshal.Cast<DisposableResource, ResourceHandle>(arr));
        }

        public void Attach(in DisposableResource buffer0, in DisposableResource buffer1, in DisposableResource buffer2, in DisposableResource buffer3, in DisposableResource buffer4) { Attach(buffer0); Attach(buffer1); Attach(buffer2); Attach(buffer3); Attach(buffer4); }
        public void Attach(in DisposableResource buffer0, in DisposableResource buffer1, in DisposableResource buffer2, in DisposableResource buffer3) { Attach(buffer0); Attach(buffer1); Attach(buffer2); Attach(buffer3); }
        public void Attach(in DisposableResource buffer0, in DisposableResource buffer1, in DisposableResource buffer2) { Attach(buffer0); Attach(buffer1); Attach(buffer2); }
        public void Attach(in DisposableResource buffer0, in DisposableResource buffer1) { Attach(buffer0); Attach(buffer1); }
        public void Attach(in DisposableResource buffer)
        {
            Attach(buffer.Handle);
        }

        private void Attach(in ResourceHandle resource)
        {
            _attachedResources.Add(resource);
        }

        /// <summary>
        /// Submits this context to the device
        /// </summary>
        public void Close()
        {
            _closedMemory = _encoder.Writer.WrittenMemory;
            _closedForReset = _encoder;
            _encoder = default;
            _closed = true;
        }

        public void Reset()
        {
            _closedMemory = default;
            _encoder = _closedForReset;
            _closedForReset = default;
            _encoder.Writer.Clear(false);
            _closed = false;
        }
    }
}
