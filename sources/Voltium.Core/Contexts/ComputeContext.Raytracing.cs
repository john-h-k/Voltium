using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TerraFX.Interop;
using Voltium.Core.Contexts;
using Buffer = Voltium.Core.Memory.Buffer;

namespace Voltium.Core
{
    public unsafe partial class ComputeContext
    {
        /// <summary>
        /// Updates a top-level acceleration structure from bottom-level acceleration structures
        /// </summary>
        /// <param name="bottomLevelGpuPointer">The GPU address of either an array of RaytracingInstanceDescs, or an array of pointers to RaytracingInstanceDescs, depending on the value of <paramref name="layout"/></param>
        /// <param name="layout">The layout of <paramref name="bottomLevelGpuPointer"/>, either an array, or an array of pointers</param>
        /// <param name="numBottomLevelStructures">The number of structures pointed to by <paramref name="bottomLevelGpuPointer"/></param>
        /// <param name="source">The source <see cref="Buffer"/> of the old acceleration structure to update</param>
        /// <param name="scratch">The scratch-space <see cref="Buffer"/> used by the driver during the update</param>
        /// <param name="dest">The destination buffer for the updated structure. This can be the same as <paramref name="source"/> to perform an in-place update</param>
        /// <param name="flags">The <see cref="BuildAccelerationStructureFlags"/> to apply to the update</param>
        public void UpdateAccelerationStructure(
            ulong bottomLevelGpuPointer,
            Layout layout,
            uint numBottomLevelStructures,
            [RequiresResourceState(ResourceState.RayTracingAccelerationStructure)] in Buffer source,
            [RequiresResourceState(ResourceState.UnorderedAccess)] in Buffer scratch,
            [RequiresResourceState(ResourceState.RayTracingAccelerationStructure)] in Buffer dest,
            BuildAccelerationStructureFlags flags = BuildAccelerationStructureFlags.None
        )
        {
            D3D12_BUILD_RAYTRACING_ACCELERATION_STRUCTURE_DESC build = new()
            {
                SourceAccelerationStructureData = source.GpuAddress,
                ScratchAccelerationStructureData = scratch.GpuAddress,
                DestAccelerationStructureData = dest.GpuAddress,
                Inputs = new D3D12_BUILD_RAYTRACING_ACCELERATION_STRUCTURE_INPUTS
                {
                    Type = D3D12_RAYTRACING_ACCELERATION_STRUCTURE_TYPE.D3D12_RAYTRACING_ACCELERATION_STRUCTURE_TYPE_TOP_LEVEL,
                    Flags = ((D3D12_RAYTRACING_ACCELERATION_STRUCTURE_BUILD_FLAGS)flags.RemoveFlags()) | D3D12_RAYTRACING_ACCELERATION_STRUCTURE_BUILD_FLAGS.D3D12_RAYTRACING_ACCELERATION_STRUCTURE_BUILD_FLAG_PERFORM_UPDATE,
                    DescsLayout = (D3D12_ELEMENTS_LAYOUT)layout,
                    InstanceDescs = bottomLevelGpuPointer,
                    NumDescs = numBottomLevelStructures
                }
            };

            FlushBarriers();
            InsertBarrierIfNecessary(dest, flags);
            List->BuildRaytracingAccelerationStructure(&build, 0, null);
        }

