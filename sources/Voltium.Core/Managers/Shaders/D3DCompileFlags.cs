using System;
using System.Runtime.InteropServices;

namespace Voltium.Core.Managers
{
    /// <summary>
    /// The flags used by <see cref="ShaderManager"/> and the D3D compiler
    /// </summary>
    [Flags]
    public enum D3DCompileFlags : uint
    {
        /// <summary>
        /// Directs the compiler to insert debug file/line/type/symbol information into the output code.
        /// </summary>
        D3DCOMPILE_DEBUG = 1 << 0,

        /// <summary>
        /// Directs the compiler not to validate the generated code against known capabilities and constraints. We recommend that you use this constant only with shaders that have been successfully compiled in the past. DirectX always validates shaders before it sets them to a device.
        /// </summary>
        D3DCOMPILE_SKIP_VALIDATION = 1 << 1,

        /// <summary>
        /// Directs the compiler to skip optimization steps during code generation. We recommend that you set this constant for debug purposes only.
        /// </summary>
        D3DCOMPILE_SKIP_OPTIMIZATION = 1 << 2,

        /// <summary>
        /// Directs the compiler to pack matrices in row-major order on input and output from the shader.
        /// </summary>
        D3DCOMPILE_PACK_MATRIX_ROW_MAJOR = 1 << 3,

        /// <summary>
        /// Directs the compiler to pack matrices in column-major order on input and output from the shader. This type of packing is generally more efficient because a series of dot-products can then perform vector-matrix multiplication.
        /// </summary>
        D3DCOMPILE_PACK_MATRIX_COLUMN_MAJOR = 1 << 4,

        /// <summary>
        /// Directs the compiler to perform all computations with partial precision. If you set this constant, the compiled code might run faster on some hardware.
        /// </summary>
        D3DCOMPILE_PARTIAL_PRECISION = 1 << 5,

        /// <summary>
        /// Directs the compiler to compile a vertex shader for the next highest shader profile. This constant turns debugging on and optimizations off.
        /// </summary>
        D3DCOMPILE_FORCE_VS_SOFTWARE_NO_OPT = 1 << 6,

        /// <summary>
        /// Directs the compiler to compile a pixel shader for the next highest shader profile. This constant also turns debugging on and optimizations off.
        /// </summary>
        D3DCOMPILE_FORCE_PS_SOFTWARE_NO_OPT = 1 << 7,

        /// <summary>
        /// Directs the compiler to disable Preshaders. If you set this constant, the compiler does not pull out static expression for evaluation.
        /// </summary>
        D3DCOMPILE_NO_PRESHADER = 1 << 8,

        /// <summary>
        /// Directs the compiler to not use flow-control constructs where possible.
        /// </summary>
        D3DCOMPILE_AVOID_FLOW_CONTROL = 1 << 9,

        /// <summary>
        /// Directs the compiler to use flow-control constructs where possible.
        /// </summary>
        D3DCOMPILE_PREFER_FLOW_CONTROL = 1 << 10,

        /// <summary>
        /// Forces strict compile, which might not allow for legacy syntax.
        /// By default, the compiler disables strictness on deprecated syntax.
        /// </summary>
        D3DCOMPILE_ENABLE_STRICTNESS = 1 << 11,

        /// <summary>
        /// Directs the compiler to enable older shaders to compile to 5_0 targets.
        /// </summary>
        D3DCOMPILE_ENABLE_BACKWARDS_COMPATIBILITY = 1 << 12,

        /// <summary>
        /// Forces the IEEE strict compile.
        /// </summary>
        D3DCOMPILE_IEEE_STRICTNESS = 1 << 13,

        /// <summary>
        ///Directs the compiler to use the lowest optimization level.If you set this constant, the compiler might produce slower code but produces the code more quickly. Set this constant when you develop the shader iteratively.
        ///
        /// </summary>
        D3DCOMPILE_OPTIMIZATION_LEVEL0 = 1 << 14,

        /// <summary>
        /// Directs the compiler to use the second lowest optimization level.
        /// </summary>
        D3DCOMPILE_OPTIMIZATION_LEVEL1 = 0,

        /// <summary>
        /// Directs the compiler to use the second highest optimization level.
        /// </summary>
        D3DCOMPILE_OPTIMIZATION_LEVEL2 = (1 << 14) | (1 << 15),

        /// <summary>
        /// Directs the compiler to use the highest optimization level. If you set this constant, the compiler produces the best possible code but might take significantly longer to do
        /// so.Set this constant for final builds of an application when performance is the most important factor.
        /// </summary>
        D3DCOMPILE_OPTIMIZATION_LEVEL3 = 1 << 15,

        /// <summary>
        /// Directs the compiler to treat all warnings as errors when it compiles the shader code.We recommend that you use this constant for new shader code, so that you can resolve all warnings and lower the number of hard - to - find code defects.
        /// </summary>
        D3DCOMPILE_WARNINGS_ARE_ERRORS = 1 << 18,

        /// <summary>
        /// Directs the compiler to assume that unordered access views(UAVs) and shader resource views(SRVs) may alias for cs_5_0.
        /// </summary>
        D3DCOMPILE_RESOURCES_MAY_ALIAS = 1 << 19,

        /// <summary>
        /// Directs the compiler to enable unbounded descriptor tables.
        /// </summary>
        D3DCOMPILE_ENABLE_UNBOUNDED_DESCRIPTOR_TABLES = 1 << 20,

        /// <summary>
        /// Directs the compiler to ensure all resources are bound.
        /// </summary>
        D3DCOMPILE_ALL_RESOURCES_BOUND = 1 << 21,

        /// <summary>
        /// Merge unordered access view (UAV) slots in the secondary data that the pSecondaryData parameter points to.
        /// </summary>
        D3DCOMPILE_SECDATA_MERGE_UAV_SLOTS = 1 << 29,

        /// <summary>
        /// Preserve template slots in the secondary data that the pSecondaryData parameter points to.
        /// </summary>
        D3DCOMPILE_SECDATA_PRESERVE_TEMPLATE_SLOTS = 1 << 30,

        /// <summary>
        /// Require that templates in the secondary data that the pSecondaryData parameter points to match when the compiler compiles the HLSL code.
        /// </summary>
        D3DCOMPILE_SECDATA_REQUIRE_TEMPLATE_MATCH = 1U << 31
    }

    internal static class D3DCompileFlagsExtensions
    {
        public const int SecDataShift = 29;
        public static uint GetSecDataFlags(D3DCompileFlags flags) => ((uint)flags >> SecDataShift);
        public static uint GetCompileFlags(D3DCompileFlags flags) => ((uint)flags & (0b111 >> 29));
    }
}
