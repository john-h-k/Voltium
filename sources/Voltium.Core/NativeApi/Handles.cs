using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Voltium.Common;

namespace Voltium.Core.NativeApi
{
    /// <summary>
    /// A handle with a generation and an ID
    /// </summary>
    [GenerateEquality]
    public readonly partial struct GenerationalHandle : IEquatable<GenerationalHandle>
    {
        /// <summary>
        /// The generation of this handle
        /// </summary>
        public readonly uint Generation;

        /// <summary>
        /// The ID of this handle
        /// </summary>
        public readonly uint Id;

        /// <summary>
        /// Creates a new <see cref="GenerationalHandle"/>
        /// </summary>
        /// <param name="generation">The generation of this handle</param>
        /// <param name="id">The ID of this handle</param>
        public GenerationalHandle(uint generation, uint id)
        {
            Generation = generation;
            Id = id;
        }

        /// <summary>
        /// Returns a hash code for this <see cref="GenerationalHandle"/>
        /// </summary>
        /// <returns>The hash code of this handle</returns>
        public override int GetHashCode() => (Generation | ((ulong)Id >> 32)).GetHashCode();

        /// <summary>
        /// Determines whether 2 handles have the same generation and ID
        /// </summary>
        /// <param name="other">The other <see cref="GenerationalHandle"/> to compare this one to</param>
        /// <returns>Whether the 2 handles have the same generation and ID</returns>
        public bool Equals(GenerationalHandle other) => Generation == other.Generation && Id == other.Id;
    }

    /// <summary>
    /// Represents an opaque handle
    /// </summary>
    /// <typeparam name="THandle">The type of the handle</typeparam>
    public interface IHandle<THandle> where THandle : struct, IHandle<THandle>
    {
        /// <summary>
        /// The <see cref="GenerationalHandle"/> for this handle. This can't be inspected by user code
        /// </summary>
        public GenerationalHandle Generational { get; }

        /// <summary>
        /// Creates a new <typeparamref name="THandle"/> from a given <see cref="GenerationalHandle"/>
        /// </summary>
        /// <param name="handle">The <see cref="GenerationalHandle"/> to create this from</param>
        /// <returns>A new <typeparamref name="THandle"/></returns>
        public THandle FromGenerationHandle(GenerationalHandle handle);
    }


#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
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
