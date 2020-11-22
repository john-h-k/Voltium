
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
    // This is our vertex type used in the shader
    // [ShaderInput] triggers a source generator to create a shader input description which we need later
    [ShaderInput]
    internal partial struct HelloWorldVertex
    {
        public Vector3 Position;
        public Vector4 Color;
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
        private Buffer _blas, _tlas;
        private Texture _renderTarget;
        private DescriptorHandle _target;

        private Buffer _raygenBuffer, _missBuffer, _hitGroupBuffer;

        private ShaderRecord _raygen;
        private ShaderRecordTable _miss, _hitGroup;

        private HelloTriangleConstantBuffer _cb = new()
        {
            Viewport = new() { Left = -1.0f, Top = -1.0f, Right = 1.0f, Bottom = 1.0f }
        };

        private const string RayGenerationShaderName = "MyRaygenShader", MissShaderName = "MyMissShader", ClosestHitShaderName = "MyClosestHitShader", HitGroupName = "MyHitGroup";

        public override string Name => nameof(HelloTriangleRaytracingApp);

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

            ReadOnlySpan<ushort> indices = stackalloc ushort[3] { 0, 1, 2 };

            float depthValue = 1.0f;
            float offset = 0.5f;
            // The vertices for our triangle
            ReadOnlySpan<HelloWorldVertex> vertices = stackalloc HelloWorldVertex[3]
            {
                new HelloWorldVertex { Position = new Vector3(0, -offset, depthValue), Color = (Vector4)Rgba128.Blue },
                new HelloWorldVertex { Position = new Vector3(-offset, offset, depthValue), Color = (Vector4)Rgba128.Green },
                new HelloWorldVertex { Position = new Vector3(offset, offset, depthValue), Color = (Vector4)Rgba128.Red },
            };

            // Allocate the vertices, using the overload which takes some initial data
            var vertexBuffer = _device.Allocator.AllocateUploadBuffer(vertices);
            var indexBuffer = _device.Allocator.AllocateUploadBuffer(indices);

            var triangles = new TriangleGeometryDesc()
            {
                VertexBuffer = vertexBuffer,
                VertexFormat = VertexFormat.R32G32B32Single,
                VertexCount = (uint)vertices.Length,
                VertexStride = (uint)sizeof(HelloWorldVertex),
                IndexBuffer = indexBuffer,
                IndexFormat = IndexFormat.R16UInt,
                IndexCount = (uint)indices.Length
            };

            var desc = new GeometryDesc()
            {
                Type = GeometryType.Triangles,
                Flags = GeometryFlags.None,
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

            _target = _device.CreateUnorderedAccessView(_renderTarget);
        }

        public override void Update(ApplicationTimer timer) { /* This app doesn't do any updating */ }
        public override void Render()
        {
            var context = (ComputeContext)_device.BeginGraphicsContext(_pso);

            context.SetShaderResourceBuffer(0, _tlas);
            context.SetRootDescriptorTable(1, _target);

            context.DispatchRays(new RayDispatchDesc
            {
                RayGenerationShaderRecord = _raygen,
                MissShaderTable = _miss,
                HitGroupTable = _hitGroup,
                Width = (uint)_output.Resolution.Width,
                Height = (uint)_output.Resolution.Height,
                Depth = 1
            });

            using (context.ScopedBarrier(stackalloc[]
            {
                ResourceBarrier.Transition(_renderTarget, ResourceState.UnorderedAccess, ResourceState.CopySource),
                ResourceBarrier.Transition(_output.OutputBuffer, ResourceState.Present, ResourceState.CopyDestination)
            }))
            {
                context.CopyResource(_renderTarget, _output.OutputBuffer);
            }

            context.Close();

            // Execute the context and wait for it to finish
            var task = _device.Execute(context);

            task.Block();

            // Present the rendered frame to the output
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
}
