using System;
using System.Diagnostics;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Extensions.Logging;
using TerraFX.Interop;
using Voltium.Common.Debugging;
using Voltium.Core.Devices;
using static TerraFX.Interop.Windows;


namespace Voltium.Common
{
    // Enabled if 'ENABLE_DX_DEBUG_SHIM' env var is "true" or "1"

    // Rider debugger (the https://github.com/samsung/netcoredbg one) doesn't play well with native output
    // to debug console (OutputDebugString() in C). It works fine in VS19 with native code debugging turned on,
    // but this allows it to play nicely with other debuggers.
    // All debug layer messages are written to this queue. This isn't a very customisable shim *yet* (TODO)
    // and it currently just filters out info/other messages, and makes external code throw
    // an SEHException when an error/warning is emitted. 'WriteAllMessages' is called by various error handler
    // code and will give out all the input for inspection

    // This also nicely alows

    /// <summary>
    /// Defines the settings for Device-Removed Extended Data (DRED)
    /// </summary>
    [Flags]
    public enum DredFlags
    {
        /// <summary>
        /// None. DRED is disabled
        /// </summary>
        None = 0,

        /// <summary>
        /// Auto-breadcrumb metadata to track execution progress is enabled
        /// </summary>
        AutoBreadcrumbs = 1,

        /// <summary>
        /// Allocation metadata to track page faults is enabled
        /// </summary>
        PageFaultMetadata = 2,

        /// <summary>
        /// Watson dump is enabled via Windows Error Reporting (WER)
        /// </summary>
        WatsonDumpEnablement = 4,

        /// <summary>
        /// All DRED settings are enabled
        /// </summary>
        All = AutoBreadcrumbs | PageFaultMetadata | WatsonDumpEnablement
    }

    /// <summary>
    /// Defines the settings for the debug layer
    /// </summary>
    [Flags]
    public enum DebugFlags
    {
        /// <summary>
        /// None. The debug layer is disabled
        /// </summary>
        None = 0,

        /// <summary>
        /// Enable the debug layer
        /// </summary>
        DebugLayer = 1 << 0,

        /// <summary>
        /// Enable GPU-based validation. This allow more thorough debugging but will significantly slow down your app
        /// </summary>
        GpuBasedValidation = 1 << 1,
    }

    /// <summary>
    /// Defines global settings for creation of a <see cref="ComputeDevice"/> or <see cref="GraphicsDevice"/>
    /// </summary>
    public unsafe static class DeviceCreationSettings
    {
        internal static bool HasDeviceBeenCreated = false;

        private static void ThrowIfDeviceCreated()
        {
            if (HasDeviceBeenCreated)
            {
                ThrowHelper.ThrowInvalidOperationException("Cannot change DeviceCreationSettings after a device has been created");
            }
        }

        internal static bool AreMetaCommandsEnabled { get; private set; } = false;
        internal static bool AreExperimentalShaderModelsEnabled { get; private set; } = false;

        private static ComPtr<IDXGIDebug1> _dxgiDebugLayer = GetDxgiDebug();
        private static ComPtr<IDXGraphicsAnalysis> _frameCapture = GetPixIfAttached();
        private static ComPtr<ID3D12Debug> _debug = GetDebug(out _supportedLayer);
        private static ComPtr<ID3D12DeviceRemovedExtendedDataSettings> _dred = GetDred();
        private static SupportedDebugLayer _supportedLayer;
        private enum SupportedDebugLayer { Unknown, Debug, Debug3 };

        private static ComPtr<ID3D12Debug> GetDebug(out SupportedDebugLayer supportedLayer)
        {
            using ComPtr<ID3D12Debug> debug = default;
            Guard.TryGetInterface(D3D12GetDebugInterface(debug.Iid, ComPtr.GetVoidAddressOf(&debug)));
            supportedLayer = debug.HasInterface<ID3D12Debug3>() ? SupportedDebugLayer.Debug3 : SupportedDebugLayer.Debug;
            return debug.Move();
        }

        private static ComPtr<IDXGIDebug1> GetDxgiDebug()
        {
            using ComPtr<IDXGIDebug1> debug = default;
            Guard.TryGetInterface(DXGIGetDebugInterface1(0, debug.Iid, ComPtr.GetVoidAddressOf(&debug)));
            return debug.Move();
        }

        private static ComPtr<ID3D12DeviceRemovedExtendedDataSettings> GetDred()
        {
            using ComPtr<ID3D12DeviceRemovedExtendedDataSettings> dredSettings = default;
            Guard.TryGetInterface(D3D12GetDebugInterface(dredSettings.Iid, ComPtr.GetVoidAddressOf(&dredSettings)));
            return dredSettings;
        }

