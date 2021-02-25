using System.Drawing;
using TerraFX.Interop;
using Voltium.Common;
using Voltium.Extensions;
using Voltium.Core.Memory;
using static TerraFX.Interop.Windows;
using System.Threading;
using Voltium.Common.Threading;

namespace Voltium.Core.Devices
{
    struct InterlockedBool
    {
        private const int True = 1;
        private const int False = 0;

        private volatile int _value;

        public bool Value
        {
            get => _value == True;
            set => _value = value ? True : False;
        }

        public bool Exchange(bool value)
        {
            return Interlocked.Exchange(ref _value, value ? True : False) == True;
        }

        public bool CompareExchange(bool value, bool comparand)
        {
            return Interlocked.CompareExchange(ref _value, value ? True : False, comparand ? True : False) == True;
        }

        public static implicit operator bool(InterlockedBool val) => val.Value;
    }

    internal unsafe sealed class SwapChainOutput : Output
    {
        private UniqueComPtr<IDXGISwapChain3> _swapChain;
        private CommandQueue _presentQueue;
        private bool _presentQueueIsSeperate;
        private BackBufferBuffer16 _actualBackBuffers;

        private bool UseSeperatePresentQueue()
        {
            return _device.Adapter.IsAmd;
        }

        internal SwapChainOutput(GraphicsDevice device, Size size, IOutputOwner owner, OutputConfiguration desc) : base(device, desc)
        {
            _device = device;

            var swapChainDesc = CreateDesc(desc, size);

            if (UseSeperatePresentQueue())
            {
                _presentQueue = new CommandQueue(device, ExecutionContext.Graphics, enableTdr: true);
                _presentQueueIsSeperate = true;
            }
            else
            {
                _presentQueue = device.GraphicsQueue;
            }

            using var factory = CreateFactory();
            using UniqueComPtr<IDXGISwapChain1> swapChain = default;

            var pQueue = (IUnknown*)_presentQueue.GetQueue();
            var ppChain = (IDXGISwapChain1**)&swapChain;

            IDXGIOutput* pRestrict = null;

            Guard.ThrowIfFailed(
                owner.Type switch
                {
                    OutputType.Hwnd => factory.Ptr->CreateSwapChainForHwnd(
                        pQueue,
                        owner.GetOutput(),
                        &swapChainDesc,
                        null,
                        pRestrict,
                        ppChain
                    ),

                    OutputType.ICoreWindow => factory.Ptr->CreateSwapChainForCoreWindow(
                        pQueue,
                        (IUnknown*)owner.GetOutput(),
                        &swapChainDesc,
                        pRestrict,
                        ppChain
                    ),

                    OutputType.SwapChainPanel => factory.Ptr->CreateSwapChainForComposition(
                        pQueue,
                        &swapChainDesc,
                        pRestrict,
                        ppChain
                    ),

                    _ => E_INVALIDARG, // this way we fail if weird enum arg provided
                }
            );

            if (owner.Type is OutputType.SwapChainPanel)
            {
                Guard.ThrowIfFailed(((ISwapChainPanelNative*)owner.GetOutput())->SetSwapChain((IDXGISwapChain*)swapChain.Ptr));
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

        public override bool IsFullyOccluded => _swapChain.Ptr->Present(0, DXGI_PRESENT_TEST) == DXGI_STATUS_OCCLUDED;   

        private static DXGI_SWAP_CHAIN_DESC1 CreateDesc(OutputConfiguration desc, Size outputArea)
        {
            if (desc.BackBufferCount > BackBufferBuffer16.BufferLength)
            {
                ThrowHelper.ThrowArgumentException($"Cannot have more than {BackBufferBuffer16.BufferLength} back buffers");
            }

            return new DXGI_SWAP_CHAIN_DESC1
            {
                AlphaMode = DXGI_ALPHA_MODE.DXGI_ALPHA_MODE_IGNORE, // TODO document
                BufferCount = desc.BackBufferCount,
                BufferUsage = (uint)desc.Flags.UsageFlags(), // this is the output chain
                Flags = 0,
                Format = (DXGI_FORMAT)desc.BackBufferFormat,
                Height = (uint)outputArea.Height,
                Width = (uint)outputArea.Width,
                SampleDesc = new DXGI_SAMPLE_DESC(count: 1, quality: 0), // backbuffer MSAA is not supported in D3D12
                Scaling = DXGI_SCALING.DXGI_SCALING_NONE,
                Stereo = FALSE, // stereoscopic rendering, 2 images, e.g VR or 3D holo
                SwapEffect = desc.Flags.HasFlag(OutputFlags.PreserveBackBuffer) ? DXGI_SWAP_EFFECT.DXGI_SWAP_EFFECT_FLIP_SEQUENTIAL : DXGI_SWAP_EFFECT.DXGI_SWAP_EFFECT_FLIP_DISCARD
            };
        }

        internal override void InternalResize(Size newSize, uint newBufferCount, BackBufferFormat format)
        {
            MonitorLock? @lock = null;
            try
            {
                @lock = _presentQueue.IdleAndTakeLock();

                for (var i = 0U; i < _desc.BackBufferCount; i++)
                {
                    _backBuffers[i].Dispose();
                }

                _device.ThrowIfFailed(_swapChain.Ptr->ResizeBuffers(
                       newBufferCount, // preserve existing number
                       (uint)newSize.Width,
                       (uint)newSize.Height,
                       (DXGI_FORMAT)format, // preserve existing format
                       0
                ));

                _backBufferIndex = _swapChain.Ptr->GetCurrentBackBufferIndex();
            }
            finally
            {
                @lock?.Exit();
            }

            CreateBuffersFromSwapChain();
        }

        private void CreateBuffersFromSwapChain()
        {
            ref var backBuffers = ref _presentQueueIsSeperate ? ref _actualBackBuffers : ref _backBuffers;
            for (var i = 0U; i < _desc.BackBufferCount; i++)
            {
                using UniqueComPtr<ID3D12Resource> buffer = default;
                _device.ThrowIfFailed(_swapChain.Ptr->GetBuffer(i, buffer.Iid, (void**)&buffer));
                DebugHelpers.SetName(buffer.Ptr, $"BackBuffer #{i}");

                backBuffers[i] = Texture.FromResource(_device, buffer.Move());
            }

            if (_presentQueueIsSeperate)
            {
                DXGI_SWAP_CHAIN_DESC1 swapChainDesc;
                _device.ThrowIfFailed(_swapChain.Ptr->GetDesc1(&swapChainDesc));
                var desc = new TextureDesc
                {
                    ClearValue = null,
                    DepthOrArraySize = Helpers.Int32ToBool(swapChainDesc.Stereo) ? 2 : 1,
                    Width = swapChainDesc.Width,
                    Height = swapChainDesc.Height,
                    Dimension = TextureDimension.Tex2D,
                    Format = (DataFormat)swapChainDesc.Format,
                    Layout = TextureLayout.Optimal,
                    MipCount = 1,
                    ResourceFlags = ResourceFlags.AllowRenderTarget
                };


                for (var i = 0U; i < _desc.BackBufferCount; i++)
                {
                    _backBuffers[i] = _device.Allocator.AllocateTexture(desc, ResourceState.Present, AllocFlags.ForceAllocateComitted);
                }
            }

            _backBufferIndex = _swapChain.Ptr->GetCurrentBackBufferIndex();
            CreateViews();
        }



        internal override void InternalPartialPresent(in GpuTask presentAfter, System.ReadOnlySpan<Rectangle> dirtyRects, Rectangle scrollRect, Point scrollOffset)
        {
            _presentQueue.Wait(presentAfter);

            fixed (Rectangle* pDirty = dirtyRects)
            {
                var present = new DXGI_PRESENT_PARAMETERS
                {
                    DirtyRectsCount = (uint)dirtyRects.Length,
                    pDirtyRects = (RECT*)pDirty,
                    pScrollOffset = (POINT*)&scrollOffset,
                    pScrollRect = (RECT*)&scrollRect,
                };

                _device.ThrowIfFailed(_swapChain.Ptr->Present1(_desc.SyncInterval, 0, &present));
            }
        }

        internal override void InternalPresent(in GpuTask presentAfter)
        {
            _presentQueue.Wait(presentAfter);

            _device.ThrowIfFailed(_swapChain.Ptr->Present(_desc.SyncInterval, 0));
        }
    }
}
