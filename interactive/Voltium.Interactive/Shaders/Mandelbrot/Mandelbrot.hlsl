#include "Complex.hlsli"

StructuredBuffer<float4> Colors : register(t0);

#if DOUBLE
typedef double FP;
typedef double2 FP2;
typedef ComplexD TComplex;
#else
typedef float FP;
typedef float2 FP2;
typedef Complex TComplex;
#endif

struct MandelbrotFactors
{
#if DOUBLE
    uint2 Scale;
    uint2 CenterXY;
    uint2 CenterZW;
#else
    FP Scale;
    FP2 Center;
#endif
    float AspectRatio;
    uint ColorCount;
};

#if FXC
cbuffer _Constants
{
    MandelbrotFactors Constants;
}
#else
ConstantBuffer<MandelbrotFactors> Constants : register(b0);
#endif

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
    double2 centerVal = double2(asdouble(Constants.CenterXY.x, Constants.CenterXY.y), asdouble(Constants.CenterZW.x, Constants.CenterZW.y));
    //double2 center = asdouble(Constants.CenterXY, Constants.CenterZW);
#else
    FP scale = Constants.Scale;
    FP2 centerVal = Constants.Center;
#endif
    TComplex center = Complex_Create(centerVal);
    tex = tex - 0.5;

    tex.y = tex.y / Constants.AspectRatio;

    TComplex seed = Complex_Add(Complex_Create(tex * scale), center);
    int i;

    TComplex z = seed;
    for (i = 0; i < ITER; i++)
    {
        z = Complex_Add(Complex_Mul(z, z), seed);

        if (Complex_MagnitudeSquared(z) > 4.0)
        {
            break;
        }
    }

    i = i == ITER ? 0 : i % Constants.ColorCount;
    return Colors[i];
}

