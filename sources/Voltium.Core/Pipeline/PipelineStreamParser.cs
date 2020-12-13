using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TerraFX.Interop;
using Voltium.Common;
using static TerraFX.Interop.D3D12_PIPELINE_STATE_SUBOBJECT_TYPE;

namespace Voltium.Core.Pipeline
{
    internal static unsafe class PipelineStreamParser
    {
        public static void DebugStream(D3D12_PIPELINE_STATE_STREAM_DESC* pDesc)
        {
            nint bytesParsed = 0;
            byte* pStart = (byte*)pDesc->pPipelineStateSubobjectStream;

            while (bytesParsed < (nint)pDesc->SizeInBytes)
            {
                var element = *(D3D12_PIPELINE_STATE_SUBOBJECT_TYPE*)&pStart[bytesParsed];
                bytesParsed += sizeof(D3D12_PIPELINE_STATE_SUBOBJECT_TYPE);

                Console.WriteLine($"Element {element}");

                bytesParsed += MathHelpers.AlignUp(GetSubobjectSize(element), sizeof(void*));
            }
        }

        public static int GetSubobjectSize(D3D12_PIPELINE_STATE_SUBOBJECT_TYPE type)
        {
            return type switch
            {
                D3D12_PIPELINE_STATE_SUBOBJECT_TYPE_ROOT_SIGNATURE => sizeof(ID3D12RootSignature*),
                D3D12_PIPELINE_STATE_SUBOBJECT_TYPE_STREAM_OUTPUT => sizeof(D3D12_STREAM_OUTPUT_DESC),
                D3D12_PIPELINE_STATE_SUBOBJECT_TYPE_BLEND => sizeof(D3D12_BLEND_DESC),
                D3D12_PIPELINE_STATE_SUBOBJECT_TYPE_SAMPLE_MASK => sizeof(uint),
                D3D12_PIPELINE_STATE_SUBOBJECT_TYPE_RASTERIZER => sizeof(D3D12_RASTERIZER_DESC),
                D3D12_PIPELINE_STATE_SUBOBJECT_TYPE_DEPTH_STENCIL => sizeof(D3D12_DEPTH_STENCIL_DESC),
                D3D12_PIPELINE_STATE_SUBOBJECT_TYPE_INPUT_LAYOUT => sizeof(D3D12_INPUT_LAYOUT_DESC),
                D3D12_PIPELINE_STATE_SUBOBJECT_TYPE_IB_STRIP_CUT_VALUE => sizeof(D3D12_INDEX_BUFFER_STRIP_CUT_VALUE),
                D3D12_PIPELINE_STATE_SUBOBJECT_TYPE_PRIMITIVE_TOPOLOGY => sizeof(D3D12_PRIMITIVE_TOPOLOGY_TYPE),
                D3D12_PIPELINE_STATE_SUBOBJECT_TYPE_RENDER_TARGET_FORMATS => sizeof(D3D12_RT_FORMAT_ARRAY),
                D3D12_PIPELINE_STATE_SUBOBJECT_TYPE_DEPTH_STENCIL_FORMAT => sizeof(DXGI_FORMAT),
                D3D12_PIPELINE_STATE_SUBOBJECT_TYPE_SAMPLE_DESC => sizeof(DXGI_SAMPLE_DESC),
                D3D12_PIPELINE_STATE_SUBOBJECT_TYPE_NODE_MASK => sizeof(D3D12_NODE_MASK),
                D3D12_PIPELINE_STATE_SUBOBJECT_TYPE_CACHED_PSO => sizeof(D3D12_CACHED_PIPELINE_STATE),
                D3D12_PIPELINE_STATE_SUBOBJECT_TYPE_FLAGS => sizeof(D3D12_PIPELINE_STATE_FLAGS),
                D3D12_PIPELINE_STATE_SUBOBJECT_TYPE_DEPTH_STENCIL1 => sizeof(D3D12_DEPTH_STENCIL_DESC1),
                D3D12_PIPELINE_STATE_SUBOBJECT_TYPE_VIEW_INSTANCING => sizeof(D3D12_VIEW_INSTANCING_DESC),
                D3D12_PIPELINE_STATE_SUBOBJECT_TYPE_VS => sizeof(D3D12_SHADER_BYTECODE),
                D3D12_PIPELINE_STATE_SUBOBJECT_TYPE_PS => sizeof(D3D12_SHADER_BYTECODE),
                D3D12_PIPELINE_STATE_SUBOBJECT_TYPE_DS => sizeof(D3D12_SHADER_BYTECODE),
                D3D12_PIPELINE_STATE_SUBOBJECT_TYPE_HS => sizeof(D3D12_SHADER_BYTECODE),
                D3D12_PIPELINE_STATE_SUBOBJECT_TYPE_GS => sizeof(D3D12_SHADER_BYTECODE),
                D3D12_PIPELINE_STATE_SUBOBJECT_TYPE_CS => sizeof(D3D12_SHADER_BYTECODE),
                D3D12_PIPELINE_STATE_SUBOBJECT_TYPE_AS => sizeof(D3D12_SHADER_BYTECODE),
                D3D12_PIPELINE_STATE_SUBOBJECT_TYPE_MS => sizeof(D3D12_SHADER_BYTECODE),
                _ => throw new InvalidDataException("Didn't find a valid subobject type")
            };
        }
    }
}
