//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using TerraFX.Interop;

//namespace Voltium.Core.Devices
//{
//    public unsafe partial class ComputeDevice
//    {
//        /// <inheritdoc cref="GetBuildInfo(ReadOnlySpan{GeometryDesc}, BuildAccelerationStructureFlags)"/>
//        public AccelerationStructureBuildInfo GetBuildInfo(in GeometryDesc geometry, BuildAccelerationStructureFlags flags = BuildAccelerationStructureFlags.None)
//            => GetBuildInfo(stackalloc[] { geometry }, flags);

//        /// <summary>
//        /// Calculates <see cref="AccelerationStructureBuildInfo"/> for a bottom-level acceleration structure build
//        /// </summary>
//        /// <param name="geometry">The <see cref="GeometryDesc"/> to build the acceleration structure from</param>
//        /// <param name="flags">The <see cref="BuildAccelerationStructureFlags"/> to apply to the build info generation</param>
//        /// <returns>An <see cref="AccelerationStructureBuildInfo"/> describing the requirements for building the acceleration structure</returns>
//        public AccelerationStructureBuildInfo GetBuildInfo(ReadOnlySpan<GeometryDesc> geometry, BuildAccelerationStructureFlags flags = BuildAccelerationStructureFlags.None)
//        {
//            fixed (GeometryDesc* pGeo = geometry)
//            {
//                var inputs = new D3D12_BUILD_RAYTRACING_ACCELERATION_STRUCTURE_INPUTS
//                {
//                    Type = D3D12_RAYTRACING_ACCELERATION_STRUCTURE_TYPE.D3D12_RAYTRACING_ACCELERATION_STRUCTURE_TYPE_BOTTOM_LEVEL,
//                    Flags = (D3D12_RAYTRACING_ACCELERATION_STRUCTURE_BUILD_FLAGS)flags,
//                    DescsLayout = D3D12_ELEMENTS_LAYOUT.D3D12_ELEMENTS_LAYOUT_ARRAY,
//                    pGeometryDescs = (D3D12_RAYTRACING_GEOMETRY_DESC*)pGeo,
//                    NumDescs = (uint)geometry.Length
//                };

//                D3D12_RAYTRACING_ACCELERATION_STRUCTURE_PREBUILD_INFO info;
//                GetAccelerationStructuredPrebuildInfo(&inputs, &info);

//                return new AccelerationStructureBuildInfo { ScratchSize = info.ScratchDataSizeInBytes, DestSize = info.ResultDataMaxSizeInBytes };
//            }
//        }

//        /// <summary>
//        /// Calculates <see cref="AccelerationStructureBuildInfo"/> for a top-level acceleration structure build
//        /// </summary>
//        /// <param name="layout">The <see cref="Layout"/> used by the top-level acceleration structure input</param>
//        /// <param name="numBottomLevelStructures">The number of bottom-level acceleration structures in the input</param>
//        /// <param name="flags">The <see cref="BuildAccelerationStructureFlags"/> to apply to the build info generation</param>
//        /// <returns>An <see cref="AccelerationStructureBuildInfo"/> describing the requirements for building the acceleration structure</returns>
//        public AccelerationStructureBuildInfo GetBuildInfo(Layout layout, uint numBottomLevelStructures, BuildAccelerationStructureFlags flags = BuildAccelerationStructureFlags.None)
//        {
//            var inputs = new D3D12_BUILD_RAYTRACING_ACCELERATION_STRUCTURE_INPUTS
//            {
//                Type = D3D12_RAYTRACING_ACCELERATION_STRUCTURE_TYPE.D3D12_RAYTRACING_ACCELERATION_STRUCTURE_TYPE_TOP_LEVEL,
//                Flags = (D3D12_RAYTRACING_ACCELERATION_STRUCTURE_BUILD_FLAGS)flags,
//                DescsLayout = (D3D12_ELEMENTS_LAYOUT)layout,
//                InstanceDescs = 1,
//                NumDescs = numBottomLevelStructures
//            };

//            D3D12_RAYTRACING_ACCELERATION_STRUCTURE_PREBUILD_INFO info;
//            GetAccelerationStructuredPrebuildInfo(&inputs, &info);

//            return new AccelerationStructureBuildInfo { ScratchSize = info.ScratchDataSizeInBytes, DestSize = info.ResultDataMaxSizeInBytes };
//        }

