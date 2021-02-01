using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FlowAnalysis;
using Microsoft.CodeAnalysis.Operations;
using Silk.NET.Direct3D.Compilers;
using Voltium.Analyzers;

namespace Voltium.ShaderCompiler
{
    internal readonly struct HlslSemantic
    {
        public readonly string SemanticName;
        public readonly ITypeSymbol Type;
        public readonly string ParamName;

        public HlslSemantic(string name, ITypeSymbol type, string paramName)
        {
            SemanticName = name;
            Type = type;
            ParamName = paramName;
        }
    }

    [Generator]
    internal sealed class ShaderCompiler : PredicatedGenerator<MethodDeclarationSyntax>
    {
        private static readonly string[] ShaderTypes =
        {
            "Voltium.Core.ShaderLang.Shader+ComputeShaderAttribute",
        };

        protected override void Generate(GeneratorExecutionContext context, ISymbol symbol, MethodDeclarationSyntax syntax)
        {
            var compiler = new MethodCompiler(context, syntax.SyntaxTree);
            var s = compiler.CompileComputeShader((IMethodSymbol)symbol);
        }

        protected override bool Predicate(GeneratorExecutionContext context, ISymbol decl)
        {
            return ShaderTypes.Any(type => decl.HasAttribute(type, context.Compilation));
        }
    }

    internal static class ArrayExtensions
    {
        public static void Deconstruct<T>(this T[] arr, out T t0, out T t1, out T t2)
        {
            int i = 0;
            t0 = arr[i++];
            t1 = arr[i++];
            t2 = arr[i++];
        }
    }

    internal sealed class ShaderStrings
    {
        public const string ComputeShaderAttribute = "Voltium.Core.ShaderLang.Shader+ComputeShaderAttribute";
        public const string IntrinsicAttribute = "Voltium.Core.ShaderLang.Shader+IntrinsicAttribute";
        public const string SemanticAttribute = "Voltium.Core.ShaderLang.Shader+SemanticAttribute";
    }

    internal sealed class MethodCompiler
    {
        private const string ComputeShaderAttribute = "Voltium.Core.ShaderLang.Shader+ComputeShaderAttribute";
        private const string IntrinsicAttribute = "Voltium.Core.ShaderLang.Shader+IntrinsicAttribute";
        private const string SemanticAttribute = "Voltium.Core.ShaderLang.Shader+SemanticAttribute";

        private GeneratorExecutionContext _context;
        private SemanticModel _model;
        public string CompileComputeShader(IMethodSymbol method)
        {
            var computeShaderAttributeType = _context.Compilation.GetTypeByMetadataName(ComputeShaderAttribute);
            Debug.Assert(computeShaderAttributeType is not null);
            //Debugger.Launch();

            Debug.Assert(method.HasAttribute(computeShaderAttributeType));

            var body = method.DeclaringSyntaxReferences.Select(syntaxRef => syntaxRef.GetSyntax()).OfType<MethodDeclarationSyntax>().Select(decl => decl.Body).OfType<BlockSyntax>().SingleOrDefault();

            if (body is null)
            {
                return string.Empty;
            }

            var semantics = ParseParameterList(method);

            var attr = method.GetAttribute(computeShaderAttributeType!);
            var (x, y, z) = attr.ConstructorArguments.Select(x => (int)x.Value!).ToArray();

            var builder = new StringBuilder();

            builder.AppendLine($"[numthreads({x}, {y}, {z})]");
            builder.AppendLine($"void {method.Name}");
            builder.Append('(');

            for (int i = 0; i < semantics.Count; i++)
            {
                var semantic = semantics[i];

                builder.Append(semantic.Type.Name);
                builder.Append(' ');
                builder.Append(semantic.ParamName);
                builder.Append(':');
                builder.Append(semantic.SemanticName);

                if (i != semantics.Count - 1)
                {
                    builder.Append(',');
                }
            }

            builder.Append(')');
            builder.AppendLine();

            builder.Append('{');
            builder.Append('\n');

            WriteMethodBody(builder, body);

            builder.Append('\n');
            builder.Append('}');

            return builder.ToString();
        }

#pragma warning disable RS2008 // Enable analyzer release tracking
        private static readonly DiagnosticDescriptor NoArbitraryParamsInShaderSignature = new DiagnosticDescriptor(
            "VCS0002",
            nameof(NoArbitraryParamsInShaderSignature),
            "Cannot have non-semantic param ('{0}') in a shader signature",
            "Correctness",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true
        );


