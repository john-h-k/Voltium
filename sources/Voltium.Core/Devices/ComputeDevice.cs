using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Collections.Extensions;
using TerraFX.Interop;
using Voltium.Allocators;
using Voltium.Common;
using Voltium.Core.Contexts;
using Voltium.Core.Devices.Shaders;
using Voltium.Core.Infrastructure;
using Voltium.Core.Memory;
using Voltium.Core.MetaCommands;
using Voltium.Core.Pipeline;
using Voltium.Core.Pool;
using static TerraFX.Interop.D3D12_FEATURE;
using static TerraFX.Interop.D3D_FEATURE_LEVEL;
using static TerraFX.Interop.Windows;
using Buffer = Voltium.Core.Memory.Buffer;

using SysDebug = System.Diagnostics.Debug;

namespace Voltium.Core.Devices
{
    internal struct TextureLayout
    {
        public D3D12_PLACED_SUBRESOURCE_FOOTPRINT[] Layouts;
        public uint[] NumRows;
        public ulong[] RowSizes;
        public ulong TotalSize;

    }




    /// <summary>
    /// Describes the layout of a CPU-side subresource
    /// </summary>
    public struct SubresourceLayout
    {
        /// <summary>
        /// The size of the rows, in bytes
        /// </summary>
        public ulong RowSize;

        /// <summary>
        /// The number of rows
        /// </summary>
        public uint NumRows;
    }
    internal enum SupportedGraphicsCommandList { Unknown, GraphicsCommandList, GraphicsCommandList1, GraphicsCommandList2, GraphicsCommandList3, GraphicsCommandList4, GraphicsCommandList5, GraphicsCommandList6 }

    internal struct DeviceArchitecture
    {
        public int AddressSpaceBits { init; get; }
        public int ResourceAddressBits { init; get; }

        public bool IsCacheCoherentUma { init; get; }

        public ulong VirtualAddressSpaceSize { init; get; }

    }

    /// <summary>
    /// Flags used when creating a <see cref="GpuContext"/>
    /// </summary>
    public enum ContextFlags
    {
        /// <summary>
        /// None
        /// </summary>
        None,

        /// <summary>
        /// Immediately begin executing the context when it is closed or disposed
        /// </summary>
        ExecuteOnClose,

        /// <summary>
        /// Immediately begin executing, and synchronously block until the end, when it is closed or disposed
        /// </summary>
        BlockOnClose
    }

    public enum DeviceFeature
    {
        Raytracing,
        InlineRaytracing
    }

    /// <summary>
    /// 
    /// </summary>
    public unsafe partial class ComputeDevice : IDisposable, IInternalD3D12Object
    {
        /// <summary>
        /// The <see cref="Adapter"/> this device uses
        /// </summary>
        public Adapter Adapter { get; private set; }

        /// <summary>
        /// The default allocator for the device
        /// </summary>
        public GpuAllocator Allocator { get; private protected set; }

        /// <summary>
        /// The default pipeline manager for the device
        /// </summary>
        public PipelineManager PipelineManager { get; private protected set; }

        // ClearUnorderedAccessView methods require a CPU descriptor handle that is not shader visible and a GPU descriptor handle that is shader visible
        // These methods can be implemented by the driver as a Dispatch (which requires a shader visible handle)
        // But also may be implemented by fixed-function hardware like ClearRenderTargetView which require a CPU descriptor
        // Because shader visible heaps are created in UPLOAD/WRITE_BACK memory, they are very slow to read from, so a non-shader visible CPU descriptor is required for perf
        // This is the heap for that
        private protected DescriptorHeap OpaqueUavs;
        private protected DescriptorHeap UavCbvSrvs;

        private protected UniqueComPtr<ID3D12Device> Device;
        internal SupportedDevice DeviceLevel;
        internal enum SupportedDevice { Unknown, Device, Device1, Device2, Device3, Device4, Device5, Device6, Device7, Device8 }
        private protected static Dictionary<Adapter, ComputeDevice> AdapterDeviceMap = new(1);
        private protected DebugLayer? Debug;
        private protected ContextPool ContextPool;

        private protected UniqueComPtr<ID3D12DeviceDownlevel> _deviceDownlevel;

