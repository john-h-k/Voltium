using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using TerraFX.Interop;
using Voltium.Common;
using Voltium.Core.Devices;
using Voltium.Core.Memory;
using Voltium.Core.NativeApi;

namespace Voltium.Core
{
    /// <summary>
    /// Defines a root signatuFUCK
    /// </summary>
    public unsafe struct LocalRootSignature : IDisposable
    {
        internal LocalRootSignatureHandle Handle;
        private Disposal<LocalRootSignatureHandle> _disposal;

        internal LocalRootSignature(LocalRootSignatureHandle handle, Disposal<LocalRootSignatureHandle> disposal)
        {
            Handle = handle;
            _disposal = disposal;
        }


        /// <inheritdoc/>
        public void Dispose()
        {
            _disposal.Dispose(ref Handle);
        }
    }

    /// <summary>
    /// Defines a root signatuFUCK
    /// </summary>
    public unsafe struct RootSignature : IDisposable
    {
        internal RootSignatureHandle Handle;
        private Disposal<RootSignatureHandle> _disposal;

        internal RootSignature(RootSignatureHandle handle, Disposal<RootSignatureHandle> disposal)
        {
            Handle = handle;
            _disposal = disposal;
        }


        /// <inheritdoc/>
        public void Dispose()
        {
            _disposal.Dispose(ref Handle);
        }
    }
}
