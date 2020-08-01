using System;

// ReSharper disable IdentifierTypo

namespace Voltium.Common.Pix
{
#pragma warning disable 649
    internal unsafe struct PIXEventsThreadInfo
    {
        public override string ToString()
        {
            return $"Block: {(nuint)Block: X8}" +
                   $"Destination: {(nuint)Destination: X8}" +
                   $"BiasedLimit: {(nuint)BiasedLimit: X8}";
        }

        public void* Block; // EventsBlockInfo*
        public ulong* BiasedLimit;
        public ulong* Destination;
    }
#pragma warning restore 649
}
