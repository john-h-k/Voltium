using System.Diagnostics;
using System.Drawing;
using System.Resources;
using TerraFX.Interop;
using Voltium.Common;
using Voltium.Core;
using Voltium.Core.Managers;
using static TerraFX.Interop.D3D12_RESOURCE_STATES;
using static TerraFX.Interop.DXGI_FORMAT;
using static TerraFX.Interop.Windows;
using static TerraFX.Interop.D3D12_PRIMITIVE_TOPOLOGY_TYPE;
using static TerraFX.Interop.D3D12_ROOT_SIGNATURE_FLAGS;
using static TerraFX.Interop.D3D12_INPUT_CLASSIFICATION;
using ResourceManager = Voltium.Core.Managers.ResourceManager;
using System.Numerics;
using Voltium.Core.GpuResources;
using System.Runtime.CompilerServices;
using Voltium.Core.Managers.Shaders;
using System;

namespace Voltium.Interactive
{
    public partial struct ObjectConstants
    {
        public Matrix4x4 World;
        public Matrix4x4 View;
        public Matrix4x4 Projection;
    }

    [ShaderInput]
    public partial struct Vertex
    {
        public Vertex(Vector3 position, Vector4 color)
        {
            Position = position;
            Color = color;
        }

        public Vector3 Position;
        public Vector4 Color;
    }

    public unsafe class DefaultRenderer : Renderer
    {
        private PipelineManager _pipelineManager = null!;
        private GpuAllocator _allocator = null!;
        private VertexBuffer<Vertex> _vertexBuffer;
        private IndexBuffer<ushort> _indexBuffer;

        public override void Init(GraphicalConfiguration config, in ScreenData screen, ID3D12Device* device)
        {
            var aspectRatio = (float)screen.Width / screen.Height;
            var fovAngleY = 70.0f * MathF.PI / 180.0f;
            _constants = new ObjectConstants
            {
                World = Matrix4x4.Transpose(Matrix4x4.Identity),
                View = Matrix4x4.Transpose(Matrix4x4.CreateLookAt(new Vector3(0.0f, 0.7f, 1.5f), new Vector3(0.0f, -0.1f, 0.0f), new Vector3(0.0f, 1.0f, 0.0f))),
                Projection = Matrix4x4.Transpose(Matrix4x4.CreatePerspectiveFieldOfView(fovAngleY, aspectRatio, 0.001f, 100f))
            };

            _pipelineManager = new PipelineManager(ComPtr<ID3D12Device>.CopyFromPointer(device));
            _allocator = new GpuAllocator(ComPtr<ID3D12Device>.CopyFromPointer(device));

            var verticesDesc = new GpuResourceDesc(
                GpuResourceFormat.Buffer((ulong)sizeof(Vertex) * 3),
                GpuMemoryType.CpuWriteOptimized,
                D3D12_RESOURCE_STATE_GENERIC_READ,
                GpuAllocFlags.ForceAllocateComitted
            );

            var cubeVertices = stackalloc Vertex[8]
            {
                new Vertex(new Vector3(-0.5f, -0.5f, -0.5f), new Vector4(0.0f, 0.0f, 0.0f, 1.0f)),
                new Vertex(new Vector3(-0.5f, -0.5f, 0.5f), new Vector4(0.0f, 0.0f, 1.0f, 1.0f)),
                new Vertex(new Vector3(-0.5f, 0.5f, -0.5f), new Vector4(0.0f, 1.0f, 0.0f, 1.0f)),
                new Vertex(new Vector3(-0.5f, 0.5f, 0.5f), new Vector4(0.0f, 1.0f, 1.0f, 1.0f)),
                new Vertex(new Vector3(0.5f, -0.5f, -0.5f), new Vector4(1.0f, 0.0f, 0.0f, 1.0f)),
                new Vertex(new Vector3(0.5f, -0.5f, 0.5f), new Vector4(1.0f, 0.0f, 1.0f, 1.0f)),
                new Vertex(new Vector3(0.5f, 0.5f, -0.5f), new Vector4(1.0f, 1.0f, 0.0f, 1.0f)),
                new Vertex(new Vector3(0.5f, 0.5f, 0.5f), new Vector4(1.0f, 1.0f, 1.0f, 1.0f))
            };

            var cubeIndices = stackalloc ushort[36]
            {
                0,
                2,
                1, // -x
                1,
                2,
                3,

                4,
                5,
                6, // +x
                5,
                7,
                6,

                0,
                1,
                5, // -y
                0,
                5,
                4,

                2,
                6,
                7, // +y
                2,
                7,
                3,

                0,
                4,
                6, // -z
                0,
                6,
                2,

                1,
                3,
                7, // +z
                1,
                7,
                5,
            };

            var vertices = _allocator.AllocateVertexBuffer<Vertex>(8, GpuMemoryType.CpuWriteOptimized, GpuAllocFlags.ForceAllocateComitted);
            var indices = _allocator.AllocateIndexBuffer<ushort>(36, GpuMemoryType.CpuWriteOptimized, GpuAllocFlags.ForceAllocateComitted);

            vertices.Resource.Map(0);
            Unsafe.CopyBlock(vertices.Resource.CpuAddress, cubeVertices, (uint)sizeof(Vertex) * 8);
            vertices.Resource.Unmap(0);

            indices.Resource.Map(0);
            Unsafe.CopyBlock(indices.Resource.CpuAddress, cubeIndices, (uint)sizeof(ushort) * 36);
            indices.Resource.Unmap(0);

            _vertexBuffer = vertices;
            _indexBuffer = indices;

            var objectConstants = RootParameter.CreateDescriptor(RootParameterType.ConstantBufferView, 0, 0);
            _rootSig = RootSignature.Create(device, new[] { objectConstants }, default);

            var semanticName0 = stackalloc ulong[2] {
                0x4E4F495449534F50,     // POSITION
                0x0000000000000000,
            };

            var semanticName1 = stackalloc ulong[1] {
                0x000000524F4C4F43,     // COLOR
            };

            var inputElementDescs = stackalloc D3D12_INPUT_ELEMENT_DESC[2] {
                new D3D12_INPUT_ELEMENT_DESC
                {
                    SemanticName = (sbyte*)semanticName0,
                    Format = DXGI_FORMAT_R32G32B32_FLOAT,
                    InputSlotClass = D3D12_INPUT_CLASSIFICATION_PER_VERTEX_DATA,
                },
                new D3D12_INPUT_ELEMENT_DESC
                {
                    SemanticName = (sbyte*)semanticName1,
                    Format = DXGI_FORMAT_R32G32B32A32_FLOAT,
                    AlignedByteOffset = 12,
                    InputSlotClass = D3D12_INPUT_CLASSIFICATION_PER_VERTEX_DATA,
                },
            };

            var vertexShader = ShaderManager.ReadCompiledShader("SimpleVertexShader.cso", ShaderType.Vertex);
            var pixelShader = ShaderManager.ReadCompiledShader("SimplePixelShader.cso", ShaderType.Pixel);

            D3D12_GRAPHICS_PIPELINE_STATE_DESC psoDesc;

            fixed (byte* vs = vertexShader)
            fixed (byte* ps = pixelShader)
            {
                psoDesc = new()
                {
                    InputLayout = new D3D12_INPUT_LAYOUT_DESC
                    {
                        pInputElementDescs = inputElementDescs,
                        NumElements = 2,
                    },
                    pRootSignature = _rootSig.Value,
                    VS = new D3D12_SHADER_BYTECODE(vs, (nuint)vertexShader.Length),
                    PS = new D3D12_SHADER_BYTECODE(ps, (nuint)pixelShader.Length),
                    RasterizerState = D3D12_RASTERIZER_DESC.DEFAULT,
                    BlendState = D3D12_BLEND_DESC.DEFAULT,
                    DepthStencilState = D3D12_DEPTH_STENCIL_DESC.DEFAULT,
                    DSVFormat = config.DepthStencilFormat,
                    SampleMask = uint.MaxValue,
                    PrimitiveTopologyType = D3D12_PRIMITIVE_TOPOLOGY_TYPE_TRIANGLE,
                    NumRenderTargets = 1,
                    SampleDesc = new DXGI_SAMPLE_DESC(count: 1, quality: 0),
                };
                psoDesc.RTVFormats[0] = config.BackBufferFormat;
            }

            _pipelineManager.CreatePso("Default", psoDesc);
            _cachedPso = _pipelineManager.RetrievePso("Default").Move();

            _objectConstants = _allocator.Allocate(
                new GpuResourceDesc(GpuResourceFormat.Buffer((ulong)sizeof(ObjectConstants)),
                GpuMemoryType.CpuWriteOptimized,
                D3D12_RESOURCE_STATE_GENERIC_READ,
                GpuAllocFlags.ForceAllocateComitted)
            );

            _objectConstants.Map(0);

            SetHueDegrees(MathF.PI / 200);
        }

