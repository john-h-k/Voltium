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

        public void Dispose() => StringHelper.ReturnStringBuilder(this);
    }
    internal static class StringHelper
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

        public static int FastHash(string str)
            => str is object
                ? ArbitraryHash.HashBytes(
                    ref Unsafe.As<char, byte>(
                        ref Unsafe.AsRef(in str.GetPinnableReference())), (nuint)(str.Length * sizeof(char)))
                : 0;

        public static unsafe MemoryHandle MarshalToUnmanagedAscii(string str)
        {
            var arr = RentedArray<byte>.Create(Encoding.ASCII.GetMaxByteCount(str.Length), PinnedArrayPool<byte>.Default);

            int len = Encoding.ASCII.GetBytes(str, arr.Value);

            return arr.CreatePinnable(underlyingArrayIsPrePinned: true).Pin();
        }
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
