using System.Collections.Generic;
using Voltium.Common;

namespace Voltium.Core.Devices.Shaders
{
    /// <summary>
    /// Represents the shader type and model you are compiling for
    /// </summary>
    public readonly partial struct ShaderModel
    {
        /// <summary>
        /// Creates a new <see cref="ShaderModel"/>
        /// </summary>
        public ShaderModel(ShaderType type, byte major, byte minor)
        {
            Guard.True(type.IsValid(), "ShaderType was invalid");
            Type = type;
            Major = major;
            Minor = minor;
        }

        /// <summary>
        /// The type of shader you are compiling.
        /// This is the "vs" part of "vs_6_0"
        /// </summary>
        public readonly ShaderType Type;

        /// <summary>
        /// The major version of the shader model you are compiling for.
        /// This is the "6" part of "vs_6_0"
        /// </summary>
        public readonly byte Major;

        /// <summary>
        /// The minor version of the shader model you are compiling for.
        /// This is the "0" part of "vs_6_0"
        /// </summary>
        public readonly byte Minor;

        /// <summary>
        /// Whether the current <see cref="ShaderModel"/> targets DXIL rather than DXBC, that is, targets shader model 6.0+
        /// </summary>
        public readonly bool IsDxil => Major >= 6;

        /// <summary>
        /// Creates a new <see cref="ShaderModel"/> from the current instance,
        /// but with a different <see cref="Minor"/> value
        /// </summary>
        /// <param name="minor">The new minor version value</param>
        /// <returns>A new <see cref="ShaderModel"/> from the current instance,
        /// but with a different <see cref="Minor"/> value</returns>
        public ShaderModel WithMinor(byte minor)
            => new ShaderModel(Type, Major, minor);

        /// <summary>
        /// Creates a new <see cref="ShaderModel"/> from the current instance,
        /// but with a different <see cref="Major"/> value
        /// </summary>
        /// <param name="major">The new major version value</param>
        /// <returns>A new <see cref="ShaderModel"/> from the current instance,
        /// but with a different <see cref="Major"/> value</returns>
        public ShaderModel WithMajor(byte major)
            => new ShaderModel(Type, major, Minor);

        /// <summary>
        /// Creates a new <see cref="ShaderModel"/> from the current instance,
        /// but with a different <see cref="Type"/> value
        /// </summary>
        /// <param name="type">The new <see cref="ShaderType"/></param>
        /// <returns>A new <see cref="ShaderModel"/> from the current instance,
        /// but with a different <see cref="Type"/> value</returns>
        public ShaderModel WithType(ShaderType type)
            => new ShaderModel(type, Major, Minor);

        /// <summary>
        /// Creates a new <see cref="ShaderModel"/> from the current instance,
        /// but with a different <see cref="Major"/> and <see cref="Minor"/> value
        /// </summary>
        /// <param name="major">The new major version value</param>
        /// <param name="minor">The new minor version value</param>
        /// <returns>A new <see cref="ShaderModel"/> from the current instance,
        /// but with a different <see cref="Major"/> and <see cref="Minor"/> value</returns>
        public ShaderModel WithVersion(byte major, byte minor)
            => new ShaderModel(Type, major, minor);

        /// <summary>
        /// Returns the <see cref="string"/> representation of the
        /// <see cref="ShaderModel"/>
        /// </summary>
        /// <returns>The <see cref="string"/> representation of the
        /// <see cref="ShaderModel"/></returns>
        public override string ToString()
             // e.g vs_4_0 or lib_6_3
             => $"{ShaderNameMap[Type]}_{Major}_{Minor}";

        internal static readonly Dictionary<ShaderType, string> ShaderNameMap = new()
        {
            [ShaderType.Vertex] = "vs",
            [ShaderType.Pixel] = "ps",
            [ShaderType.Domain] = "ds",
            [ShaderType.Hull] = "hs",
            [ShaderType.Geometry] = "gs",
            [ShaderType.Compute] = "cs",
            [ShaderType.Library] = "lib"
        };
    }
}
