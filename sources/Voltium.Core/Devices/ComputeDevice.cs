using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using TerraFX.Interop;
using Voltium.Common;
using Voltium.Core.Memory;
using Voltium.Core.Infrastructure;
using Voltium.Core.Devices;
using Voltium.Core.Pipeline;
using ZLogger;
using Voltium.Core.Devices.Shaders;
using Voltium.Core.Pool;

using static TerraFX.Interop.Windows;
using static TerraFX.Interop.D3D12_FEATURE;

using Buffer = Voltium.Core.Memory.Buffer;
using Voltium.Allocators;
using System.Threading;
using System.Diagnostics;
using Voltium.Core.Contexts;

namespace Voltium.Core.Devices
{
    /// <summary>
    /// 
    /// </summary>
    public struct TextureFootprint
    {
        /// <summary>
        /// 
        /// </summary>
        public ulong RowSize;
        /// <summary>
        /// 
        /// </summary>
        public uint NumRows;
    }

    /// <summary>
    /// 
    /// </summary>
    public unsafe partial class ComputeDevice : IDisposable, IInternalD3D12Object
    {
        /// <summary>
        /// The default allocator for the device
        /// </summary>
        public GpuAllocator Allocator { get; private protected set; } = null!;

        /// <summary>
        /// The default pipeline manager for the device
        /// </summary>
        public PipelineManager PipelineManager { get; private protected set; } = null!;

        private protected ComPtr<ID3D12Device> Device;
        internal SupportedDevice DeviceLevel;
        internal enum SupportedDevice { Unknown, Device, Device1, Device2, Device3, Device4, Device5, Device6, Device7, Device8 }
        private protected static HashSet<ulong> PreexistingDevices = new(1);
        private protected DebugLayer? Debug;
        private protected ContextPool ContextPool;

        private protected SynchronizedCommandQueue CopyQueue;
        private protected SynchronizedCommandQueue ComputeQueue;

        private protected SynchronizedCommandQueue GraphicsQueue;

        private protected ulong CpuFrequency;

        private ref SynchronizedCommandQueue GetQueueForContext(ExecutionContext context)
        {
            switch (context)
            {
                case ExecutionContext.Copy:
                    return ref CopyQueue;
                case ExecutionContext.Compute:
                    return ref ComputeQueue;
                case ExecutionContext.Graphics when this is GraphicsDevice:
                    return ref GraphicsQueue;
            }

            return ref Helpers.NullRef<SynchronizedCommandQueue>();
        }

        /// <summary>
        /// The <see cref="Adapter"/> this device uses
        /// </summary>
        public Adapter Adapter { get; private set; }

        internal enum SupportedGraphicsCommandList { Unknown, GraphicsCommandList, GraphicsCommandList1, GraphicsCommandList2, GraphicsCommandList3, GraphicsCommandList4, GraphicsCommandList5 }
        private SupportedGraphicsCommandList SupportedList;

        internal ID3D12Device* DevicePointer => Device.Get();
        internal TDevice* DevicePointerAs<TDevice>() where TDevice : unmanaged => Device.AsBase<TDevice>().Get();

        /// <summary>
        /// The number of physical adapters, referred to as nodes, that the device uses
        /// </summary>
        public uint NodeCount => DevicePointer->GetNodeCount();


        private static readonly Adapter DefaultAdapter = GetDefaultAdapter();

        /// <summary>
        /// The highest <see cref="ShaderModel"/> supported by this device
        /// </summary>
        public ShaderModel HighestSupportedShaderModel { get; private set; }

        /// <summary>
        /// Whether DXIL is supported, rather than the old DXBC bytecode form.
        /// This is equivalent to cheking if <see cref="HighestSupportedShaderModel"/> supports shader model 6
        /// </summary>
        public bool IsDxilSupported => HighestSupportedShaderModel.IsDxil;

        // this fence is specifically used by the device for MakeResidentAsync. unrelated to queue fences
        private ComPtr<ID3D12Fence> _residencyFence;
        private ulong _lastFenceSignal;

        /// <summary>
        /// Indicates whether calls to <see cref="MakeResidentAsync{T}(T)"/> and <see cref="MakeResidentAsync{T}(ReadOnlySpan{T})"/> can succeed
        /// </summary>
        public bool CanMakeResidentAsync => DeviceLevel >= SupportedDevice.Device3;

