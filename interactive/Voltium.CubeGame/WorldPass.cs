using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualBasic;
using Voltium.Core;
using Voltium.Core.Devices;
using Voltium.Core.Devices.Shaders;
using Voltium.Core.Memory;
using Voltium.Core.Pipeline;
using Voltium.ModelLoading;
using Voltium.RenderEngine;
using Voltium.RenderEngine.EntityComponentSystem;
using Voltium.TextureLoading;
using Buffer = Voltium.Core.Memory.Buffer;

namespace Voltium.CubeGame
{
    internal struct Block
    {
        public uint TextureId;
    }


    [ShaderInput]
    internal partial struct BlockVertex
    {
        public Vector3 Position;
        public Vector3 Normal;
        public Vector3 Tangent;
        public Vector2 TexCoord;

        public BlockVertex(
            float positionX, float positionY, float positionZ,
            float normalX, float normalY, float normalZ,
            float tangentX, float tangentY, float tangentZ,
            float texCoordX, float texCoordY
        ) :
            this(
            new Vector3(positionX, positionY, positionZ),
            new Vector3(normalX, normalY, normalZ),
            new Vector3(tangentX, tangentY, tangentZ),
            new Vector2(texCoordX, texCoordY)
            )
        {
        }

        public BlockVertex(Vector3 position, Vector3 normal, Vector3 tangent, Vector2 texCoord)
        {
            Position = position;
            Normal = normal;
            Tangent = tangent;
            TexCoord = texCoord;
        }
    }

    internal struct RenderResources
    {
        public TextureHandle SceneColor;
        public TextureHandle SceneDepth;
    }

    internal struct ChunkMesh
    {
        public Buffer Constants;

        public Buffer Vertices;
        public Buffer Indices;

        public int VertexCount;
        public int IndexCount;
    }

    internal struct RenderChunk
    {
        public Chunk Chunk;
        public ChunkMesh Mesh;
    }

    internal struct FrameConstants
    {
        public Matrix4x4 View;
        public Matrix4x4 Projection;
    }

    internal struct ChunkConstants
    {
        public Matrix4x4 World;
        public Matrix4x4 TexTransform;
        public int TextureIndex;
    }

    internal sealed class WorldPass : GraphicsRenderPass
    {
        private GraphicsDevice _device;
        private RenderChunk[] Chunks;
        private Buffer _frameConstants;
        private DescriptorHandle _textures;
        private Texture _bricks;
        private static readonly Rgba128 DefaultSkyColor = Rgba128.CornflowerBlue;

        public const int Width = 2, Height = 2, Depth = 2, NumChunkFaces = Width * Height * Depth * 6;

        private static class RootSignatureConstants
        {
            public const int FrameConstantsIndex = 0;
            public const int ObjectConstantsIndex = 1;
            public const int TextureIndex = 2;
        }

        public WorldPass(GraphicsDevice device)
        {
            _device = device;
            Chunks = new[] { new RenderChunk() };
            Chunks[0].Chunk.Blocks = new Block?[Width * Height * Depth];
            Chunks[0].Chunk.NeedsRebuild = true;

            foreach (ref readonly var block in Chunks[0].Chunk.Blocks.Span)
            {
                Unsafe.AsRef(in block) = new Block { TextureId = 0 };
            }

            var @params = new RootParameter[]
            {
                RootParameter.CreateDescriptor(RootParameterType.ConstantBufferView, 0, 0),
                RootParameter.CreateDescriptor(RootParameterType.ConstantBufferView, 1, 0),
                RootParameter.CreateDescriptorTable(DescriptorRangeType.ShaderResourceView, 0, 1, 0)
            };

            var samplers = new StaticSampler[]
            {
                new StaticSampler(TextureAddressMode.Clamp, SamplerFilterType.Point, 0, 0, ShaderVisibility.Pixel)
            };

            var rootSig = _device.CreateRootSignature(@params, samplers);

            var shaderFlags = new[]
            {
                ShaderCompileFlag.PackMatricesInRowMajorOrder
            };

            var psoDesc = new GraphicsPipelineDesc
            {
                RootSignature = rootSig,
                Topology = TopologyClass.Triangle,
                RenderTargetFormats = BackBufferFormat.R8G8B8A8UnsignedNormalized,
                DepthStencilFormat = DataFormat.Depth32Single,

                VertexShader = ShaderManager.CompileShader("Shaders/ChunkShader.hlsl", ShaderType.Vertex, shaderFlags, "VertexMain"),
                PixelShader = ShaderManager.CompileShader("Shaders/ChunkShader.hlsl", ShaderType.Pixel, shaderFlags, "PixelMain"),
                Inputs = InputLayout.FromType<BlockVertex>()
            };

            DefaultPipelineState = _device.PipelineManager.CreatePipelineStateObject(psoDesc, "ChunkPso");
            Debugger.Break();



            UploadTextures();
            SetConstants();
        }

