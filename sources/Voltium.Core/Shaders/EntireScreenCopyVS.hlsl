// Creates an entire screen triangle
// This is used when you want to apply a pixel shader to the entire screen

void main(
    in uint vertexID : SV_VertexID,
    out float4 position : SV_Position,
)
{
    float uv = vec2((vertexID << 1) & 2, vertexID & 2);
    return float4(outUV * 2.0f - 1.0f, 0.0f, 1.0f);
}
