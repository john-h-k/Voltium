using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Voltium.Common.HashHelper;

namespace Voltium.Core.Objects
{
    /// <summary>
    /// The properties used to represent a material
    /// </summary>
    public struct Material : IEquatable<Material>
    {
        /// <summary>
        /// The roughness of a material. This is equivalent to 1 - the shininess of the material
        /// </summary>
        public float Roughness;

        /// <summary>
        /// 
        /// </summary>
        public Vector3 DiffuseAlbedo;

        /// <summary>
        /// 
        /// </summary>
        public Vector3 Fresnel;

        /// <inheritdoc/>
        public override bool Equals(object? obj)
            => obj is Material other && Equals(other);

        /// <inheritdoc/>
        public unsafe override int GetHashCode()
            => ArbitraryHash.HashBytes(ref Unsafe.As<Material, byte>(ref this), (nuint)sizeof(Material));

        /// <inheritdoc/>
        public bool Equals(Material other)
            => Roughness == other.Roughness && DiffuseAlbedo == other.DiffuseAlbedo && Fresnel == other.Fresnel;
    }
}
