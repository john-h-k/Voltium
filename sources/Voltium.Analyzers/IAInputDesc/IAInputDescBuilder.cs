using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Microsoft.CodeAnalysis;

namespace Voltium.Analyzers
{
    internal sealed class IAInputDescBuilder
    {
        private List<(string Name, string Type)> _elements = new();

        public unsafe void Add(string name, string format)
        {
            var elemDesc = (name, format);

            _elements.Add(elemDesc);
        }


        public string ToString(INamedTypeSymbol type)
        {
            var template = @"
using System.Collections.Immutable;
using Voltium.Core.Managers.Shaders;

namespace {0}
{{
    partial struct {1} : IBindableShaderType
    {{
        ImmutableArray<ShaderInput> IBindableShaderType.GetShaderInputs() => Elements;
        private static readonly ImmutableArray<ShaderInput> Elements = ImmutableArray.Create(new ShaderInput[] {{{2}}});
    }}
}}
";
            var str = string.Format(template, type.ContainingNamespace, type.Name, FormatElements());

            _elements.Clear();


            return str;
        }

        private string FormatElements()
        {
            var builder = new StringBuilder();

            bool notFirst = false;
            foreach (var desc in _elements)
            {
                if (notFirst)
                {
                    builder.Append(", \n");
                }
                notFirst = true;
                builder.Append($@"new ShaderInput(""{desc.Name}"", {desc.Type})");
            }

            return builder.ToString();
        }
    }
}
