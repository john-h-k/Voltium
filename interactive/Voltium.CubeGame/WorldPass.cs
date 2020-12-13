using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualBasic;
using Voltium.Core;
using Voltium.Core.Configuration.Graphics;
using Voltium.Core.Devices;
using Voltium.Core.Devices.Shaders;
using Voltium.Core.Memory;
using Voltium.Core.Pipeline;
using Voltium.Core.Views;
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

    internal struct RenderSettings
    {
        public MsaaDesc Msaa;
    }

    internal struct ChunkMesh
    {
        public Buffer Constants;
        public Buffer TexIndices;

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
    }

    internal sealed class WorldPass : GraphicsRenderPass
    {
        private GraphicsDevice _device;
        private RenderChunk[] Chunks;
        private Buffer _frameConstants;
        private DescriptorHeap _rtvs, _dsvs;
        private DescriptorAllocation _texViews;
        private Texture[] _textures;
        private Camera _camera;
        private static readonly Rgba128 DefaultSkyColor = Rgba128.CornflowerBlue;

        public const int Width = 2, Height = 2, Depth = 2, NumChunkFaces = Width * Height * Depth * 6;

        private static class RootSignatureConstants
        {
            public const int FrameConstantsIndex = 0;
            public const int ObjectConstantsIndex = 1;
            public const int ObjectTexIndicesIndex = 2;
            public const int TextureIndex = 3;
        }

        public WorldPass(GraphicsDevice device, Camera camera)
        {
            _device = device;
            _camera = camera;

            _rtvs = _device.CreateDescriptorHeap(DescriptorHeapType.RenderTargetView, 1);
            _dsvs = _device.CreateDescriptorHeap(DescriptorHeapType.RenderTargetView, 1);

            var @params = new RootParameter[]
            {
                RootParameter.CreateDescriptor(RootParameterType.ConstantBufferView, 0, 0),
                RootParameter.CreateDescriptor(RootParameterType.ConstantBufferView, 1, 0),
                RootParameter.CreateDescriptor(RootParameterType.ShaderResourceView, 0, 0, ShaderVisibility.Pixel),
                RootParameter.CreateDescriptorTable(DescriptorRangeType.ShaderResourceView, 1, 2, 0, visibility: ShaderVisibility.Pixel)
            };

            var samplers = new StaticSampler[]
            {
                new StaticSampler(TextureAddressMode.Clamp, SamplerFilterType.MagPoint | SamplerFilterType.MinPoint | SamplerFilterType.MipLinear, 0, 0, ShaderVisibility.Pixel)
            };

            RootSignature = _device.CreateRootSignature(@params, samplers);

            var shaderFlags = new[]
            {
                ShaderCompileFlag.PackMatricesInRowMajorOrder,
                ShaderCompileFlag.DisableOptimizations,
                ShaderCompileFlag.EnableDebugInformation,
                ShaderCompileFlag.WriteDebugInformationToFile()
            };

            var psoDesc = new GraphicsPipelineDesc
            {
                RootSignature = RootSignature,
                Topology = TopologyClass.Triangle,
                RenderTargetFormats = BackBufferFormat.R8G8B8A8UnsignedNormalized,
                
                DepthStencilFormat = DataFormat.Depth32Single,

                VertexShader = ShaderManager.CompileShader("Shaders/ChunkShader.hlsl", ShaderType.Vertex, shaderFlags, "VertexMain"),
                PixelShader = ShaderManager.CompileShader("Shaders/ChunkShader.hlsl", ShaderType.Pixel, shaderFlags, "PixelMain"),
                Inputs = InputLayout.FromType<BlockVertex>()
            };

            Pso = _device.PipelineManager.CreatePipelineStateObject(psoDesc, "ChunkPso");

            psoDesc.Msaa = MsaaDesc.X8;

            MsaaPso = _device.PipelineManager.CreatePipelineStateObject(psoDesc, "MsaaChunkPso");

            UploadTextures();

            Chunks = new[] { new RenderChunk() };
            Chunks[0].Chunk.Blocks = new Block?[Width * Height * Depth];
            Chunks[0].Chunk.NeedsRebuild = true;

            var rng = new Random();
            foreach (ref readonly var block in Chunks[0].Chunk.Blocks.Span)
            {
                Unsafe.AsRef(in block) = new Block { TextureId = (uint)rng.Next(0, _textures.Length) };
            }

            SetConstants();
        }

        private RootSignature RootSignature;
        private PipelineStateObject Pso, MsaaPso;

        [MemberNotNull(nameof(_textures))]
        private void UploadTextures()
        {
            using var upload = _device.BeginUploadContext();

            _textures = new Texture[2];

            _textures[0] = upload.UploadTexture("Assets/Textures/bricks.dds");
            _textures[1] = upload.UploadTexture("Assets/Textures/stone.dds");

            _texViews = _device.AllocateResourceDescriptors(_textures.Length);

            for (var i = 0; i < _textures.Length; i++)
            {
                _device.CreateShaderResourceView(_textures[i], _texViews[i]);
            }
        }

        private unsafe void SetConstants()
        {
            _frameConstants = _device.Allocator.AllocateUploadBuffer<FrameConstants>();
            Chunks[0].Mesh.Constants = _device.Allocator.AllocateUploadBuffer<ChunkConstants>();

            FrameConstants* constants = _frameConstants.As<FrameConstants>();

            constants->View = Matrix4x4.CreateLookAt(
                new Vector3(0.0f, 0.7f, 1.5f),
                new Vector3(0.0f, -0.1f, 0.0f),
                new Vector3(0.0f, 1.0f, 0.0f)
            );
            const float defaultFov = 70.0f * (float)Math.PI / 180.0f;
            constants->Projection = Matrix4x4.CreatePerspectiveFieldOfView(defaultFov, 1, 0.01f, 100f);

            ChunkConstants* chunkConstants = Chunks[0].Mesh.Constants.As<ChunkConstants>();
            chunkConstants->TexTransform = Matrix4x4.Identity;
            chunkConstants->World = Matrix4x4.CreateTranslation(0, 0, -5);
        }

        public override bool Register(ref RenderPassBuilder builder, ref Resolver resolver)
        {
            var settings = resolver.GetComponent<RenderSettings>();

            RenderResources resources;

            resources.SceneColor = builder.CreatePrimaryOutputRelativeTexture(
                TextureDesc.CreateRenderTargetDesc(BackBufferFormat.R8G8B8A8UnsignedNormalized, DefaultSkyColor, settings.Msaa),
                ResourceState.RenderTarget
            );

            resources.SceneDepth = builder.CreatePrimaryOutputRelativeTexture(
                TextureDesc.CreateDepthStencilDesc(DataFormat.Depth32Single, 1, 0, false, settings.Msaa),
                ResourceState.DepthWrite
            );

            resolver.CreateComponent(resources);

            DefaultPipelineState = settings.Msaa.IsMultiSampled ? MsaaPso : Pso;

            builder.SetOutput(resources.SceneColor);

            return true;
        }

        public override unsafe void Record(GraphicsContext context, ref Resolver resolver)
        {
            var resources = resolver.GetComponent<RenderResources>();
            var sceneColor = resolver.ResolveResource(resources.SceneColor);
            var sceneDepth = resolver.ResolveResource(resources.SceneDepth);

            var frame = _frameConstants.As<FrameConstants>();
            frame->View = _camera.View;
            frame->Projection = _camera.Projection;

            _device.CreateRenderTargetView(sceneColor, _rtvs[0]);
            _device.CreateDepthStencilView(sceneDepth, _dsvs[0]);

            var progressBuffer = _device.Allocator.AllocateBuffer(1024, MemoryAccess.GpuOnly);

            context.SetRootSignature(RootSignature);
            context.SetAndClearRenderTarget(_rtvs[0], DefaultSkyColor, _dsvs[0]);
            context.SetRootDescriptorTable(RootSignatureConstants.TextureIndex, _texViews[0]);
            context.SetConstantBuffer<FrameConstants>(RootSignatureConstants.FrameConstantsIndex, _frameConstants);
            context.SetTopology(Topology.TriangleList);
            context.SetViewportAndScissor(sceneColor.Resolution);

            for (var i = 0u; i < Chunks.Length; i++)
            {
                ref var chunk = ref Chunks[i];

                if (chunk.Chunk.NeedsRebuild)
                {
                    BuildMesh(ref chunk);
                }

                context.SetConstantBuffer<ChunkConstants>(RootSignatureConstants.ObjectConstantsIndex, chunk.Mesh.Constants);
                context.SetShaderResourceBuffer<uint>(RootSignatureConstants.ObjectTexIndicesIndex, chunk.Mesh.TexIndices);

                context.SetVertexBuffers<BlockVertex>(chunk.Mesh.Vertices, (uint)chunk.Mesh.VertexCount, 0);
                context.SetIndexBuffer<uint>(chunk.Mesh.Indices, (uint)chunk.Mesh.IndexCount);

                context.WriteBufferImmediate(progressBuffer.GpuAddress + (sizeof(int) * i), GetDrawIndex(i), CopyContext.WriteBufferImmediateMode.In);
                context.DrawIndexed(chunk.Mesh.IndexCount);


                static uint GetDrawIndex(uint i) => i;
            }
        }

        public unsafe void BuildMesh(ref RenderChunk chunkPair)
        {
            ref var chunk = ref chunkPair.Chunk;
            ref var mesh = ref chunkPair.Mesh;

            var constants = mesh.Constants.As<ChunkConstants>();
            constants->World = Matrix4x4.CreateTranslation(0, 0, -5);
            constants->TexTransform = Matrix4x4.Identity;

            if (!mesh.Vertices.IsAllocated)
            {
                mesh.Vertices = _device.Allocator.AllocateUploadBuffer(FaceHelper.FaceVerticesSize * NumChunkFaces);
            }
            if (!mesh.Indices.IsAllocated)
            {
                mesh.Indices = _device.Allocator.AllocateUploadBuffer(FaceHelper.FaceIndicesSize * NumChunkFaces);
            }
            if (!mesh.TexIndices.IsAllocated)
            {
                mesh.TexIndices = _device.Allocator.AllocateUploadBuffer<uint>(NumChunkFaces);
            }

            int numVertices = 0, numIndices = 0;

            var vertexSpan = mesh.Vertices.AsSpan<BlockVertex>();
            var indexSpan = mesh.Indices.AsSpan<uint>();
            var texIds = mesh.TexIndices.AsSpan<uint>();

            var blocks = new ReadOnlySpan3D<Block?>(chunk.Blocks.Span, Width, Height, Depth);

            for (var i = 0; i < Width; i++)
            {
                for (var j = 0; j < Height; j++)
                {
                    for (var k = 0; k < Depth; k++)
                    {
                        BuildVisibleFaces(ref blocks, ref vertexSpan, ref indexSpan, ref texIds, i, j, k, ref numVertices, ref numIndices);
                    }
                }
            }

            mesh.VertexCount = numVertices;
            mesh.IndexCount = numIndices;

            chunk.NeedsRebuild = false;
        }

        private void BuildVisibleFaces(ref ReadOnlySpan3D<Block?> blocks, ref Span<BlockVertex> vertices, ref Span<uint> indices, ref Span<uint> texIds, int x, int y, int z, ref int numGeneratedVertices, ref int numGeneratedIndices)
        {
            var maybeBlock = blocks[x, y, z];

            if (maybeBlock is null)
            {
                return;
            }

            var block = maybeBlock.GetValueOrDefault();

            // Check for faces
            // TODO clean this up
            var hasLeftFace = x == 0 || blocks[x - 1, y, z] is null;
            var hasRightFace = x == Width - 1 || blocks[x + 1, y, z] is null;

            var hasBottomFace = y == 0 || blocks[x, y - 1, z] is null;
            var hasTopFace = y == Height - 1 || blocks[x, y + 1, z] is null;

            var hasFrontFace = z == 0 || blocks[x, y, z - 1] is null;
            var hasBackFace = z == Depth - 1 || blocks[x, y, z + 1] is null;

            if (hasLeftFace)
            {
                AddFace(block, FaceHelper.LeftFace, ref vertices, ref indices, ref texIds, x, y, z, ref numGeneratedVertices, ref numGeneratedIndices);
            }
            if (hasRightFace)
            {
                AddFace(block, FaceHelper.RightFace, ref vertices, ref indices, ref texIds, x, y, z, ref numGeneratedVertices, ref numGeneratedIndices);
            }
            if (hasTopFace)
            {
                AddFace(block, FaceHelper.TopFace, ref vertices, ref indices, ref texIds, x, y, z, ref numGeneratedVertices, ref numGeneratedIndices);
            }
            if (hasBottomFace)
            {
                AddFace(block, FaceHelper.BottomFace, ref vertices, ref indices, ref texIds, x, y, z, ref numGeneratedVertices, ref numGeneratedIndices);
            }
            if (hasFrontFace)
            {
                AddFace(block, FaceHelper.FrontFace, ref vertices, ref indices, ref texIds, x, y, z, ref numGeneratedVertices, ref numGeneratedIndices);
            }
            if (hasBackFace)
            {
                AddFace(block, FaceHelper.BackFace, ref vertices, ref indices, ref texIds, x, y, z, ref numGeneratedVertices, ref numGeneratedIndices);
            }

            static void AddFace(in Block block, BlockVertex[] blockVertices, ref Span<BlockVertex> vertices, ref Span<uint> indices, ref Span<uint> texIds, int x, int y, int z, ref int numGeneratedVertices, ref int numGeneratedIndices)
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

                texIds[0] = block.TextureId;
                texIds = texIds[1..];

                numGeneratedVertices += 4;
                numGeneratedIndices += 6;
            }
        }
    }
}
