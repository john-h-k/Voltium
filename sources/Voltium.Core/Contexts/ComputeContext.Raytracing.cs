//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace Voltium.Core.Contexts
//{
//    public partial class ComputeContext
//    {


//        /// <summary>
//        /// Updates a top-level acceleration structure from bottom-level acceleration structures
//        /// </summary>
//        /// <param name="bottomLevelGpuPointer">The GPU address of either an array of RaytracingInstanceDescs, or an array of pointers to RaytracingInstanceDescs, depending on the value of <paramref name="layout"/></param>
//        /// <param name="layout">The layout of <paramref name="bottomLevelGpuPointer"/>, either an array, or an array of pointers</param>
//        /// <param name="numBottomLevelStructures">The number of structures pointed to by <paramref name="bottomLevelGpuPointer"/></param>
//        /// <param name="source">The source <see cref="Buffer"/> of the old acceleration structure to update</param>
//        /// <param name="scratch">The scratch-space <see cref="Buffer"/> used by the driver during the update</param>
//        /// <param name="dest">The destination buffer for the updated structure. This can be the same as <paramref name="source"/> to perform an in-place update</param>
//        /// <param name="flags">The <see cref="BuildAccelerationStructureFlags"/> to apply to the update</param>
//        public void UpdateAccelerationStructure(ulong bottomLevelGpuPointer, Layout layout, uint numBottomLevelStructures, in Buffer source, in Buffer scratch, in Buffer dest, BuildAccelerationStructureFlags flags = BuildAccelerationStructureFlags.None)
//        {
//            D3D12_BUILD_RAYTRACING_ACCELERATION_STRUCTURE_DESC build = new()
//            {
//                SourceAccelerationStructureData = source.GpuAddress,
//                ScratchAccelerationStructureData = scratch.GpuAddress,
//                DestAccelerationStructureData = dest.GpuAddress,
//                Inputs = new D3D12_BUILD_RAYTRACING_ACCELERATION_STRUCTURE_INPUTS
//                {
//                    Type = D3D12_RAYTRACING_ACCELERATION_STRUCTURE_TYPE.D3D12_RAYTRACING_ACCELERATION_STRUCTURE_TYPE_TOP_LEVEL,
//                    Flags = ((D3D12_RAYTRACING_ACCELERATION_STRUCTURE_BUILD_FLAGS)flags) | D3D12_RAYTRACING_ACCELERATION_STRUCTURE_BUILD_FLAGS.D3D12_RAYTRACING_ACCELERATION_STRUCTURE_BUILD_FLAG_PERFORM_UPDATE,
//                    DescsLayout = (D3D12_ELEMENTS_LAYOUT)layout,
//                    InstanceDescs = bottomLevelGpuPointer,
//                    NumDescs = numBottomLevelStructures
//                }
//            };

//            List->BuildRaytracingAccelerationStructure(&build, 0, null);
//        }

//        /// <summary>
//        /// Builds a top-level acceleration structure from bottom-level acceleration structures
//        /// </summary>
//        /// <param name="bottomLevelGpuPointer">The GPU address of either an array of RaytracingInstanceDescs, or an array of pointers to RaytracingInstanceDescs, depending on the value of <paramref name="layout"/></param>
//        /// <param name="layout">The layout of <paramref name="bottomLevelGpuPointer"/>, either an array, or an array of pointers</param>
//        /// <param name="numBottomLevelStructures">The number of structures pointed to by <paramref name="bottomLevelGpuPointer"/></param>
//        /// <param name="scratch">The scratch-space <see cref="Buffer"/> used by the driver during the build</param>
//        /// <param name="dest">The destination buffer for the structure</param>
//        /// <param name="flags">The <see cref="BuildAccelerationStructureFlags"/> to apply to the build</param>
//        public void BuildAccelerationStructure(in Buffer bottomLevelGpuPointer, Layout layout, uint numBottomLevelStructures, in Buffer scratch, in Buffer dest, BuildAccelerationStructureFlags flags = BuildAccelerationStructureFlags.None)
//        {
//            D3D12_BUILD_RAYTRACING_ACCELERATION_STRUCTURE_DESC build = new()
//            {
//                ScratchAccelerationStructureData = scratch.GpuAddress,
//                DestAccelerationStructureData = dest.GpuAddress,
//                Inputs = new D3D12_BUILD_RAYTRACING_ACCELERATION_STRUCTURE_INPUTS
//                {
//                    Type = D3D12_RAYTRACING_ACCELERATION_STRUCTURE_TYPE.D3D12_RAYTRACING_ACCELERATION_STRUCTURE_TYPE_TOP_LEVEL,
//                    Flags = (D3D12_RAYTRACING_ACCELERATION_STRUCTURE_BUILD_FLAGS)flags,
//                    DescsLayout = (D3D12_ELEMENTS_LAYOUT)layout,
//                    InstanceDescs = bottomLevelGpuPointer.GpuAddress,
//                    NumDescs = numBottomLevelStructures
//                }
//            };

//            List->BuildRaytracingAccelerationStructure(&build, 0, null);
//        }