        /// <summary>
        /// Builds a top-level acceleration structure from bottom-level acceleration structures
        /// </summary>
        /// <param name="bottomLevelGpuPointer">The GPU address of either an array of RaytracingInstanceDescs, or an array of pointers to RaytracingInstanceDescs, depending on the value of <paramref name="layout"/></param>
        /// <param name="layout">The layout of <paramref name="bottomLevelGpuPointer"/>, either an array, or an array of pointers</param>
        /// <param name="numBottomLevelStructures">The number of structures pointed to by <paramref name="bottomLevelGpuPointer"/></param>
        /// <param name="scratch">The scratch-space <see cref="Buffer"/> used by the driver during the build</param>
        /// <param name="dest">The destination buffer for the structure</param>
        /// <param name="flags">The <see cref="BuildAccelerationStructureFlags"/> to apply to the build</param>
        public void BuildAccelerationStructure(
            in Buffer bottomLevelGpuPointer,
            Layout layout,
            uint numBottomLevelStructures,
            [RequiresResourceState(ResourceState.UnorderedAccess)] in Buffer scratch,
            [RequiresResourceState(ResourceState.RayTracingAccelerationStructure)] in Buffer dest,
            BuildAccelerationStructureFlags flags = BuildAccelerationStructureFlags.None
        )
        {
            D3D12_BUILD_RAYTRACING_ACCELERATION_STRUCTURE_DESC build = new()
            {
                ScratchAccelerationStructureData = scratch.GpuAddress,
                DestAccelerationStructureData = dest.GpuAddress,
                Inputs = new D3D12_BUILD_RAYTRACING_ACCELERATION_STRUCTURE_INPUTS
                {
                    Type = D3D12_RAYTRACING_ACCELERATION_STRUCTURE_TYPE.D3D12_RAYTRACING_ACCELERATION_STRUCTURE_TYPE_TOP_LEVEL,
                    Flags = (D3D12_RAYTRACING_ACCELERATION_STRUCTURE_BUILD_FLAGS)flags.RemoveFlags(),
                    DescsLayout = (D3D12_ELEMENTS_LAYOUT)layout,
                    InstanceDescs = bottomLevelGpuPointer.GpuAddress,
                    NumDescs = numBottomLevelStructures
                }
            };

            InsertBarrierIfNecessary(dest, flags);
            List->BuildRaytracingAccelerationStructure(&build, 0, null);
        }

        /// <inheritdoc cref="BuildAccelerationStructure(ReadOnlySpan{GeometryDesc}, in Buffer, in Buffer, BuildAccelerationStructureFlags)" />
        public void BuildAccelerationStructure(
            in GeometryDesc geometry,
            [RequiresResourceState(ResourceState.UnorderedAccess)] in Buffer scratch,
            [RequiresResourceState(ResourceState.RayTracingAccelerationStructure)] in Buffer dest,
            BuildAccelerationStructureFlags flags = BuildAccelerationStructureFlags.None
        )
            => BuildAccelerationStructure(stackalloc[] { geometry }, scratch, dest, flags);

        /// <summary>
        /// Builds a bottom-level acceleration structure from geometry
        /// </summary>
        /// <param name="geometry">The <see cref="GeometryDesc"/>s to use to build the structure</param>
        /// <param name="scratch">The scratch-space <see cref="Buffer"/> used by the driver during the build</param>
        /// <param name="dest">The destination buffer for the structure</param>
        /// <param name="flags">The <see cref="BuildAccelerationStructureFlags"/> to apply to the build</param>
        public void BuildAccelerationStructure(
            ReadOnlySpan<GeometryDesc> geometry,
            [RequiresResourceState(ResourceState.UnorderedAccess)] in Buffer scratch,
            [RequiresResourceState(ResourceState.RayTracingAccelerationStructure)] in Buffer dest,
            BuildAccelerationStructureFlags flags = BuildAccelerationStructureFlags.None
        )
        {
            fixed (GeometryDesc* pGeo = geometry)
            {
                D3D12_BUILD_RAYTRACING_ACCELERATION_STRUCTURE_DESC build = new()
                {
                    ScratchAccelerationStructureData = scratch.GpuAddress,
                    DestAccelerationStructureData = dest.GpuAddress,
                    Inputs = new D3D12_BUILD_RAYTRACING_ACCELERATION_STRUCTURE_INPUTS
                    {
                        Type = D3D12_RAYTRACING_ACCELERATION_STRUCTURE_TYPE.D3D12_RAYTRACING_ACCELERATION_STRUCTURE_TYPE_BOTTOM_LEVEL,
                        Flags = (D3D12_RAYTRACING_ACCELERATION_STRUCTURE_BUILD_FLAGS)flags.RemoveFlags(),
                        DescsLayout = D3D12_ELEMENTS_LAYOUT.D3D12_ELEMENTS_LAYOUT_ARRAY,
                        pGeometryDescs = (D3D12_RAYTRACING_GEOMETRY_DESC*)pGeo,
                        NumDescs = (uint)geometry.Length
                    }
                };

                FlushBarriers();
                InsertBarrierIfNecessary(dest, flags);
                List->BuildRaytracingAccelerationStructure(&build, 0, null);
            }
        }


