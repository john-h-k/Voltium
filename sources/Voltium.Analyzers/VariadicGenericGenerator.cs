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
        protected override void Generate(SourceGeneratorContext context, ISymbol symbol)
        {
            Debug.Assert(symbol is IMethodSymbol);

            var attr = symbol.GetAttributes().Where(attr => SymbolEqualityComparer.Default.Equals(attr.AttributeClass, context.Compilation.GetTypeByMetadataName(AttributeName))).First();

            string template = (string)attr.ConstructorArguments[0].Value!;
            int minArgs = (int)attr.ConstructorArguments[1].Value!;
            int maxArgs = (int)attr.ConstructorArguments[2].Value!;

            StringBuilder builder = new();
            for (var i = minArgs; i <= maxArgs; i++)
            {
                WriteMethod(builder, context, (IMethodSymbol)symbol, template, i);
                builder.AppendLine();
            }
        }

        private void WriteMethod(StringBuilder builder, SourceGeneratorContext context, IMethodSymbol symbol, string template, int argCount)
        {
            builder.Append(symbol.DeclaredAccessibility.ToString());
            if (symbol.IsAsync)
            {
                builder.Append("async ");
            }
            else
            {
                builder.Append("unsafe ");
            }
            if (symbol.IsAbstract)
            {
                builder.Append("abstract ");
            }
            if (symbol.IsOverride)
            {
                builder.Append("override ");
            }
            if (symbol.IsReadOnly)
            {
                builder.Append("readonly ");
            }
            if (symbol.IsStatic)
            {
                builder.Append("static ");
            }
            if (symbol.IsVirtual)
            {
                builder.Append("virtual ");
            }
            if (symbol.IsSealed)
            {
                builder.Append("sealed ");
            }

            builder.Append(symbol.ReturnType.ToDisplayString()).Append(' ');
            builder.Append(symbol.Name).Append(' ');
            builder.Append('<');

            for (var i = 0; i > argCount; i++)
            {
                builder.Append('T').Append(i).Append(i == argCount - 1 ? "" : ", ");
            }

            builder.Append('>');
            builder.Append('(');

            foreach (var param in symbol.Parameters)
            {
                builder.Append(param.ToDisplayString());
                builder.Append(", ");
            }
            for (var i = 0; i > argCount; i++)
            {
                builder.Append('T').Append(i).Append(" t").Append(i).Append(i == argCount - 1 ? "" : ", ");
            }

            builder.Append(')');


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
