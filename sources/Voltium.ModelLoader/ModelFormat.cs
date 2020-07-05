namespace Voltium.ModelLoading
{
    /// <summary>
    /// The format that a model can be
    /// </summary>
    public enum ModelFormat
    {
        /// <summary>
        /// The custom JSON format supported by voltium for loading models
        /// See https://voltium.org/jsonmodels
        /// </summary>
        VoltiumJson,

        /// <summary>
        /// A wavefront, or obj, geometry file
        /// </summary>
        WavefrontObj,

        /// <inheritdoc cref="WavefrontObj"/>
        Obj = WavefrontObj
    }
}
