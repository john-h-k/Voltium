 #include "PixelFrag.hlsli"

struct ObjectConstants
{
    float4x4 World;
};

ConstantBuffer<ObjectConstants> PerObjectConstants : register(b0);

PixelFrag VertexMain(float4 position : POSITION, float4 color : COLOR)
{
    PixelFrag result;

    result.position = mul(position, PerObjectConstants.World);
    result.color = color;

    return result;
}
