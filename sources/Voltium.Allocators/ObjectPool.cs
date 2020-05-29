using System;

namespace Voltium.Allocators
{
    /// <summary>
    ///
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class ObjectPool<T> : IDisposable
    {
        /// <summary>
        ///
        /// </summary>
        /// <param name="resetObject"></param>
        /// <returns></returns>
        /// <exception cref="NotSupportedException">Thrown if <typeparamref name="T"/> does not implement <see cref="IResettable"/> and <paramref name="resetObject"/> is <code>true</code></exception>
        public abstract T Rent(bool resetObject = false);

        /// <summary>
        ///
        /// </summary>
        /// <param name="resetObject"></param>
        /// <returns></returns>
        /// <exception cref="NotSupportedException">Thrown if <typeparamref name="T"/> does not implement <see cref="IResettable"/> and <paramref name="resetObject"/> is <code>true</code></exception>
        public abstract IObjectOwner<T> RentAsDisposable(bool resetObject = false);

        /// <summary>
        ///
        /// </summary>
        /// <param name="obj"></param>
        public abstract void Return(T obj);

        /// <summary>
        /// Frees all resources used by the memory pool.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Frees all resources used by the memory pool.
        /// </summary>
        /// <param name="disposing"></param>
        protected abstract void Dispose(bool disposing);
    }
}