        private static ComPtr<IDXGraphicsAnalysis> GetPixIfAttached()
        {
            using ComPtr<IDXGraphicsAnalysis> analysis = default;
            Guard.TryGetInterface(DXGIGetDebugInterface1(0, analysis.Iid, ComPtr.GetVoidAddressOf(&analysis)));
            if (analysis.Exists)
            {
                LogHelper.LogInformation("PIX debugger is attached");
            }

            return analysis.Move();
        }

        /// <summary>
        /// Enables the experimental D3D12 Meta Commands feature
        /// </summary>
        public static void EnableMetaCommands()
        {
            Guid iid = D3D12MetaCommand;
            Guard.ThrowIfFailed(D3D12EnableExperimentalFeatures(1, &iid, null, null));
            AreMetaCommandsEnabled = true;
        }

        /// <summary>
        /// Enables experimental D3D12 shader models
        /// </summary>
        public static void EnableExperimentalShaderModels()
        {
            Guid iid = D3D12ExperimentalShaderModels;
            Guard.ThrowIfFailed(D3D12EnableExperimentalFeatures(1, &iid, null, null));
            AreExperimentalShaderModelsEnabled = true;
        }

        /// <summary>
        /// Enables the D3D12 debug layer
        /// </summary>
        public static void EnableDebugLayer() => _debug.Get()->EnableDebugLayer();

        /// <summary>
        /// Enables GPU-based validation. This can help provide significantly more metadata than the traditional debug layer,
        /// but can have a major performance penalty
        /// </summary>
        public static void EnableGpuBasedValidation()
        {
            if (_supportedLayer == SupportedDebugLayer.Debug3)
            {
                _debug.AsBase<ID3D12Debug3>().Get()->SetEnableGPUBasedValidation(TRUE);
            }
            else
            {
                ThrowHelper.ThrowPlatformNotSupportedException("GPU based validation is not supported on this system");
            }
        }

        /// <summary>
        /// Enables Device-Removed Extended Metadata (DRED)
        /// </summary>
        /// <param name="features">The <see cref="DredFlags"/> to enable</param>
        public static void EnableDred(DredFlags features)
        {
            if (!_dred.Exists && features != DredFlags.None)
            {
                ThrowHelper.ThrowPlatformNotSupportedException("GPU based device removed metadata is not supported on this system");
            }

            if (features.HasFlag(DredFlags.AutoBreadcrumbs))
            {
                _dred.Get()->SetAutoBreadcrumbsEnablement(D3D12_DRED_ENABLEMENT.D3D12_DRED_ENABLEMENT_FORCED_ON);
            }
            if (features.HasFlag(DredFlags.PageFaultMetadata))
            {
                _dred.Get()->SetPageFaultEnablement(D3D12_DRED_ENABLEMENT.D3D12_DRED_ENABLEMENT_FORCED_ON);
            }
            if (features.HasFlag(DredFlags.WatsonDumpEnablement))
            {
                _dred.Get()->SetWatsonDumpEnablement(D3D12_DRED_ENABLEMENT.D3D12_DRED_ENABLEMENT_FORCED_ON);
            }
        }


        internal static void BeginCapture()
        {
            if (_frameCapture.Exists)
            {
                _frameCapture.Get()->BeginCapture();
            }
            else
            {
                LogPixNotAttached();
            }
        }

        internal static void EndCapture()
        {
            if (_frameCapture.Exists)
            {
                _frameCapture.Get()->EndCapture();
            }
            else
            {
                LogPixNotAttached();
            }
        }

        /// <summary>
        /// <see langword="true"/> if PIX is attached, else <see langword="false"/>
        /// </summary>
        public static bool IsPixAttached => _frameCapture.Exists;

        private static void LogPixNotAttached()
        {
            LogHelper.LogInformation("PIX Frame capture was created but PIX is not attached, so the capture was dropped");
        }
    }

    internal unsafe class DebugLayer
    {
        private ComPtr<ID3D12InfoQueue> _d3d12InfoQueue;
        private ComPtr<IDXGIInfoQueue> _dxgiInfoQueue;
        private ComPtr<ID3D12DebugDevice> _d3d12DebugDevice;

        // For some reason, Debug3 inherits from Debug, but Debug1 and Debug2 are seperate types
        // [Me]          > why is the inheritance tree of the debug layer types so confusing??!
        // [DirectX dev] > Because someone made a mistake
        //
        // Ignore Debug1/2, just have 0 and 3 (which are base/derived)

