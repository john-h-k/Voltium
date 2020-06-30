using System;
using System.Collections;
using System.Collections.Generic;
using TerraFX.Interop;
using Voltium.Common;

namespace Voltium.Core.Infrastructure
{
    /// <summary>
    /// Represents a DXGI output
    /// </summary>
    public unsafe struct Output : IDisposable
    {
        private ComPtr<IDXGIOutput> _output;

        /// <summary>
        /// The name of the output device
        /// </summary>
        public string DeviceName { get; }

        /// <summary>
        /// The bounds of the output, in pixels
        /// </summary>
        public Rectangle DesktopCoordinates { get; }

        /// <summary>
        /// Whether the output is attached to the desktop
        /// </summary>
        public bool AttachedToDesktop { get; }

        /// <summary>
        /// The rotation between the graphical output and the desktop
        /// </summary>
        public DXGI_MODE_ROTATION Rotation { get; }

        /// <summary>
        /// A handle to the monitor
        /// </summary>
        public HMONITOR Monitor { get; }

        /// <inheritdoc cref="IDisposable"/>
        public void Dispose() => _output.Dispose();

        /// <summary>
        /// Creates a new instance of <see cref="Output"/>
        /// </summary>
        public Output(ComPtr<IDXGIOutput> output, string deviceName, Rectangle desktopCoordinates, bool attachedToDesktop, DXGI_MODE_ROTATION rotation, HMONITOR monitor)
        {
            _output = output;
            DeviceName = deviceName;
            DesktopCoordinates = desktopCoordinates;
            AttachedToDesktop = attachedToDesktop;
            Rotation = rotation;
            Monitor = monitor;
        }

        /// <summary>
        /// Enumerate adapters for a given factory
        /// </summary>
        /// <param name="adapter">The factory used to enumerate</param>
        /// <returns>An <see cref="OutputEnumerator"/></returns>
        public static OutputEnumerator EnumerateOutputs(ComPtr<IDXGIAdapter> adapter) => new OutputEnumerator(adapter);
    }


    /// <summary>
    ///
    /// </summary>
    public unsafe struct OutputEnumerator : IEnumerator<Output>, IEnumerable<Output>
    {
        private ComPtr<IDXGIAdapter> _adapter;
        private uint _index;

        /// <summary>
        /// Create a new <see cref="OutputEnumerator"/>
        /// </summary>
        /// <param name="adapter">The factory used to enumerate adapters</param>
        public OutputEnumerator(ComPtr<IDXGIAdapter> adapter)
        {
            _adapter = adapter;
            _index = 0;
            Current = default;
        }

        /// <inheritdoc cref="IEnumerator.MoveNext"/>
        public bool MoveNext()
        {
            Current.Dispose();

            IDXGIOutput* p;
            Guard.ThrowIfFailed(_adapter.Get()->EnumOutputs(_index++, &p));

            Current = CreateOutput(p);
            return p != null;
        }

        private static Output CreateOutput(ComPtr<IDXGIOutput> dxgiOutput)
        {
            var p = dxgiOutput.Get();
            DXGI_OUTPUT_DESC desc;
            Guard.ThrowIfFailed(p->GetDesc(&desc));

            var name = new ReadOnlySpan<char>(desc.DeviceName, 32).ToString();

            return new Output(
                dxgiOutput,
                name,
                new Rectangle(desc.DesktopCoordinates),
                desc.AttachedToDesktop == Windows.TRUE,
                desc.Rotation,
                desc.Monitor
            );
        }

        /// <inheritdoc cref="IEnumerator.Reset"/>
        public void Reset() => _index = 0;

        /// <inheritdoc cref="IEnumerator{T}.Current"/>
        public Output Current { get; private set; }

        object? IEnumerator.Current => Current;

        /// <inheritdoc cref="IDisposable"/>
        public void Dispose() => _adapter.Dispose();

        /// <inheritdoc cref="IEnumerable{T}.GetEnumerator"/>
        public OutputEnumerator GetEnumerator() => this;

        IEnumerator<Output> IEnumerable<Output>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
