using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using TerraFX.Interop;
using Voltium.Common;
using Voltium.Core.Pipeline;
using Buffer = Voltium.Core.Memory.Buffer;

namespace Voltium.Core
{
    /// <summary>
    /// Wraps a <see cref="Buffer"/> for using it as a raytracing shader record
    /// </summary>
    public unsafe readonly struct ShaderRecord
    {
        private readonly UniqueComPtr<ID3D12StateObjectProperties> _properties;
        internal readonly D3D12_GPU_VIRTUAL_ADDRESS_RANGE Range;
        private readonly ulong* _pRecord;


        /// <summary>
        /// Creates a new <see cref="ShaderRecord"/> over a <see cref="Buffer"/>
        /// </summary>
        /// <param name="pso">The <see cref="RaytracingPipelineStateObject"/> used to retrieve shader data from</param>
        /// <param name="buffer">The <see cref="Buffer"/> to create the table over</param>
        /// <param name="recordSize">The size, in bytes, of the <see cref="ShaderRecord"/></param>
        public ShaderRecord(RaytracingPipelineStateObject pso, in Buffer buffer, ulong recordSize)
        {
            Guard.True(buffer.Length >= recordSize);
            Guard.True(pso.Pointer.TryQueryInterface(out _properties));
            Range = new D3D12_GPU_VIRTUAL_ADDRESS_RANGE { SizeInBytes = recordSize, StartAddress = buffer.GpuAddress };
            _pRecord = (ulong*)buffer.CpuAddress;
        }

        internal ShaderRecord(UniqueComPtr<ID3D12StateObjectProperties> properties, D3D12_GPU_VIRTUAL_ADDRESS_RANGE range, void* pRecord)
        {
            _properties = properties;
            Range = range;
            _pRecord = (ulong*)pRecord;
        }

        public static uint ShaderIdentifierSize => Windows.D3D12_SHADER_IDENTIFIER_SIZE_IN_BYTES;

        [StructLayout(LayoutKind.Sequential, Size = Windows.D3D12_SHADER_IDENTIFIER_SIZE_IN_BYTES)]
        private struct ShaderIdentifier
        {

        }

        /// <summary>
        /// The name of the shader invoked by this table. This is set-only
        /// </summary>
        public void SetShaderName(string s) => ShaderName = s;

        /// <summary>
        /// The name of the shader invoked by this table. This is set-only
        /// </summary>
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

                    *(ShaderIdentifier*)_pRecord = *pIdentifier;
                }
            }
        }

        private const uint ShaderIdentifierOffset = Windows.D3D12_SHADER_IDENTIFIER_SIZE_IN_BYTES / sizeof(ulong);

        /// <summary>
        /// Write an descriptor table to the shader table
        /// </summary>
        /// <param name="eightByteOffset">The eight-byte offset to write the descriptor table to</param>
        /// <param name="handle">The <see cref="DescriptorHandle"/> indicating the start of the descriptor table</param>
        public void WriteDescriptorTable(uint eightByteOffset, DescriptorHandle handle)
        {
            _pRecord[eightByteOffset + ShaderIdentifierOffset] = handle.GpuHandle.ptr;
        }

        /// <summary>
        /// Write an individual descriptor to the shader table
        /// </summary>
        /// <param name="eightByteOffset">The eight-byte offset to write the descriptor to</param>
        /// <param name="buffer"></param>
        public void WriteDescriptor(uint eightByteOffset, in Buffer buffer)
        {
            _pRecord[eightByteOffset + ShaderIdentifierOffset] = buffer.GpuAddress;
        }

        /// <summary>
        /// Write the constants provided to the shader table
        /// </summary>
        /// <typeparam name="T">The type of the constant data</typeparam>
        /// <param name="eightByteOffset">The eight-byte offset to write the constants to</param>
        /// <param name="value">The value to write to the shader table</param>
        public void WriteConstants<T>(uint eightByteOffset, in T value) where T : unmanaged
        {
            Unsafe.As<ulong, T>(ref _pRecord[eightByteOffset + ShaderIdentifierOffset]) = value;
        }

        /// <summary>
        /// Write the constants provided to the shader table
        /// </summary>
        /// <typeparam name="T">The type of the constant data</typeparam>
        /// <param name="eightByteOffset">The eight-byte offset to write the constants to</param>
        /// <param name="values">The values to write to the shader table</param>
        public void WriteConstants<T>(uint eightByteOffset, ReadOnlySpan<T> values) where T : unmanaged
        {
            var offset = eightByteOffset + ShaderIdentifierOffset;
            values.CopyTo(MemoryMarshal.Cast<byte, T>(new Span<byte>(&_pRecord[offset], checked((int)(Range.SizeInBytes - offset)))));
        }
    }

    /// <summary>
    /// A table of <see cref="ShaderRecord"/>s
    /// </summary>
    public unsafe struct ShaderRecordTable
    {
        private UniqueComPtr<ID3D12StateObjectProperties> _properties;
        internal readonly D3D12_GPU_VIRTUAL_ADDRESS_RANGE_AND_STRIDE RangeAndStride;
        private void* _pRecord;

        /// <summary>
        /// Creates a new <see cref="ShaderRecordTable"/> over a <see cref="Buffer"/>
        /// </summary>
        /// <param name="pso">The <see cref="RaytracingPipelineStateObject"/> used to retrieve shader data from</param>
        /// <param name="buffer">The <see cref="Buffer"/> to create the table over</param>
        /// <param name="numRecords">The number of <see cref="ShaderRecord"/>s to create over the buffer</param>
        /// <param name="recordSize">The size, in bytes, of each <see cref="ShaderRecord"/></param>
        public ShaderRecordTable(RaytracingPipelineStateObject pso, in Buffer buffer, ulong numRecords, ulong recordSize)
        {
            Guard.True(buffer.Length >= numRecords * recordSize);
            Guard.True(pso.Pointer.TryQueryInterface(out _properties));
            RangeAndStride = new D3D12_GPU_VIRTUAL_ADDRESS_RANGE_AND_STRIDE { SizeInBytes = numRecords * recordSize, StartAddress = buffer.GpuAddress, StrideInBytes = recordSize };
            _pRecord = buffer.CpuAddress;
        }

        /// <summary>
        /// Retrieves the <see cref="ShaderRecord"/> for a given index
        /// </summary>
        /// <param name="index">The index of the <see cref="ShaderRecord"/> to retrieve</param>
        /// <returns>A <see cref="ShaderRecord"/></returns>
        public ShaderRecord this[int index]
        {
            get
            {
                Guard.InRangeInclusive(index, 0, Count - 1);

                return new ShaderRecord(_properties, RangeAndStride.AsRange(), _pRecord);
            }
        }

        /// <summary>
        /// The number of <see cref="ShaderRecord"/>s in this table
        /// </summary>
        public int Count => (int)(RangeAndStride.SizeInBytes / RangeAndStride.StrideInBytes);
    }

    internal static class AddressRangeAndStrideExtensions
    {
        public static D3D12_GPU_VIRTUAL_ADDRESS_RANGE AsRange(in this D3D12_GPU_VIRTUAL_ADDRESS_RANGE_AND_STRIDE obj) => new D3D12_GPU_VIRTUAL_ADDRESS_RANGE { StartAddress = obj.StartAddress, SizeInBytes = obj.SizeInBytes };
    }
}
