#include "PixelFrag.hlsli"

PixelFrag main(float3 position : POSITION, float3 normal : NORMAL, float2 texC : TEXC)
{
    PixelFrag result;

    result.WorldPosition = mul(float4(position, 1), Object.World);

    result.Position = mul(result.WorldPosition, Frame.View);
    result.Position = mul(result.Position, Frame.Projection);

    result.Normal = mul(normal, (float3x3)Object.World);

    result.TexC = texC;

    return result;
}
