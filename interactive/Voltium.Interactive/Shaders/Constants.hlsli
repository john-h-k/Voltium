#include "Lighting.hlsli"

struct FrameConstants
{
    float4x4 View;
    float4x4 Projection;
    float4 AmbientLight;
    float3 CameraPosition;
};

struct ObjectConstants
{
    float4x4 World;
    float4x4 Tex;
    Material Material;
};

ConstantBuffer<ObjectConstants> Object : b0;

ConstantBuffer<FrameConstants> Frame : b1;
