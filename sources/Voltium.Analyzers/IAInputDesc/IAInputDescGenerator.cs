using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Voltium.Analyzers
{
    /// <summary>
    /// Generates input-assembler input descriptions for pipeline generation
    /// </summary>
    [Generator]
    internal class IAInputDescGenerator : PredicatedTypeGenerator
    {
        protected override void OnExecute(GeneratorExecutionContext context)
            => InitBasicTypes(context.Compilation);

        private const string Namespace = "Voltium.Core.Devices.Shaders.";
        private const string ShaderInputAttributeName = Namespace + "ShaderInputAttribute";
        private const string InputLayoutAttributeName = Namespace + "InputLayoutAttribute";
        private const string ShaderIgnoreAttributeName = Namespace + "ShaderIgnoreAttribute";

        protected override bool Predicate(GeneratorExecutionContext context, INamedTypeSymbol decl)
        {
            return decl.HasAttribute(ShaderInputAttributeName, context.Compilation);
        }

        protected override void GenerateFromSymbol(GeneratorExecutionContext context, INamedTypeSymbol typeSymbol)
        {
            var builder = new IAInputDescBuilder();

            ResolveType(builder, (typeSymbol.Name, typeSymbol), context.Compilation);

            context.AddSource($"{typeSymbol}.InputAssemblerLayout.cs", SourceText.From(builder.ToString(typeSymbol)!, Encoding.UTF8));
        }

        private void ResolveType(IAInputDescBuilder builder, (string Name, ISymbol Symbol) args, Compilation comp)
        {
            var (name, symbol) = args;

            var fieldSymbol = symbol as IFieldSymbol;
            var typeSymbol = fieldSymbol is not null ? fieldSymbol.Type : (ITypeSymbol)symbol;

            if (typeSymbol.HasAttribute(ShaderIgnoreAttributeName, comp))
            {
                return;
            }

            var attr = fieldSymbol?.GetAttributes()
                .Where(attr => SymbolEqualityComparer.Default.Equals(attr.AttributeClass, comp.GetTypeByMetadataName(InputLayoutAttributeName)))
                .FirstOrDefault();

            if (BasicTypes.TryGetValue(typeSymbol, out var format))
            {
                builder.Add(name, format, attr);
                return;
            }

            var layout = GetLayout(typeSymbol);
            foreach (var field in layout)
            {
                ResolveType(builder, field, comp);
            }
        }

        private static IEnumerable<(string Name, IFieldSymbol Symbol)> GetLayout(ITypeSymbol type)
            => type.GetMembers()
                    .Where(member => !member.IsStatic && member.Kind == SymbolKind.Field)
                    .Select(field => (field.Name, (IFieldSymbol)field));

        private static INamedTypeSymbol GetSymbolForType<T>(Compilation comp) => comp.GetTypeByMetadataName(typeof(T).FullName!) ?? throw new ArgumentException("Invalid type");

        private void InitBasicTypes(Compilation comp)
        {
            if (BasicTypes is object)
            {
                return;
            }

            BasicTypes = new Dictionary<ITypeSymbol, string>()
            {
                [GetSymbolForType<float>(comp)] = "DataFormat.R32Single",
                [GetSymbolForType<Vector2>(comp)] = "DataFormat.R32G32Single",
                [GetSymbolForType<Vector3>(comp)] = "DataFormat.R32G32B32Single",
                [GetSymbolForType<Vector4>(comp)] = "DataFormat.R32G32B32A32Single",
            };
        }

        private Dictionary<ITypeSymbol, string> BasicTypes = null!;
    }
}
