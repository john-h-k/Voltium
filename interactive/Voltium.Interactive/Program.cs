using System;
using System.Diagnostics;
using TerraFX.Interop;

namespace Voltium.Interactive
{
    internal static class Program
    {
        private static unsafe void Main(string[] args)
        {
            Debug.WriteLine("Executing...");
            var application = new DirectXHelloWorldApplication();
            Win32Application.Run(application, Windows.GetModuleHandleW(null), Windows.SW_SHOWDEFAULT);
        }
    }
}
