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

        protected override bool Predicate(GeneratorExecutionContext context, INamedTypeSymbol decl)
            => decl.HasAttribute(FixedBufferAttributeName, context.Compilation);

        protected override void GenerateFromSymbol(GeneratorExecutionContext context, INamedTypeSymbol symbol)
        {
            _ = symbol.TryGetAttribute(FixedBufferAttributeName, context.Compilation, out var attr);

            string type = attr.ConstructorArguments[0].ToCSharpString();
            var start = type.IndexOf('(') + 1;
            type = "global::" + type.Substring(start, type.IndexOf(')') - start);
            int count = (int)attr.ConstructorArguments[1].Value!;

            var builder = new StringBuilder();
            for (var i = 0; i < count; i++)
            {
                builder.Append(string.Format(ElementTemplate, type, i));
            }

            var source = string.Format(TypeTemplate, builder, type, count);

            if ((bool)attr.ConstructorArguments[2].Value!)
            {
                builder.Append(string.Format(ImplicitConversionTemplate, symbol.Name, type));
            }

            source = symbol.CreatePartialDecl(source);

            context.AddSource($"{symbol}.FixedBuffer.cs", SourceText.From(source, Encoding.UTF8));
        }

        private const string TypeTemplate = @"
#pragma warning disable CS0649, CS1591
        {0}

        public ref {1} this[nuint index]
            => ref this[(nint)index];

        public ref {1} this[nint index]
            => ref System.Runtime.CompilerServices.Unsafe.Add(ref GetPinnableReference(), index);

        public ref {1} GetPinnableReference()
            => ref System.Runtime.InteropServices.MemoryMarshal.GetReference(System.Runtime.InteropServices.MemoryMarshal.CreateSpan(ref E0, 0));

        public System.Span<{1}> AsSpan() => System.Runtime.InteropServices.MemoryMarshal.CreateSpan(ref E0, (int)BufferLength);
        public System.Span<{1}> AsSpan(int start) => AsSpan().Slice(start);
        public System.Span<{1}> AsSpan(int start, int length) => AsSpan().Slice(start, length);

        public System.Span<{1}> AsSpan(uint start) => AsSpan((int)start);
        public System.Span<{1}> AsSpan(uint start, uint length) => AsSpan((int)start, (int)length);

        public const uint BufferLength = {2};
#pragma warning restore CS0649, CS1591
";

        private const string ImplicitConversionTemplate = @"
#pragma warning disable CS0649, CS1591

        public static implicit operator {0}({1} value) => new() { [0] = value };
#pragma warning restore CS0649, CS1591
";
        private const string ElementTemplate = "\t\tpublic {0} E{1};\n";
    }
}
