
using System;
using System.Diagnostics;
using System.Drawing;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using TerraFX.Interop;
using Voltium.Common;
using Voltium.Core;
using Voltium.Core.Contexts;
using Voltium.Core.Devices;
using Voltium.Core.Devices.Shaders;
using Voltium.Core.Pipeline;
using Voltium.Core.Memory;
using Buffer = Voltium.Core.Memory.Buffer;
using Voltium.Extensions;
using Voltium.Core.Raytracing;

namespace Voltium.Interactive.HelloTriangleRaytracing
{
    internal partial struct HelloWorldVertex
    {
        public HelloWorldVertex(float x, float y, float z) => Position = new(x, y, z);
        public Vector3 Position;
    }

    internal struct HelloTriangleViewport
    {
        public float Left, Top, Right, Bottom;
    }

    internal struct HelloTriangleConstantBuffer
    {
        public HelloTriangleViewport Viewport, Stencil;
    }

    public sealed class HelloTriangleRaytracingApp : Application
    {
        private GraphicsDevice _device = null!;
        private Output _output = null!;
        private PipelineStateObject _pso;
        private RootSignature _globalRootSig;
        private LocalRootSignature _localRootSig;
        private RaytracingAccelerationStructure _blas, _tlas;
        private Texture _renderTarget;
        private DescriptorAllocation _targetView;
        private DescriptorAllocation _accelerationStructure;
        private RayTables _rayTable;

        private Buffer _raygenBuffer, _missBuffer, _hitGroupBuffer;

        private double _millionRaysPerSecond;

        private HelloTriangleConstantBuffer _cb = new()
        {
            Viewport = new() { Left = -1.0f, Top = -1.0f, Right = 1.0f, Bottom = 1.0f }
        };

        private const string RayGenerationShaderName = "MyRaygenShader", MissShaderName = "MyMissShader", ClosestHitShaderName = "MyClosestHitShader", HitGroupName = "MyHitGroup";

        public override string Name => nameof(HelloTriangleRaytracingApp);
        public override string WindowTitle => $"Million Rays Per Second; {_millionRaysPerSecond:0.##}";

        public unsafe override void Initialize(Size outputSize, IOutputOwner output)
        {
            // Create the device and output
#if DEBUG
            var debug = DebugLayerConfiguration.Debug;
#else
            var debug = DebugLayerConfiguration.None;
#endif

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

            // The vertices and indices for our triangle
            ReadOnlySpan<ushort> indices = stackalloc ushort[3] { 0, 1, 2 };
            ReadOnlySpan<HelloWorldVertex> vertices = stackalloc HelloWorldVertex[3]
            {
                new HelloWorldVertex(+0.0f, -0.5f, +1.0f),
                new HelloWorldVertex(-0.5f, +0.5f, +1.0f),
                new HelloWorldVertex(+0.5f, +0.5f, +1.0f),
            };

            // Allocate the vertices, using the overload which takes some initial data
            var vertexBuffer = _device.Allocator.AllocateUploadBuffer(vertices);
            var indexBuffer = _device.Allocator.AllocateUploadBuffer(indices);

            var triangles = TriangleGeometryDesc.FromTypes<HelloWorldVertex, ushort>(VertexFormat.R32G32B32Single, vertexBuffer, indexBuffer);

            var desc = new GeometryDesc()
            {
                Type = GeometryType.Triangles,
                Triangles = triangles
            };

            CreatePso();
            CreateShaderTables();

            var builder = new ComputeContext();

            _blas = _device.Allocator.AllocateRaytracingAccelerationBuffer(_device.GetBottomLevelAccelerationStructureBuildInfo(desc), out var blasScratch);

            var instances = _device.Allocator.AllocateUploadBuffer<GeometryInstance>();
            *instances.As<GeometryInstance>() = new()
            {
                InstanceMask = 1,
                AccelerationStructure = _blas.DeviceAddress,
                Transform = Matrix4x4.Identity
            };

            _tlas = _device.Allocator.AllocateRaytracingAccelerationBuffer(_device.GetTopLevelAccelerationStructureBuildInfo(1), out var tlasScratch);

            builder.BuildBottomLevelAccelerationStructure(desc, blasScratch, _blas);
            builder.WriteBarrier(ResourceWriteBarrier.Create(_blas));
            builder.BuildTopLevelAccelerationStructure(instances, 1, LayoutType.Array, tlasScratch, _tlas);

            builder.Close();

            _device.GraphicsQueue.Execute(builder);

            _targetView = _device.AllocateResourceDescriptors(DescriptorType.WritableTexture, 1);
            _accelerationStructure = _device.AllocateResourceDescriptors(DescriptorType.RaytracingAccelerationStructure, 1);

            OnResize(outputSize);
        }

