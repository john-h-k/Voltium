using System;

namespace Voltium.Common
{
    /// <summary>
    /// Indicates that equality members should be generated for a type
    /// </summary>
    [AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class)]
    public class GenerateEqualityAttribute : Attribute
    {
    }

    /// <summary>
    /// Indicates that equality members should be generated for a type
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class GenerateEqualityIgnoreAttribute : Attribute
    {
    }
}
