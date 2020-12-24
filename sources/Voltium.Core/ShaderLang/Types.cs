using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Voltium.Core.ShaderLang
{



    public abstract class ShaderAttribute : Attribute
    {
        internal sealed class SemanticTypeAttribute : Attribute
        {
            public SemanticTypeAttribute(Type type, SemanticAccess access)
            {

            }


            public SemanticTypeAttribute(Type type, SemanticAccess[] access)
            {

            }
        }
    }

    public abstract partial class Shader
    {
        protected static void Unroll(uint maxIterations = uint.MaxValue) => throw new NotImplementedException();
        protected static void Loop() => throw new NotImplementedException();
        protected static void AllowUavConditionExit() => throw new NotImplementedException();


        [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Field | AttributeTargets.ReturnValue)]
        protected class SemanticAttribute : ShaderAttribute
        {
            public SemanticAttribute(string name)
            {

            }

            protected SemanticAttribute() {  }
        }

        [AttributeUsage(AttributeTargets.Method | AttributeTargets.Struct | AttributeTargets.Class)]
        private protected class IntrinsicAttribute : ShaderAttribute
        {
            public IntrinsicAttribute(string? translation = null)
            {

            }
        }

        [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
        protected sealed class RasterizerOrderedAttribute : ShaderAttribute { }

        [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
        protected sealed class GloballyCoherentAttribute : ShaderAttribute { }

        protected sealed class SV_ClipDistanceAttribute : SemanticAttribute { }
        protected sealed class SV_GroupIndex : SemanticAttribute { }
        protected sealed class SV_GroupID : SemanticAttribute { }
        protected sealed class SV_GroupThreadID : SemanticAttribute { }
        protected sealed class SV_DispatchThreadID : SemanticAttribute { }
        protected sealed class SV_IsFrontFaceAttribute : SemanticAttribute { }
        protected sealed class SV_VertexID : SemanticAttribute { }
        protected sealed class SV_CullDistanceAttribute : SemanticAttribute { }
        protected sealed class SV_StencilRef : SemanticAttribute { }
        protected sealed class SV_PositionAttribute : SemanticAttribute { }
        protected sealed class SV_TargetAttribute : SemanticAttribute { }
        protected sealed class SV_PrimitiveID : SemanticAttribute { }
        protected sealed class SV_CoverageAttribute : SemanticAttribute { }

        protected enum DepthRestriction
        {
            None,
            WriteMustBeGreaterThan,
            WriteMustBeLessThan
        }

        protected sealed class SV_DepthAttribute : SemanticAttribute
        {
            public SV_DepthAttribute(DepthRestriction depthRestriction = DepthRestriction.None)
            {

            }
        }

        enum Domain
        {
            Triangle,
            Quad,
            Isoline
        }

        enum Partion
        {
            Integer,
            Pow2,
            FractionalEven,
            FractionalOdd
        }

        enum OutputTopology
        {
            Point,
            Line,
            ClockwiseTriangle,
            CounterClockwiseTriangle
        }




        protected partial class ComputeShaderAttribute : ShaderAttribute
        {
            public ComputeShaderAttribute(int x, int y = 1, int z = 1)
            {

            }
        }

        protected partial class VertexShaderAttribute : ShaderAttribute
        {
        }


        protected partial class GeometryShaderAttribute : ShaderAttribute
        {
            public GeometryShaderAttribute(int maxPrimitiveOutput)
            {
            }
        }

        protected partial class HullShaderAttribute : ShaderAttribute
        {
        }

        protected partial class DomainShaderAttribute : ShaderAttribute
        {
        }

        protected partial class PixelShaderAttribute : ShaderAttribute
        {
        }

        protected partial class AmplificationShaderAttribute : ShaderAttribute { }
        protected partial class MeshShaderAttribute : ShaderAttribute { }


    }
}
