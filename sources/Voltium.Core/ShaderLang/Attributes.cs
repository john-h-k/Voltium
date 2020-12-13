using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Voltium.Core.ShaderLang
{
    struct VertexOut
    {
       // [SV_Position]
        public Vector4 Position;
        public Vector4 Color;
    }

    sealed class HelloWorldVertexShader : VertexShader
    { 
}
    sealed class HelloWorldPixelShader : PixelShader
    {
        [return: SV_Target]
        Vector4 Main([SV_Position] Vector4 position)
        {
            return position;
        }
    }
}


