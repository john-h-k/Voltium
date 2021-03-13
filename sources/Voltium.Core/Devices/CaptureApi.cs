using TerraFX.Interop;
using Voltium.Common;

namespace Voltium.Core.Devices
{
    /// <summary>
    /// Exposes methods for interfacing with tooling captures
    /// </summary>
    public static unsafe class CaptureApi
    {
        private static UniqueComPtr<IDXGraphicsAnalysis> _capture = GetPixCapture();

        private static UniqueComPtr<IDXGraphicsAnalysis> GetPixCapture()
        {
            UniqueComPtr<IDXGraphicsAnalysis> capture = default;
            _ = Windows.DXGIGetDebugInterface1(0, capture.Iid, (void**)&capture);
            return capture;
        }

        /// <summary>
        /// Begin a new capture
        /// </summary>
        public static void BeginCapture()
        {
            if (_capture.Exists)
            {
                _capture.Ptr->BeginCapture();
            }
            else
            {
                LogPixNotAttached();
            }
        }

        /// <summary>
        /// End a capture
        /// </summary>
        public static void EndCapture()
        {
            if (_capture.Exists)
            {
                _capture.Ptr->EndCapture();
            }
            else
            {
                LogPixNotAttached();
            }
        }

        /// <summary>
        /// <see langword="true"/> if PIX is attached, else <see langword="false"/>
        /// </summary>
        public static bool IsPixAttached => _capture.Exists;

        private static void LogPixNotAttached()
        {
            LogHelper.LogInformation("PIX Frame capture was created but PIX is not attached, so the capture was dropped");
        }
    }
}
