using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Voltium.Core.Memory;

namespace Voltium.Core.Views
{
    /// <summary>
    /// Describes the metadata used to create a shader resource view to a <see cref="Texture"/>
    /// </summary>
    public struct TextureShaderResourceViewDesc
    {
        /// <summary>
        /// The <see cref="DataFormat"/> the texture will be viewed as
        /// </summary>
        public DataFormat Format;

        /// <summary>
        /// The index of the most detailed mip to use
        /// </summary>
        public uint MostDetailedMip;

        /// <summary>
        /// The number of mip levels to use, or -1 to use all available
        /// </summary>
        public uint MipLevels;

        /// <summary>
        /// The minimum LOD to clamp to
        /// </summary>
        public float ResourceMinLODClamp;

        /// <summary>
        /// For 2D views to 2D arrays or 3D textures, the index to the plane to view
        /// </summary>
        public uint PlaneSlice;
    }

    /// <summary>
    /// Describes the metadata used to create a shader resource view to a <see cref="Texture"/>
    /// </summary>
    public struct TextureUnorderedAccessViewDesc
    {
        /// <summary>
        /// The <see cref="DataFormat"/> the texture will be viewed as
        /// </summary>
        public DataFormat Format;

        /// <summary>
        /// The index of the most detailed mip to use
        /// </summary>
        public uint MipSlice;

        /// <summary>
        /// The number of mip levels to use, or -1 to use all available
        /// </summary>
        public uint MipLevels;

        /// <summary>
        /// The minimum LOD to clamp to
        /// </summary>
        public float ResourceMinLODClamp;

        /// <summary>
        /// For 2D views to 2D arrays or 3D textures, the index to the plane to view
        /// </summary>
        public uint PlaneSlice;
    }

    /// <summary>
    /// Describes the metadata used to create a render target view to a <see cref="Texture"/>
    /// </summary>
    public struct TextureRenderTargetViewDesc
    {
        /// <summary>
        /// The <see cref="DataFormat"/> the texture will be viewed as
        /// </summary>
        public DataFormat Format;

        /// <summary>
        /// The mip index to use as the render target
        /// </summary>
        public uint MipIndex;

        /// <summary>
        /// For 2D views to 2D arrays or 3D textures, the index to the plane to view
        /// </summary>
        public uint PlaneSlice;

        /// <summary>
        /// Whether the view should be multisampled
        /// </summary>
        public bool IsMultiSampled;
    }

    /// <summary>
    /// Describes the metadata used to create a depth stencil view to a <see cref="Texture"/>
    /// </summary>
    public struct TextureDepthStencilViewDesc
    {
        /// <summary>
        /// The <see cref="DataFormat"/> the texture will be viewed as
        /// </summary>
        public DataFormat Format;

        /// <summary>
        /// The mip index to use as the render target
        /// </summary>
        public uint MipIndex;

        /// <summary>
        /// For 2D views to 2D arrays or 3D textures, the index to the plane to view
        /// </summary>
        public uint PlaneSlice;

        /// <summary>
        /// Whether the view should be multisampled
        /// </summary>
        public bool IsMultiSampled;
    }
}
