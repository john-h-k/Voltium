using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using TerraFX.Interop;
using Voltium.Common;
using Buffer = Voltium.Core.Memory.Buffer;

namespace Voltium.Core
{
    public unsafe struct ShaderTable
    {
        private UniqueComPtr<ID3D12StateObjectProperties> _properties;
        private Buffer _buffer;
        internal D3D12_GPU_VIRTUAL_ADDRESS_RANGE_AND_STRIDE RangeAndStride;

        internal ShaderTable(D3D12_GPU_VIRTUAL_ADDRESS_RANGE range, ulong stride)
            : this(new D3D12_GPU_VIRTUAL_ADDRESS_RANGE_AND_STRIDE { StartAddress = range.StartAddress, SizeInBytes = range.SizeInBytes, StrideInBytes = stride })
        {
        }


        internal ShaderTable(in D3D12_GPU_VIRTUAL_ADDRESS_RANGE_AND_STRIDE range)
        {
            RangeAndStride = range;
        }

        [StructLayout(LayoutKind.Sequential, Size = Windows.D3D12_SHADER_IDENTIFIER_SIZE_IN_BYTES)]
        private struct ShaderIdentifier
        {

        }

        public int RecordCount => (int)(RangeAndStride.SizeInBytes / RangeAndStride.StrideInBytes);

        public string ShaderName
        {
            set
            {   
                fixed (char* p = value)
                {
                    ShaderIdentifier* pIdentifier = (ShaderIdentifier*)_properties.Ptr->GetShaderIdentifier((ushort*)p);

                    if (pIdentifier is null)
                    {
                        ThrowHelper.ThrowArgumentException("Shader name invalid (ID3D12StateObjectProperties::GetShaderIdentifier returned null)");
                    }

                    *_buffer.As<ShaderIdentifier>() = *pIdentifier;
                }
            }
        }

        private const uint ShaderIdentifierOffset = Windows.D3D12_SHADER_IDENTIFIER_SIZE_IN_BYTES / sizeof(ulong);

        public void WriteDescriptorTable(uint eightByteOffset, DescriptorHandle handle)
        {
            _buffer.As<ulong>()[eightByteOffset + ShaderIdentifierOffset] = handle.GpuHandle.ptr;
        }

        public void WriteDescriptor(uint eightByteOffset, in Buffer buffer)
        {
            _buffer.As<ulong>()[eightByteOffset + ShaderIdentifierOffset] = buffer.GpuAddress;
        }

        public void WriteConstants<T>(uint eightByteOffset, in T value) where T : unmanaged
        {
            Unsafe.As<ulong, T>(ref _buffer.As<ulong>()[eightByteOffset + ShaderIdentifierOffset]) = value;
        }
    }
}
