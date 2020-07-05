using System;
using System.Runtime.Serialization;

namespace Voltium.Core.Managers.Shaders
{
    /// <summary>
    /// The exception thrown when shader compilation fails
    /// </summary>
    public class ShaderCompilationException : Exception
    {
        /// <inheritdoc/>
        public ShaderCompilationException()
        {
        }


        /// <inheritdoc/>
        internal ShaderCompilationException(ShaderCompilationData message) : base(message.ToString())
        {
        }

        /// <inheritdoc/>
        internal ShaderCompilationException(ShaderCompilationData message, Exception? innerException) : base(message.ToString(), innerException)
        {
        }

        /// <inheritdoc/>
        protected ShaderCompilationException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }

    internal ref struct ShaderCompilationData
    {
        public ReadOnlySpan<char> Filename;
        public ReadOnlySpan<char> Errors;
        public ReadOnlySpan<char> Other;

        public override string ToString()
        {
            return $"Shader compilation of file '{Filename.ToString()} failed. Error data provided: '{Errors.ToString()}'." +
                $"{(string.IsNullOrWhiteSpace(Other.ToString()) ? "" : $"Additional data provided {Other.ToString()}")}";
        }
    }
}
