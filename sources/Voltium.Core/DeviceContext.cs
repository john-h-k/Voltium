using static TerraFX.Interop.D3D12_COMMAND_LIST_TYPE;

namespace Voltium.Core
{
    // do not change. other parts of codebase (in particular allocations and fencing) rely on these values

    /// <summary>
    /// Represents the context that a GPU command
    /// can execute on
    /// </summary>
    public enum DeviceContext : ulong
    {
        /// <summary>
        /// A context used for copy operations
        /// </summary>
        Copy = D3D12_COMMAND_LIST_TYPE_COPY,

        /// <summary>
        /// A context used for copy or compute operations
        /// </summary>
        Compute = D3D12_COMMAND_LIST_TYPE_COMPUTE,

        /// <summary>
        /// A context used for copy, compute, or graphical operations
        /// </summary>
        Graphics = D3D12_COMMAND_LIST_TYPE_DIRECT,

        /// <summary>
        /// A context used for video decode operations
        /// </summary>
        VideoDecode = D3D12_COMMAND_LIST_TYPE_VIDEO_DECODE,

        /// <summary>
        /// A context used for video decode operations
        /// </summary>
        VideoEncode = D3D12_COMMAND_LIST_TYPE_VIDEO_ENCODE,

        /// <summary>
        /// A context used for video processing operations
        /// </summary>
        VideoProcess = D3D12_COMMAND_LIST_TYPE_VIDEO_PROCESS,

        /// <summary>
        /// We should execute Patrick
        /// </summary>
        Patrick = 0xFFFFFFFFFFFFFFFF,
    }
}
