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
    //public unsafe struct QueryHeap : IEvictable, IInternalD3D12Object
    //{
    //    private ComputeDevice _device;
    //    private UniqueComPtr<ID3D12QueryHeap> _queryHeap;
    //    private uint _numQueries;
    //    private uint _nextQuery;

    //    internal int NextQueryIndex => _nextQuery++;

    //    public QueryHeap(ComputeDevice device, QueryHeapType type, uint numQueries)
    //    {
    //        _device = device;
    //        _nextQuery = 0;

    //        D3D12_FEATURE_DATA_D3D12_OPTIONS3 opts3 = default;
    //        _device.QueryFeatureSupport(D3D12_FEATURE.D3D12_FEATURE_D3D12_OPTIONS3, &opts3);

    //        if (type == QueryHeapType.CopyTimestamp && opts3.CopyQueueTimestampQueriesSupported != TRUE)
    //        {
    //            ThrowHelper.ThrowNotSupportedException("Device does not support copy queue timestamps");
    //        }

    //        var desc = new D3D12_QUERY_HEAP_DESC
    //        {
    //            Count = numQueries,
    //            Type = (D3D12_QUERY_HEAP_TYPE)type,
    //            NodeMask = 0, // TODO: MULTI-GPU
    //        };

    //        _queryHeap = device.CreateQueryHeap(desc);
    //        _numQueries = numQueries;
    //    }

    //    internal ID3D12QueryHeap* GetQueryHeap() => _queryHeap.Get();

    //    bool IEvictable.IsBlittableToPointer => false;
    //    ID3D12Pageable* IEvictable.GetPageable() => (ID3D12Pageable*)_queryHeap.Get();
    //    ID3D12Object* IInternalD3D12Object.GetPointer() => (ID3D12Object*)_queryHeap.Get();
    //}

    //public enum QueryHeapType
    //{
    //    GraphicsOrComputeTimestamp = D3D12_QUERY_HEAP_TYPE.D3D12_QUERY_HEAP_TYPE_TIMESTAMP,
    //    CopyTimestamp = D3D12_QUERY_HEAP_TYPE.D3D12_QUERY_HEAP_TYPE_COPY_QUEUE_TIMESTAMP,
    //}

}
