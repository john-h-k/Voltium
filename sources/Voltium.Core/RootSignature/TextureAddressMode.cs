using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static TerraFX.Interop.D3D12_TEXTURE_ADDRESS_MODE;

namespace Voltium.Core
{
    /// <summary>
    /// Defines the way a <see cref="Sampler"/> behaves when sampling a point outside
    /// of a texture coordinate
    /// </summary>
    public enum TextureAddressMode
    {
        /// <summary>
        /// Repeatedly tiles a texture
        /// </summary>
        Wrap = D3D12_TEXTURE_ADDRESS_MODE_WRAP,

        /// <summary>
        /// Repeatedly mirrors a texture
        /// </summary>
        Mirror = D3D12_TEXTURE_ADDRESS_MODE_MIRROR,

        /// <summary>
        /// Maps the outside coordinate to the texture value closest to it
        /// </summary>
        Clamp = D3D12_TEXTURE_ADDRESS_MODE_CLAMP,

        /// <summary>
        /// Sets the color as a user-defined color decided during creation of a <see cref="Sampler"/>
        /// </summary>
        BorderColor = D3D12_TEXTURE_ADDRESS_MODE_BORDER,

        /// <summary>
        /// Mirrors the texture around 0, equivalent to using the absolute value of the coordinate with <see cref="Clamp"/>
        /// </summary>
        MirrorOnce = D3D12_TEXTURE_ADDRESS_MODE_MIRROR_ONCE
    }
}
