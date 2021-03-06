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
using Voltium.Core.CommandBuffer;
using Voltium.Core.Configuration.Graphics;
using Voltium.Core.NativeApi;

namespace Voltium.Core.Devices
{

    public struct ViewSet
    {
        internal ViewSetHandle Handle;
        internal Disposal<ViewSetHandle> _disposal;
        public readonly uint Length;

        internal ViewSet(ViewSetHandle handle, uint length, Disposal<ViewSetHandle> disposal)
        {
            Handle = handle;
            _disposal = disposal;
            Length = length;
        }

        public void Dispose() => _disposal.Dispose(ref Handle);
    }

    /// <summary>
    /// An output that displays graphics to the user
    /// </summary>
    public unsafe sealed partial class Output
    {
        private INativeOutput _output;
        private TextureDesc _backBufferDesc;


        public static Output Create(INativeOutput output) => new(output);

        private Output(
            INativeOutput output
        )
        {
            _output = output;

            _backBufferDesc = new TextureDesc
            {
                Width = _output.Width,
                Height = _output.Height,
                DepthOrArraySize = _output.IsVrStereo ? 2 : 1,
                ClearValue = null,
                Dimension = TextureDimension.Tex2D,
                Format = (DataFormat)output.Format,
                Layout = TextureLayout.Optimal,
                MipCount = 1,
                Msaa = MsaaDesc.None,
                ResourceFlags = ResourceFlags.AllowRenderTarget
            };
        }

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

            _output.Resize((uint)newSize.Width, (uint)newSize.Height);
        }

        /// <summary>
        /// The <see cref="Size"/> of the output
        /// </summary>
        public Size Resolution { get; private set; }

        public BackBufferFormat Format => _output.Format;

        /// <summary>
        /// The aspect ratio of the output
        /// </summary>
        public float AspectRatio { get; private set; }

        /// <summary>
        /// The number of output buffers
        /// </summary>
        public uint OutputBufferCount => _output.BackBufferCount;


        [FixedBufferType(typeof(Texture), 16)]
        private partial struct TextureBuffer16 { }

        [FixedBufferType(typeof(View), 16)]
        private partial struct ViewBuffer16 { }

        /// <summary>
        /// The current output buffer texture
        /// </summary>
        public Texture OutputBuffer => new (_output.BackBuffer, _backBufferDesc, default);


        /// <summary>
        /// The current output buffer texture view
        /// </summary>
        public View OutputBufferView => new (_output.BackBufferView, default);


        ///// <summary>
        ///// Creates a new <see cref="Output"/> to a <see cref="IOutputOwner"/>
        ///// </summary>
        ///// <param name="desc">The <see cref="OutputConfiguration"/> for this output</param>
        ///// <param name="device">The <see cref="GraphicsDevice"/> that will output to this buffer</param>
        ///// <param name="window">The <see cref="IOutputOwner"/> that owns the window</param>
        ///// <param name="outputArea">Optionally, the <see cref="Size"/> of the rendered output. By default, this will be the entire window</param>
        ///// <returns>A new <see cref="Output"/></returns>
        //public static Output Create(OutputConfiguration desc, GraphicsDevice device, IOutputOwner window, Size outputArea = default)
        //{
        //    return new Output(device, outputArea, window, desc);
        //}   

        ///// <summary>
        ///// Creates a new <see cref="Output"/> to a Win32 Window backed by a HWND
        ///// </summary>u
        ///// <param name="device">The <see cref="GraphicsDevice"/> that will output to this buffer</param>
        ///// <param name="desc">The <see cref="OutputConfiguration"/> for this output</param>
        ///// <param name="window">The <see cref="IHwndOwner"/> that owns the window</param>
        ///// <param name="outputArea">Optionally, the <see cref="Size"/> of the rendered output. By default, this will be the entire window</param>
        ///// <returns>A new <see cref="Output"/></returns>
        //public static Output CreateForWin32(GraphicsDevice device, OutputConfiguration desc, IHwndOwner window, Size outputArea = default)
        //    => CreateForWin32(device, desc, window.GetHwnd(), outputArea);

