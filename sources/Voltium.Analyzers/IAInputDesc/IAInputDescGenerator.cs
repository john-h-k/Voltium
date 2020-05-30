using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Unicode;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Voltium.Analyzers
{
    /// <summary>
    /// Generates input-assembler input descriptions for pipeline generation
    /// </summary>
    [Generator]
    internal class IAInputDescGenerator : PredicatedTypeGenerator
    {
        protected override void OnExecute(SourceGeneratorContext context)
            => InitBasicTypes(context.Compilation);

        private const string ShaderInputAttributeName = "Voltium.Core.Managers.Shaders.ShaderInputAttribute";

        protected override bool Predicate(SourceGeneratorContext context, INamedTypeSymbol decl)
            => decl.HasAttribute(ShaderInputAttributeName, context.Compilation);

        protected override void Generate(SourceGeneratorContext context, INamedTypeSymbol typeSymbol)
        {
            var builder = new IAInputDescBuilder();

            ResolveType(builder, (typeSymbol.Name, typeSymbol));

            context.AddSource($"{typeSymbol.Name}.IAInputLayout.cs", SourceText.From(builder.ToString(typeSymbol)!, Encoding.UTF8));
        }

        private void ResolveType(IAInputDescBuilder builder, (string Name, ITypeSymbol TypeSymbol) args)
        {
            var (name, typeSymbol) = args;
            if (BasicTypes.TryGetValue(typeSymbol, out var format))
            {
                builder.Add(name, format);
                return;
            }

            var layout = GetLayout(typeSymbol);
            foreach (var fieldOrProp in layout)
            {
                ResolveType(builder, fieldOrProp);
            }
        }

        private static IEnumerable<(string Name, ITypeSymbol Symbol)> GetLayout(ITypeSymbol type)
            => type.GetMembers()
                    .Where(member => !member.IsStatic && (member.Kind == SymbolKind.Field || member.Kind == SymbolKind.Property))
                    .Select(fieldOrProp => (fieldOrProp.Name, fieldOrProp is IFieldSymbol symbol ? symbol.Type : ((IPropertySymbol)fieldOrProp).Type));

        private static INamedTypeSymbol GetSymbolForType<T>(Compilation comp) => comp.GetTypeByMetadataName(typeof(T).FullName!) ?? throw new ArgumentException("Invalid type");

        private void InitBasicTypes(Compilation comp)
        {
            if (BasicTypes is object)
            {
                return;
            }

            BasicTypes = new Dictionary<ITypeSymbol, string>()
            {
                [GetSymbolForType<sbyte>(comp)] = "ShaderInputType.Int8",
                [GetSymbolForType<byte>(comp)] = "ShaderInputType.UInt8",
                [GetSymbolForType<short>(comp)] = "ShaderInputType.Int16",
                [GetSymbolForType<ushort>(comp)] = "ShaderInputType.UInt16",
                [GetSymbolForType<int>(comp)] = "ShaderInputType.Int32",
                [GetSymbolForType<uint>(comp)] = "ShaderInputType.UInt32",

                [GetSymbolForType<float>(comp)] = "ShaderInputType.Float",
                [GetSymbolForType<Vector2>(comp)] = "ShaderInputType.Float2",
                [GetSymbolForType<Vector3>(comp)] = "ShaderInputType.Float3",
                [GetSymbolForType<Vector4>(comp)] = "ShaderInputType.Float4",
                [GetSymbolForType<Matrix3x2>(comp)] = "ShaderInputType.Float3x2",
                [GetSymbolForType<Matrix4x4>(comp)] = "ShaderInputType.Float4x4",
            };
        }

        private Dictionary<ITypeSymbol, string> BasicTypes = null!;
    }
}
