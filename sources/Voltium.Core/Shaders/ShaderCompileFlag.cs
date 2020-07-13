using System;
using System.Linq;
using Voltium.Common;

namespace Voltium.Core.Devices
{
    /// <summary>
    /// The type used to represent a DXC flags
    /// </summary>
    [GenerateEquality]
    public readonly partial struct ShaderCompileFlag
    {
        internal ShaderCompileFlag(string value)
        {
            Value = new char[value.Length + 1]; // null char (already there)

            // Copy over the contents, replacing spaces with nulls
            var trim = value.AsSpan().Trim();
            int count = 0;
            for (var i = 0; i < trim.Length; i++)
            {
                if (trim[i] == ' ')
                {
                    Value[i] = '\0';
                    count++;
                }
                else
                {
                    Value[i] = trim[i];
                }
            }
            ArgCount = count;
        }

        internal bool IsMacro => Value.Length >= 3 && Value[0] == '-' && Value[1] == 'D' && Value[2] == '\0';

        internal bool TryDeconstructMacro(out ReadOnlySpan<char> name, out ReadOnlySpan<char> value)
        {
            if (!IsMacro)
            {
                name = default;
                value = default;
                return false;
            }

            var skipFlag = Value.AsSpan(3);

            var nullChar = skipFlag.IndexOf('\0');

            name = skipFlag.Slice(0, nullChar);

            skipFlag = skipFlag.Slice(nullChar);
            value = skipFlag.Slice(0, skipFlag.IndexOf('\0'));

            return true;
        }

        /// <inheritdoc cref="IEquatable{T}.Equals(T)"/>
        public bool Equals(ShaderCompileFlag other)
            => Value.AsSpan().SequenceEqual(other.Value);

        // the value, which is actually more like 'string[][]', or really, 'wchar**',
        // because it is several strings with null chars between them
        // this is for interop and because those strings are seperate to IDxcCompiler3
        internal readonly char[] Value;
        internal readonly int ArgCount;
    }

    ///// <summary>
    ///// Defines the optimisation level to use when compiling a shader
    ///// </summary>
    //public enum OptimisationLevel
    //{
    //    OptimizationLevel0,
    //    OptimizationLevel1,
    //    OptimizationLevel2,
    //    OptimizationLevel3,
    //    Default = OptimizationLevel3
    //}
}
