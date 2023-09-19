//using System;
//using System.Drawing;
//using System.Numerics;
//using System.Runtime.InteropServices;
//using Voltium.Common;
//using Voltium.Core;
//using Voltium.Core.Configuration.Graphics;
//using Voltium.Core.Memory;
//using Voltium.Core.Devices;
//using Voltium.Core.Devices.Shaders;
//using Voltium.Core.Pipeline;
//using Voltium.ModelLoading;
//using Voltium.TextureLoading;
//using Voltium.Common.Pix;
//using Buffer = Voltium.Core.Memory.Buffer;
//using Voltium.RenderEngine;
//using SixLabors.ImageSharp;
//using System.Diagnostics.CodeAnalysis;
//using Voltium.Core.CommandBuffer;
//using System.Data;

//namespace Voltium.Interactive.BasicRenderPipeline
//{
//    [StructLayout(LayoutKind.Sequential)]
//    public partial struct ObjectConstants
//    {
//        public Matrix4x4 World;
//        public Matrix4x4 Tex;
//        public Material Material;
//    }

//    [StructLayout(LayoutKind.Sequential)]
//    public partial struct FrameConstants
//    {
//        public Matrix4x4 View;
//        public Matrix4x4 Projection;
//        public Vector4 AmbientLight;
//        public Vector3 CameraPosition;
//    }

//    [StructLayout(LayoutKind.Sequential)]
//    public struct LightConstants
//    {
//        public DirectionalLight Light0;
//        public DirectionalLight Light1;
//        public DirectionalLight Light2;
//    }

//    [StructLayout(LayoutKind.Sequential)]
//    public partial struct DirectionalLight
//    {
//        public Vector3 Strength;
//        private float _pad0;
//        public Vector3 Direction;
//        private float _pad1;
//    }

//    public unsafe class BasicSceneRenderer : Application
//    {
//        private GraphicsDevice _device = null!;
//        private Buffer[] _vertexBuffer = null!;
//        private Buffer[] _indexBuffer = null!;
//        private Texture _texture;

//        private DescriptorAllocation _constants;
//        private DescriptorAllocation _textureHandle;

//        private View _rtv, _dsv;
//        //private Texture _normals;
//        //private DescriptorHandle _normalHandle;

//        private ObjectConstants[] _objectConstants = null!;
//        private FrameConstants _frameConstants;
//        private LightConstants _sceneLight;

//        private Buffer _obj;
//        private Buffer _frame;
//        private Buffer _light;

//        private PipelineStateObject _tex = null!;

//        private const DataFormat DepthStencilFormat = DataFormat.Depth32Single;
//        private const DataFormat RenderTargetFormat = DataFormat.R8G8B8A8UnsignedNormalized;

//        public BasicSceneRenderer(GraphicsDevice device) { }

//        public override void Initialize(System.Drawing.Size outputSize, IOutputOwner output)
//        {
//            _device = GraphicsDevice.Create(FeatureLevel.GraphicsLevel11_0, null);

//            _rtvs = _device.CreateDescriptorHeap(DescriptorHeapType.RenderTargetView, 1);
//            _dsvs = _device.CreateDescriptorHeap(DescriptorHeapType.RenderTargetView, 1);
//            _textureHandle = _device.AllocateResourceDescriptors(2);

//            var texturedObjects = ModelLoader.LoadGl_Old("Assets/Gltf/Handgun_NoTangent.gltf");
//            //_texturedObjects = new[] { GeometryGenerator.CreateCube(0.5f) };

//            _vertexBuffer = new Buffer;
//            _indexBuffer = new Buffer;

//            using (var list = _device.BeginUploadContext())
//            {
//                for (var i = 0; i < texturedObjects.Length; i++)
//                {
//                    _vertexBuffer[i] = list.UploadBuffer(texturedObjects[i].Vertices);
//                    _vertexBuffer[i].SetName("VertexBuffer");

//                    _indexBuffer[i] = list.UploadBuffer(texturedObjects[i].Indices);
//                    _indexBuffer[i].SetName("IndexBuffer");
//                }

//                _texture = list.UploadTexture("Assets/Textures/handgun_c.dds");
//                _texture.SetName("Gun texture");

//                //list.UploadTexture("Assets/Textures/handgun_n.dds");
//                //_normals.SetName("Gun normals");
//            }

//            _device.CreateShaderResourceView(_texture, _textureHandle[0]);
//            //_normalHandle = _device.CreateShaderResourceView(_normals);
//            _objectConstants = new ObjectConstants[texturedObjects.Length];

//            _obj = _device.Allocator.AllocateUploadBuffer(MathHelpers.AlignUp(sizeof(ObjectConstants), 256) * texturedObjects.Length);
//            _obj.SetName("ObjectConstants buffer");

//            _frame = _device.Allocator.AllocateUploadBuffer<FrameConstants>();
//            _frame.SetName("FrameConstants buffer");

//            _light = _device.Allocator.AllocateUploadBuffer<LightConstants>();
//            _light.SetName("LightConstants buffer");



//            CreatePipelines();
//            InitializeConstants();
//        }

//        private const int PassConstants = 0;
//        private const int ObjectConstants = 0;
//        private const int Textures = 0;

//        [MemberNotNull(nameof(_tex))]
//        public void CreatePipelines()
//        {
//            var rootParams = new RootParameter[3];
//            rootParams[ObjectConstants] = RootParameter.CreateDescriptorTable(DescriptorRangeType.ConstantBufferView, 0, 1, 0);
//            rootParams[PassConstants] = RootParameter.CreateDescriptorTable(DescriptorRangeType.ConstantBufferView, 1, 2, 0);
//            rootParams[Textures] = RootParameter.CreateDescriptorTable(DescriptorRangeType.ShaderResourceView, 0, 1, 0);

