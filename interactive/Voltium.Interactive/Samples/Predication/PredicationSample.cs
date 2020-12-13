using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TerraFX.Interop;
using Voltium.Common;
using Voltium.Common.Pix;
using Voltium.Core;
using Voltium.Core.Contexts;
using Voltium.Core.Devices;
using Voltium.Core.Devices.Shaders;
using Voltium.Core.Infrastructure;
using Voltium.Core.Memory;
using Voltium.Core.Pipeline;
using Voltium.Core.Queries;
using Buffer = Voltium.Core.Memory.Buffer;

namespace Voltium.Interactive.Samples.Predication
{
    using TQuery = OcclusionQuery;

    [ShaderInput]
    internal partial struct Vertex
    {
        public Vector3 Position;
        public Vector4 Color;
    }

    internal sealed class PredicationSample : Application
    {
        private GraphicsDevice _device = null!;
        private Output _output = null!;
        private Buffer _static, _moving, _predicationBuffer, _readbackBuffer;
        private QueryHeap _occlusionQueries;
        private GraphicsPipelineStateObject _drawPso = null!, _predicatePso = null!;
        private Texture _depth;
        private DescriptorHeap _views = null!;
        private DescriptorAllocation _depthView;

        public unsafe override void Initialize(Size outputSize, IOutputOwner output)
        {
            var debug = new DebugLayerConfiguration()
#if DEBUG
                .AddDebugFlags(DebugFlags.DebugLayer)
                .AddDredFlags(DredFlags.All)
                .WithBreakpointLogLevel(LogLevel.None)
                .AddDebugFlags(DebugFlags.GpuBasedValidation)
#endif
            ;


            using (var factory = DeviceFactory.Create())
            {
                foreach (var adapter in factory)
                {
                    Console.WriteLine(adapter);
                }
            }

            _device = GraphicsDevice.Create(FeatureLevel.GraphicsLevel11_0, null, debug);
            _output = Output.Create(OutputConfiguration.Default, _device, output);

            var (width, height) = ((uint)outputSize.Width, (uint)outputSize.Height);
            _depth = _device.Allocator.AllocateTexture(TextureDesc.CreateDepthStencilDesc(DataFormat.Depth32Single, width, height, 1, 0), ResourceState.DepthWrite);
            _views = _device.CreateDescriptorHeap(DescriptorHeapType.DepthStencilView, 1);
            _depthView = _views.AllocateHandle();
            _device.CreateDepthStencilView(_depth, _depthView.Span[0]);

            _occlusionQueries = _device.CreateQueryHeap(QueryHeapType.Occlusion, 1);

            _predicationBuffer = _device.Allocator.AllocateDefaultBuffer<TQuery>();
            _readbackBuffer = _device.Allocator.AllocateReadbackBuffer<TQuery>();

            OnResize(outputSize);

            ReadOnlySpan<Vertex> staticVertices = stackalloc Vertex[]
            {
                new Vertex { Position = new Vector3(+0.5f, -0.5f, +0.0f), Color = (Vector4)Rgba128.Red.WithA(0.5f) },
                new Vertex { Position = new Vector3(-0.5f, -0.5f, +0.0f), Color = (Vector4)Rgba128.Red.WithA(0.5f) },
                new Vertex { Position = new Vector3(+0.0f, +0.5f, +0.0f), Color = (Vector4)Rgba128.Red.WithA(0.5f) },
            };


            ReadOnlySpan<Vertex> movingVertices = stackalloc Vertex[]
            {
                new Vertex { Position = new Vector3(+0.25f, -0.25f, +0.5f), Color = (Vector4)Rgba128.Blue },
                new Vertex { Position = new Vector3(-0.25f, -0.25f, +0.5f), Color = (Vector4)Rgba128.Blue },
                new Vertex { Position = new Vector3(+0.0f, +0.25f, +0.5f), Color = (Vector4)Rgba128.Blue },
            };

            _static = _device.Allocator.AllocateUploadBuffer(staticVertices);
            _moving = _device.Allocator.AllocateUploadBuffer(movingVertices);

            var vertexShader = ShaderManager.CompileShader("HelloTriangle/Shader.hlsl", ShaderType.Vertex, entrypoint: "VertexMain");
            var pixelShader = ShaderManager.CompileShader("HelloTriangle/Shader.hlsl", ShaderType.Pixel, entrypoint: "PixelMain");

            var predicationPsoDesc = new GraphicsPipelineDesc
            {
                Topology = TopologyClass.Triangle,
                VertexShader = vertexShader,
                PixelShader = CompiledShader.Empty,
                DepthStencilFormat = DataFormat.Depth32Single,
                DepthStencil = DepthStencilDesc.Default.WithDepthWriteMask(DepthWriteMask.Zero),
                Inputs = InputLayout.FromType<Vertex>(),
                RenderTargetFormats = DataFormat.Unknown
            };

            _predicatePso = _device.PipelineManager.CreatePipelineStateObject(predicationPsoDesc, nameof(_predicatePso));

            var drawPsoDesc = new GraphicsPipelineDesc
            {
                Topology = TopologyClass.Triangle,
                VertexShader = vertexShader,
                PixelShader = pixelShader,
                DepthStencilFormat = DataFormat.Depth32Single,
                RenderTargetFormats = _output.Configuration.BackBufferFormat,
                Inputs = InputLayout.FromType<Vertex>(),
                Blend = new BlendDesc
                {
                    [0] = new RenderTargetBlendDesc
                    {
                        EnableBlendOp = true,
                        SrcBlend = BlendFactor.SourceAlpha,
                        DestBlend = BlendFactor.InvertedSourceAlpha,
                        BlendOp = BlendFunc.Add,
                        SrcBlendAlpha = BlendFactor.One,
                        DestBlendAlpha = BlendFactor.Zero,
                        AlphaBlendOp = BlendFunc.Add,
                        RenderTargetWriteMask = ColorWriteFlags.All
                    }
                }
            };

            _drawPso = _device.PipelineManager.CreatePipelineStateObject(drawPsoDesc, nameof(_drawPso));
        }


