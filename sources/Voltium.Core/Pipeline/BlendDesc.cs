using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using TerraFX.Interop;
using Voltium.Common;

namespace Voltium.Core.Pipeline
{
    /// <summary>
    /// Describes the blend state of the GPU pipeline
    /// </summary>
    public struct BlendDesc : IPipelineStreamElement<BlendDesc>
    {
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public void _Initialize() => Type = D3D12_PIPELINE_STATE_SUBOBJECT_TYPE.D3D12_PIPELINE_STATE_SUBOBJECT_TYPE_BLEND;
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

        internal D3D12_PIPELINE_STATE_SUBOBJECT_TYPE Type;
        internal D3D12_BLEND_DESC Desc;

        /// <summary>
        /// The default <see cref="BlendDesc"/>. This correspends
        /// to <c>new CD3DX12_BLEND_DESC(DEFAULT)</c>
        /// </summary>
        public static BlendDesc Default { get; } = new BlendDesc
        {
            UseAlphaToCoverage = false,
            UseIndependentBlend = false,
            [0] = RenderTargetBlendDesc.Default
        };


        /// <summary>
        /// Whether to use alpha-to-coverage AA
        /// </summary>
        public bool UseAlphaToCoverage { get => Helpers.Int32ToBool(Desc.AlphaToCoverageEnable); set => Desc.AlphaToCoverageEnable = Helpers.BoolToInt32(value); }

        /// <summary>
        /// Whether each render target should use a seperate <see cref="RenderTargetBlendDesc"/>.
        /// If this is <see langword="false"/>, all render targets will use the first <see cref="RenderTargetBlendDesc"/>
        /// </summary>
        public bool UseIndependentBlend { get => Helpers.Int32ToBool(Desc.IndependentBlendEnable); set => Desc.IndependentBlendEnable = Helpers.BoolToInt32(value); }

        /// <summary>
        /// Retrieves a <see cref="RenderTargetBlendDesc"/> by index
        /// </summary>
        /// <param name="index"></param>
        public unsafe ref RenderTargetBlendDesc this[int index] => ref Unsafe.As<D3D12_RENDER_TARGET_BLEND_DESC, RenderTargetBlendDesc>(ref Unsafe.Add(ref MemoryMarshal.GetReference(MemoryMarshal.CreateSpan(ref Desc.RenderTarget[0], 0)), index));

        ///// <summary>
        ///// A buffer of 8 <see cref="RenderTargetBlendDesc"/>s
        ///// </summary>
        //public struct RenderTargetBlendDescBuffer8
        //{
        //    /// <summary>
        //    /// Retrieves a <see cref="RenderTargetBlendDesc"/> by index
        //    /// </summary>
        //    /// <param name="index"></param>
        //    public unsafe ref RenderTargetBlendDesc this[int index]
        //    {
        //        get
        //        {
        //            Guard.Positive(index);
        //            fixed (RenderTargetBlendDesc* p0 = &RenderTarget0)
        //            {
        //                return ref *(p0 + index);
        //            }
        //        }
        //    }
        //}
    }
}
