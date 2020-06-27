using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TerraFX.Interop;
using Voltium.Core.Managers;

namespace Voltium.Core
{
    /// <summary>
    /// Represents an individual parameter of a <see cref="RootSignature"/>
    /// </summary>
    public readonly struct RootParameter
    {
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
            return new RootParameter(type, new D3D12_ROOT_DESCRIPTOR { ShaderRegister = shaderRegister, RegisterSpace = registerSpace }, visibility);
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
            uint offsetInDescriptorsFromTableStart = DescriptorRange.AppendAfterLastDescriptor,
            ShaderVisibility visibility = ShaderVisibility.All
        )
            => CreateDescriptorTable(new DescriptorRange(type, baseShaderRegister, descriptorCount, registerSpace, offsetInDescriptorsFromTableStart), visibility);

        /// <summary>
        /// Creates a new descriptor table root parameter
        /// </summary>
        /// <param name="range">The <see cref="DescriptorRange"/> to bind</param>
        /// <param name="visibility">Indicates which shaders have access to this parameter</param>
        /// <returns>A new <see cref="RootParameter"/> representing a descriptor table</returns>
        public static RootParameter CreateDescriptorTable(DescriptorRange range, ShaderVisibility visibility = ShaderVisibility.All)
            => CreateDescriptorTable(new[] { range }, visibility);


        /// <summary>
        /// Creates a new descriptor table root parameter
        /// </summary>
        /// <param name="ranges">The <see cref="DescriptorRange"/>s to bind</param>
        /// <param name="visibility">Indicates which shaders have access to this parameter</param>
        /// <returns>A new <see cref="RootParameter"/> representing a descriptor table</returns>
        public static RootParameter CreateDescriptorTable(DescriptorRange[] ranges, ShaderVisibility visibility = ShaderVisibility.All)
        {
            // do NOT make this a not pinned array. RootSignature.TranslateRootParameters relies on it
            var d3d12Ranges = GC.AllocateArray<D3D12_DESCRIPTOR_RANGE>(ranges.Length, pinned: true);

            for (var i = 0; i < ranges.Length; i++)
            {
                var range = ranges[i];
                d3d12Ranges[i] = new D3D12_DESCRIPTOR_RANGE(
                    (D3D12_DESCRIPTOR_RANGE_TYPE)range.Type,
                    range.DescriptorCount,
                    range.BaseShaderRegister,
                    range.RegisterSpace,
                    range.DescriptorOffset
                );
            }

            return new RootParameter(d3d12Ranges, visibility);
        }

        /// <summary>
        /// Creates a new constant values root parameter
        /// </summary>
        /// <param name="sizeOfConstants">The size, in bytes, of all the constants combined</param>
        /// <param name="shaderRegister">The shader register to bind this parameter to</param>
        /// <param name="registerSpace">The space to bind this parameter in</param>
        /// <param name="visibility">Indicates which shaders have access to this parameter</param>
        /// <returns>A new <see cref="RootParameter"/> representing a set of constants</returns>
        public static RootParameter CreateConstants(uint sizeOfConstants, uint shaderRegister, uint registerSpace, ShaderVisibility visibility = ShaderVisibility.All)
            => new RootParameter(new D3D12_ROOT_CONSTANTS { Num32BitValues = sizeOfConstants / 4, ShaderRegister = shaderRegister, RegisterSpace = registerSpace }, visibility);

        private RootParameter(D3D12_DESCRIPTOR_RANGE[] descriptorTable, ShaderVisibility visibility)
        {
            Type = RootParameterType.DescriptorTable;
            Visibility = visibility;
            DescriptorTable = descriptorTable;
            Descriptor = default;
            Constants = default;
        }
        private RootParameter(RootParameterType type, D3D12_ROOT_DESCRIPTOR descriptor, ShaderVisibility visibility)
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

        internal readonly D3D12_DESCRIPTOR_RANGE[]? DescriptorTable;

        internal readonly D3D12_ROOT_DESCRIPTOR Descriptor;
        internal readonly D3D12_ROOT_CONSTANTS Constants;
    }
}
