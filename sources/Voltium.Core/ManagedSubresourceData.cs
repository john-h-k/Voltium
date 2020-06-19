using System;
using System.Reflection.Metadata;
using Voltium.Common;

namespace Voltium.TextureLoading
{
    /// <summary>
    /// Represents a section of data in a single resource
    /// </summary>
    public readonly struct SubresourceData
    {
        /// <summary>
        /// Creates a new instance of <see cref="SubresourceData"/>
        /// </summary>
        /// <param name="dataOffset">The offset from the resource start, in bytes</param>
        /// <param name="rowPitch">The row pitch, or width, or physical size, in bytes, of the subresource data</param>
        /// <param name="slicePitch">The depth pitch, or width, or physical size, in bytes, of the subresource data</param>
        public SubresourceData(nuint dataOffset, nuint rowPitch, nuint slicePitch)
        {
            DataOffset = dataOffset;
            RowPitch = rowPitch;
            SlicePitch = slicePitch;
        }

        // Same format as D3D12_SUBRESOURCE_DATA, just with diff first member
        //
        // void* pData
        // nuint RowPitch
        // nuint SlicePitch
        //
        // Once the base pointer (which DataOffset is from) is pinned, you can read DataOffset, and it to the base pointer
        // and then reinterpret this type as a D3D12_SUBRESOURCE_DATA and write this value to pData
        // and then this object represents the correct subresource data for methods such as UpdateSubresources

        /// <summary>
        /// The offset from the resource start, in bytes
        /// </summary>
        public readonly nuint DataOffset;

        /// <summary>
        /// The row pitch, or width, or physical size, in bytes, of the subresource data
        /// </summary>
        public readonly nuint RowPitch;

        /// <summary>
        /// The depth pitch, or width, or physical size, in bytes, of the subresource data
        /// </summary>
        public readonly nuint SlicePitch;

        /// <inheritdoc/>
        public override string ToString() => ""; // TODO
    }
}
