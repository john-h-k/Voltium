namespace Voltium.Allocators
{
    /// <summary>
    ///
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class LinkedNode<T>
    {
        /// <summary>
        ///
        /// </summary>
        public LinkedNode<T>? Next { get; set; }

        /// <summary>
        ///
        /// </summary>
        public T Value { get; }

        /// <summary>
        ///
        /// </summary>
        /// <param name="value"></param>
        /// <param name="next"></param>
        public LinkedNode( T value, LinkedNode<T>? next = null)
        {
            Next = next;
            Value = value;
        }
    }
}
