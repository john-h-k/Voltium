using System;
using System.Drawing;
using System.Numerics;
using Voltium.Core;
using Voltium.Core.Devices;
using Voltium.Core.Devices.Shaders;
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
        private Output2D _output = null!;
        private GraphicsPipelineStateObject _pso = null!;
        private Buffer _vertices;

        public override string Name => nameof(HelloTriangleApp);

        public unsafe override void Initialize(Size outputSize, IOutputOwner output)
        {
            _device = new GraphicsDevice(DeviceConfiguration.Default);
            _output = Output2D.Create(OutputConfiguration.Default, _device, output);
            OnResize(outputSize);

            ReadOnlySpan<HelloWorldVertex> vertices = stackalloc HelloWorldVertex[3]
            {
                new HelloWorldVertex { Position = new Vector3(+0.0f, +0.25f, +0.0f), Color = (Vector4)Rgba128.Red },
                new HelloWorldVertex { Position = new Vector3(-0.25f, -0.25f, +0.0f), Color = (Vector4)Rgba128.Green },
                new HelloWorldVertex { Position = new Vector3(+0.25f, -0.25f, +0.0f), Color = (Vector4)Rgba128.Blue },
            };

            _vertices = _device.Allocator.AllocateBuffer(vertices);

            var psoDesc = new GraphicsPipelineDesc
            {
                RenderTargetFormats = _output.Configuration.BackBufferFormat,
                Topology = TopologyClass.Triangle,
                VertexShader = ShaderManager.CompileShader("HelloTriangle/Shader.hlsl", ShaderType.Vertex, entrypoint: "VertexMain"),
                PixelShader = ShaderManager.CompileShader("HelloTriangle/Shader.hlsl", ShaderType.Pixel, entrypoint: "PixelMain")
            };

            _pso = _device.PipelineManager.CreatePipelineStateObject<HelloWorldVertex>("Draw", psoDesc);
        }

        public override void OnResize(Size newOutputSize) => _output.Resize(newOutputSize); 

        public override void Update(ApplicationTimer timer) { /* This app doesn't do any updating */ }
        public override void Render()
        {
            var context = _device.BeginGraphicsContext(_pso);

            context.SetViewportAndScissor(_output.Dimensions);
            context.ResourceTransition(_output.OutputBuffer, ResourceState.RenderTarget);
            context.SetAndClearRenderTarget(_output.OutputBufferView, Rgba128.CornflowerBlue);
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