//        /// <inheritdoc cref="GetUpdateInfo(ReadOnlySpan{GeometryDesc}, BuildAccelerationStructureFlags)"/>
//        public AccelerationStructureBuildInfo GetUpdateInfo(in GeometryDesc geometry, BuildAccelerationStructureFlags flags = BuildAccelerationStructureFlags.None)
//            => GetBuildInfo(stackalloc[] { geometry }, flags);

//        /// <summary>
//        /// Calculates <see cref="AccelerationStructureBuildInfo"/> for a bottom-level acceleration structure update
//        /// </summary>
//        /// <param name="geometry">The <see cref="GeometryDesc"/> to update the acceleration structure with</param>
//        /// <param name="flags">The <see cref="BuildAccelerationStructureFlags"/> to apply to the update info generation</param>
//        /// <returns>An <see cref="AccelerationStructureBuildInfo"/> describing the requirements for updating the acceleration structure</returns>
//        public AccelerationStructureBuildInfo GetUpdateInfo(ReadOnlySpan<GeometryDesc> geometry, BuildAccelerationStructureFlags flags = BuildAccelerationStructureFlags.None)
//        {
//            fixed (GeometryDesc* pGeo = geometry)
//            {
//                var inputs = new D3D12_BUILD_RAYTRACING_ACCELERATION_STRUCTURE_INPUTS
//                {
//                    Type = D3D12_RAYTRACING_ACCELERATION_STRUCTURE_TYPE.D3D12_RAYTRACING_ACCELERATION_STRUCTURE_TYPE_BOTTOM_LEVEL,
//                    Flags = (D3D12_RAYTRACING_ACCELERATION_STRUCTURE_BUILD_FLAGS)flags | D3D12_RAYTRACING_ACCELERATION_STRUCTURE_BUILD_FLAGS.D3D12_RAYTRACING_ACCELERATION_STRUCTURE_BUILD_FLAG_PERFORM_UPDATE,
//                    DescsLayout = D3D12_ELEMENTS_LAYOUT.D3D12_ELEMENTS_LAYOUT_ARRAY,
//                    pGeometryDescs = (D3D12_RAYTRACING_GEOMETRY_DESC*)pGeo,
//                    NumDescs = (uint)geometry.Length
//                };

//                D3D12_RAYTRACING_ACCELERATION_STRUCTURE_PREBUILD_INFO info;
//                GetAccelerationStructuredPrebuildInfo(&inputs, &info);

//                return new AccelerationStructureBuildInfo { ScratchSize = info.ScratchDataSizeInBytes, DestSize = info.ResultDataMaxSizeInBytes };
//            }
//        }

//        /// <summary>
//        /// Calculates <see cref="AccelerationStructureBuildInfo"/> for a top-level acceleration structure update
//        /// </summary>
//        /// <param name="layout">The <see cref="Layout"/> used by the top-level acceleration structure input</param>
//        /// <param name="numBottomLevelStructures">The number of bottom-level acceleration structures in the input</param>
//        /// <param name="flags">The <see cref="BuildAccelerationStructureFlags"/> to apply to the update info generation</param>
//        /// <returns>An <see cref="AccelerationStructureBuildInfo"/> describing the requirements for updating the acceleration structure</returns>
//        public AccelerationStructureBuildInfo GetUpdateInfo(Layout layout, uint numBottomLevelStructures, BuildAccelerationStructureFlags flags = BuildAccelerationStructureFlags.None)
//        {
//            var inputs = new D3D12_BUILD_RAYTRACING_ACCELERATION_STRUCTURE_INPUTS
//            {
//                Type = D3D12_RAYTRACING_ACCELERATION_STRUCTURE_TYPE.D3D12_RAYTRACING_ACCELERATION_STRUCTURE_TYPE_TOP_LEVEL,
//                Flags = (D3D12_RAYTRACING_ACCELERATION_STRUCTURE_BUILD_FLAGS)flags | D3D12_RAYTRACING_ACCELERATION_STRUCTURE_BUILD_FLAGS.D3D12_RAYTRACING_ACCELERATION_STRUCTURE_BUILD_FLAG_PERFORM_UPDATE,
//                DescsLayout = (D3D12_ELEMENTS_LAYOUT)layout,
//                InstanceDescs = 1,
//                NumDescs = numBottomLevelStructures
//            };

//            D3D12_RAYTRACING_ACCELERATION_STRUCTURE_PREBUILD_INFO info;
//            GetAccelerationStructuredPrebuildInfo(&inputs, &info);

//            return new AccelerationStructureBuildInfo { ScratchSize = info.ScratchDataSizeInBytes, DestSize = info.ResultDataMaxSizeInBytes };
//        }
//    }
//}
