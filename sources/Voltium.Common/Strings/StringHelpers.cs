using System;
using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using TerraFX.Interop;
using Voltium.Allocators;
using Voltium.Common.HashHelper;
using Voltium.Extensions;

namespace Voltium.Common
{
    [SuppressMessage("ReSharper", "RedundantToStringCallForValueType")]
    // Represents a thin wrapper around a StringBuilder which is intended to be returned
    // returning a StringBuilder that wasn't rented is not an error
    internal struct RentedStringBuilder : IDisposable
    {
        public RentedStringBuilder(StringBuilder builder) => Value = builder;

        public StringBuilder Value { get; private set; }

        public RentedStringBuilder Clear() => new(Value.Clear());
        public RentedStringBuilder Append<T>(T obj) => new(Value.Append(obj?.ToString()));
        public RentedStringBuilder AppendLine<T>(T obj) => new(Value.AppendLine(obj?.ToString()));
        public RentedStringBuilder AppendLine() => new(Value.AppendLine());
        public RentedStringBuilder Insert<T>(int index, T obj) => new(Value.Insert(index, obj?.ToString()));

        public override string ToString() => Value.ToString();

        public void Dispose() => StringHelpers.ReturnStringBuilder(this);
    }
    internal static class StringHelpers
    {
        [ThreadStatic]
        private static StringBuilder? _perThreadBuilder;

        public static RentedStringBuilder RentStringBuilder()
        {
            // Get the thread's builder and replace it with null atomically. This could be a null value
            var threadBuilder = Interlocked.Exchange(ref _perThreadBuilder, null);

            // If it is null, we take the slow, non inlined path
            threadBuilder ??= GetNewStringBuilder();
            // Clear is very cheap so it doesn't matter we do it even when builder is new
            threadBuilder.Clear();
            return new RentedStringBuilder(threadBuilder);
        }

        public static void ReturnStringBuilder(RentedStringBuilder val)
        {
            // If the thread builder is null, we replace it with this. Else, we just discard this
            // It doesn't matter this isn't atomic, as if it overwrites another StringBuilder that is ok
            _perThreadBuilder ??= val.Value;
        }

        [MethodImpl(MethodTypes.SlowPath)]
        private static StringBuilder GetNewStringBuilder() => new StringBuilder(DefaultSize);
        private const int DefaultSize = 64;

        public static void WriteTabbingNewlines(ReadOnlySpan<char> str, int tabPerNewline = 1, TextWriter writer = null!)
        {
            writer ??= Console.Out;

            int index = str.IndexOf('\n');
            while (index != -1)
            {
                ReadOnlySpan<char> chunk = str.Slice(0, index);

                for (int i = 0; i < tabPerNewline; i++)
                {
                    writer.Write('\t');
                }

                writer.Write(chunk);

                str = str.Slice(index);
                index = str.IndexOf('\n');
            }
        }

        public unsafe static int StringLength(sbyte* p) => new Span<byte>(p, int.MaxValue).IndexOf((byte)0);
        public unsafe static int StringLength(char* p) => new Span<char>(p, int.MaxValue).IndexOf((char)0);
        public unsafe static Span<byte> ToSpan(sbyte* p) => new Span<byte>(p, StringLength(p));

        public unsafe static Span<char> ToSpan(char* p) => new Span<char>(p, StringLength(p));

        public static int FastHash(string str)
            => str is not null
                ? ArbitraryHash.HashBytes(
                    ref Unsafe.As<char, byte>(
                        ref Unsafe.Add(
                            ref Unsafe.AsRef(in str.GetPinnableReference()), -(sizeof(int) / sizeof(char)
                        ))),

                        (nuint)(sizeof(int) + (str.Length * sizeof(char)))
                )
                : 0;

        public static unsafe MemoryHandle MarshalToPinnedAscii(ReadOnlySpan<char> str)
        {
            var arr = RentedArray<byte>.Create(Encoding.ASCII.GetMaxByteCount(str.Length) + 1, PinnedArrayPool<byte>.Default);

            int len = Encoding.ASCII.GetBytes(str, arr.Value);
            arr.Value[len - 1] = 0; // null char

            return arr.CreatePinnable(underlyingArrayIsPrePinned: true).Pin();
        }

        public static unsafe sbyte* MarshalToUnmanagedAscii(ReadOnlySpan<char> str)
        {
            var len = Encoding.ASCII.GetMaxByteCount(str.Length) + 1;
            var buff = Helpers.Alloc<byte>(len);
            var encoded = Encoding.ASCII.GetBytes(str, new Span<byte>(buff, len));
            buff[encoded] = 0; // null terminate

            return (sbyte*)buff;
        }

        public static unsafe char* MarshalToUnmanaged(ReadOnlySpan<char> str)
        {
            var buff = Helpers.AllocSpan<char>(str.Length + 1);

            str.CopyTo(buff);
            buff[^1] = '\0';

            return Helpers.AddressOf(buff);
        }

        public static unsafe void FreeUnmanagedAscii(sbyte* str)
        {
            Helpers.Free(str);
        }

        public static unsafe GCHandle Pin(this string s, out char* p)
        {
            var handle = GCHandle.Alloc(s, GCHandleType.Pinned);
            p = (char*)handle.AddrOfPinnedObject();
            return handle;
        }
    }

    internal unsafe struct AsciiNativeString : IDisposable
    {
        private sbyte* _pString;
        private int _length;

        public AsciiNativeString(ReadOnlySpan<char> str)
        {
            _pString = Helpers.Alloc<sbyte>(str.Length + 1);
            Encoding.ASCII.GetBytes(str, new (_pString, str.Length));
            _pString[str.Length] = 0; // null
            _length = str.Length;
        }

        public static implicit operator sbyte*(AsciiNativeString str) => str._pString;

        public void Dispose()
        {
            Helpers.Free(_pString);
            this = default;
        }
    }

    internal unsafe struct FreeHelperAlloc : IPinnable
    {
        
        private void* Pointer;

        public FreeHelperAlloc(void* pointer)
        {
            Pointer = pointer;
        }

        public MemoryHandle Pin(int elementIndex) => new MemoryHandle(Pointer, pinnable: this);
        public void Unpin() => Helpers.Free(Pointer);
    }

    [Flags]
    internal enum ReflectionToStringFlags
    {
        Properties = 1,
        Fields = 2,

        Public = BindingFlags.Public,
        NonPublic = BindingFlags.NonPublic,

        BindingFlagsMask = Public | NonPublic,

        Default = Properties | Public
    }

}