        private ComPtr<ID3D12Debug> _d3d12DebugLayer;

        private Guid _dxgiProducerId = DXGI_DEBUG_DXGI;
        private ComputeDevice _device = null!;
        private DebugLayerConfiguration _config = null!;

        public bool IsActive { get; }

        public DebugLayerConfiguration Config => _config;

        public DebugLayer(ComputeDevice device, DebugLayerConfiguration? config)
        {
            IsActive = config is not null;
            _config = config!;

            if (config is null)
            {
                return;
            }

            _device = device;

            if (!_config.DebugFlags.HasFlag(DebugFlags.DebugLayer) && _config.DebugFlags.HasFlag(DebugFlags.GpuBasedValidation))
            {
                ThrowHelper.ThrowArgumentException("Cannot have GPU based validation enabled unless graphics layer validation is enabled");
            }

            if (_config.DebugFlags.HasFlag(DebugFlags.DebugLayer))
            {
                InitializeD3D12();
            }
        }

        public enum LiveObjectFlags
        {
            Summary = DXGI_DEBUG_RLO_FLAGS.DXGI_DEBUG_RLO_SUMMARY,
            Detail = DXGI_DEBUG_RLO_FLAGS.DXGI_DEBUG_RLO_DETAIL,
            InternalObjects = DXGI_DEBUG_RLO_FLAGS.DXGI_DEBUG_RLO_IGNORE_INTERNAL,
        }

        public void ResetGlobalState()
        {
            // Should probably reset DRED and debug layer etc
        }

        public void ReportDeviceLiveObjects(LiveObjectFlags flags = LiveObjectFlags.Summary)
        {
            if (!IsActive)
            {
                return;
            }

            if (!_d3d12DebugDevice.Exists)
            {
                ThrowHelper.ThrowInvalidOperationException("Cannot ReportDeviceLiveObjects because layer was not created with GraphicsLayerValidation. Try ReportLiveObjects instead");
            }
            Guard.ThrowIfFailed(_d3d12DebugDevice.Get()->ReportLiveDeviceObjects((D3D12_RLDO_FLAGS)flags));
        }

        public void FlushQueues()
        {
            if (!IsActive)
            {
                return;
            }

            if (!EnvVars.IsD3D12ShimEnabled)
            {
                return;
            }

            if (_d3d12InfoQueue.Exists)
            {
                for (ulong i = 0; i < _d3d12InfoQueue.Get()->GetNumStoredMessagesAllowedByRetrievalFilter(); i++)
                {
                    nuint pLength;
                    ThrowIfFailed(_d3d12InfoQueue.Get()->GetMessage(i, null, &pLength));

                    int length = (int)pLength;

                    using var rented = RentedArray<byte>.Create(length);

                    string transcoded;

                    fixed (void* pHeapBuffer = rented.Value)
                    {
                        var msgBuffer = (D3D12_MESSAGE*)pHeapBuffer;
                        ThrowIfFailed(_d3d12InfoQueue.Get()->GetMessage(i, msgBuffer, &pLength));

                        transcoded = Encoding.ASCII.GetString(
                            (byte*)msgBuffer->pDescription,
                            (int)msgBuffer->DescriptionByteLength
                        );

                        LogHelper.Log(GetLogLevelForSeverity(msgBuffer->Severity), transcoded);
                    }
                }
            }

            if (_dxgiInfoQueue.Exists)
            {
                for (ulong i = 0; i < _dxgiInfoQueue.Get()->GetNumStoredMessagesAllowedByRetrievalFilters(_dxgiProducerId); i++)
                {
                    nuint pLength;
                    ThrowIfFailed(_dxgiInfoQueue.Get()->GetMessage(_dxgiProducerId, i, null, &pLength));

                    int length = (int)pLength;

                    using var rented = RentedArray<byte>.Create(length);

                    string transcoded;

                    fixed (void* pHeapBuffer = rented.Value)
                    {
                        var msgBuffer = (DXGI_INFO_QUEUE_MESSAGE*)pHeapBuffer;
                        ThrowIfFailed(_dxgiInfoQueue.Get()->GetMessage(_dxgiProducerId, i, msgBuffer, &pLength));

                        transcoded = Encoding.ASCII.GetString(
                            (byte*)msgBuffer->pDescription,
                            (int)msgBuffer->DescriptionByteLength
                        );

                        LogHelper.Log(GetLogLevelForSeverity(msgBuffer->Severity), transcoded);
                    }
                }
            }

            // Guard.ThrowIfFailed calls this to flush messages, so we can't call it
            static void ThrowIfFailed(int hr)
            {
                LogHelper.LogError(
                    "if this next bit of text says E_INVALIDARG then this code is messing up. {0}. " +
                    "Else you have really messed up and have managed to break the debug message queue", DebugExtensions.TranslateHr(hr));
            }
        }

