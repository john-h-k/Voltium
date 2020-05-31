using System;
using System.Runtime.InteropServices;

namespace Voltium.Core.Managers
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
                Value = value.ToCharArray();
                var trim = value.AsSpan().Trim();
                int count = 0;
                for (var i = 0;i < trim.Length; i++)
                {
                    if (trim[i] == ' ')
                    {
                        count++;
                    }
                }
                ArgCount = count;
            }

            internal readonly char[] Value;
            internal readonly int ArgCount;

#pragma warning disable CS1591
            public static implicit operator Flag(string val) => new Flag(val);
#pragma warning restore CS1591
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
