using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Extensions.Logging;
using TerraFX.Interop;
using Voltium.Common.Debugging;
using Voltium.Core.Devices;
using Voltium.Core.Managers;
using ZLogger;

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

    internal unsafe class DebugLayer
    {
        private ComPtr<ID3D12InfoQueue> _d3d12InfoQueue;
        private ComPtr<IDXGIInfoQueue> _dxgiInfoQueue;
        private ComPtr<IDXGIDebug1> _dxgiDebugLayer;
        private ComPtr<ID3D12DebugDevice> _d3d12DebugDevice;

        // [Me]          > why is the inheritance tree of the debug layer types so confusing??!
        // [DirectX dev] > Because someone made a mistake
        //
        // Ignore Debug1/2, just have 0 and 3 (which are base/derived)

        private ComPtr<ID3D12Debug> _d3d12DebugLayer;
        private SupportedDebugLayer _supportedLayer;
        private enum SupportedDebugLayer { Debug, Debug3 };

        private Guid _dxgiProducerId = Windows.DXGI_DEBUG_DXGI;
        private ComputeDevice _device = null!;
        private DebugLayerConfiguration _config = null!;

        public DebugLayerConfiguration Config => _config;

        public DebugLayer(DebugLayerConfiguration? config = null)
        {
            _config = config ?? DebugLayerConfiguration.Default;
        }

        // Some aspects require D3D12 or DXGI global state. Horrible i know
        public void SetGlobalStateForConfig()
        {
            if (_config.Validation.GraphicsLayerValidation)
            {
                {
                    using ComPtr<ID3D12Debug> debug = default;
                    Guard.ThrowIfFailed(Windows.D3D12GetDebugInterface(debug.Iid, ComPtr.GetVoidAddressOf(&debug)));
                    _d3d12DebugLayer = debug.Move();
                }


                _d3d12DebugLayer.Get()->EnableDebugLayer();
                _supportedLayer = _d3d12DebugLayer.HasInterface<ID3D12Debug3>() ? SupportedDebugLayer.Debug3 : SupportedDebugLayer.Debug;
            }

            if (_config.Validation.GpuBasedValidation)
            {
                if (_supportedLayer == SupportedDebugLayer.Debug3)
                {
                    _d3d12DebugLayer.AsBase<ID3D12Debug3>().Get()->SetEnableGPUBasedValidation(Windows.TRUE);
                }
                else
                {
                    ThrowHelper.ThrowPlatformNotSupportedException("GPU based validation is not supported on this system");
                }
            }


            if (_config.DeviceRemovedMetadata.RequiresDredSupport)
            {
                using ComPtr<ID3D12DeviceRemovedExtendedDataSettings> dredSettings = default;
                int hr = Windows.D3D12GetDebugInterface(dredSettings.Iid, ComPtr.GetVoidAddressOf(&dredSettings));

                if (hr == Windows.E_NOINTERFACE)
                {
                    ThrowHelper.ThrowPlatformNotSupportedException("GPU based device removed metadata is not supported on this system");
                }

                Guard.ThrowIfFailed(hr, " Windows.D3D12GetDebugInterface(dredSettings.Guid, ComPtr.GetVoidAddressOf(&dredSettings));");

                if (_config.DeviceRemovedMetadata.AutoBreadcrumbMetadata)
                {
                    dredSettings.Get()->SetAutoBreadcrumbsEnablement(D3D12_DRED_ENABLEMENT.D3D12_DRED_ENABLEMENT_FORCED_ON);
                }
                if (_config.DeviceRemovedMetadata.PageFaultMetadata)
                {
                    dredSettings.Get()->SetPageFaultEnablement(D3D12_DRED_ENABLEMENT.D3D12_DRED_ENABLEMENT_FORCED_ON);
                }
                if (_config.DeviceRemovedMetadata.WindowsErrorReporting)
                {
                    dredSettings.Get()->SetWatsonDumpEnablement(D3D12_DRED_ENABLEMENT.D3D12_DRED_ENABLEMENT_FORCED_ON);
                }
            }
        }

        public void SetDeviceStateForConfig(ComputeDevice device)
        {
            _device = device;

            Guard.OnExternalError += FlushQueues;

            if (!_config.Validation.GraphicsLayerValidation && _config.Validation.GpuBasedValidation)
            {
                ThrowHelper.ThrowArgumentException("Cannot have GPU based validation enabled unless graphics layer validation is");
            }

            if (_config.Validation.GraphicsLayerValidation)
            {
                InitializeD3D12();
            }
            if (_config.Validation.InfrastructureLayerValidation)
            {
                InitializeDxgi();
            }
        }

        public enum LiveObjectFlags
        {
            Summary = DXGI_DEBUG_RLO_FLAGS.DXGI_DEBUG_RLO_SUMMARY,
            Detail = DXGI_DEBUG_RLO_FLAGS.DXGI_DEBUG_RLO_DETAIL,
            InternalObjects = DXGI_DEBUG_RLO_FLAGS.DXGI_DEBUG_RLO_IGNORE_INTERNAL,
        }

        public void ReportLiveObjects(LiveObjectFlags flags = LiveObjectFlags.Summary)
        {
            if (!_dxgiDebugLayer.Exists)
            {
                ThrowHelper.ThrowInvalidOperationException("Cannot ReportLiveObjects because layer was not created with InfrastructureLayerValidation. Try ReportDeviceLiveObjects instead");
            }
            Guard.ThrowIfFailed(_dxgiDebugLayer.Get()->ReportLiveObjects(Windows.DXGI_DEBUG_DX, (DXGI_DEBUG_RLO_FLAGS)flags));
        }

        public void ResetGlobalState()
        {
            // Should probably reset DRED and debug layer etc
        }

        public void ReportDeviceLiveObjects(LiveObjectFlags flags = LiveObjectFlags.Summary)
        {
            if (!_d3d12DebugDevice.Exists)
            {
                ThrowHelper.ThrowInvalidOperationException("Cannot ReportDeviceLiveObjects because layer was not created with GraphicsLayerValidation. Try ReportLiveObjects instead");
            }
            Guard.ThrowIfFailed(_d3d12DebugDevice.Get()->ReportLiveDeviceObjects((D3D12_RLDO_FLAGS)flags));
        }

        public void FlushQueues()
        {
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

                        _device.Logger.ZLog(GetLogLevelForSeverity(msgBuffer->Severity), transcoded);
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

                        _device.Logger.ZLog(GetLogLevelForSeverity(msgBuffer->Severity), transcoded);
                    }
                }
            }

            // Guard.ThrowIfFailed calls this to flush messages, so we can't call it
            static void ThrowIfFailed(int hr)
            {
                Console.WriteLine(
                    $"if this next bit of text says E_INVALIDARG then this code is messing up. {DebugExtensions.TranslateHr(hr)}. " +
                    "Else you have really messed up and have managed to break the debug message queue");
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
                Guard.ThrowIfFailed(_d3d12InfoQueue.Get()->SetBreakOnSeverity(allowedSeverities[i], Windows.TRUE));
            }
        }

        private void InitializeDxgi()
        {
            {
                using ComPtr<IDXGIInfoQueue> queue = default;
                Guard.ThrowIfFailed(Windows.DXGIGetDebugInterface(queue.Iid, ComPtr.GetVoidAddressOf(&queue)));
                _dxgiInfoQueue = queue.Move();


                using ComPtr<IDXGIDebug1> debug = default;
                Guard.ThrowIfFailed(Windows.DXGIGetDebugInterface(debug.Iid, ComPtr.GetVoidAddressOf(&debug)));
                _dxgiDebugLayer = debug.Move();
            }

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
                Guard.ThrowIfFailed(_dxgiInfoQueue.Get()->SetBreakOnSeverity(_dxgiProducerId, allowedSeverities[i], Windows.TRUE));
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
