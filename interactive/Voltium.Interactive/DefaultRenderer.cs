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
using System;
using Voltium.Core.Memory.GpuResources.ResourceViews;
using System.Runtime.InteropServices;

namespace Voltium.Interactive
{
    [StructLayout(LayoutKind.Sequential, Size = 256)]
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
        private GpuAllocator _allocator = null!;
        private VertexBuffer<Vertex> _vertexBuffer;
        private IndexBuffer<ushort> _indexBuffer;

        public override void Init(GraphicalConfiguration config, in ScreenData screen, ID3D12Device* device)
        {
            _allocator = DeviceManager.Allocator;

            var cube = GemeotryGenerator.CreateCube(0.5f);

            _vertexBuffer = _allocator.AllocateVertexBuffer(cube.Vertices, GpuMemoryType.CpuUpload, GpuAllocFlags.ForceAllocateComitted);
            _indexBuffer = _allocator.AllocateIndexBuffer(cube.Indices, GpuMemoryType.CpuUpload, GpuAllocFlags.ForceAllocateComitted);

            _rootSig = RootSignature.Create(device, new[] { RootParameter.CreateDescriptor(RootParameterType.ConstantBufferView, 0, 0) }, default);

            var compilationFlags = new[] { DxcCompileFlags.DisableOptimizations, DxcCompileFlags.DefineMacro("COLOR(x)", "x.color") };

            var vertexShader = ShaderManager.CompileShader("Shaders/SimpleVertexShader.hlsl", DxcCompileTarget.Vs_6_0, compilationFlags);
            var pixelShader = ShaderManager.CompileShader("Shaders/SimplePixelShader.hlsl", DxcCompileTarget.Ps_6_0, compilationFlags);

            GraphicsPipelineDesc psoDesc = new(_rootSig, config.BackBufferFormat, config.DepthStencilFormat, vertexShader, pixelShader);
            DrawPso = PipelineManager.CreatePso<Vertex>("Default", psoDesc);

            _objectConstants = _allocator.AllocateConstantBuffer<ObjectConstants>(1, GpuMemoryType.CpuUpload, GpuAllocFlags.ForceAllocateComitted);

            _objectConstants.Map();

            SetHueDegrees(MathF.PI / 200);
        }

        public override void Resize(ScreenData screen)
        {
            var aspectRatio = (float)screen.Width / screen.Height;
            var fovAngleY = 70.0f * MathF.PI / 180.0f;
            _constants = new ObjectConstants
            {
                World = Matrix4x4.Identity,
                View = Matrix4x4.CreateLookAt(new Vector3(0.0f, 0.7f, 1.5f), new Vector3(0.0f, -0.1f, 0.0f), new Vector3(0.0f, 1.0f, 0.0f)),
                Projection = Matrix4x4.CreatePerspectiveFieldOfView(fovAngleY, aspectRatio, 0.001f, 100f)
            };
        }

        private RgbaColor _color = new RgbaColor(1, 0, 0, 1);
        private RootSignature _rootSig = null!;
        private PipelineStateObject DrawPso = null!;
        private ConstantBuffer<ObjectConstants> _objectConstants;
        private ObjectConstants _constants;
        private Matrix4x4 _perFrameRotation = Matrix4x4.CreateRotationY(0.001f) * Matrix4x4.CreateRotationX(0.001f);
        //private int _totalCount = 0;

        public override void Update(ApplicationTimer timer)
        {
            var total = timer.TotalSeconds;

            // rotate a small amount each frame
            _constants.World *= _perFrameRotation;

            // scale between 0 and 5 seconds
            var scale = Matrix4x4.CreateScale((float)(Math.Abs((total % 10) - 5)) / 5);

            _objectConstants.Buffers[0].World = Matrix4x4.Transpose(_constants.World * scale);
            _objectConstants.Buffers[0].View = Matrix4x4.Transpose(_constants.View);
            _objectConstants.Buffers[0].Projection = Matrix4x4.Transpose(_constants.Projection);

            _color = ChangeHue(_color);
        }

        public override PipelineStateObject GetInitialPso()
        {
            return DrawPso;
        }

        public override void Render(GraphicsContext recorder)
        {
            var renderTarget = DeviceManager.RenderTarget;
            var renderTargetView = DeviceManager.RenderTargetView;
            var depthStencilView = DeviceManager.DepthStencilView;

            recorder.SetGraphicsRootSignature(_rootSig);

            recorder.ResourceTransition(renderTarget, D3D12_RESOURCE_STATE_RENDER_TARGET);

            recorder.SetRenderTarget(renderTargetView.CpuHandle, 1, depthStencilView.CpuHandle);

            recorder.ClearRenderTarget(renderTargetView, _color);
            recorder.ClearDepth(depthStencilView);

            recorder.SetVertexBuffers(_vertexBuffer);
            recorder.SetIndexBuffer(_indexBuffer);

            recorder.SetGraphicsConstantBufferDescriptor(0, _objectConstants, 0);

            recorder.SetPrimitiveTopology(D3D_PRIMITIVE_TOPOLOGY.D3D10_PRIMITIVE_TOPOLOGY_TRIANGLELIST);
            recorder.DrawIndexed(36);

            recorder.ResourceTransition(renderTarget, D3D12_RESOURCE_STATE_PRESENT);
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
            _objectConstants.Unmap();
            _rootSig.Dispose();
            DeviceManager.Dispose();
        }
    }
}