        private void UploadTextures()
        {
            using var upload = _device.BeginUploadContext();

            //_bricks = upload.UploadTexture(File.ReadAllBytes("Assets/Textures/bricks.dds"));
            _bricks = upload.UploadTexture(File.ReadAllBytes("Assets/Textures/stone.dds"));
            _textures = _device.CreateShaderResourceView(_bricks);
        }

        private unsafe void SetConstants()
        {
            _frameConstants = _device.Allocator.AllocateUploadBuffer<FrameConstants>();
            Chunks[0].Mesh.Constants = _device.Allocator.AllocateUploadBuffer<ChunkConstants>();

            FrameConstants* constants = _frameConstants.DataPointerAs<FrameConstants>();

            constants->View = Matrix4x4.CreateLookAt(
                new Vector3(0.0f, 0.7f, 1.5f),
                new Vector3(0.0f, -0.1f, 0.0f),
                new Vector3(0.0f, 1.0f, 0.0f)
            );
            const float defaultFov = 70.0f * (float)Math.PI / 180.0f;
            constants->Projection = Matrix4x4.CreatePerspectiveFieldOfView(defaultFov, 1, 0.01f, 100f);



            ChunkConstants* chunkConstants = Chunks[0].Mesh.Constants.DataPointerAs<ChunkConstants>();
            chunkConstants->TexTransform = Matrix4x4.Identity;
            chunkConstants->World = Matrix4x4.CreateTranslation(0, 0, -5);
            chunkConstants->TextureIndex = 0;
        }

        public override void Register(ref RenderPassBuilder builder, ref Resolver resolver)
        {
            RenderResources resources;

            resources.SceneColor = builder.CreatePrimaryOutputRelativeTexture(
                TextureDesc.CreateRenderTargetDesc(BackBufferFormat.R8G8B8A8UnsignedNormalized, DefaultSkyColor),
                ResourceState.RenderTarget
            );

            resources.SceneDepth = builder.CreatePrimaryOutputRelativeTexture(
                TextureDesc.CreateDepthStencilDesc(DataFormat.Depth32Single, 1, 0),
                ResourceState.DepthWrite
            );

            resolver.CreateComponent(resources);
        }

        public override void Record(GraphicsContext context, ref Resolver resolver)
        {
            var resources = resolver.GetComponent<RenderResources>();
            var sceneColor = resolver.ResolveResource(resources.SceneColor);
            var sceneDepth = resolver.ResolveResource(resources.SceneDepth);




            context.SetAndClearRenderTarget(_device.CreateRenderTargetView(sceneColor), DefaultSkyColor, _device.CreateDepthStencilView(sceneDepth));
            context.SetRootDescriptorTable(RootSignatureConstants.TextureIndex, _textures);
            context.SetConstantBuffer<FrameConstants>(RootSignatureConstants.FrameConstantsIndex, _frameConstants);
            context.SetTopology(Topology.TriangeList);
            context.SetViewportAndScissor(sceneColor.Resolution);

            for (var i = 0; i < Chunks.Length; i++)
            {
                ref var chunk = ref Chunks[i];

                if (chunk.Chunk.NeedsRebuild)
                {
                    BuildMesh(ref chunk);
                }

                context.SetConstantBuffer<ChunkConstants>(RootSignatureConstants.ObjectConstantsIndex, chunk.Mesh.Constants);

                context.SetVertexBuffers<BlockVertex>(chunk.Mesh.Vertices);
                context.SetIndexBuffer<uint>(chunk.Mesh.Indices);

                context.DrawIndexed(chunk.Mesh.IndexCount);
            }
        }

