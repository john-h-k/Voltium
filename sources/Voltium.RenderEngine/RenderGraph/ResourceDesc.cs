using System.Runtime.InteropServices;
using Voltium.Core;
using Voltium.Core.Memory;
using Buffer = Voltium.Core.Memory.Buffer;

namespace Voltium.RenderEngine
{
    internal struct ResourceDesc
    {
        public ResourceType Type;

        public BufferDesc BufferDesc;
        public TextureDesc TextureDesc;

        public MemoryAccess MemoryAccess;
        public ResourceState InitialState;

        public Buffer Buffer;
        public Texture Texture;

        BufTexUnion _;

        // null if resource is not swapchain relative
        public double? OutputRelativeSize;

        public string? DebugName;

        [StructLayout(LayoutKind.Explicit)]
        private struct BufTexUnion
        {
            [FieldOffset(0)]
            public Buffer Buffer;
            [FieldOffset(0)]
            public Texture Texture;
        }
    }
}
