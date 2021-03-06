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
using Voltium.Core.CommandBuffer;
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
        private QuerySet _occlusionQueries;
        private PipelineStateObject _drawPso, _predicatePso;
        private Texture _depth;
        private ViewSet _views;
        private View _depthView;

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

            _device = GraphicsDevice.Create(new D3D12NativeDevice(D3D_FEATURE_LEVEL.D3D_FEATURE_LEVEL_11_1));
            _output = Output.Create(
                new DXGINativeOutput(
                    _device.GraphicsQueue.Native,
                    new NativeOutputDesc
                    {
                        Format = BackBufferFormat.B8G8R8A8UnsignedNormalized,
                        BackBufferCount = 3,
                        PreserveBackBuffers = false,
                        VrStereo = false
                    },
                    output.GetOutput()
                )
            );

            var (width, height) = ((uint)outputSize.Width, (uint)outputSize.Height);
            _depth = _device.Allocator.AllocateTexture(TextureDesc.CreateDepthStencilDesc(DataFormat.Depth32Single, width, height, 1, 0), ResourceState.DepthWrite);
            _views = _device.CreateViewSet(1);
            _depthView = _device.CreateDefaultView(_views, 0, _depth);

            _occlusionQueries = _device.CreateQuerySet(QuerySetType.Occlusion, 1);

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

            var flags = new ShaderCompileFlag[]
            {
                ShaderCompileFlag.EnableDebugInformation,
                ShaderCompileFlag.WriteDebugInformationToFile()
            };

            var vertexShader = ShaderManager.CompileShader("HelloTriangle/Shader.hlsl", ShaderType.Vertex, flags, entrypoint: "VertexMain");
            var pixelShader = ShaderManager.CompileShader("HelloTriangle/Shader.hlsl", ShaderType.Pixel, flags, entrypoint: "PixelMain");

            RootSignature empty = _device.CreateRootSignature(default, default, RootSignatureFlags.AllowInputAssembler);

            var predicationPsoDesc = new GraphicsPipelineDesc
            {
                Topology = Topology.TriangleList,
                VertexShader = vertexShader,
                PixelShader = CompiledShader.Empty,
                DepthStencilFormat = DataFormat.Depth32Single,
                DepthStencil = DepthStencilDesc.Default.WithDepthWriteMask(DepthWriteMask.Zero),
                Inputs = InputLayout.FromType<Vertex>(),
                RenderTargetFormats = DataFormat.Unknown,
                Rasterizer = RasterizerDesc.Default
            };

            _predicatePso = _device.CreatePipelineStateObject(empty, predicationPsoDesc);

            var drawPsoDesc = new GraphicsPipelineDesc
            {
                Topology = Topology.TriangleList,
                VertexShader = vertexShader,
                PixelShader = pixelShader,
                DepthStencilFormat = DataFormat.Depth32Single,
                RenderTargetFormats = _output.Format,
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

            _drawPso = _device.CreatePipelineStateObject(empty, drawPsoDesc);
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


        private GraphicsContext _context = new(true);
        public unsafe override void Render()
        {
            var context = _context;
            context.Reset();

            var renderTarget = new RenderTarget
            {
                Load = LoadOperation.Clear,
                Store = StoreOperation.Preserve,
                ColorClear = Rgba128.CornflowerBlue,
                Resource = _output.OutputBufferView
            };

            var depthStencil = new DepthStencil
            {
                DepthClear = 1,
                DepthLoad = LoadOperation.Clear,
                DepthStore = StoreOperation.Preserve,
                Resource = _depthView
            };

            using (context.ScopedBarrier(ResourceTransition.Create(_output.OutputBuffer, ResourceState.Present, ResourceState.RenderTarget)))
            {
                {
                    context.BeginRenderPass(renderTarget, depthStencil);

                    context.SetPipelineState(_drawPso);

                    context.SetTopology(Topology.TriangleList);
                    context.SetVertexBuffers<Vertex>(_static);
                    context.Draw((int)_static.LengthAs<Vertex>());

                    // Perform a fake draw of the moving triangle. There is no pixel shader, so no output to a render target
                    // And DepthWriteMask is set to DepthWriteMask.Zero
                    context.SetPipelineState(_predicatePso);
                    context.SetVertexBuffers<Vertex>(_moving);

                    // Perform the query, which returns 1 if the object would have been drawn at all, else 0
                    using (context.ScopedQuery<TQuery>(_occlusionQueries, 0))
                    {
                        context.Draw((int)_moving.LengthAs<Vertex>());
                    }

                    context.EndRenderPass();
                }

                // Copy the query into a buffer to use
                using (context.ScopedBarrier(ResourceTransition.Create(_predicationBuffer, ResourceState.Predication | ResourceState.CopySource, ResourceState.CopyDestination)))
                {
                    context.ResolveQuery<TQuery>(_occlusionQueries, 0..1, _predicationBuffer);
                }
                context.CopyBufferRegion(_predicationBuffer, _readbackBuffer, sizeof(TQuery));


                // Set the predicate so that operations are only performed if BinaryOcclusionQuery is false, which means that no
                // samples would be drawn
                // this means the entire triangle is drawn until it is *entirely* occluded.
                context.BeginConditionalRendering(false, _predicationBuffer);

                {
                    // Need old back buffer contents
                    renderTarget = new RenderTarget
                    {
                        Load = LoadOperation.Preserve,
                        Store = StoreOperation.Preserve,
                        Resource = _output.OutputBufferView
                    };

                    context.BeginRenderPass(renderTarget, depthStencil);

                    // Actually draw the object
                    context.SetPipelineState(_drawPso);

                    context.Draw((int)_moving.LengthAs<Vertex>());
                    context.EndRenderPass();
                }
            }

            context.Close();

            _device.GraphicsQueue.Execute(context).Block();
            _lastQuery = *_readbackBuffer.As<TQuery>();

            _output.Present();
        }

        public override void Dispose()
        {
            _device.Dispose();
        }
    }
}
