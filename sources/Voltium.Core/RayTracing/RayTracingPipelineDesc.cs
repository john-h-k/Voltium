//using System;
//using System.Runtime.CompilerServices;
//using TerraFX.Interop;
//using Voltium.Common;
//using Voltium.Core.Devices;

//namespace Voltium.Core.Pipeline
//{
//    /// <summary>
//    /// Describes the state and settings of a raytracing pipeline
//    /// </summary>
//    public unsafe sealed class RayTracingPipelineDesc
//    {
//        public RootSignature GlobalRootSignature { get => RootSignature.GetRootSig(_subobjects.GlobalRootSignature.Inner->pGlobalRootSignature); set => _subobjects.GlobalRootSignature.Inner->pGlobalRootSignature = value.Value; }

//        public RootSignature LocalRootSignature { get => RootSignature.GetRootSig(_subobjects.GlobalRootSignature.Inner->pGlobalRootSignature); set => _subobjects.GlobalRootSignature.Inner->pGlobalRootSignature = value.Value; }

//        public CompiledShader ShaderLibrary
//        {
//            get => new CompiledShader(_subobjects.DxilLibrary.Inner->DXILLibrary, ShaderType.Library);
//            set => _subobjects.DxilLibrary.Inner->DXILLibrary = new D3D12_SHADER_BYTECODE(value.Pointer, value.Length);
//        }

//        public ReadOnlySpan<ShaderExport> OptionalExports
//        {
//            get
//            {
//                return _subobjects.DxilLibrary.Inner->pExports is null ? ReadOnlySpan<ShaderExport>.Empty : new ReadOnlySpan<ShaderExport>(_subobjects.DxilLibrary.Inner->pExports, (int)_subobjects.DxilLibrary.Inner->NumExports);
//            }
//            set
//            {
//                _subobjects.DxilLibrary.Inner->pExports = (D3D12_EXPORT_DESC*)Helpers.MarshalToUnmanaged(value);
//                _subobjects.DxilLibrary.Inner->NumExports = (uint)value.Length;
//            }
//        }

//        /// <summary>
//        /// The max payload size in shader exports, in bytes
//        /// </summary>
//        public ref uint MaxPayloadSize => ref _subobjects.ShaderConfig.Inner->MaxPayloadSizeInBytes;


//        /// <summary>
//        /// The max attribute size in shader exports, in bytes
//        /// </summary>
//        public ref uint MaxAttributeSize => ref _subobjects.ShaderConfig.Inner->MaxAttributeSizeInBytes;

//        /// <summary>
//        /// The max recursion depth a given ray can go to. Attempting to recurse further than this in a shader results in undefined-behaviour
//        /// and possibly device removal
//        /// </summary>
//        public ref uint MaxRecursionDepth => ref _subobjects.PipelineConfig.Inner->MaxTraceRecursionDepth;

//        // TODO support Tier1.1
//        //public ref RayTracingPipelineFlags Flags => ref Unsafe.As<D3D12_RAYTRACING_PIPELINE_FLAGS, RayTracingPipelineFlags>(ref _subobjects.PipelineConfig1.Inner->Flags);


//        internal ref D3D12_STATE_SUBOBJECT GetPinnableReference() => ref Unsafe.As<SubobjectArray, D3D12_STATE_SUBOBJECT>(ref _subobjects);
//        internal uint SubobjectCount => (uint)(sizeof(SubobjectArray) / sizeof(D3D12_STATE_SUBOBJECT));

//        private SubobjectArray _subobjects;

//        private struct SubobjectArray
//        {
//            public Subobject<D3D12_DXIL_LIBRARY_DESC> DxilLibrary;
//            public Subobject<D3D12_HIT_GROUP_DESC> HitGroup;
//            public Subobject<D3D12_RAYTRACING_PIPELINE_CONFIG> PipelineConfig;
//            public Subobject<D3D12_RAYTRACING_SHADER_CONFIG> ShaderConfig;
//            public Subobject<D3D12_GLOBAL_ROOT_SIGNATURE> GlobalRootSignature;
//            public Subobject<D3D12_LOCAL_ROOT_SIGNATURE> LocalRootSignature;
//        }

//        private struct Subobject<T> where T : unmanaged
//        {
//            private D3D12_SUBOBJECT_TO_EXPORTS_ASSOCIATION Desc;

//            public D3D12_STATE_SUBOBJECT_TYPE Type;
//            public T* Inner;
//        }
//    }

//    // TODO support Tier1.1
//    //public enum RayTracingPipelineFlags
//    //{
//    //    None,
//    //    SkipTriangles = D3D12_RAYTRACING_PIPELINE_FLAGS.D3D12_RAYTRACING_PIPELINE_FLAG_SKIP_TRIANGLES,
//    //    SkipProceduralPrimitives = D3D12_RAYTRACING_PIPELINE_FLAGS.D3D12_RAYTRACING_PIPELINE_FLAG_SKIP_PROCEDURAL_PRIMITIVES,
//    //}
//}
