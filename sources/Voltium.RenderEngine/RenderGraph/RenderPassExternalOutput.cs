using Voltium.Core;
using Voltium.Core.Devices;
using Voltium.Core.Memory;

namespace Voltium.RenderEngine
{
    /// <summary>
    /// Describes an output of a render pass
    /// </summary>
    public struct RenderPassExternalOutput
    {
        /// <summary>
        /// Indicates the pass does not have an output
        /// </summary>
        public static RenderPassExternalOutput None => new RenderPassExternalOutput { Type = OutputClass.None };

        /// <summary>
        /// Creates an <see cref="RenderPassExternalOutput"/> from a <see cref="Output"/>
        /// </summary>
        /// <param name="type">The <see cref="OutputClass"/> for the output desc</param>
        /// <param name="output">The <see cref="Output"/> to build this desc from</param>
        /// <returns>A new <see cref="RenderPassExternalOutput"/> representing <paramref name="output"/></returns>
        public static RenderPassExternalOutput FromBackBuffer(OutputClass type, Output output)
        {
            var back = output.OutputBuffer;
            return CreateTexture(type, back.Format, back.Width, back.Height, back.DepthOrArraySize);
         }

        /// <summary>
        /// Creates a <see cref="RenderPassExternalOutput"/> representing a <see cref="Texture"/>
        /// </summary>
        /// <param name="type">The <see cref="OutputClass"/> for the output desc</param>
        /// <param name="format">The <see cref="DataFormat"/> for the output desc</param>
        /// <param name="width">The width, in texels, of the output</param>
        /// <param name="height">The height, in texels, of the output, if it is 2D</param>
        /// <param name="depthOrArraySize">The depth, in texels, if the resource is 3D, else the array size of the output</param>
        /// <returns>A new <see cref="RenderPassExternalOutput"/> representing a <see cref="Texture"/></returns>
        public static RenderPassExternalOutput CreateTexture(OutputClass type, DataFormat format, ulong width, uint height = 1, ushort depthOrArraySize = 1)
            => new RenderPassExternalOutput { ResourceType = ResourceType.Texture, Format = format, Type = type, TextureWidth = width, TextureHeight = height, TextureDepthOrArraySize = depthOrArraySize };


        /// <summary>
        /// Creates a <see cref="RenderPassExternalOutput"/> representing a <see cref="Buffer"/>
        /// </summary>
        /// <param name="type">The <see cref="OutputClass"/> for the output desc</param>
        /// <param name="length">The length, in bytes, of the output</param>
        /// <returns>A new <see cref="RenderPassExternalOutput"/> representing a <see cref="Buffer"/></returns>
        public static RenderPassExternalOutput CreateBuffer(OutputClass type, ulong length)
            => new RenderPassExternalOutput { ResourceType = ResourceType.Texture, Type = type, BufferLength = length };

        internal OutputClass Type;
        internal ResourceType ResourceType;
        internal ulong BufferLength;

        internal DataFormat Format;
        internal ulong TextureWidth;
        internal uint TextureHeight;
        internal ushort TextureDepthOrArraySize;
    }
}
