using Voltium.Core;
using Voltium.Core.Devices;
using Voltium.Core.Memory;

namespace Voltium.RenderEngine
{
    /// <summary>
    /// Describes an output of a render pass
    /// </summary>
    public struct OutputDesc
    {
        /// <summary>
        /// Indicates the pass does not have an output
        /// </summary>
        public static OutputDesc None => new OutputDesc { Type = OutputClass.None };

        /// <summary>
        /// Creates an <see cref="OutputDesc"/> from a <see cref="TextureOutput"/>
        /// </summary>
        /// <param name="type">The <see cref="OutputClass"/> for the output desc</param>
        /// <param name="output">The <see cref="TextureOutput"/> to build this desc from</param>
        /// <returns>A new <see cref="OutputDesc"/> representing <paramref name="output"/></returns>
        public static OutputDesc FromBackBuffer(OutputClass type, TextureOutput output)
        {
            var back = output.OutputBuffer;
            return CreateTexture(type, back.Width, back.Height, back.DepthOrArraySize);
         }

        /// <summary>
        /// Creates a <see cref="OutputDesc"/> representing a <see cref="Texture"/>
        /// </summary>
        /// <param name="type">The <see cref="OutputClass"/> for the output desc</param>
        /// <param name="width">The width, in texels, of the output</param>
        /// <param name="height">The height, in texels, of the output, if it is 2D</param>
        /// <param name="depthOrArraySize">The depth, in texels, if the resource is 3D, else the array size of the output</param>
        /// <returns>A new <see cref="OutputDesc"/> representing a <see cref="Texture"/></returns>
        public static OutputDesc CreateTexture(OutputClass type, ulong width, uint height = 1, ushort depthOrArraySize = 1)
            => new OutputDesc { ResourceType = ResourceType.Texture, Type = type, TextureWidth = width, TextureHeight = height, TextureDepthOrArraySize = depthOrArraySize };


        /// <summary>
        /// Creates a <see cref="OutputDesc"/> representing a <see cref="Buffer"/>
        /// </summary>
        /// <param name="type">The <see cref="OutputClass"/> for the output desc</param>
        /// <param name="length">The length, in bytes, of the output</param>
        /// <returns>A new <see cref="OutputDesc"/> representing a <see cref="Buffer"/></returns>
        public static OutputDesc CreateBuffer(OutputClass type, ulong length)
            => new OutputDesc { ResourceType = ResourceType.Texture, Type = type, BufferLength = length };

        internal OutputClass Type;
        internal ResourceType ResourceType;
        internal ulong BufferLength;

        internal ulong TextureWidth;
        internal uint TextureHeight;
        internal ushort TextureDepthOrArraySize;
    }
}