        private const string Error_CantMakeResidentAsync = "Cannot MakeResidentAsync on a system that does not support ID3D12Device3.\n" +
            "Check CanMakeResidentAsync to determine if you can call MakeResidentAsync without it failing";

        /// <summary>
        /// An empty <see cref="RootSignature"/>
        /// </summary>
        public RootSignature EmptyRootSignature { get; }

        /// <summary>
        /// Creates a new <see cref="RootSignature"/>
        /// </summary>
        /// <param name="rootParameter">The <see cref="RootParameter"/> in the signature</param>
        /// <param name="staticSampler">The <see cref="StaticSampler"/> in the signature</param>
        /// <returns>A new <see cref="RootSignature"/></returns>
        public RootSignature CreateRootSignature(in RootParameter rootParameter, in StaticSampler staticSampler)
            => CreateRootSignature(new[] { rootParameter }, new[] { staticSampler });

        /// <summary>
        /// Creates a new <see cref="RootSignature"/>
        /// </summary>
        /// <param name="rootParameters">The <see cref="RootParameter"/>s in the signature</param>
        /// <param name="staticSampler">The <see cref="StaticSampler"/> in the signature</param>
        /// <returns>A new <see cref="RootSignature"/></returns>
        public RootSignature CreateRootSignature(ReadOnlyMemory<RootParameter> rootParameters, in StaticSampler staticSampler)
            => CreateRootSignature(rootParameters, new[] { staticSampler });

        /// <summary>
        /// Creates a new <see cref="RootSignature"/>
        /// </summary>
        /// <param name="rootParameters">The <see cref="RootParameter"/>s in the signature</param>
        /// <param name="staticSamplers">The <see cref="StaticSampler"/>s in the signature</param>
        /// <returns>A new <see cref="RootSignature"/></returns>
        public RootSignature CreateRootSignature(ReadOnlyMemory<RootParameter> rootParameters, ReadOnlyMemory<StaticSampler> staticSamplers = default)
            => RootSignature.Create(this, rootParameters, staticSamplers);

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

            Guard.ThrowIfFailed(DevicePointerAs<ID3D12Device3>()->EnqueueMakeResident(
                D3D12_RESIDENCY_FLAGS.D3D12_RESIDENCY_FLAG_DENY_OVERBUDGET,
                1,
                &pageable,
                _residencyFence.Get(),
                newValue
            ));

            return new GpuTask(_residencyFence, newValue);
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
                    Guard.ThrowIfFailed(DevicePointerAs<ID3D12Device3>()->EnqueueMakeResident(
                        D3D12_RESIDENCY_FLAGS.D3D12_RESIDENCY_FLAG_DENY_OVERBUDGET,
                        (uint)evicted.Length,
                        (ID3D12Pageable**)pEvictables,
                        _residencyFence.Get(),
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

                    Guard.ThrowIfFailed(DevicePointerAs<ID3D12Device3>()->EnqueueMakeResident(
                        D3D12_RESIDENCY_FLAGS.D3D12_RESIDENCY_FLAG_DENY_OVERBUDGET,
                        (uint)evicted.Length,
                        pEvictables,
                        _residencyFence.Get(),
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

                    Guard.ThrowIfFailed(DevicePointerAs<ID3D12Device3>()->EnqueueMakeResident(
                        D3D12_RESIDENCY_FLAGS.D3D12_RESIDENCY_FLAG_DENY_OVERBUDGET,
                        (uint)evicted.Length,
                        (ID3D12Pageable**)Unsafe.AsPointer(ref MemoryMarshal.GetArrayDataReference(pool.Value)),
                        _residencyFence.Get(),
                        newValue
                    ));
                }
            }

            return new GpuTask(_residencyFence, newValue);
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
            Guard.ThrowIfFailed(changeFunc(1, &pageable));
        }

        private void ChangeResidency<T>(delegate* stdcall<uint, ID3D12Pageable**, int> changeFunc, ReadOnlySpan<T> evictables) where T : IEvictable
        {
            // classes will never be blittable to pointer, so this handles that
            if (default(T)?.IsBlittableToPointer ?? false)
            {
                fixed (void* pEvictables = &Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(evictables)))
                {
                    Guard.ThrowIfFailed(changeFunc((uint)evictables.Length, (ID3D12Pageable**)pEvictables));
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

                    Guard.ThrowIfFailed(changeFunc((uint)evictables.Length, pEvictables));
                }
                else
                {
                    using var pool = RentedArray<nuint>.Create(evictables.Length, PinnedArrayPool<nuint>.Default);

                    for (int i = 0; i < evictables.Length; i++)
                    {
                        pool.Value[i] = (nuint)evictables[i].GetPageable();
                    }

                    Guard.ThrowIfFailed(changeFunc((uint)evictables.Length, (ID3D12Pageable**)Unsafe.AsPointer(ref MemoryMarshal.GetArrayDataReference(pool.Value))));
                }
            }
        }

