using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using TerraFX.Interop;
using Voltium.Common;
using Voltium.Core.Devices;
using Voltium.Core.Memory;
using Voltium.Core.Pool;
using Buffer = Voltium.Core.Memory.Buffer;

namespace Voltium.Core
{
    /// <summary>
    /// Represents a generic Gpu context
    /// </summary>
    public unsafe partial class GpuContext : IDisposable
    {
        internal ContextParams Params;
        internal List<IDisposable?> AttachedResources { get; private set; } = new();

        internal ID3D12GraphicsCommandList* GetListPointer() => (ID3D12GraphicsCommandList*)List;

        internal ID3D12GraphicsCommandList6* List => Params.List.Ptr;
        internal ID3D12CommandAllocator* Allocator => Params.Allocator.Ptr;
        internal ComputeDevice Device => Params.Device;

        internal ExecutionContext Context => Params.Context;

        internal GpuContext(in ContextParams @params)
        {
            Params = @params;
            // We can't read past this many buffers as we skip init'ing them

            // Don't bother zero'ing expensive buffer
        }

        [VariadicGeneric("Attach(%t); %t = default!;", minNumberArgs: 1)]
        public void Attach<T0>(ref T0 t0)
            where T0 : IDisposable
        {
            Attach(t0);
            t0 = default!;
            VariadicGenericAttribute.InsertExpressionsHere();
        }

        private void Attach(IDisposable resource)
        {
            AttachedResources.Add(resource);
        }

        /// <summary>
        /// Submits this context to the device
        /// </summary>
        public virtual void Close() => Dispose();

        /// <summary>
        /// Submits this context to the device
        /// </summary>
        public virtual void Dispose()
        {
            Params.Device.ThrowIfFailed(List->Close());
            if (Params.Flags is ContextFlags.ExecuteOnClose or ContextFlags.BlockOnClose)
            {
                var task = Params.Device.Execute(this);
                if (Params.Flags is ContextFlags.BlockOnClose)
                {
                    task.Block();
                }
            }
        }
    }
}
