using TerraFX.Interop;

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
        NoIndexBuffer =
#if D3D12
            DataFormat.Unknown,
#else
            VkIndexType.VK_INDEX_TYPE_NONE_KHR,
#endif

        /// <summary>
        /// A 16 bit unsigned integer (<see cref="ushort"/>) is being used for the indices
        /// </summary>
        R16UInt =
            #if D3D12
            DataFormat.R16UInt,
            #else
            VkIndexType.VK_INDEX_TYPE_UINT16,
#endif
        /// <summary>
        /// A 32 bit unsigned integer (<see cref="uint"/>) is being used for the indices
        /// </summary>
        R32UInt =
#if D3D12
            DataFormat.R32UInt,
#else
            VkIndexType.VK_INDEX_TYPE_UINT32,
#endif
    }
}