        private static Adapter GetDefaultAdapter()
        {
            using var factory = new DxgiDeviceFactory().GetEnumerator();
            _ = factory.MoveNext();
            return factory.Current;
        }

        private static readonly object StateLock = new object();

        internal ComPtr<ID3D12RootSignature> CreateRootSignature(uint nodeMask, void* pSignature, uint signatureLength)
        {
            using ComPtr<ID3D12RootSignature> rootSig = default;
            Guard.ThrowIfFailed(DevicePointer->CreateRootSignature(
                nodeMask,
                pSignature,
                signatureLength,
                rootSig.Iid,
                ComPtr.GetVoidAddressOf(&rootSig)
            ));

            return rootSig.Move();
        }

        /// <summary>
        /// Create a new <see cref="ComputeDevice"/>
        /// </summary>
        /// <param name="adapter">The <see cref="Adapter"/> to create the device on, or <see langword="null"/> to use the default adapter</param>
        /// <param name="config">The <see cref="DeviceConfiguration"/> to create the device with</param>
        public ComputeDevice(DeviceConfiguration config, in Adapter? adapter)
        {
            Debug = new DebugLayer(config.DebugLayerConfiguration);

            {
                // Prevent another device creation messing with our settings
                lock (StateLock)
                {
                    Debug.SetGlobalStateForConfig();

                    CreateNewDevice(adapter, config.RequiredFeatureLevel);

                    Debug.ResetGlobalState();
                }

                if (!Device.Exists)
                {
                    ThrowHelper.ThrowPlatformNotSupportedException($"FATAL: Creation of ID3D12Device with feature level '{config.RequiredFeatureLevel}' failed");
                }

                Debug.SetDeviceStateForConfig(this);
            }

            ulong frequency;
            var res = QueryPerformanceFrequency((LARGE_INTEGER*) /* <- can we do that? */ &frequency);

            if (res == 0)
            {
                ThrowHelper.ThrowPlatformNotSupportedException("No high resolution timer found");
            }

            CpuFrequency = frequency;

            _residencyFence = CreateFence();

            QueryFeaturesOnCreation();
            ContextPool = new ContextPool(this);
            CopyQueue = new SynchronizedCommandQueue(this, ExecutionContext.Copy);
            ComputeQueue = new SynchronizedCommandQueue(this, ExecutionContext.Compute);
            EmptyRootSignature = RootSignature.Create(this, ReadOnlyMemory<RootParameter>.Empty, ReadOnlyMemory<StaticSampler>.Empty);
        }
        internal unsafe ComPtr<ID3D12Fence> CreateFence()
        {
            ComPtr<ID3D12Fence> fence = default;

            Guard.ThrowIfFailed(DevicePointer->CreateFence(
                0,
                0,
                fence.Iid,
                (void**)&fence
            ));

            return fence;
        }

