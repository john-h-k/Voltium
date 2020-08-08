using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Voltium.Core;
using Voltium.Core.Memory;
using Voltium.Interactive.BasicRenderPipeline;
using Voltium.Interactive.RenderGraphSamples;
using Voltium.RenderEngine;
using Voltium.Interactive.HelloTriangle;
using Voltium.Interactive.FloatMultiplySample;
using TerraFX.Interop;
using Voltium.Common;
using Voltium.Core.Exceptions;

namespace Voltium.Interactive
{
    internal static unsafe class Program
    {
        private static int Main(string[] args)
        {
            var application = new RenderPipeline();
            return ApplicationRunner.RunWin32(application);
        }
    }
}
