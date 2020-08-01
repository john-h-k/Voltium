struct Vertex
{
    float4 Position : SV_Position;
    float4 Color : COLOR;
};

Vertex VertexMain(float4 position : POSITION, float4 color : COLOR)
{
    Vertex v;
    v.Position = position;
    v.Color = color;
    return v;
}


float4 PixelMain(Vertex v) : SV_Target
{
    return v.Color;
}
