using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Text;
using System.Threading.Tasks;
using TerraFX.Interop;

namespace Voltium.Common
{
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

        public static bool IsNullRef<T>(ref T val)
          => Unsafe.AreSame(ref val, ref NullRef<T>());

        public static bool IsNullIn<T>(in T val)
            => IsNullRef(ref Unsafe.AsRef(in val));

        public static bool IsNullOut<T>(out T val)
        {
            Unsafe.SkipInit(out val);
            return IsNullRef(ref val);
        }

        public static ref T NullRef<T>()
            => ref Unsafe.AsRef<T>(null);

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

            return lhs.Equals(rhs);
        }

        public static bool Int32ToBool(int val) => val != 0;
        public static int BoolToInt32(bool val) => (int)(uint)Unsafe.As<bool, byte>(ref val);

        public static T* AddressOf<T>(T[] arr) where T : unmanaged => (T*)Unsafe.AsPointer(ref MemoryMarshal.GetArrayDataReference(arr));
        public static T* AddressOf<T>(Span<T> arr) where T : unmanaged => (T*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(arr));
        public static T* AddressOf<T>(ReadOnlySpan<T> arr) where T : unmanaged => (T*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(arr));
    }
}
