using TerraFX.Interop;
using Buffer = Voltium.Core.Memory.Buffer;

namespace Voltium.Core
{
    /// <summary>
    /// Describes the parameters used in a call to <see cref="ComputeContext.DispatchRays"/>
    /// </summary>
    public struct RayDispatchDesc
    {
        internal D3D12_DISPATCH_RAYS_DESC Desc;

        internal Buffer RayGenBuffer, HitBuffer, MissBuffer, CallableBuffer;

        /// <summary>
        /// The <see cref="ShaderRecord"/> used as the ray-generation shader
        /// </summary>
        public ShaderRecord RayGenerationShaderRecord
        {
            set => Desc.RayGenerationShaderRecord = value.Range;
        }

        /// <summary>
        /// The <see cref="ShaderRecordTable"/> for the miss shaders
        /// </summary>
        public ShaderRecordTable MissShaderTable
        {
            set => Desc.MissShaderTable = value.RangeAndStride;
        }

        /// <summary>
        /// The <see cref="ShaderRecordTable"/> for the hit group shaders
        /// </summary>
        public ShaderRecordTable HitGroupTable
        {
            set => Desc.HitGroupTable = value.RangeAndStride;
        }

        /// <summary>
        /// The <see cref="ShaderRecordTable"/> for any additional callable shaders
        /// </summary>
        public ShaderRecordTable CallableShaderTable
        {
            set => Desc.CallableShaderTable = value.RangeAndStride;
        }

        /// <summary>
        /// The width of the dispatch
        /// </summary>
        public uint Width { get => Desc.Width; set => Desc.Width = value; }

        /// <summary>
        /// The height of the dispatch
        /// </summary>
        public uint Height { get => Desc.Height; set => Desc.Height = value; }

        /// <summary>
        /// The depth of the dispatch
        /// </summary>
        public uint Depth { get => Desc.Depth; set => Desc.Depth = value; }
    }
}