        private void InitializeD3D12()
        {
            void CreateAndAssert<T>(out ComPtr<T> result) where T : unmanaged
            {
                var success = _device.TryQueryInterface(out result);
                Debug.Assert(success);
                _ = success;
            }

            CreateAndAssert(out _d3d12DebugDevice);
            CreateAndAssert(out _d3d12InfoQueue);

            Span<D3D12_MESSAGE_SEVERITY> allowedSeverities = stackalloc D3D12_MESSAGE_SEVERITY[MaxSeverityCount];

            GetSeveritiesForLogLevel(_config.ValidationLogLevel, allowedSeverities, out int numAllowed);

            var filter = new D3D12_INFO_QUEUE_FILTER
            {
                AllowList = new D3D12_INFO_QUEUE_FILTER_DESC
                {
                    NumSeverities = (uint)numAllowed,
                    pSeverityList = (D3D12_MESSAGE_SEVERITY*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(allowedSeverities))
                }
            };

            Guard.ThrowIfFailed(_d3d12InfoQueue.Get()->AddRetrievalFilterEntries(&filter));

            GetSeveritiesForLogLevel(_config.BreakpointLogLevel, allowedSeverities, out int numBreakOn);

            for (var i = 0; i < numBreakOn; i++)
            {
                Guard.ThrowIfFailed(_d3d12InfoQueue.Get()->SetBreakOnSeverity(allowedSeverities[i], TRUE));
            }
        }

        private void InitializeDxgi()
        {
            ComPtr<IDXGIInfoQueue> infoQueue = default;
            Guard.ThrowIfFailed(DXGIGetDebugInterface1(0, infoQueue.Iid, ComPtr.GetVoidAddressOf(&infoQueue)));
            _dxgiInfoQueue = infoQueue.Move();

            // we deny retrieving anything that isn't an error/warning/corruption
            Span<DXGI_INFO_QUEUE_MESSAGE_SEVERITY> allowedSeverities = stackalloc DXGI_INFO_QUEUE_MESSAGE_SEVERITY[MaxSeverityCount];

            GetSeveritiesForLogLevel(_config.ValidationLogLevel, allowedSeverities, out int numAllowed);

            var filter = new DXGI_INFO_QUEUE_FILTER
            {
                AllowList = new DXGI_INFO_QUEUE_FILTER_DESC
                {
                    NumSeverities = (uint)numAllowed,
                    pSeverityList = (DXGI_INFO_QUEUE_MESSAGE_SEVERITY*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(allowedSeverities))
                }
            };

            Guard.ThrowIfFailed(_dxgiInfoQueue.Get()->AddRetrievalFilterEntries(_dxgiProducerId, &filter));

            GetSeveritiesForLogLevel(_config.BreakpointLogLevel, allowedSeverities, out int numBreakOn);

            for (var i = 0; i < numBreakOn; i++)
            {
                Guard.ThrowIfFailed(_dxgiInfoQueue.Get()->SetBreakOnSeverity(_dxgiProducerId, allowedSeverities[i], TRUE));
            }
        }

        private int MaxSeverityCount = 5;

        private LogLevel GetLogLevelForSeverity(D3D12_MESSAGE_SEVERITY severity)
        {
            return severity switch
            {
                D3D12_MESSAGE_SEVERITY.D3D12_MESSAGE_SEVERITY_CORRUPTION => LogLevel.Critical,
                D3D12_MESSAGE_SEVERITY.D3D12_MESSAGE_SEVERITY_ERROR => LogLevel.Error,
                D3D12_MESSAGE_SEVERITY.D3D12_MESSAGE_SEVERITY_WARNING => LogLevel.Warning,
                D3D12_MESSAGE_SEVERITY.D3D12_MESSAGE_SEVERITY_INFO => LogLevel.Information,
                D3D12_MESSAGE_SEVERITY.D3D12_MESSAGE_SEVERITY_MESSAGE => LogLevel.Trace,
                _ => (LogLevel)(-1)
            };
        }

