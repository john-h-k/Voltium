#include "PixelFrag.hlsli"
#include "../Constants.hlsli"

struct Vertex
{
    float3 Position : POSITION;
    float3 Normal : NORMAL;
    float4 Color : COLOR;
};


PixelFrag main(Vertex vertex)
{
    PixelFrag result;

    result.WorldPosition = mul(float4(vertex.Position, 1), Object.World);

    result.Position = mul(result.WorldPosition, Frame.View);
    result.Position = mul(result.Position, Frame.Projection);

    result.Normal = mul(vertex.Normal, (float3x3) Object.World);

    result.Color = vertex.Color;

    return result;
}
