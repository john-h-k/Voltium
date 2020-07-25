using System.Linq;
using Microsoft.CodeAnalysis;

namespace Voltium.Analyzers
{
    internal static class SymbolExtensions
    {
        public static bool HasAttribute(this ISymbol type, string attributeName, Compilation comp)
            => type.GetAttributes()
                    .Any(attr => SymbolEqualityComparer.Default.Equals(attr.AttributeClass, comp.GetTypeByMetadataName(attributeName)));


        public static bool TryGetAttribute(this ISymbol type, string attributeName, Compilation comp, out AttributeData attr)
        {
            attr = type.GetAttributes().Where(attr => SymbolEqualityComparer.Default.Equals(attr.AttributeClass, comp.GetTypeByMetadataName(attributeName))).FirstOrDefault();

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
    }
}
