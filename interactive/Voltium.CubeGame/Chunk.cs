using System;
using System.Numerics;
using System.Runtime.InteropServices;
using Buffer = Voltium.Core.Memory.Buffer;

namespace Voltium.CubeGame
{
    internal struct Chunk
    {
        public ReadOnlyMemory<Block?> Blocks { get; set; }
        public bool NeedsRebuild { get; set; }
    }
}
