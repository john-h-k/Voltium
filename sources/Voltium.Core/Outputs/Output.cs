using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using TerraFX.Interop;
using Voltium.Common;
using Voltium.Core.Infrastructure;
using Voltium.Core.Devices;
using Voltium.Core.Memory;
using static TerraFX.Interop.Windows;
using Rectangle = System.Drawing.Rectangle;

namespace Voltium.Core.Devices
{
    /// <summary>
    /// Describes a <see cref="Output"/>
    /// </summary>
    public struct OutputConfiguration
    {
        /// <summary>
        /// The number of buffers the swapchain should contain
        /// </summary>
        public uint BackBufferCount;

        /// <summary>
        /// The <see cref="BackBufferFormat"/> for the back buffer
        /// </summary>
        public BackBufferFormat BackBufferFormat;

        /// <summary>
        /// The sync interval to use when presenting
        /// </summary>
        public uint SyncInterval;
    }


    /// <summary>
    /// An output that displays graphics to the user
    /// </summary>
    public unsafe class Output
    {
        private struct BackBufferBuffer5
        {
            public static readonly uint MaxBufferCount = 5;

#pragma warning disable CS0649
            public Texture E0;
            public Texture E1;
            public Texture E2;
            public Texture E3;
            public Texture E4;
#pragma warning restore CS0649

            public ref Texture this[uint index]
                => ref Unsafe.Add(ref GetPinnableReference(), (int)index);

            public ref Texture GetPinnableReference()
                => ref MemoryMarshal.GetReference(MemoryMarshal.CreateSpan(ref E0, 0));
        }

        private OutputConfiguration _desc;
        private GraphicsDevice _device;

        private ComPtr<IDXGISwapChain3> _swapChain;
        private BackBufferBuffer5 _backBuffers;
        private uint _backBufferIndex;

        private bool _implicitExecuteOnPresent;

        //private IntPtr _hwnd;
        //private IUnknown* _window;
        //private Stream? _output;

        private Output(GraphicsDevice device, OutputConfiguration desc, bool implicitExecuteOnPresent)
        {
            _device = device;
            _desc = desc;
            _implicitExecuteOnPresent = implicitExecuteOnPresent;

            CreateTexturesFromBuffers();
        }

        private Output(GraphicsDevice device, ComPtr<IDXGISwapChain1> swapChain, OutputConfiguration desc, bool implicitExecuteOnPresent)
        {
            _device = device;
            _implicitExecuteOnPresent = implicitExecuteOnPresent;

            if (!swapChain.TryQueryInterface(out ComPtr<IDXGISwapChain3> swapChain3))
            {
                ThrowHelper.ThrowPlatformNotSupportedException("Couldn't create IDXGISwapChain3, which is required for DX12");
            }

            _desc = desc;
            _swapChain = swapChain3.Move();

            CreateTexturesFromBuffers();
        }

        private void CreateTexturesFromBuffers()
        {
            for (var i = 0U; i < _desc.BackBufferCount; i++)
            {
                using ComPtr<ID3D12Resource> buffer = default;
                Guard.ThrowIfFailed(_swapChain.Get()->GetBuffer(i, buffer.Iid, ComPtr.GetVoidAddressOf(&buffer)));
                DebugHelpers.SetName(buffer.Get(), $"BackBuffer #{i}");

                _backBufferIndex = _swapChain.Get()->GetCurrentBackBufferIndex();
                _backBuffers[i] = Texture.FromResource(_device, buffer.Move());
            }
        }

