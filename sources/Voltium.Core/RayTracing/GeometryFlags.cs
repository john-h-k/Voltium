﻿using System;
using TerraFX.Interop;

namespace Voltium.Core
{
    [Flags]
    public enum GeometryFlags
    {
        None = D3D12_RAYTRACING_GEOMETRY_FLAGS.D3D12_RAYTRACING_GEOMETRY_FLAG_NONE,
        NoDuplicateAnyHitInvocation = D3D12_RAYTRACING_GEOMETRY_FLAGS.D3D12_RAYTRACING_GEOMETRY_FLAG_NO_DUPLICATE_ANYHIT_INVOCATION,
        Opaque = D3D12_RAYTRACING_GEOMETRY_FLAGS.D3D12_RAYTRACING_GEOMETRY_FLAG_OPAQUE,
    }
}
