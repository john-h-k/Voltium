using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Diagnostics.Tracing.Parsers.Clr;

using TerraFX.Interop;
using static TerraFX.Interop.Windows;

using Voltium.Core;
using Voltium.Core.Contexts;
using Voltium.Core.Devices;
using Voltium.Core.Infrastructure;
using Voltium.Core.Memory;
using Voltium.Core.Queries;
using Buffer = Voltium.Core.Memory.Buffer;
using ExecutionContext = Voltium.Core.ExecutionContext;

namespace Voltium.Interactive
{


    internal unsafe static class MemoryApi
    {
        [DllImport("kernel32")]
        public static extern uint GetLastError();

        [DllImport("kernel32")]
        public static extern nuint GetLargePageMinimum();

        [DllImport("kernel32")]
        public static extern nuint VirtualQuery(
            void* lpAddress,
            MEMORY_BASIC_INFORMATION* lpBuffer,
            nuint dwLength
        );

        [DllImport("kernel32")]
        public static extern int VirtualProtect(
          void* lpAddress,
          nuint dwSize,
          uint flNewProtect,
          uint* lpflOldProtect
        );

        [DllImport("kernel32")]
        public static extern void* VirtualAlloc(
          void* lpAddress,
          nuint dwSize,
          uint flAllocationType,
          uint flProtect
        );
    }

    internal unsafe static class DmaBenchmark
    {
        private static bool Commit => true;
        private static int BuffLength => 4096 * 4096 * 4 * 10;

        private static void* _copyBuffer = AllocateLargePage();// GC.AllocateArray<byte>(BuffLength, pinned: true);
        private static void* _otherCopyBuffer = AllocateLargePage();// GC.AllocateArray<byte>(BuffLength, pinned: true);
        private static void* _otherCopyBufferWriteCombine = AllocateWriteCombined();

        private static uint NumCopies = 5;

        private static GraphicsDevice _device = null!;

        private static QueryHeap _copyQueryHeap;
        private static QueryHeap _queryHeap;

        private static Buffer _uploadBuffer, _readbackBuffer, _defaultBuffer;
        private static Buffer _queryDestination;

        private static Stopwatch _watch = Stopwatch.StartNew();


        private static unsafe void* AllocateLargePage()
        {
            void* p = MemoryApi.VirtualAlloc(null, (nuint)BuffLength, 0x00001000 | 0x00002000 /*| 0x20000000*/, PAGE_READWRITE);

            if (p == null)
            {
                char* lpMsgBuf;
                uint dw = MemoryApi.GetLastError();

                FormatMessageW(
                    FORMAT_MESSAGE_ALLOCATE_BUFFER |
                    FORMAT_MESSAGE_FROM_SYSTEM |
                    FORMAT_MESSAGE_IGNORE_INSERTS,
                    null,
                    dw,
                    LANG_USER_DEFAULT,
                    (ushort*)&lpMsgBuf,
                    0,
                    null
                );

                throw new Win32Exception((int)dw, new string(lpMsgBuf));
            }
            return p;
        }

        private static unsafe void* AllocateWriteCombined()
        {
            void* p = MemoryApi.VirtualAlloc(null, (nuint)BuffLength, 0x00001000 | 0x00002000, PAGE_READWRITE | PAGE_WRITECOMBINE);
            if (p == null)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }
            return p;
        }

        private static unsafe bool IsWriteCombined(void* pPage)
        {
            MEMORY_BASIC_INFORMATION info;
            nuint errno = MemoryApi.VirtualQuery(pPage, &info, (uint)sizeof(MEMORY_BASIC_INFORMATION));
            if (errno == 0)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            return (info.Protect & PAGE_WRITECOMBINE) != 0;
        }

