Texture2D Color : register(t0);

SamplerState Ssaa : register(s0);

struct SsaaConstants
{
    uint Factor;
};

ConstantBuffer<SsaaConstants> Constants : register(b0);

float4 main(
    in float4 pos : SV_Position,
    in float2 tex : TEXCOORD
) : SV_Target
{
    return Color.Sample(Ssaa, tex / Constants.Factor);
}
