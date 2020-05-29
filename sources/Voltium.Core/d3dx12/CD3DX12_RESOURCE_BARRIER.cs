using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using TerraFX.Interop;
#pragma warning disable 1591

namespace Voltium.Core.d3dx12
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public static unsafe class CD3DX12_RESOURCE_BARRIER
    {
        public static D3D12_RESOURCE_BARRIER Create(in D3D12_RESOURCE_BARRIER o)
        {
            return o;
        }

        public static D3D12_RESOURCE_BARRIER Transition(
            [In] ID3D12Resource* pResource,
            D3D12_RESOURCE_STATES stateBefore,
            D3D12_RESOURCE_STATES stateAfter,
            uint subresource = D3D12.D3D12_RESOURCE_BARRIER_ALL_SUBRESOURCES,
            D3D12_RESOURCE_BARRIER_FLAGS flags = D3D12_RESOURCE_BARRIER_FLAGS.D3D12_RESOURCE_BARRIER_FLAG_NONE
            )
        {
            return new D3D12_RESOURCE_BARRIER
            {
                Type = D3D12_RESOURCE_BARRIER_TYPE.D3D12_RESOURCE_BARRIER_TYPE_TRANSITION,
                Flags = flags,
                Anonymous =
                {
                    Transition =
                    {
                        pResource = pResource, StateBefore = stateBefore,
                        StateAfter = stateAfter, Subresource = subresource
                    }
                }
            };
        }

        public static D3D12_RESOURCE_BARRIER Aliasing(
            [In] ID3D12Resource* pResourceBefore,
            [In] ID3D12Resource* pResourceAfter)
        {
            return new D3D12_RESOURCE_BARRIER
            {
                Type = D3D12_RESOURCE_BARRIER_TYPE.D3D12_RESOURCE_BARRIER_TYPE_ALIASING,
                Anonymous = { Aliasing = { pResourceBefore = pResourceBefore, pResourceAfter = pResourceAfter } }
            };
        }

        public static D3D12_RESOURCE_BARRIER UAV(
            [In] ID3D12Resource* pResource)
        {
            return new D3D12_RESOURCE_BARRIER
            {
                Type = D3D12_RESOURCE_BARRIER_TYPE.D3D12_RESOURCE_BARRIER_TYPE_UAV,
                Anonymous = { UAV = { pResource = pResource } }
            };
        }
    }
}
