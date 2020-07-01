using System;
using TerraFX.Interop;
using Voltium.Common;
using Voltium.Core.Devices;
using Voltium.Core.Managers;
using Voltium.Core.Memory.GpuResources;

namespace Voltium.Core.Infrastructure
{
    /// <summary>
    /// Represents a swapchain to a window
    /// </summary>
    internal unsafe struct SwapChain : IDisposable
    {
        private ComPtr<IDXGISwapChain3> _swapChain;

        public SwapChain(ComPtr<IDXGISwapChain3> swapChain) => _swapChain = swapChain.Move();

        public uint BackBufferIndex => _swapChain.Get()->GetCurrentBackBufferIndex();
        public int Present(uint syncInterval, uint flags) => _swapChain.Get()->Present(syncInterval, flags);

        public void ResizeBuffers(uint width, uint height)
        {
            DXGI_SWAP_CHAIN_DESC1 desc;
            Guard.ThrowIfFailed(_swapChain.Get()->GetDesc1(&desc));
            Guard.ThrowIfFailed(_swapChain.Get()->ResizeBuffers(
                desc.BufferCount,
                width,
                height,
                desc.Format,
                0
            ));
        }

        public Texture GetBackBuffer(ComputeDevice device, uint index)
        {
            using ComPtr<ID3D12Resource> buffer = default;
            Guard.ThrowIfFailed(_swapChain.Get()->GetBuffer(index, buffer.Guid, ComPtr.GetVoidAddressOf(&buffer)));
            DirectXHelpers.SetObjectName(buffer.Get(), $"BackBuffer #{index}");

            return Texture.FromResource(device, buffer.Move());
        }

        /// <inheritdoc cref="IDisposable"/>
        public void Dispose() => _swapChain.Dispose();
    }
}
