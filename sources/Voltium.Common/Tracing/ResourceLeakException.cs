using System;
using System.Runtime.Serialization;

namespace Voltium.Common.Tracing
{
    internal class ResourceLeakException : Exception
    {
        public ResourceLeakException()
        {
        }

        protected ResourceLeakException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        public ResourceLeakException(string? message) : base(message)
        {
        }

        public ResourceLeakException(string? message, Exception? innerException) : base(message, innerException)
        {
        }

        public ResourceLeakException(string? message, object resource, object? resourceData = null) : this(message)
        {
            Resource = resource;
            ResourceData = resourceData;
        }

        public object? Resource { get; }
        public object? ResourceData { get; }
    }
}
