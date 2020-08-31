using System;
using System.Collections.Generic;

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TerraFX.Interop;
using Voltium.Allocators;
using Voltium.Common;
using Voltium.Core.Memory;

namespace Voltium.Core.Devices
{
    public unsafe partial class ComputeDevice
    {
        // this fence is specifically used by the device for MakeResidentAsync. unrelated to queue fences
        private UniqueComPtr<ID3D12Fence> _residencyFence;
        private ulong _lastFenceSignal;

        /// <summary>
        /// Indicates whether calls to <see cref="MakeResidentAsync{T}(T)"/> and <see cref="MakeResidentAsync{T}(T)"/> can succeed
        /// </summary>
        public bool CanMakeResidentAsync => DeviceLevel >= SupportedDevice.Device3;

        private const string Error_CantMakeResidentAsync = "Cannot MakeResidentAsync on a system that does not support ID3D12Device3.\n" +
            "Check CanMakeResidentAsync to determine if you can call MakeResidentAsync without it failing";


        /// <summary>
        /// Asynchronously makes <paramref name="evicted"/> resident on the device
        /// </summary>
        /// <typeparam name="T">The type of the evicted resource</typeparam>
        /// <param name="evicted">The <typeparamref name="T"/> to make resident</param>
        /// <returns>A <see cref="GpuTask"/> that can be used to work out when the resource is resident</returns>
        public GpuTask MakeResidentAsync<T>(T evicted) where T : IEvictable
        {
            if (DeviceLevel < SupportedDevice.Device3)
            {
                ThrowHelper.ThrowNotSupportedException(Error_CantMakeResidentAsync);
            }

            var newValue = Interlocked.Increment(ref _lastFenceSignal);
            var pageable = evicted.GetPageable();

            ThrowIfFailed(DevicePointerAs<ID3D12Device3>()->EnqueueMakeResident(
                D3D12_RESIDENCY_FLAGS.D3D12_RESIDENCY_FLAG_DENY_OVERBUDGET,
                1,
                &pageable,
                _residencyFence.Ptr,
                newValue
            ));

            return new GpuTask(this, _residencyFence, newValue);
        }

        /// <summary>
        /// Asynchronously makes <paramref name="evicted"/> resident on the device
        /// </summary>
        /// <typeparam name="T">The type of the evicted resourcse</typeparam>
        /// <param name="evicted">The <typeparamref name="T"/>s to make resident</param>
        /// <returns>A <see cref="GpuTask"/> that can be used to work out when the resource is resident</returns>
        public GpuTask MakeResidentAsync<T>(ReadOnlySpan<T> evicted) where T : IEvictable
        {
            if (DeviceLevel < SupportedDevice.Device3)
            {
                ThrowHelper.ThrowNotSupportedException(Error_CantMakeResidentAsync);
            }

            var newValue = Interlocked.Increment(ref _lastFenceSignal);
            // classes will never be blittable to pointer, so this handles that
            if (default(T)?.IsBlittableToPointer ?? false)
            {
                fixed (void* pEvictables = &Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(evicted)))
                {
                    ThrowIfFailed(DevicePointerAs<ID3D12Device3>()->EnqueueMakeResident(
                        D3D12_RESIDENCY_FLAGS.D3D12_RESIDENCY_FLAG_DENY_OVERBUDGET,
                        (uint)evicted.Length,
                        (ID3D12Pageable**)pEvictables,
                        _residencyFence.Ptr,
                        newValue
                    ));
                }
            }
            else
            {

                if (StackSentinel.SafeToStackallocPointers(evicted.Length))
                {
                    ID3D12Pageable** pEvictables = stackalloc ID3D12Pageable*[evicted.Length];
                    for (int i = 0; i < evicted.Length; i++)
                    {
                        pEvictables[i] = evicted[i].GetPageable();
                    }

                    ThrowIfFailed(DevicePointerAs<ID3D12Device3>()->EnqueueMakeResident(
                        D3D12_RESIDENCY_FLAGS.D3D12_RESIDENCY_FLAG_DENY_OVERBUDGET,
                        (uint)evicted.Length,
                        pEvictables,
                        _residencyFence.Ptr,
                        newValue
                    ));
                }
                else
                {
                    using var pool = RentedArray<nuint>.Create(evicted.Length, PinnedArrayPool<nuint>.Default);

                    for (int i = 0; i < evicted.Length; i++)
                    {
                        pool.Value[i] = (nuint)evicted[i].GetPageable();
                    }

                    ThrowIfFailed(DevicePointerAs<ID3D12Device3>()->EnqueueMakeResident(
                        D3D12_RESIDENCY_FLAGS.D3D12_RESIDENCY_FLAG_DENY_OVERBUDGET,
                        (uint)evicted.Length,
                        (ID3D12Pageable**)Unsafe.AsPointer(ref MemoryMarshal.GetArrayDataReference(pool.Value)),
                        _residencyFence.Ptr,
                        newValue
                    ));
                }
            }

