namespace Voltium.Core.Devices
{
    /// <summary>
    /// Describes the layout of a CPU-side subresource
    /// </summary>
    public struct SubresourceLayout
    {
        /// <summary>
        /// The size of the rows, in bytes
        /// </summary>
        public ulong RowSize;

        /// <summary>
        /// The number of rows
        /// </summary>
        public uint NumRows;
    }
}
