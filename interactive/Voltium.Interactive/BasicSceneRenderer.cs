using System;
using System.Drawing;
using System.Numerics;
using System.Runtime.InteropServices;
using Voltium.Common;
using Voltium.Core;
using Voltium.Core.Configuration.Graphics;
using Voltium.Core.Memory;
using Voltium.Core.Devices;
using Voltium.Core.Devices.Shaders;
using Voltium.Core.Pipeline;
using Voltium.ModelLoading;
using Voltium.TextureLoading;
using Voltium.Common.Pix;
using Buffer = Voltium.Core.Memory.Buffer;

namespace Voltium.Interactive
{
    [StructLayout(LayoutKind.Sequential)]
    public partial struct ObjectConstants
    {
        public Matrix4x4 World;
        public Matrix4x4 Tex;
        public Material Material;
    }

    [StructLayout(LayoutKind.Sequential)]
    public partial struct FrameConstants
    {
        public Matrix4x4 View;
        public Matrix4x4 Projection;
        public Vector4 AmbientLight;
        public Vector3 CameraPosition;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct LightConstants
    {
        public DirectionalLight Light0;
        public DirectionalLight Light1;
        public DirectionalLight Light2;
    }

    [StructLayout(LayoutKind.Sequential)]
    public partial struct DirectionalLight
    {
        public Vector3 Strength;
        private float _pad0;
        public Vector3 Direction;
        private float _pad1;
    }

    public unsafe class BasicSceneRenderer : Renderer
    {
        private GpuAllocator _allocator = null!;
        private Buffer[] _vertexBuffer = null!;
        private Buffer[] _indexBuffer = null!;
        private Texture _texture;
        private Texture _normals;
        private int _zoomDelta;
        private RenderObject<TexturedVertex>[] _texturedObjects = null!;
        private GraphicsDevice _device = null!;
        private DescriptorHandle _texHandle;
        private DescriptorHandle _normalHandle;
        private Size _outputResolution;

        private RootSignature _rootSig = null!;
        private MsaaDesc _msaaDesc = MsaaDesc.None;
        private MsaaDesc _maxMsaaDesc = MsaaDesc.None;

        private ObjectConstants[] _objectConstants = null!;
        private FrameConstants _frameConstants;
        private LightConstants _sceneLight;

        private Buffer _obj;
        private Buffer _frame;
        private Buffer _light;

        private Texture _renderTarget;
        private DescriptorHandle _renderTargetView;


        private Texture _depthStencil;
        private DescriptorHandle _depthStencilView;

        private bool _msaa = false;

        public override void Init(GraphicsDevice device, in Size screen)
        {
            _device = device;
            _allocator = _device.Allocator;
            _outputResolution = screen;

            _maxMsaaDesc = MsaaDesc.None;

            _texturedObjects = ModelLoader.LoadGl_Old("Assets/Gltf/Handgun_Tangent.gltf");
            var texture = TextureLoader.LoadTextureDesc("Assets/Textures/handgun_c.dds");
            var normals = TextureLoader.LoadTextureDesc("Assets/Textures/handgun_n.dds");

            _vertexBuffer = new Buffer[_texturedObjects.Length];
            _indexBuffer = new Buffer[_texturedObjects.Length];

            using (var list = _device.BeginUploadContext())
            using (_device.BeginScopedCapture())
            {
                for (var i = 0; i < _texturedObjects.Length; i++)
                {
                    _vertexBuffer[i] = list.UploadBuffer(_texturedObjects[i].Vertices);
                    _vertexBuffer[i].SetName("VertexBuffer");

                    _indexBuffer[i] = list.UploadBuffer(_texturedObjects[i].Indices);
                    _indexBuffer[i].SetName("IndexBuffer");
                }

                _texture = list.UploadTexture(texture.Data.Span, texture.SubresourceData.Span, texture.Desc);
                _texture.SetName("Gun texture");

                _normals = list.UploadTexture(normals.Data.Span, normals.SubresourceData.Span, normals.Desc);
                _normals.SetName("Gun normals");
            }

            _texHandle = _device.CreateShaderResourceView(_texture);
            _normalHandle = _device.CreateShaderResourceView(_normals);
            _objectConstants = new ObjectConstants[_texturedObjects.Length];

            _obj = _allocator.AllocateBuffer(MathHelpers.AlignUp(sizeof(ObjectConstants), 256) * _texturedObjects.Length, MemoryAccess.CpuUpload);
            _obj.SetName("ObjectConstants buffer");

            _frame = _allocator.AllocateBuffer(sizeof(FrameConstants), MemoryAccess.CpuUpload);
            _frame.SetName("FrameConstants buffer");

            _light = _allocator.AllocateBuffer(sizeof(LightConstants), MemoryAccess.CpuUpload);
            _light.SetName("LightConstants buffer");

            Resize(screen);
            CreatePipelines();
            InitializeConstants();
        }

