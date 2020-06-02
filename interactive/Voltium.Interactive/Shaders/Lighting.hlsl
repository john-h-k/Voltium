#define pow2(v) (v * v)
#define pow3(v) (v * v * v)
#define pow4(v) (v * v * v * v)
#define pow5(v) (v * v * v * v * v)

enum LightType
{
    Point,
    Directional,
    Spotlight
};

struct Light
{
    LightType Type;
    float3 Color;

    float FalloffStart;
    float FalloffEnd;

    float3 Direction;
};

struct Material
{
    float4 DiffuseAlbedo;
    float4 ReflectionFactor;
    float Shininess;
};

float LinearFalloff(float depth, float falloffStart , float falloffEnd)
{
    // range between depth and the end of the falloff
    float range = falloffEnd - depth;

    // range off falloff
    float falloffRange = falloffEnd - falloffStart;

    return saturate(range / falloffRange);
}


// Schlick approx of fresnel equations
float3 ReflectedLight(float3 R0, float3 normal, float3 ray)
{
    float rhs = 1 - saturate(dot(normal, ray));

    return R0 + (1 - R0) * pow5(rhs);
}

float3 CalculateLight(
    float3 strength,
    float3 normal,
    float3 ray,
    float3 eye
    Material material
)
{
    float shininess = material.Shininess * 256;
    float3 midpoint = normalize(ray + eye);

    float3 specularReflection = max(dot(midpoint, normal), 0);

    float roughness = ((shininess + 8) * pow(specularReflection, shininess)) / 8;

    float3 specularAlbedo = roughness * ReflectedLight(material.ReflectionFactor, midpoint, ray);

    // if we go HDR, this is unnecessary
    specularAlbedo /= specularAlbedo + 1;

    float3 light = material.DiffuseAlbedo.rgb + specularAlbedo;

    return light * length;
}
