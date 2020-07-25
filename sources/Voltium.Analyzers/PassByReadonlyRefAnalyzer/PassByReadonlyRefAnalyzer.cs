using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Analyzer.Utilities.Extensions;

namespace Voltium.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal sealed class PassByReadonlyRefAnalyzer : DiagnosticAnalyzer
    {
        internal const string Category = "Performance";
        internal const string RuleId = "PE0001";
        internal const string Title = "PassByReadonlyRef";
        internal const string Message = "Types which are estimated to have a runtime size of greater than 16 byte should be passed by pointer or readonly ref (in)";



        public static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
#pragma warning disable RS2008 // Enable analyzer release tracking
           RuleId,
#pragma warning restore RS2008 // Enable analyzer release tracking
           Title,
           Message,
           Category,
           DiagnosticSeverity.Warning,
           true
        );

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            //Debugger.Launch();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
            context.EnableConcurrentExecution();

            context.RegisterCompilationStartAction(comp => InitializeCompilation(comp));
        }

        private void InitializeCompilation(CompilationStartAnalysisContext context)
        {
            var compResolver = new TypeSizeResolver(context.Compilation);

            context.RegisterSyntaxNodeAction(
                invoc => AnalyzeInvocation(invoc, compResolver),
                SyntaxKind.LocalFunctionStatement,
                SyntaxKind.MethodDeclaration,
                SyntaxKind.ConstructorDeclaration,
                SyntaxKind.DelegateDeclaration
            );
        }

        private void AnalyzeInvocation(SyntaxNodeAnalysisContext context, TypeSizeResolver resolver)
        {
            var semantics = context.SemanticModel;

            var @params =
                (context.Node as MethodDeclarationSyntax)?.ParameterList.Parameters
                ?? (context.Node as ConstructorDeclarationSyntax)?.ParameterList.Parameters
                ?? (context.Node as DelegateDeclarationSyntax)?.ParameterList.Parameters
                ?? (context.Node as LocalFunctionStatementSyntax)?.ParameterList.Parameters;

            foreach (var param in @params!)
            {
                var symbol = semantics.GetDeclaredSymbol(param);

                if (symbol is null)
                {
                    throw new Exception("null param symbol. not sure what to do here to be honest. :(");
                }

                if (CanModifyMethod(semantics.GetDeclaredSymbol(context.Node)!) && resolver.AdvantageousToPassByRef(symbol))
                {
                    context.ReportDiagnostic(Diagnostic.Create(Rule, param.GetLocation()));
                }
            }

            static bool CanModifyMethod(ISymbol invocation) => invocation is IMethodSymbol method && method.Parameters.All(p => !p.HasExplicitDefaultValue) && !method.IsImplementationOfInterfaceMethod() && !method.IsOverride;
        }
    }

    internal class TypeSizeResolver
    {
        private Dictionary<ITypeSymbol, TypeSizeInfo> _typeInfo;
        private ITypeSymbol _structLayoutAttribute;

        public TypeSizeResolver(Compilation comp)
        {
            _typeInfo = CreateTypeInfoForCompilation(comp);
            _structLayoutAttribute = GetStructLayoutSymbolForCompilation(comp);
        }

        private static Dictionary<ITypeSymbol, TypeSizeInfo> CreateTypeInfoForCompilation(Compilation comp)
        {
            return new Dictionary<ITypeSymbol, TypeSizeInfo>()
            {
                [comp.GetSpecialType(SpecialType.System_Boolean)] = new TypeSizeInfo(size: 1),
                [comp.GetSpecialType(SpecialType.System_Char)] = new TypeSizeInfo(size: 2),

                [comp.GetSpecialType(SpecialType.System_SByte)] = new TypeSizeInfo(size: 1),
                [comp.GetSpecialType(SpecialType.System_Byte)] = new TypeSizeInfo(size: 1),
                [comp.GetSpecialType(SpecialType.System_Int16)] = new TypeSizeInfo(size: 2),
                [comp.GetSpecialType(SpecialType.System_UInt16)] = new TypeSizeInfo(size: 2),
                [comp.GetSpecialType(SpecialType.System_Int32)] = new TypeSizeInfo(size: 4),
                [comp.GetSpecialType(SpecialType.System_UInt32)] = new TypeSizeInfo(size: 4),
                [comp.GetSpecialType(SpecialType.System_Int64)] = new TypeSizeInfo(size: 8),
                [comp.GetSpecialType(SpecialType.System_UInt64)] = new TypeSizeInfo(size: 8),

                [comp.GetSpecialType(SpecialType.System_Single)] = new TypeSizeInfo(size: 4),
                [comp.GetSpecialType(SpecialType.System_Double)] = new TypeSizeInfo(size: 8),

                [comp.GetSpecialType(SpecialType.System_IntPtr)] = TypeSizeResolver.ReferenceTypeInfo,
                [comp.GetSpecialType(SpecialType.System_UIntPtr)] = TypeSizeResolver.ReferenceTypeInfo,

                [comp.GetSpecialType(SpecialType.System_Decimal)] = new TypeSizeInfo(size: 16, alignment: 4),
            };
        }

        private static ITypeSymbol GetStructLayoutSymbolForCompilation(Compilation comp)
            => comp.GetTypeByMetadataName(typeof(StructLayoutAttribute).FullName)!;


        private const int PassByReadonlyRefThreshold = 16;

        public bool AdvantageousToPassByRef(IParameterSymbol symbol)
            => symbol.Type.TypeKind is TypeKind.Struct && symbol.RefKind == RefKind.None && AdvantageousToPassByRef(symbol.Type);

        public bool AdvantageousToPassByRef(ITypeSymbol symbol)
        {
            var sizeInfo = EstimateTypeInfo(symbol);
            return sizeInfo.AlignedSize > PassByReadonlyRefThreshold;
        }

        private static bool Assume64Bit => true;
        public static TypeSizeInfo ReferenceTypeInfo => Assume64Bit ? new TypeSizeInfo(size: 8) : new TypeSizeInfo(size: 4);

        public TypeSizeInfo EstimateTypeInfo(ITypeSymbol type)
        {
            if (_typeInfo.TryGetValue(type, out var info))
            {
                return info;
            }

            if (type.IsReferenceType || type.TypeKind is TypeKind.Pointer)
            {
                return ReferenceTypeInfo;
            }

            int size = 0, explicitSize = 0;
            int alignment = 0, explicitAlignment = 0;

            var layout = type.GetAttributes().Where(attr => SymbolEqualityComparer.Default.Equals(attr.AttributeClass, _structLayoutAttribute)).FirstOrDefault();
            if (layout is not null)
            {
                explicitSize = layout.NamedArguments.Where(kvp => kvp.Key == "Size").First().Value.Value as int? ?? 0;
                explicitAlignment = layout.NamedArguments.Where(kvp => kvp.Key == "Pack").First().Value.Value as int? ?? 0;
            }


            ImmutableArray<IFieldSymbol> fields;
            if (type.IsUnmanagedType)
            {
                fields = ImmutableArray.CreateRange(type.GetMembers().Where(symbol => !symbol.IsStatic).OfType<IFieldSymbol>());
            }
            else
            {
                fields = OrderFields(type);
            }

            foreach (var field in fields)
            {
                info = EstimateTypeInfo(field.Type);
                size += info.AlignedSize;

                // with managed types or unspecified alignment types, alignment is size of largest field
                if (!type.IsUnmanagedType || explicitAlignment == 0)
                {
                    alignment = Math.Max(alignment, info.Alignment);
                }
            }

            info = new TypeSizeInfo(Math.Max(size, explicitSize), alignment);
            _typeInfo[type] = info;
            return info;
        }

        private ImmutableArray<IFieldSymbol> OrderFields(ITypeSymbol type)
        {
            var fields = type.GetMembers().Where(symbol => !symbol.IsStatic).OfType<IFieldSymbol>().ToArray();
            Array.Sort(fields, (lhs, rhs) => EstimateTypeInfo(lhs.Type).AlignedSize.CompareTo(EstimateTypeInfo(rhs.Type).AlignedSize));

            return ImmutableArray.Create(fields);
        }


    }


    internal readonly struct TypeSizeInfo
    {
        public readonly int Size;
        public readonly int Alignment;

        public int AlignedSize => Math.Max(Size, Alignment);

        public const int AlignmentIsSize = -1;

        public TypeSizeInfo(int size, int alignment = AlignmentIsSize)
        {
            Size = size;
            Alignment = alignment == AlignmentIsSize ? size : alignment;
        }

        public override string ToString()
            => $"Size: {Size}, Alignment: {Alignment}, AlignedSize: {AlignedSize}";
    }
}
