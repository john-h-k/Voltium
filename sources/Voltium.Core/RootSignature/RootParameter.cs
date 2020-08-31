using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using TerraFX.Interop;

namespace Voltium.Core
{
    /// <summary>
    /// Represents an individual parameter of a <see cref="RootSignature"/>
    /// </summary>
    public readonly struct RootParameter
    {
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static uint Num32BitValues<T>() => (uint)Unsafe.SizeOf<T>() / 4;

        /// <summary>
        /// The <see cref="RootParameterType"/> of this parameter
        /// </summary>
        public readonly RootParameterType Type;

        /// <summary>
        /// Indicates which shaders can access this parameter
        /// </summary>
        public readonly ShaderVisibility Visibility;

        /// <summary>
        /// Creates a new directly-bound descriptor root parameter
        /// </summary>
        /// <param name="type">The type of the descriptor to bind. This must either be <see cref="RootParameterType.ConstantBufferView"/>,
        /// <see cref="RootParameterType.ShaderResourceView"/>, or <see cref="RootParameterType.UnorderedAccessView"/></param>
        /// <param name="shaderRegister">The shader register to bind this parameter to</param>
        /// <param name="registerSpace">The space to bind this parameter in</param>
        /// <param name="visibility">Indicates which shaders have access to this parameter</param>
        /// <returns>A new <see cref="RootParameter"/> representing a directly-bound descriptor</returns>
        public static RootParameter CreateDescriptor(RootParameterType type, uint shaderRegister, uint registerSpace, ShaderVisibility visibility = ShaderVisibility.All)
        {
            Debug.Assert(type is RootParameterType.ConstantBufferView or RootParameterType.ShaderResourceView or RootParameterType.UnorderedAccessView);
            return new RootParameter(type, new D3D12_ROOT_DESCRIPTOR1 { ShaderRegister = shaderRegister, RegisterSpace = registerSpace }, visibility);
        }

        /// <summary>
        /// Creates a new descriptor table root parameter
        /// </summary>
        /// <returns>A new <see cref="RootParameter"/> representing a descriptor table</returns>
        public static RootParameter CreateDescriptorTable(
            DescriptorRangeType type,
            uint baseShaderRegister,
            uint descriptorCount,
            uint registerSpace,
            uint offsetInDescriptorsFromTableStart = DescriptorRangeParameter.AppendAfterLastDescriptor,
            ShaderVisibility visibility = ShaderVisibility.All
        )
            => CreateDescriptorTable(new DescriptorRangeParameter(type, baseShaderRegister, descriptorCount, registerSpace, offsetInDescriptorsFromTableStart), visibility);

        /// <summary>
        /// Creates a new descriptor table root parameter
        /// </summary>
        /// <param name="range">The <see cref="DescriptorRangeParameter"/> to bind</param>
        /// <param name="visibility">Indicates which shaders have access to this parameter</param>
        /// <returns>A new <see cref="RootParameter"/> representing a descriptor table</returns>
        public static RootParameter CreateDescriptorTable(DescriptorRangeParameter range, ShaderVisibility visibility = ShaderVisibility.All)
            => CreateDescriptorTable(new[] { range }, visibility);


        /// <summary>
        /// Creates a new descriptor table root parameter
        /// </summary>
        /// <param name="ranges">The <see cref="DescriptorRangeParameter"/>s to bind</param>
        /// <param name="visibility">Indicates which shaders have access to this parameter</param>
        /// <returns>A new <see cref="RootParameter"/> representing a descriptor table</returns>
        public static RootParameter CreateDescriptorTable(DescriptorRangeParameter[] ranges, ShaderVisibility visibility = ShaderVisibility.All)
        {
            // do NOT make this a not pinned array. RootSignature.TranslateRootParameters relies on it
            var d3d12Ranges = GC.AllocateArray<D3D12_DESCRIPTOR_RANGE1>(ranges.Length, pinned: true);

            for (var i = 0; i < ranges.Length; i++)
            {
                var range = ranges[i];
                d3d12Ranges[i] = new D3D12_DESCRIPTOR_RANGE1(
                    (D3D12_DESCRIPTOR_RANGE_TYPE)range.Type,
                    range.DescriptorCount,
                    range.BaseShaderRegister,
                    range.RegisterSpace,
                    D3D12_DESCRIPTOR_RANGE_FLAGS.D3D12_DESCRIPTOR_RANGE_FLAG_DATA_STATIC,
                    range.DescriptorOffset
                );
            }

            return new RootParameter(d3d12Ranges, visibility);
        }

        /// <summary>
        /// Creates a new constant values root parameter to fit a certain type
        /// </summary>
        /// <typeparam name="T">The type to create a constant root parameter for</typeparam>
        /// <param name="shaderRegister">The shader register to bind this parameter to</param>
        /// <param name="registerSpace">The space to bind this parameter in</param>
        /// <param name="visibility">Indicates which shaders have access to this parameter</param>
        /// <returns>A new <see cref="RootParameter"/> representing a set of constants</returns>
        public static RootParameter CreateConstants<T>(uint shaderRegister, uint registerSpace, ShaderVisibility visibility = ShaderVisibility.All)
            => CreateConstants(Num32BitValues<T>(), shaderRegister, registerSpace, visibility);

        /// <summary>
        /// Creates a new constant values root parameter
        /// </summary>
        /// <param name="num32bitValues">The size, in 32 bit values, of all the constants combined</param>
        /// <param name="shaderRegister">The shader register to bind this parameter to</param>
        /// <param name="registerSpace">The space to bind this parameter in</param>
        /// <param name="visibility">Indicates which shaders have access to this parameter</param>
        /// <returns>A new <see cref="RootParameter"/> representing a set of constants</returns>
        public static RootParameter CreateConstants(uint num32bitValues, uint shaderRegister, uint registerSpace, ShaderVisibility visibility = ShaderVisibility.All)
            => new RootParameter(new D3D12_ROOT_CONSTANTS { Num32BitValues = num32bitValues, ShaderRegister = shaderRegister, RegisterSpace = registerSpace }, visibility);

        private RootParameter(D3D12_DESCRIPTOR_RANGE1[] descriptorTable, ShaderVisibility visibility)
        {
            Type = RootParameterType.DescriptorTable;
            Visibility = visibility;
            DescriptorTable = descriptorTable;
            Descriptor = default;
            Constants = default;
        }

        private RootParameter(RootParameterType type, D3D12_ROOT_DESCRIPTOR1 descriptor, ShaderVisibility visibility)
        {
            Type = type;
            Visibility = visibility;
            DescriptorTable = default;
            Descriptor = descriptor;
            Constants = default;
        }

        private RootParameter(D3D12_ROOT_CONSTANTS constants, ShaderVisibility visibility)
        {
            Type = RootParameterType.DwordConstants;
            Visibility = visibility;
            DescriptorTable = default;
            Descriptor = default;
            Constants = constants;
        }

        internal readonly D3D12_DESCRIPTOR_RANGE1[]? DescriptorTable;

        internal readonly D3D12_ROOT_DESCRIPTOR1 Descriptor;
        internal readonly D3D12_ROOT_CONSTANTS Constants;
    }
}
