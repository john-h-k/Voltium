#include "PixelFrag.hlsli"

float4 PixelMain(PixelFrag frag) : SV_TARGET
{
    return COLOR(frag);
}
