//using System;
//using System.Diagnostics;
//using System.Reflection.Metadata;
//using System.Runtime.CompilerServices;
//using System.Runtime.InteropServices;
//using System.Text;
//using Microsoft.Extensions.Logging;
//using TerraFX.Interop;
//using Voltium.Common.Debugging;
//using Voltium.Core.Devices;
//using static TerraFX.Interop.Windows;


//namespace Voltium.Common
//{
//    // Enabled if 'ENABLE_DX_DEBUG_SHIM' env var is "true" or "1"

//    // Rider debugger (the https://github.com/samsung/netcoredbg one) doesn't play well with native output
//    // to debug console (OutputDebugString() in C). It works fine in VS19 with native code debugging turned on,
//    // but this allows it to play nicely with other debuggers.
//    // All debug layer messages are written to this queue. This isn't a very customisable shim *yet* (TODO)
//    // and it currently just filters out info/other messages, and makes external code throw
//    // an SEHException when an error/warning is emitted. 'WriteAllMessages' is called by various error handler
//    // code and will give out all the input for inspection

//    // This also nicely alows



//    /// <summary>
//    /// Defines global settings for creation of a <see cref="ComputeDevice"/> or <see cref="GraphicsDevice"/>
//    /// </summary>
//    public static unsafe class DeviceCreationSettings
//    {

//        private static UniqueComPtr<IDXGIDebug1> _dxgiDebugLayer = GetDxgiDebug();
//        private static UniqueComPtr<IDXGraphicsAnalysis> _frameCapture = GetPixIfAttached();
//        private static UniqueComPtr<ID3D12Debug> _debug = GetDebug(out _supportedLayer);
//        private static UniqueComPtr<ID3D12DeviceRemovedExtendedDataSettings1> _dred = GetDred();
//        private static SupportedDebugLayer _supportedLayer;
//        private enum SupportedDebugLayer { Unknown, Debug, Debug3 };

//        private static UniqueComPtr<ID3D12Debug> GetDebug(out SupportedDebugLayer supportedLayer)
//        {
//            using UniqueComPtr<ID3D12Debug> debug = default;
//            Guard.TryGetInterface(D3D12GetDebugInterface(debug.Iid, (void**)&debug));
//            supportedLayer = debug.HasInterface<ID3D12Debug3>() ? SupportedDebugLayer.Debug3 : SupportedDebugLayer.Debug;
//            return debug.Move();
//        }

//        private static UniqueComPtr<IDXGIDebug1> GetDxgiDebug()
//        {
//            using UniqueComPtr<IDXGIDebug1> debug = default;
//            Guard.TryGetInterface(DXGIGetDebugInterface1(0, debug.Iid, (void**)&debug));
//            return debug.Move();
//        }

//        private static UniqueComPtr<ID3D12DeviceRemovedExtendedDataSettings1> GetDred()
//        {
//            using UniqueComPtr<ID3D12DeviceRemovedExtendedDataSettings1> dredSettings = default;
//            Guard.TryGetInterface(D3D12GetDebugInterface(dredSettings.Iid, (void**)&dredSettings));
//            return dredSettings.Move();
//        }

//        private static UniqueComPtr<IDXGraphicsAnalysis> GetPixIfAttached()
//        {
//            using UniqueComPtr<IDXGraphicsAnalysis> analysis = default;
//            Guard.TryGetInterface(DXGIGetDebugInterface1(0, analysis.Iid, (void**)&analysis));
//            if (analysis.Exists)
//            {
//                LogHelper.LogInformation("PIX debugger is attached");
//            }

//            return analysis.Move();
//        }

//        /// <summary>
//        /// Enables the D3D12 debug layer
//        /// </summary>
//        public static void EnableDebugLayer() => _debug.Ptr->EnableDebugLayer();

//        /// <summary>
//        /// Enables GPU-based validation. This can help provide significantly more metadata than the traditional debug layer,
//        /// but can have a major performance penalty
//        /// </summary>
//        public static void EnableGpuBasedValidation()
//        {
//            if (_supportedLayer == SupportedDebugLayer.Debug3)
//            {
//                _debug.As<ID3D12Debug3>().Ptr->SetEnableGPUBasedValidation(TRUE);
//            }
//            else
//            {
//                ThrowHelper.ThrowPlatformNotSupportedException("GPU based validation is not supported on this system");
//            }
//        }

