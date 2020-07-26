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
        protected override void GenerateFromSyntax(SourceGeneratorContext context, TypeDeclarationSyntax syntax, SyntaxTree tree)
        {
            //Debugger.Launch();
            var semantics = context.Compilation.GetSemanticModel(tree);

            var typeSymbol = semantics.GetDeclaredSymbol(syntax)!;


            var vtblType = ((IFieldSymbol)typeSymbol.GetMembers("Vtbl").First()).Type.ToString();

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
                    wrappers += GenerateManagedWrapper("stdcall", remap, symbol) + "\n\n";
                }
            }

            var hasExplicitLayout = typeSymbol.TryGetAttribute("System.Runtime.InteropServices.StructLayoutAttribute", context.Compilation, out var attr)
                && attr.ConstructorArguments[0].Value is (int)LayoutKind.Explicit;

            var create = string.Format(CreateTemplate, vtblType, index + 1, initBody);
            var asThis = string.Format(typeSymbol.IsUnmanagedType ? UnmanagedAsThisTemplate : ManagedAsThisTemplate, typeSymbol.Name);
            var source = string.Format(TypeTemplate, typeSymbol.ContainingNamespace, typeSymbol.Name, hasExplicitLayout ? "[FieldOffset(0)]" : "", create + wrappers + asThis);

            context.AddSource($"{typeSymbol.Name}.ComImplementation.cs", SourceText.From(source, Encoding.UTF8));
        }

        private const string CreateTemplate = @"

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
            unsafe partial struct {1}
            {{
                //{2}
                //private void** Vtbl;


                public void Init()
                {{
                    Vtbl = StaticVtbl;
                }}

                {3}
            }}

            unsafe partial struct {1}Extensions
            {{
            }}
        }}";

        private int GetComMethodVtableIndex(Compilation comp, IMethodSymbol symbol)
            => symbol.GetAttributes()
                .Where(attr => SymbolEqualityComparer.Default.Equals(attr.AttributeClass, comp.GetTypeByMetadataName(NativeComMethodAttributeName)))
                .First()
                .NamedArguments.Where(kvp => kvp.Key == "Index").FirstOrDefault().Value.Value as int? ?? -1;

        private string GenerateManagedWrapper(string callingConv, string remapName, IMethodSymbol method)
        {
            var @params = GetParams(method);
            return string.Format(
                @"[UnmanagedCallersOnly]" +
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
            return string.Format("vtbl[{0}] = ({1})&{2}", index, FuncPtrForMethod("stdcall", method), remapName);
        }

        private string FuncPtrForMethod(string callingConv, IMethodSymbol method)
        {
            var @params = GetStrippedParams(method);

            @params = string.IsNullOrWhiteSpace(@params) ? "void*" : "void*, " + @params;

            return string.Format("delegate* {0}<{1}, {2}>", /* not yet supported */ /*callingConv*/ "", @params, method.ReturnType.Name);
        }

        private string GetParams(IMethodSymbol method) => string.Join(", ", method.Parameters.Select(p => p.Type.ToString() + " " + p.Name));
        private string GetStrippedParams(IMethodSymbol method) => string.Join(", ", method.Parameters.Select(p => p.Type.ToString()));
        private string GetParamNames(IMethodSymbol method) => string.Join(", ", method.Parameters.Select(p => p.Name));

        protected override bool Predicate(SourceGeneratorContext context, INamedTypeSymbol decl)
        {
            return decl.HasAttribute(NativeComTypeAttributeName, context.Compilation);
        }

        private const string NativeComTypeAttributeName = "Voltium.Annotations.NativeComTypeAttribute";
        private const string NativeComMethodAttributeName = "Voltium.Annotations.NativeComMethodAttribute";
    }
}