        ///// <summary>
        ///// Creates a new <see cref="Output"/> to a Win32 Window backed by a HWND
        ///// </summary>
        ///// <param name="device">The <see cref="GraphicsDevice"/> that will output to this buffer</param>
        ///// <param name="desc">The <see cref="OutputConfiguration"/> for this output</param>
        ///// <param name="window">The HWND for the window to bind to</param>
        ///// <param name="outputArea">Optionally, the <see cref="Size"/> of the rendered output. By default, this will be the entire window</param>
        ///// <returns>A new <see cref="Output"/></returns>
        //public static Output CreateForWin32(GraphicsDevice device, OutputConfiguration desc, IntPtr window, Size outputArea = default)
        //{
        //    return new Output(device, outputArea, IOutputOwner.FromHwnd(window), desc);
        //}


        ///// <summary>
        ///// Creates a new <see cref="Output"/> to a WinRT ICoreWindow
        ///// </summary>
        ///// <param name="device">The <see cref="GraphicsDevice"/> that will output to this buffer</param>
        ///// <param name="desc">The <see cref="OutputConfiguration"/> for this output</param>
        ///// <param name="window">The <see cref="ICoreWindowsOwner"/> that owns the window</param>
        ///// <param name="outputArea">Optionally, the <see cref="Size"/> of the rendered output. By default, this will be the entire window</param>
        ///// <returns>A new <see cref="Output"/></returns>
        //public static Output CreateForWinRT(GraphicsDevice device, OutputConfiguration desc, ICoreWindowsOwner window, Size outputArea = default)
        //    => CreateForWinRT(device, desc, window.GetIUnknownForWindow(), outputArea);

        ///// <summary>
        ///// Creates a new <see cref="Output"/> to a WinRT ICoreWindow
        ///// </summary>
        ///// <param name="device">The <see cref="GraphicsDevice"/> that will output to this buffer</param>
        ///// <param name="desc">The <see cref="OutputConfiguration"/> for this output</param>
        ///// <param name="window">The IUnknown* for the window to bind to</param>
        ///// <param name="outputArea">Optionally, the <see cref="Size"/> of the rendered output. By default, this will be the entire window</param>
        ///// <returns>A new <see cref="Output"/></returns>
        //public static Output CreateForWinRT(GraphicsDevice device, OutputConfiguration desc, void* window, Size outputArea = default)
        //{
        //    return new Output(device, outputArea, IOutputOwner.FromICoreWindow(window), desc);
        //}


        ///// <summary>
        ///// Creates a new <see cref="Output"/> to a WinRT ISwapChainPanelNative
        ///// </summary>
        ///// <param name="device">The <see cref="GraphicsDevice"/> that will output to this buffer</param>
        ///// <param name="desc">The <see cref="OutputConfiguration"/> for this output</param>
        ///// <param name="swapChainPanelNative">The IUnknown* for the ISwapChainPanelNative to bind to</param>
        ///// <param name="outputArea">The <see cref="Size"/> of the rendered output</param>
        ///// <returns>A new <see cref="Output"/></returns>
        //public static Output CreateForSwapChainPanel(GraphicsDevice device, OutputConfiguration desc, void* swapChainPanelNative, Size outputArea)
        //{
        //    return new Output(device, outputArea, IOutputOwner.FromSwapChainPanel(swapChainPanelNative), desc);
        //}


        /// <summary>
        /// Presents the current back buffer to the output, and advances to the next back buffer
        /// </summary>
        public void Present(PresentFlags flags = PresentFlags.None)
        {
            _output.Present(flags);
        }

        public void PresentPartial(
            ReadOnlySpan<Rectangle> dirtyRects,
            Rectangle scrollRect,
            Point scrollOffset,
            PresentFlags flags = PresentFlags.None
            )
        {
            _output.PresentPartial(dirtyRects, scrollRect, scrollOffset, flags);
        }

        /// <inheritdoc cref="IDisposable"/>
        public void Dispose()
        {

        }
    }
}
