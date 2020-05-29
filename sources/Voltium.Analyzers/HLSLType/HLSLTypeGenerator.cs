using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;

namespace Voltium.Analyzers.HLSLType
{
    [Generator]
    internal sealed class HLSLTypeGenerator : PredicatedTypeGenerator
    {
        protected override void Generate(SourceGeneratorContext context, INamedTypeSymbol symbol)
        {

        }

        private const string ShaderTypeAttribute = "Voltium.Core.Managers.Shaders.ShaderTypeAttribute";

        protected override bool Predicate(SourceGeneratorContext context, INamedTypeSymbol decl)
            => decl.HasAttribute(ShaderTypeAttribute, context.Compilation);
    }
}
