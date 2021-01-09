using System;
using System.Collections.Generic;

using System.Text;
using System.Threading.Tasks;
using TerraFX.Interop;
using Voltium.Common;
using Voltium.Core.Devices;
using static TerraFX.Interop.Windows;
using Buffer = Voltium.Core.Memory.Buffer;

namespace Voltium.Core.Queries
{
    public unsafe struct QueryHeap : IEvictable, IInternalD3D12Object
    {
        private ComputeDevice _device;
        private UniqueComPtr<ID3D12QueryHeap> _queryHeap;
        private uint _numQueries;
        private uint _nextQuery;

        public uint Length => _numQueries;

        internal int NextQueryIndex => (int)_nextQuery++;

        internal QueryHeap(ComputeDevice device, QueryHeapType type, int numQueries)
        {
            _device = device;
            _nextQuery = 0;

            if (type == QueryHeapType.CopyTimestamp
                && _device.QueryFeatureSupport<D3D12_FEATURE_DATA_D3D12_OPTIONS3>(D3D12_FEATURE.D3D12_FEATURE_D3D12_OPTIONS3).CopyQueueTimestampQueriesSupported != TRUE)
            {
                ThrowHelper.ThrowNotSupportedException("Device does not support copy queue timestamps");
            }

            var desc = new D3D12_QUERY_HEAP_DESC
            {
                Count = (uint)numQueries,
                Type = (D3D12_QUERY_HEAP_TYPE)type,
                NodeMask = 0, // TODO: MULTI-GPU
            };

            _queryHeap = device.CreateQueryHeap(desc);
            _numQueries = (uint)numQueries;
        }

#if D3D12
        internal ID3D12QueryHeap* GetQueryHeap() => _queryHeap.Ptr;
#else
        internal ulong GetQueryHeap() => _queryHeap.Ptr;
#endif

        bool IEvictable.IsBlittableToPointer => false;
        ID3D12Pageable* IEvictable.GetPageable() => (ID3D12Pageable*)_queryHeap.Ptr;
        ID3D12Object* IInternalD3D12Object.GetPointer() => (ID3D12Object*)_queryHeap.Ptr;
    }

}
