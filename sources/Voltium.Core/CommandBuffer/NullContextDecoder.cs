using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Voltium.Core.CommandBuffer
{
    internal unsafe sealed class NullContextDecoder
    {

        public void Encode(GpuContext context)
        {
            var buff = context.CommandBuffer;

            fixed (byte* pBuff = &buff[0])
            {
                byte* pPacketStart = pBuff;
                byte* pPacketEnd = pPacketStart + buff.Length;

                while (pPacketStart < pPacketEnd)
                {
                    var cmd = (Command*)pPacketStart;
                    switch (cmd->Type)
                    {
                        case CommandType.InsertMarker:
                            break;
                        case CommandType.BeginEvent:
                            break;
                        case CommandType.EndEvent:
                            break;
                        case CommandType.Transition:
                            break;
                        case CommandType.WriteBarrier:
                            break;
                        case CommandType.AliasingBarrier:
                            break;
                        case CommandType.SetPipeline:
                            break;
                        case CommandType.SetIndexBuffer:
                            break;
                        case CommandType.SetVertexBuffer:
                            break;
                        case CommandType.SetViewports:
                            break;
                        case CommandType.SetScissorRectangles:
                            break;
                        case CommandType.SetShadingRate:
                            break;
                        case CommandType.SetShadingRateImage:
                            break;
                        case CommandType.SetTopology:
                            break;
                        case CommandType.SetStencilRef:
                            break;
                        case CommandType.SetBlendFactor:
                            break;
                        case CommandType.SetDepthBounds:
                            break;
                        case CommandType.SetSamplePositions:
                            break;
                        case CommandType.SetViewInstanceMask:
                            break;
                        case CommandType.BindVirtualAddress:
                            break;
                        case CommandType.BindDescriptors:
                            break;
                        case CommandType.Bind32BitConstants:
                            break;
                        case CommandType.BeginRenderPass:
                            break;
                        case CommandType.EndRenderPass:
                            break;
                        case CommandType.ReadTimestamp:
                            break;
                        case CommandType.BeginQuery:
                            break;
                        case CommandType.EndQuery:
                            break;
                        case CommandType.ResolveQuery:
                            break;
                        case CommandType.BeginConditionalRendering:
                            break;
                        case CommandType.EndConditionalRendering:
                            break;
                        case CommandType.BufferCopy:
                            break;
                        case CommandType.TextureCopy:
                            break;
                        case CommandType.BufferToTextureCopy:
                            break;
                        case CommandType.TextureToBufferCopy:
                            break;
                        case CommandType.WriteConstants:
                            break;
                        case CommandType.ClearBuffer:
                            break;
                        case CommandType.ClearBufferInteger:
                            break;
                        case CommandType.ClearTexture:
                            break;
                        case CommandType.ClearTextureInteger:
                            break;
                        case CommandType.ClearDepthStencil:
                            break;
                        case CommandType.BuildAccelerationStructure:
                            break;
                        case CommandType.CopyAccelerationStructure:
                            break;
                        case CommandType.CompactAccelerationStructure:
                            break;
                        case CommandType.SerializeAccelerationStructure:
                            break;
                        case CommandType.DeserializeAccelerationStructure:
                            break;
                        case CommandType.ExecuteIndirect:
                            break;
                        case CommandType.Draw:
                            break;
                        case CommandType.DrawIndexed:
                            break;
                        case CommandType.Dispatch:
                            break;
                        case CommandType.RayTrace:
                            break;
                        case CommandType.MeshDispatch:
                            break;
                    }

                    void AdvanceCommand<T>(T* pVal)
                        where T : unmanaged => pPacketStart = &cmd->Arguments + sizeof(T);

                    void AdvanceVariableCommand<T, TVariable>(T* pVal, TVariable* pVariable, uint pVariableCount)
                        where T : unmanaged where TVariable : unmanaged => pPacketStart = &cmd->Arguments + sizeof(T) + (sizeof(TVariable) * pVariableCount);

                    void AdvanceVariableCommand2<T, TVariable1, TVariable2>(T* pVal, TVariable1* pVariable1, TVariable2* pVariable2, uint pVariableCount)
                        where T : unmanaged where TVariable1 : unmanaged where TVariable2 : unmanaged => pPacketStart = &cmd->Arguments + sizeof(T) + (sizeof(TVariable1) * pVariableCount) + (sizeof(TVariable2) * pVariableCount);

                    void AdvanceEmptyCommand() => pPacketStart = &cmd->Arguments;
                }
            }
        }
    }
}
