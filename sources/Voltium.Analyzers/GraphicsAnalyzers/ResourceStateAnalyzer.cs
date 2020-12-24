using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Voltium.Analyzers.GraphicsAnalyzers
{
#pragma warning disable RS2008 // Enable analyzer release tracking
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal sealed class ResourceStateAnalyzer : DiagnosticAnalyzer
    {
        private const string ResourceStateName = "Voltium.Core.ResourceState";
        private const string ResourceStateInfoName = "Voltium.Core.ResourceStateInfo";
        private const string AccessName = "Voltium.Core.Access";

        private enum Access
        {
            Opaque = 0,
            Read = 1,
            Write = 2,
            ReadWrite = Read | Write
        }


        public static readonly DiagnosticDescriptor CantCombineReadAndWriteFlags = new DiagnosticDescriptor(
           "VR0001",
           nameof(CantCombineReadAndWriteFlags),
           "A writable state ({0}) cannot be combined with a read state ({1})",
           "Correctness",
           DiagnosticSeverity.Error,
           isEnabledByDefault: true
        );

        public static readonly DiagnosticDescriptor OnlySingleWriteFlag = new DiagnosticDescriptor(
           "VR0002",
           nameof(OnlySingleWriteFlag),
           "Only a single writable state ({0}) may be used at once, and cannot be combined with other write states ({1})",
           "Correctness",
           DiagnosticSeverity.Error,
           isEnabledByDefault: true
        );

        public static readonly DiagnosticDescriptor CantCombineWithCommonOrPresent = new DiagnosticDescriptor(
           "VR0003",
           nameof(CantCombineWithCommonOrPresent),
           "ResourceState.Common and ResourceState.Present cannot be combined with other states ({0})",
           "Correctness",
           DiagnosticSeverity.Error,
           isEnabledByDefault: true
        );

        public static readonly DiagnosticDescriptor ReadToReadTransitionIsInefficient = new DiagnosticDescriptor(
           "VR0004",
           nameof(ReadToReadTransitionIsInefficient),
           "Performing a resource barrier from a read-only state ({0}) to a read-only state ({0}) is inefficient, and both states should be combined in the previous barrier",
           "Performance",
           DiagnosticSeverity.Warning,
           isEnabledByDefault: true
        );

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(CantCombineReadAndWriteFlags, OnlySingleWriteFlag, CantCombineWithCommonOrPresent, ReadToReadTransitionIsInefficient);



        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction((context) =>
                {
                    var resStateType = context.Compilation.GetTypeByMetadataName(ResourceStateName);
                    var model = context.SemanticModel;


                    var lhs = ((BinaryExpressionSyntax)context.Node).Left;
                    var rhs = ((BinaryExpressionSyntax)context.Node).Right;
                    var lhsType = model.GetTypeInfo(lhs).Type!;
                    var rhsType = model.GetTypeInfo(rhs).Type!;

                    if (!SymbolEqualityComparer.Default.Equals(lhsType, resStateType)
                        || !SymbolEqualityComparer.Default.Equals(rhsType, resStateType))
                    {
                        return;
                    }


                    if (!context.Node.IsKind(SyntaxKind.BitwiseOrExpression))
                    {
                        return;
                    }

                    static bool TryGetConstantValue<T>(SemanticModel model, ExpressionSyntax expr, out T? val)
                    {
                        var opt = model.GetConstantValue(expr);
                        val = opt.HasValue ? (T)opt.Value : default;
                        return opt.HasValue;
                    }

                    if (!TryGetConstantValue<uint>(model, lhs, out var leftVal)
                        || !TryGetConstantValue<uint>(model, rhs, out var rightVal))
                    {
                        return;
                    }

                    var enumVals = resStateType.GetMembers().OfType<IFieldSymbol>().Where(field => field.IsConst);

                    Access lhsAccess = (Access)(int)GetEnumField(leftVal).GetAttribute(ResourceStateInfoName, context.Compilation).ConstructorArguments[0].Value!;
                    Access rhsAccess = (Access)(int)GetEnumField(rightVal).GetAttribute(ResourceStateInfoName, context.Compilation).ConstructorArguments[0].Value!;

                    IFieldSymbol GetEnumField(uint val) => enumVals.Where(field => (uint)field.ConstantValue! == val).First();
                    bool IsWriteCombinedWithWrite(Access left, Access right) => left.HasFlag(Access.Write) && right.HasFlag(Access.Write);

                    bool IsWriteCombinedWithRead(Access left, Access right) => left != right && (left | right) == Access.ReadWrite;

                    if (leftVal == /* Common/Present */ 0 || rightVal == 0)
                    {
                        context.ReportDiagnostic(Diagnostic.Create(CantCombineWithCommonOrPresent, context.Node.GetLocation(), (leftVal == 0 ? rhs : lhs).ToString()));
                    }
                    else if (IsWriteCombinedWithWrite(lhsAccess, rhsAccess))
                    {
                        context.ReportDiagnostic(Diagnostic.Create(OnlySingleWriteFlag, context.Node.GetLocation(), lhs.ToString(), rhs.ToString()));
                    }
                    else if (IsWriteCombinedWithRead(lhsAccess, rhsAccess) || IsWriteCombinedWithRead(lhsAccess, rhsAccess))
                    {
                        context.ReportDiagnostic(Diagnostic.Create(CantCombineReadAndWriteFlags, context.Node.GetLocation(), lhs.ToString(), rhs.ToString()));
                    }
                },
                SyntaxKind.BitwiseOrExpression
            );

            //context.RegisterSyntaxNodeAction(
            //    context =>
            //    {
            //        var model = context.SemanticModel;
            //        var node = context.Node;
            //        var copyContextType = context.Compilation.GetTypeByMetadataName(CopyContextName);

            //        if (!(model.GetSymbolInfo(node).Symbol is IMethodSymbol method
            //            && SymbolEqualityComparer.Default.Equals(method.ContainingType, copyContextType)
            //            && method.Name == BarrierName))
            //        {
            //            return;
            //        }


            //    },
            //    SyntaxKind.InvocationExpression
            //);
        }

        private const string CopyContextName = "Voltium.Core.CopyContext";
        private const string BarrierName = "Barrier";
    }
}