            return new GpuTask(this, _residencyFence, newValue);
        }

        // MakeResident is 34th member of vtable
        // Evict is 35th
        private delegate* stdcall<uint, ID3D12Pageable**, int> MakeResidentFunc => (delegate* stdcall<uint, ID3D12Pageable**, int>)DevicePointer->lpVtbl[34];
        private delegate* stdcall<uint, ID3D12Pageable**, int> EvictFunc => (delegate* stdcall<uint, ID3D12Pageable**, int>)DevicePointer->lpVtbl[35];

        // take advantage of the fact make resident and evict have same sig to reduce code duplication

        /// <summary>
        /// Synchronously makes <paramref name="evicted"/> resident on the device
        /// </summary>
        /// <typeparam name="T">The type of the evicted resource</typeparam>
        /// <param name="evicted">The <typeparamref name="T"/> to make resident</param>
        public void MakeResident<T>(T evicted) where T : IEvictable
            => ChangeResidency(MakeResidentFunc, evicted);

        /// <summary>
        /// Synchronously makes <paramref name="evicted"/> resident on the device
        /// </summary>
        /// <typeparam name="T">The type of the evicted resource</typeparam>
        /// <param name="evicted">The <typeparamref name="T"/>s to make resident</param>
        public void MakeResident<T>(ReadOnlySpan<T> evicted) where T : IEvictable
            => ChangeResidency(MakeResidentFunc, evicted);

        /// <summary>
        /// Indicates <paramref name="evicted"/> can be evicted if necessary
        /// </summary>
        /// <typeparam name="T">The type of the evictable resource</typeparam>
        /// <param name="evicted">The <typeparamref name="T"/> to mark as evictable</param>
        public void Evict<T>(T evicted) where T : IEvictable
            => ChangeResidency(EvictFunc, evicted);

        /// <summary>
        /// Indicates <paramref name="evicted"/> can be evicted if necessary
        /// </summary>
        /// <typeparam name="T">The type of the evictable resource</typeparam>
        /// <param name="evicted">The <typeparamref name="T"/>s to mark as evictable</param>
        public void Evict<T>(ReadOnlySpan<T> evicted) where T : IEvictable
            => ChangeResidency(EvictFunc, evicted);

        private void ChangeResidency<T>(delegate* stdcall<uint, ID3D12Pageable**, int> changeFunc, T evictable) where T : IEvictable
        {
            var pageable = evictable.GetPageable();
            ThrowIfFailed(changeFunc(1, &pageable));
        }

        private void ChangeResidency<T>(delegate* stdcall<uint, ID3D12Pageable**, int> changeFunc, ReadOnlySpan<T> evictables) where T : IEvictable
        {
            // classes will never be blittable to pointer, so this handles that
            if (default(T)?.IsBlittableToPointer ?? false)
            {
                fixed (void* pEvictables = &Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(evictables)))
                {
                    ThrowIfFailed(changeFunc((uint)evictables.Length, (ID3D12Pageable**)pEvictables));
                }
            }
            else
            {

                if (StackSentinel.SafeToStackallocPointers(evictables.Length))
                {
                    ID3D12Pageable** pEvictables = stackalloc ID3D12Pageable*[evictables.Length];
                    for (int i = 0; i < evictables.Length; i++)
                    {
                        pEvictables[i] = evictables[i].GetPageable();
                    }

                    ThrowIfFailed(changeFunc((uint)evictables.Length, pEvictables));
                }
                else
                {
                    using var pool = RentedArray<nuint>.Create(evictables.Length, PinnedArrayPool<nuint>.Default);

                    for (int i = 0; i < evictables.Length; i++)
                    {
                        pool.Value[i] = (nuint)evictables[i].GetPageable();
                    }

                    ThrowIfFailed(changeFunc((uint)evictables.Length, (ID3D12Pageable**)Unsafe.AsPointer(ref MemoryMarshal.GetArrayDataReference(pool.Value))));
                }
            }
        }
    }
}
