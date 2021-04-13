using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;
using TerraFX.Interop;
using Voltium.Allocators;
using Voltium.Common;
using Voltium.Core.Devices;

namespace Voltium.Core.Pipeline
{
    /// <summary>
    /// Describes the state and settings of a raytracing pipeline
    /// </summary>
    public unsafe sealed class RaytracingPipelineDesc
    {
        public RaytracingPipelineDesc()
        {
            _libraries = new(1);
            _triangleHitGroups = new(1);
            _proceduralPrimitiveHitGroups = new(1);
            _localRootSigs = new(1);
            NodeMask = 1;
        }

        public void AddLibrary(CompiledShader library, params ShaderExport[] exports)
            => AddLibrary(library, exports.AsMemory());

        public void AddLibrary(CompiledShader library, ReadOnlyMemory<ShaderExport> exports)
        {
            _libraries.Add(new ShaderLibrary { Library = library, Exports = exports });
            _totalNumExports += exports.Length;
            _totalNumSubObjects++;
        }

        public void AddLocalRootSignature(LocalRootSignature signature, params string[] associations)
            => AddLocalRootSignature(signature, associations.AsMemory());

        public void AddLocalRootSignature(LocalRootSignature signature, ReadOnlyMemory<string> associations)
        {
            _localRootSigs.Add(new LocalRootSignatureAssociation { RootSignature = signature.Handle, Associations = associations });
            _totalNumAssociations += associations.Length;
            _totalNumAssocationObjects += associations.IsEmpty ? 0 : 1;
            _totalNumSubObjects += associations.IsEmpty ? 1 : 2;
        }


        public void AddTriangleHitGroup(string name, string? closestHitShader, string? anyHitShader)
        {
            _triangleHitGroups.Add(new TriangleHitGroup
            {
                Name = name,
                AnyHitShader = anyHitShader,
                ClosestHitShader = closestHitShader
            });
            _totalNumSubObjects++;
        }

        public void AddProceduralPrimitiveHitGroup(string name, string? closestHitShader, string? anyHitShader, string? intersectionShader)
        {
            _proceduralPrimitiveHitGroups.Add(new ProceduralPrimitiveHitGroup
            {
                Name = name,
                ClosestHitShader = closestHitShader,
                AnyHitShader = anyHitShader,
                IntersectionShader = intersectionShader
            });
            _totalNumSubObjects++;
        }

        /// <summary>
        /// The global root signature for the pipeline
        /// </summary>
        public RootSignature? GlobalRootSignature { get; set; }

        /// <summary>
        /// The max payload size in shader exports, in bytes
        /// </summary>
        public uint MaxPayloadSize { get; set; }

        /// <summary>
        /// The max attribute size in shader exports, in bytes
        /// </summary>
        public uint MaxAttributeSize { get; set; }

        /// <summary>
        /// The max recursion depth a given ray can go to. Attempting to recurse further than this in a shader results in undefined-behaviour
        /// and possibly device removal
        /// </summary>
        public uint MaxRecursionDepth { get; set; }

        /// <summary>
        /// The node mask for the raytracing PSO
        /// </summary>
        public uint NodeMask { get; set; }

        private enum HitGroupType
        {
            Triangle = D3D12_HIT_GROUP_TYPE.D3D12_HIT_GROUP_TYPE_TRIANGLES,
            Procedural = D3D12_HIT_GROUP_TYPE.D3D12_HIT_GROUP_TYPE_PROCEDURAL_PRIMITIVE
        }

        internal Span<ShaderLibrary> Libraries => _libraries.AsSpan();
        internal Span<LocalRootSignatureAssociation> LocalRootSignatures => _localRootSigs.AsSpan();
        internal Span<TriangleHitGroup> TriangleHitGroups => _triangleHitGroups.AsSpan();
        internal Span<ProceduralPrimitiveHitGroup> ProceduralPrimitiveHitGroups => _proceduralPrimitiveHitGroups.AsSpan();

        private int _totalNumExports;
        private int _totalNumAssocationObjects;
        private int _totalNumAssociations;
        private int _totalNumSubObjects = 4;
        private ValueList<LocalRootSignatureAssociation> _localRootSigs;
        private ValueList<ShaderLibrary> _libraries;
        private ValueList<TriangleHitGroup> _triangleHitGroups;
        private ValueList<ProceduralPrimitiveHitGroup> _proceduralPrimitiveHitGroups;

    }

    // TODO support Tier1.1
    //public enum RayTracingPipelineFlags
    //{
    //    None,
    //    SkipTriangles = D3D12_RAYTRACING_PIPELINE_FLAGS.D3D12_RAYTRACING_PIPELINE_FLAG_SKIP_TRIANGLES,
    //    SkipProceduralPrimitives = D3D12_RAYTRACING_PIPELINE_FLAGS.D3D12_RAYTRACING_PIPELINE_FLAG_SKIP_PROCEDURAL_PRIMITIVES,
    //}
}
