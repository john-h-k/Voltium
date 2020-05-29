using System.Diagnostics.CodeAnalysis;
using TerraFX.Interop;
#pragma warning disable 1591

namespace Voltium.Core.d3dx12
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public static class CD3DX12_HEAP_PROPERTIES
    {
        public static D3D12_HEAP_PROPERTIES Create(in D3D12_HEAP_PROPERTIES o)
        {
            return new D3D12_HEAP_PROPERTIES
            {
                Type = o.Type,
                CPUPageProperty = o.CPUPageProperty,
                MemoryPoolPreference = o.MemoryPoolPreference,
                CreationNodeMask = o.CreationNodeMask,
                VisibleNodeMask = o.VisibleNodeMask
            };
        }

        public static D3D12_HEAP_PROPERTIES Create(
            D3D12_CPU_PAGE_PROPERTY cpuPageProperty,
            D3D12_MEMORY_POOL memoryPoolPreference,
            uint creationNodeMask = 1,
            uint nodeMask = 1)
        {
            return new D3D12_HEAP_PROPERTIES
            {
                Type = D3D12_HEAP_TYPE.D3D12_HEAP_TYPE_CUSTOM,
                CPUPageProperty = cpuPageProperty,
                MemoryPoolPreference = memoryPoolPreference,
                CreationNodeMask = creationNodeMask,
                VisibleNodeMask = nodeMask
            };
        }

        public static D3D12_HEAP_PROPERTIES Create(
            D3D12_HEAP_TYPE type,
            uint creationNodeMask = 1,
            uint nodeMask = 1)
        {
            return new D3D12_HEAP_PROPERTIES
            {
                Type = type,
                CPUPageProperty = D3D12_CPU_PAGE_PROPERTY.D3D12_CPU_PAGE_PROPERTY_UNKNOWN,
                MemoryPoolPreference = D3D12_MEMORY_POOL.D3D12_MEMORY_POOL_UNKNOWN,
                CreationNodeMask = creationNodeMask,
                VisibleNodeMask = nodeMask
            };
        }

        public static bool IsCPUAccessible(this D3D12_HEAP_PROPERTIES obj)
        {
            return obj.Type == D3D12_HEAP_TYPE.D3D12_HEAP_TYPE_UPLOAD || obj.Type == D3D12_HEAP_TYPE.D3D12_HEAP_TYPE_READBACK ||
                  (obj.Type == D3D12_HEAP_TYPE.D3D12_HEAP_TYPE_CUSTOM &&
                  (obj.CPUPageProperty == D3D12_CPU_PAGE_PROPERTY.D3D12_CPU_PAGE_PROPERTY_WRITE_COMBINE ||
                   obj.CPUPageProperty == D3D12_CPU_PAGE_PROPERTY.D3D12_CPU_PAGE_PROPERTY_WRITE_BACK));
        }
    }
}
