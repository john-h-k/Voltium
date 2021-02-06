using System;
using System.Collections.Generic;
using System.Text;

namespace Voltium.ShaderCompiler
{
    internal enum WellKnownIdentifier
    {
        StructuredBuffer,
        AppendBuffer,
        ConsumeBuffer,
        RawBuffer,

        WritableStructuredBuffer,
        WritableRawBuffer,

        Texture1D,
        Texture1DArray,
        Texture2D,
        Texture2DArray,
        MultiSampledTexture2D,
        MultiSampledTexture2DArray,
        Texture3D,
        TextureCube,
        TextureCubeArray,

        WritableTexture1D,
        WritableTexture2D,
        WritableTexture3D,
        WritableTextureCube,

        IgnoreHit,
        AcceptHitAndEndSearch,
        ReportHit,
        CallShader,
        TraceRay,

        DispatchRaysIndex,
        DispatchRaysDimensions,
        WorldRayOrigin,
        WorldRayDirection,
        RayTMin,
        RayTCurrent,
        RayFlags,
        InstanceIndex,
        InstanceID,
        GeometryIndex,
        PrimitiveIndex,
        ObjectRayOrigin,
        ObjectRayDirection,
        ObjectToWorld3x4,
        ObjectToWorld4x3,
        WorldToObject3x4,
        WorldToObject4x3,
        HitKind
    }
}
