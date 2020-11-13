using System.Drawing;
using TerraFX.Interop;
using Voltium.Common;
using Voltium.Extensions;
using Voltium.Core.Memory;
using static TerraFX.Interop.Windows;

namespace Voltium.Core.Devices
{
    internal unsafe sealed class SwapChainOutput : Output
    {
        private UniqueComPtr<IDXGISwapChain3> _swapChain;
        private CommandQueue _presentQueue;

        private bool UseSeperatePresentQueue()
        {
            return _device.Adapter.IsAmd;
        }

        internal SwapChainOutput(GraphicsDevice device, in DXGI_SWAP_CHAIN_DESC1 swapChainDesc, IOutputOwner owner, OutputConfiguration desc) : base(device, desc)
        {
            _device = device;

            if (UseSeperatePresentQueue())
            {
                _presentQueue = new CommandQueue(device, ExecutionContext.Graphics, enableTdr: true);
            }
            else
            {
                _presentQueue = device.GraphicsQueue;
            }

            using var factory = CreateFactory();
            using UniqueComPtr<IDXGISwapChain1> swapChain = default;

            fixed (DXGI_SWAP_CHAIN_DESC1* pDesc = &swapChainDesc)
            {
                IUnknown* pQueue = (IUnknown*)_presentQueue.GetQueue();
                IDXGISwapChain1** ppChain = (IDXGISwapChain1**)&swapChain;
                Guard.ThrowIfFailed(
                    owner.Type switch
                    {
                        OutputType.Hwnd => factory.Ptr->CreateSwapChainForHwnd(
                            pQueue,
                            owner.GetOutput(),
                            pDesc,
                            null,
                            null,
                            ppChain
                        ),

                        OutputType.ICoreWindow => factory.Ptr->CreateSwapChainForCoreWindow(
                            pQueue,
                            (IUnknown*)owner.GetOutput(),
                            pDesc,
                            null,
                            ppChain
                        ),

                        OutputType.SwapChainPanel => factory.Ptr->CreateSwapChainForComposition(
                            pQueue,
                            pDesc,
                            null,
                            ppChain
                        ),

                        _ => E_INVALIDARG, // this way we fail if weird enum arg provided
                    }
                );

                if (owner.Type is OutputType.SwapChainPanel)
                {
                    Guard.ThrowIfFailed(((ISwapChainPanelNative*)owner.GetOutput())->SetSwapChain((IDXGISwapChain*)swapChain.Ptr));
                }
            }

            if (!swapChain.TryQueryInterface(out UniqueComPtr<IDXGISwapChain3> swapChain3))
            {
                ThrowHelper.ThrowPlatformNotSupportedException("Couldn't create IDXGISwapChain3, which is required");
            }
            swapChain.Dispose();

            DXGI_SWAP_CHAIN_DESC1 postCreateDesc;
            _device.ThrowIfFailed(swapChain3.Ptr->GetDesc1(&postCreateDesc));
            Resolution = new Size((int)postCreateDesc.Width, (int)postCreateDesc.Height);
            AspectRatio = Resolution.AspectRatio();

            _desc = desc;
            _swapChain = swapChain3.Move();

            _backBufferIndex = _swapChain.Ptr->GetCurrentBackBufferIndex();

            CreateBuffersFromSwapChain();
        }

        internal override void InternalResize(Size newSize)
        {
            _presentQueue.Idle();

            DataFormat format = _backBuffers[0].Format;
            for (var i = 0U; i < _desc.BackBufferCount; i++)
            {
                _backBuffers[i].Dispose();
            }

            _device.ThrowIfFailed(_swapChain.Ptr->ResizeBuffers(
                   0, // preserve existing number
                   (uint)newSize.Width,
                   (uint)newSize.Height,
                   DXGI_FORMAT.DXGI_FORMAT_UNKNOWN, // preserve existing format
                   0
            ));

            _backBufferIndex = _swapChain.Ptr->GetCurrentBackBufferIndex();

            CreateBuffersFromSwapChain();
        }

        private void CreateBuffersFromSwapChain()
        {
            for (var i = 0U; i < _desc.BackBufferCount; i++)
            {
                using UniqueComPtr<ID3D12Resource> buffer = default;
                _device.ThrowIfFailed(_swapChain.Ptr->GetBuffer(i, buffer.Iid, (void**)&buffer));
                DebugHelpers.SetName(buffer.Ptr, $"BackBuffer #{i}");

                _backBuffers[i] = Texture.FromResource(_device, buffer.Move());
            }

            _backBufferIndex = _swapChain.Ptr->GetCurrentBackBufferIndex();
            CreateViews();
        }

        internal override void InternalPresent() => _device.ThrowIfFailed(_swapChain.Ptr->Present(_desc.SyncInterval, 0));
    }
}
