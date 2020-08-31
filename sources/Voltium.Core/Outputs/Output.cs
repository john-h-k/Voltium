using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using TerraFX.Interop;
using Voltium.Common;
using Voltium.Core.Infrastructure;
using Voltium.Core.Devices;
using Voltium.Extensions;
using Voltium.Core.Memory;
using static TerraFX.Interop.Windows;
using Rectangle = System.Drawing.Rectangle;
using System.Buffers;

namespace Voltium.Core.Devices
{
    /// <summary>
    /// An output that displays graphics to the user
    /// </summary>
    public unsafe partial class Output
    {
        [FixedBufferType(typeof(Texture), 8)]
        private partial struct BackBufferBuffer8 { }

        [FixedBufferType(typeof(DescriptorHandle), 8)]
        private partial struct DescriptorHandleBuffer8 { }

        private OutputConfiguration _desc;
        private GraphicsDevice _device;

        //private IBufferWriter<byte>? _bufferWriter;

        private DescriptorHeap _viewHeap;

        private ComPtr<IDXGISwapChain3> _swapChain;
        private BackBufferBuffer8 _backBuffers;
        private DescriptorHandleBuffer8 _views;
        private uint _backBufferIndex;

        /// <summary>
        /// The <see cref="OutputConfiguration"/> used 
        /// </summary>
        public OutputConfiguration Configuration => _desc;

        /// <summary>
        /// Resize the render resources
        /// </summary>
        /// <param name="newSize">The <see cref="Size"/> indicating the size to resize to</param>
        public void Resize(Size newSize)
        {
            if (newSize == Dimensions)
            {
                return;
            }

            _device.Idle();
            Dimensions = newSize;
            AspectRatio = Dimensions.AspectRatio();

            ResizeBuffers(newSize);
            CreateTexturesFromBuffers();
            CreateViews();
        }

        /// <summary>
        /// The <see cref="Size"/> of the output
        /// </summary>
        public Size Dimensions { get; private set; }

        /// <summary>
        /// The aspect ratio of the output
        /// </summary>
        public float AspectRatio { get; private set; }

        /// <summary>
        /// The number of output buffers
        /// </summary>
        public uint OutputBufferCount => _desc.BackBufferCount;

        /// <summary>
        /// The current output buffer texture
        /// </summary>
        public Texture OutputBuffer => _backBuffers[_backBufferIndex];


        /// <summary>
        /// The current output buffer texture view
        /// </summary>
        public DescriptorHandle OutputBufferView => _views[_backBufferIndex];

        private Output(GraphicsDevice device, OutputConfiguration desc)
        {
            _device = device;
            _desc = desc;

            CreateTexturesFromBuffers();

            // need to create views etc
            throw new NotImplementedException();
        }

        private Output(GraphicsDevice device, ComPtr<IDXGISwapChain1> swapChain, OutputConfiguration desc)
        {
            _device = device;

            if (!swapChain.TryQueryInterface(out ComPtr<IDXGISwapChain3> swapChain3))
            {
                ThrowHelper.ThrowPlatformNotSupportedException("Couldn't create IDXGISwapChain3, which is required");
            }

            DXGI_SWAP_CHAIN_DESC1 swapChainDesc;
            _device.ThrowIfFailed(swapChain3.Ptr->GetDesc1(&swapChainDesc));
            Dimensions = new Size((int)swapChainDesc.Width, (int)swapChainDesc.Height);
            AspectRatio = Dimensions.AspectRatio();

            _desc = desc;
            _swapChain = swapChain3.Move();

            CreateTexturesFromBuffers();
            CreateViews();
        }

        private void CreateViews()
        {
            // Create or reset the heap
            if (!_viewHeap.Exists)
            {
                _viewHeap = DescriptorHeap.Create(_device, DescriptorHeapType.RenderTargetView, Configuration.BackBufferCount);
            }
            else
            {
                _viewHeap.ResetHeap();
            }


            for (var i = 0u; i < Configuration.BackBufferCount; i++)
            {
                _views[i] = _device.CreateRenderTargetView(_backBuffers[i], _viewHeap.GetNextHandle());
            }
        }

