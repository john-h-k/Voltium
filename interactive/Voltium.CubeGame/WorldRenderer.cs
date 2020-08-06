//using System.Collections.Generic;
//using System.Linq;
//using System.Numerics;
//using System.Runtime.CompilerServices;
//using System.Text;
//using System.Threading.Tasks;
//using Microsoft.VisualBasic;
//using Voltium.Core;
//using Voltium.Core.Devices;
//using Voltium.Core.Devices.Shaders;
//using Voltium.Core.Memory;
//using Voltium.Core.Pipeline;
//using Voltium.ModelLoading;
//using Voltium.RenderEngine;
//using Voltium.RenderEngine.EntityComponentSystem;

//namespace Voltium.CubeGame
//{
//    internal struct Renderable
//    {
//        public Mesh<BlockVertex> Mesh;


//        internal struct Constants
//        {
//            public Matrix4x4 World;
//            public int TextureId;
//        }

//        public Constants ObjectConstants;
//    }

//    internal struct Block
//    {
//        public uint TextureId;
//    }

//    [ShaderInput]
//    internal struct BlockVertex
//    {
//        public Vector3 Position;
//        public Vector3 Normal;
//        public Vector3 Tangent;
//        public Vector2 TexCoord;

//        public BlockVertex(
//            float positionX, float positionY, float positionZ,
//            float normalX, float normalY, float normalZ,
//            float tangentX, float tangentY, float tangentZ,
//            float texCoordX, float texCoordY
//        ) :
//            this(
//            new Vector3(positionX, positionY, positionZ),
//            new Vector3(normalX, normalY, normalZ),
//            new Vector3(tangentX, tangentY, tangentZ),
//            new Vector2(texCoordX, texCoordY)
//            )
//        {
//        }

//        public BlockVertex(Vector3 position, Vector3 normal, Vector3 tangent, Vector2 texCoord)
//        {
//            Position = position;
//            Normal = normal;
//            Tangent = tangent;
//            TexCoord = texCoord;
//        }
//    }

//    internal struct RenderResources
//    {
//        public TextureHandle SceneColor;
//        public TextureHandle SceneDepth;
//    }

//    internal sealed class WorldRenderer : GraphicsRenderPass
//    {
//        private GraphicsDevice _device;
//        private GraphicsPipelineStateObject _chunkPso;
//        private Chunk[] Chunks;
//        private static readonly Rgba128 DefaultSkyColor;

//        public WorldRenderer(GraphicsDevice device)
//        {
//            _device = device;

//            var psoDesc = new GraphicsPipelineDesc
//            {
//                Topology = TopologyClass.Triangle,
//            };

//            _chunkPso = _device.PipelineManager.CreatePipelineStateObject(nameof(_chunkPso), psoDesc);
//        }

//        public override void Register(ref RenderPassBuilder builder, ref Resolver resolver)
//        {
//            RenderResources resources;

//            resources.SceneColor = builder.CreatePrimaryOutputRelativeTexture(
//                TextureDesc.CreateRenderTargetDesc(BackBufferFormat.R8G8B8A8UnsignedNormalized, DefaultSkyColor),
//                ResourceState.RenderTarget
//            );

//            resources.SceneDepth = builder.CreatePrimaryOutputRelativeTexture(
//                TextureDesc.CreateDepthStencilDesc(DataFormat.Depth32Single, 1, 0),
//                ResourceState.DepthRead
//            );

//            resolver.CreateComponent(resources);
//        }

//        public override void Record(GraphicsContext context, ref Resolver resolver)
//        {
//            var resources = resolver.GetComponent<RenderResources>();
//            var sceneColor = resolver.ResolveResource(resources.SceneColor);
//            var sceneDepth = resolver.ResolveResource(resources.SceneDepth);


//            context.SetAndClearRenderTarget(_device.CreateRenderTargetView(sceneColor), DefaultSkyColor, _device.CreateDepthStencilView(sceneDepth));

//            for (var i = 0; i < Chunks.Length; i++)
//            {
//                ref var chunk = ref Chunks[i];

//                if (chunk.NeedsRebuild)
//                {

//                }
//            }
//        }
//    }
//}
