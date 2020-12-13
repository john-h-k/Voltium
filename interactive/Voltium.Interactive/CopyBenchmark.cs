using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Diagnostics.Tracing.Parsers.Clr;
using Voltium.Core;
using Voltium.Core.Contexts;
using Voltium.Core.Devices;
using Voltium.Core.Memory;
using Voltium.Core.Queries;
using Buffer = Voltium.Core.Memory.Buffer;

namespace Voltium.Interactive
{
    internal static class CopyBenchmark
    {
        private static bool Commit => false;
        private static uint BuffLength => 4096 * 4096 * 4;
        private static uint TexLength => 4096;

        private static uint NumCopies = 50;

        private static GraphicsDevice _device = null!;

        private static QueryHeap _copyQueryHeap;
        private static QueryHeap _queryHeap;

        private static Buffer _sourceBuffer, _destBuffer;
        private static Texture _sourceTexture, _destTexture;

        private static Buffer _queryDestination;

        public static void _Main()
        {
#if DEBUG
            var layer = DebugLayerConfiguration.Debug;
#else
            var layer = DebugLayerConfiguration.None;
#endif

            _device = GraphicsDevice.Create(FeatureLevel.GraphicsLevel11_0, null, layer);

            var flags = Commit ? AllocFlags.ForceAllocateComitted : AllocFlags.ForceAllocateNotComitted;
            _sourceBuffer = _device.Allocator.AllocateBuffer(BuffLength, MemoryAccess.GpuOnly);
            _destBuffer = _device.Allocator.AllocateBuffer(BuffLength, MemoryAccess.GpuOnly);

            var texDesc = TextureDesc.CreateShaderResourceDesc(DataFormat.R8G8B8A8UInt, TextureDimension.Tex2D, TexLength, TexLength);
            texDesc.MipCount = 1;
            _sourceTexture = _device.Allocator.AllocateTexture(texDesc, ResourceState.CopySource, flags);
            _destTexture = _device.Allocator.AllocateTexture(texDesc, ResourceState.Common, flags);

            var contexts = new (CopyContext, CopyContext, ulong)[]
            {
                (_device.BeginCopyContext(), _device.BeginCopyContext(),  _device.QueueFrequency(ExecutionContext.Copy)),
                (_device.BeginComputeContext(), _device.BeginComputeContext(),  _device.QueueFrequency(ExecutionContext.Compute)),
                (_device.BeginGraphicsContext(), _device.BeginGraphicsContext(),  _device.QueueFrequency(ExecutionContext.Graphics))
            };

            int numQueries = contexts.Length * 4;

            _copyQueryHeap = _device.CreateQueryHeap(QueryHeapType.CopyTimestamp, 2 * numQueries);
            _queryHeap = _device.CreateQueryHeap(QueryHeapType.GraphicsOrComputeTimestamp, 2 * numQueries);
            _queryDestination = _device.Allocator.AllocateReadbackBuffer<ulong>(numQueries, flags);

            ulong bufferSize = BuffLength;
            _device.GetTextureInformation(texDesc, out ulong textureSize, out _);

            int i = 0;
            foreach (var (buffCopy, texCopy, frequency) in contexts)
            {
                DoCopy(buffCopy, c => c.CopyResource(_sourceBuffer, _destBuffer), i);
                i += 2;
                DoCopy(texCopy, c => c.CopyResource(_sourceTexture, _destTexture), i);
                i += 2;

                var queries = _queryDestination.AsSpan<TimestampQuery>();
                var buffTime = TimestampQuery.Interval(frequency, queries[i - 4], queries[i - 3]) / NumCopies;
                var texTime = TimestampQuery.Interval(frequency, queries[i - 2], queries[i - 1]) / NumCopies;

                Console.WriteLine($"{buffCopy.GetType().Name} - Buffer copy: {ToGigabytes(bufferSize) / buffTime.TotalSeconds } GB/s");
                Console.WriteLine($"{texCopy.GetType().Name} - Texture copy: {ToGigabytes(textureSize) / texTime.TotalSeconds} GB/s");
            }

            static double ToGigabytes(ulong bytes) => bytes / (1024.0 * 1024 * 1024);
        }

        private static void DoCopy(CopyContext ctx, Action<CopyContext> copy, int queryIndex)
        {
            var queryHeap = ctx is ComputeContext ? _queryHeap : _copyQueryHeap;

            ctx.QueryTimestamp(queryHeap, (uint)queryIndex);
            for (int i = 0; i < NumCopies; i++)
            {
                copy(ctx);
            }
            ctx.QueryTimestamp(queryHeap, (uint)queryIndex + 1);

            ctx.ResolveQuery(queryHeap, QueryType.Timestamp, queryIndex..(queryIndex + 2), _queryDestination, (uint)(queryIndex * sizeof(ulong)));

            ctx.Close();
            _device.Execute(ctx).Block();
        }
    }
}
