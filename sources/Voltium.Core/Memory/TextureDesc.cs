using System;
using Voltium.Core.Configuration.Graphics;
using Rectangle = System.Drawing.Rectangle;

namespace Voltium.Core.Memory
{
    /// <summary>
    /// Describes a buffer, for use by the <see cref="GpuAllocator"/>
    /// </summary>
    public struct TextureDesc
    {
        /// <summary>
        /// Creates a new <see cref="TextureDesc"/> representing a 2D render target
        /// </summary>
        /// <param name="format">The <see cref="BackBufferFormat"/> for the render target</param>
        /// <param name="width">The width, in texels, of the target</param>
        /// <param name="height">The height, in texels, of the target</param>
        /// <param name="clearColor">The <see cref="Rgba128"/> to set to be the optimized clear value</param>
        /// <param name="msaa">Optionally, the <see cref="MultisamplingDesc"/> for the render target</param>
        /// <returns>A new <see cref="TextureDesc"/> representing a render target</returns>
        public static TextureDesc CreateRenderTargetDesc(BackBufferFormat format, uint width, uint height, Rgba128 clearColor, MultisamplingDesc msaa = default)
            => CreateRenderTargetDesc((DataFormat)format, width, height, clearColor, msaa);

        /// <summary>
        /// Creates a new <see cref="TextureDesc"/> representing a 2D render target, with no height or width, for unspecified size targets
        /// </summary>
        /// <param name="format">The <see cref="BackBufferFormat"/> for the render target</param>
        /// <param name="clearColor">The <see cref="Rgba128"/> to set to be the optimized clear value</param>
        /// <param name="msaa">Optionally, the <see cref="MultisamplingDesc"/> for the render target</param>
        /// <returns>A new <see cref="TextureDesc"/> representing a render target</returns>
        public static TextureDesc CreateRenderTargetDesc(BackBufferFormat format, Rgba128 clearColor, MultisamplingDesc msaa = default)
            => CreateRenderTargetDesc((DataFormat)format, 0, 0, clearColor, msaa);

        /// <summary>
        /// Creates a new <see cref="TextureDesc"/> representing a 2D render target, with no height or width, for unspecified size targets
        /// </summary>
        /// <param name="format">The <see cref="BackBufferFormat"/> for the render target</param>
        /// <param name="clearColor">The <see cref="Rgba128"/> to set to be the optimized clear value</param>
        /// <param name="msaa">Optionally, the <see cref="MultisamplingDesc"/> for the render target</param>
        /// <returns>A new <see cref="TextureDesc"/> representing a render target</returns>
        public static TextureDesc CreateRenderTargetDesc(DataFormat format, Rgba128 clearColor, MultisamplingDesc msaa = default)
            => CreateRenderTargetDesc(format, 0, 0, clearColor, msaa);

        /// <summary>
        /// Creates a new <see cref="TextureDesc"/> representing a 2D render target
        /// </summary>
        /// <param name="format">The <see cref="DataFormat"/> for the render target</param>
        /// <param name="width">The width, in texels, of the target</param>
        /// <param name="height">The height, in texels, of the target</param>
        /// <param name="clearColor">The <see cref="Rgba128"/> to set to be the optimized clear value</param>
        /// <param name="msaa">Optionally, the <see cref="MultisamplingDesc"/> for the render target</param>
        /// <returns>A new <see cref="TextureDesc"/> representing a render target</returns>
        public static TextureDesc CreateRenderTargetDesc(DataFormat format, uint width, uint height, Rgba128 clearColor, MultisamplingDesc msaa = default)
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
            };
        }

        /// <summary>
        /// Creates a new <see cref="TextureDesc"/> representing a 2D depth stencil, with no height or width, for unspecified size targets
        /// </summary>
        /// <param name="format">The <see cref="DataFormat"/> for the depth stencil</param>s
        /// <param name="clearDepth">The <see cref="float"/> to set to be the optimized clear value for the depth element</param>
        /// <param name="clearStencil">The <see cref="byte"/> to set to be the optimized clear value for the stencil element</param>
        /// <param name="shaderVisible">Whether the <see cref="Texture"/> is shader visible. <see langword="true"/> by default</param>
        /// <param name="msaa">Optionally, the <see cref="MultisamplingDesc"/> for the depth stencil</param>
        /// <returns>A new <see cref="TextureDesc"/> representing a depth stencil</returns>
        public static TextureDesc CreateDepthStencilDesc(DataFormat format, float clearDepth, byte clearStencil, bool shaderVisible = true, MultisamplingDesc msaa = default)
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
        /// <param name="msaa">Optionally, the <see cref="MultisamplingDesc"/> for the depth stencil</param>
        /// <returns>A new <see cref="TextureDesc"/> representing a depth stencil</returns>
        public static TextureDesc CreateDepthStencilDesc(DataFormat format, uint width, uint height, float clearDepth, byte clearStencil, bool shaderVisible = true, MultisamplingDesc msaa = default)
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
                ResourceFlags = ResourceFlags.AllowUnorderedAccess
            };
        }

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
        /// Optionally, the <see cref="MultisamplingDesc"/> describing multi-sampling for this texture.
        /// This is only meaningful when used with a render target or depth stencil
        /// </summary>
        public MultisamplingDesc Msaa { get; set; }
    }
}
