using TerraFX.Interop;

namespace Voltium.Core.Devices
{
    public enum FenceFlags
    {
        None = D3D12_FENCE_FLAGS.D3D12_FENCE_FLAG_NONE,
        ProcessShared = D3D12_FENCE_FLAGS.D3D12_FENCE_FLAG_SHARED,
        AdapterShared = D3D12_FENCE_FLAGS.D3D12_FENCE_FLAG_SHARED_CROSS_ADAPTER
    }
}
