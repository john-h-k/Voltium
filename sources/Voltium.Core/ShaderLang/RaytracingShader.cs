//using System;
//using TerraFX.Interop;

//namespace Voltium.Core.ShaderLang
//{
//    public abstract partial class RaytracingShader : Shader
//    {


//        [NameRemap("RaytracingAccelerationStructure")]
//        protected class RaytracingAccelerationStructure { }


//        protected enum CandidateType
//        {
//            NonOpaqueTriangle,
//            ProceduralPrimitive
//        }


//        protected enum CommittedType
//        {
//            None,
//            Triangle,
//            ProceduralPrimitive
//        }

//            protected class RayQuery
//            {
//                public RayQuery()
//                {

//                }

//                public void TraceRayInline(
//                    RaytracingAccelerationStructure accelerationStructure,
//                    RayDesc ray,
//                    uint instanceInclusionMask,
//                    TraceRayFlags flags = TraceRayFlags.None
//                ) => throw null!;

//                public bool Proceed() => throw null!;

//                public void Abort() => throw null!;

//                public CommittedType CommittedType => throw null!;
//            }




//        protected enum TraceRayFlags : uint
//        {
//            None = D3D12_RAY_FLAGS.D3D12_RAY_FLAG_NONE,
//            ForceOpaque = D3D12_RAY_FLAGS.D3D12_RAY_FLAG_FORCE_OPAQUE,
//            ForceNonOpaque = D3D12_RAY_FLAGS.D3D12_RAY_FLAG_FORCE_NON_OPAQUE,
//            AcceptFirstHitAndEndSearch = D3D12_RAY_FLAGS.D3D12_RAY_FLAG_ACCEPT_FIRST_HIT_AND_END_SEARCH,
//            SkipClosestHitShader = D3D12_RAY_FLAGS.D3D12_RAY_FLAG_SKIP_CLOSEST_HIT_SHADER,
//            CullBackFacingTriangles = D3D12_RAY_FLAGS.D3D12_RAY_FLAG_CULL_BACK_FACING_TRIANGLES,
//            CullFrontFacingTriangles = D3D12_RAY_FLAGS.D3D12_RAY_FLAG_CULL_FRONT_FACING_TRIANGLES,
//            CullOpaque = D3D12_RAY_FLAGS.D3D12_RAY_FLAG_CULL_OPAQUE,
//            CullNonOpaque = D3D12_RAY_FLAGS.D3D12_RAY_FLAG_CULL_NON_OPAQUE,
//            SkipTriangles = D3D12_RAY_FLAGS.D3D12_RAY_FLAG_SKIP_TRIANGLES,
//            SkipProceduralPrimitives = D3D12_RAY_FLAGS.D3D12_RAY_FLAG_SKIP_PROCEDURAL_PRIMITIVES,
//        }

