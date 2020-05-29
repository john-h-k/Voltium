using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
