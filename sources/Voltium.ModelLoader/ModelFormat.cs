using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Voltium.ModelLoader
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
