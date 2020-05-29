using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using TerraFX.Interop;
#pragma warning disable 1591

namespace Voltium.Core.d3dx12
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public static unsafe class CD3DX12_ROOT_PARAMETER
    {
        public static D3D12_ROOT_PARAMETER Create(in D3D12_ROOT_PARAMETER o)
        {
            return o;
        }

        public static void InitAsDescriptorTable(
            out D3D12_ROOT_PARAMETER rootParam,
            uint numDescriptorRanges,
            [In] D3D12_DESCRIPTOR_RANGE* pDescriptorRanges,
            D3D12_SHADER_VISIBILITY visibility = D3D12_SHADER_VISIBILITY.D3D12_SHADER_VISIBILITY_ALL)
        {
            rootParam = default;
            rootParam.ParameterType = D3D12_ROOT_PARAMETER_TYPE.D3D12_ROOT_PARAMETER_TYPE_DESCRIPTOR_TABLE;
            rootParam.ShaderVisibility = visibility;
            CD3DX12_ROOT_DESCRIPTOR_TABLE.Init(out rootParam.Anonymous.DescriptorTable, numDescriptorRanges, pDescriptorRanges);
        }

        public static void InitAsConstants(
            out D3D12_ROOT_PARAMETER rootParam,
            uint num32BitValues,
            uint shaderRegister,
            uint registerSpace = 0,
            D3D12_SHADER_VISIBILITY visibility = D3D12_SHADER_VISIBILITY.D3D12_SHADER_VISIBILITY_ALL)
        {
            rootParam = default;
            rootParam.ParameterType = D3D12_ROOT_PARAMETER_TYPE.D3D12_ROOT_PARAMETER_TYPE_32BIT_CONSTANTS;
            rootParam.ShaderVisibility = visibility;
            CD3DX12_ROOT_CONSTANTS.Init(out rootParam.Anonymous.Constants, num32BitValues, shaderRegister, registerSpace);
        }

        public static void InitAsConstantBufferView(
            out D3D12_ROOT_PARAMETER rootParam,
            uint shaderRegister,
            uint registerSpace = 0,
            D3D12_SHADER_VISIBILITY visibility = D3D12_SHADER_VISIBILITY.D3D12_SHADER_VISIBILITY_ALL)
        {
            rootParam = default;
            rootParam.ParameterType = D3D12_ROOT_PARAMETER_TYPE.D3D12_ROOT_PARAMETER_TYPE_CBV;
            rootParam.ShaderVisibility = visibility;
            CD3DX12_ROOT_DESCRIPTOR.Init(out rootParam.Anonymous.Descriptor, shaderRegister, registerSpace);
        }

        public static void InitAsShaderResourceView(
            out D3D12_ROOT_PARAMETER rootParam,
            uint shaderRegister,
            uint registerSpace = 0,
            D3D12_SHADER_VISIBILITY visibility = D3D12_SHADER_VISIBILITY.D3D12_SHADER_VISIBILITY_ALL)
        {
            rootParam = default;
            rootParam.ParameterType = D3D12_ROOT_PARAMETER_TYPE.D3D12_ROOT_PARAMETER_TYPE_SRV;
            rootParam.ShaderVisibility = visibility;
            CD3DX12_ROOT_DESCRIPTOR.Init(out rootParam.Anonymous.Descriptor, shaderRegister, registerSpace);
        }

        public static void InitAsUnorderedAccessView(
            out D3D12_ROOT_PARAMETER rootParam,
            uint shaderRegister,
            uint registerSpace = 0,
            D3D12_SHADER_VISIBILITY visibility = D3D12_SHADER_VISIBILITY.D3D12_SHADER_VISIBILITY_ALL)
        {
            rootParam = default;
            rootParam.ParameterType = D3D12_ROOT_PARAMETER_TYPE.D3D12_ROOT_PARAMETER_TYPE_UAV;
            rootParam.ShaderVisibility = visibility;
            CD3DX12_ROOT_DESCRIPTOR.Init(out rootParam.Anonymous.Descriptor, shaderRegister, registerSpace);
        }
    }
}
