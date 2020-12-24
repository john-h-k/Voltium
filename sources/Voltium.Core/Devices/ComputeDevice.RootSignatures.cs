using System;
using System.Collections.Generic;

using System.Text;
using System.Threading.Tasks;
using TerraFX.Interop;
using Voltium.Common;

namespace Voltium.Core.Devices
{
    public enum RootSignatureFlags
    {
        None = D3D12_ROOT_SIGNATURE_FLAGS.D3D12_ROOT_SIGNATURE_FLAG_NONE,
        AllowInputAssembler = D3D12_ROOT_SIGNATURE_FLAGS.D3D12_ROOT_SIGNATURE_FLAG_ALLOW_INPUT_ASSEMBLER_INPUT_LAYOUT,
        DenyPixelShaderAccess = D3D12_ROOT_SIGNATURE_FLAGS.D3D12_ROOT_SIGNATURE_FLAG_DENY_PIXEL_SHADER_ROOT_ACCESS,
        DenyVertexShaderAccess = D3D12_ROOT_SIGNATURE_FLAGS.D3D12_ROOT_SIGNATURE_FLAG_DENY_VERTEX_SHADER_ROOT_ACCESS,
        DenyHullShaderAccess = D3D12_ROOT_SIGNATURE_FLAGS.D3D12_ROOT_SIGNATURE_FLAG_DENY_HULL_SHADER_ROOT_ACCESS,
        DenyDomainShaderAccess = D3D12_ROOT_SIGNATURE_FLAGS.D3D12_ROOT_SIGNATURE_FLAG_DENY_DOMAIN_SHADER_ROOT_ACCESS,
        DenyMeshShaderAccess = D3D12_ROOT_SIGNATURE_FLAGS.D3D12_ROOT_SIGNATURE_FLAG_DENY_MESH_SHADER_ROOT_ACCESS,
        DenyAmplificationShaderAccess = D3D12_ROOT_SIGNATURE_FLAGS.D3D12_ROOT_SIGNATURE_FLAG_DENY_AMPLIFICATION_SHADER_ROOT_ACCESS
    }

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
        /// <param name="flags">The <see cref="RootSignatureFlags"/> for this signature</param>
        /// <returns>A new <see cref="RootSignature"/></returns>
        public RootSignature CreateRootSignature(in RootParameter rootParameter, RootSignatureFlags flags = RootSignatureFlags.None)
            => CreateRootSignature(new[] { rootParameter }, Array.Empty<StaticSampler>(), flags);

        /// <summary>
        /// Creates a new <see cref="RootSignature"/>
        /// </summary>
        /// <param name="rootParameter">The <see cref="RootParameter"/> in the signature</param>
        /// <param name="staticSampler">The <see cref="StaticSampler"/> in the signature</param>
        /// <param name="flags">The <see cref="RootSignatureFlags"/> for this signature</param>
        /// <returns>A new <see cref="RootSignature"/></returns>
        public RootSignature CreateRootSignature(in RootParameter rootParameter, in StaticSampler staticSampler, RootSignatureFlags flags = RootSignatureFlags.None)
            => CreateRootSignature(new[] { rootParameter }, new[] { staticSampler }, flags);

        /// <summary>
        /// Creates a new <see cref="RootSignature"/>
        /// </summary>
        /// <param name="rootParameters">The <see cref="RootParameter"/>s in the signature</param>
        /// <param name="staticSampler">The <see cref="StaticSampler"/> in the signature</param>
        /// <param name="flags">The <see cref="RootSignatureFlags"/> for this signature</param>
        /// <returns>A new <see cref="RootSignature"/></returns>
        public RootSignature CreateRootSignature(ReadOnlyMemory<RootParameter> rootParameters, in StaticSampler staticSampler, RootSignatureFlags flags = RootSignatureFlags.None)
            => CreateRootSignature(rootParameters, new[] { staticSampler }, flags);

        /// <summary>
        /// Creates a new <see cref="RootSignature"/>
        /// </summary>
        /// <param name="rootParameters">The <see cref="RootParameter"/>s in the signature</param>
        /// <param name="staticSamplers">The <see cref="StaticSampler"/>s in the signature</param>
        /// <param name="flags">The <see cref="RootSignatureFlags"/> for this signature</param>
        /// <returns>A new <see cref="RootSignature"/></returns>
        public RootSignature CreateRootSignature(ReadOnlyMemory<RootParameter> rootParameters, ReadOnlyMemory<StaticSampler> staticSamplers = default, RootSignatureFlags flags = RootSignatureFlags.None)
            => RootSignature.Create(this, rootParameters, staticSamplers, flags);

        /// <summary>
        /// Creates a new <see cref="RootSignature"/> for use as a raytracing local root signature
        /// </summary>
        /// <param name="rootParameter">The <see cref="RootParameter"/> in the signature</param>
        /// <param name="flags">The <see cref="RootSignatureFlags"/> for this signature</param>
        /// <returns>A new <see cref="RootSignature"/></returns>
        public RootSignature CreateLocalRootSignature(in RootParameter rootParameter, RootSignatureFlags flags = RootSignatureFlags.None)
            => CreateLocalRootSignature(new[] { rootParameter }, Array.Empty<StaticSampler>(), flags);

        /// <summary>
        /// Creates a new <see cref="RootSignature"/> for use as a raytracing local root signature
        /// </summary>
        /// <param name="rootParameter">The <see cref="RootParameter"/> in the signature</param>
        /// <param name="staticSampler">The <see cref="StaticSampler"/> in the signature</param>
        /// <param name="flags">The <see cref="RootSignatureFlags"/> for this signature</param>
        /// <returns>A new <see cref="RootSignature"/></returns>
        public RootSignature CreateLocalRootSignature(in RootParameter rootParameter, in StaticSampler staticSampler, RootSignatureFlags flags = RootSignatureFlags.None)
            => CreateLocalRootSignature(new[] { rootParameter }, new[] { staticSampler }, flags);

        /// <summary>
        /// Creates a new <see cref="RootSignature"/> for use as a raytracing local root signature
        /// </summary>
        /// <param name="rootParameters">The <see cref="RootParameter"/>s in the signature</param>
        /// <param name="staticSampler">The <see cref="StaticSampler"/> in the signature</param>
        /// <param name="flags">The <see cref="RootSignatureFlags"/> for this signature</param>
        /// <returns>A new <see cref="RootSignature"/></returns>
        public RootSignature CreateLocalRootSignature(ReadOnlyMemory<RootParameter> rootParameters, in StaticSampler staticSampler, RootSignatureFlags flags = RootSignatureFlags.None)
            => CreateLocalRootSignature(rootParameters, new[] { staticSampler }, flags);

        /// <summary>
        /// Creates a new <see cref="RootSignature"/> for use as a raytracing local root signature
        /// </summary>
        /// <param name="rootParameters">The <see cref="RootParameter"/>s in the signature</param>
        /// <param name="staticSamplers">The <see cref="StaticSampler"/>s in the signature</param>
        /// <param name="flags">The <see cref="RootSignatureFlags"/> for this signature</param>
        /// <returns>A new <see cref="RootSignature"/></returns>
        public RootSignature CreateLocalRootSignature(ReadOnlyMemory<RootParameter> rootParameters, ReadOnlyMemory<StaticSampler> staticSamplers = default, RootSignatureFlags flags = RootSignatureFlags.None)
            => RootSignature.Create(this, rootParameters, staticSamplers, (RootSignatureFlags)D3D12_ROOT_SIGNATURE_FLAGS.D3D12_ROOT_SIGNATURE_FLAG_LOCAL_ROOT_SIGNATURE | flags);
    }
}
