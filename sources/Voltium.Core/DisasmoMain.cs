using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TerraFX.Interop;
using Voltium.Core.Devices;
using Voltium.Core.Memory;

namespace Voltium.Core
{
    // used for checking disassembly
    internal static class DisasmoMain
    {
        private static unsafe void Main()
        {
            var config = new DeviceConfiguration
            {
                DebugLayerConfiguration = null,
                RequiredFeatureLevel = FeatureLevel.ComputeLevel1_0
            };
            var device = new ComputeDevice(config, null);

            var desc = new InternalAllocDesc
            {
                Desc = D3D12_RESOURCE_DESC.Buffer(128),
                InitialState = D3D12_RESOURCE_STATES.D3D12_RESOURCE_STATE_COMMON
            };

            device.CreateCommittedResource(&desc);
        }
    }
}
