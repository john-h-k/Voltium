using System.Runtime.CompilerServices;

namespace Voltium.Core.Memory
{
    /// <summary>
    /// Describes a buffer, for use by the <see cref="GraphicsAllocator"/>
    /// </summary>
    public struct BufferDesc
    {
        /// <summary>
        /// Creates a new <see cref="BufferDesc"/>
        /// </summary>
        /// <param name="length">The length, in bytes, of the desired buffer</param>
        /// <param name="resourceFlags">Any <see cref="ResourceFlags"/> for the resource</param>
        /// <returns>A new <see cref="BufferDesc"/></returns>
        public static BufferDesc Create(long length, ResourceFlags resourceFlags = ResourceFlags.None)
        {
            return new BufferDesc { Length = length, ResourceFlags = resourceFlags };
        }

        /// <summary>
        /// Creates a new <see cref="BufferDesc"/>
        /// </summary>
        /// <typeparam name="T">The type of the elements in the buffer</typeparam>
        /// <param name="elemCount">The number of elements of type <typeparamref name="T"/> of the desired buffer</param>
        /// <param name="resourceFlags">Any <see cref="ResourceFlags"/> for the resource</param>
        /// <returns>A new <see cref="BufferDesc"/></returns>
        public static BufferDesc Create<T>(long elemCount, ResourceFlags resourceFlags = ResourceFlags.None)
            => Create(Unsafe.SizeOf<T>() * elemCount, resourceFlags);

        /// <summary>
        /// The size of the buffer, in bytes
        /// </summary>
        public long Length;

        /// <summary>
        /// Any addition resource flags
        /// </summary>
        public ResourceFlags ResourceFlags;
    }
}
