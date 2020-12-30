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
        public SubresourceData(ulong dataOffset, uint rowPitch, uint slicePitch)
        {
            DataOffset = dataOffset;
            RowPitch = rowPitch;
            SlicePitch = slicePitch;
        }

        // Same format as D3D12_SUBRESOURCE_INFO
        // and then this object represents the correct subresource data for methods such as UpdateSubresources

        /// <summary>
        /// The offset from the resource start, in bytes
        /// </summary>
        public readonly ulong DataOffset;

        /// <summary>
        /// The row pitch, or width, or physical size, in bytes, of the subresource data
        /// </summary>
        public readonly uint RowPitch;

        /// <summary>
        /// The depth pitch, or width, or physical size, in bytes, of the subresource data
        /// </summary>
        public readonly uint SlicePitch;

        /// <inheritdoc/>
        public override string ToString() => ""; // TODO
    }
}