        /// <summary>
        /// Reserve video memory for a given node and <see cref="MemorySegment"/>. Set this to the minimum required value for your app to run,
        /// to allow the OS to minimise interruptions caused by sudden high memory pressure.
        /// </summary>
        /// <param name="reservation">The reservation, in bytes</param>
        /// <param name="segment">The <see cref="MemorySegment"/> to reserve for</param>
        /// <param name="node">The node to reserve for</param>
        public unsafe void ReserveVideoMemory(ulong reservation, MemorySegment segment, uint node = 0)
        {
            if (Adapter._adapter.TryQueryInterface<IDXGIAdapter3>(out var adapter3))
            {
                using (adapter3)
                {
                    Guard.ThrowIfFailed(adapter3.Ptr->SetVideoMemoryReservation(node, (DXGI_MEMORY_SEGMENT_GROUP)segment, reservation));
                }
            }
            else
            {
                ThrowHelper.ThrowPlatformNotSupportedException("Can't reserve video memory on WSL/Win7");
            }
        }

        /// <summary>
        /// Queries the <see cref="AdapterMemoryInfo"/> for a given <see cref="MemorySegment"/> and node
        /// </summary>
        /// <param name="segment">The <see cref="MemorySegment"/> to query for</param>
        /// <param name="node">The index of the node to query for</param>
        /// <returns></returns>
        public unsafe AdapterMemoryInfo QueryMemory(MemorySegment segment, uint node = 0)
        {
            DXGI_QUERY_VIDEO_MEMORY_INFO info;

            if (Adapter._adapter.TryQueryInterface<IDXGIAdapter3>(out var adapter3))
            {
                using (adapter3)
                {
                    Guard.ThrowIfFailed(adapter3.Ptr->QueryVideoMemoryInfo(node, (DXGI_MEMORY_SEGMENT_GROUP)segment, &info));
                }

            }
            else if (Adapter._adapter.TryQueryInterface<IDXCoreAdapter>(out var adapter))
            {
                using (adapter)
                {
                    var input = new DXCoreAdapterMemoryBudgetNodeSegmentGroup { nodeIndex = node, segmentGroup = (DXCoreSegmentGroup)segment };
                    Unsafe.SkipInit(out DXCoreAdapterMemoryBudget dxcInfo);
                    Guard.ThrowIfFailed(adapter.Ptr->QueryState(
                        DXCoreAdapterState.AdapterMemoryBudget,
                        Helpers.SizeOf(input),
                        &input,
                        Helpers.SizeOf(dxcInfo),
                        &dxcInfo
                    ));

                    // DXCoreAdapterMemoryBudget has identical layout :)
                    info = *(DXGI_QUERY_VIDEO_MEMORY_INFO*)&dxcInfo;
                }
            }
            else
            {
                // win7
                Guard.ThrowIfFailed(_deviceDownlevel.Ptr->QueryVideoMemoryInfo(node, (DXGI_MEMORY_SEGMENT_GROUP)segment, &info));
            }

            return CreateInfo(info);

            static AdapterMemoryInfo CreateInfo(in DXGI_QUERY_VIDEO_MEMORY_INFO info)
            {
                return new AdapterMemoryInfo
                {
                    Budget = info.Budget,
                    AvailableForReservation = info.AvailableForReservation,
                    CurrentReservation = info.CurrentReservation,
                    CurrentUsage = info.CurrentUsage
                };
            }
        }

        public bool Supports(DeviceFeature feature)
        {
            return feature switch
            {
                DeviceFeature.Raytracing => QueryFeatureSupport<D3D12_FEATURE_DATA_D3D12_OPTIONS5>(D3D12_FEATURE_D3D12_OPTIONS5).RaytracingTier >= D3D12_RAYTRACING_TIER.D3D12_RAYTRACING_TIER_1_0,
                DeviceFeature.InlineRaytracing => QueryFeatureSupport<D3D12_FEATURE_DATA_D3D12_OPTIONS5>(D3D12_FEATURE_D3D12_OPTIONS5).RaytracingTier >= D3D12_RAYTRACING_TIER.D3D12_RAYTRACING_TIER_1_1,
                _ => ThrowHelper.ThrowArgumentException<bool>(nameof(feature))
            };
        }

        /// <summary>
        /// The callback used when the adapter memory budget changes
        /// </summary>
        /// <param name="sender">The <see cref="ComputeDevice"/> which had an adapter budget change</param>
        public delegate void AdapterMemoryInfoChanged(ComputeDevice sender);


