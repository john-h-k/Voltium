//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using TerraFX.Interop;
//using Voltium.Common;
//using Voltium.Core.Contexts;
//using Voltium.Core.Memory;
//using Buffer = Voltium.Core.Memory.Buffer;

//namespace Voltium.Core
//{
//    public unsafe partial class ComputeContext
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
//        [IllegalBundleMethod, IllegalRenderPassMethod]
//        public void UpdateAccelerationStructure(
//            ulong bottomLevelGpuPointer,
//            Layout layout,
//            uint numBottomLevelStructures,
//            [RequiresResourceState(ResourceState.RaytracingAccelerationStructure)] in RaytracingAccelerationStructure source,
//            [RequiresResourceState(ResourceState.UnorderedAccess)] in Buffer scratch,
//            [RequiresResourceState(ResourceState.RaytracingAccelerationStructure)] in RaytracingAccelerationStructure dest,
//            BuildAccelerationStructureFlags flags = BuildAccelerationStructureFlags.None
//        )
//        {
//            D3D12_BUILD_RAYTRACING_ACCELERATION_STRUCTURE_DESC build = new()
//            {
//                SourceAccelerationStructureData = source.GpuAddress,
//                ScratchAccelerationStructureData = scratch.GpuAddress,
//                DestAccelerationStructureData = dest.GpuAddress,
//                Inputs = new D3D12_BUILD_RAYTRACING_ACCELERATION_STRUCTURE_INPUTS
//                {
//                    Type = D3D12_RAYTRACING_ACCELERATION_STRUCTURE_TYPE.D3D12_RAYTRACING_ACCELERATION_STRUCTURE_TYPE_TOP_LEVEL,
//                    Flags = ((D3D12_RAYTRACING_ACCELERATION_STRUCTURE_BUILD_FLAGS)flags.RemoveFlags()) | D3D12_RAYTRACING_ACCELERATION_STRUCTURE_BUILD_FLAGS.D3D12_RAYTRACING_ACCELERATION_STRUCTURE_BUILD_FLAG_PERFORM_UPDATE,
//                    DescsLayout = (D3D12_ELEMENTS_LAYOUT)layout,
//                    InstanceDescs = bottomLevelGpuPointer,
//                    NumDescs = numBottomLevelStructures
//                }
//            };

//            FlushBarriers();
//            List->BuildRaytracingAccelerationStructure(&build, 0, null);
//            InsertBarrierIfNecessary(dest, flags);
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
//        [IllegalBundleMethod, IllegalRenderPassMethod]
//        public void BuildAccelerationStructure(
//            in Buffer bottomLevelGpuPointer,
//            Layout layout,
//            uint numBottomLevelStructures,
//            [RequiresResourceState(ResourceState.UnorderedAccess)] in Buffer scratch,
//            [RequiresResourceState(ResourceState.RaytracingAccelerationStructure)] in RaytracingAccelerationStructure dest,
//            BuildAccelerationStructureFlags flags = BuildAccelerationStructureFlags.None
//        )
//        {
//            D3D12_BUILD_RAYTRACING_ACCELERATION_STRUCTURE_DESC build = new()
//            {
//                ScratchAccelerationStructureData = scratch.GpuAddress,
//                DestAccelerationStructureData = dest.GpuAddress,
//                Inputs = new D3D12_BUILD_RAYTRACING_ACCELERATION_STRUCTURE_INPUTS
//                {
//                    Type = D3D12_RAYTRACING_ACCELERATION_STRUCTURE_TYPE.D3D12_RAYTRACING_ACCELERATION_STRUCTURE_TYPE_TOP_LEVEL,
//                    Flags = (D3D12_RAYTRACING_ACCELERATION_STRUCTURE_BUILD_FLAGS)flags.RemoveFlags(),
//                    DescsLayout = (D3D12_ELEMENTS_LAYOUT)layout,
//                    InstanceDescs = bottomLevelGpuPointer.GpuAddress,
//                    NumDescs = numBottomLevelStructures
//                }
//            };

//            List->BuildRaytracingAccelerationStructure(&build, 0, null);
//            InsertBarrierIfNecessary(dest, flags);
//        }

//        /// <inheritdoc cref="BuildAccelerationStructure(ReadOnlySpan{GeometryDesc}, in Buffer, in RaytracingAccelerationStructure, BuildAccelerationStructureFlags)" />
//        [IllegalBundleMethod, IllegalRenderPassMethod]
//        public void BuildAccelerationStructure(
//            in GeometryDesc geometry,
//            [RequiresResourceState(ResourceState.UnorderedAccess)] in Buffer scratch,
//            [RequiresResourceState(ResourceState.RaytracingAccelerationStructure)] in RaytracingAccelerationStructure dest,
//            BuildAccelerationStructureFlags flags = BuildAccelerationStructureFlags.None
//        )
//            => BuildAccelerationStructure(stackalloc[] { geometry }, scratch, dest, flags);

