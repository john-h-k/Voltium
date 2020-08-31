using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BenchmarkDotNet.Attributes;
using TerraFX.Interop;
using Voltium.Core;
using Voltium.Core.Devices;
using Voltium.Core.Memory;


namespace Voltium.Interactive
{
    internal unsafe class ThrowExForHrBenchmark
    {
        private ID3D12Resource* pResource;
        private Buffer _buffer;

        [GlobalSetup]
        public void Setup()
        {
            var device = GraphicsDevice.Create(FeatureLevel.GraphicsLevel11_0, null, null);

            _buffer = device.Allocator.AllocateBuffer(16, MemoryAccess.CpuUpload, /* so GetHeapProperties succeeds */ allocFlags: AllocFlags.ForceAllocateNotComitted);
            pResource = _buffer.GetResourcePointer();
        }

        [GlobalCleanup]
        public void Dispose()
        {
            _buffer.Dispose();
        }

        [Benchmark]
        public void NotInlined()
        {
            D3D12_HEAP_PROPERTIES props;
            D3D12_HEAP_FLAGS flags;
            NotInlined_ThrowForExHr(pResource->GetHeapProperties(&props, &flags));
        }

        [Benchmark]
        public void Inlined()
        {
            D3D12_HEAP_PROPERTIES props;
            D3D12_HEAP_FLAGS flags;
            Inlined_ThrowForExHr(pResource->GetHeapProperties(&props, &flags));
        }

        private void ThrowExternalException()
        {
            throw new ExternalException("blah");

        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Inlined_ThrowForExHr(int hr)
        {
            if (hr < 0)
            {
                ThrowExternalException();
            }
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void NotInlined_ThrowForExHr(int hr)
        {
            if (hr < 0)
            {
                throw new ExternalException("blah");
            }
        }
    }
}
