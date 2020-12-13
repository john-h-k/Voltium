using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using TerraFX.Interop;
using Voltium.Common;

namespace Voltium.Core.Pipeline
{
    /// <summary>
    /// Describes the settings and state of the depth stencil of the pipeline
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    [Fluent]
    public partial struct DepthStencilDesc
    {
        /// <summary>
        /// The default <see cref="DepthStencilDesc"/>. This correspends
        /// to <c>new CD3DX12_DEPTH_STENCIL_DESC(DEFAULT)</c>
        /// </summary>
        public static DepthStencilDesc Default { get; } = new DepthStencilDesc
        {
            EnableDepthTesting = true,
            DepthWriteMask = DepthWriteMask.All,
            DepthComparison = Comparison.LessThan,
            EnableStencilTesting = false,
            StencilReadMask = Windows.D3D12_DEFAULT_STENCIL_READ_MASK,
            StencilWriteMask = Windows.D3D12_DEFAULT_STENCIL_WRITE_MASK,
            FrontFace = StencilFuncDesc.Default,
            BackFace = StencilFuncDesc.Default
        };

        /// <summary>
        /// The default <see cref="DepthStencilDesc"/>. This correspends
        /// to <c>new CD3DX12_DEPTH_STENCIL_DESC(DEFAULT)</c>
        /// </summary>
        public static DepthStencilDesc DisableDepthStencil { get; } = new DepthStencilDesc
        {
            EnableDepthTesting = false,
            DepthWriteMask = DepthWriteMask.Zero,
            DepthComparison = Comparison.True,
            EnableStencilTesting = false,
            StencilReadMask = 0,
            StencilWriteMask = 0,
            FrontFace = StencilFuncDesc.Default,
            BackFace = StencilFuncDesc.Default
        };

        [FieldOffset(0)]
        internal AlignedSubobjectType<D3D12_DEPTH_STENCIL_DESC1> Type;

        [FieldOffset(0)]
        internal nuint _Pad;

        /// <summary>
        /// Whether depth testing should occur
        /// </summary>
        public bool EnableDepthTesting { get => Helpers.Int32ToBool(Type.Inner.DepthEnable); set => Type.Inner.DepthEnable = Helpers.BoolToInt32(value); }


        /// <summary>
        /// Whether dynamic depth bounds testing should occur. Set these bounds via <see cref="GraphicsContext.SetDepthsBounds(float, float)"/>
        /// </summary>
        public bool EnableDepthBoundsTesting { get => Helpers.Int32ToBool(Type.Inner.DepthBoundsTestEnable); set => Type.Inner.DepthBoundsTestEnable = Helpers.BoolToInt32(value); }

        /// <summary>
        /// The <see cref="DepthWriteMask"/>
        /// </summary>
        public DepthWriteMask DepthWriteMask { get => (DepthWriteMask)Type.Inner.DepthWriteMask; set => Type.Inner.DepthWriteMask = (D3D12_DEPTH_WRITE_MASK)value; }

        /// <summary>
        /// The function to use when comparing depth data against other depth data
        /// </summary>
        public Comparison DepthComparison { get => (Comparison)Type.Inner.DepthFunc; set => Type.Inner.DepthFunc = (D3D12_COMPARISON_FUNC)value; }

        /// <summary>
        /// Whether stencil testing should occur
        /// </summary>
        public bool EnableStencilTesting { get => Helpers.Int32ToBool(Type.Inner.StencilEnable); set => Type.Inner.StencilEnable = Helpers.BoolToInt32(value); }

        /// <summary>
        /// Identify a portion of the depth-stencil buffer for reading stencil data
        /// </summary>
        public byte StencilReadMask { get => Type.Inner.StencilReadMask; set => Type.Inner.StencilReadMask = value; }

        /// <summary>
        /// Identify a portion of the depth-stencil buffer for writing stencil data
        /// </summary>
        public byte StencilWriteMask { get => Type.Inner.StencilWriteMask; set => Type.Inner.StencilWriteMask = value; }

        /// <summary>
        /// The <see cref="StencilFuncDesc"/> describing the operations to occur during stenciling for the front face
        /// </summary>
        public ref StencilFuncDesc FrontFace => ref Unsafe.As<D3D12_DEPTH_STENCILOP_DESC, StencilFuncDesc>(ref MemoryMarshal.GetReference(MemoryMarshal.CreateSpan(ref Type.Inner.FrontFace, 0)));

        /// <summary>
        /// The <see cref="StencilFuncDesc"/> describing the operations to occur during stenciling for the back face
        /// </summary>
        public ref StencilFuncDesc BackFace => ref Unsafe.As<D3D12_DEPTH_STENCILOP_DESC, StencilFuncDesc>(ref MemoryMarshal.GetReference(MemoryMarshal.CreateSpan(ref Type.Inner.BackFace, 0)));
    }
}
