using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using TerraFX.Interop;
using Voltium.Core.Devices;
using TerraFX.Interop.DirectX;
using static TerraFX.Interop.Windows.Windows;
using static TerraFX.Interop.DirectX.D3D12;

namespace Voltium.Interactive
{
    public unsafe class CommandListMethodBenchmark
    {
        private ID3D12Device* pDevice;
        private ID3D12CommandAllocator* pDispatchAllocator;
        private ID3D12PipelineState* pCompute;
        private ID3D12GraphicsCommandList* pDrawInstanced, pDrawIndexedInstanced, pDispatch;

        private const string EmptyVertexShader = @"
        float4 main(uint id : SV_VertexID) : SV_Position { return float4(0, 0, 0, 0); }
";

        private const string EmptyComputeShader = @"
        [numthreads(64, 1, 1)] void main(uint id : SV_DispatchThreadID) { }
";

        [GlobalSetup]
        public void Setup()
        {
#if DEBUG
            ID3D12Debug* pDebug = null;
            DirectX.D3D12GetDebugInterface(__uuidof(pDebug), (void**)&pDebug);
            pDebug->EnableDebugLayer();
#endif

            fixed (ID3D12Device** ppDevice = &pDevice)
            {
                DirectX.D3D12CreateDevice(null, D3D_FEATURE_LEVEL.D3D_FEATURE_LEVEL_11_0, __uuidof(pDevice), (void**)ppDevice);
            }

            var desc = new D3D12_ROOT_SIGNATURE_DESC();

            ID3DBlob* pBlob;
            ID3DBlob* pError;
            if (FAILED(DirectX.D3D12SerializeRootSignature(&desc, D3D_ROOT_SIGNATURE_VERSION.D3D_ROOT_SIGNATURE_VERSION_1_0, &pBlob, &pError)))
            {
                Console.WriteLine(new string((sbyte*)pError->GetBufferPointer(), 0, (int)pError->GetBufferSize()));
            }

            ID3D12RootSignature* pRootSig = null;
            pDevice->CreateRootSignature(0, pBlob->GetBufferPointer(), pBlob->GetBufferSize(), __uuidof(pRootSig), (void**)&pRootSig);

            var cshader = ShaderManager.CompileShader(nameof(EmptyComputeShader), EmptyComputeShader, ShaderType.Compute);

            var cpsoDesc = new D3D12_COMPUTE_PIPELINE_STATE_DESC
            {
                pRootSignature = pRootSig,
                CS = new D3D12_SHADER_BYTECODE { BytecodeLength = cshader.Length, pShaderBytecode = cshader.Pointer }
            };

            fixed (ID3D12PipelineState** ppCompute = &pCompute)  
            {
                pDevice->CreateComputePipelineState(&cpsoDesc, __uuidof(*ppCompute), (void**)&ppCompute);
            }

            fixed (ID3D12GraphicsCommandList** ppDrawInstanced = &pDrawInstanced)
            fixed (ID3D12GraphicsCommandList** ppDrawIndexedInstanced = &pDrawIndexedInstanced)
            fixed (ID3D12GraphicsCommandList** ppDispatch = &pDispatch)
            fixed (ID3D12CommandAllocator** ppDispatchAllocator = &pDispatchAllocator)
            {
                pDevice->CreateCommandAllocator(D3D12_COMMAND_LIST_TYPE.D3D12_COMMAND_LIST_TYPE_DIRECT, __uuidof(*ppDispatchAllocator), (void**)ppDispatchAllocator);
                pDevice->CreateCommandList(0, D3D12_COMMAND_LIST_TYPE.D3D12_COMMAND_LIST_TYPE_DIRECT, pDispatchAllocator, pCompute, __uuidof(*ppDispatch), (void**)ppDispatch);
            }
        }

        [IterationCleanup]
        public void Cleanup()
        {
            pDispatch->Close();
            pDispatchAllocator->Reset();
            pDispatch->Reset(pDispatchAllocator, pCompute);
        }    

        [Benchmark(OperationsPerInvoke = 256 * 16)]
        public void Dispatch()
        {
            pDispatch->Dispatch(69, 420, 1);
        }

        //[Benchmark]
        public void DrawInstanced()
        {
            pDrawInstanced->DrawInstanced(1, 10, 5, 7);
        }

        //[Benchmark]
        public void DrawIndexedInstanced()
        {
            pDrawIndexedInstanced->DrawIndexedInstanced(1, 10, 5, 10, 9);
        }
    }
}