        public override void Resize(Size newScreenData)
        {
            _outputResolution = newScreenData;
            var dsDesc = TextureDesc.CreateDepthStencilDesc(DataFormat.Depth32Single, (uint)newScreenData.Width, (uint)newScreenData.Height, 1, 0, false, _msaaDesc);
            var rtDesc = TextureDesc.CreateRenderTargetDesc(DataFormat.R8G8B8A8UnsignedNormalized, (uint)newScreenData.Width, (uint)newScreenData.Height, Rgba128.CornflowerBlue, _msaaDesc);

            _depthStencil.Dispose();
            _renderTarget.Dispose();

            _depthStencil = _allocator.AllocateTexture(dsDesc, ResourceState.DepthWrite);
            _depthStencil.SetName("Depth Stencil");
            _renderTarget = _allocator.AllocateTexture(rtDesc, ResourceState.RenderTarget);
            _renderTarget.SetName("Render Target");

            _depthStencilView = _device.CreateDepthStencilView(_depthStencil);
            _renderTargetView = _device.CreateRenderTargetView(_renderTarget);

            var aspectRatio = (float)newScreenData.Width / newScreenData.Height;
            var fovAngleY = 70.0f * MathF.PI / 180.0f;
            _frameConstants.Projection = Matrix4x4.CreatePerspectiveFieldOfView(fovAngleY, aspectRatio, 0.001f, 100f);
        }

        public void CreatePipelines()
        {
            var rootParams = new[]
            {
                RootParameter.CreateDescriptor(RootParameterType.ConstantBufferView, 0, 0),
                RootParameter.CreateDescriptor(RootParameterType.ConstantBufferView, 1, 0),
                RootParameter.CreateDescriptor(RootParameterType.ConstantBufferView, 2, 0),
                RootParameter.CreateDescriptorTable(DescriptorRangeType.ShaderResourceView, 0, 2, 0)
            };

            var sampler = new StaticSampler(
                TextureAddressMode.Clamp,
                SamplerFilterType.Anistropic,
                shaderRegister: 0,
                registerSpace: 0,
                ShaderVisibility.All,
                StaticSampler.OpaqueWhite
            );

            _rootSig = _device.CreateRootSignature(rootParams, sampler);

            var compilationFlags = new[]
            {
                ShaderCompileFlag.PackMatricesInRowMajorOrder,
                ShaderCompileFlag.AllResourcesBound
                //ShaderCompileFlag.DefineMacro("NORMALS")
            };

            var vertexShader = ShaderManager.CompileShader("Shaders/SimpleTexture/TextureVertexShader.hlsl", ShaderType.Vertex, compilationFlags);
            var pixelShader = ShaderManager.CompileShader("Shaders/SimpleTexture/TexturePixelShader.hlsl", ShaderType.Pixel, compilationFlags);

            var psoDesc = new GraphicsPipelineDesc
            {
                RootSignature = _rootSig,
                RenderTargetFormats = _renderTarget.Format,
                DepthStencilFormat = _depthStencil.Format,
                VertexShader = vertexShader,
                PixelShader = pixelShader,
                Topology = TopologyClass.Triangle,
                Inputs = InputLayout.FromType<TexturedVertex>()
            };

            _pso = _device.PipelineManager.CreatePipelineStateObject(psoDesc, "Texture");

           // psoDesc.Msaa = MultisamplingDesc.X8;
            _msaaPso = _device.PipelineManager.CreatePipelineStateObject(psoDesc, "TextureMSAA");
        }

        public void InitializeConstants()
        {
            for (var i = 0; i < _texturedObjects.Length; i++)
            {
                var geometry = _texturedObjects[i];
                _objectConstants[i] = new ObjectConstants
                {
                    World = geometry.World,
                    Tex = Matrix4x4.Identity,
                    Material = geometry.Material
                };
            }

            _frameConstants = new FrameConstants
            {
                View = Matrix4x4.CreateLookAt(
                    new Vector3(0.0f, 0.7f, 1f),
                    new Vector3(0.0f, 0.0f, 0.0f),
                    new Vector3(0.0f, 1.0f, 0.0f)
                ),
                AmbientLight = new Vector4(0.25f, 0.25f, 0.35f, 1.0f) / 2,
                CameraPosition = new Vector3(0.0f, 0.7f, 1.5f),
            };

            _sceneLight.Light0 = new DirectionalLight
            {
                Strength = new Vector3(0.5f),
                Direction = new Vector3(0.57735f, -0.57735f, 0.57735f)
            };

            _sceneLight.Light1 = new DirectionalLight
            {
                Strength = new Vector3(0.5f),
                Direction = new Vector3(-0.57735f, -0.57735f, 0.57735f)
            };

            _sceneLight.Light2 = new DirectionalLight
            {
                Strength = new Vector3(0.5f),
                Direction = new Vector3(0.0f, -0.707f, -0.707f)
            };
        }


