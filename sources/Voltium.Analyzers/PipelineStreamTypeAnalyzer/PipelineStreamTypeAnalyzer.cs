using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Voltium.Analyzers.PipelineStreamTypeAnalyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal sealed class PipelineStreamTypeAnalyzer : DiagnosticAnalyzer
    {
        internal const string Category = "Correctness";

        private const string IPipelineStreamElement = "Voltium.Core.Pipeline.IPipelineStreamElement`1";
        private const string IPipelineStreamType = "Voltium.Core.Pipeline.IPipelineStreamType";

        public static readonly DiagnosticDescriptor NonPipelineElementField = new DiagnosticDescriptor(
#pragma warning disable RS2008 // Enable analyzer release tracking
           "VO1111",
#pragma warning restore RS2008 // Enable analyzer release tracking
           "InvalidPipelineStream",
           "Types which are IPipelineStreamType should only contain IPipelineElement<T> subobjects",
           Category,
           DiagnosticSeverity.Error,
           true
        );

        public static readonly DiagnosticDescriptor NotValueTypeIPipelineStream = new DiagnosticDescriptor(
#pragma warning disable RS2008 // Enable analyzer release tracking
           "VO1112",
#pragma warning restore RS2008 // Enable analyzer release tracking
           "InvalidPipelineStream",
           "Types which are IPipelineStreamType should always be structs",
           Category,
           DiagnosticSeverity.Error,
           true
        );



        public static readonly DiagnosticDescriptor PipelineStreamTypeHasAutoProps = new DiagnosticDescriptor(
#pragma warning disable RS2008 // Enable analyzer release tracking
           "VO1113",
#pragma warning restore RS2008 // Enable analyzer release tracking
           "InvalidPipelineStream",
           "Types which are IPipelineStreamType should only contain fields, not auto-properties",
           Category,
           DiagnosticSeverity.Error,
           true
        );

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(NonPipelineElementField, NotValueTypeIPipelineStream, PipelineStreamTypeHasAutoProps);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
            context.EnableConcurrentExecution();
            context.RegisterSymbolAction((symbolContext) =>
            {
                var symbol = (INamedTypeSymbol)symbolContext.Symbol;
                if (symbol.Interfaces.Contains(symbolContext.Compilation.GetTypeByMetadataName(IPipelineStreamType), SymbolEqualityComparer.Default))
                {
                    if (!symbol.IsValueType)
                    {
                        symbolContext.ReportDiagnostic(Diagnostic.Create(NotValueTypeIPipelineStream, symbol.Locations[0]));
                    }   

                    var elementType = symbolContext.Compilation.GetTypeByMetadataName(IPipelineStreamElement)!.ConstructUnboundGenericType();

                    foreach (var field in symbol.GetMembers().OfType<IFieldSymbol>())
                    {
                        if (field.IsImplicitlyDeclared)
                        {
                            symbolContext.ReportDiagnostic(Diagnostic.Create(PipelineStreamTypeHasAutoProps, field.Locations[0]));
                        }

                        var genericInterfaces = field.Type.AllInterfaces.Where(@interface => @interface.IsGenericType).Select(@interface => @interface.ConstructUnboundGenericType());
                        if (!genericInterfaces.Contains(elementType, SymbolEqualityComparer.Default) && /* special case the enum */ field.Type.Name != "TopologyClass")
                        {
                            symbolContext.ReportDiagnostic(Diagnostic.Create(NonPipelineElementField, field.Locations[0]));
                        }
                    }
                }
            }, SymbolKind.NamedType);
        }
    }
}
