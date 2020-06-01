#include "PixelFrag.hlsli"

float4 main(PixelFrag frag) : SV_TARGET
{
    return COLOR(frag);
}
