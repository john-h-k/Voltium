using System;
using System.Diagnostics;
using System.Reflection;
using TerraFX.Interop;

namespace Voltium.Common
{
    internal static unsafe class DebugExtensions
    {
        [DebuggerNonUserCode]
        public static void PossibleDeviceDisconnect(this ComPtr<ID3D12Device> device)
            => PossibleDeviceDisconnect(device.Get());

        [DebuggerNonUserCode]
        public static void PossibleDeviceDisconnect(ID3D12Device* device)
        {
#if !DEBUG
            throw new Exception("This should never even be present in release builds. Use only for specific debug issues");
#else
            var reason = device->GetDeviceRemovedReason();
            if (reason == Windows.S_OK)
            {
                return;
            }

            D3D12DebugShim.WriteAllMessages();

            /* inspect this */
            throw new DeviceDisconnectedException("Device disconnected unexpectedly", reason);
#endif
        }

        public static bool IsDeviceRemoved(this ComPtr<ID3D12Device> device) =>
            device.Get()->GetDeviceRemovedReason() != Windows.S_OK;

        public static string DeviceRemovedMessage(int removedReason) => TranslateHr(removedReason);

        public static string TranslateHr(int hr)
        {
#if REFLECTION
            var type = typeof(Windows);
            foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                if (field.GetValue(null) is int value && value == hr)
                {
                    return field.Name;
                }
            }
#endif

            return "<unmapped>";
        }
    }

    internal class DeviceDisconnectedException : Exception
    {
        public DeviceDisconnectedException(string message, int hr, object? otherData = null)
            : base($"{message} -- Error code: {DebugExtensions.DeviceRemovedMessage(hr)}")
        {
        }
    }
}
