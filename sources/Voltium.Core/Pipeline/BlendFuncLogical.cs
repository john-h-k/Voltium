using static TerraFX.Interop.D3D12_LOGIC_OP;



namespace Voltium.Core.Pipeline
{
    /// <summary>
    /// The logical function used during blending
    /// </summary>
    public enum BlendFuncLogical
    {
        /// <summary>
        /// No logical operation occurs
        /// </summary>
        None = -1,

        /// <summary>
        /// Clears the value  to 0
        /// </summary>
        Clear = D3D12_LOGIC_OP_CLEAR,

        /// <summary>
        /// Sets the value to 1
        /// </summary>
        Set = D3D12_LOGIC_OP_SET,

        /// <summary>
        /// Copies the source - equivalent to 'src'
        /// </summary>
        Copy = D3D12_LOGIC_OP_COPY,

        /// <summary>
        /// Copies the bitwise not'd source - equivalent to '~src'
        /// </summary>
        CopyInverted = D3D12_LOGIC_OP_COPY_INVERTED,

        /// <summary>
        /// Performs no operation - equivalent to 'dest'
        /// </summary>
        Nop = D3D12_LOGIC_OP_NOOP,

        /// <summary>
        /// Performs a bitwise invert - equivalent to '~dest'
        /// </summary>
        Invert = D3D12_LOGIC_OP_INVERT,

        /// <summary>
        /// Performs a bitwise and - equivalent to 'src &amp; dest'
        /// </summary>
        And = D3D12_LOGIC_OP_AND,

        /// <summary>
        /// Performs a bitwise nand - equivalent to '~(src &amp; dest)'
        /// </summary>
        Nand = D3D12_LOGIC_OP_NAND,

        /// <summary>
        /// Performs a bitwise or - equivalent to 'src | dest'
        /// </summary>
        Or = D3D12_LOGIC_OP_OR,

        /// <summary>
        /// Performs a bitwise nor - equivalent to '~(src | dest)'
        /// </summary>
        Nor = D3D12_LOGIC_OP_NOR,

        /// <summary>
        /// Performs a bitwise xor - equivalent to 'src ^ dest'
        /// </summary>
        Xor = D3D12_LOGIC_OP_XOR,

        /// <summary>
        /// Performs an equality test. Sets all bits if the test passes - equivalent to 'src == dest ? -1 : 0'
        /// </summary>
        Equality = D3D12_LOGIC_OP_EQUIV,

        // these 2 are equivalent but we provide aliased names for clarity

        /// <summary>
        /// Performs a bitwise not xo - equivalent to '~(src ^ dest)'
        /// </summary>
        XNor = Equality,

        /// <summary>
        /// Performs a bitwise and on the source and the bitwise not of dest - equivalent to 's &amp; ~d'
        /// </summary>
        AndReversed = D3D12_LOGIC_OP_AND_REVERSE,

        /// <summary>
        /// Performs a bitwise and on the bitwise not of source and dest - equivalent to '~s &amp; d'
        /// </summary>
        AndReversedInverted = D3D12_LOGIC_OP_AND_INVERTED,

        /// <summary>
        /// Performs a bitwise or on the source and the bitwise not of dest - equivalent to 's | ~d'
        /// </summary>
        OrReversed = D3D12_LOGIC_OP_OR_REVERSE,

        /// <summary>
        /// Performs a bitwise or on the bitwise not of source and dest - equivalent to '~s | d'
        /// </summary>
        OrReversedInverted = D3D12_LOGIC_OP_OR_INVERTED
    }
}