        private static readonly DiagnosticDescriptor InvalidSemanticForShader = new DiagnosticDescriptor(
            "VCS0003",
            nameof(InvalidSemanticForShader),
            "Cannot have semantic '{0}' in a shader of type {1}",
            "Correctness",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true
        );


        private static readonly DiagnosticDescriptor OnlyASingleSemanticAllowed = new DiagnosticDescriptor(
            "VCS0004",
            nameof(OnlyASingleSemanticAllowed),
            "Only a single semantic is allowed per parameter",
            "Correctness",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true
        );


        private static readonly DiagnosticDescriptor ShaderAccessibleItemsMustBePrivateOrProtectedOrPrivateProtected = new DiagnosticDescriptor(
            "VCS0004",
            nameof(ShaderAccessibleItemsMustBePrivateOrProtectedOrPrivateProtected),
            "Shader accessible items must be <see langword=\"private\"\\>, <see langword=\"protected\"\\> or <see langword=\"private protected\"\\>",
            "Correctness",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true
        );



        private static readonly DiagnosticDescriptor CantWriteToReadOnlySemantic = new DiagnosticDescriptor(
            "VCS0005",
            nameof(CantWriteToReadOnlySemantic),
            "Semantic {0} is read-only in the {1} shader and cannot be written to",
            "Correctness",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true
        );


        private static readonly DiagnosticDescriptor CantReadFromWriteOnlySemantic = new DiagnosticDescriptor(
            "VCS0006",
            nameof(CantReadFromWriteOnlySemantic),
            "Semantic {0} is write-only in the {1} shader and cannot be read from",
            "Correctness",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true
        );


        private static readonly DiagnosticDescriptor SemanticTypeMismatch = new DiagnosticDescriptor(
            "VCS0007",
            nameof(SemanticTypeMismatch),
            "Type {0} is not a valid type for semantic {1} - valid types are {2}",
            "Correctness",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true
        );


        private static readonly DiagnosticDescriptor InvalidIntrinsicUsage = new DiagnosticDescriptor(
            "VCS0008",
            nameof(InvalidIntrinsicUsage),
            "Intrinsic {0} is not usable from the {1} shader",
            "Correctness",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true
        );
#pragma warning restore RS2008 // Enable analyzer release tracking

        private List<HlslSemantic> ParseParameterList(IMethodSymbol symbol)
        {
            var semanticAttributeType = _context.Compilation.GetTypeByMetadataName(SemanticAttribute)!;

            var semantics = new List<HlslSemantic>();

            foreach (var param in symbol.Parameters)
            {
                INamedTypeSymbol? semanticAttr = null;

                foreach (var attr in param.GetAttributes())
                {
                    if (attr.AttributeClass?.IsTypeOrChildOf(semanticAttributeType) ?? false)
                    {
                        if (semanticAttr is not null)
                        {
                            _context.ReportDiagnostic(Diagnostic.Create(OnlyASingleSemanticAllowed, param.Locations[0]));
                        }

                        if (!ValidComputeShaderSemantics.Any(semantic => SymbolEqualityComparer.Default.Equals(_context.Compilation.GetTypeByMetadataName(semantic), attr.AttributeClass)))
                        {
                            _context.ReportDiagnostic(Diagnostic.Create(InvalidSemanticForShader, param.Locations[0], attr.AttributeClass.Name, "Compute"));
                        }

                        semanticAttr = attr.AttributeClass;
                    }
                }

                // No semantic was found
                if (semanticAttr is null)
                {
                    _context.ReportDiagnostic(Diagnostic.Create(NoArbitraryParamsInShaderSignature, param.Locations[0], $"{param.Type.Name} {param.Name}"));
                }
                else
                {
                    semantics.Add(new HlslSemantic(semanticAttr.Name, param.Type, param.Name));
                }
            }

            return semantics;
        }

        private void WriteMethodBody(StringBuilder builder, BlockSyntax body)
        {
            var intrinsicType = _context.Compilation.GetTypeByMetadataName(IntrinsicAttribute)!;
            var walker = new ShaderBodyWalker(_context, _model, builder);
            body.Accept(walker);
            Debugger.Break();
        }

