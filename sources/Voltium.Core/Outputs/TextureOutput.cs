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
using System.Buffers;

namespace Voltium.Core.Devices
{
    /// <summary>
    /// An output that displays graphics to the user
    /// </summary>
    public unsafe class TextureOutput
    {
        private struct BackBufferBuffer5
        {
            public static readonly uint MaxBufferCount = 8;

#pragma warning disable CS0649
            public Texture E0;
            public Texture E1;
            public Texture E2;
            public Texture E3;
            public Texture E4;
            public Texture E5;
            public Texture E6;
            public Texture E7;
#pragma warning restore CS0649

            public ref Texture this[uint index]
                => ref Unsafe.Add(ref GetPinnableReference(), (int)index);

            public ref Texture GetPinnableReference()
                => ref MemoryMarshal.GetReference(MemoryMarshal.CreateSpan(ref E0, 0));
        }

        private OutputConfiguration _desc;
        private GraphicsDevice _device;

        //private IBufferWriter<byte>? _bufferWriter;

        private ComPtr<IDXGISwapChain3> _swapChain;
        private BackBufferBuffer5 _backBuffers;
        private uint _backBufferIndex;

        /// <summary>
        /// The <see cref="OutputConfiguration"/> used 
        /// </summary>
        public OutputConfiguration Configuration => _desc;

        private TextureOutput(GraphicsDevice device, OutputConfiguration desc)
        {
            _device = device;
            _desc = desc;

            CreateTexturesFromBuffers();
        }

