using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
namespace Voltium.Analyzers
{
    internal sealed class IAInputDescBuilder
    {
        private List<(string Name, string Type, AttributeData? Attr)> _elements = new();

        public unsafe void Add(string name, string format, AttributeData? attr)
        {
            _elements.Add((name, format, attr));
        }


        public string ToString(INamedTypeSymbol type)
        {
            var template = @"
using System;
using System.Collections.Immutable;
using Voltium.Core;
using Voltium.Core.Devices.Shaders;

namespace {0}
{{
    partial struct {1} : IBindableShaderType
    {{
        ReadOnlyMemory<ShaderInput> IBindableShaderType.GetShaderInputs() => ShaderInputs.AsMemory();

        /// <summary>
        /// The <see cref=""ShaderInput""/> for this type. This field is generated
        /// </summary>
        public static readonly ImmutableArray<ShaderInput> ShaderInputs = ImmutableArray.Create({2});
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

            foreach (var desc in _elements)
            {
                builder.Append($"new ShaderInput(");

                string? name = null;
                string? type = null;

                if (desc.Attr is not null)
                {
                    for (int i = 0; i < desc.Attr.NamedArguments.Length; i++)
                    {
                        var layoutMember = desc.Attr.NamedArguments[i];
                        switch (layoutMember.Key)
                        {
                            case "Name":
                                if (layoutMember.Value.Value is null)
                                {
                                    name = "\"" + desc.Name + "\"";
                                }
                                else
                                {
                                    name = layoutMember.Value.ToCSharpString();
                                }
                                break;

                            case "Type":
                                if (layoutMember.Value.Value is null)
                                {
                                    type = desc.Type;
                                }
                                else
                                {
                                    type = layoutMember.Value.ToCSharpString();
                                }
                                break;

                            default:
                                var keyName = layoutMember.Key;
                                builder.Append($"{char.ToLower(keyName[0]) + keyName.Substring(1) }: {layoutMember.Value.ToCSharpString()}");
                                break;
                        }
                    }
                    builder.Append($"name: {name?.ToUpper() ?? desc.Name}, type: {type ?? desc.Type}");
                }
                else
                {
                    builder.Append($@"name: ""{desc.Name.ToUpper()}"", type: {desc.Type}");
                }

                builder.Append("), ");

                // ctor is
                // - public ShaderInput(string name, DataFormat type, uint offset = 0xFFFFFFFF, uint nameIndex = 0, uint channel = 0, InputClass inputClass = InputClass.PerVertex)
            }

            builder.Length -= 2; // trim off ending comma

            return builder.ToString();
        }
    }
}