        private protected void CreateNewDevice(
            in Adapter? adapter,
            FeatureLevel level
        )
        {
            var usedAdapter = adapter is Adapter notNull ? notNull : DefaultAdapter;

            if (PreexistingDevices.Contains(adapter.GetValueOrDefault().AdapterLuid))
            {
                ThrowHelper.ThrowArgumentException("Device already exists for adapter");
            }

            {
                using ComPtr<ID3D12Device> p = default;

                Guard.ThrowIfFailed(D3D12CreateDevice(
                    // null device triggers D3D12 to select a default device
                    usedAdapter.GetAdapterPointer(),
                    (D3D_FEATURE_LEVEL)level,
                    p.Iid,
                    ComPtr.GetVoidAddressOf(&p)
                ));
                Device = p.Move();

                LogHelper.LogInformation($"New D3D12 device created from adapter: \n{usedAdapter}");
            }

            this.SetName("Primary Device");

            DeviceLevel = Device switch
            {
                _ when Device.HasInterface<ID3D12Device8>() => SupportedDevice.Device8,
                _ when Device.HasInterface<ID3D12Device7>() => SupportedDevice.Device7,
                _ when Device.HasInterface<ID3D12Device6>() => SupportedDevice.Device6,
                _ when Device.HasInterface<ID3D12Device5>() => SupportedDevice.Device5,
                _ when Device.HasInterface<ID3D12Device4>() => SupportedDevice.Device4,
                _ when Device.HasInterface<ID3D12Device3>() => SupportedDevice.Device3,
                _ when Device.HasInterface<ID3D12Device2>() => SupportedDevice.Device2,
                _ when Device.HasInterface<ID3D12Device1>() => SupportedDevice.Device1,
                _ => SupportedDevice.Device
            };

            LUID luid = DevicePointer->GetAdapterLuid();
            System.Diagnostics.Debug.Assert(sizeof(LUID) == sizeof(ulong));
            _ = PreexistingDevices.Add(Unsafe.As<LUID, ulong>(ref luid));
            Adapter = usedAdapter;
        }

        internal void QueryFeatureSupport<T>(D3D12_FEATURE feature, ref T val) where T : unmanaged
        {
            fixed (T* pVal = &val)
            {
                Guard.ThrowIfFailed(DevicePointer->CheckFeatureSupport(feature, pVal, (uint)sizeof(T)));
            }
        }

        internal void QueryFeatureSupport<T>(D3D12_FEATURE feature, T* pVal) where T : unmanaged
        {
            Guard.ThrowIfFailed(DevicePointer->CheckFeatureSupport(feature, pVal, (uint)sizeof(T)));
        }

        private protected virtual void QueryFeaturesOnCreation()
        {
            D3D12_FEATURE_DATA_SHADER_MODEL highest;

            // the highest shader model that the app understands
            highest.HighestShaderModel = D3D_SHADER_MODEL.D3D_SHADER_MODEL_6_6;
            QueryFeatureSupport(D3D12_FEATURE_SHADER_MODEL, ref highest);

            GetMajorMinorForShaderModel(highest.HighestShaderModel, out var major, out var minor);
            HighestSupportedShaderModel = new ShaderModel(ShaderType.Unspecified, (byte)major, (byte)minor);
        }

        private static void GetMajorMinorForShaderModel(D3D_SHADER_MODEL model, out int major, out int minor)
            => (major, minor) = model switch
            {
                D3D_SHADER_MODEL.D3D_SHADER_MODEL_5_1 => (5, 1),
                D3D_SHADER_MODEL.D3D_SHADER_MODEL_6_0 => (6, 0),
                D3D_SHADER_MODEL.D3D_SHADER_MODEL_6_1 => (6, 1),
                D3D_SHADER_MODEL.D3D_SHADER_MODEL_6_2 => (6, 2),
                D3D_SHADER_MODEL.D3D_SHADER_MODEL_6_3 => (6, 3),
                D3D_SHADER_MODEL.D3D_SHADER_MODEL_6_4 => (6, 4),
                D3D_SHADER_MODEL.D3D_SHADER_MODEL_6_5 => (6, 5),
                D3D_SHADER_MODEL.D3D_SHADER_MODEL_6_6 => (6, 6),
                _ => throw new ArgumentException()
            };

        internal bool TryQueryInterface<T>(out ComPtr<T> result) where T : unmanaged
        {
            var success = ComPtr.TryQueryInterface(DevicePointer, out T* val);
            result = new ComPtr<T>(val);
            return success;
        }

