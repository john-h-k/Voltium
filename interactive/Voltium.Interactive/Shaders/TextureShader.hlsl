#define SCENE_LIGHTS 3

struct Lights
{
    DirectionalLight Lights[SCENE_LIGHTS];
};

ConstantBuffer<Lights> SceneLight : register(b2);
Texture2D Texture : register(t0);
SamplerState Sampler : register(s0);

struct PixelFrag
{
    float4 Position : SV_POSITION;
    float4 WorldPosition : POSITION; // for lighting
    float3 Normal : NORMAL;
    float2 TexC : TEXC;
};

struct Vertex
{
    float3 Position : POSITION;
    float3 Normal : NORMAL;
    float3 TexC : TEXC;
};

PixelFrag main(Vertex vertex)
{
    PixelFrag result;

    result.WorldPosition = mul(float4(vertex.Position, 1), Object.World);

    result.Position = mul(result.WorldPosition, Frame.View);
    result.Position = mul(result.Position, Frame.Projection);

    result.Normal = mul(vertex.Normal, (float3x3) Object.World);

    result.TexC = vertex.TexC;

    return result;
}

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
