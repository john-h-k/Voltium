using System;

namespace Voltium.Core.Devices
{
    /// <summary>
    /// The flags used by <see cref="ShaderManager"/> and the DXC compilers
    /// </summary>
    public static partial class DxcCompileFlags
    {
        /// <summary>
        /// The type used to represent a DXC flags
        /// </summary>
        public readonly struct Flag
        {
            internal Flag(string value)
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

            // the value, which is actually more like 'string[][]', or really, 'wchar**',
            // because it is several strings with null chars between them
            // this is for interop and because those strings are seperate to IDxcCompiler3
            internal readonly char[] Value;
            internal readonly int ArgCount;
        }
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