//        protected interface IRayQueryFlag { }
//        protected static class RayQueryFlags
//        {
//            public struct None : IRayQueryFlag { }
//            public struct ForceOpaque : IRayQueryFlag { }
//            public struct ForceNonOpaque : IRayQueryFlag { }
//            public struct AcceptFirstHitAndEndSearch : IRayQueryFlag { }
//            public struct SkipClosestHitShader : IRayQueryFlag { }
//            public struct CullBackFacingTriangles : IRayQueryFlag { }
//            public struct CullFrontFacingTriangles : IRayQueryFlag { }
//            public struct CullOpaque : IRayQueryFlag { }
//            public struct CullNonOpaque : IRayQueryFlag { }
//            public struct SkipTriangles : IRayQueryFlag { }
//            public struct SkipProceduralPrimitives : IRayQueryFlag { }
//        }
//        protected class RayQuery<TFlag0> : RayQuery where TFlag0 : IRayQueryFlag { }
//        protected class RayQuery<TFlag0, TFlag1> : RayQuery where TFlag0 : IRayQueryFlag where TFlag1 : IRayQueryFlag { }
//        protected class RayQuery<TFlag0, TFlag1, TFlag2> : RayQuery where TFlag0 : IRayQueryFlag where TFlag1 : IRayQueryFlag where TFlag2 : IRayQueryFlag { }
//        protected class RayQuery<TFlag0, TFlag1, TFlag2, TFlag3> : RayQuery where TFlag0 : IRayQueryFlag where TFlag1 : IRayQueryFlag where TFlag2 : IRayQueryFlag where TFlag3 : IRayQueryFlag { }
//        protected class RayQuery<TFlag0, TFlag1, TFlag2, TFlag3, TFlag4> : RayQuery where TFlag0 : IRayQueryFlag where TFlag1 : IRayQueryFlag where TFlag2 : IRayQueryFlag where TFlag3 : IRayQueryFlag where TFlag4 : IRayQueryFlag { }
//        protected class RayQuery<TFlag0, TFlag1, TFlag2, TFlag3, TFlag4, TFlag5> : RayQuery where TFlag0 : IRayQueryFlag where TFlag1 : IRayQueryFlag where TFlag2 : IRayQueryFlag where TFlag3 : IRayQueryFlag where TFlag4 : IRayQueryFlag where TFlag5 : IRayQueryFlag { }
//        protected class RayQuery<TFlag0, TFlag1, TFlag2, TFlag3, TFlag4, TFlag5, TFlag6> : RayQuery where TFlag0 : IRayQueryFlag where TFlag1 : IRayQueryFlag where TFlag2 : IRayQueryFlag where TFlag3 : IRayQueryFlag where TFlag4 : IRayQueryFlag where TFlag5 : IRayQueryFlag where TFlag6 : IRayQueryFlag { }
//        protected class RayQuery<TFlag0, TFlag1, TFlag2, TFlag3, TFlag4, TFlag5, TFlag6, TFlag7> : RayQuery where TFlag0 : IRayQueryFlag where TFlag1 : IRayQueryFlag where TFlag2 : IRayQueryFlag where TFlag3 : IRayQueryFlag where TFlag4 : IRayQueryFlag where TFlag5 : IRayQueryFlag where TFlag6 : IRayQueryFlag where TFlag7 : IRayQueryFlag { }
//        protected class RayQuery<TFlag0, TFlag1, TFlag2, TFlag3, TFlag4, TFlag5, TFlag6, TFlag7, TFlag8> : RayQuery where TFlag0 : IRayQueryFlag where TFlag1 : IRayQueryFlag where TFlag2 : IRayQueryFlag where TFlag3 : IRayQueryFlag where TFlag4 : IRayQueryFlag where TFlag5 : IRayQueryFlag where TFlag6 : IRayQueryFlag where TFlag7 : IRayQueryFlag where TFlag8 : IRayQueryFlag { }
//        protected class RayQuery<TFlag0, TFlag1, TFlag2, TFlag3, TFlag4, TFlag5, TFlag6, TFlag7, TFlag8, TFlag9> : RayQuery where TFlag0 : IRayQueryFlag where TFlag1 : IRayQueryFlag where TFlag2 : IRayQueryFlag where TFlag3 : IRayQueryFlag where TFlag4 : IRayQueryFlag where TFlag5 : IRayQueryFlag where TFlag6 : IRayQueryFlag where TFlag7 : IRayQueryFlag where TFlag8 : IRayQueryFlag where TFlag9 : IRayQueryFlag { }



//        [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property)]
//        private protected class TargetsAttribute : ShaderAttribute
//        {
//            public TargetsAttribute(params Type[] targets)
//            {

//            }
//        }

//        protected partial class RayGenerationShaderAttribute : ShaderAttribute { }
//        protected partial class RayIntersectionShaderAttribute : ShaderAttribute { }
//        protected partial class RayClosestHitShaderAttribute : ShaderAttribute { }
//        protected partial class RayAnyHitShaderAttribute : ShaderAttribute { }
//        protected partial class RayMissShaderAttribute : ShaderAttribute { }
//        protected partial class RayCallableShadersAttribute : ShaderAttribute { }
//        protected partial class HitGroupAttribute : ShaderAttribute
//        {
//            public string IntersectionShader = "";
//            public string AnyHitShader = "";
//            public string ClosestHitShader = "";
//        }


//        [AttributeUsage(AttributeTargets.Class)]
//        protected partial class SubobjectAssociation : ShaderAttribute
//        {
//            public SubobjectAssociation(string subobjectName, string[] associations)
//            {

//            }
//        }

//        [AttributeUsage(AttributeTargets.Class)]
//        protected partial class RaytracingPipelineConfigAttribute : ShaderAttribute
//        {
//            public RaytracingPipelineConfigAttribute(uint maxRecursionDepth, uint maxAttributeSize, uint maxPayloadSize)
//            {

//            }
//        }


//        protected struct RayDesc
//        {
//            public Vector3<float> Origin;
//            public float TMin;
//            public Vector3<float> Direction;
//            public float TMax;
//        }

//        protected struct BuiltInTriangleIntersectionAttributes
//        {
//            public Vector2<float> Barycentrics;
//        }


//        [Targets(typeof(RayAnyHitShaderAttribute))]
//        protected void IgnoreHit() => throw null!;

//        [Targets(typeof(RayAnyHitShaderAttribute))]
//        protected void AcceptHitAndEndSearch() => throw null!;


//        [Targets(typeof(RayIntersectionShaderAttribute))]
//        protected bool ReportHit<TAttribute>(out float tHit, out uint hitKind, out TAttribute attributes) => throw null!;

//        [Targets(typeof(RayGenerationShaderAttribute), typeof(RayClosestHitShaderAttribute), typeof(RayMissShaderAttribute), typeof(RayCallableShadersAttribute))]
//        protected void CallShader<TParam>(uint shaderIndex, ref TParam param) => throw null!;

