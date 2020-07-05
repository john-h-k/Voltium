using System;
using System.Collections;
using System.Collections.Generic;

namespace Voltium.Core.Infrastructure
{
    /// <summary>
    /// The type used for enumerating physical adapters and creating devices
    /// </summary>
    public unsafe abstract class DeviceFactory : IEnumerable<Adapter>, IDisposable
    {
        /// <summary>
        /// Creates a new <see cref="DeviceFactory"/>
        /// </summary>
        /// <returns>A new <see cref="DeviceFactory"/></returns>
        public static DeviceFactory Create() => /* DXCore is a bit more flexible than DXGI */ new DxCoreDeviceFactory();

        /// <summary>
        /// Creates a new <see cref="DeviceFactory"/> using a specific <see cref="DeviceEnumerationLayer"/>
        /// </summary>
        /// <returns>A new <see cref="DeviceFactory"/></returns>
        public static DeviceFactory Create(DeviceEnumerationLayer layer)
        {
            return layer switch
            {
                DeviceEnumerationLayer.Dxgi => new DxgiDeviceFactory(),
                DeviceEnumerationLayer.DxCore => new DxCoreDeviceFactory(),
                _ => throw new ArgumentException("Invalid DeviceEnumerationLayer", nameof(layer))
            };
        }

        /// <summary>
        /// Creates a new <see cref="DeviceFactory"/> to enumerate a specific <see cref="DeviceType"/>
        /// </summary>
        /// <returns>A new <see cref="DeviceFactory"/></returns>
        public static DeviceFactory Create(DeviceType type)
            => new DxCoreDeviceFactory(type); // only DXCore supports non-graphics devices

        /// <summary>
        /// Try and enable the <see cref="DeviceFactory"/> into enumerating devices by a <see cref="DevicePreference"/>
        /// </summary>
        /// <returns><c>true</c> if this setting was succesfully applied, else <c>false</c></returns>
        public abstract bool TryEnablePreferentialOrdering(DevicePreference preference);

        // the method implemented to actually enumerate devices
        internal abstract bool TryGetAdapterByIndex(uint index, out Adapter adapter);

        /// <inheritdoc cref="IEnumerable{T}.GetEnumerator"/>
        public Enumerator GetEnumerator() => new(this);

        IEnumerator<Adapter> IEnumerable<Adapter>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <inheritdoc/>
        public abstract void Dispose();

        /// <summary>
        /// Used to enumerate a <see cref="DeviceFactory"/>
        /// </summary>
        public unsafe struct Enumerator : IEnumerator<Adapter>
        {
            private DeviceFactory _base;
            private uint _index;

            /// <summary>
            /// Create a new <see cref="Enumerator"/>
            /// </summary>
            /// <param name="base">The factory used to enumerate adapters</param>
            internal Enumerator(DeviceFactory @base)
            {
                _base = @base;
                _index = 0;
                Current = default;
            }

            /// <inheritdoc cref="IEnumerator.MoveNext"/>
            public bool MoveNext()
            {
                Current.Dispose();

                bool result = _base.TryGetAdapterByIndex(_index, out var current);
                Current = current;
                return result;
            }

            /// <inheritdoc cref="IEnumerator.Reset"/>
            public void Reset() => _index = 0;

            /// <inheritdoc cref="IEnumerator{T}.Current"/>
            public Adapter Current { get; private set; }

            object? IEnumerator.Current => Current;

            /// <inheritdoc cref="IDisposable"/>
            public void Dispose()
            {
                _base.Dispose();
            }
        }
    }
}
