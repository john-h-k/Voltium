using System;

namespace Voltium.Allocators
{
    /// <summary>
    /// Owner of <typeparamref name="T"/> that is responsible for disposing the underlying memory appropriately.
    /// </summary>
    /// <typeparam name="T">The type of <see cref="Object"/></typeparam>
    public interface IObjectOwner<T> : IDisposable
    {
        /// <summary>
        /// Returns the object
        /// </summary>
        public T Object { get; }
    }
}
