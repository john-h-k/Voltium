struct VertexIn
{
    float3 Position : POSITION;
    float3 Normal : NORMAL;
    float3 Tangent : TANGENT;
    float2 TexCoord : TEXCOORD;
};

struct VertexOut
{
    float4 Position : SV_POSITION;
    //float4 WorldPosition : POSITION; // for lighting
    float3 Normal : NORMAL;
    float3 Tangent : TANGENT;
    float2 TexC : TEXC;
};

struct FrameConstants
{
    float4x4 View;
    float4x4 Projection;
};

struct ObjectConstants
{
    float4x4 World;
    float4x4 TexTransform;
    int TextureIndex;
};

ConstantBuffer<FrameConstants> Frame : register(b0);
ConstantBuffer<ObjectConstants> Object : register(b1);

VertexOut VertexMain(in VertexIn vertexIn)
{
    VertexOut vertexOut;

    //vertexOut.WorldPosition = mul(float4(vertexIn.Position, 1), Object.World);
    
    vertexOut.Position = mul(float4(vertexIn.Position, 1), Object.World);
    vertexOut.Position = mul(vertexOut.Position, Frame.View);
    vertexOut.Position = mul(vertexOut.Position, Frame.Projection);

    vertexOut.Normal = mul(vertexIn.Normal, (float3x3) Object.World);

    vertexOut.Tangent = mul(vertexIn.Tangent, (float3x3) Object.World);

    vertexOut.TexC = mul(float4(vertexIn.TexCoord, 0, 1), Object.TexTransform).xy;

    return vertexOut;
}


SamplerState DefaultSampler : register(s0);
Texture2D Textures[] : register(t0);


float4 PixelMain(VertexOut fragment) : SV_Target
{
    
    return Textures[Object.TextureIndex].Sample(DefaultSampler, fragment.TexC);
}
