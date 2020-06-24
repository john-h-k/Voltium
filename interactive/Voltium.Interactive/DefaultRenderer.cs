using System.Diagnostics;
using System.Drawing;
using System.Resources;
using Voltium.Core;
using Voltium.Core.Managers;
using System.Numerics;
using Voltium.Core.GpuResources;
using Voltium.Core.Pipeline;
using System.Runtime.CompilerServices;
using Voltium.Core.Managers.Shaders;
using Voltium.Core.Configuration.Graphics;
using Voltium.Core.Memory.GpuResources;
using System;
using System.Runtime.InteropServices;
using Buffer = Voltium.Core.Memory.GpuResources.Buffer;
using Voltium.Core.D3D12;
using Voltium.ModelLoading;
using Voltium.TextureLoading;
using System.Linq;

namespace Voltium.Interactive
{
    [StructLayout(LayoutKind.Sequential, Size = 256)]
    public partial struct ObjectConstants
    {
        public Matrix4x4 World;
        public Matrix4x4 Tex;
        public Material Material;
    }

    [StructLayout(LayoutKind.Sequential, Size = 256)]
    public partial struct FrameConstants
    {
        public Matrix4x4 View;
        public Matrix4x4 Projection;
        public Vector4 AmbientLight;
        public Vector3 CameraPosition;
    }

    [StructLayout(LayoutKind.Sequential, Size = 256)]
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

    [ShaderType]
    [StructLayout(LayoutKind.Sequential)]
    public partial struct Material
    {
        public Vector4 DiffuseAlbedo;
        public Vector4 ReflectionFactor;
        public float Shininess;
    };

    //[ShaderInput]
    //[StructLayout(LayoutKind.Sequential)]
    //public partial struct Vertex
    //{
    //    public Vertex(Vector3 position, Vector3 normal, Vector2 texC)
    //    {
    //        Position = position;
    //        Normal = normal;
    //        TexC = texC;
    //    }

    //    public Vertex(
    //        float vertexX, float vertexY, float vertexZ,
    //        float normalX, float normalY, float normalZ,
    //        float tangentX, float tangentY, float tangentZ,
    //        float texU, float texV)
    //    {
    //        Position = new(vertexX, vertexY, vertexZ);
    //        Normal = new(normalX, normalY, normalZ);
    //        TexC = new(texU, texV);
    //    }

    //    public Vector3 Position;
    //    public Vector3 Normal;
    //    public Vector2 TexC;
    //}

    public unsafe class DefaultRenderer : Renderer
    {
        private GpuAllocator _allocator = null!;
        private Buffer[] _vertexBuffer = null!;
        private Buffer[] _indexBuffer = null!;
        private Texture _texture;
        private int _zoomDelta;
        private Geometry<TexturedVertex>[] _texturedObjects = null!;
        //private Geometry<ColorVertex>[] _colorObjects = null!;
        private GraphicsDevice _device = null!;
        private DescriptorHandle _texHandle;

        public override void Init(GraphicsDevice device, GraphicalConfiguration config, in ScreenData screen)
        {
            _device = device;
            _allocator = _device.Allocator;

            ModelLoader.Load("Assets/Handgun.gltf", out var meshes, out _);
            //var meshes = new[] { GemeotryGenerator.LoadSingleModel("logo.obj") };
            var texture = TextureLoader.CreateTexture("Assets/handgun_c.dds");

            _texturedObjects = new Geometry<TexturedVertex>[meshes.Length];
            _vertexBuffer = new Buffer[meshes.Length];
            _indexBuffer = new Buffer[meshes.Length];

            using (var list = _device.BeginCopyContext())
            {
                var buffIndex = 0;
                for (var i = 0; i < _texturedObjects.Length; i++, buffIndex++)
                {
                    _texturedObjects[i] = new Geometry<TexturedVertex>(meshes[i].Vertices, meshes[i].Indices, world: meshes[i].World);
                    list.UploadBuffer(_allocator, _texturedObjects[i].Vertices, MemoryAccess.GpuOnly, out _vertexBuffer[buffIndex]);
                    list.UploadBuffer(_allocator, _texturedObjects[i].Indices, MemoryAccess.GpuOnly, out _indexBuffer[buffIndex]);
                }

                list.UploadTexture(_allocator, texture.BitData.Span, texture.SubresourceData.Span, texture.Desc, out _texture);
            }

            var srvDesc = new TextureShaderResourceViewDesc
            {
                MipLevels = texture.MipCount,
                Format = texture.Desc.Format,
                MostDetailedMip = 0
            };

            _texHandle = _device.CreateShaderResourceView(_texture, srvDesc);
            _objectConstants = new ObjectConstants[_texturedObjects.Length];

            _obj = _allocator.AllocateBuffer(sizeof(ObjectConstants) * _texturedObjects.Length, MemoryAccess.CpuUpload);
            _frame = _allocator.AllocateBuffer(sizeof(FrameConstants), MemoryAccess.CpuUpload);
            _light = _allocator.AllocateBuffer(sizeof(LightConstants), MemoryAccess.CpuUpload);

            CreatePipelines(config);
        }

