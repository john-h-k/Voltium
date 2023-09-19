using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TerraFX.Interop;

namespace Voltium.Core.Devices
{
    public unsafe partial class ComputeDevice
    {
        /// <inheritdoc cref="GetBottomLevelAccelerationStructureBuildInfo(ReadOnlySpan{GeometryDesc}, BuildAccelerationStructureFlags)"/>
        public AccelerationStructureBuildInfo GetBottomLevelAccelerationStructureBuildInfo(in GeometryDesc geometry, BuildAccelerationStructureFlags flags = BuildAccelerationStructureFlags.None)
            => GetBottomLevelAccelerationStructureBuildInfo(stackalloc[] { geometry }, flags);

        /// <summary>
        /// Calculates <see cref="AccelerationStructureBuildInfo"/> for a bottom-level acceleration structure build
        /// </summary>
        /// <param name="geometry">The <see cref="GeometryDesc"/> to build the acceleration structure from</param>
        /// <param name="flags">The <see cref="BuildAccelerationStructureFlags"/> to apply to the build info generation</param>
        /// <returns>An <see cref="AccelerationStructureBuildInfo"/> describing the requirements for building the acceleration structure</returns>
        public AccelerationStructureBuildInfo GetBottomLevelAccelerationStructureBuildInfo(ReadOnlySpan<GeometryDesc> geometry, BuildAccelerationStructureFlags flags = BuildAccelerationStructureFlags.None)
        {
            var (dest, scratch, _) = _device.GetBottomLevelAccelerationStructureBuildInfo(geometry, flags);
            return new AccelerationStructureBuildInfo { ScratchSize = scratch, DestSize = dest };
        }

        /// <summary>
        /// Calculates <see cref="AccelerationStructureBuildInfo"/> for a top-level acceleration structure build
        /// </summary>
        /// <param name="numBottomLevelStructures">The number of bottom-level acceleration structures in the input</param>
        /// <param name="flags">The <see cref="BuildAccelerationStructureFlags"/> to apply to the build info generation</param>
        /// <returns>An <see cref="AccelerationStructureBuildInfo"/> describing the requirements for building the acceleration structure</returns>
        public AccelerationStructureBuildInfo GetTopLevelAccelerationStructureBuildInfo(uint numBottomLevelStructures, BuildAccelerationStructureFlags flags = BuildAccelerationStructureFlags.None)
        {
            var (dest, scratch, _) = _device.GetTopLevelAccelerationStructureBuildInfo(numBottomLevelStructures, flags);
            return new AccelerationStructureBuildInfo { ScratchSize = scratch, DestSize = dest };
        }
    }
}
