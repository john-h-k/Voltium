using System.Diagnostics;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Voltium.Analyzers
{
    [Generator]
    internal class FluentGenerator : AttributedTypeGenerator
    {
        protected override string AttributeName => "Voltium.Common.FluentAttribute";
        private const string FluentNameAttributeName = "Voltium.Common.FluentNameAttribute";

        protected override void GenerateFromSymbol(GeneratorExecutionContext context, INamedTypeSymbol symbol)
        {
            var builder = new StringBuilder();
            var resolver = new TypeSizeResolver(context.Compilation);
            var explicitName = context.Compilation.GetTypeByMetadataName(FluentNameAttributeName)!;

            foreach (var member in symbol.GetMembers())
            {
                if (member.IsStatic
                    || member is not IPropertySymbol or IFieldSymbol
                    || member.DeclaredAccessibility is not Accessibility.Public or Accessibility.Internal
                    || (member is IPropertySymbol { IsIndexer: true }))
                {
                    continue;
                }

                var prop = member as IPropertySymbol;
                var field = member as IFieldSymbol;

                string name;
                if (member.TryGetAttribute(explicitName, out var attr))
                {
                    name = (string)attr.ConstructorArguments[0].Value!;
                }
                else
                {
                    name = prop?.Name ?? field!.Name;
                }

                GenerateFluentMethod(context.Compilation, builder, symbol, name, field?.Type ?? prop!.Type, resolver);
            }

            var str = symbol.CreatePartialDecl(builder.ToString());
            context.AddSource($"{symbol}.Fluent.cs", SourceText.From(str, Encoding.UTF8));
        }

        private const string Template = @"
        public {0} With{1}({2} @new)
        {{
            {1} = @new;
            return this;
        }}
";


        private const string FlagsTemplate = @"
        public {0} Add{1}({2} @new)
        {{
            {3} |= @new;
            return this;
        }}
";

        private void GenerateFluentMethod(Compilation comp, StringBuilder builder, INamedTypeSymbol self, string name, ITypeSymbol param, TypeSizeResolver resolver)
        {
            var passByReadOnlyRef = resolver.AdvantageousToPassByRef(param);

            builder.Append(string.Format(Template, self.FullyQualifiedName(), name, (passByReadOnlyRef ? "in " : "") + param.FullyQualifiedName()));

            if (param.HasAttribute("System.FlagsAttribute", comp))
            {
                builder.Append(string.Format(FlagsTemplate, self.FullyQualifiedName(), name.EndsWith("Flags") ? name : name + "Flags", param.FullyQualifiedName(), name));
            }
        }
    }
}
