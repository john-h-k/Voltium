using System;
using System.Collections.Generic;

using System.Text;
using System.Threading.Tasks;
using TerraFX.Interop;
using Voltium.Common;
using Voltium.Core.MetaCommands;

namespace Voltium.Core.Devices
{
    public unsafe partial class ComputeDevice
    {
        private Lazy<MetaCommandDesc[]?>? _metaCommandDescs;

        /// <summary>
        /// The device meta commands, if <see cref="DeviceCreationSettings.EnableMetaCommands"/> was called before device creation
        /// </summary>
        public ReadOnlyMemory<MetaCommandDesc> MetaCommands => _metaCommandDescs?.Value;



        private MetaCommandDesc[]? EnumMetaCommands()
        {
            if (DeviceLevel < SupportedDevice.Device5)
            {
                ThrowHelper.ThrowPlatformNotSupportedException("Current OS does not support ID3D12Device5, which is required for meta commands");
            }

            int numMetaCommands;
            ThrowIfFailed(DevicePointerAs<ID3D12Device5>()->EnumerateMetaCommands((uint*)&numMetaCommands, null));

            var meta = RentedArray<D3D12_META_COMMAND_DESC>.Create(numMetaCommands);

            ThrowIfFailed(DevicePointerAs<ID3D12Device5>()->EnumerateMetaCommands((uint*)&numMetaCommands, Helpers.AddressOf(meta.Value)));

            var descs = new MetaCommandDesc[numMetaCommands];

            for (int i = 0; i < numMetaCommands; i++)
            {
                descs[i] = new MetaCommandDesc(meta.Value[i].Id, new string((char*)meta.Value[i].Name));
            }

            return descs;
        }
    }
}
