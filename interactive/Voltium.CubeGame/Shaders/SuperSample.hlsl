
SamplerState Sampler : register(s0);

Texture2D Source : register(t0);
RWTexture2D Destination : register(t1);

[numthread(8, 8, 1)]
void _2X(int2 xy : SV_DispatchThreadID)
{
}