        public void CreatePipelines(GraphicalConfiguration config)
        {
            var rootParams = new[]
            {
                RootParameter.CreateDescriptor(RootParameterType.ConstantBufferView, 0, 0),
                RootParameter.CreateDescriptor(RootParameterType.ConstantBufferView, 1, 0),
                RootParameter.CreateDescriptor(RootParameterType.ConstantBufferView, 2, 0),
                RootParameter.CreateDescriptorTable(new DescriptorRange(DescriptorRangeType.ShaderResourceView, 0, 1, 0))
            };

            var samplers = new[]
            {
                new StaticSampler(
                    TextureAddressMode.Wrap,
                    SamplerFilterType.MagPoint | SamplerFilterType.MinPoint | SamplerFilterType.MipPoint,
                    shaderRegister: 0,
                    registerSpace: 0,
                    ShaderVisibility.All,
                    StaticSampler.OpaqueWhite
                )
            };

            _rootSig = RootSignature.Create(_device, rootParams, samplers);

            var compilationFlags = new[]
            {
                DxcCompileFlags.PackMatricesInRowMajorOrder,
                DxcCompileFlags.EnableDebugInformation,
                DxcCompileFlags.WriteDebugInformationToFile()
            };

            var vertexShader = ShaderManager.CompileShader("Shaders/SimpleTexture/TextureVertexShader.hlsl", DxcCompileTarget.Vs_6_0, compilationFlags);
            var pixelShader = ShaderManager.CompileShader("Shaders/SimpleTexture/TexturePixelShader.hlsl", DxcCompileTarget.Ps_6_0, compilationFlags);

            var psoDesc = new GraphicsPipelineDesc(_rootSig, config.BackBufferFormat, config.DepthStencilFormat, vertexShader, pixelShader);

            _texPso = PipelineManager.CreatePso<TexturedVertex>(_device, "Texture", psoDesc);
        }

        private ObjectConstants[] _objectConstants = null!;
        private FrameConstants _frameConstants;
        private LightConstants _sceneLight;

        private Buffer _obj;
        private Buffer _frame;
        private Buffer _light;

        public override void Resize(ScreenData screen)
        {
            var aspectRatio = (float)screen.Width / screen.Height;
            var fovAngleY = 70.0f * MathF.PI / 180.0f;

            for (var i = 0; i < _texturedObjects.Length; i++)
            {
                var geometry = _texturedObjects[i];
                _objectConstants[i] = new ObjectConstants
                {
                    World = geometry.World,
                    Tex = Matrix4x4.Identity,
                    Material = new Material
                    {
                        DiffuseAlbedo = new Vector4(1.0f, 1.0f, 1.0f, 1.0f),
                        ReflectionFactor = new(0.05f),
                        Shininess = 0.8f
                    }
                };
            }

            _frameConstants = new FrameConstants
            {
                View = Matrix4x4.CreateLookAt(
                    new Vector3(0.0f, 0.7f, 1.5f),
                    new Vector3(0.0f, -0.1f, 0.0f),
                    new Vector3(0.0f, 1.0f, 0.0f)
                ),
                Projection = Matrix4x4.CreatePerspectiveFieldOfView(fovAngleY, aspectRatio, 0.001f, 100f),
                AmbientLight = new Vector4(0.25f, 0.25f, 0.35f, 1.0f) / 2,
                CameraPosition = new Vector3(0.0f, 0.7f, 1.5f),
            };

            _sceneLight.Light0 = new DirectionalLight
            {
                Strength = new Vector3(0, 0, 0.8f),
                Direction = new Vector3(0.57735f, -0.57735f, 0.57735f)
            };


            _sceneLight.Light1 = new DirectionalLight
            {
                Strength = new Vector3(0, 0, 0.7f),
                Direction = new Vector3(-0.57735f, -0.57735f, 0.57735f)
            };

            _sceneLight.Light2 = new DirectionalLight
            {
                Strength = new Vector3(0, 1, 0),
                Direction = new Vector3(0.0f, -0.707f, -0.707f)
            };
        }

        private RootSignature _rootSig = null!;
        private PipelineStateObject _texPso = null!;
        //private PipelineStateObject _colorPso = null!;

        private Matrix4x4 _perFrameRotation = Matrix4x4.CreateRotationY(10f)/* * Matrix4x4.CreateRotationX(0.001f)*/;
        //private int _totalCount = 0;

