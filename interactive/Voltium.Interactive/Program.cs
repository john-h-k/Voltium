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
using TerraFX.Interop;
using Voltium.Common;
using Voltium.Core.Exceptions;
using Microsoft.Extensions.Logging;
using Voltium.Core.ShaderLang;
using System.Numerics;
using Voltium.Interactive.Samples.Predication;

namespace Voltium.Interactive
{
    internal static unsafe class Program
    {
        private static int Main(string[] args)
        {
            ApplicationRunner.RunWin32(new HelloTriangleApp());
            return 0;
        }
    }

    public class LogBenchmark
    {
        [Benchmark]
        public void Console_WriteLine()
        {
            Console.WriteLine("Hello world, Console.WriteLine here!");
        }

        [Benchmark]
        public void LogHelper_WriteLine()
        {
             //LogHelper.Log(LogLevel.Trace, "Hello world, Console.WriteLine here!");
        }
    }

    //public class HelloWorldShader : Shader
    //{
    //    [StructLayout(LayoutKind.Sequential)]
    //    struct VertexIn
    //    {
    //        [Semantic("Position")] public Vector4 Position;
    //        [Semantic("Color")] public Vector3 Color;
    //    }

    //    struct VertexOut
    //    {
    //        [SV_Position] public Vector4 Position;
    //        public Vector3 Color;
    //    }

    //    [VertexShader]
    //    VertexOut VertexMain(in VertexIn @in)
    //    {
    //        return new VertexOut
    //        {
    //            Position = @in.Position,
    //            Color = @in.Color
    //        };
    //    }


    //    [PixelShader]
    //    [return: SV_Target]
    //    Vector4 PixelMain(in VertexOut @in)
    //    {
    //        return new Vector4(@in.Color, 1);
    //    }
    //}
}
