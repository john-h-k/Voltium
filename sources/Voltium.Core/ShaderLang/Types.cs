using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Voltium.Core.ShaderLang
{
    struct Vector3<T> { }




    abstract class Shader
    {
        protected abstract class ShaderAttribute : Attribute { }

        [Flags]
        protected enum SemanticAccess
        {
            Vertex = 1 << 1,
            Domain = 1 << 2,
            Hull = 1 << 3,
            Geometry = 1 << 4,
            Mesh = 1 << 5,
            Amplification = 1 << 6,
            Pixel = 1 << 7,
            Compute = 1 << 8,

            RasterizerInput = Vertex | Domain | Hull | Geometry,
            GraphicsPipeline = RasterizerInput | Pixel,
            MeshPipeline = Amplification | Mesh | Pixel,

            All = GraphicsPipeline | MeshPipeline | Compute,

            ReadOnly = 1 << 9,
            WriteOnly = 1 << 10,
            ReadWrite = ReadOnly | WriteOnly
        }

        protected sealed class SemanticTypeAttribute : Attribute
        {
            public SemanticTypeAttribute(Type type, SemanticAccess access)
            {

            }


            public SemanticTypeAttribute(Type type, SemanticAccess[] access)
            {

            }
        }

        [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Field | AttributeTargets.ReturnValue)]
        protected abstract class SemanticAttribute : ShaderAttribute { }

        [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
        protected sealed class RasterizerOrderedAttribute : ShaderAttribute { }

        [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
        protected sealed class GloballyCoherentAttribute : ShaderAttribute { }

        [SemanticType(typeof(float),
            new[] {
                SemanticAccess.Vertex | SemanticAccess.WriteOnly,
                (SemanticAccess.RasterizerInput & ~SemanticAccess.Vertex) | SemanticAccess.ReadWrite
            }
        )] protected sealed class SV_ClipDistanceAttribute : SemanticAttribute { }

    }


    abstract partial class ComputeShader : Shader
    {
        [SemanticType(typeof(uint), SemanticAccess.ReadOnly)] protected sealed class SV_GroupIndex : SemanticAttribute { }
        [SemanticType(typeof(Vector3<uint>), SemanticAccess.ReadOnly)] protected sealed class SV_GroupID : SemanticAttribute { }
        [SemanticType(typeof(Vector3<uint>), SemanticAccess.ReadOnly)] protected sealed class SV_GroupThreadID : SemanticAttribute { }
        [SemanticType(typeof(Vector3<uint>), SemanticAccess.ReadOnly)] protected sealed class SV_DispatchThreadID : SemanticAttribute { }
    }

    abstract partial class VertexShader : Shader
    {
        [SemanticType(typeof(uint), SemanticAccess.ReadOnly)] protected sealed class SV_VertexID : SemanticAttribute { }
    }


    abstract partial class GeometryShader : Shader
    {
        [SemanticType(typeof(bool), SemanticAccess.WriteOnly)] protected sealed class SV_IsFrontFaceAttribute : SemanticAttribute { }
        [SemanticType(typeof(float), SemanticAccess.ReadWrite)] protected sealed class SV_CullDistanceAttribute : SemanticAttribute { }
    }

    abstract partial class HullShader : Shader
    {
        [SemanticType(typeof(float), SemanticAccess.ReadWrite)] protected sealed class SV_CullDistanceAttribute : SemanticAttribute { }
    }

    abstract partial class DomainShader : Shader
    {
        [SemanticType(typeof(float), SemanticAccess.ReadWrite)] protected sealed class SV_CullDistanceAttribute : SemanticAttribute { }
    }

    abstract partial class PixelShader : Shader
    {
        [SemanticType(typeof(uint), SemanticAccess.WriteOnly)] protected sealed class SV_StencilRef : SemanticAttribute { }
        [SemanticType(typeof(bool), SemanticAccess.WriteOnly)] protected sealed class SV_IsFrontFaceAttribute : SemanticAttribute { }
        [SemanticType(typeof(Vector4), SemanticAccess.ReadOnly)] protected sealed class SV_PositionAttribute : SemanticAttribute { }
        [SemanticType(typeof(Vector4), SemanticAccess.WriteOnly)] protected sealed class SV_TargetAttribute : SemanticAttribute { }
        [SemanticType(typeof(uint), SemanticAccess.ReadWrite)] protected sealed class SV_PrimitiveID : SemanticAttribute { }
        [SemanticType(typeof(float), SemanticAccess.ReadWrite)] protected sealed class SV_CullDistanceAttribute : SemanticAttribute { }
        [SemanticType(typeof(uint), SemanticAccess.ReadWrite)] protected sealed class SV_CoverageAttribute : SemanticAttribute { }

        protected enum DepthRestriction
        {
            None,
            WriteMustBeGreaterThan,
            WriteMustBeLessThan
        }

        [SemanticType(typeof(float), SemanticAccess.WriteOnly)]
        protected sealed class SV_DepthAttribute : SemanticAttribute
        {
            public SV_DepthAttribute(DepthRestriction depthRestriction = DepthRestriction.None)
            {

            }
        }
    }

    abstract partial class AmplificationShader : Shader { }
    abstract partial class MeshShader : Shader { }


    abstract partial class RayGenerationShader : Shader { }
    abstract partial class RayIntersectionShader : Shader { }
    abstract partial class RayClosestHitShader : Shader { }
    abstract partial class RayAnyHitShader : Shader { }
    abstract partial class RayMissShader : Shader { }
    abstract partial class RayCallableShaders : Shader { }
}
