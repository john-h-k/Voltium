#include "ChunkTypes.hlsli"


#define BufferSpace space0
#define TextureSpace space1

#define _RootSig "RootFlags(DENY_HULL_SHADER_ROOT_ACCESS | DENY_DOMAIN_SHADER_ROOT_ACCESS | DENY_GEOMETRY_SHADER_ROOT_ACCESS | DENY_MESH_SHADER_ROOT_ACCESS | DENY_AMPLIFICATION_SHADER_ROOT_ACCESS),"\
                 "CBV(b0),"\
                 "SRV(t0),"\
                 "DescriptorTable(SRV(t0, space=1), visibility=SHADER_VISIBILITY_PIXEL),"\
                 "StaticSampler(s0, filter=FILTER_MIN_MAG_MIP_POINT, visibility=SHADER_VISIBILITY_PIXEL)"

struct VertexOut
{
    float4 Position : SV_POSITION;
    float2 TexC : TEXC;
    nointerpolation uint TexIndex : TEXINDEX;
    //float3 Normal : NORMAL;
    //float3 Tangent : TANGENT;
};

struct FrameConstants
{
    float4 CameraPosition;
    float4x4 View;
    float4x4 Projection;
};


struct CubeInfo
{
    float4 Position;
    uint TexIndex[6];
};

ConstantBuffer<FrameConstants> Frame : register(b0);
StructuredBuffer<CubeInfo> Cubes : register(t0, BufferSpace);

[RootSignature(_RootSig)]
VertexOut VertexMain(in uint index : SV_VertexID)
{
    uint3 xyz = uint3(index & 1, (index & 4) >> 2, (index & 2) >> 1);

    CubeInfo instance = Cubes[index >> 3];
    float3 localPosition = Frame.CameraPosition.xyz - instance.Position.xyz;

    VertexOut result;
    
    if (localPosition.x > 0)
    {
        xyz.x = 1 - xyz.x;
    }
    if (localPosition.y > 0)
    {
        xyz.y = 1 - xyz.y;
    }
    if (localPosition.z > 0)
    {
        xyz.z = 1 - xyz.z;
    }

    float3 tex = float3(xyz);
    float3 pos = tex * 2.0 - 1.0;

    result.Position = pos;
    result.TexC = tex.xy;
    result.TexIndex = instance.TexIndex;
    
    return result;
}


SamplerState DefaultSampler : register(s0);
Texture2DArray Textures : register(t0, TextureSpace);

[RootSignature(_RootSig)]
float4 PixelMain(VertexOut fragment) : SV_Target
{
    float4 color = Textures.Sample(DefaultSampler, float3(fragment.TexC, fragment.TexIndex));
    return color;
}