//            var sampler = new StaticSampler(
//                TextureAddressMode.Clamp,
//                SamplerFilterType.Anistropic,
//                shaderRegister: 0,
//                registerSpace: 0,
//                ShaderVisibility.All,
//                StaticSampler.OpaqueWhite
//            );

//            var compilationFlags = new[]
//            {
//                ShaderCompileFlag.PackMatricesInRowMajorOrder,
//                ShaderCompileFlag.AllResourcesBound,
//                ShaderCompileFlag.EnableDebugInformation,
//                ShaderCompileFlag.WriteDebugInformationToFile()
//                //ShaderCompileFlag.DefineMacro("NORMALS")
//            };

//            var vertexShader = ShaderManager.CompileShader("Shaders/SimpleTexture/TextureVertexShader.hlsl", ShaderModel.Vs_6_0, compilationFlags);
//            var pixelShader = ShaderManager.CompileShader("Shaders/SimpleTexture/TexturePixelShader.hlsl", ShaderModel.Ps_6_0, compilationFlags);

//            var psoDesc = new GraphicsPipelineDesc
//            {
//                RootSignature = _device.CreateRootSignature(rootParams, sampler),
//                RenderTargetFormats = RenderTargetFormat,
//                DepthStencilFormat = DepthStencilFormat,
//                VertexShader = vertexShader,
//                PixelShader = pixelShader,
//                Topology = TopologyClass.Triangle,
//                Inputs = InputLayout.FromType<TexturedVertex>()
//            };

//            _tex = _device.PipelineManager.CreatePipelineStateObject(psoDesc, "Texture");
//        }


//        public void InitializeConstants()
//        {
//            for (var i = 0; i < _texturedObjects.Length; i++)
//            {
//                var geometry = _texturedObjects[i];
//                _objectConstants[i] = new ObjectConstants
//                {
//                    World = geometry.World,
//                    Tex = Matrix4x4.Identity,
//                    Material = geometry.Material
//                };
//            }

//            _frameConstants = new FrameConstants
//            {
//                View = Matrix4x4.CreateLookAt(
//                    new Vector3(0.0f, 0.7f, 1.5f),
//                    new Vector3(0.0f, -0.1f, 0.0f),
//                    new Vector3(0.0f, 1.0f, 0.0f)
//                ),
//                Projection = Matrix4x4.Identity,
//                AmbientLight = new Vector4(0.25f, 0.25f, 0.35f, 1.0f) / 2,
//                CameraPosition = new Vector3(0.0f, 0.7f, 1.5f),
//            };

//            _sceneLight.Light0 = new DirectionalLight
//            {
//                Strength = new Vector3(0.5f),
//                Direction = new Vector3(0.57735f, -0.57735f, 0.57735f)
//            };

//            _sceneLight.Light1 = new DirectionalLight
//            {
//                Strength = new Vector3(0.5f),
//                Direction = new Vector3(-0.57735f, -0.57735f, 0.57735f)
//            };

//            _sceneLight.Light2 = new DirectionalLight
//            {
//                Strength = new Vector3(0.5f),
//                Direction = new Vector3(0.0f, -0.707f, -0.707f)
//            };
//        }


//        public void WriteConstantBuffers()
//        {
//            for (var i = 0U; i < _objectConstants.Length; i++)
//            {
//                _obj.WriteConstantBufferData(ref _objectConstants[i], i);
//            }

//            _frame.WriteConstantBufferData(ref _frameConstants, 0);
//            _light.WriteConstantBufferData(ref _sceneLight, 0);
//        }

//        public override void Update(ApplicationTimer timer)
//        {
//            var resources = new PipelineResources();
//            var settings = resolver.GetComponent<PipelineSettings>();

//            resources.SceneColor = builder.CreatePrimaryOutputRelativeTexture(
//                TextureDesc.CreateRenderTargetDesc(DataFormat.R8G8B8A8UnsignedNormalized, Rgba128.CornflowerBlue, settings.Msaa),
//                ResourceState.RenderTarget,
//                debugName: nameof(resources.SceneColor)
//            );

//            resources.SceneDepth = builder.CreatePrimaryOutputRelativeTexture(
//                TextureDesc.CreateDepthStencilDesc(DataFormat.Depth32Single, 1.0f, 0, false, settings.Msaa),
//                ResourceState.DepthWrite,
//                debugName: nameof(resources.SceneDepth)
//            );

//            var fovAngleY = 70.0f * MathF.PI / 180.0f;
//            _frameConstants.Projection = Matrix4x4.CreatePerspectiveFieldOfView(fovAngleY, settings.AspectRatio, 0.001f, 100f);

//        public override void Render()
//        {
//            WriteConstantBuffers();

//            using var recorder = _device.BeginGraphicsContext(_tex);

//            recorder.BeginRenderPass(
//                new RenderTarget
//                {
//                    Resource = _rtv,
//                    Load = LoadOperation.Clear,
//                    Store = StoreOperation.Preserve,
//                    ColorClear = Rgba128.CornflowerBlue
//                },
//                new DepthStencil
//                {
//                    Resource = _dsv,
//                    DepthClear = 1,
//                    DepthLoad = LoadOperation.Clear,
//                    DepthStore = StoreOperation.Preserve,
//                }
//            );

//            for (var i = 0u; i < _texturedObjects.Length; i++)
//            {
//                recorder.BindDescriptors();

//                recorder.SetVertexBuffers<TexturedVertex>(_vertexBuffer[i]);
//                recorder.SetIndexBuffer<uint>(_indexBuffer[i]);

//                recorder.DrawIndexed(_texturedObjects[i].Indices.Length);
//            }

//            recorder.EndRenderPass();
//        }
//    }
//}
