// Creates an entire screen triangle
// This is used when you want to apply a pixel shader to the entire screen

void main(
    in uint VertID : SV_VertexID,
    out float4 Pos : SV_Position,
    out float2 Tex : TEXCOORD
)
{
    // Texture coordinates range [0, 2], but only [0, 1] appears on screen.
    Tex = float2(uint2(VertID, VertID << 1) & 2);
    Pos = float4(lerp(float2(-1, 1), float2(1, -1), Tex), 0, 1);
}
