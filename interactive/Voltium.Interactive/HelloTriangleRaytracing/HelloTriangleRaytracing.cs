
//using System;
//using System.Diagnostics;
//using System.Drawing;
//using System.Numerics;
//using System.Runtime.CompilerServices;
//using System.Runtime.InteropServices;
//using Microsoft.Extensions.Logging;
//using TerraFX.Interop;
//using Voltium.Common;
//using Voltium.Core;
//using Voltium.Core.Contexts;
//using Voltium.Core.Devices;
//using Voltium.Core.Devices.Shaders;
//using Voltium.Core.Pipeline;
//using Voltium.Core.Memory;
//using Buffer = Voltium.Core.Memory.Buffer;

//namespace Voltium.Interactive.HelloTriangleRaytracing
//{
//    // This is our vertex type used in the shader
//    // [ShaderInput] triggers a source generator to create a shader input description which we need later
//    [ShaderInput]
//    internal partial struct HelloWorldVertex
//    {
//        public Vector3 Position;
//        public Vector4 Color;
//    }

//    public sealed class HelloTriangleApp : Application
//    {
//        private GraphicsDevice _device = null!;
//        private Output _output = null!;
//        private PipelineStateObject _pso = null!;
//        private RootSignature _rootSig = null!;
//        private Buffer _tlas;
//        private Texture _renderTarget;

//        public override string Name => nameof(HelloTriangleApp);

//        public unsafe override void Initialize(Size outputSize, IOutputOwner output)
//        {
//            // Create the device and output
//            var debug = new DebugLayerConfiguration()
//                .WithDebugFlags(DebugFlags.DebugLayer)
//                .WithDredFlags(DredFlags.All)
//                .WithBreakpointLogLevel(LogLevel.None);

//            _device = GraphicsDevice.Create(FeatureLevel.GraphicsLevel12_1, null, debug);

//            _output = Output.Create(OutputConfiguration.Default, _device, output);

//            OnResize(outputSize);

//            // The vertices for our triangle
//            ReadOnlySpan<HelloWorldVertex> vertices = stackalloc HelloWorldVertex[3]
//            {
//                new HelloWorldVertex { Position = new Vector3(+0.25f, -0.25f, +0.0f), Color = (Vector4)Rgba128.Blue },
//                new HelloWorldVertex { Position = new Vector3(-0.25f, -0.25f, +0.0f), Color = (Vector4)Rgba128.Green },
//                new HelloWorldVertex { Position = new Vector3(+0.0f, +0.25f, +0.0f), Color = (Vector4)Rgba128.Red },
//            };

//            // Allocate the vertices, using the overload which takes some initial data
//            var vertexBuffer = _device.Allocator.AllocateUploadBuffer(vertices);

//            TriangleGeometryDesc triangles = new TriangleGeometryDesc()
//            {
//                VertexBuffer = vertexBuffer,
//                VertexFormat = VertexFormat.R32G32B32Single,
//                VertexCount = (uint)vertices.Length,
//                VertexStride = (uint)sizeof(HelloWorldVertex)
//            };

//            GeometryDesc desc = new GeometryDesc()
//            {
//                Type = GeometryType.Triangles,
//                Flags = GeometryFlags.None,
//                Triangles = triangles
//            };

//            _renderTarget = _device.Allocator.AllocateTexture(
//                TextureDesc.CreateUnorderedAccessResourceDesc(
//                    (DataFormat)_output.Configuration.BackBufferFormat,
//                    TextureDimension.Tex2D,
//                    _output.OutputBuffer.Width,
//                    _output.OutputBuffer.Height
//                ),
//                ResourceState.CopySource
//            );

//            using (var context = _device.BeginComputeContext(flags: ContextFlags.BlockOnClose))
//            {
//                var blasDest = _device.Allocator.AllocateRayTracingAccelerationBuffer(_device.GetBuildInfo(desc), out var blasScratch);
//                var tlasDest = _device.Allocator.AllocateRayTracingAccelerationBuffer(_device.GetBuildInfo(Layout.Array, 1), out var tlasScratch);

//                context.BuildAccelerationStructure(desc, blasScratch, blasDest);
//                context.UavBarrier(blasDest);
//                context.BuildAccelerationStructure(blasDest, Layout.Array, 1, tlasScratch, _tlas);
//                context.UavBarrier(_tlas);
//            }
//        }

//        public override void OnResize(Size newOutputSize) => _output.Resize(newOutputSize);

//        public override void Update(ApplicationTimer timer) { /* This app doesn't do any updating */ }
//        public override void Render()
//        {
//            var context = _device.BeginGraphicsContext(_pso);

//            context.SetRootSignature(_rootSig);

//            context.ResourceBarrier(ResourceBarrier.Transition(_renderTarget, ResourceState.CopySource, ResourceState.UnorderedAccess));
//            context.SetShaderResourceBuffer(0, _tlas);

//            context.DispatchRays(new RayDispatchDesc
//            {
//                Width = (uint)_output.Resolution.Width,
//                Height = (uint)_output.Resolution.Height,
//                Depth = 1
//            });

//            context.ResourceBarrier(ResourceBarrier.Uav(_renderTarget));

//            context.ResourceBarrier(ResourceBarrier.Transition(_renderTarget, ResourceState.UnorderedAccess, ResourceState.CopySource));
//            context.ResourceBarrier(ResourceBarrier.Transition(_output.OutputBuffer, ResourceState.Present, ResourceState.CopyDestination));

//            context.CopyResource(_renderTarget, _output.OutputBuffer);

//            // We need to transition the back buffer to ResourceState.Present so it can be presented
//            context.ResourceBarrier(ResourceBarrier.Transition(_output.OutputBuffer, ResourceState.CopyDestination, ResourceState.Present));

//            context.Close();

//            // Execute the context and wait for it to finish
//            var task = _device.Execute(context);

//            task.Block();

//            // Present the rendered frame to the output
//            _output.Present();
//        }

//        public override void Dispose()
//        {
//            _pso.Dispose();
//            _tlas.Dispose();
//            _output.Dispose();
//            _device.Dispose();
//        }
//    }
//}
