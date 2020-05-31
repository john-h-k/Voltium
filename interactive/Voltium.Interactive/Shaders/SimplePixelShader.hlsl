#include "Shaders/PixelFrag.hlsli"

float4 main(PixelFrag frag) : SV_TARGET
{
    return frag.color;
}
