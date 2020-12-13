namespace Voltium.Core
{
    /// <summary>
    /// The format of vertices used in the raytracing pipeline
    /// </summary>
    public enum VertexFormat : uint
    {
        R32G32Single = DataFormat.R32G32Single,
        R32G32B32Single = DataFormat.R32G32B32Single,
        R16G16Single = DataFormat.R16G16Single,
        R16G16B16A16Single = DataFormat.R16G16B16A16Single,

        R16G16Normalized = DataFormat.R16G16Normalized,
        R16G16B16A16Normalized = DataFormat.R16G16B16A16Normalized
    }
}
