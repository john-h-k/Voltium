using System;

namespace Voltium.Core.Managers
{
    /// <summary>
    /// 
    /// </summary>
    internal readonly struct ShaderCompilationOptions
    {
        public readonly D3DCompileFlags Flags;
        public readonly HlslCompilerTargetType Target;
        public readonly ReadOnlyMemory<ShaderDefine> Defines;
        public readonly ReadOnlyMemory<byte> SecondaryData;

        public ShaderCompilationOptions(
            D3DCompileFlags flags,
            HlslCompilerTargetType target,
            ReadOnlyMemory<ShaderDefine> defines = default,
            ReadOnlyMemory<byte> secondaryData = default
        )
        {
            Flags = flags;
            Target = target;
            Defines = defines;
            SecondaryData = secondaryData;
        }
    }
}
