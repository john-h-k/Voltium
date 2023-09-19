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
using Voltium.Core.CommandBuffer;
using Voltium.Core.Configuration.Graphics;
using Voltium.Core.Devices;
using Voltium.Core.Devices.Shaders;
using Voltium.Core.Memory;
using Voltium.Core.Pipeline;
using Voltium.ModelLoading;
using Voltium.RenderEngine;
using Voltium.RenderEngine.EntityComponentSystem;
using Voltium.TextureLoading;
using static Voltium.RenderEngine.RenderGraph;
using Buffer = Voltium.Core.Memory.Buffer;

namespace Voltium.CubeGame
{
    public enum BlockId
    {
        Air,
        Stone,
        Brick,
        Dirt
    }

    internal struct Block
    {
        public bool IsOpaque => BlockId != BlockId.Air;

        public BlockId BlockId;
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

    internal sealed class WorldPass
    {
        private GraphicsDevice _device;
        private Output _output;
        private Buffer _frameConstants;
        private View _rtv, _dsv;
        private DescriptorAllocation _texViews;
        private Texture _textures;
        private Buffer _faceVertices;
        private Buffer _chunkIndices;
        private PipelineStateObject _chunkDrawPso;

        private DataFormat _renderFormat = DataFormat.R8G8B8A8UnsignedNormalized;

        public const uint MaxChunkCount = 256;
        public const int
            ChunkWidth = 16,
            ChunkHeight = 16,
            ChunkDepth = 16,
            NumChunkFaces = ChunkWidth * ChunkHeight * ChunkDepth * 6,
            NumChunkVertices = ChunkWidth * ChunkHeight * ChunkDepth * 24 / 2;



        private Camera _camera;
        private static readonly Rgba128 DefaultSkyColor = Rgba128.CornflowerBlue;


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

            BuildPsos();
        }

        private void BuildMeshes()
        {
            float Radius = 0.5f;
            var vertices = new[]
            {
                // Fill in the front face vertex data.
	            new BlockVertex(-Radius, -Radius, -Radius, 0.0f, 0.0f, -1.0f, 1.0f, 0.0f, 0.0f, 0.0f, 1.0f),
                new BlockVertex(-Radius, +Radius, -Radius, 0.0f, 0.0f, -1.0f, 1.0f, 0.0f, 0.0f, 0.0f, 0.0f),
                new BlockVertex(+Radius, +Radius, -Radius, 0.0f, 0.0f, -1.0f, 1.0f, 0.0f, 0.0f, 1.0f, 0.0f),
                new BlockVertex(+Radius, -Radius, -Radius, 0.0f, 0.0f, -1.0f, 1.0f, 0.0f, 0.0f, 1.0f, 1.0f),

                // Fill in the back face vertex data.
                new BlockVertex(-Radius, -Radius, +Radius, 0.0f, 0.0f, 1.0f, -1.0f, 0.0f, 0.0f, 1.0f, 1.0f),
                new BlockVertex(+Radius, -Radius, +Radius, 0.0f, 0.0f, 1.0f, -1.0f, 0.0f, 0.0f, 0.0f, 1.0f),
                new BlockVertex(+Radius, +Radius, +Radius, 0.0f, 0.0f, 1.0f, -1.0f, 0.0f, 0.0f, 0.0f, 0.0f),
                new BlockVertex(-Radius, +Radius, +Radius, 0.0f, 0.0f, 1.0f, -1.0f, 0.0f, 0.0f, 1.0f, 0.0f),

                // Fill in the left face vertex data.
                new BlockVertex(-Radius, -Radius, +Radius, -1.0f, 0.0f, 0.0f, 0.0f, 0.0f, -1.0f, 0.0f, 1.0f),
                new BlockVertex(-Radius, +Radius, +Radius, -1.0f, 0.0f, 0.0f, 0.0f, 0.0f, -1.0f, 0.0f, 0.0f),
                new BlockVertex(-Radius, +Radius, -Radius, -1.0f, 0.0f, 0.0f, 0.0f, 0.0f, -1.0f, 1.0f, 0.0f),
                new BlockVertex(-Radius, -Radius, -Radius, -1.0f, 0.0f, 0.0f, 0.0f, 0.0f, -1.0f, 1.0f, 1.0f),

                // Fill in the right face vertex data.
                new BlockVertex(+Radius, -Radius, -Radius, 1.0f, 0.0f, 0.0f, 0.0f, 0.0f, 1.0f, 0.0f, 1.0f),
                new BlockVertex(+Radius, +Radius, -Radius, 1.0f, 0.0f, 0.0f, 0.0f, 0.0f, 1.0f, 0.0f, 0.0f),
                new BlockVertex(+Radius, +Radius, +Radius, 1.0f, 0.0f, 0.0f, 0.0f, 0.0f, 1.0f, 1.0f, 0.0f),
                new BlockVertex(+Radius, -Radius, +Radius, 1.0f, 0.0f, 0.0f, 0.0f, 0.0f, 1.0f, 1.0f, 1.0f),

                // Fill in the top face vertex data.
                new BlockVertex(-Radius, +Radius, -Radius, 0.0f, 1.0f, 0.0f, 1.0f, 0.0f, 0.0f, 0.0f, 1.0f),
                new BlockVertex(-Radius, +Radius, +Radius, 0.0f, 1.0f, 0.0f, 1.0f, 0.0f, 0.0f, 0.0f, 0.0f),
                new BlockVertex(+Radius, +Radius, +Radius, 0.0f, 1.0f, 0.0f, 1.0f, 0.0f, 0.0f, 1.0f, 0.0f),
                new BlockVertex(+Radius, +Radius, -Radius, 0.0f, 1.0f, 0.0f, 1.0f, 0.0f, 0.0f, 1.0f, 1.0f),

                // Fill in the bottom face vertex data.
                new BlockVertex(-Radius, -Radius, -Radius, 0.0f, -1.0f, 0.0f, -1.0f, 0.0f, 0.0f, 1.0f, 1.0f),
                new BlockVertex(+Radius, -Radius, -Radius, 0.0f, -1.0f, 0.0f, -1.0f, 0.0f, 0.0f, 0.0f, 1.0f),
                new BlockVertex(+Radius, -Radius, +Radius, 0.0f, -1.0f, 0.0f, -1.0f, 0.0f, 0.0f, 0.0f, 0.0f),
                new BlockVertex(-Radius, -Radius, +Radius, 0.0f, -1.0f, 0.0f, -1.0f, 0.0f, 0.0f, 1.0f, 0.0f),
            };

            var upload = new UploadBatch();

            _faceVertices = upload.UploadBuffer(vertices);
            _chunkIndices = _device.Allocator.AllocateDefaultBuffer<ushort>(MaxChunkCount);
        }

