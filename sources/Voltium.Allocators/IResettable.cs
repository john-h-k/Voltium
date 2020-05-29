namespace Voltium.Allocators
{
    /// <summary>
    /// Defines an interface used to indicate an object can be reset and reused
    /// </summary>
    public interface IResettable
    {
        /// <summary>
        /// Reset the object so it can be reused
        /// </summary>
        void Reset();
    }
}