        private unsafe void CreatePso()
        {
            _globalRootSig = _device.CreateRootSignature(
                new RootParameter[]
                {
                    RootParameter.CreateDescriptorTable(DescriptorRangeType.UnorderedAccessView, 0, 1, 0),
                    RootParameter.CreateDescriptorTable(DescriptorRangeType.ShaderResourceView, 0, 1, 0),
                }
            );

            _localRootSig = _device.CreateLocalRootSignature(new[] { RootParameter.CreateConstants<HelloTriangleConstantBuffer>(0, 0) });

            var desc = new RaytracingPipelineDesc
            {
                GlobalRootSignature = _globalRootSig,
                MaxRecursionDepth = 1,
                MaxPayloadSize = sizeof(float) * 4,
                MaxAttributeSize = sizeof(float) * 2
            };

            desc.AddLibrary(ShaderManager.CompileShader("HelloTriangleRaytracing\\HelloTriangleRaytracingShader.hlsl", ShaderType.Library));
            desc.AddTriangleHitGroup(HitGroupName, ClosestHitShaderName, null);
            desc.AddLocalRootSignature(_localRootSig, RayGenerationShaderName);

            _pso = _device.CreatePipelineStateObject(desc);
        }

        private unsafe void CreateShaderTables()
        {
            var idSize = _device.Info.RaytracingShaderIdentifierSize;
            _raygenBuffer = _device.Allocator.AllocateUploadBuffer(idSize + (uint)sizeof(HelloTriangleConstantBuffer));
            _missBuffer = _device.Allocator.AllocateUploadBuffer(idSize);
            _hitGroupBuffer = _device.Allocator.AllocateUploadBuffer(idSize);

            _device.GetRaytracingShaderIdentifier(_pso, RayGenerationShaderName, _raygenBuffer.AsSpan());
            _device.GetRaytracingShaderIdentifier(_pso, MissShaderName, _missBuffer.AsSpan());
            _device.GetRaytracingShaderIdentifier(_pso, HitGroupName, _hitGroupBuffer.AsSpan());

            _rayTable.SetRayGenerationShaderRecord(_raygenBuffer, (uint)_raygenBuffer.Length);
            _rayTable.SetMissShaderTable(_missBuffer, (uint)_missBuffer.Length, 1);
            _rayTable.SetHitGroupTable(_hitGroupBuffer, (uint)_hitGroupBuffer.Length, 1);
        }

        public unsafe override void OnResize(Size newOutputSize)
        {
            _output.Resize(newOutputSize);

            var aspectRatio = newOutputSize.AspectRatio();
            float border = 0.1f;
            if (newOutputSize.Width <= newOutputSize.Height)
            {
                _cb.Stencil = new HelloTriangleViewport { Left = -1 + border, Top = -1 + (border * aspectRatio), Right = 1.0f - border, Bottom = 1 - (border * aspectRatio) };
            }
            else
            {
                _cb.Stencil = new HelloTriangleViewport { Left = -1 + (border / aspectRatio), Top = -1 + border, Right = 1 - (border / aspectRatio), Bottom = 1.0f - border };
            }

            var cb = (HelloTriangleConstantBuffer*)(_raygenBuffer.As<byte>() + _device.Info.RaytracingShaderIdentifierSize);
            *cb = _cb;

            _renderTarget.Dispose();
            _renderTarget = _device.Allocator.AllocateTexture(
                new TextureDesc
                {
                    Dimension = TextureDimension.Tex2D,
                    Format = (DataFormat)_output.Format,
                    Height = (uint)newOutputSize.Height,
                    Width = (uint)newOutputSize.Width,
                    MipCount = 1,
                    ResourceFlags = ResourceFlags.AllowUnorderedAccess
                },
                ResourceState.UnorderedAccess
            );

            using var views = _device.CreateViewSet(2);

            _device.CreateDefaultView(views, 0, _renderTarget);
            _device.CreateDefaultView(views, 1, _tlas);

            _device.UpdateDescriptors(views, _targetView, 1);
            _device.UpdateDescriptors(views, 1, _accelerationStructure, 0, 1);
        }