//        /// <summary>
//        /// Builds a bottom-level acceleration structure from geometry
//        /// </summary>
//        /// <param name="geometry">The <see cref="GeometryDesc"/>s to use to build the structure</param>
//        /// <param name="scratch">The scratch-space <see cref="Buffer"/> used by the driver during the build</param>
//        /// <param name="dest">The destination buffer for the structure</param>
//        /// <param name="flags">The <see cref="BuildAccelerationStructureFlags"/> to apply to the build</param>
//        [IllegalBundleMethod, IllegalRenderPassMethod]
//        public void BuildAccelerationStructure(
//            ReadOnlySpan<GeometryDesc> geometry,
//            [RequiresResourceState(ResourceState.UnorderedAccess)] in Buffer scratch,
//            [RequiresResourceState(ResourceState.RaytracingAccelerationStructure)] in RaytracingAccelerationStructure dest,
//            BuildAccelerationStructureFlags flags = BuildAccelerationStructureFlags.None
//        )
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
//                        Flags = (D3D12_RAYTRACING_ACCELERATION_STRUCTURE_BUILD_FLAGS)flags.RemoveFlags(),
//                        DescsLayout = D3D12_ELEMENTS_LAYOUT.D3D12_ELEMENTS_LAYOUT_ARRAY,
//                        pGeometryDescs = (D3D12_RAYTRACING_GEOMETRY_DESC*)pGeo,
//                        NumDescs = (uint)geometry.Length
//                    }
//                };

//                FlushBarriers();
//                List->BuildRaytracingAccelerationStructure(&build, 0, null);
//                InsertBarrierIfNecessary(dest, flags);
//            }
//        }


//        /// <inheritdoc cref="UpdateAccelerationStructure(ReadOnlySpan{GeometryDesc}, in RaytracingAccelerationStructure, in Buffer, in RaytracingAccelerationStructure, BuildAccelerationStructureFlags)"/>
//        [IllegalBundleMethod, IllegalRenderPassMethod]
//        public void UpdateAccelerationStructure(
//            in GeometryDesc geometry,
//            [RequiresResourceState(ResourceState.RaytracingAccelerationStructure)] in RaytracingAccelerationStructure source,
//            [RequiresResourceState(ResourceState.UnorderedAccess)] in Buffer scratch,
//            [RequiresResourceState(ResourceState.RaytracingAccelerationStructure)] in RaytracingAccelerationStructure dest,
//            BuildAccelerationStructureFlags flags = BuildAccelerationStructureFlags.None
//        )
//            => UpdateAccelerationStructure(stackalloc[] { geometry }, source, scratch, dest, flags);

//        /// <summary>
//        /// Updates a bottom-level acceleration structure from geometry
//        /// </summary>
//        /// <param name="geometry">The <see cref="GeometryDesc"/>s to use to update the structure</param>
//        /// <param name="source">The source <see cref="Buffer"/> of the old acceleration structure to update</param>
//        /// <param name="scratch">The scratch-space <see cref="Buffer"/> used by the driver during the update</param>
//        /// <param name="dest">The destination buffer for the updated structure. This can be the same as <paramref name="source"/> to perform an in-place update</param>
//        /// <param name="flags">The <see cref="BuildAccelerationStructureFlags"/> to apply to the build</param>
//        [IllegalBundleMethod, IllegalRenderPassMethod]
//        public void UpdateAccelerationStructure(
//            ReadOnlySpan<GeometryDesc> geometry,
//            [RequiresResourceState(ResourceState.RaytracingAccelerationStructure)] in RaytracingAccelerationStructure source,
//            [RequiresResourceState(ResourceState.UnorderedAccess)] in Buffer scratch,
//            [RequiresResourceState(ResourceState.RaytracingAccelerationStructure)] in RaytracingAccelerationStructure dest,
//            BuildAccelerationStructureFlags flags = BuildAccelerationStructureFlags.None
//        )
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
//                        Flags = ((D3D12_RAYTRACING_ACCELERATION_STRUCTURE_BUILD_FLAGS)flags.RemoveFlags()) | D3D12_RAYTRACING_ACCELERATION_STRUCTURE_BUILD_FLAGS.D3D12_RAYTRACING_ACCELERATION_STRUCTURE_BUILD_FLAG_PERFORM_UPDATE,
//                        DescsLayout = D3D12_ELEMENTS_LAYOUT.D3D12_ELEMENTS_LAYOUT_ARRAY,
//                        pGeometryDescs = (D3D12_RAYTRACING_GEOMETRY_DESC*)pGeo,
//                        NumDescs = (uint)geometry.Length
//                    }
//                };

