using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Voltium.Common
{
    internal sealed class NopMemoryOwner<T> : IMemoryOwner<T>
    {
        public NopMemoryOwner(Memory<T> memory)
            => Memory = memory;

        public Memory<T> Memory { get; private set; }

        public void Dispose()
        {
        }
    }
}