        private uint _vidMemCookie;
        private bool _registered;
        private AdapterMemoryInfoChanged? _onAdapterMemoryInfoChanged = (_) => { };
        private object _eventLock = new();

        /// <summary>
        /// The event fired when the adapter memory budget changes
        /// </summary>
        public event AdapterMemoryInfoChanged OnAdapterMemoryInfoChanged
        {
            add
            {
                lock (_eventLock)
                {
                    if (_onAdapterMemoryInfoChanged is null)
                    {
                        RegisterMemoryEventDelegate();
                    }
                    _onAdapterMemoryInfoChanged += value;
                }
            }

            remove
            {
                lock (_eventLock)
                {
                    _onAdapterMemoryInfoChanged -= value;

                    if (_onAdapterMemoryInfoChanged is null)
                    {
                        UnregisterMemoryEventDelegate();
                    }
                }
            }
        }

        [UnmanagedCallersOnly]
        private static void DxCoreMemoryInfoChangedCallback(DXCoreNotificationType type, IUnknown* pAdapter, void* context)
        {
            SysDebug.Assert(type == DXCoreNotificationType.AdapterBudgetChange);
            MemoryInfoInvokeFromContext(context);
        }

        [UnmanagedCallersOnly]
        private static void DxgiMemoryInfoChangedCallback(void* context, byte /* timeout or wait, irrelevant */ _)
        {
            MemoryInfoInvokeFromContext(context);
        }

        private static void MemoryInfoInvokeFromContext(void* context)
        {
            var handle = GCHandle.FromIntPtr((IntPtr)context);
            var device = (ComputeDevice)handle.Target!;
            handle.Free();

            device._onAdapterMemoryInfoChanged?.Invoke(device);
        }

        private void RegisterMemoryEventDelegate()
        {
            if (_registered)
            {
                return;
            }
            _registered = true;
            var gcHandle = GCHandle.Alloc(this);
            var context = (void*)GCHandle.ToIntPtr(gcHandle);

            fixed (uint* pCookie = &_vidMemCookie)
            {
                if (Adapter._adapter.TryQueryInterface<IDXGIAdapter3>(out var adapter3))
                {
                    using (adapter3)
                    {
                        IntPtr handle = CreateEventW(null, FALSE, FALSE, null);

                        Guard.ThrowIfFailed(adapter3.Ptr->RegisterVideoMemoryBudgetChangeNotificationEvent(handle, pCookie));

                        IntPtr newHandle;

                        int err = RegisterWaitForSingleObject(
                            &newHandle,
                            handle,
                            &DxgiMemoryInfoChangedCallback,
                            context,
                            INFINITE,
                            0
                        );

                        if (err == 0)
                        {
                            ThrowHelper.ThrowWin32Exception("RegisterWaitForSingleObject failed");
                        }
                    }
                }
                else if (Adapter._adapter.TryQueryInterface<IDXCoreAdapter>(out var adapter))
                {
                    using (adapter)
                    {
                        using UniqueComPtr<IDXCoreAdapterFactory> factory = default;
                        Guard.ThrowIfFailed(adapter.Ptr->GetFactory(factory.Iid, (void**)&factory));

                        Guard.ThrowIfFailed(factory.Ptr->RegisterEventNotification(
                            Adapter.GetAdapterPointer(),
                            DXCoreNotificationType.AdapterBudgetChange,
                            &DxCoreMemoryInfoChangedCallback,
                            context,
                            pCookie
                        ));
                    }
                }
            }
        }


        private void UnregisterMemoryEventDelegate()
        {
            if (!_registered)
            {
                return;
            }
            _registered = false;

            if (Adapter._adapter.TryQueryInterface<IDXGIAdapter3>(out var adapter3))
            {
                using (adapter3)
                {
                    adapter3.Ptr->UnregisterVideoMemoryBudgetChangeNotification(_vidMemCookie);
                }
            }
            else if (Adapter._adapter.TryQueryInterface<IDXCoreAdapter>(out var adapter))
            {
                using (adapter)
                {
                    using UniqueComPtr<IDXCoreAdapterFactory> factory = default;
                    Guard.ThrowIfFailed(adapter.Ptr->GetFactory(factory.Iid, (void**)&factory));

                    Guard.ThrowIfFailed(factory.Ptr->UnregisterEventNotification(_vidMemCookie));
                }
            }
        }

