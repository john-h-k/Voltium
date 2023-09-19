using System;
using TerraFX.Interop;
using Voltium.Common;
using Voltium.Core.Memory;
using Voltium.Core.NativeApi;

namespace Voltium.Core.Pipeline
{
    /// <summary>
    /// A pipeline state object
    /// </summary>
    public unsafe struct PipelineStateObject
    {
        internal PipelineStateObject(PipelineHandle handle, Disposal<PipelineHandle> dispose)
        {
            Handle = handle;
            _dispose = dispose;
        }

        internal PipelineHandle Handle;
        private Disposal<PipelineHandle> _dispose;

        public void Dispose() => _dispose.Dispose(ref Handle);
    }
}
