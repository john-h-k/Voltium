using System.Diagnostics.CodeAnalysis;
using TerraFX.Interop;
#pragma warning disable 1591

namespace Voltium.Core.d3dx12
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public static class CD3DX12_GPU_DESCRIPTOR_HANDLE
    {
        public static D3D12_GPU_DESCRIPTOR_HANDLE Create(CD3DX12_DEFAULT _)
        {
            return new D3D12_GPU_DESCRIPTOR_HANDLE { ptr = 0 };
        }

        public static D3D12_GPU_DESCRIPTOR_HANDLE Create(in D3D12_GPU_DESCRIPTOR_HANDLE other,
            int offsetScaledByIncrementSize)
        {
            return new D3D12_GPU_DESCRIPTOR_HANDLE
            {
                ptr = (ulong)offsetScaledByIncrementSize
            };
        }

        public static D3D12_GPU_DESCRIPTOR_HANDLE Create(in D3D12_GPU_DESCRIPTOR_HANDLE other, int offsetInDescriptors,
            uint descriptorIncrementSize)
        {
            return new D3D12_GPU_DESCRIPTOR_HANDLE
            {
                ptr = other.ptr + (uint)offsetInDescriptors * descriptorIncrementSize
            };
        }

        public static void Offset(ref this D3D12_GPU_DESCRIPTOR_HANDLE handle, int offsetInDescriptors,
            uint descriptorIncrementSize)
        {
            handle.ptr += (ulong)offsetInDescriptors * descriptorIncrementSize;
        }

        public static void Offset(ref this D3D12_GPU_DESCRIPTOR_HANDLE handle, int offsetScaledByIncrementSize)
        {
            handle.ptr += (uint)offsetScaledByIncrementSize;
        }

        public static void InitOffsetted(out D3D12_GPU_DESCRIPTOR_HANDLE handle, in
            D3D12_GPU_DESCRIPTOR_HANDLE @base, int offsetScaledByIncrementSize)
        {
            handle.ptr = @base.ptr + (ulong)offsetScaledByIncrementSize;
        }

        static void InitOffsetted(out D3D12_GPU_DESCRIPTOR_HANDLE handle, in D3D12_GPU_DESCRIPTOR_HANDLE @base,
            int offsetInDescriptors, uint descriptorIncrementSize)
        {
            handle.ptr = @base.ptr + (ulong)offsetInDescriptors * descriptorIncrementSize;
        }
    }
}