        private void BuildPsos()
        {
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

            var rootSig = _device.CreateRootSignature(@params, samplers, RootSignatureFlags.AllowInputAssembler);

            var shaderFlags = new[]
            {
                ShaderCompileFlag.PackMatricesInRowMajorOrder,
                ShaderCompileFlag.DisableOptimizations,
                ShaderCompileFlag.EnableDebugInformation,
                ShaderCompileFlag.WriteDebugInformationToFile()
            };

            var psoDesc = new GraphicsPipelineDesc
            {
                RootSignature = rootSig,
                Topology = Topology.TriangleList,
                RenderTargetFormats = _renderFormat,

                DepthStencilFormat = DataFormat.Depth32Single,

                VertexShader = ShaderManager.CompileShader("Shaders/ChunkShader.hlsl", ShaderType.Vertex, shaderFlags, "VertexMain"),
                PixelShader = ShaderManager.CompileShader("Shaders/ChunkShader.hlsl", ShaderType.Pixel, shaderFlags, "PixelMain"),
                Inputs = InputLayout.FromType<BlockVertex>()
            };

            _chunkDrawPso = _device.CreatePipelineStateObject(psoDesc);
            _frameConstants = _device.Allocator.AllocateUploadBuffer<FrameConstants>();
        }

        private RenderGraph _graph;
        public unsafe void Render()
        {
            var graph = _graph;
            var builder = graph.CreatePassBuilder();

            var color = graph.CreateTexture(
                TextureDesc.CreateRenderTargetDesc(state.Format, state.Width, state.Height, state.Background)
            );

            var depth = graph.CreateTexture(
                TextureDesc.CreateDepthStencilDesc(DataFormat.Depth32Single, state.Width, state.Height, 1, 0)
            );

            var colorView = graph.CreateView(color);
            var depthView = graph.CreateView(depth);

            graph.AddPass(
                "ChunkRender",

                (
                    Renderer: this,
                    Color: color,
                    Depth: depth,
                    ColorView: colorView,
                    DepthView: depthView,
                    SkyColor: DefaultSkyColor,
                    ChunkDrawState: _chunkDrawPso,
                    FrameConstants: _frameConstants
                ),

                new PassDescription
                {
                    Dependencies = stackalloc Dependency[]
                    {
                        Dependency.Create(color, ResourceState.RenderTarget),
                        Dependency.Create(depth, ResourceState.DepthWrite),
                    },
                    Decision = PassRegisterDecision.ExecutePass
                },

                static (resolver, state, info) =>
                {
                    var context = info.CommandBuffer;
                    var chunks = state.Chunks;

                    var sceneColorView = resolver.ResolveView(state.ColorView);
                    var sceneDepthView = resolver.ResolveView(state.DepthView);

                    context.SetPipelineState(state.ChunkDrawState);

                    context.BeginRenderPass(
                        new RenderTarget
                        {
                            Resource = sceneColorView,
                            Load = LoadOperation.Clear,
                            Store = StoreOperation.Preserve,
                            ColorClear = state.SkyColor
                        },
                        new DepthStencil
                        {
                            Resource = sceneDepthView,
                            DepthLoad = LoadOperation.Clear,
                            DepthStore = StoreOperation.Preserve,
                            DepthClear = 0,
                        }
                    );

                    context.SetIndexBuffer<ushort>(state.IndexBuffer, MaxChunkCount * NumChunkVertices);

                    FrameConstants* constants = state.FrameConstants.As<FrameConstants>();

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

                    for (var i = 0u; i < chunks.Length; i++)
                    {
                        ref var chunk = ref chunks[i];

                        context.DrawIndexed(chunk.Mesh.IndexCount);
                    }

                    context.EndRenderPass();
                }
            );

            graph.AddPass(
                "Tonemap",
                (Scene: color, Output: _output),

                new PassDescription
                {
                    Dependencies = stackalloc Dependency[]
                    {
                        Dependency.Create(color, ResourceState.CopySource)
                    },

                    Decision = PassRegisterDecision.ExecutePass | PassRegisterDecision.HasExternalOutputs
                },

                static (resolver, state, info) =>
                {
                    var context = info.CommandBuffer;

                    var scene = resolver.ResolveResource(state.Scene);
                    var output = state.Output.OutputBuffer;

                    context.Barrier(ResourceTransition.Create(output, ResourceState.Present, ResourceState.CopySource));

                    context.CopyTexture(scene, output, 0);

                    context.Barrier(ResourceTransition.Create(output, ResourceState.CopySource, ResourceState.Present));
                }
            );

            graph.ExecuteGraph();
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

            var blocks = new ReadOnlySpan3D<Block>(chunk.Blocks.Span, ChunkWidth, ChunkHeight, ChunkDepth);

            for (var i = 0; i < ChunkWidth; i++)
            {
                for (var j = 0; j < ChunkHeight; j++)
                {
                    for (var k = 0; k < ChunkDepth; k++)
                    {
                        BuildVisibleFaces(ref blocks, ref vertexSpan, ref indexSpan, ref texIds, i, j, k, ref numVertices, ref numIndices);
                    }
                }
            }

            mesh.VertexCount = numVertices;
            mesh.IndexCount = numIndices;

            chunk.NeedsRebuild = false;
        }

        private void BuildVisibleFaces(ref ReadOnlySpan3D<Block> blocks, ref Span<BlockVertex> vertices, ref Span<uint> indices, ref Span<uint> texIds, int x, int y, int z, ref int numGeneratedVertices, ref int numGeneratedIndices)
        {
            var block = blocks[x, y, z];

            // Check for faces
            // TODO clean this up
            var hasLeftFace = x == 0 || !blocks[x - 1, y, z].IsOpaque;
            var hasRightFace = x == ChunkWidth - 1 || !blocks[x + 1, y, z].IsOpaque;

            var hasBottomFace = y == 0 || !blocks[x, y - 1, z].IsOpaque;
            var hasTopFace = y == ChunkHeight - 1 || !blocks[x, y + 1, z].IsOpaque;

            var hasFrontFace = z == 0 || !blocks[x, y, z - 1].IsOpaque;
            var hasBackFace = z == ChunkDepth - 1 || !blocks[x, y, z + 1].IsOpaque;

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
