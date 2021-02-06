using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using TerraFX.Interop;
using Voltium.Common;

namespace Voltium.Core
{
    /// <summary>
    /// Describes a set of geoemetry, either a <see cref="TriangleGeometryDesc"/> or a <see cref="AxisAlignedBoundingBoxGeometryDesc"/>, for use in
    /// building acceleration structures
    /// </summary>
    public struct GeometryDesc
    {
        internal D3D12_RAYTRACING_GEOMETRY_DESC Desc;

        /// <summary>
        /// Creates a new <see cref="GeometryDesc"/> from a <see cref="TriangleGeometryDesc"/>
        /// </summary>
        /// <param name="desc">The <see cref="TriangleGeometryDesc"/> that describes the geometry</param>
        /// <returns>A new <see cref="GeometryDesc"/></returns>
        public static GeometryDesc FromTriangles(in TriangleGeometryDesc desc)
            => new() { Type = GeometryType.Triangles, Triangles = desc };

        /// <summary>
        /// Creates a new <see cref="GeometryDesc"/> from a <see cref="FromAlignedAxisBoundingBoxes"/>
        /// </summary>
        /// <param name="desc">The <see cref="FromAlignedAxisBoundingBoxes"/> that describes the geometry</param>
        /// <returns>A new <see cref="GeometryDesc"/></returns>
        public static GeometryDesc FromAlignedAxisBoundingBoxes(in AxisAlignedBoundingBoxGeometryDesc desc)
            => new() { Type = GeometryType.AxisAlignedBoundingBoxes, AxisAlignedBoundingBoxes = desc };

        /// <summary>
        /// The type of geometry used in the description
        /// </summary>
        public GeometryType Type { get => (GeometryType)Desc.Type; set => Desc.Type = (D3D12_RAYTRACING_GEOMETRY_TYPE)value; }

        /// <summary>
        /// The <see cref="GeometryFlags"/> for this <see cref="GeometryDesc"/>
        /// </summary>
        public GeometryFlags Flags { get => (GeometryFlags)Desc.Flags; set => Desc.Flags = (D3D12_RAYTRACING_GEOMETRY_FLAGS)value; }

        /// <summary>
        /// If <see cref="Type"/> is <see cref="GeometryType.Triangles"/>, the <see cref="TriangleGeometryDesc"/> for this instance
        /// </summary>
        public ref TriangleGeometryDesc Triangles
        {
            get
            {
                ThrowForMismatch(GeometryType.Triangles);
                return ref MemoryMarshal.GetReference(MemoryMarshal.CreateSpan(ref Unsafe.As<D3D12_RAYTRACING_GEOMETRY_TRIANGLES_DESC, TriangleGeometryDesc>(ref Desc.Triangles), 0));
            }
        }


        /// <summary>
        /// If <see cref="Type"/> is <see cref="GeometryType.AxisAlignedBoundingBoxes"/>, the <see cref="AxisAlignedBoundingBoxGeometryDesc"/> for this instance
        /// </summary>
        public ref AxisAlignedBoundingBoxGeometryDesc AxisAlignedBoundingBoxes
        {
            get
            {
                ThrowForMismatch(GeometryType.AxisAlignedBoundingBoxes);
                return ref MemoryMarshal.GetReference(MemoryMarshal.CreateSpan(ref Unsafe.As<D3D12_RAYTRACING_GEOMETRY_AABBS_DESC, AxisAlignedBoundingBoxGeometryDesc>(ref Desc.AABBs), 0));
            }
        }

        private void ThrowForMismatch(GeometryType expected)
        {
            if (Type != expected)
            {
                ThrowHelper.ThrowInvalidOperationException($"Can't access GeometryDesc.{expected} when type is GeometryType.{Type}");
            }
        }
    }
}
