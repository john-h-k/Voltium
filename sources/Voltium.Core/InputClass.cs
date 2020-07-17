using TerraFX.Interop;
using Voltium.Core.Devices.Shaders;

namespace Voltium.Core
{
    /// <summary>
    /// Defines what class of input data a given <see cref="ShaderInput"/> is
    /// </summary>
    public enum InputClass
    {
        /// <summary>
        /// The data is per-vertex
        /// </summary>
        PerVertex = D3D12_INPUT_CLASSIFICATION.D3D12_INPUT_CLASSIFICATION_PER_VERTEX_DATA,

        /// <summary>
        /// The data is per-instance
        /// </summary>
        PerInstance = D3D12_INPUT_CLASSIFICATION.D3D12_INPUT_CLASSIFICATION_PER_INSTANCE_DATA
    }
}
