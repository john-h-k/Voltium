using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using TerraFX.Interop;
using Voltium.Common;
using Voltium.Core.GpuResources;
using Voltium.Core.Memory.GpuResources.ResourceViews;

namespace Voltium.Core.Memory.GpuResources
{
    public enum BufferType
    {
        ConstantBuffer,
        StructuredBuffer,
        VertexBuffer,
        IndexBuffer,
    }

    
    public struct Buffer<T> where T : unmanaged
    {
        public BufferType Type { get; }

        public readonly uint Count;

        [StructLayout(LayoutKind.Sequential)]
        private struct BufferUnion
        {
            [FieldOffset(0)]
            public ConstantBuffer<T> ConstantBuffer;
            public VertexBuffer<T> VertexBuffer;
            public IndexBuffer<T> IndexBuffer;
            public StructuredBuffer<T> StructuredBuffer;
        }
    }
}
