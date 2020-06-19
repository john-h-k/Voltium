using System.Diagnostics;
using System.Drawing;
using System.Resources;
using TerraFX.Interop;
using Voltium.Common;
using Voltium.Core;
using Voltium.Core.Managers;
using static TerraFX.Interop.D3D12_RESOURCE_STATES;
using static TerraFX.Interop.DXGI_FORMAT;
using static TerraFX.Interop.D3D12_INPUT_CLASSIFICATION;
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
using Voltium.TextureLoading;

namespace Voltium.Interactive
{
    [StructLayout(LayoutKind.Sequential, Size = 256)]
    public partial struct ObjectConstants
    {
        public Matrix4x4 World;
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
    }

    [StructLayout(LayoutKind.Sequential)]
    public partial struct DirectionalLight
    {
        public Vector3 Strength;
        private float _pad0;
        public Vector3 Direction;
        private float _pad1;
    }

    [StructLayout(LayoutKind.Sequential)]
    public partial struct Material
    {
        public Vector4 DiffuseAlbedo;
        public Vector4 ReflectionFactor;
        public float Shininess;
    };

    [ShaderInput]
    public partial struct Vertex
    {
        public Vertex(Vector3 position, Vector3 normal, Vector2 texC)
        {
            Position = position;
            Normal = normal;
            TexC = texC;
        }

        public Vertex(
            float vertexX, float vertexY, float vertexZ,
            float normalX, float normalY, float normalZ,
            float tangentX, float tangentY, float tangentZ,
            float texU, float texV)
        {
            Position = new(vertexX, vertexY, vertexZ);
            Normal = new(normalX, normalY, normalZ);
            TexC = new(texU, texV);
        }

        public Vector3 Position;
        public Vector3 Normal;
        public Vector2 TexC;
    }

    public unsafe class DefaultRenderer : Renderer
    {
        private GpuAllocator _allocator = null!;
        private Buffer _vertexBuffer;
        private Buffer _indexBuffer;
        private Texture _texture;
        private int _zoomDelta;
        private Geometry _object;
        private GraphicsDevice _device = null!;
        private DescriptorHeap _textures;

        public override void Init(GraphicsDevice device, GraphicalConfiguration config, in ScreenData screen)
        {
            _device = device;
            _allocator = _device.Allocator;

            //_object = GemeotryGenerator.LoadSingleModel("logo.obj");
            _object = GemeotryGenerator.CreateCube(0.5f);

            var texture = TextureLoader.CreateDdsTexture("Assets/bricks3.dds");
            var desc = new TextureDesc
            {
                Width = texture.Width,
                Height = texture.Width,
                DepthOrArraySize = (ushort)(texture.Depth == 0 ? texture.Depth : texture.ArraySize),
                Format = texture.Format,
                Dimension = texture.ResourceDimension,
                MemoryKind = MemoryAccess.GpuOnly,
                InitialResourceState = ResourceState.PixelShaderResource
            };

            _textures = DescriptorHeap.CreateConstantBufferShaderResourceUnorderedAccessViewHeap(_device, 1);

            _allocator.AllocateBuffer(11586 * 11586, MemoryAccess.CpuUpload, ResourceState.GenericRead);

            _texture = _allocator.AllocateTexture(desc);
            _vertexBuffer = _allocator.AllocateBuffer(_object.Vertices.Length * sizeof(Vertex), MemoryAccess.GpuOnly, ResourceState.CopyDestination);
            _indexBuffer = _allocator.AllocateBuffer(_object.Indices.Length * sizeof(ushort), MemoryAccess.GpuOnly, ResourceState.CopyDestination);

            _obj = _allocator.AllocateBuffer(sizeof(ObjectConstants), MemoryAccess.CpuUpload, ResourceState.GenericRead);
            _frame = _allocator.AllocateBuffer(sizeof(FrameConstants), MemoryAccess.CpuUpload, ResourceState.GenericRead);
            _light = _allocator.AllocateBuffer(sizeof(LightConstants), MemoryAccess.CpuUpload, ResourceState.GenericRead);

            var list = _device.BeginGraphicsContext();
            list.UploadResource(_allocator, MemoryMarshal.AsBytes(_object.Vertices.AsSpan()), _vertexBuffer);
            list.UploadResource(_allocator, MemoryMarshal.AsBytes(_object.Indices.AsSpan()), _indexBuffer);
            list.UploadResource(_allocator, texture.BitData.Span, texture.SubresourceData.Span, _texture);
            _device.End(list);

            var rootParams = new[]
            {
                RootParameter.CreateDescriptor(RootParameterType.ConstantBufferView, 0, 0),
                RootParameter.CreateDescriptor(RootParameterType.ConstantBufferView, 1, 0),
                RootParameter.CreateDescriptor(RootParameterType.ConstantBufferView, 2, 0)
            };

            var samplers = new[]
            {
                new StaticSampler(
                    new Sampler(
                        TextureAddressMode.BorderColor,
                        TextureAddressMode.BorderColor,
                        TextureAddressMode.BorderColor,
                        SamplerFilterType.Anistropic,
                        0,
                        8,
                        SampleComparisonFunc.LessThanOrEqual,
                        StaticSampler.OpaqueBlack,
                        0,
                        Windows.D3D12_FLOAT32_MAX
                    ),
                    0,
                    0,
                    ShaderVisibility.All)
            };

            _rootSig = RootSignature.Create(device.Device, rootParams, samplers);

            var compilationFlags = new[]
            {
                DxcCompileFlags.PackMatricesInRowMajorOrder
            };

            var vertexShader = ShaderManager.CompileShader("Shaders/SimpleVertexShader.hlsl", DxcCompileTarget.Vs_6_0, compilationFlags);
            var pixelShader = ShaderManager.CompileShader("Shaders/TexturePixelShader.hlsl", DxcCompileTarget.Ps_6_0, compilationFlags);

            var psoDesc = new GraphicsPipelineDesc(_rootSig, config.BackBufferFormat, config.DepthStencilFormat, vertexShader, pixelShader);

            _drawPso = PipelineManager.CreatePso<Vertex>(_device, "Default", psoDesc);

            SetHueDegrees(MathF.PI / 500);
        }

