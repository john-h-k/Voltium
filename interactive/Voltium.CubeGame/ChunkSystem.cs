using System;
using System.Runtime.InteropServices;
using Voltium.Core.Devices;
using Voltium.Core.Pipeline;

namespace Voltium.CubeGame
{
    internal class ChunkSystem
    {
        private GraphicsDevice _device;

#pragma warning disable IDE0044, CS0649 // Add readonly modifier
        private Memory<Chunk> _chunks;
#pragma warning restore IDE0044 // Add readonly modifier
        public ReadOnlyMemory<Chunk> Chunks => _chunks;

        public ChunkSystem(GraphicsDevice device)
        {
            _device = device;
        }

       
    }
}