        /// <summary>
        /// Represents a scoped PIX capture that ends when the type is disposed. Use <see cref="BeginScopedCapture"/> to create one.
        /// If PIX is not attached and the debug layer is attached, a message will be emitted explaining that the capture was dropped because PIX was not attached.
        /// If PIX is not attached and the debug layer is disabled, the capture will be silently dropped
        /// </summary>
        public struct ScopedCapture : IDisposable
        {
            private DebugLayer? _layer;

            internal ScopedCapture(DebugLayer? layer)
            {
                _layer = layer;
                layer?.BeginCapture();
            }

            /// <summary>
            /// Ends the capture
            /// </summary>
            public void Dispose() => _layer?.EndCapture();
        }

        /// <summary>
        /// Begins a scoped PIX capture that ends when the <see cref="ScopedCapture"/> is disposed
        /// </summary>
        /// <returns>A new <see cref="ScopedCapture"/></returns>
        public ScopedCapture BeginScopedCapture() => new ScopedCapture(Debug);

        /// <summary>
        /// Begins a PIX capture
        /// </summary>
        public void BeginCapture() => Debug?.BeginCapture();

        /// <summary>
        /// Ends a PIX capture
        /// </summary>
        public void EndCapture() => Debug?.EndCapture();

        /// <summary>
        /// Returns a <see cref="CopyContext"/> used for recording copy commands
        /// </summary>
        public CopyContext BeginCopyContext(bool executeOnClose = false)
        {
            var context = ContextPool.Rent(ExecutionContext.Copy, null, executeOnClose: executeOnClose);
            return new CopyContext(context);
        }

        /// <summary>
        /// Returns a <see cref="UploadContext"/> used for recording upload commands
        /// </summary>
        /// <returns>A new <see cref="UploadContext"/></returns>
        public UploadContext BeginUploadContext()
        {
            var context = ContextPool.Rent(ExecutionContext.Copy, null, false);

            return new UploadContext(context);
        }

        /// <summary>
        /// Returns a <see cref="ComputeContext"/> used for recording compute commands
        /// </summary>
        public ComputeContext BeginComputeContext(ComputePipelineStateObject? pso = null, bool executeOnClose = false)
        {
            var @params = ContextPool.Rent(ExecutionContext.Compute, pso, executeOnClose: executeOnClose);

            var context = new ComputeContext(@params);
            SetDefaultState(context, pso);
            return context;  
        }

        /// <summary>
        /// Submit a set of recorded commands to the list
        /// </summary>
        /// <param name="context">The commands to submit for execution</param>
        public GpuTask Execute(GpuContext context)
        {
            ID3D12GraphicsCommandList* list = context.List;

            ref var queue = ref GetQueueForContext(context.Context);
            if (Helpers.IsNullRef(ref queue))
            {
                ThrowHelper.ThrowInvalidOperationException("Invalid to try and execute a GpuContext that is not a CopyContext or ComputeContext on a ComputeDevice");
            }

            var finish = queue.ExecuteCommandLists(1, (ID3D12CommandList**)&list);

            ContextPool.Return(context.Params, finish);

            return finish;
        }

        /// <summary>
        /// Execute all provided command lists
        /// </summary>
        public GpuTask Execute(Span<GpuContext> contexts)
        {
            if (contexts.IsEmpty)
            {
                return GpuTask.Completed;
            }

            StackSentinel.StackAssert(StackSentinel.SafeToStackallocPointers(contexts.Length));

            ID3D12CommandList** ppLists = stackalloc ID3D12CommandList*[contexts.Length];

            ExecutionContext requiredContext = ExecutionContext.Copy;
            for (var i = 0; i < contexts.Length; i++)
            {
                ppLists[i] = (ID3D12CommandList*)contexts[i].List;


                requiredContext = contexts[i].Context switch
                {
                    ExecutionContext.Copy => requiredContext,
                    ExecutionContext.Compute when requiredContext is ExecutionContext.Copy => ExecutionContext.Compute,
                    ExecutionContext.Graphics when requiredContext is ExecutionContext.Copy or ExecutionContext.Compute => ExecutionContext.Graphics,
                    _ => requiredContext
                };
            }

            if (requiredContext == (ExecutionContext)(0xFFFFFFFF))
            {
                ThrowHelper.ThrowArgumentException("Invalid execution context type provided in param contexts");
            }
            ref var queue = ref GetQueueForContext(requiredContext);
            if (Helpers.IsNullRef(ref queue))
            {
                ThrowHelper.ThrowInvalidOperationException("Invalid to try and execute a GpuContext that is not a CopyContext or ComputeContext on a ComputeDevice");
            }
            var finish = queue.ExecuteCommandLists((uint)contexts.Length, ppLists);

            for (var i = 0; i < contexts.Length; i++)
            {
                ContextPool.Return(contexts[i].Params, finish);
                contexts[i].Params.List = default;
            }

            return finish;
        }