        public static unsafe void _Main()
        {
#if DEBUG
            var layer = DebugLayerConfiguration.Debug;
#else
            var layer = DebugLayerConfiguration.None;
#endif
            _device = GraphicsDevice.Create(FeatureLevel.GraphicsLevel11_0, null, layer);

            var flags = Commit ? AllocFlags.ForceAllocateComitted : AllocFlags.ForceAllocateNotComitted;
            _uploadBuffer = _device.Allocator.AllocateBuffer(BuffLength, MemoryAccess.CpuUpload, allocFlags: flags);
            _readbackBuffer = _device.Allocator.AllocateBuffer(BuffLength, MemoryAccess.CpuReadback, allocFlags: flags);
            _defaultBuffer = _device.Allocator.AllocateBuffer(BuffLength, MemoryAccess.GpuOnly, allocFlags: flags);


            Console.WriteLine(IsWriteCombined(_uploadBuffer.Pointer) ? "" : "Error: Upload heap not write_combined");

            ulong bufferSize = (ulong)BuffLength;

            var copyBuffer = new Span<byte>(_copyBuffer, BuffLength);
            var otherCopyBuffer = new Span<byte>(_otherCopyBuffer, BuffLength);
            var otherCopyBufferWriteCombine = new Span<byte>(_otherCopyBufferWriteCombine, BuffLength);

            Console.WriteLine($"Control CPU copy (WRITE_BACK HOST -> WRITE_BACK HOST): {ToGigabytes((uint)copyBuffer.Length) / DoCpuCopy(copyBuffer, otherCopyBuffer).TotalSeconds} GB/s");
            Console.WriteLine($"Control CPU copy  (WRITE_BACK HOST -> WRITE_COMBINE HOST): {ToGigabytes((uint)copyBuffer.Length) / DoCpuCopy(copyBuffer, otherCopyBufferWriteCombine).TotalSeconds} GB/s");
            Console.WriteLine();
            Console.WriteLine($"CPU copy (WRITE_BACK HOST -> WRITE_COMBINE DEVICE_VISIBLE): {ToGigabytes((uint)copyBuffer.Length) / DoCpuCopy(copyBuffer, _uploadBuffer).TotalSeconds} GB/s");
            Console.WriteLine($"CPU copy (WRITE_BACK HOST -> WRITE_BACK DEVICE_VISIBLE): {ToGigabytes((uint)copyBuffer.Length) / DoCpuCopy(copyBuffer, _readbackBuffer).TotalSeconds} GB/s");
            Console.WriteLine();
            Console.WriteLine($"CPU copy (WRITE_COMBINE DEVICE_VISIBLE -> WRITE_BACK HOST): {ToGigabytes(_uploadBuffer.Length) / DoCpuCopy(_uploadBuffer, copyBuffer).TotalSeconds} GB/s");
            Console.WriteLine($"CPU copy (WRITE_BACK DEVICE_VISIBLE -> WRITE_BACK HOST): {ToGigabytes(_readbackBuffer.Length) / DoCpuCopy(_readbackBuffer, copyBuffer).TotalSeconds} GB/s");
            Console.WriteLine();

            var warmups = new (CopyContext, ExecutionContext)[3]
            {
                (_device.BeginCopyContext(), ExecutionContext.Copy),
                (_device.BeginComputeContext(), ExecutionContext.Compute),
                (_device.BeginGraphicsContext(), ExecutionContext.Graphics)
            };

            var contexts = new (CopyContext, CopyContext, CopyContext, CopyContext, ulong)[]
            {
                (_device.BeginCopyContext(), _device.BeginCopyContext(), _device.BeginCopyContext(),_device.BeginCopyContext(),  _device.QueueFrequency(ExecutionContext.Copy)),
                (_device.BeginComputeContext(), _device.BeginComputeContext(), _device.BeginComputeContext(), _device.BeginComputeContext(),  _device.QueueFrequency(ExecutionContext.Compute)),
                (_device.BeginGraphicsContext(), _device.BeginGraphicsContext(), _device.BeginGraphicsContext(), _device.BeginGraphicsContext(),  _device.QueueFrequency(ExecutionContext.Graphics))
            };

            int numQueries = contexts.Length * 8;

            _copyQueryHeap = _device.CreateQueryHeap(QueryHeapType.CopyTimestamp, 2 * numQueries);
            _queryHeap = _device.CreateQueryHeap(QueryHeapType.GraphicsOrComputeTimestamp, 2 * numQueries);
            _queryDestination = _device.Allocator.AllocateReadbackBuffer<ulong>(numQueries, flags);


            Console.WriteLine("Warming up");

            foreach (var warmup in warmups)
            {
                var upload = _device.Allocator.AllocateUploadBuffer<byte>(BuffLength / 10, flags);
                var readback = _device.Allocator.AllocateReadbackBuffer<byte>(BuffLength / 10, flags);
                var @default = _device.Allocator.AllocateDefaultBuffer<byte>(BuffLength / 10, flags);
                for (var j = 0; j < NumCopies; j++)
                {
                    using (warmup.Item1.ScopedBarrier(stackalloc[] {
                        ResourceBarrier.Transition(@default, ResourceState.CopyDestination, ResourceState.CopySource),
                    }))
                    {
                        warmup.Item1.CopyResource(@default, readback);
                    }
                    warmup.Item1.CopyResource(upload, @default);
                }
                warmup.Item1.Attach(ref upload, ref readback, ref @default);
                warmup.Item1.Close();
            }

            GpuTask.BlockAll(warmups.Select(warmup => _device.Execute(warmup.Item1, warmup.Item2)).ToArray());

            Console.WriteLine("Warmup done...");

            int i = 0;
            foreach (var (_0, _1, _2, _3, frequency) in contexts)
            {
                DoCopy(_0, c => c.CopyResource(_uploadBuffer, _defaultBuffer), i);
                i += 2;
                DoCopy(_1, c => c.CopyResource(_readbackBuffer, _defaultBuffer), i);
                i += 2;
                DoCopy(_2, c => c.CopyResource(_defaultBuffer, _uploadBuffer), i);
                i += 2;
                DoCopy(_3, c => c.CopyResource(_defaultBuffer, _readbackBuffer), i);
                i += 2;

                var queries = _queryDestination.AsSpan<TimestampQuery>();
                var uploadTime = TimestampQuery.Interval(frequency, queries[i - 8], queries[i - 7]) / NumCopies;
                var readbackTime = TimestampQuery.Interval(frequency, queries[i - 6], queries[i - 5]) / NumCopies;
                var uploadTimeFromGpu = TimestampQuery.Interval(frequency, queries[i - 4], queries[i - 3]) / NumCopies;
                var readbackTimeFromGpu = TimestampQuery.Interval(frequency, queries[i - 2], queries[i - 1]) / NumCopies;

                Console.WriteLine($"{_0.GetType().Name} - GPU copy (WRITE_COMBINE DEVICE_VISIBLE -> DEVICE): {ToGigabytes(bufferSize) / uploadTime.TotalSeconds } GB/s");
                Console.WriteLine($"{_1.GetType().Name} - GPU copy (WRITE_BACK DEVICE_VISIBLE -> DEVICE): {ToGigabytes(bufferSize) / readbackTime.TotalSeconds} GB/s");
                Console.WriteLine($"{_2.GetType().Name} - GPU copy (DEVICE -> WRITE_COMBINE DEVICE_VISIBLE): {ToGigabytes(bufferSize) / uploadTimeFromGpu.TotalSeconds } GB/s");
                Console.WriteLine($"{_3.GetType().Name} - GPU copy (DEVICE -> WRITE_BACK DEVICE_VISIBLE): {ToGigabytes(bufferSize) / readbackTimeFromGpu.TotalSeconds} GB/s");
                Console.WriteLine();
            }

            static double ToGigabytes(ulong bytes) => bytes / (1024.0 * 1024 * 1024);
        }

        private static TimeSpan DoCpuCopy(in Buffer source, Span<byte> dest) => DoCpuCopy(source.AsSpan<byte>(), dest);
        private static TimeSpan DoCpuCopy(Span<byte> source, in Buffer dest) => DoCpuCopy(source, dest.AsSpan<byte>());

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static TimeSpan DoCpuCopy(Span<byte> source, Span<byte> dest)
        {
            var start = _watch.Elapsed;
            for (var j = 0; j < NumCopies; j++)
            {
                source.CopyTo(dest);
            }
            return (_watch.Elapsed - start) / NumCopies;
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
