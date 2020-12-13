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
        /// The device meta commands
        /// </summary>
        public ReadOnlyMemory<MetaCommandDesc> MetaCommands => _metaCommandDescs?.Value;


        private MetaCommandDesc[]? EnumMetaCommands()
        {
            int numMetaCommands;
            ThrowIfFailed(As<ID3D12Device5>()->EnumerateMetaCommands((uint*)&numMetaCommands, null));

            var metas = RentedArray<D3D12_META_COMMAND_DESC>.Create(numMetaCommands);
            var descs = new MetaCommandDesc[numMetaCommands];

            fixed (D3D12_META_COMMAND_DESC* pDesc = metas.Value)
            {
                ThrowIfFailed(As<ID3D12Device5>()->EnumerateMetaCommands((uint*)&numMetaCommands, pDesc));

                for (int i = 0; i < numMetaCommands; i++)
                {
                    ref readonly D3D12_META_COMMAND_DESC meta = ref metas.Value[i];
                    descs[i] = new MetaCommandDesc(meta.Id, new string((char*)meta.Name));
                }
            }

            return descs;
        }
    }
}
