using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Collections.Extensions;
using TerraFX.Interop;

namespace Voltium.Common
{
    internal unsafe static class PinManager
    {

        private static Dictionary<nuint, Disposable> _map = new();

        public static void RegisterPin(void* p, in Disposable handle) => _map.Add((nuint)p, handle);
        public static void RegisterPin(void* p, delegate*<void> dispose) => RegisterPin(p, new Disposable(dispose));
        public static void RegisterPin(void* p, Action dispose) => RegisterPin(p, new Disposable(dispose));
        public static void RegisterPin(MemoryHandle handle) => _map.Add((nuint)handle.Pointer, new Disposable(handle));

        public static void RegisterPin(void* p, GCHandle handle)
            => RegisterPin(new MemoryHandle(p, handle: handle));

        public static void RegisterPin(void* p, IPinnable pinnable)
            => RegisterPin(new MemoryHandle(p, pinnable: pinnable));

        public static Disposable GetDisposable(void* p)
        {
            var h = _map[(nuint)p];
            _map.Remove((nuint)p);

            return h;
        }
    }
    internal unsafe struct Disposable
    {
        private MemoryHandle Handle;
        private delegate*<void> DisposePtr;
        private Action? DisposeFn;

        public void Dispose()
        {
            if (DisposePtr != null)
            {
                DisposePtr();
            }
            else if (DisposeFn != null)
            {
                DisposeFn();
            }
            else 
            {
                Handle.Dispose();
            }
        }
        public Disposable(MemoryHandle handle)
        {
            DisposePtr = null;
            DisposeFn = null;
            Handle = handle;
        }

        public Disposable(Action dispose)
        {
            DisposePtr = null;
            DisposeFn = dispose;
            Handle = default;
        }

        public Disposable(delegate*<void> dispose)
        {
            DisposePtr = dispose;
            DisposeFn = null;
            Handle = default;
        }
    }

    internal unsafe static class Helpers
    {
        public static IntPtr Heap { get; } = Windows.GetProcessHeap();

        public static void Copy(void* src, void* dest, int length, int destLength)
        {
            new Span<byte>(src, length).CopyTo(new Span<byte>(dest, destLength));
        }

        public static void Copy(void* src, void* dest, nint length)
        {
            new Span<byte>(src, checked((int)length)).CopyTo(new Span<byte>(dest, int.MaxValue));
        }
        public static void Copy(void* src, void* dest, nuint length)
        {
            new Span<byte>(src, checked((int)length)).CopyTo(new Span<byte>(dest, int.MaxValue));
        }

        public static void Copy<T>(ReadOnlySpan<T> source, void* dest, int destLength)
        {
            source.CopyTo(new Span<T>(dest, destLength));
        }

        public static void* Alloc(nuint size) => Windows.HeapAlloc(Heap, 0, size);
        public static void* Alloc(nint size) => Alloc((nuint)size);
        public static T* Alloc<T>(nint count = 1) where T : unmanaged => (T*)Alloc(sizeof(T) * count);


        public static Span<byte> AllocSpan(nuint size) => new Span<byte>(Alloc(size), checked((int)size));
        public static Span<byte> AllocSpan(nint size) => new Span<byte>(Alloc(size), checked((int)size));
        public static Span<T> AllocSpan<T>(nint count = 1) where T : unmanaged => new Span<T>(Alloc<T>(count), checked((int)count));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool AreSame<T, U>() => typeof(T) == typeof(U);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsPrimitive<T>()
            => AreSame<T, byte>() || AreSame<T, sbyte>() ||
                AreSame<T, ushort>() || AreSame<T, short>() ||
                AreSame<T, uint>() || AreSame<T, int>() ||
                AreSame<T, ulong>() || AreSame<T, long>() ||
                AreSame<T, float>() || AreSame<T, double>() ||
                AreSame<T, bool>() ||
                AreSame<T, char>();

        public static nint ByteOffset<T, U>(ref T origin, ref U target) => Unsafe.ByteOffset(ref origin, ref Unsafe.As<U, T>(ref target));
        public static nint Offset<T>(ref T origin, ref T target) => ByteOffset(ref origin, ref target) / Unsafe.SizeOf<T>();


        public static nint ByteOffset<T, U>(T* origin, U* target) where T : unmanaged where U : unmanaged => ByteOffset(ref *origin, ref *target);
        public static nint Offset<T>(T* origin, T* target) where T : unmanaged => ByteOffset(origin, target) / Unsafe.SizeOf<T>();

