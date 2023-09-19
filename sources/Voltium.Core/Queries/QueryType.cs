using static TerraFX.Interop.DirectX.D3D12_QUERY_TYPE;

namespace Voltium.Core
{
    public enum QueryType : uint
    {
        Occlusion = D3D12_QUERY_TYPE_OCCLUSION,
        BinaryOcclusion = D3D12_QUERY_TYPE_BINARY_OCCLUSION,
        Timestamp = D3D12_QUERY_TYPE_TIMESTAMP,
        PipelineStatistics = D3D12_QUERY_TYPE_PIPELINE_STATISTICS,
        VideoDecodeStatistics = D3D12_QUERY_TYPE_VIDEO_DECODE_STATISTICS
    }
}