        private LogLevel GetLogLevelForSeverity(DXGI_INFO_QUEUE_MESSAGE_SEVERITY severity)
        {
            return severity switch
            {
                DXGI_INFO_QUEUE_MESSAGE_SEVERITY.DXGI_INFO_QUEUE_MESSAGE_SEVERITY_CORRUPTION => LogLevel.Critical,
                DXGI_INFO_QUEUE_MESSAGE_SEVERITY.DXGI_INFO_QUEUE_MESSAGE_SEVERITY_ERROR => LogLevel.Error,
                DXGI_INFO_QUEUE_MESSAGE_SEVERITY.DXGI_INFO_QUEUE_MESSAGE_SEVERITY_WARNING => LogLevel.Warning,
                DXGI_INFO_QUEUE_MESSAGE_SEVERITY.DXGI_INFO_QUEUE_MESSAGE_SEVERITY_INFO => LogLevel.Information,
                DXGI_INFO_QUEUE_MESSAGE_SEVERITY.DXGI_INFO_QUEUE_MESSAGE_SEVERITY_MESSAGE => LogLevel.Trace,
                _ => (LogLevel)(-1)
            };
        }

        private void GetSeveritiesForLogLevel(LogLevel level, Span<D3D12_MESSAGE_SEVERITY> severities, out int numSeverities)
        {
            numSeverities = 0;
            switch (level)
            {
                case LogLevel.Trace:
                    severities[numSeverities] = D3D12_MESSAGE_SEVERITY.D3D12_MESSAGE_SEVERITY_MESSAGE;
                    numSeverities++;
                    goto case LogLevel.Debug;

                case LogLevel.Debug:
                case LogLevel.Information:
                    severities[numSeverities] = D3D12_MESSAGE_SEVERITY.D3D12_MESSAGE_SEVERITY_INFO;
                    numSeverities++;
                    goto case LogLevel.Warning;

                case LogLevel.Warning:
                    severities[numSeverities] = D3D12_MESSAGE_SEVERITY.D3D12_MESSAGE_SEVERITY_WARNING;
                    numSeverities++;
                    goto case LogLevel.Error;

                case LogLevel.Error:
                    severities[numSeverities] = D3D12_MESSAGE_SEVERITY.D3D12_MESSAGE_SEVERITY_ERROR;
                    numSeverities++;
                    goto case LogLevel.Critical;

                case LogLevel.Critical:
                    severities[numSeverities] = D3D12_MESSAGE_SEVERITY.D3D12_MESSAGE_SEVERITY_CORRUPTION;
                    numSeverities++;
                    goto case LogLevel.None;

                case LogLevel.None:
                    break;
            }
        }

        private void GetSeveritiesForLogLevel(LogLevel level, Span<DXGI_INFO_QUEUE_MESSAGE_SEVERITY> severities, out int numSeverities)
        {
            numSeverities = 0;
            switch (level)
            {
                case LogLevel.Trace:
                    severities[numSeverities] = DXGI_INFO_QUEUE_MESSAGE_SEVERITY.DXGI_INFO_QUEUE_MESSAGE_SEVERITY_MESSAGE;
                    numSeverities++;
                    goto case LogLevel.Debug;

                case LogLevel.Debug:
                case LogLevel.Information:
                    severities[numSeverities] = DXGI_INFO_QUEUE_MESSAGE_SEVERITY.DXGI_INFO_QUEUE_MESSAGE_SEVERITY_INFO;
                    numSeverities++;
                    goto case LogLevel.Warning;

                case LogLevel.Warning:
                    severities[numSeverities] = DXGI_INFO_QUEUE_MESSAGE_SEVERITY.DXGI_INFO_QUEUE_MESSAGE_SEVERITY_WARNING;
                    numSeverities++;
                    goto case LogLevel.Error;

                case LogLevel.Error:
                    severities[numSeverities] = DXGI_INFO_QUEUE_MESSAGE_SEVERITY.DXGI_INFO_QUEUE_MESSAGE_SEVERITY_ERROR;
                    numSeverities++;
                    goto case LogLevel.Critical;

                case LogLevel.Critical:
                    severities[numSeverities] = DXGI_INFO_QUEUE_MESSAGE_SEVERITY.DXGI_INFO_QUEUE_MESSAGE_SEVERITY_CORRUPTION;
                    numSeverities++;
                    goto case LogLevel.None;

                case LogLevel.None:
                    break;
            }
        }

        public void Dispose()
        {
            _d3d12InfoQueue.Dispose();
            _dxgiInfoQueue.Dispose();
        }
    }
}
