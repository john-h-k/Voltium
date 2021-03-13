using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Voltium.Common;
using Voltium.Core.Infrastructure;
using Voltium.Core.Memory;

namespace Voltium.Core.Devices
{
    public partial class GraphicsDevice
    {
        public static GraphicsDevice Create<TNativeDevice>(TNativeDevice device) where TNativeDevice : INativeDevice
        {
            return new(device);
        }

        private GraphicsDevice(INativeDevice device) : base(device)
        {
            base.Allocator = new GraphicsAllocator(this);

            if (_device is D3D12NativeDevice d3d12)
            {
                GraphicsQueue = new CommandQueue(new D3D12NativeQueue(d3d12, ExecutionEngine.Graphics), ExecutionEngine.Graphics);
                ComputeQueue = new CommandQueue(new D3D12NativeQueue(d3d12, ExecutionEngine.Compute), ExecutionEngine.Compute);
                CopyQueue = new CommandQueue(new D3D12NativeQueue(d3d12, ExecutionEngine.Copy), ExecutionEngine.Copy);
            }
            else
            {
                throw new PlatformNotSupportedException();
            }

            //DispatchIndirect = CreateIndirectCommand(IndirectArgument.CreateDispatch());
            //DrawIndirect = CreateIndirectCommand(IndirectArgument.CreateDraw());
            //DrawIndexedIndirect = CreateIndirectCommand(IndirectArgument.CreateDrawIndexed());
            //DispatchMeshIndirect = CreateIndirectCommand(IndirectArgument.CreateDispatchMesh());
            //DispatchRaysIndirect = CreateIndirectCommand(IndirectArgument.CreateDispatchRays());
        }
    }
}
