using System;
using TerraFX.Interop;
using Voltium.Common;

namespace Voltium.Core.GpuResources.OldStyle
{
    /// <summary>
    /// </summary>
    public abstract unsafe class GraphicsResource
    {
        /// <summary>
        /// The underlying resource object
        /// </summary>
        public ID3D12Resource* Value => _resource.Get();

        /// <summary>
        /// why the fuck do protected stuff need XML learn it urself u eejit
        /// </summary>
        protected ComPtr<ID3D12Resource> _resource;

        /// <summary>
        /// The current state of the resource
        /// </summary>
        public D3D12_RESOURCE_STATES State { get; private set; }

        /// <summary>
        /// The GPU handle to the resource
        /// </summary>
        public GpuHandle GpuHandle { get; private set; }

        /// <summary>
        /// The GPU address of the resource
        /// </summary>
        public ulong GpuAddress { get; private set; }

        /// <summary>
        /// Disposes of any resources owned by the class. If you override this type,
        /// you should call base.Dispose(disposing) and not dispose any parents resources
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _resource.Dispose();
            }
        }

        /// <inheritdoc cref="IComType"/>
        public void Dispose()
        {
            _resource.Dispose();
            Dispose(true);
            GC.SuppressFinalize(this);
        }

#if TRACE_DISPOSABLES || DEBUG
        /// <summary>
        /// go fuck yourself roslyn why the fucking fuckeroni do finalizers need xml comments fucking fuck off fucking twatty compiler
        /// </summary>
        ~GraphicsResource()
        {
            Guard.MarkDisposableFinalizerEntered();
            ThrowHelper.NeverReached();
        }
#endif
    }
}
