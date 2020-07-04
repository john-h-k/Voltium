using System.Collections.Generic;
using Voltium.Core.D3D12;

namespace Voltium.Core.Pool
{
    internal interface FenceReturner
    {

    }

    internal sealed class WaitForFenceHelper<T, TOnReturn> where TOnReturn : struct, FenceReturner
    {
        private SortedList<FenceMarker, T> _inFlight = new();

        public void MarkFenceReached(FenceMarker reached)
        {

        }

        public void Add(T value, FenceMarker fenceToReturnOn)
        {

        }
    }
}
