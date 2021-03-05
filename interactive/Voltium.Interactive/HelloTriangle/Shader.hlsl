struct VertexOut
{
    float4 Position : SV_Position;
    float4 Color : COLOR;
};

VertexOut VertexMain(float3 position : POSITION, float4 color : COLOR)
{
    VertexOut v;
    v.Position = float4(position, 1);
    v.Color = color;
    return v;
}


float4 PixelMain(VertexOut v) : SV_Target
{
    return v.Color;
}
