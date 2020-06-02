// The standard dynamic range present method

Texture2D<float3> SceneColor : register(t0);

float3 main(float4 position : SV_Position) : SV_Target0
{
    return SceneColor[(int2)position.xy];
}