        private class ShaderContext
        {
            //private string[] ValidSemantics { get; }
            //private string Name { get; }
        }


        private static string[] ValidComputeShaderSemantics =
        {
            "Voltium.Core.ShaderLang.Shader+SV_GroupID",
            "Voltium.Core.ShaderLang.Shader+SV_GroupIndex",
            "Voltium.Core.ShaderLang.Shader+SV_GroupThreadID",
            "Voltium.Core.ShaderLang.Shader+SV_DispatchThreadID"
        };

        public MethodCompiler(GeneratorExecutionContext context, SyntaxTree tree)
        {
            _context = context;
            _model = _context.Compilation.GetSemanticModel(tree);
        }
    }

    internal enum HlslWriterOptions
    {
        None,
        Prettify = 1
    }

    internal sealed class HlslWriter
    {
        private bool _pretty;
        private char _statementEnd;
        private StringWriter _base;

        private int _tabCount;
        private bool _onNewLine;

        private void Tab()
        {
            if (!_pretty || !_onNewLine)
            {
                return;
            }

            for (var i = 0; i < _tabCount; i++)
            {
                _base.Write(_tabCount);
            }
        }

        private void NewLine()
        {
            if (!_pretty)
            {
                return;
            }

            _base.WriteLine();
            _onNewLine = true;
        }

        public HlslWriter(HlslWriterOptions options = HlslWriterOptions.None)
        {
            _base = new();
            _pretty = options.HasFlag(HlslWriterOptions.Prettify);
            _statementEnd = ';';
        }

        public void WriteStatement(string statement)
        {
            Tab();
            _onNewLine = false;
            _base.Write(statement);
            _base.Write(_statementEnd);
        }

        public void WriteRaw(char c) => _base.Write(c);
        public void WriteRaw(string s) => _base.Write(s);
        public void WriteExpression(string expr)
        {
            Tab();
            _onNewLine = false;
            _base.Write(expr);
        }

        public void BeginBlock(string? preamble = null)
        {
            if (preamble is not null)
            {
                Tab();
                _base.Write(preamble);
                NewLine();
            }
            Tab();
            _base.Write('{');
            NewLine();
            _tabCount++;
        }

        public void EndBlock()
        {
            _tabCount--;
            Tab();
            _base.Write('}');
            NewLine();
        }
    }

    internal sealed class ShaderWriter : OperationWalker
    {

        private GeneratorExecutionContext _context;
        private SemanticModel _model;
        private HlslWriter _builder;
        private INamedTypeSymbol _remapAttribute;

        public const string NameRemapAttribute = "Voltium.Core.ShaderLang.Shader+NameRemapAttribute";

        private DiagnosticDescriptor NotSupportedFeature = new DiagnosticDescriptor(
#pragma warning disable RS2008 // Enable analyzer release tracking
            "VCS0008",
#pragma warning restore RS2008 // Enable analyzer release tracking
            "Not Supported",
            "Feature '{0}' is not supported in shaders",
            "Correctness",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true
        );

        public ShaderWriter(GeneratorExecutionContext context, SemanticModel model, HlslWriter builder)
        {
            _context = context;
            _model = model;
            _builder = builder;
            _remapAttribute = context.Compilation.GetTypeByMetadataName(NameRemapAttribute)!;
        }

        private string GetRemappedName(ISymbol symbol)
        {
            if (symbol.TryGetAttribute(_remapAttribute, out var attr))
            {
                var name = (string)attr.ConstructorArguments[0].Value!;
                name = string.IsNullOrEmpty(name) ? symbol.Name : name;
                var typeParams = symbol switch
                {
                    INamedTypeSymbol type => type.TypeParameters,
                    IMethodSymbol method => method.TypeParameters,
                    _ => default
                };
                name = string.Format(name, typeParams.Select(p => p.Name));
                return name;
            }
            return symbol.Name;
        }

        private void NotSupported(IOperation operation) => _context.ReportDiagnostic(Diagnostic.Create(NotSupportedFeature, operation.Syntax.GetLocation(), operation.Kind));
        private void NotSupported(IOperation operation, string feature) => _context.ReportDiagnostic(Diagnostic.Create(NotSupportedFeature, operation.Syntax.GetLocation(), feature));

