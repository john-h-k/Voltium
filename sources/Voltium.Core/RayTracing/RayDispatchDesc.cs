using TerraFX.Interop;
using Buffer = Voltium.Core.Memory.Buffer;

namespace Voltium.Core
{
    public struct RayDispatchDesc
    {
        internal D3D12_DISPATCH_RAYS_DESC Desc;

        internal Buffer RayGenBuffer, HitBuffer, MissBuffer, CallableBuffer;


        public ShaderTable RayGenerationShaderRecord
        {
            set
            {
                Desc.RayGenerationShaderRecord.StartAddress = value.RangeAndStride.StartAddress;
                Desc.RayGenerationShaderRecord.SizeInBytes = value.RangeAndStride.SizeInBytes;
            }
            get => new ShaderTable(Desc.RayGenerationShaderRecord, Desc.RayGenerationShaderRecord.SizeInBytes);
        }


        public ShaderTable MissShaderTable
        {
            set
            {
                Desc.MissShaderTable = value.RangeAndStride;
            }
            get => new ShaderTable(Desc.MissShaderTable);
        }
        public ShaderTable HitGroupTable
        {
            set
            {
                Desc.HitGroupTable = value.RangeAndStride;
            }
            get => new ShaderTable(Desc.HitGroupTable);
        }
        public ShaderTable CallableShaderTable
        {
            set
            {
                Desc.CallableShaderTable = value.RangeAndStride;
            }
            get => new ShaderTable(Desc.CallableShaderTable);
        }

        public uint Width { get => Desc.Width; set => Desc.Width = value; }
        public uint Height { get => Desc.Height; set => Desc.Height = value; }
        public uint Depth { get => Desc.Depth; set => Desc.Depth = value; }
    }
}
