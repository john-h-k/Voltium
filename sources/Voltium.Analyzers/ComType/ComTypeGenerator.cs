using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace Voltium.Analyzers.ComType
{
    [Generator]
    internal class ComTypeGenerator : PredicatedTypeGenerator
    {
        protected override void GenerateFromSyntax(GeneratorExecutionContext context, TypeDeclarationSyntax syntax, SyntaxTree tree)
        {
            var semantics = context.Compilation.GetSemanticModel(tree);

            var typeSymbol = semantics.GetDeclaredSymbol(syntax)!;

            _ = typeSymbol.TryGetAttribute(NativeComTypeAttributeName, context.Compilation, out var typeAttr);
            var implName = typeAttr.ConstructorArguments[0].IsNull ? null : typeAttr.ConstructorArguments[0].Value?.ToString();

            var vtblType = ((IFieldSymbol)typeSymbol.GetMembers("Vtbl").FirstOrDefault()!)?.Type.ToString();

            var initBody = "";
            var wrappers = "";

            int index = -1;
            foreach (var methodDecl in syntax.Members.OfType<MethodDeclarationSyntax>())
            {
                if (semantics.GetDeclaredSymbol(methodDecl) is IMethodSymbol symbol && symbol.HasAttribute(NativeComMethodAttributeName, context.Compilation))
                {
                    var specIndex = GetComMethodVtableIndex(context.Compilation, symbol);
                    index = specIndex == -1 ? index + 1 : specIndex;

                    var remap = "_Generated_Com_" + symbol.Name;
                    initBody += GenerateVtableEntry(index, remap, symbol) + ";\n";
                    wrappers += GenerateManagedWrapper(null, remap, symbol) + "\n\n";
                }
            }

            var hasExplicitLayout = typeSymbol.TryGetAttribute("System.Runtime.InteropServices.StructLayoutAttribute", context.Compilation, out var attr)
                && attr.ConstructorArguments[0].Value is (int)LayoutKind.Explicit;

            var vtablePrefix = hasExplicitLayout ? "[FieldOffset(0)]" : "";
            var create = string.Format(CreateTemplate, vtblType ?? DefaultVtblType, index + 1, initBody, vtblType is null ? vtablePrefix + "public readonly nuint Vtbl;\n" : "");
            var asThis = string.Format(typeSymbol.IsUnmanagedType ? UnmanagedAsThisTemplate : ManagedAsThisTemplate, typeSymbol.Name);
            var extensions = implName is null ? "" : string.Format(ExtensionsTemplate, typeSymbol.Name, implName, typeSymbol.DeclaredAccessibility.ToString().ToLower());
            var source = string.Format(TypeTemplate, typeSymbol.ContainingNamespace, typeSymbol.Name, create + wrappers + asThis, vtblType is null && !hasExplicitLayout ? AutoLayout : "", extensions);

            context.AddSource($"{typeSymbol.Name}.ComImplementation.cs", SourceText.From(source, Encoding.UTF8));
        }

        private const string DefaultVtblType = "nuint";
        private const string AutoLayout = "[StructLayout(LayoutKind.Auto)]";

        private const string CreateTemplate = @"
        {3}

        private static {0} StaticVtbl = CreateVtbl();

        private static {0} CreateVtbl()
        {{
            void** vtbl = (void**)HeapAlloc(GetProcessHeap(), 0, (uint)sizeof(nuint) * {1});

            {2}

            return ({0})vtbl;
        }}";


        // sequential is not respected for managed types so we need to make a vtable offset
        private const string ManagedAsThisTemplate = @"
        private static int VtblOffset {{ get; }} = CalculateVtblOffset();

        private static int CalculateVtblOffset()
        {{
            Unsafe.SkipInit(out {0} val);

            return (int)((byte*)Unsafe.AsPointer(ref val) - (byte*)&val.Vtbl);
        }}
        private static ref {0} AsThis(void* pThis) => ref Unsafe.AsRef<{0}>((byte*)pThis + VtblOffset);";

        private const string UnmanagedAsThisTemplate = @"private static ref {0} AsThis(void* pThis) => ref Unsafe.AsRef<{0}>(pThis);";


        private const string TypeTemplate = @"
        using System;
        using System.Runtime.CompilerServices;
        using System.Runtime.InteropServices;
        using static TerraFX.Interop.Windows;

        namespace {0}
        {{
            {3}
            unsafe partial struct {1}
            {{
                public void Init()
                {{
                    Unsafe.AsRef(in Vtbl) = StaticVtbl;
                }}

                {2}
            }}

            {4}
        }}";

        private const string ExtensionsTemplate = @"
        {2} unsafe static partial class {0}Extensions
        {{
            public static ref {1} GetPinnableReference(this ref {0} @this) => ref Unsafe.As<nuint, {1}>(ref Unsafe.AsRef(in @this.Vtbl));
        }}
";


        private int GetComMethodVtableIndex(Compilation comp, IMethodSymbol symbol)
            => symbol.GetAttributes()
                .Where(attr => SymbolEqualityComparer.Default.Equals(attr.AttributeClass, comp.GetTypeByMetadataName(NativeComMethodAttributeName)))
                .First()
                .NamedArguments.Where(kvp => kvp.Key == "Index").FirstOrDefault().Value.Value as int? ?? -1;

        
        private string GenerateManagedWrapper(string? callingConv, string remapName, IMethodSymbol method)
        {
            // [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
            var @params = GetParams(method);
            return string.Format(
                @$"[UnmanagedCallersOnly{(callingConv is null ? "" : $"(CallConvs = new[] {{{{ typeof(CallConv{callingConv}) }}}})")}]" +
                @"private static unsafe {0} {1}(void* @this{2}) => AsThis(@this).{3}({4});",
                method.ReturnType.Name,
                remapName,
                string.IsNullOrWhiteSpace(@params) ? string.Empty : ", " + @params,
                method.Name,
                GetParamNames(method)
            );
        }

        private string GenerateVtableEntry(int index, string remapName, IMethodSymbol method)
        {
            return string.Format("vtbl[{0}] = ({1})&{2}", index, FuncPtrForMethod(null, method), remapName);
        }

        private string FuncPtrForMethod(string? callingConv, IMethodSymbol method)
        {
            var @params = GetStrippedParams(method);

            @params = string.IsNullOrWhiteSpace(@params) ? "void*" : "void*, " + @params;

            return string.Format("delegate* unmanaged{0}<{1}, {2}>", callingConv is null ? "" : $"[{callingConv}]", @params, method.ReturnType.Name);
        }

        private string GetParams(IMethodSymbol method) => string.Join(", ", method.Parameters.Select(p => p.Type.ToString() + " " + p.Name));
        private string GetStrippedParams(IMethodSymbol method) => string.Join(", ", method.Parameters.Select(p => p.Type.ToString()));
        private string GetParamNames(IMethodSymbol method) => string.Join(", ", method.Parameters.Select(p => p.Name));

        protected override bool Predicate(GeneratorExecutionContext context, INamedTypeSymbol decl)
        {
            return decl.HasAttribute(NativeComTypeAttributeName, context.Compilation);
        }

        private const string NativeComTypeAttributeName = "Voltium.Annotations.NativeComTypeAttribute";
        private const string NativeComMethodAttributeName = "Voltium.Annotations.NativeComMethodAttribute";
    }
}
