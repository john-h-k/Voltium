using System;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using TerraFX.Interop;
using Voltium.Common;
using Voltium.Core.Configuration.Graphics;
using Voltium.Core.Contexts;
using Voltium.Core.Devices;
using Voltium.Core.Memory;

namespace Voltium.Core.Memory
{
    public interface IGpuDisposable// : IDisposable
    {
        internal IDisposable Disposable { get; }

        public void Dispose(in GpuTask disposeAfterTask);
    }

    /// <summary>
    /// Represents an in-memory texture
    /// </summary>
    public unsafe struct Texture : IEvictable, IInternalD3D12Object, IDisposable
    {
        bool IEvictable.IsBlittableToPointer => false;
        ID3D12Pageable* IEvictable.GetPageable() => ((IEvictable)_resource).GetPageable();
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
        public readonly MsaaDesc Msaa;

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
            Msaa = desc.Msaa;
        }

        // Required to get Texture's from SwapChains
        internal static Texture FromResource(ComputeDevice device, UniqueComPtr<ID3D12Resource> buffer)
        {
            var resDesc = buffer.Ptr->GetDesc();
            var desc = new InternalAllocDesc { Desc = resDesc, InitialState = D3D12_RESOURCE_STATES.D3D12_RESOURCE_STATE_COMMON };
            var res = new GpuResource(device, buffer.Move(), &desc, null, -1);

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

        internal readonly GpuResource Resource => _resource;

        internal readonly ID3D12Resource* GetResourcePointer() => _resource.GetResourcePointer();


        /// <inheritdoc/>
        public void Dispose()
        {
            _resource?.Dispose();
            _resource = null!;
        }


        /// <inheritdoc/>
        public void Dispose(in GpuTask disposeAfter)
        {
            static void _Dispose(GpuResource resource) => resource.Dispose();

            disposeAfter.RegisterCallback(_resource, &_Dispose);

            _resource = null!;
        }

    }

    public static unsafe class TextureExtensions
    {
        public static void WriteToSubresource<T>([RequiresResourceState(ResourceState.Common)] this in Texture texture, ReadOnlySpan<T> data, uint rowPitch, uint depthPitch, uint subresource = 0) where T : unmanaged
        {
            fixed (T* pData = data)
            {
                Guard.ThrowIfFailed(texture.GetResourcePointer()->WriteToSubresource(subresource, null, pData, rowPitch, depthPitch));
            }
        }

        public static void ReadFromSubresource<T>([RequiresResourceState(ResourceState.Common)] this in Texture texture, Span<T> data, uint rowPitch, uint subresource = 0) where T : unmanaged
        {
            fixed (T* pData = data)
            {
                Guard.ThrowIfFailed(texture.GetResourcePointer()->ReadFromSubresource(pData, rowPitch, (uint)data.Length, subresource, null));
            }
        }
    }

    // /// <summary>
    // /// Represents an in-memory texture
    // /// </summary>
    // // public unsafe class CTexture : MemoryMananager<byte>, IEvictable, IInternalD3D12Object, IDisposable
    // {
    //     bool IEvictable.IsBlittableToPointer => false;
    //     ID3D12Pageable* IEvictable.GetPageable() => ((IEvictable)_resource).GetPageable();
    //     ID3D12Object* IInternalD3D12Object.GetPointer() => _resource.GetPointer();
    //     private GpuResource _resource;
    //
    //     /// <summary>
    //     /// The format ofrmat format
    //     /// </summary>
    //     public readonly DataFormat Format;
    //
    //     /// <summary>
    //     /// The number of dimensions of the texture
    //     /// </summary>
    //     public readonly TextureDimension Dimension;
    //
    //     /// <summary>
    //     /// Whether the texture is an array of textures. This is always <see langword="false"/> if the <see cref="Dimension"/>
    //     /// is 3D
    //     /// </summary>
    //     public bool IsArray => Dimension != TextureDimension.Tex3D && DepthOrArraySize > 1;
    //
    //     // Tex size is not necessarily Width * Height * DepthOrArraySize. ID3D12Device::GetAllocationInfo must be called
    //     // to understand the real size and alignment of a given texture
    //     private readonly ulong _length;
    //
    //     /// <summary>
    //     /// The width, in texels, of the texture
    //     /// </summary>
    //     public readonly ulong Width;
    //
    //     /// <summary>
    //     /// The height, in texels, of the texture
    //     /// </summary>
    //     public readonly uint Height;
    //
    //     /// <summary>
    //     /// The resolution, in texels, of the texture
    //     /// </summary>
    //     public Size Resolution => new Size((int)Width, (int)Height);
    //
    //     /// <summary>
    //     /// The depth, in bytes, of the texture, if <see cref="Dimension"/> is <see cref="TextureDimension.Tex3D"/>,
    //     /// else the number of elemnts in the texture array
    //     /// </summary>
    //     public readonly ushort DepthOrArraySize;
    //
    //     /// <summary>
    //     /// If applicable, the multisampling description for the resource
    //     /// </summary>
    //     public readonly MsaaDesc Msaa;
    //
    //     internal CTexture(in TextureDesc desc, GpuResource resource)
    //     {
    //         // no null 😡😡😡😡😡😡😡😡😡😡😡😡😡😡😡😡😡😡😡😡😡😡😡😡😡😡😡😡😡😡😡😡😡
    //         Debug.Assert(resource is not null);
    //
    //         Dimension = desc.Dimension;
    //         Format = desc.Format;
    //         Width = desc.Width;
    //         Height = desc.Height;
    //         DepthOrArraySize = desc.DepthOrArraySize;
    //         _length = resource.Block.Size;
    //         _resource = resource;
    //         Msaa = desc.Msaa;
    //     }
    //
    //     // Required to get Texture's from SwapChains
    //     internal static CTexture FromResource(ComputeDevice device, UniqueComPtr<ID3D12Resource> buffer)
    //     {
    //         var resDesc = buffer.Ptr->GetDesc();
    //         var desc = new InternalAllocDesc { Desc = resDesc, InitialState = D3D12_RESOURCE_STATES.D3D12_RESOURCE_STATE_COMMON };
    //         var res = new GpuResource(device, buffer.Move(), &desc, null, -1);
    //
    //         Debug.Assert(resDesc.Dimension == D3D12_RESOURCE_DIMENSION.D3D12_RESOURCE_DIMENSION_TEXTURE2D);
    //
    //         var texDesc = new TextureDesc
    //         {
    //             Format = (DataFormat)resDesc.Format,
    //             Dimension = TextureDimension.Tex2D,
    //             Width = resDesc.Width,
    //             Height = resDesc.Height,
    //             DepthOrArraySize = resDesc.DepthOrArraySize,
    //         };
    //
    //         return new CTexture(texDesc, res);
    //     }
    //
    //     internal GpuResource Resource => _resource;
    //
    //     internal ID3D12Resource* GetResourcePointer() => _resource.GetResourcePointer();
    //
    //
    //     /// <inheritdoc/>
    //     public void Dispose()
    //     {
    //         _resource?.Dispose();
    //         _resource = null!;
    //     }
    //
    //
    //     /// <inheritdoc/>
    //     public void Dispose(in GpuTask disposeAfter)
    //     {
    //         static void _Dispose(GpuResource resource) => resource.Dispose();
    //
    //         disposeAfter.RegisterCallback(_resource, &_Dispose);
    //
    //         _resource = null!;
    //     }
    //
    // }
}
