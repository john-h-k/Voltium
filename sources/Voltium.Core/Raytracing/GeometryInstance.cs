using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Text;
using System.Threading.Tasks;
using TerraFX.Interop;

namespace Voltium.Core.Raytracing
{
    public unsafe struct GeometryInstance
    {
        private D3D12_RAYTRACING_INSTANCE_DESC Desc;

        public Matrix4x4 Transform
        {
            get
            {
                var val = Unsafe.As<float, Matrix4x4>(ref Desc.Transform[0]);
                Unsafe.As<float, Vector128<float>>(ref val.M41) = Vector128<float>.Zero;
                return val;
            }

            set
            {
                Unsafe.As<float, Vector256<float>>(ref Desc.Transform[0]) = Unsafe.As<float, Vector256<float>>(ref value.M11);
                Unsafe.As<float, Vector128<float>>(ref Desc.Transform[8]) = Unsafe.As<float, Vector128<float>>(ref value.M31);
            }
        }

        public uint InstanceID { get => Desc.InstanceID; set => Desc.InstanceID = value; }
        public uint InstanceMask { get => Desc.InstanceMask; set => Desc.InstanceMask = value; }
        public uint InstanceContributionToHitGroupIndex { get => Desc.InstanceContributionToHitGroupIndex; set => Desc.InstanceContributionToHitGroupIndex = value; }
        public uint Flags { get => Desc.Flags; set => Desc.Flags = value; }

        public ulong AccelerationStructure { get => Desc.AccelerationStructure; set => Desc.AccelerationStructure = value; }
    }
}
