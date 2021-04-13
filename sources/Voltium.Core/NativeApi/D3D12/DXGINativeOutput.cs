using System;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using TerraFX.Interop;
using Voltium.Common;
using Voltium.Core.Memory;
using static TerraFX.Interop.Windows;
using static TerraFX.Interop.DXGI_SWAP_CHAIN_FLAG;
using static TerraFX.Interop.DXGI_SWAP_EFFECT;
using Voltium.Core.NativeApi;

namespace Voltium.Core.Devices
{
    using ConfigVars = Common.Debugging.ConfigVars;

    public struct NativeOutputDesc
    {
        public BackBufferFormat Format;
        public uint BackBufferCount;
        public bool PreserveBackBuffers;
        public bool VrStereo;
    }

    public unsafe partial class DXGINativeOutput : INativeOutput
    {
        private static readonly UniqueComPtr<IDXGIFactory2> _factory = GetFactory();

        private static UniqueComPtr<IDXGIFactory2> GetFactory()
        {
            UniqueComPtr<IDXGIFactory2> factory = default;

            Guard.ThrowIfFailed(CreateDXGIFactory2(
                ConfigVars.IsDebug ? DXGI_CREATE_FACTORY_DEBUG : 0,
                factory.Iid,
                (void**)&factory
            ));

            return factory;
        }

        private INativeDevice _device;
        private UniqueComPtr<IDXGISwapChain3> _swapchain;
        private ViewSetHandle _viewSet;
        private TextureBuffer16 _backBuffers;
        private ViewBuffer16 _backBufferViews;
        private Win32EventTaskSource _taskSource;
        private uint _backBufferIndex;
        private uint _backBufferCount;
        private readonly object _presentLock;
        private IntPtr _presentWait;

        public uint BackBufferIndex => _backBufferIndex;
        public uint BackBufferCount => _backBufferCount;

        [FixedBufferType(typeof(TextureHandle), 16)]
        private partial struct TextureBuffer16 { }

        [FixedBufferType(typeof(ViewHandle), 16)]
        private partial struct ViewBuffer16 { }

        public DXGINativeOutput(
            INativeQueue queue,
            in NativeOutputDesc desc,
            HWND hwnd
        )
        {
            _device = queue.Device;
            if (_device is not D3D12NativeDevice)
            {
                ThrowHelper.ThrowPlatformNotSupportedException("Invalid platform (not D3D12!) for DXGI swapchain");
            }

            _presentLock = new();

            var sc = GetDesc(desc);

            using UniqueComPtr<IDXGISwapChain1> swapChain1 = default;

            Guard.ThrowIfFailed(_factory.Ptr->CreateSwapChainForHwnd(
                Unsafe.As<D3D12NativeQueue>(queue).GetQueue(),
                hwnd,
                &sc,
                null,
                null,
                (IDXGISwapChain1**)&swapChain1
            ));

            _swapchain = swapChain1.QueryInterface<IDXGISwapChain3>();
            _backBufferCount = desc.BackBufferCount;
            _backBufferIndex = _swapchain.Ptr->GetCurrentBackBufferIndex();
            _taskSource = new(true);
            _presentWait = _swapchain.Ptr->GetFrameLatencyWaitableObject();
            _viewSet = _device.CreateViewSet(_backBufferCount);

            RecreateBuffers();
        }

        private static DXGI_SWAP_CHAIN_DESC1 GetDesc(in NativeOutputDesc desc)
            => new()
            {
                AlphaMode = DXGI_ALPHA_MODE.DXGI_ALPHA_MODE_IGNORE,
                Scaling = DXGI_SCALING.DXGI_SCALING_NONE,
                BufferCount = desc.BackBufferCount,
                Format = (DXGI_FORMAT)desc.Format,
                BufferUsage = DXGI_USAGE_BACK_BUFFER,
                SampleDesc = new(1, 0),
                Stereo = Helpers.BoolToInt32(desc.VrStereo),
                Width = 0,
                Height = 0,
                SwapEffect =
                desc.PreserveBackBuffers ? DXGI_SWAP_EFFECT_FLIP_SEQUENTIAL : DXGI_SWAP_EFFECT_FLIP_DISCARD,
                Flags = (uint)(DXGI_SWAP_CHAIN_FLAG_FRAME_LATENCY_WAITABLE_OBJECT | DXGI_SWAP_CHAIN_FLAG_ALLOW_TEARING)
            };