//                FlushBarriers();
//                List->BuildRaytracingAccelerationStructure(&build, 0, null);
//                InsertBarrierIfNecessary(dest, flags);
//            }
//        }




//        /// <summary>
//        /// Copies an acceleration structure
//        /// </summary>
//        /// <param name="source">The acceleration structure to copy from</param>
//        /// <param name="destination">The acceleration structure to copy to</param>
//        [IllegalBundleMethod, IllegalRenderPassMethod]
//        public void CopyAccelerationStructure(
//            [RequiresResourceState(ResourceState.RaytracingAccelerationStructure)] in RaytracingAccelerationStructure source,
//            [RequiresResourceState(ResourceState.RaytracingAccelerationStructure)] in RaytracingAccelerationStructure destination
//        )
//        {
//            FlushBarriers();
//            List->CopyRaytracingAccelerationStructure(source.GpuAddress, destination.GpuAddress, D3D12_RAYTRACING_ACCELERATION_STRUCTURE_COPY_MODE.D3D12_RAYTRACING_ACCELERATION_STRUCTURE_COPY_MODE_CLONE);
//        }

//        [IllegalBundleMethod, IllegalRenderPassMethod]
//        public void EmitAccelerationStructureCompactedSize(
//            [RequiresResourceState(ResourceState.RaytracingAccelerationStructure)] in RaytracingAccelerationStructure accelerationStructure,
//            [RequiresResourceState(ResourceState.UnorderedAccess)] in Buffer dest
//        )
//        {
//            var info = new D3D12_RAYTRACING_ACCELERATION_STRUCTURE_POSTBUILD_INFO_DESC
//            {
//                InfoType = D3D12_RAYTRACING_ACCELERATION_STRUCTURE_POSTBUILD_INFO_TYPE.D3D12_RAYTRACING_ACCELERATION_STRUCTURE_POSTBUILD_INFO_COMPACTED_SIZE,
//                DestBuffer = dest.GpuAddress
//            };

//            var asAddress = accelerationStructure.GpuAddress;

//            List->EmitRaytracingAccelerationStructurePostbuildInfo(&info, 1, &asAddress);
//        }

//        [IllegalBundleMethod, IllegalRenderPassMethod]
//        public void EmitAccelerationStructureSerializationInfo(
//            [RequiresResourceState(ResourceState.RaytracingAccelerationStructure)] in RaytracingAccelerationStructure accelerationStructure,
//            [RequiresResourceState(ResourceState.UnorderedAccess)] in Buffer dest
//        )
//        {
//            var info = new D3D12_RAYTRACING_ACCELERATION_STRUCTURE_POSTBUILD_INFO_DESC
//            {
//                InfoType = D3D12_RAYTRACING_ACCELERATION_STRUCTURE_POSTBUILD_INFO_TYPE.D3D12_RAYTRACING_ACCELERATION_STRUCTURE_POSTBUILD_INFO_SERIALIZATION,
//                DestBuffer = dest.GpuAddress
//            };

//            var asAddress = accelerationStructure.GpuAddress;

//            List->EmitRaytracingAccelerationStructurePostbuildInfo(&info, 1, &asAddress);
//        }

//        /// <summary>
//        /// Serializes an acceleration structure into a device-opaque format.
//        /// Intended for use as a debugging API, rather than for caching.
//        /// Deserializing a acceleration structure is (likely) slower than rebuilding it entirely
//        /// </summary>
//        /// <param name="accelerationStructure">The acceleration structure to be serialized</param>
//        /// <param name="serialized">The output to contain the opaque serialized data</param>
//        [IllegalBundleMethod, IllegalRenderPassMethod]
//        public void SerializeAccelerationStructure(
//            [RequiresResourceState(ResourceState.RaytracingAccelerationStructure)] in Buffer accelerationStructure,
//            [RequiresResourceState(ResourceState.UnorderedAccess)] in Buffer serialized
//        )
//        {
//            FlushBarriers();
//            List->CopyRaytracingAccelerationStructure(serialized.GpuAddress, accelerationStructure.GpuAddress, D3D12_RAYTRACING_ACCELERATION_STRUCTURE_COPY_MODE.D3D12_RAYTRACING_ACCELERATION_STRUCTURE_COPY_MODE_SERIALIZE);
//        }

