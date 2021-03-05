using Voltium.Core.Contexts;

namespace Voltium.Core.Devices
{
    /// <summary>
    /// Flags used when creating a <see cref="GpuContext"/>
    /// </summary>
    public enum ContextFlags
    {
        /// <summary>
        /// None
        /// </summary>
        None,

        /// <summary>
        /// Immediately begin executing the context when it is closed or disposed
        /// </summary>
        ExecuteOnClose,

        /// <summary>
        /// Immediately begin executing, and synchronously block until the end, when it is closed or disposed
        /// </summary>
        BlockOnClose
    }
}
