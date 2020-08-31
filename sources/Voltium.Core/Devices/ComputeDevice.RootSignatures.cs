using System;
using System.Collections.Generic;

using System.Text;
using System.Threading.Tasks;
using TerraFX.Interop;
using Voltium.Common;

namespace Voltium.Core.Devices
{
    public unsafe partial class ComputeDevice
    {
        /// <summary>
        /// An empty <see cref="RootSignature"/>
        /// </summary>
        public RootSignature EmptyRootSignature { get; }

        /// <summary>
        /// Creates a new <see cref="RootSignature"/>
        /// </summary>
        /// <param name="rootParameter">The <see cref="RootParameter"/> in the signature</param>
        /// <param name="staticSampler">The <see cref="StaticSampler"/> in the signature</param>
        /// <returns>A new <see cref="RootSignature"/></returns>
        public RootSignature CreateRootSignature(in RootParameter rootParameter, in StaticSampler staticSampler)
            => CreateRootSignature(new[] { rootParameter }, new[] { staticSampler });

        /// <summary>
        /// Creates a new <see cref="RootSignature"/>
        /// </summary>
        /// <param name="rootParameters">The <see cref="RootParameter"/>s in the signature</param>
        /// <param name="staticSampler">The <see cref="StaticSampler"/> in the signature</param>
        /// <returns>A new <see cref="RootSignature"/></returns>
        public RootSignature CreateRootSignature(ReadOnlyMemory<RootParameter> rootParameters, in StaticSampler staticSampler)
            => CreateRootSignature(rootParameters, new[] { staticSampler });

        /// <summary>
        /// Creates a new <see cref="RootSignature"/>
        /// </summary>
        /// <param name="rootParameters">The <see cref="RootParameter"/>s in the signature</param>
        /// <param name="staticSamplers">The <see cref="StaticSampler"/>s in the signature</param>
        /// <returns>A new <see cref="RootSignature"/></returns>
        public RootSignature CreateRootSignature(ReadOnlyMemory<RootParameter> rootParameters, ReadOnlyMemory<StaticSampler> staticSamplers = default)
            => RootSignature.Create(this, rootParameters, staticSamplers);
    }
}
