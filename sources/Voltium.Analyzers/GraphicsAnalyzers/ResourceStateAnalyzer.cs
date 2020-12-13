using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Voltium.Analyzers.GraphicsAnalyzers
{
#pragma warning disable RS2008 // Enable analyzer release tracking
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal sealed class ResourceStateAnalyzer : DiagnosticAnalyzer
    {
        public static readonly DiagnosticDescriptor OnlySingleWriteFlag = new DiagnosticDescriptor(
           "VR0001",
           nameof(OnlySingleWriteFlag),
           "Only a single writable state may be used at once, and cannot be combined with other states",
           "Correctness",
           DiagnosticSeverity.Error,
           true
        );

        public static readonly DiagnosticDescriptor CantCombineWithCommonOrPresent = new DiagnosticDescriptor(
           "VR0002",
           nameof(CantCombineWithCommonOrPresent),
           "ResourceState.Common and ResourceState.Present cannot be combined with other states",
           "Correctness",
           DiagnosticSeverity.Error,
           true
        );

        public static readonly DiagnosticDescriptor ReadToReadTransitionIsInefficient = new DiagnosticDescriptor(
           "VR0003",
           nameof(ReadToReadTransitionIsInefficient),
           "Performing a resource barrier from a read-only state to a read-only state is ineffecient, and both states should be combined in the previous barrier",
           "Performance",
           DiagnosticSeverity.Warning,
           true
        );

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(OnlySingleWriteFlag, CantCombineWithCommonOrPresent, ReadToReadTransitionIsInefficient);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
            context.EnableConcurrentExecution();
        }
    }
}
