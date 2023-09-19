using TerraFX.Interop.DirectX;

namespace Voltium.Core.Queries
{
    public enum QuerySetType
    {
        GraphicsOrComputeTimestamp = D3D12_QUERY_HEAP_TYPE.D3D12_QUERY_HEAP_TYPE_TIMESTAMP,
        CopyTimestamp = D3D12_QUERY_HEAP_TYPE.D3D12_QUERY_HEAP_TYPE_COPY_QUEUE_TIMESTAMP,
        Occlusion = D3D12_QUERY_HEAP_TYPE.D3D12_QUERY_HEAP_TYPE_OCCLUSION,
        VideoDecodeStatistics = D3D12_QUERY_HEAP_TYPE.D3D12_QUERY_HEAP_TYPE_VIDEO_DECODE_STATISTICS,
        PipelineStatistics = D3D12_QUERY_HEAP_TYPE.D3D12_QUERY_HEAP_TYPE_PIPELINE_STATISTICS
    }

}