//        /// <summary>
//        /// Deserializes a serialized acceleration structure from a device-opaque format.
//        /// Intended for use as a debugging API, rather than for caching.
//        /// Deserializing a acceleration structure is (likely) slower than rebuilding it entirely. 
//        /// </summary>
//        /// <param name="accelerationStructure">The opaque serialized data to be deserialized</param>
//        /// <param name="serialized">The output to contain the acceleration structure</param>
//        [IllegalBundleMethod, IllegalRenderPassMethod]
//        public void DeserializeAccelerationStructure(
//            [RequiresResourceState(ResourceState.NonPixelShaderResource)] in Buffer serialized,
//            [RequiresResourceState(ResourceState.RaytracingAccelerationStructure)] in Buffer accelerationStructure
//        )
//        {
//            FlushBarriers();
//            List->CopyRaytracingAccelerationStructure(accelerationStructure.GpuAddress, serialized.GpuAddress, D3D12_RAYTRACING_ACCELERATION_STRUCTURE_COPY_MODE.D3D12_RAYTRACING_ACCELERATION_STRUCTURE_COPY_MODE_DESERIALIZE);
//        }

//        /// <summary>
//        /// Compacts an acceleration structure so it occupies less space in memory
//        /// </summary>
//        /// <param name="uncompacted">The acceleration structure to compact</param>
//        /// <param name="compacted">The output to contain the acceleration structure</param>
//        [IllegalBundleMethod, IllegalRenderPassMethod]
//        public void CompactAccelerationStructure(
//            [RequiresResourceState(ResourceState.RaytracingAccelerationStructure)] in RaytracingAccelerationStructure uncompacted,
//            [RequiresResourceState(ResourceState.RaytracingAccelerationStructure)] in RaytracingAccelerationStructure compacted
//        )
//        {
//            FlushBarriers();
//            List->CopyRaytracingAccelerationStructure(uncompacted.GpuAddress, compacted.GpuAddress, D3D12_RAYTRACING_ACCELERATION_STRUCTURE_COPY_MODE.D3D12_RAYTRACING_ACCELERATION_STRUCTURE_COPY_MODE_COMPACT);
//        }

//        /// <summary>
//        /// Dispatches a raytracing operation
//        /// </summary>
//        /// <param name="desc">The <see cref="RayDispatchDesc"/> which describes the raytracing operation</param>
//        [IllegalRenderPassMethod]
//        public void DispatchRays(in RayDispatchDesc desc)
//        {
//            fixed (D3D12_DISPATCH_RAYS_DESC* pDesc = &desc.Desc)
//            {
//                List->DispatchRays(pDesc);
//            }
//        }

//        /// <summary>
//        /// Dispatches a raytracing operation
//        /// </summary>
//        [IllegalRenderPassMethod]
//        public void DispatchRays(
//            uint width,
//            in ShaderRecord raygenRecord,
//            in ShaderRecordTable hitGroupTable = default,
//            in ShaderRecordTable missShaderTable = default,
//            in ShaderRecordTable callableShaderTable = default
//        ) => DispatchRays(width, 1, 1, raygenRecord, hitGroupTable, missShaderTable, callableShaderTable);

//        /// <summary>
//        /// Dispatches a raytracing operation
//        /// </summary>
//        [IllegalRenderPassMethod]
//        public void DispatchRays(
//            uint width,
//            uint height,
//            in ShaderRecord raygenRecord,
//            in ShaderRecordTable hitGroupTable = default,
//            in ShaderRecordTable missShaderTable = default,
//            in ShaderRecordTable callableShaderTable = default
//        ) => DispatchRays(width, height, 1, raygenRecord, hitGroupTable, missShaderTable, callableShaderTable);

//        /// <summary>
//        /// Dispatches a raytracing operation
//        /// </summary>
//        [IllegalRenderPassMethod]
//        public void DispatchRays(
//            uint width,
//            uint height,
//            uint depth,
//            in ShaderRecord raygenRecord,
//            in ShaderRecordTable hitGroupTable = default,
//            in ShaderRecordTable missShaderTable = default,
//            in ShaderRecordTable callableShaderTable = default
//        )
//        {
//            var desc = new D3D12_DISPATCH_RAYS_DESC
//            {
//                Width = width,
//                Height = height,
//                Depth = depth,
//                RayGenerationShaderRecord = raygenRecord.Range,
//                HitGroupTable = hitGroupTable.RangeAndStride,
//                MissShaderTable = missShaderTable.RangeAndStride,
//                CallableShaderTable = callableShaderTable.RangeAndStride
//            };

//            List->DispatchRays(&desc);
//        }


//        private void InsertBarrierIfNecessary(in RaytracingAccelerationStructure dest, BuildAccelerationStructureFlags flags)
//        {
//            if (flags.HasFlag(BuildAccelerationStructureFlags.InsertUavBarrier))
//            {
//                Barrier(ResourceBarrier.UnorderedAccess(dest));
//            }
//        }
//    }
//}
