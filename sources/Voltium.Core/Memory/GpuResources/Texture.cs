using System;
using System.Diagnostics;
using TerraFX.Interop;
using Voltium.Common;
using Voltium.Core.GpuResources;

namespace Voltium.Core.Memory.GpuResources
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
    public unsafe struct Texture : IDisposable
    {
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
        /// The width, in bytes, of the texture
        /// </summary>
        public readonly ulong Width;

        /// <summary>
        /// The height, in bytes, of the texture
        /// </summary>
        public readonly uint Height;

        /// <summary>
        /// The depth, in bytes, of the texture, if <see cref="Dimension"/> is <see cref="TextureDimension.Tex3D"/>,
        /// else the number of elemnts in the texture array
        /// </summary>
        public readonly ushort DepthOrArraySize;

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
        }


        // I don't like how this needs knowledge of DXGI. Should probably rewrite
        internal static Texture FromBackBuffer(IDXGISwapChain* swapChain, uint bufferIndex)
        {
            using ComPtr<ID3D12Resource> buffer = default;
            Guard.ThrowIfFailed(swapChain->GetBuffer(bufferIndex, buffer.Guid, ComPtr.GetVoidAddressOf(&buffer)));

            DXGI_SWAP_CHAIN_DESC desc;
            Guard.ThrowIfFailed(swapChain->GetDesc(&desc));
            var bufferDesc = desc.BufferDesc;

            var resDesc = buffer.Get()->GetDesc();

            var res = new GpuResource(buffer.Move(), new InternalAllocDesc { Desc = resDesc, InitialState = D3D12_RESOURCE_STATES.D3D12_RESOURCE_STATE_COMMON });
            DirectXHelpers.SetObjectName(res, $"BackBuffer #{bufferIndex}");

            var texDesc = new TextureDesc
            {
                Format = (DataFormat)bufferDesc.Format,
                Dimension = TextureDimension.Tex2D,
                Width = bufferDesc.Width,
                Height = bufferDesc.Height,
                DepthOrArraySize = 1,
            };

            return new Texture(texDesc, res);
        }

        /// <summary>
        /// The texture data. This may be empty if the data is not CPU writable.
        /// This data requires understanding of the GPU's texture layout and must be written and read with
        /// care.
        /// </summary>
        public Span<byte> Data
        {
            get
            {
                if (_cpuAddress == null)
                {
                    _cpuAddress = _resource.Map(0);
                }

                return new Span<byte>(_cpuAddress, (int)_length);
            }
        }

        /// <summary>
        /// The debug name of the texture
        /// </summary>
        public string Name
        {
            set => DirectXHelpers.SetObjectName(_resource, value);
            get => DirectXHelpers.GetObjectName(_resource);
        }

        internal GpuResource Resource => _resource;

        /// <summary>
        /// Do not use
        /// </summary>
        /// <returns></returns>
        public ID3D12Resource* GetResourcePointer() => _resource.UnderlyingResource;

        private GpuResource _resource;

        /// <inheritdoc/>
        public void Dispose() => _resource?.Dispose();
    }
}
