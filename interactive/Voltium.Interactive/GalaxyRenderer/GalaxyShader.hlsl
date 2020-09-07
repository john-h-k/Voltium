
struct System
{
    uint IdLo;
    uint IdHi;
    double X;
    double Y;
    double Z;
};

struct Viewport
{
    int X;
    int Y;
    int Width;
    int Height;
};

StructuredBuffer<System> Systems : register(t0);
ConstantBuffer<Viewport> View : register(b0);

RWTexture2D<uint> Density : register(t1);

[numthreads(64, 1, 1)]
void main(uint3 id : SV_DispatchThreadID)
{
    System system = Systems[id.x];

    Density[system.X ]

    GroupMemoryBarrierWithGroupSync();

    if ()
}
