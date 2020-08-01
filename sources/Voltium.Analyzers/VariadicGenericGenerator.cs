using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Voltium.Analyzers
{
    internal sealed class VariadicGenericGenerator : PredicatedGenerator<MethodDeclarationSyntax>
    {
        private const string AttributeName = "Voltium.Common.VariadicGenericAttribute";
        private const string TargetExpressionName = "InsertExpressionHere";
        protected override void Generate(SourceGeneratorContext context, ISymbol symbol, MethodDeclarationSyntax syntax)
        {
            Debug.Assert(symbol is IMethodSymbol);

            var attr = symbol.GetAttributes().Where(attr => SymbolEqualityComparer.Default.Equals(attr.AttributeClass, context.Compilation.GetTypeByMetadataName(AttributeName))).First();

            string template = (string)attr.ConstructorArguments[0].Value!;
            int minArgs = (int)attr.ConstructorArguments[1].Value!;
            int maxArgs = (int)attr.ConstructorArguments[2].Value!;

            StringBuilder builder = new();
            for (var i = minArgs; i <= maxArgs; i++)
            {
                WriteMethod(builder, context, (IMethodSymbol)symbol, syntax, template, i);
                builder.AppendLine();
            }
        }

        private void WriteMethod(StringBuilder builder, SourceGeneratorContext context, IMethodSymbol symbol, MethodDeclarationSyntax syntax, string template, int argCount)
        {
            var targetAttribute = context.Compilation.GetTypeByMetadataName(AttributeName);
            var targetSymbol = targetAttribute!.GetMembers(TargetExpressionName).First();

            var semantics = context.Compilation.GetSemanticModel(syntax.SyntaxTree);

            WriteMethodDecl(builder, context, symbol, syntax, argCount);
            var body = syntax.Body;

            if (body is null)
            {
                return; // abstract method
            }

            int targetStatementIndex = 0;
            foreach (var statement in body.Statements)
            {
                if (statement is ExpressionStatementSyntax expr && expr.Expression is  InvocationExpressionSyntax invoke
                    && invoke.Expression is IdentifierNameSyntax target && SymbolEqualityComparer.Default.Equals(semantics.GetDeclaredSymbol(target), targetSymbol))
                {
                    break;
                }
                targetStatementIndex++;
            }

            var stripTarget = body.Statements.RemoveAt(targetStatementIndex);
        }

        private void WriteMethodDecl(StringBuilder builder, SourceGeneratorContext context, IMethodSymbol symbol, MethodDeclarationSyntax syntax, int argCount)
        {
            builder.AppendLine(symbol.ToDisplayString());
        }

        private const string PartialTemplate = @"
namespace {0}
{{
    partial {1} {2}
    {{
        {3}
    }}
}}";

        protected override bool Predicate(SourceGeneratorContext context, ISymbol decl)
            => decl.HasAttribute(AttributeName, context.Compilation);
    }
}