        public override void Update(ApplicationTimer timer)
        {
            // scale between 0 and 5 seconds
            //var scale = Matrix4x4.CreateScale((float)(Math.Abs((total % 10) - 5)) / 5);u
            float scale = _zoomDelta;

            if (scale < 0)
            {
                scale = 1 / Math.Abs(scale);
            }
            else if (scale == 0)
            {
                scale = 1;
            }

            //_objectConstants.World *= Matrix4x4.CreateScale(scale);

            // rotate a small amount each frame
            //

            for (var i = 0u; i < _objectConstants.Length; i++)
            {
                _objectConstants[i].World *= Matrix4x4.CreateRotationY(0.5f * (float)timer.ElapsedSeconds);
                _obj.WriteConstantBufferData(ref _objectConstants[i], i);
            }

            _frame.WriteConstantBufferData(ref _frameConstants, 0);
            _light.WriteConstantBufferData(ref _sceneLight, 0);

            _zoomDelta = 0;
        }

        public override PipelineStateObject GetInitialPso()
        {
            return _texPso;
        }

        public override void Render(GraphicsContext recorder)
        {
            var renderTarget = _device.RenderTarget;
            var renderTargetView = _device.RenderTargetView;
            var depthStencilView = _device.DepthStencilView;

            recorder.ResourceTransition(renderTarget, ResourceState.RenderTarget);

            recorder.SetRenderTarget(renderTargetView, depthStencilView);
            recorder.ClearRenderTarget(renderTargetView, RgbaColor.CornflowerBlue);
            recorder.ClearDepth(depthStencilView);

            recorder.SetGraphicsRootSignature(_rootSig);
            recorder.SetGraphicsConstantBuffer(1, _frame);
            recorder.SetGraphicsConstantBuffer(2, _light);
            recorder.SetGraphicsRootDescriptorTable(3, _texHandle);

            recorder.SetTopology(Topology.TriangeList);

            RenderObjects<TexturedVertex, ushort>(recorder);

            recorder.ResourceTransition(renderTarget, ResourceState.Present);
        }

        private void RenderObjects<TVertex, TIndex>(GraphicsContext recorder)
            where TVertex : unmanaged
            where TIndex : unmanaged
        {
            for (var i = 0u; i < _texturedObjects.Length; i++)
            {
                recorder.SetGraphicsConstantBuffer<ObjectConstants>(0, _obj, i);
                recorder.SetVertexBuffers<TVertex>(_vertexBuffer[i]);
                recorder.SetIndexBuffer<TIndex>(_indexBuffer[i]);

                recorder.DrawIndexed(_texturedObjects[i].Indices.Length);
            }
        }

        public override void Destroy()
        {
            _rootSig.Dispose();
            _device.Dispose();
        }

        public override void OnMouseScroll(int scroll)
        {
            _zoomDelta = scroll;
        }
    }
}

/*
        private void SetHueDegrees(float radians)
        {
            var cosA = MathF.Cos(radians);
            var sinA = MathF.Sin(radians);

            HueMatrix[0, 0] = cosA + ((1.0f - cosA) / 3.0f);
            HueMatrix[0, 1] = (1.0f / 3.0f * (1.0f - cosA)) - (MathF.Sqrt(1.0f / 3.0f) * sinA);
            HueMatrix[0, 2] = (1.0f / 3.0f * (1.0f - cosA)) + (MathF.Sqrt(1.0f / 3.0f) * sinA);
            HueMatrix[1, 0] = (1.0f / 3.0f * (1.0f - cosA)) + (MathF.Sqrt(1.0f / 3.0f) * sinA);
            HueMatrix[1, 1] = cosA + (1.0f / 3.0f * (1.0f - cosA));
            HueMatrix[1, 2] = (1.0f / 3.0f * (1.0f - cosA)) - (MathF.Sqrt(1.0f / 3.0f) * sinA);
            HueMatrix[2, 0] = (1.0f / 3.0f * (1.0f - cosA)) - (MathF.Sqrt(1.0f / 3.0f) * sinA);
            HueMatrix[2, 1] = (1.0f / 3.0f * (1.0f - cosA)) + (MathF.Sqrt(1.0f / 3.0f) * sinA);
            HueMatrix[2, 2] = cosA + (1.0f / 3.0f * (1.0f - cosA));
        }

        private float[,] HueMatrix = new float[3, 3];

        private RgbaColor ChangeHue(RgbaColor color)
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

            var fr0 = (r0 * HueMatrix[0, 0]) + (g0 * HueMatrix[0, 1]) + (b0 * HueMatrix[0, 2]);
            var fg0 = (r0 * HueMatrix[1, 0]) + (g0 * HueMatrix[1, 1]) + (b0 * HueMatrix[1, 2]);
            var fb0 = (r0 * HueMatrix[2, 0]) + (g0 * HueMatrix[2, 1]) + (b0 * HueMatrix[2, 2]);

            return new RgbaColor(Clamp(fr0) / 255f, Clamp(fg0) / 255f, Clamp(fb0) / 255f, 1);
        }*/
