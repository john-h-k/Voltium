//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace Voltium.Core.Devices
//{
//    public partial class ComputeDevice
//    {




//        public AccelerationStructureBuildInfo GetBuildInfo(in GeometryDesc geometry, BuildAccelerationStructureFlags flags = BuildAccelerationStructureFlags.None)
//            => GetBuildInfo(stackalloc[] { geometry }, flags);

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

//            return new AccelerationStructureBuildInfo { ScratchSize = (long)info.ScratchDataSizeInBytes, DestSize = (long)info.ResultDataMaxSizeInBytes };
//        }

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

//        public AccelerationStructureBuildInfo GetUpdateInfo(in GeometryDesc geometry, BuildAccelerationStructureFlags flags = BuildAccelerationStructureFlags.None)
//            => GetBuildInfo(stackalloc[] { geometry }, flags);

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

//            return new AccelerationStructureBuildInfo { ScratchSize = (long)info.ScratchDataSizeInBytes, DestSize = (long)info.ResultDataMaxSizeInBytes };
//        }

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
//    }
//}
