StructuredBuffer<float4> Colors : register(t0);

#if DOUBLE
typedef double FP;
typedef double2 FP2;
#else
typedef float FP;
typedef float2 FP2;
#endif

struct MandelbrotFactors
{
#if DOUBLE
    uint2 Scale;
    uint2 CenterXY;
    uint2 CenterZW;
#else
    float Scale;
    float2 Center;
#endif
    float AspectRatio;
    uint ColorCount;
};

ConstantBuffer<MandelbrotFactors> Constants : register(b0);

FP lengthsquared(FP2 val)
{
    return (val.x * val.x) + (val.y * val.y);
}

float4 main(
    in float4 pos : SV_Position,
    in float2 tex : TEXCOORD
) : SV_Target
{
#if DOUBLE
    double scale = asdouble(Constants.Scale.x, Constants.Scale.y);
    double2 center = double2(asdouble(Constants.CenterXY.x, Constants.CenterXY.y), asdouble(Constants.CenterZW.x, Constants.CenterZW.y));
    //double2 center = asdouble(Constants.CenterXY, Constants.CenterZW);
#else
    FP scale = Constants.Scale;
    FP2 center = Constants.Center;
#endif
    tex = tex - 0.5;

    tex.y = tex.y / Constants.AspectRatio;

    FP2 seed = tex * scale + center;

    const int iter = 256 * 4 * 10;
    int i;

    FP2 z = seed;
    for (i = 0; i < iter; i++)
    {
        FP2 zz = z * z.xy;
        FP2 invzz = z * z.yx;

        FP x = ((zz.x) - (zz.y)) + seed.x;
        FP y = ((invzz.x) + (invzz.y)) + seed.y;

        FP2 xy = FP2(x, y);

        if (lengthsquared(xy) > 4.0)
        {
            break;
        }

        z = xy;
    }

    i = i == iter ? 0 : i % Constants.ColorCount;
    return Colors[i];
}

