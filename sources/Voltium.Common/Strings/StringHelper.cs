using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
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

        public StringBuilder Clear() => Value.Clear();
        public StringBuilder Append<T>(T obj) => Value.Append(obj?.ToString());
        public StringBuilder AppendLine<T>(T obj) => Value.AppendLine(obj?.ToString());
        public StringBuilder Insert<T>(int index, T obj) => Value.Insert(index, obj?.ToString());

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

        [MethodImplAttribute(MethodTypes.SlowPath)]
        private static StringBuilder GetNewStringBuilder() => new StringBuilder(DefaultSize);
        private const int DefaultSize = 64;

        public static string DefaultToString<T>(T @this, ReflectionToStringFlags flags = ReflectionToStringFlags.Default)
        {
            if (@this is null)
            {
                return "null";
            }

            using RentedStringBuilder builder = RentStringBuilder();

            BindingFlags bindingFlags = (BindingFlags)(flags & ReflectionToStringFlags.BindingFlagsMask) | BindingFlags.Instance;
            if (flags.HasFlag(ReflectionToStringFlags.Properties))
            {
                foreach (PropertyInfo prop in @this.GetType().GetProperties(bindingFlags))
                {
                    builder.AppendLine($"{prop.Name}: {prop.GetValue(@this)}");
                }
            }
            else if (flags.HasFlag(ReflectionToStringFlags.Fields))
            {
                foreach (FieldInfo field in @this.GetType().GetFields(bindingFlags))
                {
                    builder.AppendLine($"{field.Name}: {field.GetValue(@this)}");
                }
            }

            return builder.ToString();
        }

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