        public OutputInfo Info { get; }

        public uint Width { get; private set; }
        public uint Height { get; private set; }
        public BackBufferFormat Format { get; private set; }
        public bool IsVrStereo { get; }

        public TextureHandle BackBuffer => _backBuffers[_backBufferIndex];

        public ViewHandle BackBufferView => _backBufferViews[_backBufferIndex];

        public void Dispose() => _swapchain.Dispose();

        public void Present(PresentFlags flags = PresentFlags.None)
        {
            lock (_presentLock)
            {
                WaitForNextPresent();
                ThrowIfFailed(_swapchain.Ptr->Present(0, (uint)flags));
                _backBufferIndex = (_backBufferIndex + 1) % _backBufferCount;
            }
        }

        public void PresentPartial(
          ReadOnlySpan<Rectangle> dirtyRects,
          Rectangle scrollRect,
          Point scrollOffset,
          PresentFlags flags = PresentFlags.None
        )
        {

            lock (_presentLock)
            {
                fixed (Rectangle* pDirty = dirtyRects)
                {
                    var @params = new DXGI_PRESENT_PARAMETERS
                    {
                        DirtyRectsCount = (uint)dirtyRects.Length,
                        pDirtyRects = (RECT*)pDirty,
                        pScrollOffset = (POINT*)&scrollOffset,
                        pScrollRect = (RECT*)&scrollRect
                    };

                    ThrowIfFailed(_swapchain.Ptr->Present1(0, (uint)flags, &@params));
                    _backBufferIndex = (_backBufferIndex + 1) % _backBufferCount;
                }
            }
        }

        public void Resize(uint width, uint height, BackBufferFormat format = 0, uint backBufferCount = 0)
        {
            lock (_presentLock)
            {
                for (var i = 0; i < _backBufferCount; i++)
                {
                    _device.DisposeTexture(_backBuffers[i]);
                }

                ThrowIfFailed(_swapchain.Ptr->ResizeBuffers(
                    backBufferCount,
                    width,
                    height,
                    (DXGI_FORMAT)format,
                    (uint)(DXGI_SWAP_CHAIN_FLAG_FRAME_LATENCY_WAITABLE_OBJECT | DXGI_SWAP_CHAIN_FLAG_ALLOW_TEARING)
                ));
                RecreateBuffers();
            }    
        }

        public bool IsReadyForNextPresent()
            => WaitForSingleObject(_presentWait, 0) == WAIT_OBJECT_0;

        public void WaitForNextPresent() => WaitForNextPresent(Timeout.InfiniteTimeSpan);
        public PresentWaitState WaitForNextPresent(TimeSpan span)
        {
            var hr = WaitForSingleObject(_presentWait, (uint)span.TotalMilliseconds);

            return hr switch
            {
                WAIT_OBJECT_0 => PresentWaitState.PresentReady,
                WAIT_TIMEOUT => PresentWaitState.Timeout,
                _ => PresentWaitState.Error
            };
        }

        public ValueTask WaitForNextPresentAsync(TimeSpan span)
        {
            _ = _taskSource.AllocateToken(_presentWait, span, out var token);
            return new ValueTask(_taskSource, token);
        }
        public ValueTask WaitForNextPresentAsync()
        {
            _ = _taskSource.AllocateToken(_presentWait, out var token);
            return new ValueTask(_taskSource, token);
        }


        private void RecreateBuffers()
        {
            for (var i = 0u; i < _backBufferCount; i++)
            {
                UniqueComPtr<ID3D12Resource> buffer = default;
                ThrowIfFailed(_swapchain.Ptr->GetBuffer(
                    i,
                    buffer.Iid,
                    (void**)&buffer
                ));

                _backBuffers[i] = Unsafe.As<D3D12NativeDevice>(_device).CreateFromPreexisting(buffer.Ptr);
                _backBufferViews[i] = Unsafe.As<D3D12NativeDevice>(_device).CreateView(_viewSet, i, _backBuffers[i]);
            }

            var desc = Unsafe.As<D3D12NativeDevice>(_device).GetMapperRef().GetInfo(_backBuffers[0]);
            Width = desc.Width;
            Height = desc.Height;
            Format = (BackBufferFormat)desc.Format;
        }


        private void ThrowIfFailed(int hr)
        {

        }

    }
}