        public void BuildMesh(ref RenderChunk chunkPair)
        {
            ref var chunk = ref chunkPair.Chunk;
            ref var mesh = ref chunkPair.Mesh;

            if (!mesh.Vertices.IsAllocated)
            {
                mesh.Vertices = _device.Allocator.AllocateBuffer(FaceHelper.FaceVerticesSize * NumChunkFaces, MemoryAccess.CpuUpload);
            }
            if (!mesh.Indices.IsAllocated)
            {
                mesh.Indices = _device.Allocator.AllocateBuffer(FaceHelper.FaceIndicesSize * NumChunkFaces, MemoryAccess.CpuUpload);
            }

            int numVertices = 0, numIndices = 0;

            var vertexSpan = mesh.Vertices.DataAs<BlockVertex>();
            var v2 = vertexSpan;
            var indexSpan = mesh.Indices.DataAs<uint>();

            for (var i = 0; i < Width; i++)
            {
                for (var j = 0; j < Height; j++)
                {
                    for (var k = 0; k < Depth; k++)
                    {
                        BuildVisibleFaces(ref chunk, ref vertexSpan, ref indexSpan, i, j, k, ref numVertices, ref numIndices);
                    }
                }
            }

            mesh.VertexCount = numVertices;
            mesh.IndexCount = numIndices;

            chunk.NeedsRebuild = false;
        }

        private void BuildVisibleFaces(ref Chunk chunk, ref Span<BlockVertex> vertices, ref Span<uint> indices, int x, int y, int z, ref int numGeneratedVertices, ref int numGeneratedIndices)
        {
            var blocks = new ReadOnlySpan3D<Block?>(chunk.Blocks.Span, Width, Height, Depth);

            var maybeBlock = blocks[x, y, z];

            if (maybeBlock is null)
            {
                return;
            }

            // Check for faces
            var hasLeftFace = x == 0 || blocks[x - 1, y, z] is null;
            var hasRightFace = x == Width - 1 || blocks[x + 1, y, z] is null;

            var hasBottomFace = y == 0 || blocks[x, y - 1, z] is null;
            var hasTopFace = y == Height - 1 || blocks[x, y + 1, z] is null;

            var hasFrontFace = z == 0 || blocks[x, y, z - 1] is null;
            var hasBackFace = z == Depth - 1 || blocks[x, y, z + 1] is null;

            if (hasLeftFace)
            {
                AddFace(FaceHelper.LeftFace, ref vertices, ref indices, x, y, z, ref numGeneratedVertices, ref numGeneratedIndices);
            }
            if (hasRightFace)
            {
                AddFace(FaceHelper.RightFace, ref vertices, ref indices, x, y, z, ref numGeneratedVertices, ref numGeneratedIndices);
            }
            if (hasTopFace)
            {
                AddFace(FaceHelper.TopFace, ref vertices, ref indices, x, y, z, ref numGeneratedVertices, ref numGeneratedIndices);
            }
            if (hasBottomFace)
            {
                AddFace(FaceHelper.BottomFace, ref vertices, ref indices, x, y, z, ref numGeneratedVertices, ref numGeneratedIndices);
            }
            if (hasFrontFace)
            {
                AddFace(FaceHelper.FrontFace, ref vertices, ref indices, x, y, z, ref numGeneratedVertices, ref numGeneratedIndices);
            }
            if (hasBackFace)
            {
                AddFace(FaceHelper.BackFace, ref vertices, ref indices, x, y, z, ref numGeneratedVertices, ref numGeneratedIndices);
            }

            static void AddFace(BlockVertex[] blockVertices, ref Span<BlockVertex> vertices, ref Span<uint> indices, int x, int y, int z, ref int numGeneratedVertices, ref int numGeneratedIndices)
            {
                for (int i = 0; i < 4; i++)
                {
                    vertices[i] = blockVertices[i];
                    vertices[i].Position = Vector3.Transform(vertices[i].Position, Matrix4x4.CreateTranslation(x, y, z));
                }
                vertices = vertices[4..];

                // 0, 1, 2,
                // 0, 2, 3,

                //indices[0] = (uint)numGeneratedVertices;
                //indices[1] = (uint)numGeneratedVertices + 1;
                //indices[2] = (uint)numGeneratedVertices + 2;
                //indices[3] = (uint)numGeneratedVertices;
                //indices[4] = (uint)numGeneratedVertices + 2;
                //indices[5] = (uint)numGeneratedVertices + 3;


                // i have no clue why but we need to reverse the order of the faces???????
                indices[0] = (uint)numGeneratedVertices + 2;
                indices[1] = (uint)numGeneratedVertices + 1;
                indices[2] = (uint)numGeneratedVertices;
                indices[3] = (uint)numGeneratedVertices + 3;
                indices[4] = (uint)numGeneratedVertices + 2;
                indices[5] = (uint)numGeneratedVertices;


                indices = indices[6..];

                numGeneratedVertices += 4;
                numGeneratedIndices += 6;
            }
        }
    }
}