        /// <inheritdoc cref="UpdateAccelerationStructure(ReadOnlySpan{GeometryDesc}, in Buffer, in Buffer, in Buffer, BuildAccelerationStructureFlags)"/>
        public void UpdateAccelerationStructure(
            in GeometryDesc geometry,
            [RequiresResourceState(ResourceState.RayTracingAccelerationStructure)] in Buffer source,
            [RequiresResourceState(ResourceState.UnorderedAccess)] in Buffer scratch,
            [RequiresResourceState(ResourceState.RayTracingAccelerationStructure)] in Buffer dest,
            BuildAccelerationStructureFlags flags = BuildAccelerationStructureFlags.None
        )
            => UpdateAccelerationStructure(stackalloc[] { geometry }, source, scratch, dest, flags);

        /// <summary>
        /// Updates a bottom-level acceleration structure from geometry
        /// </summary>
        /// <param name="geometry">The <see cref="GeometryDesc"/>s to use to update the structure</param>
        /// <param name="source">The source <see cref="Buffer"/> of the old acceleration structure to update</param>
        /// <param name="scratch">The scratch-space <see cref="Buffer"/> used by the driver during the update</param>
        /// <param name="dest">The destination buffer for the updated structure. This can be the same as <paramref name="source"/> to perform an in-place update</param>
        /// <param name="flags">The <see cref="BuildAccelerationStructureFlags"/> to apply to the build</param>
        public void UpdateAccelerationStructure(
            ReadOnlySpan<GeometryDesc> geometry,
            [RequiresResourceState(ResourceState.RayTracingAccelerationStructure)] in Buffer source,
            [RequiresResourceState(ResourceState.UnorderedAccess)] in Buffer scratch,
            [RequiresResourceState(ResourceState.RayTracingAccelerationStructure)] in Buffer dest,
            BuildAccelerationStructureFlags flags = BuildAccelerationStructureFlags.None
        )
        {
            fixed (GeometryDesc* pGeo = geometry)
            {
                D3D12_BUILD_RAYTRACING_ACCELERATION_STRUCTURE_DESC build = new()
                {
                    SourceAccelerationStructureData = source.GpuAddress,
                    ScratchAccelerationStructureData = scratch.GpuAddress,
                    DestAccelerationStructureData = dest.GpuAddress,
                    Inputs = new D3D12_BUILD_RAYTRACING_ACCELERATION_STRUCTURE_INPUTS
                    {
                        Type = D3D12_RAYTRACING_ACCELERATION_STRUCTURE_TYPE.D3D12_RAYTRACING_ACCELERATION_STRUCTURE_TYPE_BOTTOM_LEVEL,
                        Flags = ((D3D12_RAYTRACING_ACCELERATION_STRUCTURE_BUILD_FLAGS)flags.RemoveFlags()) | D3D12_RAYTRACING_ACCELERATION_STRUCTURE_BUILD_FLAGS.D3D12_RAYTRACING_ACCELERATION_STRUCTURE_BUILD_FLAG_PERFORM_UPDATE,
                        DescsLayout = D3D12_ELEMENTS_LAYOUT.D3D12_ELEMENTS_LAYOUT_ARRAY,
                        pGeometryDescs = (D3D12_RAYTRACING_GEOMETRY_DESC*)pGeo,
                        NumDescs = (uint)geometry.Length
                    }
                };

                FlushBarriers();
                InsertBarrierIfNecessary(dest, flags);
                List->BuildRaytracingAccelerationStructure(&build, 0, null);
            }
        }


