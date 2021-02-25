using System;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using TerraFX.Interop;
using Voltium.Common;
using Voltium.Core.CommandBuffer;
using Voltium.Core.Configuration.Graphics;
using Voltium.Core.Contexts;
using Voltium.Core.Devices;
using Voltium.Core.Memory;

namespace Voltium.Core.Memory
{
    public readonly struct TextureHandle : IHandle<TextureHandle>
    {
        private readonly GenerationalHandle Handle;

        public TextureHandle(GenerationalHandle handle) => Handle = handle;

        public GenerationalHandle Generational => Handle;
        public TextureHandle FromGenerationHandle(GenerationalHandle handle) => new(handle);
    }

    /// <summary>
    /// Represents an in-memory texture
    /// </summary>
    public unsafe struct Texture : IDisposable
    {
        internal TextureHandle Handle;
        private Disposal<TextureHandle> _dispose;

        /// <summary>
        /// The format ofrmat format
        /// </summary>
        public readonly DataFormat Format;

        /// <summary>
        /// The number of dimensions of the texture
        /// </summary>
        public readonly TextureDimension Dimension;

        /// <summary>
        /// Whether the texture is an array of textures. This is always <see langword="false"/> if the <see cref="Dimension"/>
        /// is 3D
        /// </summary>
        public bool IsArray => Dimension != TextureDimension.Tex3D && DepthOrArraySize > 1;

        /// <summary>
        /// The width, in texels, of the texture
        /// </summary>
        public readonly ulong Width;

        /// <summary>
        /// The height, in texels, of the texture
        /// </summary>
        public readonly uint Height;

        /// <summary>
        /// The depth, in bytes, of the texture, if <see cref="Dimension"/> is <see cref="TextureDimension.Tex3D"/>,
        /// else the number of elemnts in the texture array
        /// </summary>
        public readonly ushort DepthOrArraySize;

        /// <summary>
        /// If applicable, the multisampling description for the resource
        /// </summary>
        public readonly MsaaDesc Msaa;

        internal Texture(ref TextureHandle handle, in TextureDesc desc, delegate*<ref TextureHandle, void> dispose)
        {
            // no null 😡😡😡😡😡😡😡😡😡😡😡😡😡😡😡😡😡😡😡😡😡😡😡😡😡😡😡😡😡😡😡😡😡
            Handle = handle;
            handle = default;
            _dispose = new Disposal<TextureHandle>(dispose);

            Dimension = desc.Dimension;
            Format = desc.Format;
            Width = desc.Width;
            Height = desc.Height;
            DepthOrArraySize = desc.DepthOrArraySize;
            Msaa = desc.Msaa;
        }

        /// <inheritdoc/>
        public void Dispose() => _dispose.Dispose(ref Handle);
    }

    //public static unsafe class TextureExtensions
    //{
    //    public static void WriteToSubresource<T>([RequiresResourceState(ResourceState.Common)] this in Texture texture, ReadOnlySpan<T> data, uint rowPitch, uint depthPitch, uint subresource = 0) where T : unmanaged
    //    {
    //        fixed (T* pData = data)
    //        {
    //            Guard.ThrowIfFailed(texture.GetResourcePointer()->WriteToSubresource(subresource, null, pData, rowPitch, depthPitch));
    //        }
    //    }

    //    public static void ReadFromSubresource<T>([RequiresResourceState(ResourceState.Common)] this in Texture texture, Span<T> data, uint rowPitch, uint subresource = 0) where T : unmanaged
    //    {
    //        fixed (T* pData = data)
    //        {
    //            Guard.ThrowIfFailed(texture.GetResourcePointer()->ReadFromSubresource(pData, rowPitch, (uint)data.Length, subresource, null));
    //        }
    //    }
    //}
}
