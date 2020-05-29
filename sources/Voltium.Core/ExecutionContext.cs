using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TerraFX.Interop;
using static TerraFX.Interop.D3D12_COMMAND_LIST_TYPE;

namespace Voltium.Core
{
    // do not change. other parts of codebase (in particular allocations and fencing) rely on these values

    /// <summary>
    /// Represents the context that a GPU command
    /// can execute on
    /// </summary>
    public enum ExecutionContext : ulong
    {
        /// <summary>
        /// A context where copy commands can occur
        /// </summary>
        Copy = D3D12_COMMAND_LIST_TYPE_COPY,

        /// <summary>
        /// A context where copy or compute commands can occur
        /// </summary>
        Compute = D3D12_COMMAND_LIST_TYPE_COMPUTE,

        /// <summary>
        /// A context where any GPU command can occur
        /// </summary>
        Graphics = D3D12_COMMAND_LIST_TYPE_DIRECT,

        /// <summary>
        /// We should execute Patrick
        /// </summary>
        Patrick = 0xFFFFFFFFFFFFFFFF,
    }

    internal class ExecutionContextExtensions
    {
        public const int NumSupportedExecutionContexts = 4;
        public const ulong FenceSegmentSize = ulong.MaxValue / NumSupportedExecutionContexts;

        internal static bool InSegment(ulong fenceValue, ExecutionContext executionContext)
        {
            return fenceValue / FenceSegmentSize == (ulong)executionContext;
        }
    }
}
