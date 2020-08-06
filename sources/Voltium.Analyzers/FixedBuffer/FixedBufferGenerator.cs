using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Voltium.Analyzers.FixedBuffer
{
    [Generator]
    internal sealed class FixedBufferGenerator : PredicatedTypeGenerator
    {
        private const string FixedBufferAttributeName = "Voltium.Common.FixedBufferTypeAttribute";

        protected override bool Predicate(SourceGeneratorContext context, INamedTypeSymbol decl)
            => decl.HasAttribute(FixedBufferAttributeName, context.Compilation);

        protected override void GenerateFromSymbol(SourceGeneratorContext context, INamedTypeSymbol symbol)
        {
            _ = symbol.TryGetAttribute(FixedBufferAttributeName, context.Compilation, out var attr);

            string type = attr.ConstructorArguments[0].ToCSharpString();
            var start = type.IndexOf('(') + 1;
            type = type.Substring(start, type.IndexOf(')') - start);
            int count = (int)attr.ConstructorArguments[1].Value!;

            var builder = new StringBuilder();
            for (var i = 0; i < count; i++)
            {
                builder.Append(string.Format(ElementTemplate, type, i));
            }

            var source = string.Format(TypeTemplate, builder, type, count);
            source = symbol.CreatePartialDecl(source);



            context.AddSource($"{symbol.Name}.FixedBuffer.cs", SourceText.From(source, Encoding.UTF8));
        }

        private const string TypeTemplate = @"
#pragma warning disable CS0649
        {0}
#pragma warning restore CS0649

        public ref {1} this[uint index]
            => ref Unsafe.Add(ref GetPinnableReference(), (int)index);

        public ref {1} GetPinnableReference()
            => ref MemoryMarshal.GetReference(MemoryMarshal.CreateSpan(ref E0, 0));

        public const uint BufferLength = {2};
";

        private const string ElementTemplate = "\t\tpublic {0} E{1};\n";
    }
}
