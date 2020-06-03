#include "Lighting.hlsli"

struct PixelFrag
{
    float4 Position : SV_POSITION;
    float4 WorldPosition : POSITION; // for lighting
    float3 Normal : NORMAL;
};

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
    Material Material;
};

ConstantBuffer<ObjectConstants> Object : register(b0);

ConstantBuffer<FrameConstants> Frame : register(b1);
