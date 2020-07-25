using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using BenchmarkDotNet.Attributes;
using Microsoft.UI.Xaml;
using Voltium.Core;
using Voltium.Core.Memory;
using Voltium.Interactive.BasicRenderPipeline;
using Voltium.Interactive.RenderGraphSamples;

namespace Voltium.Interactive
{
    internal static class Program
    {
        private static int Main(string[] args)
        {
            var application = new RenderPipeline();
            return Win32Application.Run(application, 700, 700);
        }
    }
}
