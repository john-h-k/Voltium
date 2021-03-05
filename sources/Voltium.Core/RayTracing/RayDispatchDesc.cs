//using TerraFX.Interop;
//using Voltium.Core.Contexts;
//using Buffer = Voltium.Core.Memory.Buffer;

//namespace Voltium.Core
//{
//    /// <summary>
//    /// Describes the parameters used in a call to <see cref="ComputeContext.DispatchRays(in RayDispatchDesc)"/>
//    /// </summary>
//    public struct RayDispatchDesc
//    {
//        internal D3D12_DISPATCH_RAYS_DESC Desc;

//        public void SetRayGenerationShaderRecord([RequiresResourceState(ResourceState.NonPixelShaderResource)] in Buffer buffer, uint length)
//        {
//            Desc.RayGenerationShaderRecord = new D3D12_GPU_VIRTUAL_ADDRESS_RANGE { SizeInBytes = length, StartAddress = buffer.GpuAddress };
//        }

//        public void SetMissShaderTable([RequiresResourceState(ResourceState.NonPixelShaderResource)] in Buffer buffer, uint length, uint count)
//        {
//            Desc.MissShaderTable = new D3D12_GPU_VIRTUAL_ADDRESS_RANGE_AND_STRIDE { SizeInBytes = length * count, StartAddress = buffer.GpuAddress, StrideInBytes = length };
//        }

//        public void SetHitGroupTable([RequiresResourceState(ResourceState.NonPixelShaderResource)] in Buffer buffer, uint length, uint count)
//        {
//            Desc.HitGroupTable = new D3D12_GPU_VIRTUAL_ADDRESS_RANGE_AND_STRIDE { SizeInBytes = length, StartAddress = buffer.GpuAddress, StrideInBytes = length };
//        }

//        public void SetCallableShaderTable([RequiresResourceState(ResourceState.NonPixelShaderResource)] in Buffer buffer, uint length, uint count)
//        {
//            Desc.CallableShaderTable = new D3D12_GPU_VIRTUAL_ADDRESS_RANGE_AND_STRIDE { SizeInBytes = length, StartAddress = buffer.GpuAddress, StrideInBytes = length };
//        }

//        /// <summary>
//        /// The <see cref="ShaderRecord"/> used as the ray-generation shader
//        /// </summary>
//        public ShaderRecord RayGenerationShaderRecord
//        {
//            set => Desc.RayGenerationShaderRecord = value.Range;
//        }

//        /// <summary>
//        /// The <see cref="ShaderRecordTable"/> for the miss shaders
//        /// </summary>
//        public ShaderRecordTable MissShaderTable
//        {
//            set => Desc.MissShaderTable = value.RangeAndStride;
//        }

//        /// <summary>
//        /// The <see cref="ShaderRecordTable"/> for the hit group shaders
//        /// </summary>
//        public ShaderRecordTable HitGroupTable
//        {
//            set => Desc.HitGroupTable = value.RangeAndStride;
//        }

//        /// <summary>
//        /// The <see cref="ShaderRecordTable"/> for any additional callable shaders
//        /// </summary>
//        public ShaderRecordTable CallableShaderTable
//        {
//            set => Desc.CallableShaderTable = value.RangeAndStride;
//        }

//        /// <summary>
//        /// The width of the dispatch
//        /// </summary>
//        public uint Width { get => Desc.Width; set => Desc.Width = value; }

//        /// <summary>
//        /// The height of the dispatch
//        /// </summary>
//        public uint Height { get => Desc.Height; set => Desc.Height = value; }

//        /// <summary>
//        /// The depth of the dispatch
//        /// </summary>
//        public uint Depth { get => Desc.Depth; set => Desc.Depth = value; }
//    }
//}