//        [Targets(typeof(RayGenerationShaderAttribute), typeof(RayClosestHitShaderAttribute), typeof(RayMissShaderAttribute))]
//        protected void TraceRay<TPayload>(
//            RaytracingAccelerationStructure accelerationStructure,
//            ref TPayload payload,
//            RayDesc desc,
//            uint instanceInclusionMask,
//            uint rayContributionToHitGroupIndex,
//            uint multiplierForGeometryContributionToHitGroupIndex,
//            uint missShaderIndex,
//            TraceRayFlags flags = TraceRayFlags.None
//        ) => throw null!;

//        protected Vector3<uint> DispatchRaysIndex => throw null!;
//        protected Vector3<uint> DispatchRaysDimensions => throw null!;


//        [Targets(typeof(RayIntersectionShaderAttribute), typeof(RayAnyHitShaderAttribute), typeof(RayClosestHitShaderAttribute), typeof(RayMissShaderAttribute))]
//        protected Vector3<float> WorldRayOrigin => throw null!;
//        [Targets(typeof(RayIntersectionShaderAttribute), typeof(RayAnyHitShaderAttribute), typeof(RayClosestHitShaderAttribute), typeof(RayMissShaderAttribute))]
//        protected Vector3<float> WorldRayDirection => throw null!;

//        [Targets(typeof(RayIntersectionShaderAttribute), typeof(RayAnyHitShaderAttribute), typeof(RayClosestHitShaderAttribute), typeof(RayMissShaderAttribute))]
//        protected float RayTMin => throw null!;
//        [Targets(typeof(RayIntersectionShaderAttribute), typeof(RayAnyHitShaderAttribute), typeof(RayClosestHitShaderAttribute), typeof(RayMissShaderAttribute))]
//        protected float RayTCurrent => throw null!;
//        [Targets(typeof(RayIntersectionShaderAttribute), typeof(RayAnyHitShaderAttribute), typeof(RayClosestHitShaderAttribute), typeof(RayMissShaderAttribute))]
//        protected TraceRayFlags RayFlags => throw null!;

//        [Targets(typeof(RayIntersectionShaderAttribute), typeof(RayAnyHitShaderAttribute), typeof(RayClosestHitShaderAttribute))]
//        protected uint InstanceIndex => throw null!;

//        [Targets(typeof(RayIntersectionShaderAttribute), typeof(RayAnyHitShaderAttribute), typeof(RayClosestHitShaderAttribute))]
//        protected uint InstanceID => throw null!;

//        [Targets(typeof(RayIntersectionShaderAttribute), typeof(RayAnyHitShaderAttribute), typeof(RayClosestHitShaderAttribute))]
//        protected uint GeometryIndex => throw null!;

//        [Targets(typeof(RayIntersectionShaderAttribute), typeof(RayAnyHitShaderAttribute), typeof(RayClosestHitShaderAttribute))]
//        protected uint PrimitiveIndex => throw null!;

//        [Targets(typeof(RayIntersectionShaderAttribute), typeof(RayAnyHitShaderAttribute), typeof(RayClosestHitShaderAttribute))]
//        protected Vector3<float> ObjectRayOrigin => throw null!;

//        [Targets(typeof(RayIntersectionShaderAttribute), typeof(RayAnyHitShaderAttribute), typeof(RayClosestHitShaderAttribute))]
//        protected Vector3<float> ObjectRayDirection => throw null!;

//        [Targets(typeof(RayIntersectionShaderAttribute), typeof(RayAnyHitShaderAttribute), typeof(RayClosestHitShaderAttribute))]
//        protected Matrix3x4<float> ObjectToWorld3x4 => throw null!;
//        [Targets(typeof(RayIntersectionShaderAttribute), typeof(RayAnyHitShaderAttribute), typeof(RayClosestHitShaderAttribute))]
//        protected Matrix4x3<float> ObjectToWorld4x3 => throw null!;
//        [Targets(typeof(RayIntersectionShaderAttribute), typeof(RayAnyHitShaderAttribute), typeof(RayClosestHitShaderAttribute))]
//        protected Matrix3x4<float> WorldToObject3x4 => throw null!;
//        [Targets(typeof(RayIntersectionShaderAttribute), typeof(RayAnyHitShaderAttribute), typeof(RayClosestHitShaderAttribute))]
//        protected Matrix4x3<float> WorldToObject4x3 => throw null!;

//        [Targets(typeof(RayAnyHitShaderAttribute), typeof(RayClosestHitShaderAttribute))]
//        protected uint HitKind => throw null!;

//        protected const uint HitKindTriangleFrontFace = 254;
//        protected const uint HitKindTriangleBackFace = 255;
//    }
//}
