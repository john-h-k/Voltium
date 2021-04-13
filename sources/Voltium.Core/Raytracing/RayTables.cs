using TerraFX.Interop;
using Voltium.Core.Contexts;
using Buffer = Voltium.Core.Memory.Buffer;

namespace Voltium.Core
{
    /// <summary>
    /// Describes the parameters used in a call to <see cref="ComputeContext.DispatchRays(uint, uint, uint, in RayTables)"/>
    /// </summary>
    public struct RayTables
    {
        internal Buffer RayGenBuffer, MissShaderBuffer, HitGroupBuffer, CallableShaderBuffer;
        internal uint RayGenLength, MissShaderLength, HitGroupLength, CallableShaderLength;
        internal uint MissShaderCount, HitGroupCount, CallableShaderCount;

        internal struct RangeAndStride { public uint Range, Stride; }

        public void SetRayGenerationShaderRecord([RequiresResourceState(ResourceState.NonPixelShaderResource)] in Buffer buffer, uint length)
        {
            RayGenBuffer = buffer;
            RayGenLength = length;
        }

        public void SetMissShaderTable([RequiresResourceState(ResourceState.NonPixelShaderResource)] in Buffer buffer, uint size, uint count)
        {
            MissShaderBuffer = buffer;
            MissShaderLength = size;
            MissShaderCount = count;
        }

        public void SetHitGroupTable([RequiresResourceState(ResourceState.NonPixelShaderResource)] in Buffer buffer, uint size, uint count)
        {
            HitGroupBuffer = buffer;
            HitGroupLength = size;
            HitGroupCount = count;
        }

        public void SetCallableShaderTable([RequiresResourceState(ResourceState.NonPixelShaderResource)] in Buffer buffer, uint size, uint count)
        {
            CallableShaderBuffer = buffer;
            CallableShaderLength = size;
            CallableShaderCount = count;
        }

        /// <summary>
        /// The width of the dispatch
        /// </summary>
        public uint Width { get;  set; }

        /// <summary>
        /// The height of the dispatch
        /// </summary>
        public uint Height { get; set; }

        /// <summary>
        /// The depth of the dispatch
        /// </summary>
        public uint Depth { get; set; }
    }
}
