using System;
using System.Collections.Generic;

using System.Text;
using System.Threading.Tasks;
using TerraFX.Interop;
using Voltium.Common;
using Voltium.Core.Devices;
using Voltium.Core.Memory;
using static TerraFX.Interop.Windows;
using Buffer = Voltium.Core.Memory.Buffer;

namespace Voltium.Core.Queries
{
    public unsafe struct QueryHeap
    {
        internal QuerySetHandle Handle;
        public readonly uint Length;

        private Disposal<QuerySetHandle> _dispose;

        /// <inheritdoc/>
        public void Dispose() => _dispose.Dispose(ref Handle);
    }

}
