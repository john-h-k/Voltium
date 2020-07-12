namespace Voltium.Core.Devices.Shaders
{
    // I think is it OK to not have XML docs for these. They are quite self explanatory
#pragma warning disable CS1591 // XML docs
    public partial struct DxcCompileTarget
    {
        public static DxcCompileTarget Vs_4_0 { get; } = new DxcCompileTarget(ShaderType.Vertex, 4, 0);
        public static DxcCompileTarget Vs_5_0 { get; } = new DxcCompileTarget(ShaderType.Vertex, 5, 0);
        public static DxcCompileTarget Vs_6_0 { get; } = new DxcCompileTarget(ShaderType.Vertex, 6, 0);


        public static DxcCompileTarget Ps_4_0 { get; } = new DxcCompileTarget(ShaderType.Pixel, 4, 0);
        public static DxcCompileTarget Ps_5_0 { get; } = new DxcCompileTarget(ShaderType.Pixel, 5, 0);
        public static DxcCompileTarget Ps_6_0 { get; } = new DxcCompileTarget(ShaderType.Pixel, 6, 0);


        public static DxcCompileTarget Ds_4_0 { get; } = new DxcCompileTarget(ShaderType.Domain, 4, 0);
        public static DxcCompileTarget Ds_5_0 { get; } = new DxcCompileTarget(ShaderType.Domain, 5, 0);
        public static DxcCompileTarget Ds_6_0 { get; } = new DxcCompileTarget(ShaderType.Domain, 6, 0);


        public static DxcCompileTarget Hs_4_0 { get; } = new DxcCompileTarget(ShaderType.Hull, 4, 0);
        public static DxcCompileTarget Hs_5_0 { get; } = new DxcCompileTarget(ShaderType.Hull, 5, 0);
        public static DxcCompileTarget Hs_6_0 { get; } = new DxcCompileTarget(ShaderType.Hull, 6, 0);

        public static DxcCompileTarget Gs_4_0 { get; } = new DxcCompileTarget(ShaderType.Geometry, 4, 0);
        public static DxcCompileTarget Gs_5_0 { get; } = new DxcCompileTarget(ShaderType.Geometry, 5, 0);
        public static DxcCompileTarget Gs_6_0 { get; } = new DxcCompileTarget(ShaderType.Geometry, 6, 0);

        public static DxcCompileTarget Cs_4_0 { get; } = new DxcCompileTarget(ShaderType.Compute, 4, 0);
        public static DxcCompileTarget Cs_5_0 { get; } = new DxcCompileTarget(ShaderType.Compute, 5, 0);
        public static DxcCompileTarget Cs_6_0 { get; } = new DxcCompileTarget(ShaderType.Compute, 6, 0);

        public static DxcCompileTarget Lib_4_0 { get; } = new DxcCompileTarget(ShaderType.Library, 4, 0);
        public static DxcCompileTarget Lib_5_0 { get; } = new DxcCompileTarget(ShaderType.Library, 5, 0);
        public static DxcCompileTarget Lib_6_0 { get; } = new DxcCompileTarget(ShaderType.Library, 6, 0);

        // Update as compile targets change
        public static DxcCompileTarget LatestVersion(ShaderType type) => new DxcCompileTarget(type, 6, 4);
    }
#pragma warning restore CS1591 // XML docs
}
