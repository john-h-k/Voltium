using System;
using System.Runtime.CompilerServices;
using TerraFX.Interop;
using Voltium.Core.Devices;
using Voltium.Common;

using ResourceType = Voltium.Core.Devices.ResourceType;
using SysDebug = System.Diagnostics.Debug;

namespace Voltium.Core.Memory
{
    public unsafe sealed class GraphicsAllocator : ComputeAllocator
    {
        private GraphicsDevice _device;

        /// <summary>
        /// Creates a new allocator
        /// </summary>
        /// <param name="device">The <see cref="ID3D12Device"/> to allocate on</param>
        internal GraphicsAllocator(GraphicsDevice device) : base(device)
        {
            SysDebug.Assert(device is not null);

            _device = device;
            _hasMergedHeapSupport = device.Info.MergedHeapSupport;

            _4kbTextures = new();
            _64kbTextures = new();
            _4mbTextures = new();
            if (!_hasMergedHeapSupport)
            {
                _64kbRtOrDs = new();
                _4mbRtOrDs = new();
            }

            _accelerationStructureHeap = new();
        }

        /// <summary>
        /// Allocates a buffer for use as a raytracing acceleration structure
        /// </summary>
        /// <param name="info">The <see cref="AccelerationStructureBuildInfo"/> containing the required sizes of the buffers</param>
        /// <param name="scratch">On return, this is filled with a <see cref="Buffer"/> with a large anough size to be used as the scratch buffer in a raytracing acceleration structure build</param>
        /// <returns>A <see cref="RaytracingAccelerationStructure"/> with a large anough size to be used as the destination in a raytracing acceleration structure build</returns>
        public RaytracingAccelerationStructure AllocateRaytracingAccelerationBuffer(in AccelerationStructureBuildInfo info, out Buffer scratch)
            => AllocateRaytracingAccelerationBuffer(info, AllocFlags.None, out scratch);

        /// <summary>
        /// Allocates a buffer for use as a raytracing acceleration structure
        /// </summary>
        /// <param name="info">The <see cref="AccelerationStructureBuildInfo"/> containing the required sizes of the buffers</param>
        /// <param name="scratch">On return, this is filled with a <see cref="Buffer"/> with a large anough size to be used as the scratch buffer in a raytracing acceleration structure build</param>
        /// <param name="allocFlags">Any additional allocation flags</param>
        /// <returns>A <see cref="RaytracingAccelerationStructure"/> with a large anough size to be used as the destination in a raytracing acceleration structure build</returns>
        public RaytracingAccelerationStructure AllocateRaytracingAccelerationBuffer(in AccelerationStructureBuildInfo info, AllocFlags allocFlags, out Buffer scratch)
        {
            scratch = AllocateBuffer(info.ScratchSize, MemoryAccess.GpuOnly, ResourceFlags.AllowUnorderedAccess | ResourceFlags.DenyShaderResource, allocFlags);
            return AllocateRaytracingAccelerationBuffer(info.DestSize, allocFlags);
        }

        /// <summary>
        /// Allocates a buffer for use as a raytracing acceleration structure or scratch buffer for a raytracing acceleration structure build
        /// </summary>
        /// <param name="length">The length, in bytes, to allocate</param>
        /// <param name="allocFlags">Any additional allocation flags</param>
        /// <returns>A <see cref="RaytracingAccelerationStructure"/></returns>
        public RaytracingAccelerationStructure AllocateRaytracingAccelerationBuffer(ulong length, AllocFlags allocFlags = AllocFlags.None)
        {
            InternalAllocDesc allocDesc = default;
            CreateAllocDesc(
                length,
                &allocDesc,
                allocFlags
            );

            if (TryGetFreeBlock(&allocDesc, out var heap, out var index, out var offset))
            {
                var info = new AllocationInfo
                {
                    HeapIndex = index,
                    HeapInfo = heap.Info,
                    Length = allocDesc.Size,
                    Alignment = allocDesc.Alignment,
                    Offset = offset
                };

                var buffer = _device.AllocateRaytracingAccelerationStructure(length, heap, offset, new(this, &_Return));
                SetAllocationInfo(buffer.Handle, info);
                return buffer;
            }
            else
            {
                return _device.AllocateRaytracingAccelerationStructure(length);
            }
        }


