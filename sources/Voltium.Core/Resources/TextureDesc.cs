using System;
using TerraFX.Interop;
using Voltium.Common;
using Voltium.Core.Configuration.Graphics;
using Rectangle = System.Drawing.Rectangle;

namespace Voltium.Core.Memory
{
    /// <summary>
    /// Describes a buffer, for use by the <see cref="GraphicsAllocator"/>
    /// </summary>
    [Fluent]
    public partial struct TextureDesc
    {

        public static TextureDesc CreateShadingRateTextureDesc(uint width, uint height, uint tileSize, ResourceFlags flags = ResourceFlags.None)
        {
            return new TextureDesc
            {
                Height = (uint)Math.Ceiling((double)height / tileSize),
                Width = (uint)Math.Ceiling((double)width / tileSize),
                DepthOrArraySize = 1,
                Dimension = TextureDimension.Tex2D,
                MipCount = 1,
                Format = DataFormat.R8UInt,
                Layout = TextureLayout.Optimal,
                Msaa = MsaaDesc.None,
                ResourceFlags = flags,
                ClearValue = null,
            };
        }

        public static TextureDesc CreateShadingRateTextureDesc(uint width, uint height, ResourceFlags flags = ResourceFlags.None)
        {
            return new TextureDesc
            {
                Height = height,
                Width = width,
                DepthOrArraySize = 1,
                Dimension = TextureDimension.Tex2D,
                MipCount = 1,
                Format = DataFormat.R8UInt,
                Layout = TextureLayout.Optimal,
                Msaa = MsaaDesc.None,
                ResourceFlags = flags,
                ClearValue = null,
            };
        }

        /// <summary>
        /// Creates a new <see cref="TextureDesc"/> representing a 2D render target
        /// </summary>
        /// <param name="format">The <see cref="BackBufferFormat"/> for the render target</param>
        /// <param name="width">The width, in texels, of the target</param>
        /// <param name="height">The height, in texels, of the target</param>
        /// <param name="clearColor">The <see cref="Rgba128"/> to set to be the optimized clear value</param>
        /// <param name="msaa">Optionally, the <see cref="MsaaDesc"/> for the render target</param>
        /// <returns>A new <see cref="TextureDesc"/> representing a render target</returns>
        public static TextureDesc CreateRenderTargetDesc(BackBufferFormat format, uint width, uint height, Rgba128 clearColor, MsaaDesc msaa = default)
            => CreateRenderTargetDesc((DataFormat)format, width, height, clearColor, msaa);

        /// <summary>
        /// Creates a new <see cref="TextureDesc"/> representing a 2D render target, with no height or width, for unspecified size targets
        /// </summary>
        /// <param name="format">The <see cref="BackBufferFormat"/> for the render target</param>
        /// <param name="clearColor">The <see cref="Rgba128"/> to set to be the optimized clear value</param>
        /// <param name="msaa">Optionally, the <see cref="MsaaDesc"/> for the render target</param>
        /// <returns>A new <see cref="TextureDesc"/> representing a render target</returns>
        public static TextureDesc CreateRenderTargetDesc(BackBufferFormat format, Rgba128 clearColor, MsaaDesc msaa = default)
            => CreateRenderTargetDesc((DataFormat)format, 0, 0, clearColor, msaa);

        /// <summary>
        /// Creates a new <see cref="TextureDesc"/> representing a 2D render target, with no height or width, for unspecified size targets
        /// </summary>
        /// <param name="format">The <see cref="BackBufferFormat"/> for the render target</param>
        /// <param name="clearColor">The <see cref="Rgba128"/> to set to be the optimized clear value</param>
        /// <param name="msaa">Optionally, the <see cref="MsaaDesc"/> for the render target</param>
        /// <returns>A new <see cref="TextureDesc"/> representing a render target</returns>
        public static TextureDesc CreateRenderTargetDesc(DataFormat format, Rgba128 clearColor, MsaaDesc msaa = default)
            => CreateRenderTargetDesc(format, 0, 0, clearColor, msaa);

        /// <summary>
        /// Creates a new <see cref="TextureDesc"/> representing a 2D render target
        /// </summary>
        /// <param name="format">The <see cref="DataFormat"/> for the render target</param>
        /// <param name="width">The width, in texels, of the target</param>
        /// <param name="height">The height, in texels, of the target</param>
        /// <param name="clearColor">The <see cref="Rgba128"/> to set to be the optimized clear value</param>
        /// <param name="msaa">Optionally, the <see cref="MsaaDesc"/> for the render target</param>
        /// <returns>A new <see cref="TextureDesc"/> representing a render target</returns>
        public static TextureDesc CreateRenderTargetDesc(DataFormat format, uint width, uint height, Rgba128 clearColor, MsaaDesc msaa = default)
        {
            return new TextureDesc
            {
                Height = height,
                Width = width,
                DepthOrArraySize = 1,
                MipCount = 1,
                Dimension = TextureDimension.Tex2D,
                Format = format,
                ClearValue = TextureClearValue.CreateForRenderTarget(clearColor),
                Msaa = msaa,
                ResourceFlags = ResourceFlags.AllowRenderTarget,
                Layout = TextureLayout.Optimal
            };
        }