        internal CommandQueue CopyQueue;
        internal CommandQueue ComputeQueue;
        internal CommandQueue GraphicsQueue;

        private protected ulong CpuFrequency;

        private protected SupportedGraphicsCommandList SupportedList;

        internal DeviceArchitecture Architecture { get; private set; }

        internal ID3D12Device5* DevicePointer => (ID3D12Device5*)Device.Ptr;
        internal TDevice* As<TDevice>() where TDevice : unmanaged => Device.As<TDevice>().Ptr;

        /// <summary>
        /// The number of physical adapters, referred to as nodes, that the device uses
        /// </summary>
        public uint NodeCount { get; }


        private protected static readonly Lazy<Adapter> DefaultAdapter = new(() =>
        {
            using DeviceFactory.Enumerator factory = new DxgiDeviceFactory().GetEnumerator();
            _ = factory.MoveNext();
            return factory.Current;
        });

        /// <summary>
        /// The highest <see cref="ShaderModel"/> supported by this device
        /// </summary>
        public ShaderModel HighestSupportedShaderModel { get; private set; }

        /// <summary> 
        /// Whether DXIL is supported, rather than the old DXBC bytecode form.
        /// This is equivalent to cheking if <see cref="HighestSupportedShaderModel"/> supports shader model 6
        /// </summary>
        public bool IsDxilSupported => HighestSupportedShaderModel.IsDxil;

        /// <summary>
        /// Whether the device is removed
        /// </summary>
        public bool IsDeviceRemoved => DevicePointer->GetDeviceRemovedReason() != S_OK;

