using System;
using System.Collections.Generic;
using System.Text;

namespace Voltium.Annotations
{
    /// <summary>
    /// Indicates a type is a native COM type with an explicit vtable
    /// </summary>
    [AttributeUsage(AttributeTargets.Struct)]
    public class NativeComTypeAttribute : Attribute
    {
        /// <summary>
        /// Creates a new instance of <see cref="NativeComMethodAttribute"/>
        /// </summary>
        /// <param name="implements">The COM interface this type implements</param>
        public NativeComTypeAttribute(Type? implements = null)
        {

        }
    }

    /// <summary>
    /// Indicates a method has a position in a native COM vtable
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class NativeComMethodAttribute : Attribute
    {
        /// <summary>
        /// Indicates the method should be appended to the end of the vtable
        /// </summary>
        public const int Append = -1;

        /// <summary>
        /// The index in the vtable to place this at, or append it to the end by default 
        /// </summary>
        public int Index { get; set; } = Append;

        /// <summary>
        /// Creates a new instance of <see cref="NativeComMethodAttribute"/>
        /// </summary>
        public NativeComMethodAttribute()
        {

        }
    }
}
