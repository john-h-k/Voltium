using System;
using System.Buffers;
using System.Diagnostics;
using System.Text;
using TerraFX.Interop;
using Voltium.Common.Debugging;

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

    internal static unsafe class D3D12DebugShim
    {
        private static ComPtr<ID3D12InfoQueue> _infoQueue;

        static D3D12DebugShim()
        {
        }

        [Conditional("DEBUG")]
        public static void Initialize(ComPtr<ID3D12InfoQueue> infoQueue)
        {
            if (!EnvVars.IsD3D12ShimEnabled)
            {
                return;
            }

            Debug.Assert(infoQueue.Exists);
            _infoQueue = infoQueue.Move();

            // we deny retrieving anything that isn't an error/warning/corruption
            const int count = 2;
            var deniedSeverities = stackalloc D3D12_MESSAGE_SEVERITY[count]
            {
                D3D12_MESSAGE_SEVERITY.D3D12_MESSAGE_SEVERITY_INFO,
                D3D12_MESSAGE_SEVERITY.D3D12_MESSAGE_SEVERITY_MESSAGE
            };

            var filter = new D3D12_INFO_QUEUE_FILTER
            {
                DenyList = new D3D12_INFO_QUEUE_FILTER_DESC {NumSeverities = count, pSeverityList = deniedSeverities}
            };

            Guard.ThrowIfFailed(_infoQueue.Get()->AddRetrievalFilterEntries(&filter));

            // this causes an SEH exception to be thrown every time a DX error or corruption occurs
#if THROW_ON_D3D12_ERROR
            infoQueue.Get()->SetBreakOnSeverity(D3D12_MESSAGE_SEVERITY.D3D12_MESSAGE_SEVERITY_ERROR, Windows.TRUE);
            infoQueue.Get()->SetBreakOnSeverity(D3D12_MESSAGE_SEVERITY.D3D12_MESSAGE_SEVERITY_CORRUPTION, Windows.TRUE);
#endif
#if THROW_ON_WARNING
            infoQueue.Get()->SetBreakOnSeverity(D3D12_MESSAGE_SEVERITY.D3D12_MESSAGE_SEVERITY_WARNING, Windows.TRUE);
#endif
        }

        [Conditional("DEBUG")]
        public static void WriteAllMessages()
        {
            if (!EnvVars.IsD3D12ShimEnabled)
            {
                return;
            }

            byte* buffer = stackalloc byte[StackSentinel.MaxStackallocBytes];
            for (ulong i = 0; i < _infoQueue.Get()->GetNumStoredMessagesAllowedByRetrievalFilter(); i++)
            {
                nuint pLength;
                int hr = _infoQueue.Get()->GetMessage(i, null, &pLength);

                int length = (int)pLength;

                byte[]? rented = null;
                string transcoded;
                try
                {
                    if (length < StackSentinel.MaxStackallocBytes)
                    {
                        var msgBuffer = (D3D12_MESSAGE*)buffer;
                        _infoQueue.Get()->GetMessage(i, (D3D12_MESSAGE*)buffer, &pLength);
                        transcoded = Encoding.ASCII.GetString(
                            (byte*)msgBuffer->pDescription,
                            (int)msgBuffer->DescriptionByteLength
                        );
                    }
                    else
                    {
                        rented = ArrayPool<byte>.Shared.Rent(length);

                        fixed (byte* pHeapBuffer = &rented[0])
                        {
                            var msgBuffer = (D3D12_MESSAGE*)pHeapBuffer;
                            _infoQueue.Get()->GetMessage(i, msgBuffer, &pLength);
                            transcoded = Encoding.ASCII.GetString(
                                (byte*)msgBuffer->pDescription,
                                (int)msgBuffer->DescriptionByteLength
                            );
                        }
                    }
                }
                finally
                {
                    if (rented is object)
                    {
                        ArrayPool<byte>.Shared.Return(rented);
                    }
                }

                // cant really Guard.ThrowIfFailed here because that calls this method
                if (Windows.FAILED(hr))
                {
                    Console.WriteLine(
                        $"if this next bit of text says E_INVALIDARG then this code is messing up. {DebugExtensions.TranslateHr(hr)}. " +
                        "Else you have really messed up and have managed to break the debug message queue");
                }

                Console.WriteLine(transcoded);
            }
        }

        public static void Dispose() => _infoQueue.Dispose();
    }
}
