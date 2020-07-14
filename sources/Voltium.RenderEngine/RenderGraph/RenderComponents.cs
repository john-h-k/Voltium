using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Voltium.Common;

namespace Voltium.RenderEngine
{
    /// <summary>
    /// Represents a set of components passed between render passes by the <see cref="RenderGraph"/>
    /// </summary>
    public readonly struct RenderComponents
    {
        private static int Count;
        private readonly int _handle;

        private RenderComponents(int handle) => _handle = handle;

        internal static RenderComponents Create() => new RenderComponents(Count++);

        private static class TypeComponent<T>
        {
            // Optimised for the case where only one owner exists (it doesn't use a dict then)
            private static int? _single;
            [MaybeNull] private static T _singleValue;

            private static Dictionary<int, T>? _values;
            private static readonly object _lock = new();

            public static T GetComponent(int owner)
            {
                lock (_lock)
                {
                    if (_single is null && _values is null)
                    {
                        ThrowHelper.ThrowInvalidOperationException("No component of type present");
                    }

                    // if the dict is alive, _single is -1
                    if (_single == owner)
                    {
                        return _singleValue!;
                    }

                    return _values![owner];
                }
            }

            public static void SetComponent(int owner, T value)
            {
                lock (_lock)
                {
                    // check if we either can become the single owner, or already are
                    // (and the dict isn't alive)
                    if (_single is null || (_single == owner && _values is null))
                    {
                        _single = owner;
                        _singleValue = value;
                    }
                    else
                    {
                        // Lazy dict init
                        _values ??= new();

                        // move over the single value. maybe not necessary?
                        if (_single is not null)
                        {
                            _values[_single.Value] = _singleValue!;
                            _single = null;
                        }

                        _values[owner] = value;
                    }
                }
            }
        }

        /// <summary>
        /// Get a component by its type
        /// </summary>
        /// <typeparam name="T">The type of the component to retrieve</typeparam>
        /// <returns>The component, if present</returns>
        public T Get<T>() => TypeComponent<T>.GetComponent(_handle);

        /// <summary>
        /// Adds a new component by its type
        /// </summary>
        /// <typeparam name="T">The type of the component to add</typeparam>
        /// <param name="component">The value of the component</param>
        public void Add<T>(T component) => TypeComponent<T>.SetComponent(_handle, component);
    }
}
