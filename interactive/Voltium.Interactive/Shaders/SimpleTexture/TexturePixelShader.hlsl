#include "PixelFrag.hlsli"
#include "../Constants.hlsli"

#define SCENE_LIGHTS 3

struct Lights
{
    DirectionalLight Lights[SCENE_LIGHTS];
};

ConstantBuffer<Lights> SceneLight : register(b2);
Texture2D Texture : register(t0);
#ifdef NORMALS
Texture2D Normals : register(t1);
#endif
SamplerState Sampler : register(s0);

#ifdef NORMALS
float3 NormalToWorld(float3 normal, float3 unitNormal, float3 tangent)
{
    normal = (2 * normal) - 1;

    float3 t = normalize(tangent - (dot(tangent, unitNormal) * unitNormal));
    float3 b = cross(unitNormal, t);

    float3x3 transform = float3x3(t, b, unitNormal);

    return normal * (float1x3) transform;
}
#endif

float4 main(PixelFrag frag) : SV_TARGET
{
    frag.Normal = normalize(frag.Normal);

    float3 eye = normalize(Frame.CameraPosition - (float3) frag.WorldPosition);

    float4 albedo = Object.Material.DiffuseAlbedo * Texture.Sample(Sampler, frag.TexC);
    float4 ambient = Frame.AmbientLight * albedo;
    
#ifdef NORMALS
    float3 normal = (float3) Normals.Sample(Sampler, frag.TexC);
    normal = NormalToWorld(normal, frag.Normal, frag.Tangent);
#else
    float3 normal = frag.Normal;
#endif

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
            normal,
            shadowFactor
        );
    }

    float4 color = ambient + float4(directLight, 0);

    color.a = Object.Material.DiffuseAlbedo.a;

    return color;
}