        public override void Update(ApplicationTimer timer)
        {
            for (var i = 0u; i < _objectConstants.Length; i++)
            {
                _objectConstants[i].World *= Matrix4x4.CreateRotationY(0.5f * (float)timer.ElapsedSeconds);
                _obj.WriteConstantBufferData(ref _objectConstants[i], i);
            }

            _frame.WriteConstantBufferData(ref _frameConstants, 0);
            _light.WriteConstantBufferData(ref _sceneLight, 0);

            _zoomDelta = 0;
        }

        public override void ToggleMsaa()
        {
            _msaa = !_msaa;
            _msaaDesc = _msaa ? _maxMsaaDesc : MsaaDesc.None;
            Resize(_outputResolution);
        }

        public override PipelineStateObject GetInitialPso()
        {
            return _msaa ? _msaaPso : _pso;
        }

        public override void Render(GraphicsContext recorder, out Texture render)
        {
            recorder.SetViewportAndScissor(_outputResolution);
            recorder.ResourceTransition(_renderTarget, ResourceState.RenderTarget);

            recorder.SetAndClearRenderTarget(_renderTargetView, Rgba128.CornflowerBlue, _depthStencilView);

            recorder.SetConstantBuffer(1, _frame);
            recorder.SetConstantBuffer(2, _light);
            recorder.SetRootDescriptorTable(3, _texHandle);

            recorder.SetTopology(Topology.TriangleList);

            using (recorder.BeginEvent(Argb32.AliceBlue, "Render Objects"))
            {
                for (var i = 0u; i < _texturedObjects.Length; i++)
                {
                    recorder.SetConstantBuffer<ObjectConstants>(0, _obj, i);
                    recorder.SetVertexBuffers<TexturedVertex>(_vertexBuffer[i]);
                    recorder.SetIndexBuffer<ushort>(_indexBuffer[i]);

                    recorder.DrawIndexed(_texturedObjects[i].Indices.Length);
                }
            }

            render = _renderTarget;
        }

        public override void Dispose()
        {
            _rootSig.Dispose();
            _device.Dispose();
        }

        public override void OnMouseScroll(int scroll)
        {
            _zoomDelta = scroll;
        }

        private void SetHueDegrees(float radians)
        {
            var cosA = MathF.Cos(radians);
            var sinA = MathF.Sin(radians);

            _hueMatrix[0, 0] = cosA + ((1.0f - cosA) / 3.0f);
            _hueMatrix[0, 1] = (1.0f / 3.0f * (1.0f - cosA)) - (MathF.Sqrt(1.0f / 3.0f) * sinA);
            _hueMatrix[0, 2] = (1.0f / 3.0f * (1.0f - cosA)) + (MathF.Sqrt(1.0f / 3.0f) * sinA);
            _hueMatrix[1, 0] = (1.0f / 3.0f * (1.0f - cosA)) + (MathF.Sqrt(1.0f / 3.0f) * sinA);
            _hueMatrix[1, 1] = cosA + (1.0f / 3.0f * (1.0f - cosA));
            _hueMatrix[1, 2] = (1.0f / 3.0f * (1.0f - cosA)) - (MathF.Sqrt(1.0f / 3.0f) * sinA);
            _hueMatrix[2, 0] = (1.0f / 3.0f * (1.0f - cosA)) - (MathF.Sqrt(1.0f / 3.0f) * sinA);
            _hueMatrix[2, 1] = (1.0f / 3.0f * (1.0f - cosA)) + (MathF.Sqrt(1.0f / 3.0f) * sinA);
            _hueMatrix[2, 2] = cosA + (1.0f / 3.0f * (1.0f - cosA));
        }

        private float[,] _hueMatrix = new float[3, 3];
        private PipelineStateObject _msaaPso = null!;
        private PipelineStateObject _pso = null!;

        private Rgba128 ChangeHue(Rgba128 color)
        {
            static int Clamp(float v)
            {
                if (v < 0)
                {
                    return 0;
                }
                if (v > 255)
                {
                    return 255;
                }
                return (int)(v + 0.5f);
            }

            var r0 = (byte)(color.R * 255);
            var g0 = (byte)(color.G * 255);
            var b0 = (byte)(color.B * 255);

            var fr0 = (r0 * _hueMatrix[0, 0]) + (g0 * _hueMatrix[0, 1]) + (b0 * _hueMatrix[0, 2]);
            var fg0 = (r0 * _hueMatrix[1, 0]) + (g0 * _hueMatrix[1, 1]) + (b0 * _hueMatrix[1, 2]);
            var fb0 = (r0 * _hueMatrix[2, 0]) + (g0 * _hueMatrix[2, 1]) + (b0 * _hueMatrix[2, 2]);

            return new Rgba128(Clamp(fr0) / 255f, Clamp(fg0) / 255f, Clamp(fb0) / 255f, 1);
        }
    }
}
