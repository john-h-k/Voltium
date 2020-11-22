using System;
using System.Runtime.CompilerServices;
using TerraFX.Interop;

namespace Voltium.Core
{
    [Flags]
    public enum BuildAccelerationStructureFlags
    {
        None = 0,
        AllowUpdate = D3D12_RAYTRACING_ACCELERATION_STRUCTURE_BUILD_FLAGS.D3D12_RAYTRACING_ACCELERATION_STRUCTURE_BUILD_FLAG_ALLOW_UPDATE,
        AllowCompaction = D3D12_RAYTRACING_ACCELERATION_STRUCTURE_BUILD_FLAGS.D3D12_RAYTRACING_ACCELERATION_STRUCTURE_BUILD_FLAG_ALLOW_COMPACTION,
        PreferFastTrace = D3D12_RAYTRACING_ACCELERATION_STRUCTURE_BUILD_FLAGS.D3D12_RAYTRACING_ACCELERATION_STRUCTURE_BUILD_FLAG_PREFER_FAST_TRACE,
        PreferFastBuild = D3D12_RAYTRACING_ACCELERATION_STRUCTURE_BUILD_FLAGS.D3D12_RAYTRACING_ACCELERATION_STRUCTURE_BUILD_FLAG_PREFER_FAST_BUILD,
        MinimizeMemory = D3D12_RAYTRACING_ACCELERATION_STRUCTURE_BUILD_FLAGS.D3D12_RAYTRACING_ACCELERATION_STRUCTURE_BUILD_FLAG_MINIMIZE_MEMORY,
        InsertUavBarrier = 1 << 31
    }

    internal static class BuildAccelerationStructureFlagsExtensions
    {
        public static BuildAccelerationStructureFlags RemoveFlags(this BuildAccelerationStructureFlags flags) => flags & ~BuildAccelerationStructureFlags.InsertUavBarrier;
    }
}
