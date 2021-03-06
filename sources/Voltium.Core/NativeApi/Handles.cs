using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Voltium.Common;

namespace Voltium.Core.NativeApi
{
    [GenerateEquality]
    public readonly partial struct GenerationalHandle : IEquatable<GenerationalHandle>
    {
        public readonly uint Generation;
        public readonly uint Id;

        public GenerationalHandle(uint generation, uint handle)
        {
            Generation = generation;
            Id = handle;
        }

        public override int GetHashCode() => (Generation | ((ulong)Id >> 32)).GetHashCode();

        public bool Equals(GenerationalHandle other) => Generation == other.Generation && Id == other.Id;
    }

    public interface IHandle<THandle> where THandle : struct, IHandle<THandle>
    {
        public GenerationalHandle Generational { get; }

        public THandle FromGenerationHandle(GenerationalHandle handle);
    }


    [GenerateEquality]
    public readonly partial struct IndirectCommandHandle : IHandle<IndirectCommandHandle>
    {
        private readonly GenerationalHandle Handle;

        public IndirectCommandHandle(GenerationalHandle handle) => Handle = handle;

        public GenerationalHandle Generational => Handle;
        public IndirectCommandHandle FromGenerationHandle(GenerationalHandle handle) => new(handle);
    }

    [GenerateEquality]
    public readonly partial struct DescriptorSetHandle : IHandle<DescriptorSetHandle>
    {
        private readonly GenerationalHandle Handle;

        public DescriptorSetHandle(GenerationalHandle handle) => Handle = handle;

        public GenerationalHandle Generational => Handle;
        public DescriptorSetHandle FromGenerationHandle(GenerationalHandle handle) => new(handle);
    }

    [GenerateEquality]
    public readonly partial struct ViewSetHandle : IHandle<ViewSetHandle>
    {
        private readonly GenerationalHandle Handle;

        public ViewSetHandle(GenerationalHandle handle) => Handle = handle;

        public GenerationalHandle Generational => Handle;
        public ViewSetHandle FromGenerationHandle(GenerationalHandle handle) => new(handle);
    }

    [GenerateEquality]
    public readonly partial struct TextureHandle : IHandle<TextureHandle>
    {
        private readonly GenerationalHandle Handle;

        public TextureHandle(GenerationalHandle handle) => Handle = handle;

        public GenerationalHandle Generational => Handle;
        public TextureHandle FromGenerationHandle(GenerationalHandle handle) => new(handle);
    }

    [GenerateEquality]
    public readonly partial struct ViewHandle : IHandle<ViewHandle>
    {
        private readonly GenerationalHandle Handle;

        public ViewHandle(GenerationalHandle handle) => Handle = handle;

        public GenerationalHandle Generational => Handle;
        public ViewHandle FromGenerationHandle(GenerationalHandle handle) => new(handle);
    }

    [GenerateEquality]
    public readonly partial struct RootSignatureHandle : IHandle<RootSignatureHandle>
    {
        private readonly GenerationalHandle Handle;

        public RootSignatureHandle(GenerationalHandle handle) => Handle = handle;

        public GenerationalHandle Generational => Handle;
        public RootSignatureHandle FromGenerationHandle(GenerationalHandle handle) => new(handle);
    }

    [GenerateEquality]
    public readonly partial struct PipelineHandle : IHandle<PipelineHandle>
    {
        private readonly GenerationalHandle Handle;

        public PipelineHandle(GenerationalHandle handle) => Handle = handle;

        public GenerationalHandle Generational => Handle;
        public PipelineHandle FromGenerationHandle(GenerationalHandle handle) => new(handle);
    }


    [GenerateEquality]
    public readonly partial struct RaytracingAccelerationStructureHandle : IHandle<RaytracingAccelerationStructureHandle>
    {
        private readonly GenerationalHandle Handle;

        public RaytracingAccelerationStructureHandle(GenerationalHandle handle) => Handle = handle;

        public GenerationalHandle Generational => Handle;
        public RaytracingAccelerationStructureHandle FromGenerationHandle(GenerationalHandle handle) => new(handle);
    }



    [GenerateEquality]
    public readonly partial struct BufferHandle : IHandle<BufferHandle>
    {
        private readonly GenerationalHandle Handle;

        public BufferHandle(GenerationalHandle handle) => Handle = handle;

        public GenerationalHandle Generational => Handle;
        public BufferHandle FromGenerationHandle(GenerationalHandle handle) => new(handle);
    }

    [GenerateEquality]
    public readonly partial struct RenderPassHandle : IHandle<RenderPassHandle>
    {
        private readonly GenerationalHandle Handle;

        public RenderPassHandle(GenerationalHandle handle) => Handle = handle;

        public GenerationalHandle Generational => Handle;
        public RenderPassHandle FromGenerationHandle(GenerationalHandle handle) => new(handle);
    }

    [GenerateEquality]
    public readonly partial struct QuerySetHandle : IHandle<QuerySetHandle>
    {
        private readonly GenerationalHandle Handle;

        public QuerySetHandle(GenerationalHandle handle) => Handle = handle;

        public GenerationalHandle Generational => Handle;
        public QuerySetHandle FromGenerationHandle(GenerationalHandle handle) => new(handle);
    }



    [GenerateEquality]
    public readonly partial struct HeapHandle : IHandle<HeapHandle>
    {
        private readonly GenerationalHandle Handle;

        public HeapHandle(GenerationalHandle handle) => Handle = handle;

        public GenerationalHandle Generational => Handle;
        public HeapHandle FromGenerationHandle(GenerationalHandle handle) => new(handle);
    }



    [GenerateEquality]
    public readonly partial struct FenceHandle : IHandle<FenceHandle>
    {
        private readonly GenerationalHandle Handle;

        public FenceHandle(GenerationalHandle handle) => Handle = handle;

        public GenerationalHandle Generational => Handle;
        public FenceHandle FromGenerationHandle(GenerationalHandle handle) => new(handle);
    }

}