        private protected static bool TryGetDevice(FeatureLevel level, in Adapter adapter, out ComputeDevice device)
        {
            lock (AdapterDeviceMap)
            {
                if (AdapterDeviceMap.TryGetValue(adapter, out device!))
                {
                    const int numLevels = 5;
                    var pLevels = stackalloc D3D_FEATURE_LEVEL[numLevels]
                    {
                        D3D_FEATURE_LEVEL_1_0_CORE,
                        D3D_FEATURE_LEVEL_11_0,
                        D3D_FEATURE_LEVEL_11_1,
                        D3D_FEATURE_LEVEL_12_0,
                        D3D_FEATURE_LEVEL_12_1
                    };

                    D3D12_FEATURE_DATA_FEATURE_LEVELS levels;
                    levels.pFeatureLevelsRequested = pLevels;
                    levels.NumFeatureLevels = numLevels;

                    device.QueryFeatureSupport(D3D12_FEATURE_FEATURE_LEVELS, &levels);

                    if ((FeatureLevel)levels.MaxSupportedFeatureLevel < level)
                    {
                        ThrowHelper.ThrowNotSupportedException($"Requested adapter doesn't support desired feature level '{level}'");
                    }

                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Gets the <see cref="ComputeDevice"/> for a given <see cref="Adapter"/>
        /// </summary>
        /// <param name="requiredFeatureLevel">The required <see cref="FeatureLevel"/> for device creation</param>
        /// <param name="adapter">The <see cref="Adapter"/> to create the device from, or <see langword="null"/> to use the default adapter</param>
        /// <param name="config">The <see cref="DebugLayerConfiguration"/> for the device, or <see langword="null"/> to use the default</param>
        /// <returns>A <see cref="ComputeDevice"/></returns>
        public static ComputeDevice Create(FeatureLevel requiredFeatureLevel, in Adapter? adapter, DebugLayerConfiguration? config = null)
        {
            return TryGetDevice(requiredFeatureLevel, adapter ?? DefaultAdapter.Value, out var device) ? device : new ComputeDevice(requiredFeatureLevel, adapter, config);
        }

        private protected ComputeDevice(FeatureLevel level, in Adapter? adapter, DebugLayerConfiguration? config = null)
        {
            GraphicsQueue = null!;
            if (config is not null)
            {
                if (config.DebugFlags.HasFlag(DebugFlags.DebugLayer))
                {
                    DeviceCreationSettings.EnableDebugLayer();
                }
                if (config.DebugFlags.HasFlag(DebugFlags.GpuBasedValidation))
                {
                    DeviceCreationSettings.EnableGpuBasedValidation();
                }
                DeviceCreationSettings.EnableDred(config.DredFlags);
            }

            CreateDevice(adapter ?? DefaultAdapter.Value, level);

            if (!Device.Exists)
            {
                ThrowHelper.ThrowPlatformNotSupportedException($"FATAL: Creation of ID3D12Device with feature level '{level}' failed");
            }

            Debug = new DebugLayer(this, config);


            ulong frequency;
            var res = QueryPerformanceFrequency((LARGE_INTEGER*) /* <- can we do that? */ &frequency);

            if (res == 0)
            {
                ThrowHelper.ThrowPlatformNotSupportedException("No CPU high resolution timer found");
            }

            CpuFrequency = frequency;

            _residencyFence = CreateFence();

            NodeCount = DevicePointer->GetNodeCount();
            QueryFeaturesOnCreation();
            ContextPool = new ContextPool(this);
            CopyQueue = new CommandQueue(this, ExecutionContext.Copy, true);
            ComputeQueue = new CommandQueue(this, ExecutionContext.Compute, true);
            Allocator = new GpuAllocator(this);
            PipelineManager = new PipelineManager(this);
            EmptyRootSignature = RootSignature.Create(this, ReadOnlyMemory<RootParameter>.Empty, ReadOnlyMemory<StaticSampler>.Empty, D3D12_ROOT_SIGNATURE_FLAGS.D3D12_ROOT_SIGNATURE_FLAG_NONE);
            CreateDescriptorHeaps();

            if (DeviceCreationSettings.AreMetaCommandsEnabled)
            {
                _metaCommandDescs = new Lazy<MetaCommandDesc[]?>(EnumMetaCommands);
            }
        }

        private ref CommandQueue GetQueueForContext(ExecutionContext context)
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

            return ref Unsafe.NullRef<CommandQueue>();
        }


        /// <summary>
        /// Throws if a given HR is a fail code. Also properly handles device-removed error codes, unlike Guard.ThrowIfFailed
        /// </summary>
        [MethodImpl(MethodTypes.Validates)]
        internal void ThrowIfFailed(
            int hr,
            [CallerArgumentExpression("hr")] string? expression = null
#if DEBUG || EXTENDED_ERROR_INFORMATION
            ,
            [CallerFilePath] string? filepath = default,
            [CallerMemberName] string? memberName = default,
            [CallerLineNumber] int lineNumber = default
#endif
        )
        {
            // invert branch so JIT assumes the HR is S_OK
            if (SUCCEEDED(hr))
            {
                return;
            }

            HrIsFail(this, hr
#if DEBUG || EXTENDED_ERROR_INFORMATION
                , expression, filepath, memberName, lineNumber
#endif
                );

            [MethodImpl(MethodImplOptions.NoInlining)]
            static void HrIsFail(ComputeDevice device, int hr

#if DEBUG || EXTENDED_ERROR_INFORMATION
                                , string? expression, string? filepath, string? memberName, int lineNumber
#endif
)
            {
                if (hr is DXGI_ERROR_DEVICE_REMOVED or DXGI_ERROR_DEVICE_RESET or DXGI_ERROR_DEVICE_HUNG)
                {
                    throw new DeviceDisconnectedException(device, TranslateReason(device.DevicePointer->GetDeviceRemovedReason()));
                }

                static DeviceDisconnectReason TranslateReason(int hr) => hr switch
                {
                    DXGI_ERROR_DEVICE_REMOVED => DeviceDisconnectReason.Removed,
                    DXGI_ERROR_DEVICE_HUNG => DeviceDisconnectReason.Hung,
                    DXGI_ERROR_DEVICE_RESET => DeviceDisconnectReason.Reset,
                    DXGI_ERROR_DRIVER_INTERNAL_ERROR => DeviceDisconnectReason.InternalDriverError,
                    _ => DeviceDisconnectReason.Unknown
                };

                Guard.ThrowForHr(hr
#if DEBUG || EXTENDED_ERROR_INFORMATION
                    ,
                    expression, filepath, memberName, lineNumber
#endif
                    );
            }
        }

        internal unsafe UniqueComPtr<ID3D12Fence> CreateFence(ulong startValue = 0)
        {
            UniqueComPtr<ID3D12Fence> fence = default;

            ThrowIfFailed(DevicePointer->CreateFence(
                startValue,
                0,
                fence.Iid,
                (void**)&fence
            ));

            return fence;
        }

        private protected void CreateDevice(
            in Adapter adapter,
            FeatureLevel level
        )
        {
            lock (AdapterDeviceMap)
            {
                using UniqueComPtr<ID3D12Device> device = default;

                ThrowIfFailed(D3D12CreateDevice(
                    adapter.GetAdapterPointer(),
                    (D3D_FEATURE_LEVEL)level,
                    device.Iid,
                    (void**)&device
                ));

                _ = device.TryQueryInterface(out _deviceDownlevel);

                Device = device.Move();

                LogHelper.LogInformation("New D3D12 device created from adapter: \n{0}", adapter);

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

                AdapterDeviceMap[adapter] = this;
            }
        }

        /// <summary>
        /// Only use this method for debugging. It immeditately removes the device
        /// </summary>
        public void RemoveDevice()
        {
            DevicePointer->RemoveDevice();
        }

        internal T QueryFeatureSupport<T>(D3D12_FEATURE feature) where T : unmanaged
        {
            T val;
            QueryFeatureSupport(feature, &val);
            return val;
        }

        internal void QueryFeatureSupport<T>(D3D12_FEATURE feature, T* pVal) where T : unmanaged
        {
            ThrowIfFailed(DevicePointer->CheckFeatureSupport(feature, pVal, (uint)sizeof(T)));
        }

        internal void QueryFeatureSupport<T>(D3D12_FEATURE feature, out T val) where T : unmanaged
        {
            fixed (T* pVal = &val)
            {
                ThrowIfFailed(DevicePointer->CheckFeatureSupport(feature, pVal, (uint)sizeof(T)));
            }
        }

        private protected virtual void QueryFeaturesOnCreation()
        {
            D3D12_FEATURE_DATA_SHADER_MODEL highest;

            // the highest shader model that the app understands
            highest.HighestShaderModel = D3D_SHADER_MODEL.D3D_SHADER_MODEL_6_6;
            QueryFeatureSupport(D3D12_FEATURE_SHADER_MODEL, &highest);

            GetMajorMinorForShaderModel(highest.HighestShaderModel, out var major, out var minor);
            HighestSupportedShaderModel = new ShaderModel(ShaderType.Unspecified, (byte)major, (byte)minor);

            QueryFeatureSupport<D3D12_FEATURE_DATA_D3D12_OPTIONS>(D3D12_FEATURE_D3D12_OPTIONS, out var options);
            ResourceCount = options.ResourceBindingTier == D3D12_RESOURCE_BINDING_TIER.D3D12_RESOURCE_BINDING_TIER_3 ? 1_000_000 : 1_000_000;

            QueryFeatureSupport(D3D12_FEATURE_ARCHITECTURE1, out D3D12_FEATURE_DATA_ARCHITECTURE1 arch1);
            QueryFeatureSupport(D3D12_FEATURE_GPU_VIRTUAL_ADDRESS_SUPPORT, out D3D12_FEATURE_DATA_GPU_VIRTUAL_ADDRESS_SUPPORT va);

            Architecture = new()
            {
                IsCacheCoherentUma = Helpers.Int32ToBool(arch1.CacheCoherentUMA),
                AddressSpaceBits = (int)va.MaxGPUVirtualAddressBitsPerProcess,
                ResourceAddressBits = (int)va.MaxGPUVirtualAddressBitsPerResource,
                VirtualAddressSpaceSize = 2UL << (int)va.MaxGPUVirtualAddressBitsPerProcess
            };
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

        internal bool TryQueryInterface<T>(out UniqueComPtr<T> result) where T : unmanaged
        {
            var success = ComPtr.TryQueryInterface(DevicePointer, out T* val);
            result = new UniqueComPtr<T>(val);
            return success;
        }

        /// <summary>
        /// Represents a scoped PIX capture that ends when the type is disposed. Use <see cref="BeginScopedCapture"/> to create one.
        /// If PIX is not attached and the debug layer is attached, a message will be emitted explaining that the capture was dropped because PIX was not attached.
        /// If PIX is not attached and the debug layer is disabled, the capture will be silently dropped
        /// </summary>
        public struct ScopedCapture : IDisposable
        {
            /// <summary>
            /// Ends the capture
            /// </summary>
            public void Dispose() => DeviceCreationSettings.EndCapture();
        }

        /// <summary>
        /// Begins a scoped PIX capture that ends when the <see cref="ScopedCapture"/> is disposed
        /// </summary>
        /// <returns>A new <see cref="ScopedCapture"/></returns>
        public ScopedCapture BeginScopedCapture()
        {
            DeviceCreationSettings.BeginCapture();
            return new ScopedCapture();
        }

        /// <summary>
        /// Begins a PIX capture
        /// </summary>
        public void BeginCapture() => DeviceCreationSettings.BeginCapture();

        /// <summary>
        /// Ends a PIX capture
        /// </summary>
        public void EndCapture() => DeviceCreationSettings.EndCapture();

        /// <summary>
        /// Returns a <see cref="CopyContext"/> used for recording copy commands
        /// </summary>
        public CopyContext BeginCopyContext(ContextFlags flags = ContextFlags.None)
        {
            var context = ContextPool.Rent(ExecutionContext.Copy, null, flags);
            return new CopyContext(context);
        }

        /// <summary>
        /// Returns a <see cref="UploadContext"/> used for recording upload commands
        /// </summary>
        /// <returns>A new <see cref="UploadContext"/></returns>
        public UploadContext BeginUploadContext()
        {
            var context = ContextPool.Rent(ExecutionContext.Copy, null, 0);

            return new UploadContext(context);
        }


        /// <summary>
        /// Returns a <see cref="ReadbackContext"/> used for recording readback commands
        /// </summary>
        /// <returns>A new <see cref="ReadbackContext"/></returns>
        public ReadbackContext BeginReadbackContext()
        {
            var context = ContextPool.Rent(ExecutionContext.Copy, null, 0);

            return new ReadbackContext(context);
        }

        /// <summary>
        /// Returns a <see cref="ComputeContext"/> used for recording compute commands
        /// </summary>
        public ComputeContext BeginComputeContext(PipelineStateObject? pso = null, ContextFlags flags = 0)
        {
            var @params = ContextPool.Rent(ExecutionContext.Compute, pso, flags);

            var context = new ComputeContext(@params);
            SetDefaultState(context, pso);
            return context;
        }

        /// <summary>
        /// Submit a set of recorded commands to the list
        /// </summary>
        /// <param name="context">The commands to submit for execution</param>
        public GpuTask Execute(GpuContext context)
            => Execute(context, context switch
            {
                GraphicsContext => ExecutionContext.Graphics,
                ComputeContext => ExecutionContext.Compute,
                CopyContext => ExecutionContext.Copy,
                _ => ThrowHelper.ThrowArgumentException<ExecutionContext>(nameof(context)),
            });

        /// <summary>
        /// Submit a set of recorded commands to the list
        /// </summary>
        /// <param name="context">The commands to submit for execution</param>
        /// <param name="targetQueue">The <see cref="ExecutionContext"/> which indicates which GPU work queue this should be executed on</param>
        public GpuTask Execute(GpuContext context, ExecutionContext targetQueue)
        {
            ID3D12GraphicsCommandList6* list = context.List;

            ref var queue = ref GetQueueForContext(targetQueue);
            if (Unsafe.IsNullRef(ref queue))
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
        public GpuTask Execute(Span<GpuContext> contexts, ExecutionContext targetQueue)
        {
            if (contexts.IsEmpty)
            {
                return GpuTask.Completed;
            }

            StackSentinel.StackAssert(StackSentinel.SafeToStackallocPointers(contexts.Length));

            ID3D12CommandList** ppLists = stackalloc ID3D12CommandList*[contexts.Length];

            for (var i = 0; i < contexts.Length; i++)
            {
                ppLists[i] = (ID3D12CommandList*)contexts[i].List;
            }

            ref var queue = ref GetQueueForContext(targetQueue);
            if (Unsafe.IsNullRef(ref queue))
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


        private unsafe void SetDefaultState(GpuContext context, PipelineStateObject? pso)
        {
            if (pso is not null)
            {
                context.List->SetComputeRootSignature(pso.GetRootSig());
            }

            const int numHeaps = 1;
            var heaps = stackalloc ID3D12DescriptorHeap*[1] { UavCbvSrvs.GetHeap() };

            context.List->SetDescriptorHeaps(numHeaps, heaps);
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


        /// <inheritdoc/>
        public virtual void Dispose()
        {
            Allocator.Dispose();
            PipelineManager.Dispose();
            Device.Dispose();
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
        //SetResidencyPriority

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
