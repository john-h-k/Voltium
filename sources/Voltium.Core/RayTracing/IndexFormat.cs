namespace Voltium.Core
{
    /// <summary>
    /// The format of indices used in the raytracing pipeline
    /// </summary>
    public enum IndexFormat : uint
    {
        /// <summary>
        /// No index buffer is used
        /// </summary>
        NoIndexBuffer = DataFormat.Unknown,

        /// <summary>
        /// A 16 bit unsigned integer (<see cref="ushort"/>) is being used for the indices
        /// </summary>
        R16UInt = DataFormat.R16UInt,

        /// <summary>
        /// A 32 bit unsigned integer (<see cref="uint"/>) is being used for the indices
        /// </summary>
        R32UInt = DataFormat.R32UInt
    }
}
