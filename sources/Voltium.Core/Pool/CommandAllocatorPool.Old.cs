using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TerraFX.Interop;
using Voltium.Common;
using Voltium.Common.Debugging;
using Voltium.Common.Threading;
using Voltium.Core.D3D12;
using static TerraFX.Interop.D3D12_COMMAND_LIST_TYPE;

namespace Voltium.Core.Pool
{
    internal unsafe class OldCommandAllocatorPool
    {
        private const int DefaultStartCapacity = 4;

        private readonly Queue<ComPtr<ID3D12CommandAllocator>> _allocators = new(DefaultStartCapacity);
        private SpinLock _allocatorsLock = new SpinLock(EnvVars.IsDebug);

        private struct UsedAllocator : IDisposable
        {
#pragma warning disable CS0649
            public FenceMarker Fence;
            public ComPtr<ID3D12CommandAllocator> Allocator;

            public UsedAllocator Move()
            {
                var copy = this;
                copy.Allocator = Allocator.Move();
                return copy;
            }

            public void Dispose()
            {
                Allocator.Dispose();
            }
        }

        private readonly List<UsedAllocator> _usedAllocators = new(DefaultStartCapacity);
        private SpinLock _usedAllocatorLock = new SpinLock(EnvVars.IsDebug);

        private ComPtr<ID3D12Device> _device;

        private static bool _initialized;
        private static readonly object InitLock = new();

        /// <summary>
        /// The single instance of this type. You must call <see cref="Initialize"/> before retrieving the instance
        /// </summary>
        public static OldCommandAllocatorPool Pool
        {
            get
            {
                Guard.Initialized(_initialized);

                // i hit 10,000 LOC on this project around here :). - johnk, 16/05/2020. #corona

                return Value;
            }
        }

        private OldCommandAllocatorPool() { }

        private static readonly OldCommandAllocatorPool Value = new OldCommandAllocatorPool();


        /// <summary>
        /// Rent a <see cref="ID3D12CommandAllocator"/> from the pool
        /// </summary>
        /// <param name="type">The type of the command list.
        /// Note this is not currently supported except as <see cref="D3D12_COMMAND_LIST_TYPE_DIRECT"/> (TODO)</param>
        /// <returns>A new <see cref="RentedCommandAllocator"/></returns>
        public RentedCommandAllocator Rent(ExecutionContext type = ExecutionContext.Graphics)
        {
            if (type != ExecutionContext.Graphics)
            {
                ThrowHelper.ThrowNotImplementedException(); // TODO separate pools with separate types
            }

            using (_allocatorsLock.EnterScoped())
            {
                if (_allocators.TryDequeue(out ComPtr<ID3D12CommandAllocator> allocator))
                {
                    //using var _ = allocator;
                    return new RentedCommandAllocator(allocator.Move());
                }
            }

            return new RentedCommandAllocator(CreateNewAllocator(type));
        }

        /// <summary>
        /// Return
        /// </summary>
        /// <param name="allocator"></param>
        /// <param name="allocatorCanBeRecycledMarker"></param>
        public void Return(RentedCommandAllocator allocator, FenceMarker allocatorCanBeRecycledMarker)
        {
#if false
            using (_usedAllocatorLock.EnterScoped())
            {
                var usedAllocator = new UsedAllocator { Allocator = allocator.MovePtr(), Fence = allocatorCanBeRecycledMarker };
                _usedAllocators.Add(usedAllocator);
            }
#endif
        }

        internal void MarkFenceReached(FenceMarker marker)
        {
            using (_usedAllocatorLock.EnterScoped())
            {
                for (var i = 0; i < _usedAllocators.Count; i++)
                {
                    var pair = _usedAllocators[i];

                    // if we have reached the fence at or after the one the allocator can be recycled at, reuse it
                    if (marker.IsAtOrAfter(pair.Fence))
                    {
                        using (_allocatorsLock.EnterScoped())
                        {
                            _allocators.Enqueue(pair.Move().Allocator);
                            _usedAllocators.RemoveAt(i);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Initialize the single instance of this type
        /// </summary>
        /// <param name="device">The device used to initialize the type</param>
        public static void Initialize(ComPtr<ID3D12Device> device)
        {
            lock (InitLock)
            {
                Debug.Assert(!_initialized);

                _initialized = true;
                Value.CoreInitialize(device);
            }
        }

        private void CoreInitialize(ComPtr<ID3D12Device> device)
        {
            _device = device.Move();

            for (int i = 0; i < DefaultStartCapacity; i++)
            {
                using var allocator = CreateNewAllocator(ExecutionContext.Graphics);
                _allocators.Enqueue(allocator.Move());
            }
        }

        private static int _allocatorCount = 0;

        [MethodImpl(MethodTypes.SlowPath)]
        private ComPtr<ID3D12CommandAllocator> CreateNewAllocator(ExecutionContext type)
        {
            ComPtr<ID3D12CommandAllocator> allocator = default;


            Guard.ThrowIfFailed(_device.Get()->CreateCommandAllocator(
                (D3D12_COMMAND_LIST_TYPE)type,
                allocator.Guid,
                ComPtr.GetVoidAddressOf(&allocator)
            ));

            Console.WriteLine("new command allocator created");

            DirectXHelpers.SetObjectName(allocator.Get(), $"Allocator #{_allocatorCount++}");

            //D3D12DeletionNotification.BreakOnDeletion(allocator);

            return allocator.Move();
        }

        /// <inheritdoc cref="IDisposable"/>
        public void Dispose()
        {
            _device.Dispose();

            // we only read and ComPtr<T>.Dispose is reentrant so not locking is ok
            foreach (ComPtr<ID3D12CommandAllocator> allocator in _allocators)
            {
                allocator.Dispose();
            }

            foreach (UsedAllocator allocator in _usedAllocators)
            {
                allocator.Dispose();
            }
        }

        /// <summary>
        /// no
        /// </summary>
        ~OldCommandAllocatorPool() => Dispose();
    }
}
