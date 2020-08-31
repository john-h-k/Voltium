using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Voltium.Analyzers.PipelineStreamInitializer
{
    [Generator]
    internal sealed class PipelineStreamInitializerGenerator : PredicatedTypeGenerator
    {
        private const string IPipelineStreamElement = "Voltium.Core.Pipeline.IPipelineStreamElement`1";
        private const string IPipelineStreamType = "Voltium.Core.Pipeline.IPipelineStreamType";

        protected override void GenerateFromSymbol(SourceGeneratorContext context, INamedTypeSymbol decl)
        {
            var builder = new StringBuilder();
            foreach (var field in decl.GetMembers().OfType<IFieldSymbol>())
            {
                if (field.Type is not INamedTypeSymbol named || !IsElement(context, named))
                {
                    continue;
                }

                builder.AppendLine(field.Name + "._Initialize();");
            }

            var source = decl.CreatePartialDecl(string.Format(InitializeTemplate, builder.ToString()));

            context.AddSource($"{decl.Name}.PipelineStreamInitializer.cs", SourceText.From(source, Encoding.UTF8));
        }

        private string InitializeTemplate = @"
         /// <summary>
         /// Don't manually invoke this method. It is used to correctly initialize pipeline elements for native code to read them
         /// </summary>
         [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
         public void _Initialize()
         {{
             {0}
         }}
 ";


        private bool IsElement(SourceGeneratorContext context, INamedTypeSymbol decl)
        {
            var elementType = context.Compilation.GetTypeByMetadataName(IPipelineStreamElement)!.ConstructUnboundGenericType();
            return decl.AllInterfaces.Select(@interface => @interface.IsGenericType ? @interface.ConstructUnboundGenericType() : @interface).Contains(elementType, SymbolEqualityComparer.Default);
        }

        protected override bool Predicate(SourceGeneratorContext context, INamedTypeSymbol decl)
            => decl.AllInterfaces.Contains(context.Compilation.GetTypeByMetadataName(IPipelineStreamType), SymbolEqualityComparer.Default);
    }
}