        /// <summary>
        /// Copies an acceleration structure
        /// </summary>
        /// <param name="source">The acceleration structure to copy from</param>
        /// <param name="destination">The acceleration structure to copy to</param>
        public void CopyAccelerationStructure(
            [RequiresResourceState(ResourceState.RayTracingAccelerationStructure)] in Buffer source,
            [RequiresResourceState(ResourceState.RayTracingAccelerationStructure)] in Buffer destination
        )
        {
            FlushBarriers();
            List->CopyRaytracingAccelerationStructure(source.GpuAddress, destination.GpuAddress, D3D12_RAYTRACING_ACCELERATION_STRUCTURE_COPY_MODE.D3D12_RAYTRACING_ACCELERATION_STRUCTURE_COPY_MODE_CLONE);
        }

        /// <summary>
        /// Serializes an acceleration structure into a device-opaque format.
        /// Intended for use as a debugging API, rather than for caching.
        /// Deserializing a acceleration structure is (likely) slower than rebuilding it entirely
        /// </summary>
        /// <param name="accelerationStructure">The acceleration structure to be serialized</param>
        /// <param name="serialized">The output to contain the opaque serialized data</param>
        public void SerializeAccelerationStructure(
            [RequiresResourceState(ResourceState.RayTracingAccelerationStructure)] in Buffer accelerationStructure,
            [RequiresResourceState(ResourceState.UnorderedAccess)] in Buffer serialized
        )
        {
            FlushBarriers();
            List->CopyRaytracingAccelerationStructure(serialized.GpuAddress, accelerationStructure.GpuAddress, D3D12_RAYTRACING_ACCELERATION_STRUCTURE_COPY_MODE.D3D12_RAYTRACING_ACCELERATION_STRUCTURE_COPY_MODE_SERIALIZE);
        }

        /// <summary>
        /// Deserializes a serialized acceleration structure from a device-opaque format.
        /// Intended for use as a debugging API, rather than for caching.
        /// Deserializing a acceleration structure is (likely) slower than rebuilding it entirely. 
        /// </summary>
        /// <param name="accelerationStructure">The opaque serialized data to be deserialized</param>
        /// <param name="serialized">The output to contain the acceleration structure</param>
        public void DeserializeAccelerationStructure(
            [RequiresResourceState(ResourceState.NonPixelShaderResource)] in Buffer serialized,
            [RequiresResourceState(ResourceState.RayTracingAccelerationStructure)] in Buffer accelerationStructure
        )
        {
            FlushBarriers();
            List->CopyRaytracingAccelerationStructure(accelerationStructure.GpuAddress, serialized.GpuAddress, D3D12_RAYTRACING_ACCELERATION_STRUCTURE_COPY_MODE.D3D12_RAYTRACING_ACCELERATION_STRUCTURE_COPY_MODE_DESERIALIZE);
        }

        /// <summary>
        /// Compacts an acceleration structure so it occupies less space in memory
        /// </summary>
        /// <param name="uncompacted">The acceleration structure to compact</param>
        /// <param name="compacted">The output to contain the acceleration structure</param>
        public void CompactAccelerationStructure(
            [RequiresResourceState(ResourceState.RayTracingAccelerationStructure)] in Buffer uncompacted,
            [RequiresResourceState(ResourceState.RayTracingAccelerationStructure)] in Buffer compacted
        )
        {
            FlushBarriers();
            List->CopyRaytracingAccelerationStructure(uncompacted.GpuAddress, compacted.GpuAddress, D3D12_RAYTRACING_ACCELERATION_STRUCTURE_COPY_MODE.D3D12_RAYTRACING_ACCELERATION_STRUCTURE_COPY_MODE_COMPACT);
        }

        /// <summary>
        /// Dispatches a raytracing operation
        /// </summary>
        /// <param name="desc">The <see cref="RayDispatchDesc"/> which describes the raytracing operation</param>
        public void DispatchRays(in RayDispatchDesc desc)
        {
            fixed (D3D12_DISPATCH_RAYS_DESC* pDesc = &desc.Desc)
            {
                List->DispatchRays(pDesc);
            }
        }


        private void InsertBarrierIfNecessary(in Buffer dest, BuildAccelerationStructureFlags flags)
        {
            if (flags.HasFlag(BuildAccelerationStructureFlags.InsertUavBarrier))
            {
                Barrier(ResourceBarrier.UnorderedAcccess(dest));
            }
        }
    }
}