        public static void Free(void* data) => Windows.HeapFree(Heap, 0, data);

        //public static bool IsNullRef<T>(ref T val)
        //  => Unsafe.AreSame(ref val, ref NullRef<T>());

        public static bool IsNullIn<T>(in T val)
            => Unsafe.IsNullRef(ref Unsafe.AsRef(in val));

        public static bool IsNullOut<T>(out T val)
        {
            Unsafe.SkipInit(out val);
            return Unsafe.IsNullRef(ref val);
        }

        public static bool IsGuidEqual(Guid* lhs, Guid* rhs)
            => IsGuidEqual(in *lhs, in *rhs);
        public static bool IsGuidEqual(in Guid lhs, in Guid rhs)
        {
            if (Unsafe.AreSame(ref Unsafe.AsRef(in lhs), ref Unsafe.AsRef(in rhs)))
            {
                return true;
            }

            if (Sse2.IsSupported)
            {
                var comp = Sse2.CompareEqual(
                    Unsafe.As<Guid, Vector128<byte>>(ref Unsafe.AsRef(in lhs)),
                    Unsafe.As<Guid, Vector128<byte>>(ref Unsafe.AsRef(in rhs))
                );

                return Sse2.MoveMask(comp) == 0xFFFF;
            }

            return Unsafe.As<Guid, ulong>(ref Unsafe.AsRef(in lhs)) == Unsafe.As<Guid, ulong>(ref Unsafe.AsRef(in rhs)) &&
                   Unsafe.As<Guid, ulong>(ref Unsafe.AddByteOffset(ref Unsafe.AsRef(in lhs), (nint)8)) == Unsafe.As<Guid, ulong>(ref Unsafe.AddByteOffset(ref Unsafe.AsRef(in rhs), (nint)8));
        }


        public static uint Pack2x16To32(ushort lo, ushort hi) => lo | ((uint)hi << 16);
        public static (ushort Lo, ushort Hi) Unpack32To2x16(uint value) => ((ushort)value, (ushort)(value >> 16));


        public static (uint Lo32, uint Hi32) Pack2x24_16To2x32(uint lo24, uint mid24, ushort hi16) => (lo24 | (mid24 << 24), (mid24 >> 8) | ((uint)hi16 << 16));
        public static (uint Lo24, uint Mid24, ushort Hi16) Unpack2x32To2x24_16(uint lo32, uint hi32) => (lo32 & Lo24Mask, (lo32 >> 24) | ((hi32 << 8) & Lo16Mask), (ushort)(hi32 >> 16));

        public static ulong Pack2x24_16To64(uint lo24, uint mid24, ushort hi16) => lo24 | ((ulong)mid24 >> 24) | ((ulong)hi16 >> 48);
        public static (uint Lo24, uint Mid24, ushort Hi16) Unpack64To2x24_16(ulong value) => ((uint)value & Lo24Mask, (uint)(value << 24) & Lo24Mask, (ushort)(value << 48));

        private const uint Lo24Mask = 0b0000_0000__1111_1111__1111_1111__1111_1111;
        private const uint Lo16Mask = 0b0000_0000__0000_0000__1111_1111__1111_1111;
        private const uint Lo8Mask = 0b0000_0000__0000_0000__0000_0000__1111_1111;

        public static uint SizeOf<T>() => (uint)Unsafe.SizeOf<T>();

        // for type inference
        public static uint SizeOf<T>(T val) => (uint)Unsafe.SizeOf<T>();


        public static unsafe T* MarshalToUnmanaged<T>(ReadOnlySpan<T> str) where T : unmanaged
        {
            var buff = AllocSpan<T>(str.Length + 1);

            str.CopyTo(buff);

            return AddressOf(buff);
        }

        public static ulong LargeIntegerToUInt64(in LARGE_INTEGER li) => ((ulong)li.HighPart << 32) | li.LowPart;

        public static bool Int32ToBool(int val) => val != 0;
        public static int BoolToInt32(bool val) => (int)(uint)Unsafe.As<bool, byte>(ref val);

        public static T* AddressOf<T>(T[] arr) where T : unmanaged => (T*)Unsafe.AsPointer(ref MemoryMarshal.GetArrayDataReference(arr));
        public static T* AddressOf<T>(Span<T> arr) where T : unmanaged => (T*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(arr));
        public static T* AddressOf<T>(ReadOnlySpan<T> arr) where T : unmanaged => (T*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(arr));
    }
}