        /// <summary>
        /// Allocates a texture
        /// </summary>
        /// <param name="alias">The buffer to alias</param>
        /// <param name="desc">The <see cref="TextureDesc"/> describing the texture</param>
        /// <param name="initialResourceState">The state of the resource when it is allocated</param>
        /// <param name="allocFlags">Any additional allocation flags</param>
        /// <returns>A new <see cref="Texture"/></returns>
        public Texture AllocateTextureAliasing(
            in Buffer alias,
            in TextureDesc desc,
            ResourceState initialResourceState,
            AllocFlags allocFlags = AllocFlags.None
        )
        {
            var info = GetAllocationInfo(alias.Handle);
            if (info.HasImplicitHeap)
            {
                ThrowHelper.ThrowArgumentException("Cannot alias with a committed resource");
            }

            var heap = GetHeapPool(info.HeapInfo)[(int)info.HeapIndex].Heap;

            return _device.AllocateTexture(desc, initialResourceState, heap, info.Offset);
        }


        /// <summary>
        /// Allocates a texture
        /// </summary>
        /// <param name="alias">The texture to alias</param>
        /// <param name="desc">The <see cref="TextureDesc"/> describing the texture</param>
        /// <param name="initialResourceState">The state of the resource when it is allocated</param>
        /// <param name="allocFlags">Any additional allocation flags</param>
        /// <returns>A new <see cref="Texture"/></returns>
        public Texture AllocateTextureAliasing(
            in Texture alias,
            in TextureDesc desc,
            ResourceState initialResourceState,
            AllocFlags allocFlags = AllocFlags.None
        )
        {
            var info = GetAllocationInfo(alias.Handle);
            if (info.HasImplicitHeap)
            {
                ThrowHelper.ThrowArgumentException("Cannot alias with a committed resource");
            }

            var heap = GetHeapPool(info.HeapInfo)[(int)info.HeapIndex].Heap;

            return _device.AllocateTexture(desc, initialResourceState, heap, info.Offset);
        }

        /// <summary>
        /// Allocates a texture
        /// </summary>
        /// <param name="desc">The <see cref="TextureDesc"/> describing the texture</param>
        /// <param name="initialResourceState">The state of the resource when it is allocated</param>
        /// <param name="allocFlags">Any additional allocation flags</param>
        /// <returns>A new <see cref="Texture"/></returns>
        public Texture AllocateTexture(
            in TextureDesc desc,
            ResourceState initialResourceState,
            AllocFlags allocFlags = AllocFlags.None
        )
        {
            InternalAllocDesc allocDesc = default;
            CreateAllocDesc(desc, &allocDesc, initialResourceState, allocFlags);

            if (TryGetFreeBlock(&allocDesc, out var heap, out var index, out var offset))
            {
                var info = new AllocationInfo
                {
                    HeapIndex = index,
                    HeapInfo = heap.Info,
                    Length = allocDesc.Size,
                    Alignment = allocDesc.Alignment,
                    Offset = offset
                };

                var tex = _device.AllocateTexture(desc, initialResourceState, heap, offset, new(this, &_Return));
                SetAllocationInfo(tex.Handle, info);
                return tex;
            }
            else
            {
                return _device.AllocateTexture(desc, initialResourceState);
            }
        }

        internal void CreateAllocDesc(
            in TextureDesc desc,
            InternalAllocDesc* pDesc,
            ResourceState initialResourceState,
            AllocFlags allocFlags
        )
        {
            var (alignment, size) = _device.GetAllocationInfo(desc);

            var heapInfo = new HeapInfo
            {
                Alignment = (Alignment)alignment,
                Access = MemoryAccess.GpuOnly,
                Type = IsRenderTargetOrDepthStencil(desc.ResourceFlags) ? ResourceType.RenderTargetOrDepthStencilTexture : ResourceType.Texture
            };

            *pDesc = new InternalAllocDesc
            {
                Desc = new (desc),
                InitialState = initialResourceState,
                AllocFlags = allocFlags,
                Alignment = alignment,
                Size = size,
                HeapInfo = heapInfo,
                Flags = desc.ResourceFlags
            };
        }
    }
}
