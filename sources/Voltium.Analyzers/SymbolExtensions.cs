using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Voltium.Analyzers
{
    internal static class SymbolExtensions
    {
        public static bool HasAttribute(this ISymbol type, INamedTypeSymbol? attribute)
            => type.GetAttributes()
                    .Any(attr => SymbolEqualityComparer.Default.Equals(attr.AttributeClass, attribute));
        public static bool HasAttribute(this ISymbol type, string attributeName, Compilation comp)
            => type.GetAttributes()
                    .Any(attr => SymbolEqualityComparer.Default.Equals(attr.AttributeClass, comp.GetTypeByMetadataName(attributeName)));


        public static bool TryGetAttribute(this ISymbol type, string attributeName, Compilation comp, out AttributeData attr)
            => TryGetAttribute(type, comp.GetTypeByMetadataName(attributeName)!, out attr);

        public static bool TryGetAttribute(this ISymbol type, INamedTypeSymbol symbol, out AttributeData attr)
        {
            attr = type.GetAttributes().Where(attr => SymbolEqualityComparer.Default.Equals(attr.AttributeClass, symbol)).FirstOrDefault();

            return attr is not null;
        }

        public static bool IsImplementationOfInterfaceMethod(this IMethodSymbol method, ITypeSymbol? typeArgument = null)
        {
            foreach (var @interface in method.ContainingType.AllInterfaces)
            {
                foreach (var interfaceMethod in @interface.GetMembers().Where(member => member.Kind == SymbolKind.Method))
                {
                    if (method.IsImplementationOfInterfaceMethod(typeArgument, @interface, interfaceMethod.Name))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public static bool IsImplementationOfInterfaceMethod(this IMethodSymbol method, ITypeSymbol? typeArgument, INamedTypeSymbol? interfaceType, string interfaceMethodName)
        {
            INamedTypeSymbol? constructedInterface = typeArgument != null ? interfaceType?.Construct(typeArgument) : interfaceType;

            if (constructedInterface?.GetMembers(interfaceMethodName).FirstOrDefault() is IMethodSymbol interfaceMethod)
            {
                var definer = method.ContainingType.FindImplementationForInterfaceMember(interfaceMethod);
                return SymbolEqualityComparer.Default.Equals(method, definer);
            }
            return false;
        }

        public static string CreatePartialDecl(this INamedTypeSymbol symbol, string code, params INamedTypeSymbol?[]? newBases)
            => CreatePartialDecl(symbol, code, (IEnumerable<INamedTypeSymbol>?)newBases);
        public static string CreatePartialDecl(this INamedTypeSymbol symbol, string code, IEnumerable<INamedTypeSymbol>? newBases = null)
        {
            var builder = new StringBuilder();
            var syntax = (TypeDeclarationSyntax)symbol.DeclaringSyntaxReferences[0].GetSyntax();

            var usings = syntax.SyntaxTree.GetRoot().ChildNodes().OfType<UsingDirectiveSyntax>();

            foreach (var @using in usings)
            {
                builder.Append(@using.ToFullString());
            }

            var @namespace = symbol.ContainingNamespace;

            builder.Append("namespace " + @namespace.ToString() + "{\n");

            if (newBases is not null)
            {
                var bases = SyntaxFactory.SeparatedList<BaseTypeSyntax>(
                    newBases.Select(symbol => SyntaxFactory.SimpleBaseType(SyntaxFactory.ParseTypeName(symbol.Name)))
                );

                syntax = syntax.WithBaseList(syntax.BaseList is null ? SyntaxFactory.BaseList(bases) : syntax.BaseList.WithTypes(bases));
            }

            var types = new List<TypeDeclarationSyntax>();
            int nestedLevel = 0;
            do
            {
                nestedLevel++;
                types.Add(syntax);
                syntax = (syntax.Parent as TypeDeclarationSyntax)!;
            }
            while (syntax is not null);

            foreach (var type in Enumerable.Reverse(types))
            {
                var s = type.RemoveNodes(type.ChildNodes().OfType<AttributeListSyntax>(), SyntaxRemoveOptions.KeepDirectives)?.ToFullString() ?? string.Empty;
                builder.Append(s.Substring(0, s.IndexOf('{')));
                builder.Append("\n{\n");
            }

            builder.Append(code);

            for (int i = 0; i < nestedLevel; i++)
            {
                builder.Append("}\n");
            }


            builder.Append("}");

            return builder.ToString();
        }
        public static string FullyQualifiedName(this ITypeSymbol symbol)
            => $"global::{symbol.ContainingNamespace}.{symbol.Name}";
    }
}