        public override void DefaultVisit(IOperation operation) => base.DefaultVisit(operation);
        public override bool Equals(object obj) => base.Equals(obj);
        public override int GetHashCode() => base.GetHashCode();
        public override string ToString() => base.ToString();
        public override void Visit(IOperation operation) => base.Visit(operation);
        public override void VisitAddressOf(IAddressOfOperation operation) => base.VisitAddressOf(operation);
        public override void VisitAnonymousFunction(IAnonymousFunctionOperation operation) => base.VisitAnonymousFunction(operation);
        public override void VisitAnonymousObjectCreation(IAnonymousObjectCreationOperation operation) => base.VisitAnonymousObjectCreation(operation);
        public override void VisitArgument(IArgumentOperation operation) => base.VisitArgument(operation);
        public override void VisitArrayCreation(IArrayCreationOperation operation) => base.VisitArrayCreation(operation);
        public override void VisitArrayElementReference(IArrayElementReferenceOperation operation) => base.VisitArrayElementReference(operation);
        public override void VisitArrayInitializer(IArrayInitializerOperation operation) => base.VisitArrayInitializer(operation);
        public override void VisitBinaryPattern(IBinaryPatternOperation operation) => base.VisitBinaryPattern(operation);
        public override void VisitBlock(IBlockOperation operation)
        {
            _builder.BeginBlock();
            base.VisitBlock(operation);
            _builder.EndBlock();
        }

        public override void VisitBranch(IBranchOperation operation)
        {
            _builder.WriteStatement(operation.BranchKind switch
            {
                BranchKind.Continue => "continue",
                BranchKind.Break => "break",
                BranchKind.GoTo => "goto",
                _ => throw new NotImplementedException(),
            });
        }

        private string OperatorSymbol(BinaryOperatorKind op)
        {
            return op switch
            {
                BinaryOperatorKind.Add => "+=",
                BinaryOperatorKind.Subtract => "-=",
                BinaryOperatorKind.Multiply => "*=",
                BinaryOperatorKind.Divide => "/=",
                BinaryOperatorKind.IntegerDivide => "/=",
                BinaryOperatorKind.Remainder => "%=",
                BinaryOperatorKind.LeftShift => "<<=",
                BinaryOperatorKind.RightShift => ">>=",
                BinaryOperatorKind.And => "&=",
                BinaryOperatorKind.Or => "|=",
                BinaryOperatorKind.ExclusiveOr => "^=",
                _ => throw new NotImplementedException(),
            };
        }

        public override void VisitBinaryOperator(IBinaryOperation operation)
        {
            if (operation.OperatorMethod is null)
            {
                Visit(operation.LeftOperand);
                _builder.WriteRaw(OperatorSymbol(operation.OperatorKind));
                Visit(operation.RightOperand);
            }
            else
            {
                _builder.WriteExpression("=");
                _builder.WriteExpression(operation.OperatorMethod.Name);
                _builder.WriteRaw('(');
                Visit(operation.LeftOperand);
                _builder.WriteRaw(',');
                Visit(operation.RightOperand);
                _builder.WriteRaw(')');
            }
        }

        public override void VisitCompoundAssignment(ICompoundAssignmentOperation operation)
        {
            Visit(operation.Target);

            Visit(operation.Target);
            if (operation.OperatorMethod is null)
            {
                _builder.WriteRaw(OperatorSymbol(operation.OperatorKind) + "=");
                Visit(operation.Value);
            }
            else
            {
                _builder.WriteExpression("=");
                _builder.WriteExpression(operation.OperatorMethod.Name);
                _builder.WriteRaw('(');
                Visit(operation.Target);
                _builder.WriteRaw(',');
                Visit(operation.Value);
                _builder.WriteRaw(')');
            }
        }

        public override void VisitConditional(IConditionalOperation operation)
        {
            if (operation.IsRef)
            {
                NotSupported(operation, "ByRefs");
            }

            Visit(operation.Condition);
            _builder.WriteRaw('?');
            Visit(operation.WhenTrue);
            _builder.WriteRaw(':');
            Visit(operation.WhenFalse);
        }
        public override void VisitConstantPattern(IConstantPatternOperation operation) => base.VisitConstantPattern(operation);
        public override void VisitConstructorBodyOperation(IConstructorBodyOperation operation) => base.VisitConstructorBodyOperation(operation);
        public override void VisitConversion(IConversionOperation operation) => base.VisitConversion(operation);
        public override void VisitDeclarationExpression(IDeclarationExpressionOperation operation) => base.VisitDeclarationExpression(operation);
        public override void VisitDeclarationPattern(IDeclarationPatternOperation operation) => base.VisitDeclarationPattern(operation);
        public override void VisitDeconstructionAssignment(IDeconstructionAssignmentOperation operation) => base.VisitDeconstructionAssignment(operation);
        public override void VisitDefaultCaseClause(IDefaultCaseClauseOperation operation) => base.VisitDefaultCaseClause(operation);
        public override void VisitDefaultValue(IDefaultValueOperation operation)
        {
            _builder.WriteRaw('(');
            _builder.WriteRaw(GetRemappedName(operation.Type));
            _builder.WriteRaw(')');
            _builder.WriteRaw('0');
        }