        /// <summary>
        /// Creates a new <see cref="TextureDesc"/> representing a 2D depth stencil, with no height or width, for unspecified size targets
        /// </summary>
        /// <param name="format">The <see cref="DataFormat"/> for the depth stencil</param>s
        /// <param name="clearDepth">The <see cref="float"/> to set to be the optimized clear value for the depth element</param>
        /// <param name="clearStencil">The <see cref="byte"/> to set to be the optimized clear value for the stencil element</param>
        /// <param name="shaderVisible">Whether the <see cref="Texture"/> is shader visible. <see langword="true"/> by default</param>
        /// <param name="msaa">Optionally, the <see cref="MsaaDesc"/> for the depth stencil</param>
        /// <returns>A new <see cref="TextureDesc"/> representing a depth stencil</returns>
        public static TextureDesc CreateDepthStencilDesc(DataFormat format, float clearDepth, byte clearStencil, bool shaderVisible = true, MsaaDesc msaa = default)
        => CreateDepthStencilDesc(format, 0, 0, clearDepth, clearStencil, shaderVisible, msaa);

        /// <summary>
        /// Creates a new <see cref="TextureDesc"/> representing a 2D depth stencil
        /// </summary>
        /// <param name="format">The <see cref="DataFormat"/> for the depth stencil</param>
        /// <param name="width">The width, in texels, of the depth stencil</param>
        /// <param name="height">The height, in texels, of the depth stencil</param>
        /// <param name="clearDepth">The <see cref="float"/> to set to be the optimized clear value for the depth element</param>
        /// <param name="clearStencil">The <see cref="byte"/> to set to be the optimized clear value for the stencil element</param>
        /// <param name="shaderVisible">Whether the <see cref="Texture"/> is shader visible. <see langword="true"/> by default</param>
        /// <param name="msaa">Optionally, the <see cref="MsaaDesc"/> for the depth stencil</param>
        /// <returns>A new <see cref="TextureDesc"/> representing a depth stencil</returns>
        public static TextureDesc CreateDepthStencilDesc(DataFormat format, uint width, uint height, float clearDepth, byte clearStencil, bool shaderVisible = true, MsaaDesc msaa = default)
        {
            return new TextureDesc
            {
                Height = height,
                Width = width,
                DepthOrArraySize = 1,
                MipCount = 1,
                Dimension = TextureDimension.Tex2D,
                Format = format,
                ClearValue = TextureClearValue.CreateForDepthStencil(clearDepth, clearStencil),
                Msaa = msaa,
                ResourceFlags = ResourceFlags.AllowDepthStencil | (shaderVisible ? 0 : ResourceFlags.DenyShaderResource),
                Layout = TextureLayout.Optimal
            };
        }


        /// <summary>
        /// Creates a new <see cref="TextureDesc"/> representing a shader resource, with no height or width, for unspecified size resources
        /// </summary>
        /// <param name="format">The <see cref="DataFormat"/> for the shader resource</param>
        /// <param name="dimension">The <see cref="TextureDimension"/> of the resource
        /// is <see cref="TextureDimension.Tex3D"/>, else, the number of textures in the array</param>
        /// <returns>A new <see cref="TextureDesc"/> representing a shader resource</returns>
        public static TextureDesc CreateShaderResourceDesc(DataFormat format, in TextureDimension dimension)
            => CreateShaderResourceDesc(format, dimension, 0, 0, 0);

        /// <summary>
        /// Creates a new <see cref="TextureDesc"/> representing a shader resource
        /// </summary>
        /// <param name="format">The <see cref="DataFormat"/> for the shader resource</param>
        /// <param name="dimension">The <see cref="TextureDimension"/> of the resource</param>
        /// <param name="height">The height, in texels, of the resource</param>
        /// <param name="width">The width, in texels, of the resource</param>
        /// <param name="depthOrArraySize">The depth, in texels, of the resource, if <paramref name="dimension"/>
        /// is <see cref="TextureDimension.Tex3D"/>, else, the number of textures in the array</param>
        /// <returns>A new <see cref="TextureDesc"/> representing a shader resource</returns>
        public static TextureDesc CreateShaderResourceDesc(DataFormat format, in TextureDimension dimension, ulong width, uint height = 1, ushort depthOrArraySize = 1)
        {
            return new TextureDesc
            {
                Height = height,
                Width = width,
                DepthOrArraySize = depthOrArraySize,
                Dimension = dimension,
                Format = format,
                Layout = TextureLayout.Optimal
            };
        }

