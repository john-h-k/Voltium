using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Voltium.Core.Infrastructure;
using Voltium.Core.Memory;

namespace Voltium.Core.Devices
{
    public partial class ComputeDevice
    {

        /// <summary>
        /// The <see cref="Adapter"/> this device uses
        /// </summary>
        public Adapter Adapter { get; private set; }

        /// <summary>
        /// The default allocator for the device
        /// </summary>
        public GpuAllocator Allocator { get; private protected set; }

        /// <summary>
        /// The default pipeline manager for the device
        /// </summary>
        public PipelineManager PipelineManager { get; private protected set; }



        private ref CommandQueue GetQueueForContext(ExecutionContext context)
        {
            switch (context)
            {
                case ExecutionContext.Copy:
                    return ref CopyQueue;
                case ExecutionContext.Compute:
                    return ref ComputeQueue;
                case ExecutionContext.Graphics when this is GraphicsDevice:
                    return ref GraphicsQueue;
            }

            return ref Unsafe.NullRef<CommandQueue>();
        }
    }
}