        public override void VisitDelegateCreation(IDelegateCreationOperation operation) => NotSupported(operation);
        public override void VisitDiscardOperation(IDiscardOperation operation)
        {
        }

        public override void VisitDiscardPattern(IDiscardPatternOperation operation) => base.VisitDiscardPattern(operation);
        public override void VisitEmpty(IEmptyOperation operation) => _builder.WriteStatement(string.Empty);
        public override void VisitEventAssignment(IEventAssignmentOperation operation) => NotSupported(operation);
        public override void VisitEventReference(IEventReferenceOperation operation) => NotSupported(operation);
        public override void VisitExpressionStatement(IExpressionStatementOperation operation) => base.VisitExpressionStatement(operation);
        public override void VisitFieldInitializer(IFieldInitializerOperation operation) => base.VisitFieldInitializer(operation);
        public override void VisitFieldReference(IFieldReferenceOperation operation) => base.VisitFieldReference(operation);
        public override void VisitFlowAnonymousFunction(IFlowAnonymousFunctionOperation operation) => base.VisitFlowAnonymousFunction(operation);
        public override void VisitFlowCapture(IFlowCaptureOperation operation) => base.VisitFlowCapture(operation);
        public override void VisitFlowCaptureReference(IFlowCaptureReferenceOperation operation) => base.VisitFlowCaptureReference(operation);
        public override void VisitForEachLoop(IForEachLoopOperation operation) => base.VisitForEachLoop(operation);
        public override void VisitForLoop(IForLoopOperation operation) => base.VisitForLoop(operation);
        public override void VisitForToLoop(IForToLoopOperation operation) => base.VisitForToLoop(operation);
        public override void VisitIncrementOrDecrement(IIncrementOrDecrementOperation operation)
        {
            if (operation.OperatorMethod is null)
            {
                var token = operation.Kind == OperationKind.Decrement ? "--" : "++";
                if (!operation.IsPostfix)
                {
                    _builder.WriteRaw(token);
                }

                Visit(operation.Target);

                if (operation.IsPostfix)
                {
                    _builder.WriteRaw(token);
                }
            }
        }

        public override void VisitInstanceReference(IInstanceReferenceOperation operation) => base.VisitInstanceReference(operation);
        public override void VisitInvalid(IInvalidOperation operation) => base.VisitInvalid(operation);
        public override void VisitInvocation(IInvocationOperation operation) => base.VisitInvocation(operation);
        public override void VisitIsNull(IIsNullOperation operation) => base.VisitIsNull(operation);
        public override void VisitIsPattern(IIsPatternOperation operation) => base.VisitIsPattern(operation);
        public override void VisitIsType(IIsTypeOperation operation) => base.VisitIsType(operation);
        public override void VisitLabeled(ILabeledOperation operation) => base.VisitLabeled(operation);
        public override void VisitLiteral(ILiteralOperation operation) => base.VisitLiteral(operation);
        public override void VisitLocalFunction(ILocalFunctionOperation operation) => base.VisitLocalFunction(operation);
        public override void VisitLocalReference(ILocalReferenceOperation operation) => base.VisitLocalReference(operation);


        public override void VisitMemberInitializer(IMemberInitializerOperation operation) => base.VisitMemberInitializer(operation);
        public override void VisitMethodBodyOperation(IMethodBodyOperation operation) => base.VisitMethodBodyOperation(operation);
        public override void VisitMethodReference(IMethodReferenceOperation operation) => base.VisitMethodReference(operation);


