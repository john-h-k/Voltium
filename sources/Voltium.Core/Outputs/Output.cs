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

namespace Voltium.Core.Devices
{
    /// <summary>
    /// An output that displays graphics to the user
    /// </summary>
    public unsafe abstract partial class Output
    {
        [FixedBufferType(typeof(Texture), 16)]
        private protected partial struct BackBufferBuffer16 { }

        [FixedBufferType(typeof(DescriptorHandle), 16)]
        private protected partial struct DescriptorHandleBuffer16 { }

        private protected OutputConfiguration _desc;
        private protected GraphicsDevice _device;
        private protected DescriptorHeap _viewHeap;

        private protected BackBufferBuffer16 _backBuffers;
        private protected DescriptorHandleBuffer16 _views;
        private protected uint _backBufferIndex;

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
            if (newSize == Resolution)
            {
                return;
            }

            Resolution = newSize;
            AspectRatio = Resolution.AspectRatio();

            InternalResize(newSize);
        }

        /// <summary>
        /// The <see cref="Size"/> of the output
        /// </summary>
        public Size Resolution { get; private protected set; }

        /// <summary>
        /// The aspect ratio of the output
        /// </summary>
        public float AspectRatio { get; private protected set; }

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

        private protected Output(GraphicsDevice device, OutputConfiguration desc)
        {
            _device = device;
            _desc = desc;
        }

        private protected void CreateViews()
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
            return new SwapChainOutput(device, CreateDesc(desc, outputArea), window, desc);
        }

        //public static Output CreateForVideoOutput(GraphicsDevice device, OutputConfiguration desc, Size outputArea)
        //{
        //    throw new NotImplementedException();
        //}

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
            return new SwapChainOutput(device, CreateDesc(desc, outputArea), IOutputOwner.FromHwnd(window), desc);
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
            return new SwapChainOutput(device, CreateDesc(desc, outputArea), IOutputOwner.FromICoreWindow(window), desc);
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
            return new SwapChainOutput(device, CreateDesc(desc, outputArea), IOutputOwner.FromSwapChainPanel(swapChainPanelNative), desc);
        }

        private protected static UniqueComPtr<IDXGIFactory2> CreateFactory()
        {
            using UniqueComPtr<IDXGIFactory2> factory = default;

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

        /// <summary>
        /// Presents the current back buffer to the output, and advances to the next back buffer
        /// </summary>
        public void Present()
        {
            InternalPresent();
            _backBufferIndex = (_backBufferIndex + 1) % _desc.BackBufferCount;
        }

        internal abstract void InternalPresent();

        internal abstract void InternalResize(Size newSize);

        /// <inheritdoc cref="IDisposable"/>
        public void Dispose()
        {

        }
    }
}