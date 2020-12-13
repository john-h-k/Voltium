namespace Voltium.Core.Devices
{
    /// <summary>
    /// Information about the architecture of the device
    /// </summary>
    public struct DeviceArchitecture
    {
        /// <summary>
        /// The number of bits used for GPU virtual addresses
        /// </summary>
        public int AddressSpaceBits { init; get; }

        /// <summary>
        /// The maximum number of bits used for resource addresses
        /// </summary>
        public int ResourceAddressBits { init; get; }

        /// <summary>
        /// Whether the GPU is a unified-memory-architecture (UMA) device that is cache-coherent with the device
        /// </summary>
        public bool IsCacheCoherentUma { init; get; }

        /// <summary>
        /// The size, in bytes, of the virtual address space. This can be derived from <see cref="AddressSpaceBits"/>, but is
        /// provided as a field for simplicity
        /// </summary>
        public ulong VirtualAddressSpaceSize { init; get; }

    }
}
