using System;
using System.Runtime.InteropServices;

namespace Voltium.Core.Managers
{
    /// <summary>
    /// The flags used by <see cref="ShaderManager"/> and the DXC compilers
    /// </summary>
    public static partial class DxcCompileFlags
    {
        public readonly struct Flag
        {
            internal Flag(string value) => Value = value;
            internal readonly string Value;

            public static implicit operator Flag(string val) => new Flag(val);
        }

        public static Flag DontWarnUnusedArgs { get; } = "-Qunused-arguments";

        public static Flag AllResourcesBound { get; } = "-all-resources-bound";

        public static Flag PreferFlowControl { get; } = "-Gfp";
        public static Flag AvoidFlowControl { get; } = "-Gfa";

        public static Flag DisableValidation { get; } = "-Vd";

        public static Flag ResourcesMayAlias { get; } = "-res-may-alias";
        

        public static Flag CreateDefine(string name, string value)
            => name + "=" + value;
    }

    public enum OptimisationLevel
    {
        OptimizationLevel0,
        OptimizationLevel1,
        OptimizationLevel2,
        OptimizationLevel3,
        Default = OptimizationLevel3
    }
}
