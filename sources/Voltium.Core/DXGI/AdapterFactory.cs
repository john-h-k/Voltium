using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Voltium.Core.DXGI
{
    /// <summary>
    /// 
    /// </summary>
    public unsafe abstract class AdapterFactory : IEnumerable<Adapter>, IDisposable
    {
        /// <summary>
        /// Creates a new <see cref="AdapterFactory"/>
        /// </summary>
        /// <returns>A new <see cref="AdapterFactory"/></returns>
        public static AdapterFactory Create() => new DxgiAdapterFactory();

        /// <summary>
        /// Creates a new <see cref="AdapterFactory"/>
        /// </summary>
        /// <returns>A new <see cref="AdapterFactory"/></returns>
        public static AdapterFactory CreateCore() => new DxCoreAdapterFactory();

        internal abstract bool TryGetAdapterByIndex(uint index, out Adapter adapter);

        /// <inheritdoc cref="IEnumerable{T}.GetEnumerator"/>
        public Enumerator GetEnumerator() => new(this);

        IEnumerator<Adapter> IEnumerable<Adapter>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <inheritdoc/>
        public abstract void Dispose();

        /// <summary>
        /// Used to enumerate a <see cref="AdapterFactory"/>
        /// </summary>
        public unsafe struct Enumerator : IEnumerator<Adapter>
        {
            private AdapterFactory _base;
            private uint _index;

            /// <summary>
            /// Create a new <see cref="Enumerator"/>
            /// </summary>
            /// <param name="base">The factory used to enumerate adapters</param>
            internal Enumerator(AdapterFactory @base)
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
            }
        }
    }
}