        private TextureOutput(GraphicsDevice device, ComPtr<IDXGISwapChain1> swapChain, OutputConfiguration desc)
        {
            _device = device;

            if (!swapChain.TryQueryInterface(out ComPtr<IDXGISwapChain3> swapChain3))
            {
                ThrowHelper.ThrowPlatformNotSupportedException("Couldn't create IDXGISwapChain3, which is required");
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
        /// Creates a new <see cref="TextureOutput"/> to a <see cref="IOutputOwner"/>
        /// </summary>
        /// <param name="device">The <see cref="GraphicsDevice"/> that will output to this buffer</param>
        /// <param name="desc">The <see cref="OutputConfiguration"/> for this output</param>
        /// <param name="window">The <see cref="IOutputOwner"/> that owns the window</param>
        /// <param name="outputArea">Optionally, the <see cref="Size"/> of the rendered output. By default, this will be the entire window</param>
        /// <returns>A new <see cref="TextureOutput"/></returns>
        public static TextureOutput Create(GraphicsDevice device, OutputConfiguration desc, IOutputOwner window, Size outputArea = default)
        {
            return window.Type switch
            {
                OutputType.Hwnd => CreateForWin32(device, desc, window.GetOutput(), outputArea),
                OutputType.ICoreWindow => CreateForWinRT(device, desc, (void*)window.GetOutput(), outputArea),
                _ => throw new ArgumentOutOfRangeException(nameof(window))
            };
        }

        /// <summary>
        /// Creates a new <see cref="TextureOutput"/> to a Win32 Window backed by a HWND
        /// </summary>u
        /// <param name="device">The <see cref="GraphicsDevice"/> that will output to this buffer</param>
        /// <param name="desc">The <see cref="OutputConfiguration"/> for this output</param>
        /// <param name="window">The <see cref="IHwndOwner"/> that owns the window</param>
        /// <param name="outputArea">Optionally, the <see cref="Size"/> of the rendered output. By default, this will be the entire window</param>
        /// <returns>A new <see cref="TextureOutput"/></returns>
        public static TextureOutput CreateForWin32(GraphicsDevice device, OutputConfiguration desc, IHwndOwner window, Size outputArea = default)
            => CreateForWin32(device, desc, window.GetHwnd(), outputArea);

        /// <summary>
        /// Creates a new <see cref="TextureOutput"/> to a Win32 Window backed by a HWND
        /// </summary>
        /// <param name="device">The <see cref="GraphicsDevice"/> that will output to this buffer</param>
        /// <param name="desc">The <see cref="OutputConfiguration"/> for this output</param>
        /// <param name="window">The HWND for the window to bind to</param>
        /// <param name="outputArea">Optionally, the <see cref="Size"/> of the rendered output. By default, this will be the entire window</param>
        /// <returns>A new <see cref="TextureOutput"/></returns>
        public static TextureOutput CreateForWin32(GraphicsDevice device, OutputConfiguration desc, IntPtr window, Size outputArea = default)
        {
            var swapChainDesc = CreateDesc(desc, outputArea);

            using ComPtr<IDXGIFactory2> factory = CreateFactory(device);

            using ComPtr<IDXGISwapChain1> swapChain = default;

            Guard.ThrowIfFailed(factory.Get()->CreateSwapChainForHwnd(
                device.GetGraphicsQueue(),
                window,
                &swapChainDesc,
                null, //&fullscreenDesc,
                null, // TODO maybe implement
                ComPtr.GetAddressOf(&swapChain)
            ));

            var output = new TextureOutput(device, swapChain.Move(), desc);

            return output;
        }


        /// <summary>
        /// Creates a new <see cref="TextureOutput"/> to a WinRT ICoreWindow
        /// </summary>
        /// <param name="device">The <see cref="GraphicsDevice"/> that will output to this buffer</param>
        /// <param name="desc">The <see cref="OutputConfiguration"/> for this output</param>
        /// <param name="window">The <see cref="ICoreWindowsOwner"/> that owns the window</param>
        /// <param name="outputArea">Optionally, the <see cref="Size"/> of the rendered output. By default, this will be the entire window</param>
        /// <returns>A new <see cref="TextureOutput"/></returns>
        public static TextureOutput CreateForWinRT(GraphicsDevice device, OutputConfiguration desc, ICoreWindowsOwner window, Size outputArea = default)
            => CreateForWinRT(device, desc, window.GetIUnknownForWindow(), outputArea);

        /// <summary>
        /// Creates a new <see cref="TextureOutput"/> to a WinRT ICoreWindow
        /// </summary>
        /// <param name="device">The <see cref="GraphicsDevice"/> that will output to this buffer</param>
        /// <param name="desc">The <see cref="OutputConfiguration"/> for this output</param>
        /// <param name="window">The IUnknown* for the window to bind to</param>
        /// <param name="outputArea">Optionally, the <see cref="Size"/> of the rendered output. By default, this will be the entire window</param>
        /// <returns>A new <see cref="TextureOutput"/></returns>
        public static TextureOutput CreateForWinRT(GraphicsDevice device, OutputConfiguration desc, void* window, Size outputArea = default)
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


            var output = new TextureOutput(device, swapChain.Move(), desc);

            return output;
        }


        /// <summary>
        /// Creates a new <see cref="TextureOutput"/> to a WinRT ISwapChainPanelNative
        /// </summary>
        /// <param name="device">The <see cref="GraphicsDevice"/> that will output to this buffer</param>
        /// <param name="desc">The <see cref="OutputConfiguration"/> for this output</param>
        /// <param name="swapChainPanelNative">The IUnknown* for the ISwapChainPanelNative to bind to</param>
        /// <param name="outputArea">The <see cref="Size"/> of the rendered output</param>
        /// <returns>A new <see cref="TextureOutput"/></returns>
        public static TextureOutput CreateForSwapChainPanel(GraphicsDevice device, OutputConfiguration desc, void* swapChainPanelNative, Size outputArea)
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

            var output = new TextureOutput(device, swapChain.Move(), desc);

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
        /// The current output buffer texture
        /// </summary>
        public Texture OutputBuffer => _backBuffers[_backBufferIndex];


        /// <summary>
        /// Retrieves the dimensions of the output texture
        /// </summary>
        public Size GetDimensions2D()
        {
            if (_swapChain.Exists)
            {
                DXGI_SWAP_CHAIN_DESC1 desc;
                Guard.ThrowIfFailed(_swapChain.Get()->GetDesc1(&desc));
                if (desc.Stereo == TRUE)
                {
                    ThrowHelper.ThrowNotSupportedException("Output does not use a 2D swap chain outpu");
                }

                return new Size((int)desc.Width, (int)desc.Height);
            }
            else
            {
                ThrowHelper.ThrowNotSupportedException("Output does not use a 2D swap chain outpu");
                return default;
            }
        }

        /// <summary>
        /// Retrieves the dimensions of the output texture
        /// </summary>
        /// <param name="dimension">The <see cref="TextureDimension"/> of the output. This is always <see cref="TextureDimension.Tex2D"/> if this output has a swap chain</param>
        /// <param name="width">The width of the output, in pixels</param>
        /// <param name="height">If <paramref name="dimension"/> is <see cref="TextureDimension.Tex2D"/> or <see cref="TextureDimension.Tex3D"/>, the height of the output, in pixels</param>
        /// <param name="depthOrArraySize">If <paramref name="dimension"/> is <see cref="TextureDimension.Tex1D"/> or <see cref="TextureDimension.Tex2D"/>, the number of textures in the texture array of the output.
        /// Else, the depth of the output texture</param>
        public void GetDimensions(out TextureDimension dimension, out uint width, out uint? height, out uint? depthOrArraySize)
        {
            if (_swapChain.Exists)
            {
                DXGI_SWAP_CHAIN_DESC1 desc;
                Guard.ThrowIfFailed(_swapChain.Get()->GetDesc1(&desc));
                dimension = TextureDimension.Tex2D;
                width = desc.Width;
                height = desc.Height;
                depthOrArraySize = desc.Stereo == TRUE ? 2U : 1;

                return;
            }
            else
            {
                ThrowHelper.ThrowNotImplementedException();
                throw null;
            }
        }

        /// <summary>
        /// Presents the current back buffer to the output, and advances to the next back buffer
        /// </summary>
        public void Present()
        {
            Guard.ThrowIfFailed(_swapChain.Get()->Present(_desc.SyncInterval, 0));
            _backBufferIndex = (_backBufferIndex + 1) % _desc.BackBufferCount;
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
                   DXGI_FORMAT.DXGI_FORMAT_UNKNOWN, // preserve existing format
                   0
            ));
        }

        /// <inheritdoc cref="IDisposable"/>
        public void Dispose() => _swapChain.Dispose();
    }
}
