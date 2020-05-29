using Microsoft.VisualBasic.CompilerServices;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TerraFX.Interop;

namespace Voltium.Core.Memory
{
    /// <summary>
    /// Represents a contigous range in memory
    /// </summary>
    public readonly struct MemoryRange : IEquatable<MemoryRange>
    {
        // do not change layout. must be blittable with D3D12_RANGE

        /// <summary>
        /// A memory range which encompasses the entire range
        /// </summary>
        public static MemoryRange EntireRange => new MemoryRange(-1, 0);

        /// <summary>
        /// A memory range which encompasses none of the range
        /// </summary>
        public static MemoryRange NoneOfRange => new MemoryRange(0, -1);

        /// <summary>
        /// Whether the range encompasses the entire range
        /// </summary>
        public bool IsEntireRange => Start == -1;

        /// <summary>
        /// Whether the range encompasses none of the range
        /// </summary>
        public bool IsNoneOfRange => End == -1;

        /// <summary>
        /// The start of the memory range, or -1 to indicate the entire range.
        /// If this is -1, <see cref="End"/> must not be -1
        /// </summary>
        public readonly nint Start;

        /// <summary>
        /// The end of the memory range, or -1 to indicate none of the range.
        /// If this is -1, <see cref="Start"/> must not be -1
        /// </summary>
        public readonly nint End;

        /// <summary>
        /// Constructs a new <see cref="MemoryRange"/>
        /// </summary>
        public MemoryRange(nint start, nint end)
        {
            // Check that only one or neither of the values is -1, which carries special meaning
            Debug.Assert(start != end || (start != -1 || end != -1));

            Start = start;
            End = end;
        }

        /// <inheritdoc/>
        public override bool Equals([AllowNull] object obj) => obj is MemoryRange other && Equals(other);

        /// <inheritdoc/>
        public bool Equals(MemoryRange other) => Equals(this, other);

        /// <inheritdoc/>
        public static bool Equals(MemoryRange left, MemoryRange right) => left.Start == right.Start && left.End == right.End;

        /// <inheritdoc/>
        public override int GetHashCode() => HashCode.Combine(Start, End);

        /// <inheritdoc/>
        public static bool operator ==(MemoryRange left, MemoryRange right) => Equals(left, right);

        /// <inheritdoc/>
        public static bool operator !=(MemoryRange left, MemoryRange right) => !(left == right);
    }

    internal static class MemoryRangeExtensions
    {
    }
}
