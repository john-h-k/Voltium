using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Voltium.Core;
using Voltium.Core.Devices.Shaders;

namespace Voltium.Analyzers
{
    internal sealed class IAInputDescBuilder
    {
        private List<(string Name, DataFormat Type, AttributeData? Attr)> _elements = new();

        public unsafe void Add(string name, DataFormat format, AttributeData? attr)
        {
            _elements.Add((name, format, attr));
        }


        public string ToString(INamedTypeSymbol type)
        {
            var template = @$"
using System;
using Voltium.Core;
using Voltium.Core.Devices.Shaders;

namespace {{0}}
{{{{
    partial struct {{1}} : {nameof(IBindableShaderType)}
    {{{{
        {nameof(ShaderInput)}[] {nameof(IBindableShaderType)}.{nameof(IBindableShaderType.GetShaderInputs)}() => Elements;
        private static readonly {nameof(ShaderInput)}[] Elements = new {nameof(ShaderInput)}[] {{{{ {{2}} }}}};
    }}}}
}}}}
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
                builder.Append($"new {nameof(ShaderInput)}(");

                string? name = null;
                DataFormat? type = null;

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
                                    name = "\"" + (string)layoutMember.Value.Value! + "\"";
                                }
                                break;

                            case "Type":
                                if (layoutMember.Value.Value is null)
                                {
                                    type = desc.Type;
                                }
                                else
                                {
                                    type = (DataFormat)layoutMember.Value.Value!;
                                }
                                break;

                            default:
                                var keyName = layoutMember.Key;
                                builder.Append($"{char.ToLower(keyName[0]) + keyName.Substring(1) }: {layoutMember.Value.ToCSharpString()}");
                                break;
                        }
                    }
                    builder.Append($"name: {name?.ToUpper() ?? desc.Name}, type: {nameof(DataFormat)}.{type ?? desc.Type}");
                }
                else
                {
                    builder.Append($@"name: ""{desc.Name.ToUpper()}"", type: {nameof(DataFormat)}.{desc.Type}");
                }

                builder.Append("), ");

                // ctor is
                // - public ShaderInput(string name, DataFormat type, uint offset = 0xFFFFFFFF, uint nameIndex = 0, uint channel = 0, InputClass inputClass = InputClass.PerVertex)
            }

            return builder.ToString();
        }
    }
}