        /// <summary>
        /// Creates a new <see cref="Output"/> to a <see cref="IOutputOwner"/>
        /// </summary>
        /// <param name="device">The <see cref="GraphicsDevice"/> that will output to this buffer</param>
        /// <param name="desc">The <see cref="OutputConfiguration"/> for this output</param>
        /// <param name="window">The <see cref="IOutputOwner"/> that owns the window</param>
        /// <param name="outputArea">Optionally, the <see cref="Size"/> of the rendered output. By default, this will be the entire window</param>
        /// <param name="implicitExecuteOnPresent">Whether <see cref="GraphicsDevice.Execute()"/> should be called each time <see cref="Present"/> is called</param>
        /// <returns>A new <see cref="Output"/></returns>
        public static Output Create(GraphicsDevice device, OutputConfiguration desc, IOutputOwner window, Size outputArea = default, bool implicitExecuteOnPresent = false)
        {
            return window.Type switch
            {
                OutputType.Hwnd => CreateForWin32(device, desc, window.GetOutput(), outputArea, implicitExecuteOnPresent),
                OutputType.ICoreWindow => CreateForWinRT(device, desc, (void*)window.GetOutput(), outputArea, implicitExecuteOnPresent),
                _ => throw new ArgumentOutOfRangeException(nameof(window))
            };
        }


        /// <summary>
        /// Creates a new <see cref="Output"/> to a Win32 Window backed by a HWND
        /// </summary>
        /// <param name="device">The <see cref="GraphicsDevice"/> that will output to this buffer</param>
        /// <param name="desc">The <see cref="OutputConfiguration"/> for this output</param>
        /// <param name="window">The <see cref="IHwndOwner"/> that owns the window</param>
        /// <param name="outputArea">Optionally, the <see cref="Size"/> of the rendered output. By default, this will be the entire window</param>
        /// <param name="implicitExecuteOnPresent">Whether <see cref="GraphicsDevice.Execute()"/> should be called each time <see cref="Present"/> is called</param>
        /// <returns>A new <see cref="Output"/></returns>
        public static Output CreateForWin32(GraphicsDevice device, OutputConfiguration desc, IHwndOwner window, Size outputArea = default, bool implicitExecuteOnPresent = false)
            => CreateForWin32(device, desc, window.GetHwnd(), outputArea, implicitExecuteOnPresent);

        /// <summary>
        /// Creates a new <see cref="Output"/> to a Win32 Window backed by a HWND
        /// </summary>
        /// <param name="device">The <see cref="GraphicsDevice"/> that will output to this buffer</param>
        /// <param name="desc">The <see cref="OutputConfiguration"/> for this output</param>
        /// <param name="window">The HWND for the window to bind to</param>
        /// <param name="outputArea">Optionally, the <see cref="Size"/> of the rendered output. By default, this will be the entire window</param>
        /// <param name="implicitExecuteOnPresent">Whether <see cref="GraphicsDevice.Execute()"/> should be called each time <see cref="Present"/> is called</param>
        /// <returns>A new <see cref="Output"/></returns>
        public static Output CreateForWin32(GraphicsDevice device, OutputConfiguration desc, IntPtr window, Size outputArea = default, bool implicitExecuteOnPresent = false)
        {
            var swapChainDesc = CreateDesc(desc, outputArea);

            using ComPtr<IDXGIFactory2> factory = CreateFactory(device);

            using ComPtr<IDXGISwapChain1> swapChain = default;

            _ = factory.Get();

            Guard.ThrowIfFailed(factory.Get()->CreateSwapChainForHwnd(
                device.GetGraphicsQueue(),
                window,
                &swapChainDesc,
                null, //&fullscreenDesc,
                null, // TODO maybe implement
                ComPtr.GetAddressOf(&swapChain)
            ));

            var output = new Output(device, swapChain.Move(), desc, implicitExecuteOnPresent);

            return output;
        }