        private void CreateTexturesFromBuffers()
        {
            for (var i = 0U; i < _desc.BackBufferCount; i++)
            {
                using ComPtr<ID3D12Resource> buffer = default;
                _device.ThrowIfFailed(_swapChain.Ptr->GetBuffer(i, buffer.Iid, (void**)&buffer));
                DebugHelpers.SetName(buffer.Ptr, $"BackBuffer #{i}");

                _backBuffers[i] = Texture.FromResource(_device, buffer.Move());
            }

            _backBufferIndex = _swapChain.Ptr->GetCurrentBackBufferIndex();
        }

        /// <summary>
        /// Creates a new <see cref="Output"/> to a <see cref="IOutputOwner"/>
        /// </summary>
        /// <param name="desc">The <see cref="OutputConfiguration"/> for this output</param>
        /// <param name="device">The <see cref="GraphicsDevice"/> that will output to this buffer</param>
        /// <param name="window">The <see cref="IOutputOwner"/> that owns the window</param>
        /// <param name="outputArea">Optionally, the <see cref="Size"/> of the rendered output. By default, this will be the entire window</param>
        /// <returns>A new <see cref="Output"/></returns>
        public static Output Create(OutputConfiguration desc, GraphicsDevice device, IOutputOwner window, Size outputArea = default)
        {
            return window.Type switch
            {
                OutputType.Hwnd => CreateForWin32(device, desc, window.GetOutput(), outputArea),
                OutputType.ICoreWindow => CreateForWinRT(device, desc, (void*)window.GetOutput(), outputArea),
                _ => throw new ArgumentOutOfRangeException(nameof(window))
            };
        }

        /// <summary>
        /// Creates a new <see cref="Output"/> to a Win32 Window backed by a HWND
        /// </summary>u
        /// <param name="device">The <see cref="GraphicsDevice"/> that will output to this buffer</param>
        /// <param name="desc">The <see cref="OutputConfiguration"/> for this output</param>
        /// <param name="window">The <see cref="IHwndOwner"/> that owns the window</param>
        /// <param name="outputArea">Optionally, the <see cref="Size"/> of the rendered output. By default, this will be the entire window</param>
        /// <returns>A new <see cref="Output"/></returns>
        public static Output CreateForWin32(GraphicsDevice device, OutputConfiguration desc, IHwndOwner window, Size outputArea = default)
            => CreateForWin32(device, desc, window.GetHwnd(), outputArea);

        /// <summary>
        /// Creates a new <see cref="Output"/> to a Win32 Window backed by a HWND
        /// </summary>
        /// <param name="device">The <see cref="GraphicsDevice"/> that will output to this buffer</param>
        /// <param name="desc">The <see cref="OutputConfiguration"/> for this output</param>
        /// <param name="window">The HWND for the window to bind to</param>
        /// <param name="outputArea">Optionally, the <see cref="Size"/> of the rendered output. By default, this will be the entire window</param>
        /// <returns>A new <see cref="Output"/></returns>
        public static Output CreateForWin32(GraphicsDevice device, OutputConfiguration desc, IntPtr window, Size outputArea = default)
        {
            var swapChainDesc = CreateDesc(desc, outputArea);

            using ComPtr<IDXGIFactory2> factory = CreateFactory();

            using ComPtr<IDXGISwapChain1> swapChain = default;

            device.ThrowIfFailed(factory.Ptr->CreateSwapChainForHwnd(
                device.GetGraphicsQueue(),
                window,
                &swapChainDesc,
                null, //&fullscreenDesc,
                null, // TODO maybe implement
                ComPtr.GetAddressOf(&swapChain)
            ));

            var output = new Output(device, swapChain.Move(), desc);

            return output;
        }


        /// <summary>
        /// Creates a new <see cref="Output"/> to a WinRT ICoreWindow
        /// </summary>
        /// <param name="device">The <see cref="GraphicsDevice"/> that will output to this buffer</param>
        /// <param name="desc">The <see cref="OutputConfiguration"/> for this output</param>
        /// <param name="window">The <see cref="ICoreWindowsOwner"/> that owns the window</param>
        /// <param name="outputArea">Optionally, the <see cref="Size"/> of the rendered output. By default, this will be the entire window</param>
        /// <returns>A new <see cref="Output"/></returns>
        public static Output CreateForWinRT(GraphicsDevice device, OutputConfiguration desc, ICoreWindowsOwner window, Size outputArea = default)
            => CreateForWinRT(device, desc, window.GetIUnknownForWindow(), outputArea);

