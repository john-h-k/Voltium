StructuredBuffer<float4> Colors : register(t0);

float4 main(
    in float4 pos : SV_Position,
    in float2 tex : TEXCOORD
) : SV_Target
{
    float scale = 2.2;
    float2 center = float2(0.7, 0);
    
    float2 seed;
    seed.x = 1.3333 * (tex.x - 0.5) * scale - center.x;
    //seed.x = 1 * (tex.x - 0.5) * scale - center.x;
    seed.y = (tex.y - 0.5) * scale - center.y;

    const int iter = 70;
    int i;
    
    float2 z = seed;
    for (i = 0; i < iter; i++)
    {
        float2 zz = z * z.xy;
        float2 invzz = z * z.yx;
        
        float x = ((zz.x) - (zz.y)) + seed.x;
        float y = ((invzz.x) + (invzz.y)) + seed.y;

        float2 xy = float2(x, y);
        
        if (dot(xy, xy) > 4.0)
        {
            break;
        }
        
        z = xy;
    }

    return Colors[i == iter ? 0 : i];
}