//        /// <summary>
//        /// Enables Device-Removed Extended Metadata (DRED)
//        /// </summary>
//        /// <param name="features">The <see cref="DredFlags"/> to enable</param>
//        public static void EnableDred(DredFlags features)
//        {
//            if (!_dred.Exists && features != DredFlags.None)
//            {
//                ThrowHelper.ThrowPlatformNotSupportedException("GPU based device removed metadata is not supported on this system");
//            }

//            if (features.HasFlag(DredFlags.AutoBreadcrumbs))
//            {
//                _dred.Ptr->SetAutoBreadcrumbsEnablement(D3D12_DRED_ENABLEMENT.D3D12_DRED_ENABLEMENT_FORCED_ON);
//            }
//            if (features.HasFlag(DredFlags.PageFaultMetadata))
//            {
//                _dred.Ptr->SetPageFaultEnablement(D3D12_DRED_ENABLEMENT.D3D12_DRED_ENABLEMENT_FORCED_ON);
//            }
//            if (features.HasFlag(DredFlags.WatsonDumpEnablement))
//            {
//                _dred.Ptr->SetWatsonDumpEnablement(D3D12_DRED_ENABLEMENT.D3D12_DRED_ENABLEMENT_FORCED_ON);
//            }
//        }


//        internal static void BeginCapture()
//        {
//            if (_frameCapture.Exists)
//            {
//                _frameCapture.Ptr->BeginCapture();
//            }
//            else
//            {
//                LogPixNotAttached();
//            }
//        }

//        internal static void EndCapture()
//        {
//            if (_frameCapture.Exists)
//            {
//                _frameCapture.Ptr->EndCapture();
//            }
//            else
//            {
//                LogPixNotAttached();
//            }
//        }

//        /// <summary>
//        /// <see langword="true"/> if PIX is attached, else <see langword="false"/>
//        /// </summary>
//        public static bool IsPixAttached => _frameCapture.Exists;

//        private static void LogPixNotAttached()
//        {
//            LogHelper.LogInformation("PIX Frame capture was created but PIX is not attached, so the capture was dropped");
//        }
//    }

//    internal unsafe class DebugLayer
//    {
//        private UniqueComPtr<ID3D12InfoQueue> _d3d12InfoQueue;
//        private UniqueComPtr<IDXGIInfoQueue> _dxgiInfoQueue;
//        private UniqueComPtr<ID3D12DebugDevice> _d3d12DebugDevice;

//        // For some reason, Debug3 inherits from Debug, but Debug1 and Debug2 are seperate types
//        // [Me]          > why is the inheritance tree of the debug layer types so confusing??!
//        // [DirectX dev] > Because someone made a mistake
//        //
//        // Ignore Debug1/2, just have 0 and 3 (which are base/derived)

//        private UniqueComPtr<ID3D12Debug> _d3d12DebugLayer;

//        private Guid _dxgiProducerId = DXGI_DEBUG_DXGI;
//        private ComputeDevice _device = null!;
//        private DebugLayerConfiguration _config = null!;

//        public bool IsActive { get; }

//        public DebugLayerConfiguration Config => _config;

//        public DebugLayer(ComputeDevice device, DebugLayerConfiguration? config)
//        {
//            IsActive = config is not null;
//            _config = config!;

//            if (config is null)
//            {
//                return;
//            }

//            _device = device;

//            if (!_config.DebugFlags.HasFlag(DebugFlags.DebugLayer) && _config.DebugFlags.HasFlag(DebugFlags.GpuBasedValidation))
//            {
//                ThrowHelper.ThrowArgumentException("Cannot have GPU based validation enabled unless graphics layer validation is enabled");
//            }

//            if (_config.DebugFlags.HasFlag(DebugFlags.DebugLayer))
//            {
//                InitializeD3D12();
//            }
//        }

//        public enum LiveObjectFlags
//        {
//            Summary = DXGI_DEBUG_RLO_FLAGS.DXGI_DEBUG_RLO_SUMMARY,
//            Detail = DXGI_DEBUG_RLO_FLAGS.DXGI_DEBUG_RLO_DETAIL,
//            InternalObjects = DXGI_DEBUG_RLO_FLAGS.DXGI_DEBUG_RLO_IGNORE_INTERNAL,
//        }

//        public void ResetGlobalState()
//        {
//            // Should probably reset DRED and debug layer etc
//        }

