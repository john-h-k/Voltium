#include "PixelFrag.hlsli"

struct ObjectConstants
{
    float4x4 World;
    float4x4 View;
    float4x4 Projection;
};

ConstantBuffer<ObjectConstants> PerObjectConstants : register(b0);

PixelFrag main(float3 position : POSITION, float4 color : COLOR)
{
    PixelFrag result;

    result.position = float4(position, 1.0f);
    result.position = mul(result.position, PerObjectConstants.World);
    result.position = mul(result.position, PerObjectConstants.View);
    result.position = mul(result.position, PerObjectConstants.Projection);
    result.color = color;

    return result;
}
