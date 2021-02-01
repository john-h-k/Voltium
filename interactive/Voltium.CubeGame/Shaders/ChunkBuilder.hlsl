#include "ChunkTypes.hlsli"

struct Face
{
    BlockVertex Vertices[4];
};

//typedef AppendStructuredBuffer<uint3> IndexBuffer;
//typedef AppendStructuredBuffer<Face> VertexBuffer;
typedef RWByteAddressBuffer IndexBuffer;
typedef RWByteAddressBuffer VertexBuffer;

StructuredBuffer<Block> Blocks : register(s0);
VertexBuffer Vertices[] : register(u0);
IndexBuffer Indices[] : register(u0);


bool IsOpaque(Block block)
{
    return block.BlockId != BLOCK_ID_AIR;
}

void AddFaceIndices(IndexBuffer indices, uint address, uint threadOffset)
{
    indices.Store3(address, uint3(threadOffset + 2, threadOffset + 1, threadOffset));
    indices.Store3(address, uint3(threadOffset + 3, threadOffset + 2, threadOffset));
}

Block GetBlock(uint3 index)
{
    return Blocks[(((index.z * CHUNK_SIZE_Z) + index.y) * CHUNK_SIZE_Y) + index.x];

}

BlockVertex CreateBlockVertex(float positionX, float positionY, float positionZ,
                              float normalX, float normalY, float normalZ,
                              float tangentX, float tangentY, float tangentZ,
                              float texCoordX, float texCoordY
)
{
    BlockVertex vertex;
    vertex.Position = float3(positionX, positionX, positionZ);
    vertex.Normal = float3(normalX, normalY, normalZ);
    vertex.Tangent = float3(tangentX, tangentY, tangentZ);
    vertex.TexCoord = float2(texCoordX, texCoordY);
    return vertex;
}


