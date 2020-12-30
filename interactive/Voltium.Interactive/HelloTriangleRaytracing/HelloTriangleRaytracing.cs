
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
using Voltium.Core.ShaderLang;

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
        private RaytracingPipelineStateObject _pso = null!;
        private RootSignature _globalRootSig = null!, _localRootSig = null!;
        private RaytracingAccelerationStructure _blas, _tlas;
        private Texture _renderTarget;
        private DescriptorAllocation _target;

        private Buffer _raygenBuffer, _missBuffer, _hitGroupBuffer;

        private ShaderRecord _raygen;
        private ShaderRecordTable _miss, _hitGroup;

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

            _device = GraphicsDevice.Create(FeatureLevel.GraphicsLevel12_1, null, debug);

            if (!_device.Supports(DeviceFeature.Raytracing))
            {
                throw new PlatformNotSupportedException("Platform does not support Raytracing Tier 1");
            }

            _output = Output.Create(OutputConfiguration.Default, _device, output);

            OnResize(outputSize);

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

            using (var context = _device.BeginComputeContext(flags: ContextFlags.BlockOnClose))
            {
                _blas = _device.Allocator.AllocateRaytracingAccelerationBuffer(_device.GetBuildInfo(desc), out var blasScratch);

                var instances = _device.Allocator.AllocateUploadBuffer<GeometryInstance>();
                *instances.As<GeometryInstance>() = new()
                {
                    InstanceMask = 1,
                    AccelerationStructure = _blas.GpuAddress,
                    Transform = Matrix4x4.Identity
                };

                _tlas = _device.Allocator.AllocateRaytracingAccelerationBuffer(_device.GetBuildInfo(Layout.Array, 1), out var tlasScratch);

                context.BuildAccelerationStructure(desc, blasScratch, _blas, BuildAccelerationStructureFlags.InsertUavBarrier);
                context.BuildAccelerationStructure(instances, Layout.Array, 1, tlasScratch, _tlas, BuildAccelerationStructureFlags.InsertUavBarrier);
            }
        }

        private unsafe void CreatePso()
        {
            _globalRootSig = _device.CreateRootSignature(
                new RootParameter[]
                {
                    RootParameter.CreateDescriptor(RootParameterType.ShaderResourceView, 0, 0),
                    RootParameter.CreateDescriptorTable(DescriptorRangeType.UnorderedAccessView, 0, 1, 0)
                }
            );

            _localRootSig = _device.CreateLocalRootSignature(RootParameter.CreateConstants<HelloTriangleConstantBuffer>(0, 0));

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

            _pso = _device.PipelineManager.CreatePipelineStateObject(desc, nameof(HelloTriangleRaytracingApp));
        }

        private unsafe void CreateShaderTables()
        {
            _raygenBuffer = _device.Allocator.AllocateUploadBuffer(ShaderRecord.ShaderIdentifierSize + (uint)sizeof(HelloTriangleConstantBuffer));
            _missBuffer = _device.Allocator.AllocateUploadBuffer(ShaderRecord.ShaderIdentifierSize);
            _hitGroupBuffer = _device.Allocator.AllocateUploadBuffer(ShaderRecord.ShaderIdentifierSize);

            _raygen = new ShaderRecord(_pso, _raygenBuffer, ShaderRecord.ShaderIdentifierSize + (uint)sizeof(HelloTriangleConstantBuffer));
            _miss = new ShaderRecordTable(_pso, _missBuffer, 1, ShaderRecord.ShaderIdentifierSize);
            _hitGroup = new ShaderRecordTable(_pso, _hitGroupBuffer, 1, ShaderRecord.ShaderIdentifierSize);

            _raygen.SetShaderName(RayGenerationShaderName); // MyRaygenShader
            _miss[0].SetShaderName(MissShaderName); // MyMissShader
            _hitGroup[0].SetShaderName(HitGroupName); // MyClosestHitShader

            _raygen.WriteConstants(0, _cb);
        }

        public override void OnResize(Size newOutputSize)
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

            _renderTarget.Dispose();
            _renderTarget = _device.Allocator.AllocateTexture(
                new TextureDesc
                {
                    Dimension = TextureDimension.Tex2D,
                    Format = (DataFormat)_output.Configuration.BackBufferFormat,
                    Height = (uint)newOutputSize.Height,
                    Width = (uint)newOutputSize.Width,
                    MipCount = 1,
                    ResourceFlags = ResourceFlags.AllowUnorderedAccess
                },
                ResourceState.UnorderedAccess
            );

            _target = _device.AllocateResourceDescriptors(1);
            _device.CreateUnorderedAccessView(_renderTarget, _target[0]);
        }

        public override void Update(ApplicationTimer timer) => _millionRaysPerSecond = (_output.Resolution.Width * _output.Resolution.Height * timer.FramesPerSeconds) / 1_000_000.0;
        public override void Render()
        {
            var context = (ComputeContext)_device.BeginGraphicsContext(_pso);

            context.SetRaytracingAccelerationStructure(0, _tlas);
            context.SetRootDescriptorTable(1, _target[0]);

            context.DispatchRays(new RayDispatchDesc
            {
                RayGenerationShaderRecord = _raygen,
                MissShaderTable = _miss,
                HitGroupTable = _hitGroup,
                Width = (uint)_output.Resolution.Width,
                Height = (uint)_output.Resolution.Height,
                Depth = 1
            });

            using (context.ScopedBarrier(stackalloc[] {
                ResourceBarrier.Transition(_renderTarget, ResourceState.UnorderedAccess, ResourceState.CopySource),
                ResourceBarrier.Transition(_output.OutputBuffer, ResourceState.Present, ResourceState.CopyDestination)
            }))
            {
                context.CopyResource(_renderTarget, _output.OutputBuffer);
            }

            context.Close();

            // Execute the context and wait for it to finish, then present the rendered frame to the output
            _device.Execute(context).Block();
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

    public sealed class HelloTriangleRaytracingShader : RaytracingShader
    {
        #nullable disable
        RaytracingAccelerationStructure Scene = null!;
        WritableTexture2D<Vector4<float>> RenderTarget = null!;
        readonly HelloTriangleConstantBuffer g_rayGenCB = default;

        struct RayPayload
        {
            public Vector4<float> Color;
        };

        bool IsInsideViewport(Vector2<float> p, HelloTriangleViewport viewport)
            => p.X >= viewport.Left && p.X <= viewport.Right && p.Y >= viewport.Top && p.Y <= viewport.Bottom;

        [RayGenerationShader]
        void MyRaygenShader()
        {
            var lerpValues = (Vector2<float>)DispatchRaysIndex / (Vector2<float>)DispatchRaysDimensions;

            // Orthographic projection since we're raytracing in screen space.
            var rayDir = new Vector3<float>(0, 0, 1);
            var origin = new Vector3<float>(
                MathF.Lerp(g_rayGenCB.Viewport.Left, g_rayGenCB.Viewport.Right, lerpValues.X),
                MathF.Lerp(g_rayGenCB.Viewport.Top, g_rayGenCB.Viewport.Bottom, lerpValues.Y),
                0.0f
            );

            if (IsInsideViewport(origin.XY, g_rayGenCB.Stencil))
            {
                var ray = new RayDesc
                {
                    // Set the ray's extents.
                    Origin = origin,
                    Direction = rayDir,

                    // Set TMin to a non-zero small value to avoid aliasing issues due to floating - point errors.
                    // TMin should be kept small to prevent missing geometry at close contact areas.
                    TMin = 0.001f,
                    TMax = 10000.0f
                };

                var payload = new RayPayload { Color = default };

                // Trace the ray.
                TraceRay(Scene, ref payload, ray, uint.MaxValue, 0, 1, 0, TraceRayFlags.CullBackFacingTriangles);

                // Write the raytraced color to the output texture.
                RenderTarget[DispatchRaysIndex.XY] = payload.Color;
            }
            else
            {
                // Render interpolated DispatchRaysIndex outside the stencil window
                RenderTarget[DispatchRaysIndex.XY] = new Vector4<float>(lerpValues, 0, 1);
            }
        }

        [RayClosestHitShader]
        void MyClosestHitShader(ref RayPayload payload, BuiltInTriangleIntersectionAttributes attr)
        {
            payload.Color = new Vector4<float>(1 - attr.Barycentrics.X - attr.Barycentrics.Y, attr.Barycentrics.X, attr.Barycentrics.Y, 1);
        }

        [RayMissShader]
        void MyMissShader(ref RayPayload payload)
        {
            payload.Color = new Vector4<float>(0, 0, 0, 1);
        }
    }
}