        private unsafe void SetDefaultState(GpuContext context, ComputePipelineStateObject? pso)
        {
            if (pso is not null)
            {
                context.List->SetComputeRootSignature(pso.GetRootSig());
            }

            const int numHeaps = 1;
            var heaps = stackalloc ID3D12DescriptorHeap*[1] { ResourceDescriptors.GetHeap() };

            context.List->SetDescriptorHeaps(numHeaps, heaps);
        }

        internal ComPtr<ID3D12Heap> CreateHeap(D3D12_HEAP_DESC* desc)
        {
            ComPtr<ID3D12Heap> heap = default;
            Guard.ThrowIfFailed(DevicePointer->CreateHeap(
                desc,
                heap.Iid,
                ComPtr.GetVoidAddressOf(&heap)
            ));

            return heap.Move();
        }

        internal unsafe ComPtr<ID3D12CommandQueue> CreateQueue(ExecutionContext type)
        {
            var desc = new D3D12_COMMAND_QUEUE_DESC
            {
                Type = (D3D12_COMMAND_LIST_TYPE)type,
                Flags = D3D12_COMMAND_QUEUE_FLAGS.D3D12_COMMAND_QUEUE_FLAG_NONE,
                NodeMask = 0, // TODO: MULTI-GPU
                Priority = (int)D3D12_COMMAND_QUEUE_PRIORITY.D3D12_COMMAND_QUEUE_PRIORITY_NORMAL // why are you like this D3D12
            };

            ComPtr<ID3D12CommandQueue> p = default;

            Guard.ThrowIfFailed(DevicePointer->CreateCommandQueue(
                &desc,
                p.Iid,
                ComPtr.GetVoidAddressOf(&p)
            ));

            return p.Move();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="tex"></param>
        /// <param name="intermediate"></param>
        /// <param name="data"></param>
        public void ReadbackIntermediateBuffe(in TextureFootprint tex, in Buffer intermediate, Span<byte> data)
        {
            var offset = data;
            var mapped = intermediate.Data;

            var alignedRowSize = MathHelpers.AlignUp(tex.RowSize, 256);
            for (var i = 0; i < tex.NumRows - 1; i++)
            {
                mapped.Slice(0, (int)tex.RowSize).CopyTo(offset);

                mapped = mapped.Slice((int)alignedRowSize);
                offset = offset.Slice((int)tex.RowSize);
            }
            mapped.Slice(0, (int)tex.RowSize).CopyTo(offset);
        }

        /// <summary>
        /// Queries the GPU and timestamp
        /// </summary>
        /// <param name="context">The <see cref="ExecutionContext"/> indicating which queue should be queried</param>
        /// <param name="gpu">The <see cref="TimeSpan"/> representing the GPU clock</param>
        public void QueryTimestamp(ExecutionContext context, out TimeSpan gpu)
            => QueryTimestamps(context, out gpu, out _);

        /// <summary>
        /// Queries the GPU and CPU timestamps in close succession
        /// </summary>
        /// <param name="context">The <see cref="ExecutionContext"/> indicating which queue should be queried</param>
        /// <param name="gpu">The <see cref="TimeSpan"/> representing the GPU clock</param>
        /// <param name="cpu">The <see cref="TimeSpan"/> representing the CPU clock</param>
        public void QueryTimestamps(ExecutionContext context, out TimeSpan gpu, out TimeSpan cpu)
        {
            ref var queue = ref GetQueueForContext(context);

            ulong gpuTick, cpuTick;

            if (!queue.TryQueryTimestamps(&gpuTick, &cpuTick))
            {
                ThrowHelper.ThrowExternalException("GPU timestamp query failed");
            }

            gpu = TimeSpan.FromSeconds(gpuTick / (double)queue.Frequency);
            cpu = TimeSpan.FromSeconds(cpuTick / (double)CpuFrequency);
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="tex"></param>
        /// <param name="subresourceIndex"></param>
        /// <returns></returns>
        public TextureFootprint GetSubresourceFootprin(in Texture tex, uint subresourceIndex)
        {
            TextureFootprint result;
            GetCopyableFootprint(tex, subresourceIndex, 1, out _, out result.NumRows, out result.RowSize, out _);
            return result;
        }

        internal void GetCopyableFootprint(
            in Texture tex,
            uint firstSubresource,
            uint numSubresources,
            out D3D12_PLACED_SUBRESOURCE_FOOTPRINT layouts,
            out uint numRows,
            out ulong rowSizesInBytes,
            out ulong requiredSize
        )
        {
            var desc = tex.GetResourcePointer()->GetDesc();

            fixed (D3D12_PLACED_SUBRESOURCE_FOOTPRINT* pLayout = &layouts)
            {
                ulong rowSizes;
                uint rowCount;
                ulong size;
                DevicePointer->GetCopyableFootprints(&desc, 0, numSubresources, 0, pLayout, &rowCount, &rowSizes, &size);

                rowSizesInBytes = rowSizes;
                numRows = rowCount;
                requiredSize = size;
            }
        }

        internal void GetCopyableFootprints(
            in Texture tex,
            uint firstSubresource,
            uint numSubresources,
            out Span<D3D12_PLACED_SUBRESOURCE_FOOTPRINT> layouts,
            out Span<uint> numRows,
            out Span<ulong> rowSizesInBytes,
            out ulong requiredSize
        )
        {
            var desc = tex.GetResourcePointer()->GetDesc();

            var subresources = GC.AllocateUninitializedArray<byte>((sizeof(D3D12_PLACED_SUBRESOURCE_FOOTPRINT) + sizeof(uint) + sizeof(ulong)) * (int)numSubresources, pinned: true);

            var pLayouts = (D3D12_PLACED_SUBRESOURCE_FOOTPRINT*)Unsafe.AsPointer(ref MemoryMarshal.GetArrayDataReference(subresources));
            ulong* pRowSizesInBytes = (ulong*)(pLayouts + numSubresources);
            uint* pNumRows = (uint*)(pRowSizesInBytes + numSubresources);

            ulong size;
            DevicePointer->GetCopyableFootprints(&desc, 0, numSubresources, 0, pLayouts, pNumRows, pRowSizesInBytes, &size);
            requiredSize = size;

            layouts = new Span<D3D12_PLACED_SUBRESOURCE_FOOTPRINT>(pLayouts, (int)numSubresources);
            rowSizesInBytes = new Span<ulong>(pRowSizesInBytes, (int)numSubresources);
            numRows = new Span<uint>(pNumRows, (int)numSubresources);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="tex"></param>
        /// <param name="numSubresources"></param>
        /// <returns></returns>
        public ulong GetRequiredSize(
            in Texture tex,
            uint numSubresources
        )
        {
            var desc = tex.GetResourcePointer()->GetDesc();
            return GetRequiredSize(&desc, numSubresources);
        }

        internal ulong GetRequiredSize(
            D3D12_RESOURCE_DESC* desc,
            uint numSubresources
        )
        {
            ulong requiredSize;
            DevicePointer->GetCopyableFootprints(desc, 0, numSubresources, 0, null, null, null, &requiredSize);
            return requiredSize;
        }

        internal D3D12_RESOURCE_ALLOCATION_INFO GetAllocationInfo(InternalAllocDesc* desc)
            => DevicePointer->GetResourceAllocationInfo(0, 1, &desc->Desc);

        internal ComPtr<ID3D12CommandAllocator> CreateAllocator(ExecutionContext context)
        {
            using ComPtr<ID3D12CommandAllocator> allocator = default;
            Guard.ThrowIfFailed(DevicePointer->CreateCommandAllocator(
                (D3D12_COMMAND_LIST_TYPE)context,
                allocator.Iid,
                ComPtr.GetVoidAddressOf(&allocator)
            ));

            return allocator.Move();
        }

        internal ComPtr<ID3D12GraphicsCommandList> CreateList(ExecutionContext context, ID3D12CommandAllocator* allocator, ID3D12PipelineState* pso)
        {
            using ComPtr<ID3D12GraphicsCommandList> list = default;
            Guard.ThrowIfFailed(DevicePointer->CreateCommandList(
                0, // TODO: MULTI-GPU
                (D3D12_COMMAND_LIST_TYPE)context,
                allocator,
                pso,
                list.Iid,
                ComPtr.GetVoidAddressOf(&list)
            ));

            return list.Move();
        }

        internal ComPtr<ID3D12QueryHeap> CreateQueryHeap(D3D12_QUERY_HEAP_DESC desc)
        {
            using ComPtr<ID3D12QueryHeap> queryHeap = default;
            DevicePointer->CreateQueryHeap(&desc, queryHeap.Iid, ComPtr.GetVoidAddressOf(&queryHeap));
            return queryHeap.Move();
        }

        internal ComPtr<ID3D12Resource> CreatePlacedResource(ID3D12Heap* heap, ulong offset, InternalAllocDesc* desc)
        {
            var clearVal = desc->ClearValue.GetValueOrDefault();

            using ComPtr<ID3D12Resource> resource = default;

            Guard.ThrowIfFailed(DevicePointer->CreatePlacedResource(
                 heap,
                 offset,
                 &desc->Desc,
                 desc->InitialState,
                 desc->ClearValue is null ? null : &clearVal,
                 resource.Iid,
                 ComPtr.GetVoidAddressOf(&resource)
             ));

            return resource.Move();
        }

        internal ComPtr<ID3D12Resource> CreateCommittedResource(InternalAllocDesc* desc)
        {
            var heapProperties = GetHeapProperties(desc);
            var clearVal = desc->ClearValue.GetValueOrDefault();

            using ComPtr<ID3D12Resource> resource = default;

            Guard.ThrowIfFailed(DevicePointer->CreateCommittedResource(
                    &heapProperties,
                    D3D12_HEAP_FLAGS.D3D12_HEAP_FLAG_NONE,
                    &desc->Desc,
                    desc->InitialState,
                    desc->ClearValue is null ? null : &clearVal,
                    resource.Iid,
                    ComPtr.GetVoidAddressOf(&resource)
            ));

            return resource.Move();

            static D3D12_HEAP_PROPERTIES GetHeapProperties(InternalAllocDesc* desc)
            {
                return new D3D12_HEAP_PROPERTIES(desc->HeapType);
            }
        }

        /// <inheritdoc/>
        public virtual void Dispose()
        {
            Device.Dispose();
            Allocator.Dispose();
            PipelineManager.Dispose();
        }

        ID3D12Object* IInternalD3D12Object.GetPointer() => (ID3D12Object*)DevicePointer;

        //CheckFeatureSupport
        //CopyDescriptors
        //CopyDescriptorsSimple
        //CreateCommandAllocator
        //CreateCommandList
        //CreateCommandQueue
        //CreateCommandSignature
        //CreateCommittedResource
        //CreateComputePipelineState
        //CreateConstantBufferView
        //CreateDescriptorHeap
        //CreateFence
        //CreateHeap
        //CreatePlacedResource
        //CreateQueryHeap
        //CreateRootSignature
        //CreateShaderResourceView
        //CreateSharedHandle
        //CreateUnorderedAccessView
        //Evict
        //GetAdapterLuid
        //GetCopyableFootprints
        //GetCustomHeapProperties
        //GetDescriptorHandleIncrementSize
        //GetDeviceRemovedReason
        //GetNodeCount
        //GetResourceAllocationInfo
        //MakeResident
        //OpenSharedHandle
        //OpenSharedHandleByName
        //SetStablePowerState

        //CreatePipelineLibrary
        //SetEventOnMultipleFenceCompletion
        //SetResidencySetEventOnMultipleFenceCompletionPriority

        //CreatePipelineState

        //OpenExistingHeapFromAddress
        //OpenExistingHeapFromFileMapping
        //EnqueueMakeResident

        //GetResourceAllocationInfo1

        //CreateMetaCommand
        //CreateStateObject
        //EnumerateMetaCommandParameters
        //EnumerateMetaCommands
        //RemoveDevice
    }
}