        /// <summary>
        /// Creates a new <see cref="Output"/> to a WinRT ICoreWindow
        /// </summary>
        /// <param name="device">The <see cref="GraphicsDevice"/> that will output to this buffer</param>
        /// <param name="desc">The <see cref="OutputConfiguration"/> for this output</param>
        /// <param name="window">The <see cref="ICoreWindowsOwner"/> that owns the window</param>
        /// <param name="outputArea">Optionally, the <see cref="Size"/> of the rendered output. By default, this will be the entire window</param>
        /// <param name="implicitExecuteOnPresent">Whether <see cref="GraphicsDevice.Execute()"/> should be called each time <see cref="Present"/> is called</param>
        /// <returns>A new <see cref="Output"/></returns>
        public static Output CreateForWinRT(GraphicsDevice device, OutputConfiguration desc, ICoreWindowsOwner window, Size outputArea = default, bool implicitExecuteOnPresent = false)
            => CreateForWinRT(device, desc, window.GetIUnknownForWindow(), outputArea, implicitExecuteOnPresent);

        /// <summary>
        /// Creates a new <see cref="Output"/> to a WinRT ICoreWindow
        /// </summary>
        /// <param name="device">The <see cref="GraphicsDevice"/> that will output to this buffer</param>
        /// <param name="desc">The <see cref="OutputConfiguration"/> for this output</param>
        /// <param name="window">The IUnknown* for the window to bind to</param>
        /// <param name="outputArea">Optionally, the <see cref="Size"/> of the rendered output. By default, this will be the entire window</param>
        /// <param name="implicitExecuteOnPresent">Whether <see cref="GraphicsDevice.Execute()"/> should be called each time <see cref="Present"/> is called</param>
        /// <returns>A new <see cref="Output"/></returns>
        public static Output CreateForWinRT(GraphicsDevice device, OutputConfiguration desc, void* window, Size outputArea = default, bool implicitExecuteOnPresent = false)
        {
            var swapChainDesc = CreateDesc(desc, outputArea);

            using ComPtr<IDXGIFactory2> factory = CreateFactory(device);

            using ComPtr<IDXGISwapChain1> swapChain = default;

            Guard.ThrowIfFailed(factory.Get()->CreateSwapChainForCoreWindow(
                device.GetGraphicsQueue(),
                (IUnknown*)window,
                &swapChainDesc,
                null, // TODO maybe implement
                ComPtr.GetAddressOf(&swapChain)
            ));


            var output = new Output(device, swapChain.Move(), desc, implicitExecuteOnPresent);

            return output;
        }


        /// <summary>
        /// Creates a new <see cref="Output"/> to a WinRT ISwapChainPanelNative
        /// </summary>
        /// <param name="device">The <see cref="GraphicsDevice"/> that will output to this buffer</param>
        /// <param name="desc">The <see cref="OutputConfiguration"/> for this output</param>
        /// <param name="swapChainPanelNative">The IUnknown* for the ISwapChainPanelNative to bind to</param>
        /// <param name="outputArea">The <see cref="Size"/> of the rendered output</param>
        /// <param name="implicitExecuteOnPresent">Whether <see cref="GraphicsDevice.Execute()"/> should be called each time <see cref="Present"/> is called</param>
        /// <returns>A new <see cref="Output"/></returns>
        public static Output CreateForSwapChainPanel(GraphicsDevice device, OutputConfiguration desc, void* swapChainPanelNative, Size outputArea, bool implicitExecuteOnPresent = false)
        {
            var swapChainDesc = CreateDesc(desc, outputArea);

            using ComPtr<IDXGIFactory2> factory = CreateFactory(device);

            using ComPtr<IDXGISwapChain1> swapChain = default;

            Guard.ThrowIfFailed(factory.Get()->CreateSwapChainForComposition(
                device.GetGraphicsQueue(),
                &swapChainDesc,
                null, // TODO maybe implement
                ComPtr.GetAddressOf(&swapChain)
            ));

            Guard.ThrowIfFailed(((ISwapChainPanelNative*)swapChainPanelNative)->SetSwapChain((IDXGISwapChain*)swapChain.Get()));

            var output = new Output(device, swapChain.Move(), desc, implicitExecuteOnPresent);

            return output;
        }

        /// <summary>
        /// Resize the render resources
        /// </summary>
        /// <param name="newSize">The <see cref="Size"/> indicating the size to resize to</param>
        public void Resize(Size newSize)
        {
            _device.Idle();

            ResizeBuffers(newSize);
            CreateTexturesFromBuffers();
        }

