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
        private D3D12NativeDevice _device;

        /// <summary>
        /// Gets the <see cref="GraphicsDevice"/> for a given <see cref="Adapter"/>
        /// </summary>
        /// <param name="requiredFeatureLevel">The required <see cref="FeatureLevel"/> for device creation</param>
        /// <param name="adapter">The <see cref="Adapter"/> to create the device from, or <see langword="null"/> to use the default adapter</param>
        /// <param name="config">The <see cref="DebugLayerConfiguration"/> for the device, or <see langword="null"/> to use the default</param>
        /// <returns>A <see cref="GraphicsDevice"/></returns>
        public static new GraphicsDevice Create(FeatureLevel requiredFeatureLevel, in Adapter? adapter, DebugLayerConfiguration? config = null)
        {
            if (TryGetDevice(requiredFeatureLevel, adapter ?? DefaultAdapter.Value, out var device))
            {
                if (device is GraphicsDevice graphics)
                {
                    return graphics;
                }

                ThrowHelper.ThrowInvalidOperationException("Cannot create a GraphicsDevice for this adapter as a ComputeDevice was created for this adapter");
            }

            return new GraphicsDevice(requiredFeatureLevel, adapter, config);
        }


        private protected static readonly Lazy<Adapter> DefaultAdapter = new(() =>
        {
            using DeviceFactory.Enumerator factory = new DxgiDeviceFactory().GetEnumerator();
            _ = factory.MoveNext();
            return factory.Current;
        });

        private GraphicsDevice(FeatureLevel level, in Adapter? adapter, DebugLayerConfiguration? config = null) : base(level, adapter, config)
        {
            base.Allocator = new GraphicsAllocator(this);

            //DispatchIndirect = CreateIndirectCommand(IndirectArgument.CreateDispatch());
            //DrawIndirect = CreateIndirectCommand(IndirectArgument.CreateDraw());
            //DrawIndexedIndirect = CreateIndirectCommand(IndirectArgument.CreateDrawIndexed());
            //DispatchMeshIndirect = CreateIndirectCommand(IndirectArgument.CreateDispatchMesh());
            //DispatchRaysIndirect = CreateIndirectCommand(IndirectArgument.CreateDispatchRays());
        }
    }
}
