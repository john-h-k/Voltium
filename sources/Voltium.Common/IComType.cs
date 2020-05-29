using System;

namespace Voltium.Common
{
    /// <summary>
    /// Defines a wrapper around a COM type which must be ref counted
    /// </summary>
    public interface IComType : IDisposable
    {
        /// <summary>
        /// Releases a reference to the underlying COM object
        /// </summary>
        void Release() => Dispose();

        /// <summary>
        /// Releases a reference to the underlying COM object
        /// </summary>
        new void Dispose();

        /// <summary>
        /// Adds a reference to the underlying COM object
        /// </summary>
        public void AddRef();
    }
}
