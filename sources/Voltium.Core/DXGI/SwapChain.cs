using System;
using TerraFX.Interop;
using Voltium.Common;

namespace Voltium.Core.DXGI
{
    /// <summary>
    /// Represents a swapchain to a window
    /// </summary>
    public unsafe struct SwapChain : IDisposable
    {
        /// <summary>
        /// The underlying value of the swapchain
        /// </summary>
        public IDXGISwapChain1* Value => _swapChain.Get();

        private ComPtr<IDXGISwapChain1> _swapChain;

        private SwapChain(IDXGISwapChain1* value) => _swapChain = value;

        /// <summary>
        /// Create a new <see cref="SwapChain"/>
        /// </summary>
        /// <param name="factory">The DXGI factory to use</param>
        /// <param name="queue">The command queue to create the <see cref="SwapChain"/> on</param>
        /// <param name="window">The <see cref="HWND"/> window handle to the render target window</param>
        /// <param name="desc">The swapchain description</param>
        /// <param name="fullscreenDesc">The fullscreen swapchain description</param>
        /// <param name="output">The DXGI output</param>
        /// <returns>A new swapchain</returns>
        public static SwapChain CreateForWindow(
            IDXGIFactory2* factory,
            ID3D12CommandQueue* queue,
            HWND window,
            DXGI_SWAP_CHAIN_DESC1 desc,
            DXGI_SWAP_CHAIN_FULLSCREEN_DESC fullscreenDesc,
            IDXGIOutput* output
        )
        {
            IDXGISwapChain1* p;

            Guard.ThrowIfFailed(factory->CreateSwapChainForHwnd(
                (IUnknown*)queue,
                window,
                &desc,
                &fullscreenDesc,
                output,
                &p
            ));

            return new SwapChain(p);
        }

        /// <inheritdoc cref="IDisposable"/>
        public void Dispose() => _swapChain.Dispose();
    }
}
