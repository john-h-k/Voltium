using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TerraFX.Interop;
using Voltium.Common;
using Voltium.Core.Contexts;
using Voltium.Core.Memory;
using Buffer = Voltium.Core.Memory.Buffer;

namespace Voltium.Core
{
    public struct BufferRegion
    {

    }

    public unsafe partial class ComputeContext
    {
        /// <summary>
        /// Builds a top-level acceleration structure from bottom-level acceleration structures
        /// </summary>
        /// <param name="instances">The GPU address of either an array of RaytracingInstanceDescs, or an array of pointers to RaytracingInstanceDescs, depending on the value of <paramref name="layout"/></param>
        /// <param name="layout">The layout of <paramref name="instances"/>, either an array, or an array of pointers</param>
        /// <param name="instanceCount">The number of structures pointed to by <paramref name="instances"/></param>
        /// <param name="scratch">The scratch-space <see cref="Buffer"/> used by the driver during the build</param>
        /// <param name="dest">The destination buffer for the structure</param>
        /// <param name="flags">The <see cref="BuildAccelerationStructureFlags"/> to apply to the build</param>
        [IllegalBundleMethod, IllegalRenderPassMethod]
        public void BuildTopLevelAccelerationStructure(
            in Buffer instances,
            uint instanceCount,
            LayoutType layout,
            [RequiresResourceState(ResourceState.UnorderedAccess)] in Buffer scratch,
            [RequiresResourceState(ResourceState.RaytracingAccelerationStructure)] in RaytracingAccelerationStructure dest,
            BuildAccelerationStructureFlags flags = BuildAccelerationStructureFlags.None
        )
        {
            var command = new CommandBuildTopLevelAccelerationStructure
            {
                Instances = instances.Handle,
                InstanceCount = instanceCount,
                Layout = layout,
                Dest = dest.Handle,
                Scratch = scratch.Handle,
                Flags = flags,
                Offset = 0
            };

            _encoder.Emit(&command);
        }

        /// <inheritdoc cref="BuildBottomLevelAccelerationStructure(ReadOnlySpan{GeometryDesc}, in Buffer, in RaytracingAccelerationStructure, BuildAccelerationStructureFlags)" />
        [IllegalBundleMethod, IllegalRenderPassMethod]
        public void BuildBottomLevelAccelerationStructure(
            in GeometryDesc geometry,
            [RequiresResourceState(ResourceState.UnorderedAccess)] in Buffer scratch,
            [RequiresResourceState(ResourceState.RaytracingAccelerationStructure)] in RaytracingAccelerationStructure dest,
            BuildAccelerationStructureFlags flags = BuildAccelerationStructureFlags.None
        )
            => BuildBottomLevelAccelerationStructure(stackalloc[] { geometry }, scratch, dest, flags);

        /// <summary>
        /// Builds a bottom-level acceleration structure from geometry
        /// </summary>
        /// <param name="geometry">The <see cref="GeometryDesc"/>s to use to build the structure</param>
        /// <param name="scratch">The scratch-space <see cref="Buffer"/> used by the driver during the build</param>
        /// <param name="dest">The destination buffer for the structure</param>
        /// <param name="flags">The <see cref="BuildAccelerationStructureFlags"/> to apply to the build</param>
        [IllegalBundleMethod, IllegalRenderPassMethod]
        public void BuildBottomLevelAccelerationStructure(
            ReadOnlySpan<GeometryDesc> geometry,
            [RequiresResourceState(ResourceState.UnorderedAccess)] in Buffer scratch,
            [RequiresResourceState(ResourceState.RaytracingAccelerationStructure)] in RaytracingAccelerationStructure dest,
            BuildAccelerationStructureFlags flags = BuildAccelerationStructureFlags.None
        )
        {
            fixed (GeometryDesc* pGeometry = geometry)
            {
                var command = new CommandBuildBottomLevelAccelerationStructure
                {
                    GeometryCount = (uint)geometry.Length,
                    Dest = dest.Handle,
                    Scratch = scratch.Handle,
                    Flags = flags
                };

                _encoder.EmitVariable(&command, pGeometry, command.GeometryCount);
            }
        }