        public override void VisitNegatedPattern(INegatedPatternOperation operation) => base.VisitNegatedPattern(operation);
        public override void VisitObjectCreation(IObjectCreationOperation operation) => base.VisitObjectCreation(operation);
        public override void VisitObjectOrCollectionInitializer(IObjectOrCollectionInitializerOperation operation) => base.VisitObjectOrCollectionInitializer(operation);
        public override void VisitOmittedArgument(IOmittedArgumentOperation operation) => base.VisitOmittedArgument(operation);
        public override void VisitParameterInitializer(IParameterInitializerOperation operation) => base.VisitParameterInitializer(operation);
        public override void VisitParameterReference(IParameterReferenceOperation operation) => base.VisitParameterReference(operation);
        public override void VisitParenthesized(IParenthesizedOperation operation) => base.VisitParenthesized(operation);
        public override void VisitPatternCaseClause(IPatternCaseClauseOperation operation) => base.VisitPatternCaseClause(operation);
        public override void VisitPropertyInitializer(IPropertyInitializerOperation operation) => base.VisitPropertyInitializer(operation);
        public override void VisitPropertyReference(IPropertyReferenceOperation operation) => base.VisitPropertyReference(operation);
        public override void VisitPropertySubpattern(IPropertySubpatternOperation operation) => base.VisitPropertySubpattern(operation);
        public override void VisitRangeCaseClause(IRangeCaseClauseOperation operation) => base.VisitRangeCaseClause(operation);
        public override void VisitRangeOperation(IRangeOperation operation) => base.VisitRangeOperation(operation);
        public override void VisitRecursivePattern(IRecursivePatternOperation operation) => base.VisitRecursivePattern(operation);


        public override void VisitRelationalPattern(IRelationalPatternOperation operation) => base.VisitRelationalPattern(operation);
        public override void VisitReturn(IReturnOperation operation)
        {
            _builder.WriteRaw("return");
            Visit(operation.ReturnedValue);
            _builder.WriteStatement(string.Empty);
        }

        public override void VisitSimpleAssignment(ISimpleAssignmentOperation operation) => base.VisitSimpleAssignment(operation);
        public override void VisitSingleValueCaseClause(ISingleValueCaseClauseOperation operation) => base.VisitSingleValueCaseClause(operation);
        public override void VisitSizeOf(ISizeOfOperation operation)
        {
            _builder.WriteExpression("sizeof");
            _builder.WriteRaw('(');
            _builder.WriteRaw(GetRemappedName(operation.TypeOperand));
            _builder.WriteRaw(')');
        }

        public override void VisitSwitch(ISwitchOperation operation) => base.VisitSwitch(operation);
        public override void VisitSwitchCase(ISwitchCaseOperation operation) => base.VisitSwitchCase(operation);
        public override void VisitSwitchExpression(ISwitchExpressionOperation operation) => base.VisitSwitchExpression(operation);
        public override void VisitSwitchExpressionArm(ISwitchExpressionArmOperation operation) => base.VisitSwitchExpressionArm(operation);
        public override void VisitTuple(ITupleOperation operation) => base.VisitTuple(operation);
        public override void VisitTupleBinaryOperator(ITupleBinaryOperation operation) => base.VisitTupleBinaryOperator(operation);
        public override void VisitTypeOf(ITypeOfOperation operation) => base.VisitTypeOf(operation);
        public override void VisitTypeParameterObjectCreation(ITypeParameterObjectCreationOperation operation) => base.VisitTypeParameterObjectCreation(operation);
        public override void VisitTypePattern(ITypePatternOperation operation) => base.VisitTypePattern(operation);
        public override void VisitUnaryOperator(IUnaryOperation operation)
        {
            if (operation.OperatorMethod is null)
            {
                _builder.WriteRaw(operation.OperatorKind switch
                {
                    UnaryOperatorKind.None => throw new NotImplementedException(),
                    UnaryOperatorKind.BitwiseNegation => "~",
                    UnaryOperatorKind.Not => "!",
                    UnaryOperatorKind.Plus => "+",
                    UnaryOperatorKind.Minus => "-",
                    UnaryOperatorKind.True => throw new NotImplementedException(),
                    UnaryOperatorKind.False => throw new NotImplementedException(),
                    UnaryOperatorKind.Hat or _ => throw new NotImplementedException(),
                });

                Visit(operation.Operand);
            }
            else
            {
                _builder.WriteExpression(GetRemappedName(operation.OperatorMethod));
                _builder.WriteRaw('(');
                Visit(operation.Operand);
                _builder.WriteRaw(')');
            }
        }

