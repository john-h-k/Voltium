using System;
using System.Diagnostics;
using System.Drawing;
using TerraFX.Interop;
using Voltium.Common;
using Voltium.Core.Configuration.Graphics;
using Voltium.Core.Devices;
using Voltium.Core.Memory;

namespace Voltium.Core.Memory
{
    /// <summary>
    /// The kind of a texture
    /// </summary>
    public enum TextureKind
    {
        /// <summary>
        /// The texture is a render target
        /// </summary>
        RenderTarget = 1 << 0,

        /// <summary>
        /// The texture is a depth stencil
        /// </summary>
        DepthStencil = 1 << 1,

        /// <summary>
        /// The texture is a shader resource
        /// </summary>
        ShaderResource = 1 << 2,
    }

    /// <summary>
    /// Represents an in-memory texture
    /// </summary>
    public unsafe struct Texture : IInternalD3D12Object, IDisposable
    {
        ID3D12Object* IInternalD3D12Object.GetPointer() => _resource.GetPointer();
        private GpuResource _resource;

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

        // Tex size is not necessarily Width * Height * DepthOrArraySize. ID3D12Device::GetAllocationInfo must be called
        // to understand the real size and alignment of a given texture
        private void* _cpuAddress;
        private readonly ulong _length;

        /// <summary>
        /// The width, in texels, of the texture
        /// </summary>
        public readonly ulong Width;

        /// <summary>
        /// The height, in texels, of the texture
        /// </summary>
        public readonly uint Height;

        /// <summary>
        /// The resolution, in texels, of the texture
        /// </summary>
        public readonly Size Resolution => new Size((int)Width, (int)Height);

        /// <summary>
        /// The depth, in bytes, of the texture, if <see cref="Dimension"/> is <see cref="TextureDimension.Tex3D"/>,
        /// else the number of elemnts in the texture array
        /// </summary>
        public readonly ushort DepthOrArraySize;

        /// <summary>
        /// If applicable, the multisampling description for the resource
        /// </summary>
        public MultisamplingDesc Msaa { get; internal set; }

        internal Texture(in TextureDesc desc, GpuResource resource)
        {
            // no null 😡😡😡😡😡😡😡😡😡😡😡😡😡😡😡😡😡😡😡😡😡😡😡😡😡😡😡😡😡😡😡😡😡
            Debug.Assert(resource is not null);

            Dimension = desc.Dimension;
            Format = desc.Format;
            Width = desc.Width;
            Height = desc.Height;
            DepthOrArraySize = desc.DepthOrArraySize;
            _length = resource.Block.Size;
            _resource = resource;
            _cpuAddress = null;
            Msaa = desc.Msaa;
        }

        // Required to get Texture's from SwapChains
        internal static Texture FromResource(ComputeDevice device, ComPtr<ID3D12Resource> buffer)
        {
            var resDesc = buffer.Ptr->GetDesc();
            var res = new GpuResource(device, buffer.Move(), new InternalAllocDesc { Desc = resDesc, InitialState = D3D12_RESOURCE_STATES.D3D12_RESOURCE_STATE_COMMON }, null, -1);

            Debug.Assert(resDesc.Dimension == D3D12_RESOURCE_DIMENSION.D3D12_RESOURCE_DIMENSION_TEXTURE2D);

            var texDesc = new TextureDesc
            {
                Format = (DataFormat)resDesc.Format,
                Dimension = TextureDimension.Tex2D,
                Width = resDesc.Width,
                Height = resDesc.Height,
                DepthOrArraySize = resDesc.DepthOrArraySize,
            };

            return new Texture(texDesc, res);
        }

        internal GpuResource Resource => _resource;

        internal ID3D12Resource* GetResourcePointer() => _resource.GetResourcePointer();


        /// <inheritdoc/>
        public void Dispose() => _resource?.Dispose();
    }
}
