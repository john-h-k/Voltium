using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Voltium.Analyzers.HLSLType
{
    //[Generator]
    internal sealed class HLSLTypeGenerator : ISourceGenerator
    {
        public void Initialize(InitializationContext context)
        {
            // Explicit nop
        }

        public void Execute(SourceGeneratorContext context)
        {
            foreach (var file in context.AdditionalFiles)
            {
                if (!IsShader(file))
                {
                    continue;
                }

                Generate(context, file);
            }
        }

        private void Generate(SourceGeneratorContext context, AdditionalText file)
        {
            //Debugger.Launch();
            var hlsl = file.GetText()!.ToString();

            var forbidden = hlsl.Contains("class") ? "class" : hlsl.Contains("interface") ? "interface" : null;
            if (forbidden is not null)
            {
                //var ind = hlsl.IndexOf(forbidden);
                //var forbiddenSpan = new TextSpan(ind, forbidden.Length);

                //var precedingLines = hlsl.AsSpan().Slice(0, ind);
                //var lineCount = 0;

                //for (var i = 0; i < precedingLines.Length; i++)
                //{
                //    if (precedingLines[i] == '\n')
                //    {
                //        lineCount++;
                //    }
                //}

                //var forbiddenLineSpan = new LinePositionSpan(new LinePosition(lineCount, 0), new LinePosition(lineCount, forbidden.Length));
                //context.ReportDiagnostic(Diagnostic.Create(ContainsClassesOrInterfaces, Location.Create(file.Path, forbiddenSpan, forbiddenLineSpan)));
                context.ReportDiagnostic(Diagnostic.Create(ContainsClassesOrInterfaces, Location.Create(file.Path, new TextSpan(0, 1), new LinePositionSpan(new LinePosition(0, 1), new LinePosition(0, 2)))));
                return;
            }

            if (!hlsl.Contains("struct"))
            {
                return;
            }

            var @namespace = file.Path.AsMemory().Slice(file.Path.LastIndexOf('.'));
        }

#pragma warning disable RS2008 // Enable analyzer release tracking
        private static DiagnosticDescriptor ContainsClassesOrInterfaces => new DiagnosticDescriptor("HLSL 69", "HLSL classes and interfaces not supported", "HLSL classes and interfaces are deprecated in DX12", "HLSL", DiagnosticSeverity.Error, true);
#pragma warning restore RS2008 // Enable analyzer release tracking

        private static bool IsShader(AdditionalText? file)
            => file?.Path is not null && (file.Path.EndsWith(".hlsl", StringComparison.OrdinalIgnoreCase) || file.Path.EndsWith(".hlsli", StringComparison.OrdinalIgnoreCase));

        private static INamedTypeSymbol GetSymbolForType<T>(Compilation comp) => comp.GetTypeByMetadataName(typeof(T).FullName!) ?? throw new ArgumentException("Invalid type");

        private void InitBasicTypes(Compilation comp)
        {
            if (BasicTypes is object)
            {
                return;
            }

            BasicTypes = new Dictionary<ITypeSymbol, string>()
            {
                [GetSymbolForType<sbyte>(comp)] = "ShaderInputType.Int8",
                [GetSymbolForType<byte>(comp)] = "ShaderInputType.UInt8",
                [GetSymbolForType<short>(comp)] = "ShaderInputType.Int16",
                [GetSymbolForType<ushort>(comp)] = "ShaderInputType.UInt16",
                [GetSymbolForType<int>(comp)] = "ShaderInputType.Int32",
                [GetSymbolForType<uint>(comp)] = "ShaderInputType.UInt32",

                [GetSymbolForType<float>(comp)] = "ShaderInputType.Float",
                [GetSymbolForType<Vector2>(comp)] = "ShaderInputType.Float2",
                [GetSymbolForType<Vector3>(comp)] = "ShaderInputType.Float3",
                [GetSymbolForType<Vector4>(comp)] = "ShaderInputType.Float4",
                [GetSymbolForType<Matrix3x2>(comp)] = "ShaderInputType.Float3x2",
                [GetSymbolForType<Matrix4x4>(comp)] = "ShaderInputType.Float4x4",
            };
        }

        private Dictionary<ITypeSymbol, string> BasicTypes = null!;
    }
}
