namespace Voltium.Core
{
    /// <summary>
    /// Represents a
    /// </summary>
    public readonly struct DescriptorHeapMemberRef
    {
        /// <summary>
        /// The index into the <see cref="DescriptorHeap"/> of the member
        /// </summary>
        public readonly uint Index;

        /// <summary>
        /// Create a new instance of <see cref="DescriptorHeapMemberRef"/>
        /// </summary>
        public DescriptorHeapMemberRef(uint index)
        {
            Index = index;
        }
    }
}
