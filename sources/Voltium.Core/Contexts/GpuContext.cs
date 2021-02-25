using System;
using System.Buffers;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.Toolkit.HighPerformance;
using SixLabors.ImageSharp.PixelFormats;
using TerraFX.Interop;
using Voltium.Common;
using Voltium.Core.Contexts;
using Voltium.Core.Devices;
using Voltium.Core.Memory;
using Voltium.Core.Pipeline;
using Voltium.Core.Pool;
using Voltium.Core.Queries;
using Buffer = Voltium.Core.Memory.Buffer;

namespace Voltium.Core
{

    /// <summary>
    /// Represents a generic Gpu context
    /// </summary>
    public unsafe partial class GpuContext : IDisposable
    {
        internal List<IDisposable?> AttachedResources { get; private set; } = new();

        private protected ContextEncoder<PooledBufferWriter<byte>> _encoder;
        private protected GraphicsDevice _device;

        internal GpuContext(GraphicsDevice device)
        {
            _device = device;
            _encoder = ContextEncoder.Create(new PooledBufferWriter<byte>(MemoryPool<byte>.Shared));
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
        }
    }
}
