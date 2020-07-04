using System;
using System.Runtime.Serialization;

namespace Voltium.Common.Tracing
{
    internal class ArrayPoolLeakException : ResourceLeakException
    {
        public ArrayPoolLeakException()
        {
        }

        protected ArrayPoolLeakException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        public ArrayPoolLeakException(string? message) : base(message)
        {
        }

        public ArrayPoolLeakException(string? message, Exception? innerException) : base(message, innerException)
        {
        }

        public ArrayPoolLeakException(string? message, Array resource, object? resourceData = null) : base(message, resource, resourceData)
        {

        }
    }
}
