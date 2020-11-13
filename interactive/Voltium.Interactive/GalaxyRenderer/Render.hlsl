struct _Interval
{
    uint Interval;
};

ConstantBuffer<_Interval> Interval : register(b0);
RWTexture2D<uint> Density : register(t0);


float4 main(float4 position : SV_Position) : SV_Target
{
    uint density = Density[position.xy];
    
    const float4 white = (1, 1, 1, 1);
    const float4 blue = (0, 0, 1, 1);
    
    return lerp(white, blue, Interval.Interval * density);
}
