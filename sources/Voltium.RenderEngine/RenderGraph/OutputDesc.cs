#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
using Voltium.Core;
using Voltium.Core.Devices;

namespace Voltium.RenderEngine
{
    public struct OutputDesc
    {
        public static OutputDesc None => new OutputDesc { Type = OutputClass.None };

        public static OutputDesc FromOutput(OutputClass type, Output output)
        {
            var back = output.BackBuffer;
            return CreateTexture(type, back.Width, back.Height, back.DepthOrArraySize);
         }

        public static OutputDesc CreateTexture(OutputClass type, ulong width, uint height = 1, ushort depthOrArraySize = 1)
            => new OutputDesc { ResourceType = ResourceType.Texture, Type = type, TextureWidth = width, TextureHeight = height, TextureDepthOrArraySize = depthOrArraySize };


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
