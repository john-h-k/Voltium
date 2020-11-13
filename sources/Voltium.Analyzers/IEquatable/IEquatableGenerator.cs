using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Voltium.Analyzers.IEquatable
{
    [Generator]
    internal sealed class IEquatableGenerator : PredicatedTypeGenerator
    {
        // BUG: does not work with nested types
        protected override void GenerateFromSymbol(GeneratorExecutionContext context, INamedTypeSymbol symbol)
        {
            var equalsCandidates = symbol.GetMembers(nameof(Equals))
                                        .Where(member => member is IMethodSymbol method
                                                        && SymbolEqualityComparer.Default.Equals(method.ReturnType, context.Compilation.GetSpecialType(SpecialType.System_Boolean))
                                                        && method.Parameters.Length == 1
                                                        && SymbolEqualityComparer.Default.Equals(method.Parameters[0].Type, symbol));

            var ghcCandidates = symbol.GetMembers(nameof(GetHashCode))
                                        .Where(member => member is IMethodSymbol method
                                                        && SymbolEqualityComparer.Default.Equals(method.ReturnType, context.Compilation.GetSpecialType(SpecialType.System_Int32))
                                                        && method.Parameters.Length == 0);

            var builder = new StringBuilder();


            if (ghcCandidates.Count() == 0)
            {
                var ghcString = GenerateHashCodeOfAllFields(context, symbol);
                builder.AppendLine(string.Format(GetHashCodeTemplate, ghcString));
            }


            string passByRefModifier = string.Empty;
            if (symbol.IsValueType)
            {
                var shouldPassByRef = new TypeSizeResolver(context.Compilation).AdvantageousToPassByRef(symbol);

                passByRefModifier = shouldPassByRef ? "in " : "";
                builder.AppendLine(string.Format(ValueTypeEqualsTemplate, passByRefModifier, symbol.Name));
                if (equalsCandidates.Count() == 0)
                {
                    var compString = GenerateComparisonOfAllFields(context, symbol);
                    builder.AppendLine(string.Format(ValueTypeStandardEqualsTemplate, symbol.Name, compString));
                }
            }
            else
            {
                builder.AppendLine(string.Format(RefTypeEqualsTemplate, symbol.Name));
                if (equalsCandidates.Count() == 0)
                {
                    var compString = GenerateComparisonOfAllFields(context, symbol);
                    builder.AppendLine(string.Format(RefTypeStandardEqualsTemplate, symbol.Name, compString));
                }
            }

            builder.AppendLine(string.Format(EquatableTemplate, passByRefModifier, symbol.Name));

            var source = string.Format(TypeTemplate, symbol.ContainingNamespace, symbol.IsValueType ? "struct" : "class", symbol.Name, builder.ToString());

            context.AddSource($"{symbol.Name}.EqualityMembers.cs", SourceText.From(source, Encoding.UTF8));
        }

        private object GenerateHashCodeOfAllFields(GeneratorExecutionContext context, INamedTypeSymbol symbol)
        {
            var comparisons = new List<string>();

            foreach (var field in symbol.GetMembers().OfType<IFieldSymbol>().Where(field => !field.IsStatic && field.CanBeReferencedByName))
            {
                comparisons.Add(field.Name);
            }

            return "HashCode.Combine(" + string.Join(", ", comparisons) + ")";
        }

        private string GenerateComparisonOfAllFields(GeneratorExecutionContext context, INamedTypeSymbol symbol)
        {
            var comparisons = new List<string>();

            foreach (var field in symbol.GetMembers().Where(member => member.Kind == SymbolKind.Field && !member.IsStatic).Select(field => (IFieldSymbol)field))
            {
                if (field.Type.IsValueType)
                {
                    comparisons.Add($"{field.Name}.Equals(other.{field.Name})");
                }
                else
                {
                    comparisons.Add($"object.Equals({field.Name}, other.{field.Name})");
                }
            }

            return string.Join(" && ", comparisons);
        }

        private const string GenerateEqualityAttributeName = "Voltium.Common.GenerateEqualityAttribute";

        protected override bool Predicate(GeneratorExecutionContext context, INamedTypeSymbol decl)
            => decl.HasAttribute(GenerateEqualityAttributeName, context.Compilation);

        private const string TypeTemplate = @"
        using System;

        namespace {0}
        {{
            partial {1} {2} : IEquatable<{2}>
            {{
                {3}
            }}
        }}";

        private const string ValueTypeStandardEqualsTemplate = @"
        /// <inheritdoc cref=""IEquatable{{T}}""/>
        public bool Equals({0} other) => {1};";


        private const string RefTypeStandardEqualsTemplate = @"
        /// <inheritdoc cref=""IEquatable{{T}}""/>
        public bool Equals({0}? other) => other is object && {1};";

        private const string ValueTypeEqualsTemplate = @"
        /// <summary>
        /// Compares if two <see cref=""{1}""/> objects are equal
        /// </summary>
        /// <param name=""left"" > One of the <see cref=""{1}"" />s to compare</param>
        /// <param name=""right"">One of the <see cref=""{1}"" />s to compare</param>
        /// <returns><see langword=""true"" /> if both sides are equal, else <see langword=""false"" /></returns>
        public static bool Equals({0}{1} left, {0}{1} right) => left.Equals(right);";

        private const string RefTypeEqualsTemplate = @"
        public static bool Equals({0}? left, {0}? right)
        => ReferenceEquals(left, right) || (left is object && left.Equals(right));";

        private const string GetHashCodeTemplate = @"/// <inheritdoc />
        public override int GetHashCode() => {0};";

        private const string EquatableTemplate = @"/// <inheritdoc />
        public override bool Equals(object? obj) => obj is {1} other && Equals(other);

        /// <inheritdoc cref=""Equals({1})""/>
        public static bool operator ==({0}{1} left, {0}{1} right) => Equals(left, right);

        /// <summary>
        /// Compares if two <see cref=""{1}""/> objects are not equal
        /// </summary>
        /// <param name=""left"">One of the <see cref=""{1}""/>s to compare</param>
        /// <param name=""right"">One of the <see cref=""{1}""/>s to compare</param>
        /// <returns><see langword=""true""/> if both sides are not equal, else <see langword=""false"" /></returns>
        public static bool operator !=({0}{1} left, {0}{1} right) => !(left == right);";
    }
}
