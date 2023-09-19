
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
//using Voltium.Extensions;
//using Voltium.Core.Raytracing;

////using Vertex = Voltium.ModelLoading.TexturedVertex;

//namespace Voltium.Interactive.BasicRaytracingScene
//{
//    internal partial struct Vertex
//    {
//        public Vertex(float x, float y, float z) => Position = new(x, y, z);
//        public Vector3 Position;
//    }

//    internal struct Viewport
//    {
//        public float Left, Top, Right, Bottom;
//    }

//    internal struct RayGenConstantBuffer
//    {
//        public Viewport Viewport, Stencil;
//    }

//    public sealed class BasicRaytracingSceneApp : Application
//    {
//        private GraphicsDevice _device = null!;
//        private Output _output = null!;
//        private RaytracingPipelineStateObject _pso = null!;
//        private RootSignature _globalRootSig = null!, _localRootSig = null!;
//        private RaytracingAccelerationStructure _blas, _tlas;
//        private Texture _renderTarget;
//        private DescriptorAllocation _target;
//        private DescriptorHeap _opaqueUav = null!;
//        private uint _renderWidth, _renderHeight;

//        private Buffer _raygenBuffer, _missBuffer, _hitGroupBuffer;

//        private ShaderRecord _raygen;
//        private ShaderRecordTable _miss, _hitGroup;

//        private RayGenConstantBuffer _cb = new()
//        {
//            Viewport = new() { Left = -1.0f, Top = -1.0f, Right = 1.0f, Bottom = 1.0f }
//        };

//        private const string RayGenerationShaderName = "MyRaygenShader", MissShaderName = "MyMissShader", ClosestHitShaderName = "MyClosestHitShader", HitGroupName = "MyHitGroup";

//        public override string Name => nameof(BasicRaytracingSceneApp);

//        public unsafe override void Initialize(Size outputSize, IOutputOwner output)
//        {
//            // Create the device and output
//#if DEBUG
//            var debug = DebugLayerConfiguration.Debug;
//#else
//            var debug = DebugLayerConfiguration.None;
//#endif

//            _device = GraphicsDevice.Create(FeatureLevel.GraphicsLevel12_1, null, debug);

//            if (!_device.Supports(DeviceFeature.Raytracing))
//            {
//                throw new PlatformNotSupportedException("Platform does not support Raytracing Tier 1");
//            }

//            _output = Output.Create(OutputConfiguration.Default, _device, output);

//            _opaqueUav = _device.CreateDescriptorHeap(DescriptorHeapType.ConstantBufferShaderResourceOrUnorderedAccessView, 1, false);
//            _target = _device.AllocateResourceDescriptors(1);

//            OnResize(outputSize);

//            CreatePso();
//            CreateShaderTables();

//            BuildGeometry();
//        }

//        private GeometryDesc CreateGeometry<TVertex, TIndex>(Span<TVertex> vertices, Span<TIndex> indices, out Buffer vertexBuffer, out Buffer indexBuffer)
//            where TVertex : unmanaged
//            where TIndex : unmanaged
//        {

//            // Allocate the vertices, using the overload which takes some initial data
//            vertexBuffer = _device.Allocator.AllocateUploadBuffer(vertices);
//            indexBuffer = _device.Allocator.AllocateUploadBuffer(indices);

//            var triangles = TriangleGeometryDesc.FromTypes<TVertex, TIndex>(VertexFormat.R32G32B32Single, vertexBuffer, indexBuffer);

//            return GeometryDesc.FromTriangles(triangles);
//        }

//        private unsafe void BuildGeometry()
//        {
//            // The vertices and indices for our triangle
//            var cube = GeometryGenerator.CreateCube(0.5f);

//            //ReadOnlySpan<ushort> indices = stackalloc ushort[3] { 0, 1, 2 };
//            //ReadOnlySpan<Vertex> vertices = stackalloc Vertex[3]
//            //{
//            //    new Vertex(+0.0f, -0.5f, +1.0f),
//            //    new Vertex(-0.5f, +0.5f, +1.0f),
//            //    new Vertex(+0.5f, +0.5f, +1.0f),
//            //};

//            var box = CreateGeometry(cube.Vertices.AsSpan(), cube.Indices.AsSpan(), out var vertexBuffer, out var indexBuffer);

//            using (var context = _device.BeginComputeContext(flags: ContextFlags.BlockOnClose))
//            {
//                _blas = _device.Allocator.AllocateRaytracingAccelerationBuffer(_device.GetBuildInfo(box), out var blasScratch);

//                var instances = _device.Allocator.AllocateUploadBuffer<GeometryInstance>();
//                instances.AsRef<GeometryInstance>() = new()
//                {
//                    InstanceMask = 1,
//                    AccelerationStructure = _blas.GpuAddress,
//                    Transform = Matrix4x4.CreateTranslation(0, 0, -10)
//                };

//                _tlas = _device.Allocator.AllocateRaytracingAccelerationBuffer(_device.GetBuildInfo(Layout.Array, 1), out var tlasScratch);

//                context.BuildAccelerationStructure(box, blasScratch, _blas, BuildAccelerationStructureFlags.InsertUavBarrier);
//                context.BuildAccelerationStructure(instances, Layout.Array, 1, tlasScratch, _tlas, BuildAccelerationStructureFlags.InsertUavBarrier);

