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
            _libraries.Add((library, exports));
            _totalNumExports += exports.Length;
            _totalNumSubObjects++;
        }

        public void AddLocalRootSignature(RootSignature signature, params string[] associations)
            => AddLocalRootSignature(signature, associations.Select(x => x.AsMemory()).ToArray());
        public void AddLocalRootSignature(RootSignature signature, params ReadOnlyMemory<char>[] associations)
            => AddLocalRootSignature(signature, associations.AsMemory());

        public void AddLocalRootSignature(RootSignature signature, ReadOnlyMemory<ReadOnlyMemory<char>> associations)
        {
            _localRootSigs.Add((signature, associations));
            _totalNumAssociations += associations.Length;
            _totalNumAssocationObjects += associations.IsEmpty ? 0 : 1;
            _totalNumSubObjects += associations.IsEmpty ? 1 : 2;
        }

        public void AddTriangleHitGroup(string? name, string? closestHitShader, string? anyHitShader)
            => AddTriangleHitGroup(name.AsMemory(), closestHitShader.AsMemory(), anyHitShader.AsMemory());

        public void AddTriangleHitGroup(ReadOnlyMemory<char> name, ReadOnlyMemory<char> closestHitShader, ReadOnlyMemory<char> anyHitShader)
        {
            _triangleHitGroups.Add((name, anyHitShader, closestHitShader));
            _totalNumSubObjects++;
        }

        public void AddProceduralPrimitiveHitGroup(string? name, string? closestHitShader, string? anyHitShader, string? intersectionShader)
            => AddProceduralPrimitiveHitGroup(name.AsMemory(), closestHitShader.AsMemory(), anyHitShader.AsMemory(), intersectionShader.AsMemory());

        public void AddProceduralPrimitiveHitGroup(ReadOnlyMemory<char> name, ReadOnlyMemory<char> closestHitShader, ReadOnlyMemory<char> anyHitShader, ReadOnlyMemory<char> intersectionShader)
        {
            _proceduralPrimitiveHitGroups.Add((name, anyHitShader, closestHitShader, intersectionShader));
            _totalNumSubObjects++;
        }

        /// <summary>
        /// The global root signature for the pipeline
        /// </summary>
        public RootSignature? GlobalRootSignature
        {
            get => RootSignature.GetRootSig(_initial.GlobalRootSig.pGlobalRootSignature);
            set => _initial.GlobalRootSig.pGlobalRootSignature = value is null ? null : value.Value;
        }

        /// <summary>
        /// The max payload size in shader exports, in bytes
        /// </summary>
        public uint MaxPayloadSize
        {
            get => _initial.ShaderConfig.MaxPayloadSizeInBytes;
            set => _initial.ShaderConfig.MaxPayloadSizeInBytes = value;
        }


        /// <summary>
        /// The max attribute size in shader exports, in bytes
        /// </summary>
        public uint MaxAttributeSize
        {
            get => _initial.ShaderConfig.MaxAttributeSizeInBytes;
            set => _initial.ShaderConfig.MaxAttributeSizeInBytes = value;
        }

        /// <summary>
        /// The max recursion depth a given ray can go to. Attempting to recurse further than this in a shader results in undefined-behaviour
        /// and possibly device removal
        /// </summary>
        public uint MaxRecursionDepth
        {
            get => _initial.PipelineConfig.MaxTraceRecursionDepth;
            set => _initial.PipelineConfig.MaxTraceRecursionDepth = value;
        }

        /// <summary>
        /// The node mask for the raytracing PSO
        /// </summary>
        public uint NodeMask
        {
            get => _initial.NodeMask.NodeMask;
            set => _initial.NodeMask.NodeMask = value;
        }

        // TODO support Tier1.1
        //public ref RayTracingPipelineFlags Flags => ref Unsafe.As<D3D12_RAYTRACING_PIPELINE_FLAGS, RayTracingPipelineFlags>(ref _subobjects.PipelineConfig1.Inner->Flags);

        internal struct Serialized : IDisposable
        {
            private D3D12_STATE_SUBOBJECT[] _subobjects;
            private byte[] _buffer;
            private MemoryHandle[] _handles;

            public Serialized(D3D12_STATE_SUBOBJECT[] subobjects, byte[] buffer, MemoryHandle[] handles)
            {
                _subobjects = subobjects;
                _buffer = buffer;
                _handles = handles;
            }

            public D3D12_STATE_OBJECT_DESC Desc => new D3D12_STATE_OBJECT_DESC
            {
                pSubobjects = Helpers.AddressOf(_subobjects),
                NumSubobjects = (uint)_subobjects.Length,
                Type = D3D12_STATE_OBJECT_TYPE.D3D12_STATE_OBJECT_TYPE_RAYTRACING_PIPELINE
            };

            public void Dispose()
            {
                _subobjects.AsSpan().Clear();
                _buffer.AsSpan().Clear();
                foreach (ref var handle in _handles.AsSpan())
                {
                    handle.Dispose();
                    handle = default;
                }
                this = default;
            }
        }

        private const uint
            MaxStringsPerAssociation = 1,
            MaxStringsPerExport = 2,
            MaxStringsPerTriangleHitGroup = 3,
            MaxStringsPerProceduralPrimitiveHitGroup = 4;

        internal Serialized Serialize()
        {
            var subObjectSizes = sizeof(InitialSubObjects) +
                (sizeof(D3D12_LOCAL_ROOT_SIGNATURE) * _localRootSigs.Length) +
                (sizeof(D3D12_SUBOBJECT_TO_EXPORTS_ASSOCIATION) * _totalNumAssocationObjects) +
                (sizeof(char*) * _totalNumAssociations) +
                (sizeof(D3D12_DXIL_LIBRARY_DESC) * _libraries.Length) +
                (sizeof(D3D12_EXPORT_DESC) * _totalNumExports) +
                (sizeof(D3D12_HIT_GROUP_DESC) * _triangleHitGroups.Length) +
                (sizeof(D3D12_HIT_GROUP_DESC) * _proceduralPrimitiveHitGroups.Length);

            var requiredHandles = (_totalNumExports * MaxStringsPerExport) +
                   _totalNumAssociations +
                   (_triangleHitGroups.Length * MaxStringsPerTriangleHitGroup) +
                   (_proceduralPrimitiveHitGroups.Length * MaxStringsPerProceduralPrimitiveHitGroup);

            var objects = GC.AllocateArray<D3D12_STATE_SUBOBJECT>(_totalNumSubObjects, pinned: true);
            var buffer = GC.AllocateArray<byte>(subObjectSizes, pinned: true);
            var handles = new MemoryHandle[requiredHandles];

            var objSpan = objects.AsSpan();
            var bufferSpan = buffer.AsSpan();
            var handleSpan = handles.AsSpan();

            SerializeInitialSubObjects(ref objSpan, ref bufferSpan, ref handleSpan);
            SerializeLibraries(ref objSpan, ref bufferSpan, ref handleSpan);
            SerializeLocalRootSigs(ref objSpan, ref bufferSpan, ref handleSpan);
            SerializeHitGroups(ref objSpan, ref bufferSpan, ref handleSpan);

            return new Serialized(objects, buffer, handles);
        }


        private struct InitialSubObjects
        {
            public D3D12_NODE_MASK NodeMask;
            public D3D12_RAYTRACING_PIPELINE_CONFIG PipelineConfig;
            public D3D12_RAYTRACING_SHADER_CONFIG ShaderConfig;
            public D3D12_GLOBAL_ROOT_SIGNATURE GlobalRootSig;
        }

        private void SerializeInitialSubObjects(ref Span<D3D12_STATE_SUBOBJECT> objects, ref Span<byte> buff, ref Span<MemoryHandle> handles)
        {
            var pStart = WriteAndAdvance(ref buff, _initial);
            WriteAndAdvance(ref objects, new D3D12_STATE_SUBOBJECT { Type = D3D12_STATE_SUBOBJECT_TYPE.D3D12_STATE_SUBOBJECT_TYPE_NODE_MASK, pDesc = &pStart->NodeMask });
            WriteAndAdvance(ref objects, new D3D12_STATE_SUBOBJECT { Type = D3D12_STATE_SUBOBJECT_TYPE.D3D12_STATE_SUBOBJECT_TYPE_RAYTRACING_PIPELINE_CONFIG, pDesc = &pStart->PipelineConfig });
            WriteAndAdvance(ref objects, new D3D12_STATE_SUBOBJECT { Type = D3D12_STATE_SUBOBJECT_TYPE.D3D12_STATE_SUBOBJECT_TYPE_RAYTRACING_SHADER_CONFIG, pDesc = &pStart->ShaderConfig });
            WriteAndAdvance(ref objects, new D3D12_STATE_SUBOBJECT { Type = D3D12_STATE_SUBOBJECT_TYPE.D3D12_STATE_SUBOBJECT_TYPE_GLOBAL_ROOT_SIGNATURE, pDesc = &pStart->GlobalRootSig });
        }

        private void SerializeLibraries(ref Span<D3D12_STATE_SUBOBJECT> objects, ref Span<byte> buff, ref Span<MemoryHandle> handles)
        {
            foreach (ref readonly var library in _libraries.AsSpan())
            {
                var pExports = (D3D12_EXPORT_DESC*)Helpers.AddressOf(buff);

                foreach (ref readonly var export in library.Exports.Span)
                {
                    var exportDesc = new D3D12_EXPORT_DESC();

                    var name = AddHandle(ref handles, export.Name);
                    exportDesc.Name = (ushort*)name.Pointer;

                    if (!export.ExportRename.IsEmpty)
                    {
                        var rename = AddHandle(ref handles, export.ExportRename);
                        exportDesc.Name = (ushort*)rename.Pointer;
                    }

                    WriteAndAdvance(ref buff, exportDesc);
                }

                var desc = new D3D12_DXIL_LIBRARY_DESC
                {
                    DXILLibrary = new D3D12_SHADER_BYTECODE { pShaderBytecode = library.Library.Pointer, BytecodeLength = library.Library.Length },
                    NumExports = (uint)library.Exports.Length,
                    pExports = library.Exports.IsEmpty ? null : pExports
                };


                var subObject = new D3D12_STATE_SUBOBJECT
                {
                    Type = D3D12_STATE_SUBOBJECT_TYPE.D3D12_STATE_SUBOBJECT_TYPE_DXIL_LIBRARY,
                    pDesc = Helpers.AddressOf(buff)
                };

                WriteAndAdvance(ref buff, desc);
                WriteAndAdvance(ref objects, subObject);
            }
        }

        private void SerializeLocalRootSigs(ref Span<D3D12_STATE_SUBOBJECT> objects, ref Span<byte> buff, ref Span<MemoryHandle> handles)
        {
            foreach (ref readonly var sig in _localRootSigs.AsSpan())
            {
                var desc = new D3D12_LOCAL_ROOT_SIGNATURE
                {
                    pLocalRootSignature = sig.RootSig.Value
                };

                var subObject = new D3D12_STATE_SUBOBJECT
                {
                    Type = D3D12_STATE_SUBOBJECT_TYPE.D3D12_STATE_SUBOBJECT_TYPE_LOCAL_ROOT_SIGNATURE,
                    pDesc = Helpers.AddressOf(buff)
                };

                WriteAndAdvance(ref buff, desc);
                var pSubObject = WriteAndAdvance(ref objects, subObject);

                if (sig.Associations.IsEmpty)
                {
                    return;
                }

                char** pAssociations = (char**)Helpers.AddressOf(buff);
                foreach (var associationName in sig.Associations.Span)
                {
                    var handle = AddHandle(ref handles, associationName);

                    var p = (nuint)handle.Pointer;
                    WriteAndAdvance(ref buff, p);
                }

                var association = new D3D12_SUBOBJECT_TO_EXPORTS_ASSOCIATION
                {
                    pSubobjectToAssociate = pSubObject,
                    NumExports = (uint)sig.Associations.Length,
                    pExports = (ushort**)pAssociations
                };

                var pAssociationDesc = WriteAndAdvance(ref buff, association);

                var subObjectAssociation = new D3D12_STATE_SUBOBJECT
                {
                    Type = D3D12_STATE_SUBOBJECT_TYPE.D3D12_STATE_SUBOBJECT_TYPE_SUBOBJECT_TO_EXPORTS_ASSOCIATION,
                    pDesc = pAssociationDesc
                };

                WriteAndAdvance(ref objects, subObjectAssociation);
            }
        }


        private void SerializeHitGroups(ref Span<D3D12_STATE_SUBOBJECT> objects, ref Span<byte> buff, ref Span<MemoryHandle> handles)
        {
            foreach (ref readonly var triangle in _triangleHitGroups.AsSpan())
            {
                var name = AddHandle(ref handles, triangle.HitGroupName);
                var closestHit = AddHandle(ref handles, triangle.ClosestHitShader);
                var anyHit = AddHandle(ref handles, triangle.AnyHitShader);

                var group = new D3D12_HIT_GROUP_DESC
                {
                    Type = D3D12_HIT_GROUP_TYPE.D3D12_HIT_GROUP_TYPE_TRIANGLES,
                    HitGroupExport = (ushort*)name.Pointer,
                    ClosestHitShaderImport = (ushort*)closestHit.Pointer,
                    AnyHitShaderImport = (ushort*)anyHit.Pointer,
                };

                AddSingleHitGroup(ref objects, ref buff, group);
            }

            foreach (ref readonly var primitive in _proceduralPrimitiveHitGroups.AsSpan())
            {
                var name = AddHandle(ref handles, primitive.HitGroupName);
                var closestHit = AddHandle(ref handles, primitive.ClosestHitShader);
                var anyHit = AddHandle(ref handles, primitive.AnyHitShader);
                var intersection = AddHandle(ref handles, primitive.IntersectionShader);

                var group = new D3D12_HIT_GROUP_DESC
                {
                    Type = D3D12_HIT_GROUP_TYPE.D3D12_HIT_GROUP_TYPE_PROCEDURAL_PRIMITIVE,
                    HitGroupExport = (ushort*)name.Pointer,
                    ClosestHitShaderImport = (ushort*)closestHit.Pointer,
                    AnyHitShaderImport = (ushort*)anyHit.Pointer,
                    IntersectionShaderImport = (ushort*)intersection.Pointer,
                };

                AddSingleHitGroup(ref objects, ref buff, group);
            }
        }
        private void AddSingleHitGroup(ref Span<D3D12_STATE_SUBOBJECT> objects, ref Span<byte> buff, in D3D12_HIT_GROUP_DESC hitGroup)
        {
            var pBuff = WriteAndAdvance(ref buff, hitGroup);

            var obj = new D3D12_STATE_SUBOBJECT
            {
                Type = D3D12_STATE_SUBOBJECT_TYPE.D3D12_STATE_SUBOBJECT_TYPE_HIT_GROUP,
                pDesc = pBuff
            };

            WriteAndAdvance(ref objects, obj);
        }

        private MemoryHandle AddHandle(ref Span<MemoryHandle> handles, ReadOnlyMemory<char> memory)
        {
            var handle = memory.Pin();
            handles[0] = handle;
            handles = handles[1..];

            return handle;
        }

        private T* WriteAndAdvance<T>(ref Span<byte> buff, in T val) where T : unmanaged
        {
            var addr = Helpers.AddressOf(buff);
            MemoryMarshal.Write(buff, ref Unsafe.AsRef(in val));
            buff = buff[sizeof(T)..];
            return (T*)addr;
        }

        private D3D12_STATE_SUBOBJECT* WriteAndAdvance(ref Span<D3D12_STATE_SUBOBJECT> objects, in D3D12_STATE_SUBOBJECT obj)
        {
            var addr = Helpers.AddressOf(objects);
            objects[0] = obj;
            objects = objects[1..];
            return addr;
        }


        private InitialSubObjects _initial;

        private enum HitGroupType
        {
            Triangle = D3D12_HIT_GROUP_TYPE.D3D12_HIT_GROUP_TYPE_TRIANGLES,
            Procedural = D3D12_HIT_GROUP_TYPE.D3D12_HIT_GROUP_TYPE_PROCEDURAL_PRIMITIVE
        }

        private int _totalNumExports;
        private int _totalNumAssocationObjects;
        private int _totalNumAssociations;
        private int _totalNumSubObjects = 4;
        private ValueList<(RootSignature RootSig, ReadOnlyMemory<ReadOnlyMemory<char>> Associations)> _localRootSigs;
        private ValueList<(CompiledShader Library, ReadOnlyMemory<ShaderExport> Exports)> _libraries;
        private ValueList<(ReadOnlyMemory<char> HitGroupName, ReadOnlyMemory<char> AnyHitShader, ReadOnlyMemory<char> ClosestHitShader)> _triangleHitGroups;
        private ValueList<(ReadOnlyMemory<char> HitGroupName, ReadOnlyMemory<char> AnyHitShader, ReadOnlyMemory<char> ClosestHitShader, ReadOnlyMemory<char> IntersectionShader)> _proceduralPrimitiveHitGroups;

    }

    // TODO support Tier1.1
    //public enum RayTracingPipelineFlags
    //{
    //    None,
    //    SkipTriangles = D3D12_RAYTRACING_PIPELINE_FLAGS.D3D12_RAYTRACING_PIPELINE_FLAG_SKIP_TRIANGLES,
    //    SkipProceduralPrimitives = D3D12_RAYTRACING_PIPELINE_FLAGS.D3D12_RAYTRACING_PIPELINE_FLAG_SKIP_PROCEDURAL_PRIMITIVES,
    //}
}