Face FrontFace =
{
    // Fill in the front face vertex data.
    CreateBlockVertex(-RADIUS, -RADIUS, -RADIUS, 0.0f, 0.0f, -1.0f, 1.0f, 0.0f, 0.0f, 0.0f, 1.0f),
    CreateBlockVertex(-RADIUS, +RADIUS, -RADIUS, 0.0f, 0.0f, -1.0f, 1.0f, 0.0f, 0.0f, 0.0f, 0.0f),
    CreateBlockVertex(+RADIUS, +RADIUS, -RADIUS, 0.0f, 0.0f, -1.0f, 1.0f, 0.0f, 0.0f, 1.0f, 0.0f),
    CreateBlockVertex(+RADIUS, -RADIUS, -RADIUS, 0.0f, 0.0f, -1.0f, 1.0f, 0.0f, 0.0f, 1.0f, 1.0f),
};

    
Face BackFace =
{
    // Fill in the back face vertex data.
    CreateBlockVertex(-RADIUS, -RADIUS, +RADIUS, 0.0f, 0.0f, 1.0f, -1.0f, 0.0f, 0.0f, 1.0f, 1.0f),
    CreateBlockVertex(+RADIUS, -RADIUS, +RADIUS, 0.0f, 0.0f, 1.0f, -1.0f, 0.0f, 0.0f, 0.0f, 1.0f),
    CreateBlockVertex(+RADIUS, +RADIUS, +RADIUS, 0.0f, 0.0f, 1.0f, -1.0f, 0.0f, 0.0f, 0.0f, 0.0f),
    CreateBlockVertex(-RADIUS, +RADIUS, +RADIUS, 0.0f, 0.0f, 1.0f, -1.0f, 0.0f, 0.0f, 1.0f, 0.0f),
};

    
Face LeftFace =
{
    // Fill in the left face vertex data.
    CreateBlockVertex(-RADIUS, -RADIUS, +RADIUS, -1.0f, 0.0f, 0.0f, 0.0f, 0.0f, -1.0f, 0.0f, 1.0f),
    CreateBlockVertex(-RADIUS, +RADIUS, +RADIUS, -1.0f, 0.0f, 0.0f, 0.0f, 0.0f, -1.0f, 0.0f, 0.0f),
    CreateBlockVertex(-RADIUS, +RADIUS, -RADIUS, -1.0f, 0.0f, 0.0f, 0.0f, 0.0f, -1.0f, 1.0f, 0.0f),
    CreateBlockVertex(-RADIUS, -RADIUS, -RADIUS, -1.0f, 0.0f, 0.0f, 0.0f, 0.0f, -1.0f, 1.0f, 1.0f),
};

    
Face RightFace =
{
    // Fill in the right face vertex data.
    CreateBlockVertex(+RADIUS, -RADIUS, -RADIUS, 1.0f, 0.0f, 0.0f, 0.0f, 0.0f, 1.0f, 0.0f, 1.0f),
    CreateBlockVertex(+RADIUS, +RADIUS, -RADIUS, 1.0f, 0.0f, 0.0f, 0.0f, 0.0f, 1.0f, 0.0f, 0.0f),
    CreateBlockVertex(+RADIUS, +RADIUS, +RADIUS, 1.0f, 0.0f, 0.0f, 0.0f, 0.0f, 1.0f, 1.0f, 0.0f),
    CreateBlockVertex(+RADIUS, -RADIUS, +RADIUS, 1.0f, 0.0f, 0.0f, 0.0f, 0.0f, 1.0f, 1.0f, 1.0f)
};

    
Face TopFace =
{
    // Fill in the top face vertex data.
    CreateBlockVertex(-RADIUS, +RADIUS, -RADIUS, 0.0f, 1.0f, 0.0f, 1.0f, 0.0f, 0.0f, 0.0f, 1.0f),
    CreateBlockVertex(-RADIUS, +RADIUS, +RADIUS, 0.0f, 1.0f, 0.0f, 1.0f, 0.0f, 0.0f, 0.0f, 0.0f),
    CreateBlockVertex(+RADIUS, +RADIUS, +RADIUS, 0.0f, 1.0f, 0.0f, 1.0f, 0.0f, 0.0f, 1.0f, 0.0f),
    CreateBlockVertex(+RADIUS, +RADIUS, -RADIUS, 0.0f, 1.0f, 0.0f, 1.0f, 0.0f, 0.0f, 1.0f, 1.0f),
};

    
Face BottomFace =
{
    // Fill in the bottom face vertex data.
    CreateBlockVertex(-RADIUS, -RADIUS, -RADIUS, 0.0f, -1.0f, 0.0f, -1.0f, 0.0f, 0.0f, 1.0f, 1.0f),
    CreateBlockVertex(+RADIUS, -RADIUS, -RADIUS, 0.0f, -1.0f, 0.0f, -1.0f, 0.0f, 0.0f, 0.0f, 1.0f),
    CreateBlockVertex(+RADIUS, -RADIUS, +RADIUS, 0.0f, -1.0f, 0.0f, -1.0f, 0.0f, 0.0f, 0.0f, 0.0f),
    CreateBlockVertex(-RADIUS, -RADIUS, +RADIUS, 0.0f, -1.0f, 0.0f, -1.0f, 0.0f, 0.0f, 1.0f, 0.0f),
};

[NumThreads(CHUNK_SIZE_X, CHUNK_SIZE_Y, CHUNK_SIZE_Z)]
void main(uint threadOffset : SV_GroupIndex, uint3 id : SV_GroupThreadID, uint3 chunk : SV_DispatchThreadID)
{
    uint x = id.x;
    uint y = id.y;
    uint z = id.z;

    VertexBuffer vertices = Vertices[chunk.x];
    
    bool hasLeftFace = x == 0 || !IsOpaque(GetBlock(id + uint3(-1, 0, 0)));
    bool hasRightFace = x == CHUNK_SIZE_X - 1 || !IsOpaque(GetBlock(id + uint3(1, 0, 0)));
    
    bool hasBottomFace = y == 0 || !IsOpaque(GetBlock(id + uint3(0, -1, 0)));
    bool hasTopFace = y == CHUNK_SIZE_Y - 1 || !IsOpaque(GetBlock(id + uint3(0, 1, 0)));
    
    bool hasFrontFace = z == 0 || !IsOpaque(GetBlock(id + uint3(0, 0, -1)));
    bool hasBackFace = z == CHUNK_SIZE_Z - 1 || !IsOpaque(GetBlock(id + uint3(0, 0, 1)));

    if (hasLeftFace)
    {
        vertices.Append(LeftFace);
    }
    if (hasRightFace)
    {
        vertices.Append(RightFace);
    }
    if (hasBottomFace)
    {
        vertices.Append(BottomFace);
    }
    if (hasTopFace)
    {
        vertices.Append(TopFace);
    }
    if (hasFrontFace)
    {
        vertices.Append(FrontFace);
    }
    if (hasBackFace)
    {
        vertices.Append(BackFace);
    }
}
