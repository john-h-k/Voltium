//using System;
//using System.Collections.Generic;
//using System.Collections.Immutable;
//using System.Diagnostics;
//using System.Text;
//using Microsoft.CodeAnalysis;
//using Microsoft.CodeAnalysis.CSharp;
//using Microsoft.CodeAnalysis.CSharp.Syntax;
//using Microsoft.CodeAnalysis.Diagnostics;

//namespace Voltium.Analyzers
//{
//#pragma warning disable RS2008 // Enable analyzer release tracking
//    [DiagnosticAnalyzer(LanguageNames.CSharp)]
//    internal sealed class CannotAccessShaderAnalyzer : DiagnosticAnalyzer
//    {
//        private const string ShaderTypeName = "Voltium.Core.ShaderLang.Shader";

//        public static readonly DiagnosticDescriptor CannotAccessShaderFromOutsideShader = new DiagnosticDescriptor(
//           "VSC0000",
//           nameof(CannotAccessShaderFromOutsideShader),
//           "A shader method or type ({0}) cannot be accessed from outside of a shader context ({1})",
//           "Correctness",
//           DiagnosticSeverity.Error,
//           isEnabledByDefault: true
//        );

//        public static readonly DiagnosticDescriptor CannotAccessOutsideShaderFromShader = new DiagnosticDescriptor(
//           "VSC0001",
//           nameof(CannotAccessShaderFromOutsideShader),
//           "A non-shader method or type ({0}) cannot be accessed from a shader context ({1})",
//           "Correctness",
//           DiagnosticSeverity.Error,
//           isEnabledByDefault: true
//        );

//        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(CannotAccessShaderFromOutsideShader, CannotAccessOutsideShaderFromShader);

//        public override void Initialize(AnalysisContext context)
//        {
//            return;
//            //context.EnableConcurrentExecution();
//            //context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
//            //context.RegisterSyntaxNodeAction(
//            //    context =>
//            //    {
//            //        var model = context.SemanticModel;
//            //        var node = (MemberAccessExpressionSyntax)context.Node;
//            //        var shaderType = context.Compilation.GetTypeByMetadataName(ShaderTypeName);

//            //        if (false && !Debugger.IsAttached)
//            //        {
//            //            Debugger.Launch();
//            //        }
//            //        else
//            //        {
//            //            Debugger.Break();
//            //        }

//            //        SyntaxNode? type = node;
//            //        while (type is not TypeDeclarationSyntax decl)
//            //        {
//            //            type = type?.Parent;

//            //            if (type is null)
//            //            {
//            //                return;
//            //            }
//            //        }

//            //        var accessorType = model.GetTypeInfo(type).Type!;
//            //        var accessType = model.GetTypeInfo(node).Type!;

//            //        var accessorIsShader = IsShaderType(accessorType);
//            //        var accessIsShader = IsShaderType(accessType);

//            //        if (accessorIsShader && !accessIsShader)
//            //        {
//            //            context.ReportDiagnostic(Diagnostic.Create(CannotAccessOutsideShaderFromShader, node.GetLocation(), accessType.ToDisplayString(), accessorType.ToDisplayString())); 
//            //        }
//            //        else if (!accessorIsShader && accessIsShader)
//            //        {
//            //            context.ReportDiagnostic(Diagnostic.Create(CannotAccessShaderFromOutsideShader, node.GetLocation(), accessorType.ToDisplayString(), accessType.ToDisplayString()));
//            //        }

//            //        bool IsShaderType(ITypeSymbol type) => SymbolEqualityComparer.Default.Equals(shaderType, type);
//            //    },
//            //    SyntaxKind.SimpleMemberAccessExpression
//            //);
//        }
//    }
//}
