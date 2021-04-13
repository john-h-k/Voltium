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
using Voltium.Core.Configuration.Graphics;
using Voltium.Core.NativeApi;

namespace Voltium.Core.Devices
{

    public struct ViewSet : IDisposable
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

        public uint Width => (uint)Resolution.Width;
        public uint Height => (uint)Resolution.Height;

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

        public bool IsReadyForPresent => _output.WaitForNextPresent(TimeSpan.Zero) == PresentWaitState.PresentReady;
             
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