//                context.Attach(ref vertexBuffer, ref indexBuffer, ref blasScratch, ref tlasScratch, ref instances);
//            }
//        }

//        private unsafe void CreatePso()
//        {
//            _globalRootSig = _device.CreateRootSignature(
//                new []
//                {
//                    RootParameter.CreateDescriptor(RootParameterType.ShaderResourceView, 0, 0),
//                    RootParameter.CreateDescriptorTable(DescriptorRangeType.UnorderedAccessView, 0, 1, 0)
//                }
//            );

//            _localRootSig = _device.CreateLocalRootSignature(RootParameter.CreateConstants<RayGenConstantBuffer>(0, 0));

//            var desc = new RaytracingPipelineDesc
//            {
//                GlobalRootSignature = _globalRootSig,
//                MaxRecursionDepth = 1,
//                MaxPayloadSize = sizeof(float) * 4,
//                MaxAttributeSize = sizeof(float) * 2
//            };

//            desc.AddLibrary(ShaderManager.CompileShader("BasicRaytracingScene\\BasicRaytracingSceneShader.hlsl", ShaderType.Library));
//            desc.AddTriangleHitGroup(HitGroupName, ClosestHitShaderName, null);
//            desc.AddLocalRootSignature(_localRootSig, RayGenerationShaderName);

//            _pso = _device.PipelineManager.CreatePipelineStateObject(desc, nameof(BasicRaytracingSceneApp));
//        }

//        private unsafe void CreateShaderTables()
//        {
//            _raygenBuffer = _device.Allocator.AllocateUploadBuffer(ShaderRecord.ShaderIdentifierSize + (uint)sizeof(RayGenConstantBuffer));
//            _missBuffer = _device.Allocator.AllocateUploadBuffer(ShaderRecord.ShaderIdentifierSize);
//            _hitGroupBuffer = _device.Allocator.AllocateUploadBuffer(ShaderRecord.ShaderIdentifierSize);

//            _raygen = new ShaderRecord(_pso, _raygenBuffer, ShaderRecord.ShaderIdentifierSize + (uint)sizeof(RayGenConstantBuffer));
//            _miss = new ShaderRecordTable(_pso, _missBuffer, 1, ShaderRecord.ShaderIdentifierSize);
//            _hitGroup = new ShaderRecordTable(_pso, _hitGroupBuffer, 1, ShaderRecord.ShaderIdentifierSize);

//            _raygen.SetShaderName(RayGenerationShaderName); // MyRaygenShader
//            _miss[0].SetShaderName(MissShaderName); // MyMissShader
//            _hitGroup[0].SetShaderName(HitGroupName); // MyClosestHitShader

//            _raygen.WriteConstants(0, _cb);
//        }

//        public override void OnResize(Size newOutputSize)
//        {
//            _renderWidth = (uint)newOutputSize.Width;
//            _renderHeight = (uint)newOutputSize.Height;
//            _output.Resize(newOutputSize);

//            var aspectRatio = newOutputSize.AspectRatio();
//            float border = 0.1f;
//            if (newOutputSize.Width <= newOutputSize.Height)
//            {
//                _cb.Stencil = new Viewport { Left = -1 + border, Top = -1 + (border * aspectRatio), Right = 1.0f - border, Bottom = 1 - (border * aspectRatio) };
//            }
//            else
//            {
//                _cb.Stencil = new Viewport { Left = -1 + (border / aspectRatio), Top = -1 + border, Right = 1 - (border / aspectRatio), Bottom = 1.0f - border };

//            }

//            _renderTarget.Dispose();
//            _renderTarget = _device.Allocator.AllocateTexture(
//                TextureDesc.CreateUnorderedAccessResourceDesc(
//                    (DataFormat)_output.Configuration.BackBufferFormat,
//                    TextureDimension.Tex2D,
//                    (uint)newOutputSize.Width,
//                    (uint)newOutputSize.Height
//                ).WithMipCount(1),
//                ResourceState.UnorderedAccess
//            );

//            _device.CreateUnorderedAccessView(_renderTarget, _opaqueUav[0]);
//            _device.CopyDescriptors(_opaqueUav[0..1], _target[0..1]);
//        }

//        public override void Update(ApplicationTimer timer) { /* This app doesn't do any updating */ }
//        public override void Render()
//        {
//            var context = (ComputeContext)_device.BeginGraphicsContext(_pso);

//            context.ClearUnorderedAccessViewSingle(_target[0], _opaqueUav[0], _renderTarget, Rgba128.CornflowerBlue);
//            context.Barrier(ResourceBarrier.UnorderedAccess(_renderTarget));

//            context.SetRaytracingAccelerationStructure(0, _tlas);
//            context.SetRootDescriptorTable(1, _target[0]);

//            context.DispatchRays(_renderWidth, _renderHeight, _raygen, _hitGroup, _miss);

//            using (context.ScopedBarrier(stackalloc[] {
//                ResourceTransition.Create(_renderTarget, ResourceState.UnorderedAccess, ResourceState.CopySource),
//                ResourceTransition.Create(_output.OutputBuffer, ResourceState.Present, ResourceState.CopyDestination)
//            }))
//            {
//                context.CopyResource(_renderTarget, _output.OutputBuffer);
//            }

//            context.Close();

//            // Execute the context and wait for it to finish, then present the rendered frame to the output
//            var block = _device.Execute(context);
//            _output.Present(presentAfter: block);
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
