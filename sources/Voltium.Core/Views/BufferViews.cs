using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Voltium.Core.Views
{
    /// <summary>
    /// Describes the metadata used to create a render target view to a <see cref="Buffer"/>
    /// </summary>
    public struct BufferRenderTargetViewDesc
    {
        /// <summary>
        /// The <see cref="DataFormat"/> the buffer will be viewed as
        /// </summary>
        public DataFormat Format;

        /// <summary>
        /// The index of the first element 
        /// </summary>
        public uint FirstElement;

        /// <summary>
        /// The number of elements to view
        /// </summary>
        public uint NumElements;
    }


    /// <summary>
    /// Describes the metadata used to create a shader resource view to a <see cref="Buffer"/>
    /// </summary>
    public struct BufferShaderResourceViewDesc
    {
        /// <summary>
        /// The <see cref="DataFormat"/> the buffer will be viewed as
        /// </summary>
        public DataFormat Format;

        /// <summary>
        /// The number of elements to view
        /// </summary>
        public uint ElementCount;

        /// <summary>
        /// The size, in bytes, of each element
        /// </summary>
        public uint ElementStride;

        /// <summary>
        /// The offset, in elements, to start the view at
        /// </summary>
        public uint Offset;

        /// <summary>
        /// Whether the buffer should be viewed as a raw buffer
        /// </summary>
        public bool IsRaw;
    }

    /// <summary>
    /// Describe the metadata used to create a unordered access view to a <see cref="Buffer"/>
    /// </summary>
    public struct BufferUnorderedAccessViewDesc
    {
        /// <summary>
        /// The <see cref="DataFormat"/> the buffer will be viewed as
        /// </summary>
        public DataFormat Format;

        /// <summary>
        /// The number of elements to view
        /// </summary>
        public uint ElementCount;

        /// <summary>
        /// The size, in bytes, of each element
        /// </summary>
        public uint ElementStride;

        /// <summary>
        /// The offset, in elements, to start the view at
        /// </summary>
        public uint Offset;

        /// <summary>
        /// Whether the buffer should be viewed as a raw buffer
        /// </summary>
        public bool IsRaw;

        /// <summary>
        /// The offset, in bytes, to the counter for this UAV
        /// </summary>
        public uint CounterOffsetInBytes;
    }
}
