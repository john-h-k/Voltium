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

            if (syntax.Modifiers.IndexOf(SyntaxKind.PartialKeyword) == -1 && minArgs == 0)
            {
                minArgs = 1;
            }


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


            if (syntax.Body is null && syntax.ExpressionBody is null && syntax.Modifiers.IndexOf(SyntaxKind.PartialKeyword) == -1)
            {
                return; // no impl
            }

            int targetStatementIndex = 0;

            List<string> stmts;

            if (syntax.ExpressionBody is not null)
            {
                stmts = new List<string> { syntax.ExpressionBody.Expression.ToString() };
                targetStatementIndex = 1;
            }
            else if (syntax.Body is not null)
            {
                foreach (var statement in syntax.Body!.Statements)
                {
                    if (statement is ExpressionStatementSyntax expr
                        && expr.Expression is InvocationExpressionSyntax invoke
                        && SymbolEqualityComparer.Default.Equals(semantics.GetSymbolInfo(invoke).Symbol, targetSymbol))
                    {
                        break;
                    }
                    targetStatementIndex++;
                }

                stmts = syntax.Body.Statements.RemoveAt(targetStatementIndex).Select(s => s.ToString()).ToList();
            }
            else // partial method
            {
                stmts = new List<string> { "" };
                targetStatementIndex = 0;
            }

            if (template.Contains("%t..."))
            {
                var args = string.Join(", ", Enumerable.Range(0, argCount).Select(i => $"t{i}"));
                if (args != string.Empty)
                {
                    args = ", " + args;
                }

                var insertion = template.Replace("%t...", args);
                stmts.Insert(targetStatementIndex, insertion);
            }
            else
            {
                var insertions = Enumerable.Range(0, argCount).Select(i => template.Replace("%t", $"t{i}"));
                stmts.InsertRange(targetStatementIndex, insertions);
            }

            builder.Append("{" + string.Join(";\n", stmts) + ";\n" + "}");
        }

        private void WriteMethodDecl(StringBuilder builder, SourceGeneratorContext context, IMethodSymbol symbol, MethodDeclarationSyntax syntax, int argCount)
        {
            if (argCount > 0)
            {
                syntax = syntax.AddTypeParameterListParameters(Enumerable.Range(0, argCount).Select(s => SyntaxFactory.TypeParameter($"T{s}")).ToArray());
                syntax = syntax.AddParameterListParameters(Enumerable.Range(0, argCount).Select(s => SyntaxFactory.Parameter(SyntaxFactory.Identifier($"t{s}")).WithType(SyntaxFactory.IdentifierName($"T{s} "))).ToArray());


                int ind = syntax.Modifiers.IndexOf(SyntaxKind.PartialKeyword);

                if (ind != -1)
                {
                    syntax = syntax.WithModifiers(syntax.Modifiers.RemoveAt(ind));
                }
            }
            // syntax.AttributeLists.Where(attr => context.Compilation.GetSemanticModel(syntax.SyntaxTree).GetSymbolInfo(attr).Symbol.Name ==
            builder.AppendLine(syntax.WithAttributeLists(default).WithBody(null).WithExpressionBody(null).WithSemicolonToken(default).ToString());
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