        public override void VisitUsing(IUsingOperation operation) => base.VisitUsing(operation);
        public override void VisitUsingDeclaration(IUsingDeclarationOperation operation)
        {
            if (operation.IsAsynchronous)
            {
                NotSupported(operation, "Asynchronous Using Declaration");
            }
        }
        public override void VisitVariableDeclaration(IVariableDeclarationOperation operation) => base.VisitVariableDeclaration(operation);
        public override void VisitVariableDeclarationGroup(IVariableDeclarationGroupOperation operation) => base.VisitVariableDeclarationGroup(operation);
        public override void VisitVariableDeclarator(IVariableDeclaratorOperation operation) => base.VisitVariableDeclarator(operation);
        public override void VisitVariableInitializer(IVariableInitializerOperation operation) => base.VisitVariableInitializer(operation);
        public override void VisitWhileLoop(IWhileLoopOperation operation)
        {
            bool isWhile = operation.ConditionIsTop;
            if (isWhile)
            {
                WriteCondition();
            }
            else
            {
                _builder.BeginBlock("do");
            }
            foreach (var child in operation.Children)
            {
                base.Visit(child);
            }
            _builder.EndBlock();
            if (!isWhile)
            {
                WriteCondition();
            }

            void WriteCondition()
            {
                _builder.BeginBlock("while");
                _builder.WriteRaw('(');
                Visit(operation.Condition);
                _builder.WriteRaw(')');
            }
        }


        public override void VisitAwait(IAwaitOperation operation) => NotSupported(operation);
        public override void VisitStaticLocalInitializationSemaphore(IStaticLocalInitializationSemaphoreOperation operation) => NotSupported(operation);
        public override void VisitStop(IStopOperation operation) => NotSupported(operation);
        public override void VisitLock(ILockOperation operation) => NotSupported(operation);
        public override void VisitCatchClause(ICatchClauseOperation operation) => NotSupported(operation);
        public override void VisitCaughtException(ICaughtExceptionOperation operation) => NotSupported(operation);
        public override void VisitCoalesce(ICoalesceOperation operation) => NotSupported(operation);
        public override void VisitCoalesceAssignment(ICoalesceAssignmentOperation operation) => NotSupported(operation);
        public override void VisitConditionalAccess(IConditionalAccessOperation operation) => NotSupported(operation);
        public override void VisitConditionalAccessInstance(IConditionalAccessInstanceOperation operation) => NotSupported(operation);
        public override void VisitDynamicIndexerAccess(IDynamicIndexerAccessOperation operation) => NotSupported(operation);
        public override void VisitDynamicInvocation(IDynamicInvocationOperation operation) => NotSupported(operation);
        public override void VisitDynamicMemberReference(IDynamicMemberReferenceOperation operation) => NotSupported(operation);
        public override void VisitDynamicObjectCreation(IDynamicObjectCreationOperation operation) => NotSupported(operation);
        public override void VisitEnd(IEndOperation operation) => NotSupported(operation);
        [Obsolete]
        public override void VisitCollectionElementInitializer(ICollectionElementInitializerOperation operation) => base.VisitCollectionElementInitializer(operation);
        public override void VisitThrow(IThrowOperation operation) => NotSupported(operation);
        public override void VisitTranslatedQuery(ITranslatedQueryOperation operation) => NotSupported(operation);
        public override void VisitTry(ITryOperation operation) => NotSupported(operation);
        public override void VisitInterpolatedString(IInterpolatedStringOperation operation) => NotSupported(operation);
        public override void VisitInterpolatedStringText(IInterpolatedStringTextOperation operation) => NotSupported(operation);
        public override void VisitInterpolation(IInterpolationOperation operation) => NotSupported(operation);
        public override void VisitNameOf(INameOfOperation operation) => NotSupported(operation);
        public override void VisitRaiseEvent(IRaiseEventOperation operation) => NotSupported(operation);
        public override void VisitReDim(IReDimOperation operation) => NotSupported(operation);
        public override void VisitReDimClause(IReDimClauseOperation operation) => NotSupported(operation);
        public override void VisitRelationalCaseClause(IRelationalCaseClauseOperation operation) => NotSupported(operation);
        public override void VisitWith(IWithOperation operation) => NotSupported(operation);
    }
}
