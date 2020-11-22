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
            => RootSignature.Create(this, rootParameters, staticSamplers, /* TODO */ D3D12_ROOT_SIGNATURE_FLAGS.D3D12_ROOT_SIGNATURE_FLAG_NONE);


        /// <summary>
        /// Creates a new <see cref="RootSignature"/> for use as a raytracing local root signature
        /// </summary>
        /// <param name="rootParameter">The <see cref="RootParameter"/> in the signature</param>
        /// <param name="staticSampler">The <see cref="StaticSampler"/> in the signature</param>
        /// <returns>A new <see cref="RootSignature"/></returns>
        public RootSignature CreateLocalRootSignature(in RootParameter rootParameter, in StaticSampler staticSampler)
            => CreateLocalRootSignature(new[] { rootParameter }, new[] { staticSampler });

        /// <summary>
        /// Creates a new <see cref="RootSignature"/> for use as a raytracing local root signature
        /// </summary>
        /// <param name="rootParameters">The <see cref="RootParameter"/>s in the signature</param>
        /// <param name="staticSampler">The <see cref="StaticSampler"/> in the signature</param>
        /// <returns>A new <see cref="RootSignature"/></returns>
        public RootSignature CreateLocalRootSignature(ReadOnlyMemory<RootParameter> rootParameters, in StaticSampler staticSampler)
            => CreateLocalRootSignature(rootParameters, new[] { staticSampler });

        /// <summary>
        /// Creates a new <see cref="RootSignature"/> for use as a raytracing local root signature
        /// </summary>
        /// <param name="rootParameters">The <see cref="RootParameter"/>s in the signature</param>
        /// <param name="staticSamplers">The <see cref="StaticSampler"/>s in the signature</param>
        /// <returns>A new <see cref="RootSignature"/></returns>
        public RootSignature CreateLocalRootSignature(ReadOnlyMemory<RootParameter> rootParameters, ReadOnlyMemory<StaticSampler> staticSamplers = default)
            => RootSignature.Create(this, rootParameters, staticSamplers, D3D12_ROOT_SIGNATURE_FLAGS.D3D12_ROOT_SIGNATURE_FLAG_LOCAL_ROOT_SIGNATURE);
    }
}