        public override void Update(ApplicationTimer timer) => _millionRaysPerSecond = (_output.Resolution.Width * _output.Resolution.Height * timer.FramesPerSeconds) / 1_000_000.0;

        private ComputeContext _context = new(closed: true);
        public override void Render()
        {
            var context = _context;

            context.Reset();

            context.SetPipelineState(_pso);

            context.BindDescriptors(0, _targetView);
            context.BindDescriptors(1, _accelerationStructure);

            context.DispatchRays(_output.Width, _output.Height, _rayTable);

            using (context.ScopedBarrier(stackalloc[] {
                ResourceTransition.Create(_renderTarget, ResourceState.UnorderedAccess, ResourceState.CopySource),
                ResourceTransition.Create(_output.OutputBuffer, ResourceState.Present, ResourceState.CopyDestination)
            }))
            {
                context.CopyTexture(_renderTarget, _output.OutputBuffer, 0);
            }

            context.Close();

            // Execute the context and wait for it to finish, then present the rendered frame to the output
            _device.GraphicsQueue.Execute(context).Block();
            _output.Present();
        }

        public override void Dispose()
        {
            _pso.Dispose();
            _tlas.Dispose();
            _output.Dispose();
            _device.Dispose();
        }
    }

//    public sealed class HelloTriangleRaytracingShader : RaytracingShader
//    {
//#nullable disable
//        RaytracingAccelerationStructure Scene = null!;
//        WritableTexture2D<Vector4<float>> RenderTarget = null!;
//        readonly HelloTriangleConstantBuffer g_rayGenCB = default;

//        struct RayPayload
//        {
//            public Vector4<float> Color;
//        };

//        bool IsInsideViewport(Vector2<float> p, HelloTriangleViewport viewport)
//            => p.X >= viewport.Left && p.X <= viewport.Right && p.Y >= viewport.Top && p.Y <= viewport.Bottom;

//        [RayGenerationShader]
//        void MyRaygenShader()
//        {
//            var lerpValues = (Vector2<float>)DispatchRaysIndex / (Vector2<float>)DispatchRaysDimensions;

//            // Orthographic projection since we're raytracing in screen space.
//            var rayDir = new Vector3<float>(0, 0, 1);
//            var origin = new Vector3<float>(
//                MathF.Lerp(g_rayGenCB.Viewport.Left, g_rayGenCB.Viewport.Right, lerpValues.X),
//                MathF.Lerp(g_rayGenCB.Viewport.Top, g_rayGenCB.Viewport.Bottom, lerpValues.Y),
//                0.0f
//            );

//            if (IsInsideViewport(origin.XY, g_rayGenCB.Stencil))
//            {
//                var ray = new RayDesc
//                {
//                    // Set the ray's extents.
//                    Origin = origin,
//                    Direction = rayDir,

//                    // Set TMin to a non-zero small value to avoid aliasing issues due to floating - point errors.
//                    // TMin should be kept small to prevent missing geometry at close contact areas.
//                    TMin = 0.001f,
//                    TMax = 10000.0f
//                };

//                var payload = new RayPayload { Color = default };

//                // Trace the ray.
//                TraceRay(Scene, ref payload, ray, uint.MaxValue, 0, 1, 0, TraceRayFlags.CullBackFacingTriangles);

//                // Write the raytraced color to the output texture.
//                RenderTarget[DispatchRaysIndex.XY] = payload.Color;
//            }
//            else
//            {
//                // Render interpolated DispatchRaysIndex outside the stencil window
//                RenderTarget[DispatchRaysIndex.XY] = new Vector4<float>(lerpValues, 0, 1);
//            }
//        }

//        [RayClosestHitShader]
//        void MyClosestHitShader(ref RayPayload payload, BuiltInTriangleIntersectionAttributes attr)
//        {
//            payload.Color = new Vector4<float>(1 - attr.Barycentrics.X - attr.Barycentrics.Y, attr.Barycentrics.X, attr.Barycentrics.Y, 1);
//        }

//        [RayMissShader]
//        void MyMissShader(ref RayPayload payload)
//        {
//            payload.Color = new Vector4<float>(0, 0, 0, 1);
//        }
//    }
}
