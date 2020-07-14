using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Voltium.Core;
using Voltium.Interactive.BasicRenderPipeline;
using Voltium.Interactive.RenderGraphSamples;

namespace Voltium.Interactive
{
    internal static class Program
    {
        private static int Main(string[] args)
        {
            Debug.WriteLine("Executing...");

            var application = new RenderPipeline();
            return Win32Application.Run(application, 700, 700);
        }
    }
}