        private RgbaColor Color = new RgbaColor(1, 0, 0, 1);
        private RootSignature _rootSig;
        private ComPtr<ID3D12PipelineState> _cachedPso;
        private GpuResource _objectConstants = null!;
        private ObjectConstants _constants;
        private Matrix4x4 _perFrameRotation = Matrix4x4.CreateRotationY(0.001f) * Matrix4x4.CreateRotationX(0.001f);

        public override void Update()
        {
            _constants.World *= _perFrameRotation;
            Unsafe.Write(_objectConstants.CpuAddress, _constants);

            Color = ChangeHue(Color);
        }

        public override void Render(GraphicsContext recorder)
        {
            var renderTarget = ResourceManager.Manager.RenderTarget;
            var renderTargetView = ResourceManager.Manager.RenderTargetView;
            var depthStencilView = ResourceManager.Manager.DepthStencilView;

            recorder.SetGraphicsRootSignature(_rootSig);

            recorder.ResourceTransition(renderTarget, D3D12_RESOURCE_STATE_RENDER_TARGET);

            recorder.SetRenderTarget(renderTargetView.CpuHandle, 1, depthStencilView.CpuHandle);

            recorder.ClearRenderTarget(renderTargetView, Color);
            recorder.ClearDepth(depthStencilView);

            recorder.SetVertexBuffers(_vertexBuffer);
            recorder.SetIndexBuffer(_indexBuffer);

            recorder.SetGraphicsConstantBufferDescriptor(0, _objectConstants);

            recorder.DrawIndexed(36);

            recorder.ResourceTransition(renderTarget, D3D12_RESOURCE_STATE_PRESENT);
        }

        private static float IncrementColor(float val) => val > 0.999f ? 0f : val + 0.005f;

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
            _objectConstants.Unmap(0);
            _rootSig.Dispose();
            DeviceManager.Manager.Dispose();
        }

        public override ComPtr<ID3D12PipelineState> GetInitialPso()
        {
            return _cachedPso.Copy();
        }
    }
}