        private ObjectConstants _objectConstants;
        private FrameConstants _frameConstants;
        private LightConstants _sceneLight;

        private Buffer _obj;
        private Buffer _frame;
        private Buffer _light;

        public override void Resize(ScreenData screen)
        {
            var aspectRatio = (float)screen.Width / screen.Height;
            var fovAngleY = 70.0f * MathF.PI / 180.0f;

            _objectConstants = new ObjectConstants
            {
                World = Matrix4x4.Identity,
                Material = new Material
                {
                    DiffuseAlbedo = (Vector4)RgbaColor.WhiteSmoke,
                    ReflectionFactor = new(0.3f),
                    Shininess = 1f
                }
            };

            _frameConstants = new FrameConstants
            {
                View = Matrix4x4.CreateLookAt(
                    new Vector3(0.0f, 0.7f, 1.5f),
                    new Vector3(0.0f, -0.1f, 0.0f),
                    new Vector3(0.0f, 1.0f, 0.0f)
                ),
                Projection = Matrix4x4.CreatePerspectiveFieldOfView(fovAngleY, aspectRatio, 0.001f, 100f),
                AmbientLight = new Vector4(0.25f, 0.25f, 0.25f, 1.0f) / 2,
                CameraPosition = new Vector3(0.0f, 0.7f, 1.5f),
            };

            _sceneLight.Light0 = new DirectionalLight
            {
                Strength = new Vector3(1, 1, 1),
                Direction = new Vector3(0, -1, 0)
            };


            _sceneLight.Light1 = new DirectionalLight
            {
                Strength = new Vector3(0.8f),
                Direction = new Vector3(-0.57735f, 0.57735f, -0.57735f)
            };
        }

        private RgbaColor _color = new RgbaColor(1, 0, 0, 1);
        private RootSignature _rootSig = null!;
        private PipelineStateObject _drawPso = null!;

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

            _objectConstants.World *= Matrix4x4.CreateScale(scale);

            Console.WriteLine(timer.ElapsedSeconds);

            // rotate a small amount each frame
            _objectConstants.World *= Matrix4x4.CreateRotationY(0.5f * (float)timer.ElapsedSeconds);

            _obj.WriteData(ref _objectConstants, 0);
            _frame.WriteData(ref _frameConstants, 0);
            _light.WriteData(ref _sceneLight, 0);

            _zoomDelta = 0;

            //_color = ChangeHue(_color);
        }

        public override PipelineStateObject GetInitialPso()
        {
            return _drawPso;
        }

        public override void Render(GraphicsContext recorder)
        {
            var renderTarget = _device.RenderTarget;
            var renderTargetView = _device.RenderTargetView;
            var depthStencilView = _device.DepthStencilView;

            recorder.SetGraphicsRootSignature(_rootSig);

            recorder.ResourceTransition(renderTarget, ResourceState.RenderTarget);

            recorder.SetRenderTarget(renderTargetView.CpuHandle, 1, depthStencilView.CpuHandle);

            recorder.ClearRenderTarget(renderTargetView, RgbaColor.Blue);
            recorder.ClearDepth(depthStencilView);

            recorder.SetVertexBuffers<Vertex>(_vertexBuffer);
            recorder.SetIndexBuffer<ushort>(_indexBuffer);

            recorder.SetGraphicsConstantBufferDescriptor(0, _obj);
            recorder.SetGraphicsConstantBufferDescriptor(1, _frame);
            recorder.SetGraphicsConstantBufferDescriptor(2, _light);

            recorder.SetTopology(Topology.TriangeList);
            recorder.DrawIndexed((uint)_object.Indices.Length);

            recorder.ResourceTransition(renderTarget, ResourceState.Present);
        }

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
