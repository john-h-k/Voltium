using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Voltium.Analyzers
{
    [Generator]
    internal sealed class VariadicGenericGenerator : PredicatedGenerator<MethodDeclarationSyntax>
    {
        private const string AttributeName = "Voltium.Common.VariadicGenericAttribute";
        private const string TargetExpressionName = "InsertExpressionsHere";
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

            var source = ((IMethodSymbol)symbol).ContainingType.CreatePartialDecl(builder.ToString());

            context.AddSource($"{symbol.Name}.Variadics.cs", SourceText.From(source, Encoding.UTF8));
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
                if (statement is ExpressionStatementSyntax expr
                    && expr.Expression is InvocationExpressionSyntax invoke
                    && SymbolEqualityComparer.Default.Equals(semantics.GetSymbolInfo(invoke).Symbol, targetSymbol))
                {
                    break;
                }
                targetStatementIndex++;
            }

            var stmts = body.Statements.RemoveAt(targetStatementIndex).Select(s => s.ToString()).ToList();

            var insertions = Enumerable.Range(0, argCount).Select(i => template.Replace("%t", $"t{i}"));

            stmts.InsertRange(targetStatementIndex, insertions);
            builder.Append("{" + string.Join(";\n", stmts) + ";\n" + "}");
        }

        private void WriteMethodDecl(StringBuilder builder, SourceGeneratorContext context, IMethodSymbol symbol, MethodDeclarationSyntax syntax, int argCount)
        {
            syntax = syntax.AddTypeParameterListParameters(Enumerable.Range(0, argCount).Select(s => SyntaxFactory.TypeParameter($"T{s}")).ToArray());
            syntax = syntax.AddParameterListParameters(Enumerable.Range(0, argCount).Select(s => SyntaxFactory.Parameter(SyntaxFactory.Identifier($"t{s}")).WithType(SyntaxFactory.IdentifierName($"T{s} "))).ToArray());
            builder.AppendLine(syntax.WithBody(null).WithSemicolonToken(default).ToString());
        }

        private const string PartialTemplate = @"
{4}
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