        /// <summary>
        /// Copies an acceleration structure
        /// </summary>
        /// <param name="source">The acceleration structure to copy from</param>
        /// <param name="destination">The acceleration structure to copy to</param>
        /// <param name="compact">Whether to compact the acceleration structure</param>
        [IllegalBundleMethod, IllegalRenderPassMethod]
        public void CopyAccelerationStructure(
            [RequiresResourceState(ResourceState.RaytracingAccelerationStructure)] in RaytracingAccelerationStructure source,
            [RequiresResourceState(ResourceState.RaytracingAccelerationStructure)] in RaytracingAccelerationStructure destination,
            bool compact = false
        )
        {
            var command = new CommandCopyAccelerationStructure
            {
                Source = source.Handle,
                Dest = destination.Handle,
                Compact = compact
            };

            _encoder.Emit(&command);
        }

        /// <summary>
        /// Serializes an acceleration structure into a device-opaque format.
        /// Intended for use as a debugging API, rather than for caching.
        /// Deserializing a acceleration structure is (likely) slower than rebuilding it entirely
        /// </summary>
        /// <param name="accelerationStructure">The acceleration structure to be serialized</param>
        /// <param name="serialized">The output to contain the opaque serialized data</param>
        [IllegalBundleMethod, IllegalRenderPassMethod]
        public void SerializeAccelerationStructure(
            [RequiresResourceState(ResourceState.RaytracingAccelerationStructure)] in RaytracingAccelerationStructure accelerationStructure,
            [RequiresResourceState(ResourceState.UnorderedAccess)] in Buffer serialized
        )
        {
            var command = new CommandSerializeAccelerationStructure
            {
                Source = accelerationStructure.Handle,
                Dest = serialized.Handle
            };

            _encoder.Emit(&command);
        }

        /// <summary>
        /// Deserializes a serialized acceleration structure from a device-opaque format.
        /// Intended for use as a debugging API, rather than for caching.
        /// Deserializing a acceleration structure is (likely) slower than rebuilding it entirely. 
        /// </summary>
        /// <param name="accelerationStructure">The opaque serialized data to be deserialized</param>
        /// <param name="serialized">The output to contain the acceleration structure</param>
        [IllegalBundleMethod, IllegalRenderPassMethod]
        public void DeserializeAccelerationStructure(
            [RequiresResourceState(ResourceState.NonPixelShaderResource)] in Buffer serialized,
            [RequiresResourceState(ResourceState.RaytracingAccelerationStructure)] in RaytracingAccelerationStructure accelerationStructure
        )
        {
            var command = new CommandDeserializeAccelerationStructure
            {
                Source = serialized.Handle,
                Dest = accelerationStructure.Handle
            };

            _encoder.Emit(&command);
        }
        /// <summary>
        /// Dispatches a raytracing operation
        /// </summary>
        /// <param name="desc">The <see cref="RayTables"/> which describes the raytracing operation</param>
        [IllegalRenderPassMethod]
        public void DispatchRays(uint x, in RayTables desc) => DispatchRays(x, 1, 1, desc);
        /// <summary>
        /// Dispatches a raytracing operation
        /// </summary>
        /// <param name="desc">The <see cref="RayTables"/> which describes the raytracing operation</param>
        [IllegalRenderPassMethod]
        public void DispatchRays(uint x, uint y, in RayTables desc) => DispatchRays(x, y, 1, desc);

        /// <summary>
        /// Dispatches a raytracing operation
        /// </summary>
        /// <param name="desc">The <see cref="RayTables"/> which describes the raytracing operation</param>
        [IllegalRenderPassMethod]
        public void DispatchRays(uint x, uint y, uint z, in RayTables desc)
        {
            var command = new CommandRayTrace
            {
                Width = desc.Width,
                Height = desc.Height,
                Depth = desc.Depth,

                RayGeneration =
                new ShaderRecord
                {
                    Buffer = desc.RayGenBuffer.Handle,
                    Offset = 0,
                    Length = desc.RayGenLength
                },

                MissShader = new ShaderRecordArray
                {
                    RecordCount = desc.MissShaderCount,
                    Record = new ShaderRecord
                    {
                        Buffer = desc.MissShaderBuffer.Handle,
                        Offset = 0,
                        Length = desc.MissShaderLength
                    }
                },

                HitGroup = new ShaderRecordArray
                {
                    RecordCount = desc.HitGroupCount,
                    Record = new ShaderRecord
                    {
                        Buffer = desc.HitGroupBuffer.Handle,
                        Offset = 0,
                        Length = desc.HitGroupLength
                    }
                },

                Callable = new ShaderRecordArray
                {
                    RecordCount = desc.CallableShaderCount,
                    Record = new ShaderRecord
                    {
                        Buffer = desc.CallableShaderBuffer.Handle,
                        Offset = 0,
                        Length = desc.CallableShaderLength
                    }
                }
            };

            _encoder.Emit(&command);
        }
    }
}
