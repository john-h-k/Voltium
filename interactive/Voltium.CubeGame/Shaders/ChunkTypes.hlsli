struct BlockVertex
{
    float3 Position : POSITION;
    float3 Normal : NORMAL;
    float3 Tangent : TANGENT;
    float2 TexCoord : TEXCOORD;
};

struct Block
{
    uint BlockId;
    uint TextureId;
};


#define BLOCK_ID_AIR 0
#define CHUNK_SIZE_X 8
#define CHUNK_SIZE_Y 8
#define CHUNK_SIZE_Z 8
#define RADIUS 0.5
