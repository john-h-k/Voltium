using System.Linq;
using Microsoft.CodeAnalysis;

namespace Voltium.Analyzers
{
    internal static class SymbolExtensions
    {
        public static bool HasAttribute(this ISymbol type, string attributeName, Compilation comp)
            => type.GetAttributes().Any(attr
                => SymbolEqualityComparer.Default.Equals(attr.AttributeClass, comp.GetTypeByMetadataName(attributeName)));
    }
}