        public override void OnResize(Size newOutputSize) => _output.Resize(newOutputSize);

        private TQuery _lastQuery;
        private double _lastQueryPrint;
        public override void Update(ApplicationTimer timer)
        {
            if (timer.TotalSeconds - _lastQueryPrint >= 0.25)
            {
                Console.WriteLine(_lastQuery);
                _lastQueryPrint = timer.TotalSeconds;
            }

            var span = _moving.AsSpan<Vertex>();

            foreach (ref var vertex in span)
            {
                if (vertex.Position.X < 1)
                {
                    vertex.Position.X += (float)timer.ElapsedSeconds / 5;
                }
                else
                {
                    vertex.Position.X -= 2;
                }
            }
        }

        public unsafe override void Render()
        {
            var context = _device.BeginGraphicsContext(_drawPso);

            using (context.ScopedEvent(Argb32.AliceBlue, nameof(Render)))
            using (context.ScopedBarrier(ResourceBarrier.Transition(_output.OutputBuffer, ResourceState.Present, ResourceState.RenderTarget)))
            {
                context.SetViewportAndScissor(_output.Resolution);
                context.SetTopology(Topology.TriangleList);
                context.SetAndClearRenderTarget(_output.OutputBufferView, Rgba128.CornflowerBlue, _depthView.Span[0]);
                context.SetVertexBuffers<Vertex>(_static);
                context.Draw(_static.LengthAs<Vertex>());

                // Perform a fake draw of the moving triangle. There is no pixel shader, so no output to a render target
                // And DepthWriteMask is set to DepthWriteMask.Zero
                context.SetPipelineState(_predicatePso);
                context.SetVertexBuffers<Vertex>(_moving);

                // Perform the query, which returns 1 if the object would have been drawn at all, else 0
                using (context.ScopedQuery<TQuery>(_occlusionQueries, 0))
                {
                    context.Draw(_moving.LengthAs<Vertex>());
                }

                // Copy the query into a buffer to use
                using (context.ScopedBarrier(ResourceBarrier.Transition(_predicationBuffer, ResourceState.Predication | ResourceState.CopySource, ResourceState.CopyDestination)))
                {
                    context.ResolveQuery<TQuery>(_occlusionQueries, 0..1, _predicationBuffer);
                }

                context.CopyBufferRegion(_predicationBuffer, _readbackBuffer, sizeof(TQuery));

                // Actually draw the object
                context.SetPipelineState(_drawPso);

                // Set the predicate so that operations are only performed if BinaryOcclusionQuery is false, which means that no
                // samples would be drawn
                // this means the entire triangle is drawn until it is *entirely* occluded.
                context.SetPredication(false, _predicationBuffer);

                // Clear the depth because we want to draw the entire triangle unless it is fully occluded
                // Else we don't really see predication and we just see the depth buffer at work
                context.ClearDepth(_depthView.Span[0]);
                context.Draw(_moving.LengthAs<Vertex>());
            }

            context.Close();

            _device.Execute(context).Block();
            _lastQuery = *_readbackBuffer.As<TQuery>();

            _output.Present();
        }

        public override void Dispose()
        {
            _device.Dispose();
        }
    }
}
