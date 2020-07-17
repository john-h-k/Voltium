using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Voltium.Annotations;

namespace Voltium.Analyzers.ComType
{
    internal class ComTypeGenerator : PredicatedTypeGenerator
    {
        protected override void GenerateFromSyntax(SourceGeneratorContext context, TypeDeclarationSyntax syntax)
        {
            
        }

        protected override bool Predicate(SourceGeneratorContext context, INamedTypeSymbol decl)
            => decl.HasAttribute(typeof(NativeComTypeAttribute).FullName, context.Compilation);
    }
}