//        public void ReportDeviceLiveObjects(LiveObjectFlags flags = LiveObjectFlags.Summary)
//        {
//            if (!IsActive)
//            {
//                return;
//            }

//            if (!_d3d12DebugDevice.Exists)
//            {
//                ThrowHelper.ThrowInvalidOperationException("Cannot ReportDeviceLiveObjects because layer was not created with GraphicsLayerValidation. Try ReportLiveObjects instead");
//            }
//            _device.ThrowIfFailed(_d3d12DebugDevice.Ptr->ReportLiveDeviceObjects((D3D12_RLDO_FLAGS)flags));
//        }

//        public void FlushQueues()
//        {
//            if (!IsActive)
//            {
//                return;
//            }

//            if (_d3d12InfoQueue.Exists)
//            {
//                for (ulong i = 0; i < _d3d12InfoQueue.Ptr->GetNumStoredMessagesAllowedByRetrievalFilter(); i++)
//                {
//                    nuint pLength;
//                    ThrowIfFailed(_d3d12InfoQueue.Ptr->GetMessage(i, null, &pLength));

//                    int length = (int)pLength;

//                    using var rented = RentedArray<byte>.Create(length);

//                    string transcoded;

//                    fixed (void* pHeapBuffer = rented.Value)
//                    {
//                        var msgBuffer = (D3D12_MESSAGE*)pHeapBuffer;
//                        ThrowIfFailed(_d3d12InfoQueue.Ptr->GetMessage(i, msgBuffer, &pLength));

//                        transcoded = Encoding.ASCII.GetString(
//                            (byte*)msgBuffer->pDescription,
//                            (int)msgBuffer->DescriptionByteLength
//                        );

//                        LogHelper.Log(GetLogLevelForSeverity(msgBuffer->Severity), transcoded);
//                    }
//                }
//            }

//            if (_dxgiInfoQueue.Exists)
//            {
//                for (ulong i = 0; i < _dxgiInfoQueue.Ptr->GetNumStoredMessagesAllowedByRetrievalFilters(_dxgiProducerId); i++)
//                {
//                    nuint pLength;
//                    ThrowIfFailed(_dxgiInfoQueue.Ptr->GetMessage(_dxgiProducerId, i, null, &pLength));

//                    int length = (int)pLength;

//                    using var rented = RentedArray<byte>.Create(length);

//                    string transcoded;

//                    fixed (void* pHeapBuffer = rented.Value)
//                    {
//                        var msgBuffer = (DXGI_INFO_QUEUE_MESSAGE*)pHeapBuffer;
//                        ThrowIfFailed(_dxgiInfoQueue.Ptr->GetMessage(_dxgiProducerId, i, msgBuffer, &pLength));

//                        transcoded = Encoding.ASCII.GetString(
//                            (byte*)msgBuffer->pDescription,
//                            (int)msgBuffer->DescriptionByteLength
//                        );

//                        LogHelper.Log(GetLogLevelForSeverity(msgBuffer->Severity), transcoded);
//                    }
//                }
//            }

//            // _device.ThrowIfFailed calls this to flush messages, so we can't call it
//            static void ThrowIfFailed(int hr)
//            {
//                LogHelper.LogError(
//                    "if this next bit of text says E_INVALIDARG then this code is messing up. {0}. " +
//                    "Else you have really messed up and have managed to break the debug message queue", DebugExtensions.TranslateHr(hr));
//            }
//        }


//        private static readonly D3D12_MESSAGE_ID[] BannedMessages = new D3D12_MESSAGE_ID[]
//        {
//            D3D12_MESSAGE_ID.D3D12_MESSAGE_ID_LOADPIPELINE_NAMENOTFOUND,
//            D3D12_MESSAGE_ID.D3D12_MESSAGE_ID_CREATEPIPELINELIBRARY_INVALIDLIBRARYBLOB,
//        };

//        private void InitializeD3D12()
//        {
//            void CreateAndAssert<T>(out UniqueComPtr<T> result) where T : unmanaged
//            {
//                var success = _device.TryQueryInterface(out result);
//                Debug.Assert(success);
//                _ = success;
//            }

//            CreateAndAssert(out _d3d12DebugDevice);
//            CreateAndAssert(out _d3d12InfoQueue);

//            Span<D3D12_MESSAGE_SEVERITY> allowedSeverities = stackalloc D3D12_MESSAGE_SEVERITY[MaxSeverityCount];

