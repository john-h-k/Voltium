// Creates an entire screen triangle
// This is used when you want to apply a pixel shader to the entire screen

void main(
    in uint vertID : SV_VertexID,
    out float4 pos : SV_Position,
    out float2 tex : TEXCOORD
)
{
    // Texture coordinates range [0, 2], but only [0, 1] appears on screen.
    tex = float2(uint2(vertID, VertID << 1) & 2);
    pos = float4(lerp(float2(-1, 1), float2(1, -1), tex), 0, 1);
}
