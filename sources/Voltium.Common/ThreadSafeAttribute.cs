using System;

namespace Voltium.Common
{
    /// <summary>
    /// Indicates a type is thread-safe
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class ThreadSafeAttribute : Attribute
    {

    }
}