//            GetSeveritiesForLogLevel(_config.ValidationLogLevel, allowedSeverities, out int numAllowed);

//            fixed (D3D12_MESSAGE_ID* pBanned = BannedMessages)
//            {
//                var filter = new D3D12_INFO_QUEUE_FILTER
//                {
//                    AllowList = new D3D12_INFO_QUEUE_FILTER_DESC
//                    {
//                        NumSeverities = (uint)numAllowed,
//                        pSeverityList = (D3D12_MESSAGE_SEVERITY*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(allowedSeverities))
//                    },
//                    DenyList = new D3D12_INFO_QUEUE_FILTER_DESC
//                    {
//                        pIDList = pBanned,
//                        NumIDs = (uint)BannedMessages.Length
//                    }
//                };

//                _device.ThrowIfFailed(_d3d12InfoQueue.Ptr->AddStorageFilterEntries(&filter));
//            }

//            GetSeveritiesForLogLevel(_config.BreakpointLogLevel, allowedSeverities, out int numBreakOn);

//            for (var i = 0; i < numBreakOn; i++)
//            {
//                _device.ThrowIfFailed(_d3d12InfoQueue.Ptr->SetBreakOnSeverity(allowedSeverities[i], TRUE));
//            }
//        }

//        private void InitializeDxgi()
//        {
//            UniqueComPtr<IDXGIInfoQueue> infoQueue = default;
//            _device.ThrowIfFailed(DXGIGetDebugInterface1(0, infoQueue.Iid, (void**)&infoQueue));
//            _dxgiInfoQueue = infoQueue.Move();

//            // we deny retrieving anything that isn't an error/warning/corruption
//            Span<DXGI_INFO_QUEUE_MESSAGE_SEVERITY> allowedSeverities = stackalloc DXGI_INFO_QUEUE_MESSAGE_SEVERITY[MaxSeverityCount];

//            GetSeveritiesForLogLevel(_config.ValidationLogLevel, allowedSeverities, out int numAllowed);

//            var filter = new DXGI_INFO_QUEUE_FILTER
//            {
//                AllowList = new DXGI_INFO_QUEUE_FILTER_DESC
//                {
//                    NumSeverities = (uint)numAllowed,
//                    pSeverityList = (DXGI_INFO_QUEUE_MESSAGE_SEVERITY*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(allowedSeverities))
//                }
//            };

//            _device.ThrowIfFailed(_dxgiInfoQueue.Ptr->AddRetrievalFilterEntries(_dxgiProducerId, &filter));

//            GetSeveritiesForLogLevel(_config.BreakpointLogLevel, allowedSeverities, out int numBreakOn);

//            for (var i = 0; i < numBreakOn; i++)
//            {
//                _device.ThrowIfFailed(_dxgiInfoQueue.Ptr->SetBreakOnSeverity(_dxgiProducerId, allowedSeverities[i], TRUE));
//            }
//        }

//        private int MaxSeverityCount = 5;

//        private LogLevel GetLogLevelForSeverity(D3D12_MESSAGE_SEVERITY severity)
//        {
//            return severity switch
//            {
//                D3D12_MESSAGE_SEVERITY.D3D12_MESSAGE_SEVERITY_CORRUPTION => LogLevel.Critical,
//                D3D12_MESSAGE_SEVERITY.D3D12_MESSAGE_SEVERITY_ERROR => LogLevel.Error,
//                D3D12_MESSAGE_SEVERITY.D3D12_MESSAGE_SEVERITY_WARNING => LogLevel.Warning,
//                D3D12_MESSAGE_SEVERITY.D3D12_MESSAGE_SEVERITY_INFO => LogLevel.Information,
//                D3D12_MESSAGE_SEVERITY.D3D12_MESSAGE_SEVERITY_MESSAGE => LogLevel.Trace,
//                _ => (LogLevel)(-1)
//            };
//        }

