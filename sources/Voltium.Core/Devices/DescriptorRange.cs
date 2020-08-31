using Voltium.Common;

namespace Voltium.Core.Devices
{
    /// <summary>
    /// Represents a range in a <see cref="DescriptorHeap"/>
    /// </summary>
    public struct DescriptorRange
    {
        /// <summary>
        /// The first <see cref="DescriptorHandle"/> in the range
        /// </summary>
        public readonly DescriptorHandle Start;

        /// <summary>
        /// The number of <see cref="DescriptorHandle"/>s in the range
        /// </summary>
        public readonly int Length;

        /// <summary>
        /// Gets the <see cref="DescriptorHandle"/> at a given index in the range
        /// </summary>
        /// <param name="index">The index of the <see cref="DescriptorHandle"/> to get</param>
        /// <returns>A <see cref="DescriptorHandle"/></returns>
        public DescriptorHandle this[int index]
        {
            get
            {
                if ((uint)index >= Length)
                {
                    ThrowHelper.ThrowArgumentOutOfRangeException(nameof(index));
                }

                return Start + index;
            }
        }

        internal DescriptorRange(DescriptorHandle start, int length)
        {
            Start = start;
            Length = length;
        }
    }
}