        /// <summary>
        /// Creates a new <see cref="TextureDesc"/> representing a unordered access resource
        /// </summary>
        /// <param name="format">The <see cref="DataFormat"/> for the unordered access resource</param>
        /// <param name="dimension">The <see cref="TextureDimension"/> of the unordered access resource</param>
        /// <param name="height">The height, in texels, of the resource</param>
        /// <param name="width">The width, in texels, of the resource</param>
        /// <param name="depthOrArraySize">The depth, in texels, of the resource, if <paramref name="dimension"/>
        /// is <see cref="TextureDimension.Tex3D"/>, else, the number of textures in the array</param>
        /// <returns>A new <see cref="TextureDesc"/> representing a shader resource</returns>
        public static TextureDesc CreateUnorderedAccessResourceDesc(DataFormat format, in TextureDimension dimension, ulong width, uint height = 1, ushort depthOrArraySize = 1)
        {
            return new TextureDesc
            {
                Height = height,
                Width = width,
                DepthOrArraySize = depthOrArraySize,
                Dimension = dimension,
                Format = format,
                ResourceFlags = ResourceFlags.AllowUnorderedAccess,
                Layout = TextureLayout.Optimal
            };
        }

        /// <summary>
        /// Creates a new <see cref="TextureDesc"/> representing a unordered access resource, with no height or width, for unspecified size resources
        /// </summary>
        /// <param name="format">The <see cref="DataFormat"/> for the unordered access resource</param>
        /// <param name="dimension">The <see cref="TextureDimension"/> of the unordered access resource
        /// is <see cref="TextureDimension.Tex3D"/>, else, the number of textures in the array</param>
        /// <returns>A new <see cref="TextureDesc"/> representing a shader resource</returns>
        public static TextureDesc CreateUnorderedAccessResourceDesc(DataFormat format, in TextureDimension dimension)
            => CreateUnorderedAccessResourceDesc(format, dimension, 0, 0, 0);

        /// <summary>
        /// The format of the texture
        /// </summary>
        public DataFormat Format { get; set; }

        /// <summary>
        /// The number of dimensions in the texture
        /// </summary>
        public TextureDimension Dimension { get; set; }

        /// <summary>
        /// The number of mips this resource will contain
        /// </summary>
        public ushort MipCount { get; set; }

        /// <summary>
        /// The width, in bytes, of the texture
        /// </summary>
        public ulong Width { get; set; }

        /// <summary>
        /// The height, in bytes, of the texture
        /// </summary>
        public uint Height { get; set; }

        /// <summary>
        /// The depth, if <see cref="Dimension"/> is <see cref="TextureDimension.Tex3D"/>, else the number of elements in this texture array
        /// </summary>
        public ushort DepthOrArraySize { get; set; }

        /// <summary>
        /// If this texture is a render target or depth stencil, the value for which it is optimised to call <see cref="GraphicsContext.ClearRenderTarget(DescriptorHandle, Rgba128, Rectangle)"/>
        /// or <see cref="GraphicsContext.ClearDepthStencil(DescriptorHandle, float, byte, ReadOnlySpan{Rectangle})"/> for
        /// </summary>
        public TextureClearValue? ClearValue { get; set; }

        /// <summary>
        /// Any addition resource flags
        /// </summary>
        public ResourceFlags ResourceFlags { get; set; }

        /// <summary>
        /// Optionally, the <see cref="MsaaDesc"/> describing multi-sampling for this texture.
        /// This is only meaningful when used with a render target or depth stencil
        /// </summary>
        public MsaaDesc Msaa { get; set; }

        /// <summary>
        /// The <see cref="TextureLayout"/> for the texture
        /// </summary>
        public TextureLayout Layout { get; set; }
    }


    /// <summary>
    /// Defines the layout of a texture
    /// </summary>
    public enum TextureLayout
    {
        /// <summary>
        /// Allow the driver to determine the texture layout, meaning the entire layout is opaque to the application
        /// </summary>
        Optimal = D3D12_TEXTURE_LAYOUT.D3D12_TEXTURE_LAYOUT_UNKNOWN,

        /// <summary>
        /// Split the texture into 64kb tiles, for use with reserved resources, but keeping an optimal layout within the tiles which is opaque
        /// to the application
        /// </summary>
        Optimal64KbTile = D3D12_TEXTURE_LAYOUT.D3D12_TEXTURE_LAYOUT_64KB_UNDEFINED_SWIZZLE,

        /// <summary>
        /// Split the texture into 64kb tiles, for use with reserved resources, and use the standard-swizzle pattern so the application
        /// can understand and access the layout directly
        /// </summary>
        StandardSwizzle64KbTile = D3D12_TEXTURE_LAYOUT.D3D12_TEXTURE_LAYOUT_64KB_STANDARD_SWIZZLE
    }
}