//        private LogLevel GetLogLevelForSeverity(DXGI_INFO_QUEUE_MESSAGE_SEVERITY severity)
//        {
//            return severity switch
//            {
//                DXGI_INFO_QUEUE_MESSAGE_SEVERITY.DXGI_INFO_QUEUE_MESSAGE_SEVERITY_CORRUPTION => LogLevel.Critical,
//                DXGI_INFO_QUEUE_MESSAGE_SEVERITY.DXGI_INFO_QUEUE_MESSAGE_SEVERITY_ERROR => LogLevel.Error,
//                DXGI_INFO_QUEUE_MESSAGE_SEVERITY.DXGI_INFO_QUEUE_MESSAGE_SEVERITY_WARNING => LogLevel.Warning,
//                DXGI_INFO_QUEUE_MESSAGE_SEVERITY.DXGI_INFO_QUEUE_MESSAGE_SEVERITY_INFO => LogLevel.Information,
//                DXGI_INFO_QUEUE_MESSAGE_SEVERITY.DXGI_INFO_QUEUE_MESSAGE_SEVERITY_MESSAGE => LogLevel.Trace,
//                _ => (LogLevel)(-1)
//            };
//        }

//        private void GetSeveritiesForLogLevel(LogLevel level, Span<D3D12_MESSAGE_SEVERITY> severities, out int numSeverities)
//        {
//            numSeverities = 0;
//            switch (level)
//            {
//                case LogLevel.Trace:
//                    severities[numSeverities] = D3D12_MESSAGE_SEVERITY.D3D12_MESSAGE_SEVERITY_MESSAGE;
//                    numSeverities++;
//                    goto case LogLevel.Debug;

//                case LogLevel.Debug:
//                case LogLevel.Information:
//                    severities[numSeverities] = D3D12_MESSAGE_SEVERITY.D3D12_MESSAGE_SEVERITY_INFO;
//                    numSeverities++;
//                    goto case LogLevel.Warning;

//                case LogLevel.Warning:
//                    severities[numSeverities] = D3D12_MESSAGE_SEVERITY.D3D12_MESSAGE_SEVERITY_WARNING;
//                    numSeverities++;
//                    goto case LogLevel.Error;

//                case LogLevel.Error:
//                    severities[numSeverities] = D3D12_MESSAGE_SEVERITY.D3D12_MESSAGE_SEVERITY_ERROR;
//                    numSeverities++;
//                    goto case LogLevel.Critical;

//                case LogLevel.Critical:
//                    severities[numSeverities] = D3D12_MESSAGE_SEVERITY.D3D12_MESSAGE_SEVERITY_CORRUPTION;
//                    numSeverities++;
//                    goto case LogLevel.None;

//                case LogLevel.None:
//                    break;
//            }
//        }

//        private void GetSeveritiesForLogLevel(LogLevel level, Span<DXGI_INFO_QUEUE_MESSAGE_SEVERITY> severities, out int numSeverities)
//        {
//            numSeverities = 0;
//            switch (level)
//            {
//                case LogLevel.Trace:
//                    severities[numSeverities] = DXGI_INFO_QUEUE_MESSAGE_SEVERITY.DXGI_INFO_QUEUE_MESSAGE_SEVERITY_MESSAGE;
//                    numSeverities++;
//                    goto case LogLevel.Debug;

//                case LogLevel.Debug:
//                case LogLevel.Information:
//                    severities[numSeverities] = DXGI_INFO_QUEUE_MESSAGE_SEVERITY.DXGI_INFO_QUEUE_MESSAGE_SEVERITY_INFO;
//                    numSeverities++;
//                    goto case LogLevel.Warning;

//                case LogLevel.Warning:
//                    severities[numSeverities] = DXGI_INFO_QUEUE_MESSAGE_SEVERITY.DXGI_INFO_QUEUE_MESSAGE_SEVERITY_WARNING;
//                    numSeverities++;
//                    goto case LogLevel.Error;

//                case LogLevel.Error:
//                    severities[numSeverities] = DXGI_INFO_QUEUE_MESSAGE_SEVERITY.DXGI_INFO_QUEUE_MESSAGE_SEVERITY_ERROR;
//                    numSeverities++;
//                    goto case LogLevel.Critical;

//                case LogLevel.Critical:
//                    severities[numSeverities] = DXGI_INFO_QUEUE_MESSAGE_SEVERITY.DXGI_INFO_QUEUE_MESSAGE_SEVERITY_CORRUPTION;
//                    numSeverities++;
//                    goto case LogLevel.None;

//                case LogLevel.None:
//                    break;
//            }
//        }

//        public void Dispose()
//        {
//            _d3d12InfoQueue.Dispose();
//            _dxgiInfoQueue.Dispose();
//        }
//    }
//}
