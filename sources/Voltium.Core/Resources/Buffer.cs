using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using TerraFX.Interop;
using Voltium.Common;
using Voltium.Core.Devices;
using Voltium.Core.Memory;
using Voltium.Core.NativeApi;

namespace Voltium.Core.Memory
{
    public unsafe struct Disposal<T>
    {
        public Disposal(object state, delegate*<object, ref T, void> ptr)
        {
            State = state;
            Hack.Value = 0;
            Hack.FnPtr = (delegate*<object, ref byte, void>)ptr;
        }

        private NotGenericHack Hack;
        private object State;

        public void Dispose(ref T val)
        {
            var dispose = (delegate*<object, ref T, void>)Interlocked.Exchange(ref Hack.Value, (nint)0);
            if (dispose != null)
            {
                dispose(State, ref val);
            }
        }
    }

    [StructLayout(LayoutKind.Explicit)]
    internal unsafe struct NotGenericHack
    {
        [FieldOffset(0)]
        public nint Value;
        [FieldOffset(0)]
        public delegate*<object, ref byte, void> FnPtr;
    }

    /// <summary>
    /// Represents an untyped buffer of GPU data
    /// </summary>
    public unsafe struct Buffer : IDisposable
    {
        /// <summary>
        /// The size, in bytes, of the buffer
        /// </summary>
        public readonly ulong Length;

        public readonly ulong LengthAs<T>() => Length / (uint)Unsafe.SizeOf<T>();

        internal BufferHandle Handle;
        private Disposal<BufferHandle> _dispose;
        private void* _address;
        private ulong _gpuAddress;

        public ulong GpuAddress => _gpuAddress;
        public void* Address => _address;
        public T* As<T>(int offset = 0) where T : unmanaged => (T*)_address + offset;
        public ref T AsRef<T>() where T : unmanaged => ref *(T*)_address;
        public Span<byte> AsSpan() => AsSpan<byte>();
        public Span<byte> AsSpan(int offset) => AsSpan<byte>(offset);
        public Span<byte> AsSpan(int offset, int length) => AsSpan<byte>(offset, length);
        public Span<T> AsSpan<T>() where T : unmanaged => Address is null ? Span<T>.Empty : new(Address, checked((int)LengthAs<T>()));
        public Span<T> AsSpan<T>(int offset) where T : unmanaged => Address is null ? Span<T>.Empty : new((T*)Address + offset, checked((int)LengthAs<T>()) - offset);
        public Span<T> AsSpan<T>(int offset, int length) where T : unmanaged => Address is null ? Span<T>.Empty : new((T*)Address + offset, length);

        public Buffer(ulong length, ulong gpuAddress, void* address, BufferHandle handle, Disposal<BufferHandle> dispose)
        {
            Length = length;
            Handle = handle;
            _gpuAddress = gpuAddress;
            _address = address;
            _dispose = dispose;
        }

        public void SetName(string s) { }

        /// <inheritdoc/>
        public void Dispose() => _dispose.Dispose(ref Handle);
    }

}
