#include "PixelFrag.hlsli"
#include "../Constants.hlsli"

#define SCENE_LIGHTS 3

struct Lights
{
    DirectionalLight Lights[SCENE_LIGHTS];
};

ConstantBuffer<Lights> SceneLight : register(b2);
Texture2D Texture : register(t0);
SamplerState Sampler : register(s0);

float4 main(PixelFrag frag) : SV_TARGET
{
    frag.Normal = normalize(frag.Normal);

    float3 eye = normalize(Frame.CameraPosition - (float3) frag.WorldPosition);

    float4 albedo = Object.Material.DiffuseAlbedo * Texture.Sample(Sampler, frag.TexC);
    float4 ambient = Frame.AmbientLight * albedo;

    float3 shadowFactor = 1.0f;

    float3 directLight = 0;

    Material mat;
    mat.DiffuseAlbedo = albedo;
    mat.ReflectionFactor = Object.Material.ReflectionFactor;
    mat.Shininess = Object.Material.Shininess;

    for (int i = 0; i < SCENE_LIGHTS; i++)
    {
        directLight += ComputeDirectionalLight(
            SceneLight.Lights[i],
            mat,
            eye,
            frag.Normal,
            shadowFactor
        );
    }

    float4 color = ambient + float4(directLight, 0);

    color.a = Object.Material.DiffuseAlbedo.a;

    return color;
}
