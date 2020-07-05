#define pow2(v) (v * v)
#define pow3(v) (v * v * v)
#define pow4(v) (v * v * v * v)
#define pow5(v) (v * v * v * v * v)

struct DirectionalLight
{
    float3 Strength;
    float3 Direction;
};

struct Material
{
    float4 DiffuseAlbedo;
    float3 ReflectionFactor;
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
    float3 eye,
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

    return light * strength;
}

float3 ComputeDirectionalLight(DirectionalLight light, Material material, float3 eye, float3 normal, float3 shadowFactor)
{
    float3 ray = -light.Direction;

    float strengthFactor = max(dot(normal, ray), 0.0f);
    float3 strength = light.Strength * strengthFactor;

    return shadowFactor * CalculateLight(strength, ray, normal, eye, material);
}