        /// <summary>
        /// Creates a new <see cref="Output"/> to a WinRT ICoreWindow
        /// </summary>
        /// <param name="device">The <see cref="GraphicsDevice"/> that will output to this buffer</param>
        /// <param name="desc">The <see cref="OutputConfiguration"/> for this output</param>
        /// <param name="window">The IUnknown* for the window to bind to</param>
        /// <param name="outputArea">Optionally, the <see cref="Size"/> of the rendered output. By default, this will be the entire window</param>
        /// <returns>A new <see cref="Output"/></returns>
        public static Output CreateForWinRT(GraphicsDevice device, OutputConfiguration desc, void* window, Size outputArea = default)
        {
            var swapChainDesc = CreateDesc(desc, outputArea);

            using ComPtr<IDXGIFactory2> factory = CreateFactory();

            using ComPtr<IDXGISwapChain1> swapChain = default;

            device.ThrowIfFailed(factory.Ptr->CreateSwapChainForCoreWindow(
                device.GetGraphicsQueue(),
                (IUnknown*)window,
                &swapChainDesc,
                null, // TODO maybe implement
                ComPtr.GetAddressOf(&swapChain)
            ));


            var output = new Output(device, swapChain.Move(), desc);

            return output;
        }


        /// <summary>
        /// Creates a new <see cref="Output"/> to a WinRT ISwapChainPanelNative
        /// </summary>
        /// <param name="device">The <see cref="GraphicsDevice"/> that will output to this buffer</param>
        /// <param name="desc">The <see cref="OutputConfiguration"/> for this output</param>
        /// <param name="swapChainPanelNative">The IUnknown* for the ISwapChainPanelNative to bind to</param>
        /// <param name="outputArea">The <see cref="Size"/> of the rendered output</param>
        /// <returns>A new <see cref="Output"/></returns>
        public static Output CreateForSwapChainPanel(GraphicsDevice device, OutputConfiguration desc, void* swapChainPanelNative, Size outputArea)
        {
            var swapChainDesc = CreateDesc(desc, outputArea);

            using ComPtr<IDXGIFactory2> factory = CreateFactory();

            using ComPtr<IDXGISwapChain1> swapChain = default;

            device.ThrowIfFailed(factory.Ptr->CreateSwapChainForComposition(
                device.GetGraphicsQueue(),
                &swapChainDesc,
                null, // TODO maybe implement
                ComPtr.GetAddressOf(&swapChain)
            ));

            device.ThrowIfFailed(((ISwapChainPanelNative*)swapChainPanelNative)->SetSwapChain((IDXGISwapChain*)swapChain.Ptr));

            var output = new Output(device, swapChain.Move(), desc);

            return output;
        }

        private static ComPtr<IDXGIFactory2> CreateFactory()
        {
            using ComPtr<IDXGIFactory2> factory = default;

            int hr = CreateDXGIFactory2(DXGI_CREATE_FACTORY_DEBUG, factory.Iid, (void**)&factory);
            
            if (hr == E_NOINTERFACE)
            {
                // we don't actually *need* IDXGIFactory2, we just need to do CreateSwapChain (rather than CreateSwapChainForHwnd etc) without it which is currently not implemented
                ThrowHelper.ThrowPlatformNotSupportedException("Platform does not support IDXGIFactory2, which is required");
            }

            Guard.ThrowIfFailed(hr, "CreateDXGIFactory2(DXGI_CREATE_FACTORY_DEBUG, factory.Iid, (void**)&factory)");

            return factory.Move();
        }

        private static DXGI_SWAP_CHAIN_DESC1 CreateDesc(OutputConfiguration desc, Size outputArea)
        {
            if (desc.BackBufferCount > BackBufferBuffer8.BufferLength)
            {
                ThrowHelper.ThrowArgumentException($"Cannot have more than {BackBufferBuffer8.BufferLength} back buffers");
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
        /// Presents the current back buffer to the output, and advances to the next back buffer
        /// </summary>
        public void Present()
        {
            _device.ThrowIfFailed(_swapChain.Ptr->Present(_desc.SyncInterval, 0));
            _backBufferIndex = (_backBufferIndex + 1) % _desc.BackBufferCount;
        }

        internal void ResizeBuffers(Size newSize)
        {
            for (var i = 0U; i < BackBufferBuffer8.BufferLength; i++)
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
        }

        /// <inheritdoc cref="IDisposable"/>
        public void Dispose() => _swapChain.Dispose();
    }
}
