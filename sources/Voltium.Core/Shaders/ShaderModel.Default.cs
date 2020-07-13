namespace Voltium.Core.Devices.Shaders
{
    // I think is it OK to not have XML docs for these. They are quite self explanatory
#pragma warning disable CS1591 // XML docs
    public partial struct ShaderModel
    {
        public static ShaderModel Vs_4_0 { get; } = new ShaderModel(ShaderType.Vertex, 4, 0);
        public static ShaderModel Vs_5_0 { get; } = new ShaderModel(ShaderType.Vertex, 5, 0);
        public static ShaderModel Vs_6_0 { get; } = new ShaderModel(ShaderType.Vertex, 6, 0);


        public static ShaderModel Ps_4_0 { get; } = new ShaderModel(ShaderType.Pixel, 4, 0);
        public static ShaderModel Ps_5_0 { get; } = new ShaderModel(ShaderType.Pixel, 5, 0);
        public static ShaderModel Ps_6_0 { get; } = new ShaderModel(ShaderType.Pixel, 6, 0);


        public static ShaderModel Ds_4_0 { get; } = new ShaderModel(ShaderType.Domain, 4, 0);
        public static ShaderModel Ds_5_0 { get; } = new ShaderModel(ShaderType.Domain, 5, 0);
        public static ShaderModel Ds_6_0 { get; } = new ShaderModel(ShaderType.Domain, 6, 0);


        public static ShaderModel Hs_4_0 { get; } = new ShaderModel(ShaderType.Hull, 4, 0);
        public static ShaderModel Hs_5_0 { get; } = new ShaderModel(ShaderType.Hull, 5, 0);
        public static ShaderModel Hs_6_0 { get; } = new ShaderModel(ShaderType.Hull, 6, 0);

        public static ShaderModel Gs_4_0 { get; } = new ShaderModel(ShaderType.Geometry, 4, 0);
        public static ShaderModel Gs_5_0 { get; } = new ShaderModel(ShaderType.Geometry, 5, 0);
        public static ShaderModel Gs_6_0 { get; } = new ShaderModel(ShaderType.Geometry, 6, 0);

        public static ShaderModel Cs_4_0 { get; } = new ShaderModel(ShaderType.Compute, 4, 0);
        public static ShaderModel Cs_5_0 { get; } = new ShaderModel(ShaderType.Compute, 5, 0);
        public static ShaderModel Cs_6_0 { get; } = new ShaderModel(ShaderType.Compute, 6, 0);

        public static ShaderModel Lib_4_0 { get; } = new ShaderModel(ShaderType.Library, 4, 0);
        public static ShaderModel Lib_5_0 { get; } = new ShaderModel(ShaderType.Library, 5, 0);
        public static ShaderModel Lib_6_0 { get; } = new ShaderModel(ShaderType.Library, 6, 0);

        // Update as compile targets change
        public static ShaderModel LatestVersion(ShaderType type) => new ShaderModel(type, 6, 4);
    }
#pragma warning restore CS1591 // XML docs
}
