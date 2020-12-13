
//struct System
//{
//    double3 Location;
//};

//struct Viewport
//{
//    double2 TopLeft;
//    double2 BottomRight;
//};

//struct ShaderOutput
//{
//    uint MaxDensity;
//};

//StructuredBuffer<System> Systems : register(t0);
//ConstantBuffer<Viewport> View : register(b0);

//globallycoherent RWStructuredBuffer<ShaderOutput> Max : register(u0);
//globallycoherent RWTexture2D<uint> Density : register(u1);


//[numthreads(64, 1, 1)]
//void main(uint3 id : SV_DispatchThreadID)
//{
//    System system = Systems[id.x];

//    double2 coords = floor((system.Location.xy - View.TopLeft) / (View.BottomRight - View.TopLeft));
//    InterlockedAdd(Density[coords], 1);
//    InterlockedMax(Max[0].MaxDensity, Density[coords]);
//}

// ================================================================
// AUTO GENERATED
// ================================================================
// This shader was created by ComputeSharp.
// For info or issues: https://github.com/Sergio0694/ComputeSharp.
// ================================================================
// TShader: EDStatistics_Core.SystemsCalculationShader

// Scalar/vector variables
cbuffer _ : register(b0)
{
    uint __x__; // Target X iterations
    uint __y__; // Target Y iterations
    uint __z__; // Target Z iterations
    int coordinatesSize;
    int width;
    int height;
    float2 TopLeft;
    float2 BottomRight;
    int iterations;
}

struct System
{
    float3 Coords;
};

// ReadOnlyBuffer<float> buffer "coordinates"
StructuredBuffer<System> coordinates : register(t0);

// ReadWriteBuffer<int> buffer "density"
RWTexture2D<int> density : register(u0);

// ReadWriteBuffer<int> buffer "maxDensity"
RWStructuredBuffer<int> maxDensity : register(u1);

// Shader body
[Shader("compute")]
[NumThreads(32, 1, 1)]
void CSMain(uint3 id : SV_DispatchThreadId)
{
    if (id.x < __x__ &&
        id.y < __y__ &&
        id.z < __z__) // Automatic bounds check
    {
        int2 dim;

        density.GetDimensions(dim.x, dim.y);
        
        int num = 0;
        for (int i = 0; i < iterations; i++)
        {
            int num2 = id.x * iterations + i;
            if (num2 * 3 >= coordinatesSize)
            {
                return;
            }

            int index = (int) floor((coordinates[num2].Coords.xz - TopLeft) / BottomRight - TopLeft * float2(dim));
            
            if (index < dim && index >= 0)
            {
                InterlockedAdd(density[dim], 1);
                InterlockedMax(maxDensity[0], density[dim]);
            }
        }
    }
}
