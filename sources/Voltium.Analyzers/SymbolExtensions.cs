using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;

namespace Voltium.Analyzers
{
    internal static class SymbolExtensions
    {
        public static bool HasAttribute(this INamedTypeSymbol type, string attributeName, Compilation comp)
            => type.GetAttributes().Any(attr
                => SymbolEqualityComparer.Default.Equals(attr.AttributeClass, comp.GetTypeByMetadataName(attributeName)));
    }
}
