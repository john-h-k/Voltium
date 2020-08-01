#include "PixelFrag.hlsli"
#include "../Constants.hlsli"

PixelFrag main(
    float3 position : POSITION,
    float3 normal : NORMAL,
    //float3 tangent : TANGENT,
    float2 texC : TEXCOORD
)
{
    PixelFrag result;

    result.WorldPosition = mul(float4(position, 1), Object.World);

    result.Position = mul(result.WorldPosition, Frame.View);
    result.Position = mul(result.Position, Frame.Projection);

    result.Normal = mul(normal, (float3x3) Object.World);

    //result.Tangent = mul(tangent, (float3x3) Object.World);

    result.TexC = mul(float4(texC, 0, 1), Object.Tex).xy;

    return result;
}