        private static ComPtr<IDXGIFactory2> CreateFactory(GraphicsDevice device)
        {
            using ComPtr<IDXGIFactory2> factory = default;

            // Try get the factory from the device if possible. Won't work if the device is a IDXCoreAdapter tho, then we fallback to manual creation
            int hr;
            if (device.Adapter.UnderlyingAdapter is not null && ComPtr.TryQueryInterface(device.Adapter.UnderlyingAdapter, out IDXGIAdapter* dxgiAdapter))
            {
                hr = dxgiAdapter->GetParent(factory.Iid, ComPtr.GetVoidAddressOf(&factory));
                _ = dxgiAdapter->Release();
            }
            else
            {
                hr = CreateDXGIFactory1(factory.Iid, ComPtr.GetVoidAddressOf(&factory));
            }

            if (hr == E_NOINTERFACE)
            {
                // we don't actually *need* IDXGIFactory2, we just need to do CreateSwapChain (rather than CreateSwapChainForHwnd etc) without it which is currently not implemented
                ThrowHelper.ThrowPlatformNotSupportedException("Platform does not support IDXGIFactory2, which is required");
            }

            Guard.ThrowIfFailed(hr);

            return factory.Move();
        }

        private static DXGI_SWAP_CHAIN_DESC1 CreateDesc(OutputConfiguration desc, Size outputArea)
        {
            if (desc.BackBufferCount > BackBufferBuffer5.MaxBufferCount)
            {
                ThrowHelper.ThrowArgumentException($"Cannot have more than {BackBufferBuffer5.MaxBufferCount} back buffers");
            }

            return new DXGI_SWAP_CHAIN_DESC1
            {
                AlphaMode = DXGI_ALPHA_MODE.DXGI_ALPHA_MODE_IGNORE, // TODO document
                BufferCount = desc.BackBufferCount,
                BufferUsage = DXGI_USAGE_RENDER_TARGET_OUTPUT, // this is the output chain
                Flags = 0,
                Format = (DXGI_FORMAT)desc.BackBufferFormat,
                Height = (uint)outputArea.Height,
                Width = (uint)outputArea.Width,
                SampleDesc = new DXGI_SAMPLE_DESC(count: 1, quality: 0), // backbuffer MSAA is not supported in D3D12
                Scaling = DXGI_SCALING.DXGI_SCALING_NONE,
                Stereo =  FALSE, // stereoscopic rendering, 2 images, e.g VR or 3D holo
                SwapEffect = DXGI_SWAP_EFFECT.DXGI_SWAP_EFFECT_FLIP_DISCARD
            };
        }

        /// <summary>
        /// The current BackBuffer
        /// </summary>
        public Texture BackBuffer => _backBuffers[_backBufferIndex];

        /// <summary>
        /// Presents the current back buffer to the output, and advances to the next back buffer
        /// </summary>
        public void Present()
        {
            if (_implicitExecuteOnPresent)
            {
                _device.Execute();
            }

            Guard.ThrowIfFailed(_swapChain.Get()->Present(_desc.SyncInterval, 0));
            _backBufferIndex = (_backBufferIndex + 1) % _desc.BackBufferCount;

            if (_implicitExecuteOnPresent)
            {
                _device.MoveToNextFrame();
            }
        }

        internal void ResizeBuffers(Size newSize)
        {
            for (var i = 0U; i < BackBufferBuffer5.MaxBufferCount; i++)
            {
                _backBuffers[i].Dispose();
            }

            Guard.ThrowIfFailed(_swapChain.Get()->ResizeBuffers(
                   0, // preserve existing number
                   (uint)newSize.Width,
                   (uint)newSize.Height,
                   0, // preserve existing format
                   0
            ));
        }

        /// <inheritdoc cref="IDisposable"/>
        public void Dispose() => _swapChain.Dispose();
    }
}
