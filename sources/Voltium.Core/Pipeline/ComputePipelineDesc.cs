using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using TerraFX.Interop;
using Voltium.Core.Devices;
using Voltium.Core.Memory;
using Voltium.Core.NativeApi;

namespace Voltium.Core.Pipeline
{
    /// <summary>
    /// Describes the state and settings of a compute pipeline
    /// </summary>
    public unsafe partial struct ComputePipelineDesc
    {
        /// <summary>
        /// The <see cref="RootSignatureHandle"/> for this pipeline
        /// </summary>
        public RootSignature? RootSignature;

        /// <summary>
        /// The compute shader for the pipeline
        /// </summary>
        public CompiledShader ComputeShader;

        /// <summary>
        /// Which nodes this pipeline is valid to be used on
        /// </summary>
        public uint NodeMask;
    }

    /// <summary>
    /// Describes the state and settings of a compute pipeline
    /// </summary>
    public unsafe partial struct NativeComputePipelineDesc
    {
        /// <summary>
        /// The <see cref="RootSignatureHandle"/> for this pipeline
        /// </summary>
        public RootSignatureHandle RootSignature;

        /// <summary>
        /// The compute shader for the pipeline
        /// </summary>
        public CompiledShader ComputeShader;

        /// <summary>
        /// Which nodes this pipeline is valid to be used on
        /// </summary>
        public uint NodeMask;
    }

    public struct TriangleHitGroup
    {
        public string Name;
        public string? ClosestHitShader;
        public string? AnyHitShader;
    }

    public struct ProceduralPrimitiveHitGroup
    {
        public string Name;
        public string? ClosestHitShader;
        public string? AnyHitShader;
        public string? IntersectionShader;
    }

    public struct LocalRootSignatureAssociation
    {
        public LocalRootSignatureHandle RootSignature;
        public ReadOnlyMemory<string> Associations;
    }

    public struct ShaderLibrary
    {
        public CompiledShader Library;
        public ReadOnlyMemory<ShaderExport> Exports;
    }

    /// <summary>
    /// Describes the state and settings of a compute pipeline
    /// </summary>
    public unsafe partial struct NativeRaytracingPipelineDesc
    {
        public ReadOnlyMemory<ShaderLibrary> Libraries;
        public ReadOnlyMemory<LocalRootSignatureAssociation> LocalRootSignatures;
        public ReadOnlyMemory<ProceduralPrimitiveHitGroup> ProceduralPrimitiveHitGroups;
        public ReadOnlyMemory<TriangleHitGroup> TriangleHitGroups;

        public RootSignatureHandle RootSignature;

        public uint MaxPayloadSize;
        public uint MaxAttributeSize;
        public uint MaxRecursionDepth;

        /// <summary>
        /// Which nodes this pipeline is valid to be used on
        /// </summary>
        public uint NodeMask;
    }
}
