using System;
using System.Diagnostics;
using Voltium.Core;

namespace Voltium.Interactive
{
    internal static class Program
    {
        private static int Main(string[] args)
        {
            Debug.WriteLine("Executing...");
            var application = new DirectXHelloWorldApplication();
            return Win32Application.Run(application);
        }
    }
}
