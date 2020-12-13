using System;

namespace Voltium.Core.Memory
{
    /// <summary>
    /// Flags uesd by the GPU allocator
    /// </summary>
    [Flags]
    public enum AllocFlags
    {
        /// <summary>
        /// No flags
        /// </summary>
        None = 0,

        /// <summary>
        /// Force the allocation to be comitted
        /// </summary>
        ForceAllocateComitted = 1,

        /// <summary>
        ///  Force the allocation to not be comitted, and implicitly to be placed or reserved
        /// </summary>
        ForceAllocateNotComitted = 2,

        /// <summary>
        /// Disallows allocating buffers from larger buffers to improve efficiency. This flag is implicitly set when <see cref="ForceAllocateComitted"/> is set
        /// </summary>
        NoBufferSuballocation = 4,

        /// <summary>
        /// Try and make releasing the resource as fast as possible. This generally occurs by evicting rather than releasing the resource, and so won't immediately
        /// make the memory available and won't free the address range
        /// </summary>
        FastRelease = 8,
    }

}
