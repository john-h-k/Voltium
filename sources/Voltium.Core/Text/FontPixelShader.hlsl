
struct FontRenderConstants
{
    uint BufferWidth;
    uint BufferHeight;
    bool IsClearType;
};

Buffer<float> Alpha : register(t0);
ConstantBuffer<FontRenderConstants> Constants : register(b0);

float4 main(int2 xy)
{
    return float4(1, 1, 1, Alpha[xy.x + (xy.y * Constants.BufferWidth)]);
}
