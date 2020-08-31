using static TerraFX.Interop.D3D12_PRIMITIVE_TOPOLOGY_TYPE;
using static TerraFX.Interop.D3D12_PIPELINE_STATE_SUBOBJECT_TYPE;
using TerraFX.Interop;
using System.Runtime.InteropServices;

namespace Voltium.Core.Pipeline
{
    /// <summary>
    /// The class of topology (either Point, Line, Triangle, or Patch)
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    public struct TopologyClass : IPipelineStreamElement<TopologyClass>
    {
        [FieldOffset(0)]
        internal D3D12_PIPELINE_STATE_SUBOBJECT_TYPE Type;

        [FieldOffset(sizeof(D3D12_PIPELINE_STATE_SUBOBJECT_TYPE))]
        internal D3D12_PRIMITIVE_TOPOLOGY_TYPE Class;

        [FieldOffset(0)]
        internal nuint _Pad;

        internal TopologyClass(D3D12_PRIMITIVE_TOPOLOGY_TYPE type)
        {
            _Pad = default;
            Type = D3D12_PIPELINE_STATE_SUBOBJECT_TYPE_PRIMITIVE_TOPOLOGY;
            Class = type;
        }

        /// <summary>
        /// Primitives are points
        /// </summary>
        public static TopologyClass Point => new(D3D12_PRIMITIVE_TOPOLOGY_TYPE_POINT);

        /// <summary>
        /// Primitives are lines
        /// </summary>
        public static TopologyClass Line => new(D3D12_PRIMITIVE_TOPOLOGY_TYPE_LINE);

        /// <summary>
        /// Primitives are triangles
        /// </summary>
        public static TopologyClass Triangle => new(D3D12_PRIMITIVE_TOPOLOGY_TYPE_TRIANGLE);

        /// <summary>
        /// Primitives are control patches used in tesselation
        /// </summary>
        public static TopologyClass Patch => new(D3D12_PRIMITIVE_TOPOLOGY_TYPE_PATCH);

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public void _Initialize() => Type = D3D12_PIPELINE_STATE_SUBOBJECT_TYPE_PRIMITIVE_TOPOLOGY;
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
    }

    internal struct AlignedSubobjectType<T> where T : unmanaged
    {
        internal D3D12_PIPELINE_STATE_SUBOBJECT_TYPE Type;
        internal T Inner;
    }
}
