using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Voltium.Core.D3D12
{
    /// <summary>
    /// Represents a position on a fence
    /// </summary>
    public readonly struct FenceMarker : IEquatable<FenceMarker>, IComparable<FenceMarker>
    {
        internal static ulong GetFirstFenceForExecutionContext(ExecutionContext executionContext)
            => (ulong)executionContext * ExecutionContextExtensions.FenceSegmentSize;

        internal FenceMarker(ulong fenceValue) => Value = fenceValue;
        internal FenceMarker(ulong fenceValue, ExecutionContext executionContext)
        {
            Debug.Assert(ExecutionContextExtensions.InSegment(fenceValue, executionContext));
            Value = fenceValue;
        }

        private readonly ulong Value;

        internal ulong FenceValue => Value;

        internal ExecutionContext ExecutionContext => (ExecutionContext)(Value / ExecutionContextExtensions.FenceSegmentSize);

        /// <inheritdoc/>
        public bool Equals(FenceMarker other) => Equals(this, other);

        /// <inheritdoc/>
        public static bool Equals(FenceMarker left, FenceMarker right) => left.Value == right.Value;

        /// <inheritdoc/>
        public override int GetHashCode() => Value.GetHashCode();

        /// <summary>
        /// Tests whether the current fence occurs in the same <see cref="ExecutionContext"/> and after or at the same time as another fence
        /// </summary>
        /// <param name="other">The other fence to compare to</param>
        /// <returns><code>true</code> if this fence is afterwards and on the same <see cref="ExecutionContext"/>, else <code>false</code></returns>
        public bool IsAtOrAfter(FenceMarker other) => ExecutionContext == other.ExecutionContext && Value >= other.Value;

        /// <summary>
        /// Tests whether the current fence occurs in the same <see cref="ExecutionContext"/> and before or at the same time as another fence
        /// </summary>
        /// <param name="other">The other fence to compare to</param>
        /// <returns><code>true</code> if this fence is before and on the same <see cref="ExecutionContext"/>, else <code>false</code></returns>
        public bool IsAtOrBefore(FenceMarker other) => ExecutionContext == other.ExecutionContext && Value <= other.Value;

        /// <summary>
        /// Tests whether the current fence occurs in the same <see cref="ExecutionContext"/> and after another fence
        /// </summary>
        /// <param name="other">The other fence to compare to</param>
        /// <returns><code>true</code> if this fence is afterwards and on the same <see cref="ExecutionContext"/>, else <code>false</code></returns>
        public bool IsAfter(FenceMarker other) => ExecutionContext == other.ExecutionContext && Value > other.Value;

        /// <summary>
        /// Tests whether the current fence occurs in the same <see cref="ExecutionContext"/> and before or at the same time as another fence
        /// </summary>
        /// <param name="other">The other fence to compare to</param>
        /// <returns><code>true</code> if this fence is before and on the same <see cref="ExecutionContext"/>, else <code>false</code></returns>
        public bool IsBefore(FenceMarker other) => ExecutionContext == other.ExecutionContext && Value < other.Value;

        internal ulong Normalise() => FenceValue / ExecutionContextExtensions.NumSupportedExecutionContexts;

        /// <summary>
        /// Compares one <see cref="FenceMarker"/> to another, based off the fence value from the start of the current segment 
        /// (rather than the absolute value of the fence)
        /// </summary>
        /// <param name="other">The <see cref="FenceMarker"/> to compare to</param>
        /// <returns>Less than 0 if <paramref name="other"/> is before, 0 if they are equivalent, else greater than 0</returns>
        public int CompareTo(FenceMarker other)
        {
            return Normalise().CompareTo(other.Normalise());
        }

        /// <summary>
        /// Add a specified value to a fence
        /// </summary>
        /// <param name="left">The fence to add to</param>
        /// <param name="right">The value to add to the fence</param>
        /// <returns>A new fence with <paramref name="right"/> added</returns>
        public static FenceMarker operator +(FenceMarker left, uint right) => new FenceMarker(left.Value + right);

        /// <summary>
        /// Subtract a specified from to a fence
        /// </summary>
        /// <param name="left">The fence to subtract from</param>
        /// <param name="right">The value to subtract from the fence</param>
        /// <returns>A new fence with <paramref name="right"/> subtracted</returns>
        public static FenceMarker operator -(FenceMarker left, uint right) => new FenceMarker(left.Value - right);


        /// <summary>
        /// Add 1 to a fence
        /// </summary>
        /// <param name="marker">The fence to add to</param>
        /// <returns>A new fence with <c>1</c>added</returns>
        public static FenceMarker operator ++(FenceMarker marker) => new FenceMarker(marker.Value + 1);

        /// <summary>
        /// Subtract 1 from a fence
        /// </summary>
        /// <param name="marker">The fence to subtract from</param>
        /// <returns>A new fence with <c>1</c>subtracted</returns>
        public static FenceMarker operator -(FenceMarker marker) => new FenceMarker(marker.Value - 1);
    }
}
