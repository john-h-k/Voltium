using System;

namespace Voltium.Core.GpuResources
{
    /// <summary>
    /// Flags uesd by the GPU allocator
    /// </summary>
    [Flags]
    public enum GpuAllocFlags
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
    }

}