//        /// <inheritdoc cref="BuildAccelerationStructure(ReadOnlySpan{GeometryDesc}, in Buffer, in Buffer, BuildAccelerationStructureFlags)" />
//        public void BuildAccelerationStructure(in GeometryDesc geometry, in Buffer scratch, in Buffer dest, BuildAccelerationStructureFlags flags = BuildAccelerationStructureFlags.None)
//            => BuildAccelerationStructure(stackalloc[] { geometry }, scratch, dest, flags);

//        /// <summary>
//        /// Builds a bottom-level acceleration structure from geometry
//        /// </summary>
//        /// <param name="geometry">The <see cref="GeometryDesc"/>s to use to build the structure</param>
//        /// <param name="scratch">The scratch-space <see cref="Buffer"/> used by the driver during the build</param>
//        /// <param name="dest">The destination buffer for the structure</param>
//        /// <param name="flags">The <see cref="BuildAccelerationStructureFlags"/> to apply to the build</param>
//        public void BuildAccelerationStructure(ReadOnlySpan<GeometryDesc> geometry, in Buffer scratch, in Buffer dest, BuildAccelerationStructureFlags flags = BuildAccelerationStructureFlags.None)
//        {
//            fixed (GeometryDesc* pGeo = geometry)
//            {
//                D3D12_BUILD_RAYTRACING_ACCELERATION_STRUCTURE_DESC build = new()
//                {
//                    ScratchAccelerationStructureData = scratch.GpuAddress,
//                    DestAccelerationStructureData = dest.GpuAddress,
//                    Inputs = new D3D12_BUILD_RAYTRACING_ACCELERATION_STRUCTURE_INPUTS
//                    {
//                        Type = D3D12_RAYTRACING_ACCELERATION_STRUCTURE_TYPE.D3D12_RAYTRACING_ACCELERATION_STRUCTURE_TYPE_BOTTOM_LEVEL,
//                        Flags = (D3D12_RAYTRACING_ACCELERATION_STRUCTURE_BUILD_FLAGS)flags,
//                        DescsLayout = D3D12_ELEMENTS_LAYOUT.D3D12_ELEMENTS_LAYOUT_ARRAY,
//                        pGeometryDescs = (D3D12_RAYTRACING_GEOMETRY_DESC*)pGeo,
//                        NumDescs = (uint)geometry.Length
//                    }
//                };

//                List->BuildRaytracingAccelerationStructure(&build, 0, null);
//            }
//        }


//        /// <inheritdoc cref="UpdateAccelerationStructure(ReadOnlySpan{GeometryDesc}, in Buffer, in Buffer, in Buffer, BuildAccelerationStructureFlags)"/>
//        public void UpdateAccelerationStructure(in GeometryDesc geometry, in Buffer source, in Buffer scratch, in Buffer dest, BuildAccelerationStructureFlags flags = BuildAccelerationStructureFlags.None)
//            => UpdateAccelerationStructure(stackalloc[] { geometry }, source, scratch, dest, flags);

//        /// <summary>
//        /// Updates a bottom-level acceleration structure from geometry
//        /// </summary>
//        /// <param name="geometry">The <see cref="GeometryDesc"/>s to use to update the structure</param>
//        /// <param name="source">The source <see cref="Buffer"/> of the old acceleration structure to update</param>
//        /// <param name="scratch">The scratch-space <see cref="Buffer"/> used by the driver during the update</param>
//        /// <param name="dest">The destination buffer for the updated structure. This can be the same as <paramref name="source"/> to perform an in-place update</param>
//        /// <param name="flags">The <see cref="BuildAccelerationStructureFlags"/> to apply to the build</param>
//        public void UpdateAccelerationStructure(ReadOnlySpan<GeometryDesc> geometry, in Buffer source, in Buffer scratch, in Buffer dest, BuildAccelerationStructureFlags flags = BuildAccelerationStructureFlags.None)
//        {
//            fixed (GeometryDesc* pGeo = geometry)
//            {
//                D3D12_BUILD_RAYTRACING_ACCELERATION_STRUCTURE_DESC build = new()
//                {
//                    SourceAccelerationStructureData = source.GpuAddress,
//                    ScratchAccelerationStructureData = scratch.GpuAddress,
//                    DestAccelerationStructureData = dest.GpuAddress,
//                    Inputs = new D3D12_BUILD_RAYTRACING_ACCELERATION_STRUCTURE_INPUTS
//                    {
//                        Type = D3D12_RAYTRACING_ACCELERATION_STRUCTURE_TYPE.D3D12_RAYTRACING_ACCELERATION_STRUCTURE_TYPE_BOTTOM_LEVEL,
//                        Flags = ((D3D12_RAYTRACING_ACCELERATION_STRUCTURE_BUILD_FLAGS)flags) | D3D12_RAYTRACING_ACCELERATION_STRUCTURE_BUILD_FLAGS.D3D12_RAYTRACING_ACCELERATION_STRUCTURE_BUILD_FLAG_PERFORM_UPDATE,
//                        DescsLayout = D3D12_ELEMENTS_LAYOUT.D3D12_ELEMENTS_LAYOUT_ARRAY,
//                        pGeometryDescs = (D3D12_RAYTRACING_GEOMETRY_DESC*)pGeo,
//                        NumDescs = (uint)geometry.Length
//                    }
//                };

//                List->BuildRaytracingAccelerationStructure(&build, 0, null);
//            }
//        }
//    }
//}
