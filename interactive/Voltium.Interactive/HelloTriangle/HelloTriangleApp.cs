using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Voltium.Core;
using Voltium.Core.Devices;
using Voltium.Core.Devices.Shaders;
using Voltium.Core.Memory;
using Voltium.Core.Pipeline;
using Buffer = Voltium.Core.Memory.Buffer;

namespace Voltium.Interactive.HelloTriangle
{
    [ShaderInput]
    internal partial struct HelloWorldVertex
    {
        public Vector3 Position;
        public Vector4 Color;
    }

    public sealed class HelloTriangleApp : Application
    {
        private GraphicsDevice _device = null!;
        private TextureOutput _output = null!;
        private GraphicsPipelineStateObject _pso = null!;
        private Buffer _vertices;
        private DescriptorHandle[] _rtvs = null!;
        private Size _viewSize;

        public override string Name => nameof(HelloTriangleApp);

        public override void Initialize(Size outputSize, IOutputOwner output)
        {
            _device = new GraphicsDevice(DeviceConfiguration.Default);
            _output = TextureOutput.Create(_device, OutputConfiguration.Default, output);
            _viewSize = outputSize;

            _rtvs = new DescriptorHandle[_output.OutputBufferCount];
            for (uint i = 0; i < _output.OutputBufferCount; i++)
            {
                _rtvs[i] = _device.CreateRenderTargetView(_output.GetOutputBuffer(i));
            }

            ReadOnlySpan<HelloWorldVertex> vertices = stackalloc HelloWorldVertex[3]
            {
                new HelloWorldVertex { Position = new Vector3(+0.0f, +0.25f, +0.0f) * 0.5f, Color = (Vector4)Rgba128.Red },
                new HelloWorldVertex { Position = new Vector3(-0.25f, -0.25f, +0.0f) * 0.5f, Color = (Vector4)Rgba128.Green },
                new HelloWorldVertex { Position = new Vector3(+0.25f, -0.25f, +0.0f) * 0.5f, Color = (Vector4)Rgba128.Blue },
            };

            _vertices = _device.Allocator.AllocateBuffer(BufferDesc.Create<HelloWorldVertex>(vertices.Length), MemoryAccess.CpuUpload);
            _vertices.WriteData(vertices);

            var psoDesc = new GraphicsPipelineDesc
            {
                RootSignature = _device.EmptyRootSignature,
                RenderTargetFormats = _output.Configuration.BackBufferFormat,
                VertexShader = ShaderManager.CompileShader("HelloTriangle/Shader.hlsl", ShaderType.Vertex, entrypoint: "VertexMain"),
                PixelShader = ShaderManager.CompileShader("HelloTriangle/Shader.hlsl", ShaderType.Pixel, entrypoint: "PixelMain"),
                Topology = TopologyClass.Triangle
            };

            _pso = _device.PipelineManager.CreatePipelineStateObject<HelloWorldVertex>("Draw", psoDesc);
        }

        public override void OnResize(Size newOutputSize) { }

        public override void Update(ApplicationTimer timer) { }
        public override void Render()
        {
            var context = _device.BeginGraphicsContext(_pso);

            context.SetViewportAndScissor(_viewSize);
            context.ResourceTransition(_output.OutputBuffer, ResourceState.RenderTarget);
            context.SetAndClearRenderTarget(_rtvs[_output.CurrentOutputBufferIndex], Rgba128.CornflowerBlue);
            context.SetTopology(Topology.TriangeList);
            context.SetVertexBuffers<HelloWorldVertex>(_vertices);
            context.Draw(3);
            context.ResourceTransition(_output.OutputBuffer, ResourceState.Present);

            context.Close();

            _device.Execute(context).Block();

            _output.Present();
        }

        public override void Dispose()
        {
            _pso.Dispose();
            _vertices.Dispose();
            _output.Dispose();
            _device.Dispose();
        }
    }
}
