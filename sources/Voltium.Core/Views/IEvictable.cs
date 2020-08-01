using TerraFX.Interop;

namespace Voltium.Core
{
    /// <summary>
    /// Indicates a type is evictable
    /// </summary>
    public unsafe interface IEvictable
    {
        internal ID3D12Pageable* GetPageable();

        // true if we can treat the type as being an ID3D12Pageable* directly
        internal bool IsBlittableToPointer { get; }
    }
}
