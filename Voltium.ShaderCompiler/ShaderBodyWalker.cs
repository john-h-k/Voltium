using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Voltium.Analyzers;

namespace Voltium.ShaderCompiler
{
    internal sealed class ShaderBodyWalker : CSharpSyntaxWalker
    {
        private GeneratorExecutionContext _context;
        private SemanticModel _model;
        private StringBuilder _builder;

        public ShaderBodyWalker(GeneratorExecutionContext context, SemanticModel model, StringBuilder builder) : base(SyntaxWalkerDepth.Node)
        {
            _context = context;
            _model = model;
            _builder = builder;
        }


        private string RemapType(ExpressionSyntax type) => type.ToString();

        private void NotSupported(string? s = null) => throw null!;

        public override void DefaultVisit(SyntaxNode node) => base.DefaultVisit(node);
        public override bool Equals(object obj) => base.Equals(obj);
        public override int GetHashCode() => base.GetHashCode();
        public override string ToString() => base.ToString();
        public override void Visit(SyntaxNode? node) => base.Visit(node);
        public override void VisitAliasQualifiedName(AliasQualifiedNameSyntax node) => base.VisitAliasQualifiedName(node);
        public override void VisitAnonymousMethodExpression(AnonymousMethodExpressionSyntax node) => base.VisitAnonymousMethodExpression(node);
        public override void VisitAnonymousObjectCreationExpression(AnonymousObjectCreationExpressionSyntax node) => base.VisitAnonymousObjectCreationExpression(node);
        public override void VisitAnonymousObjectMemberDeclarator(AnonymousObjectMemberDeclaratorSyntax node) => base.VisitAnonymousObjectMemberDeclarator(node);
        public override void VisitArgument(ArgumentSyntax node) => base.VisitArgument(node);
        public override void VisitArgumentList(ArgumentListSyntax node) => base.VisitArgumentList(node);
        public override void VisitArrayCreationExpression(ArrayCreationExpressionSyntax node) => base.VisitArrayCreationExpression(node);
        public override void VisitArrayRankSpecifier(ArrayRankSpecifierSyntax node) => base.VisitArrayRankSpecifier(node);
        public override void VisitArrayType(ArrayTypeSyntax node) => base.VisitArrayType(node);
        public override void VisitArrowExpressionClause(ArrowExpressionClauseSyntax node) => base.VisitArrowExpressionClause(node);
        public override void VisitAssignmentExpression(AssignmentExpressionSyntax node)
        {
            base.Visit(node.Left);
            _builder.Append(node.OperatorToken);
            base.Visit(node.Right);
        }
        public override void VisitAttribute(AttributeSyntax node) => base.VisitAttribute(node);
        public override void VisitAttributeArgument(AttributeArgumentSyntax node) => base.VisitAttributeArgument(node);
        public override void VisitAttributeArgumentList(AttributeArgumentListSyntax node) => base.VisitAttributeArgumentList(node);
        public override void VisitAttributeList(AttributeListSyntax node) => base.VisitAttributeList(node);
        public override void VisitAttributeTargetSpecifier(AttributeTargetSpecifierSyntax node) => base.VisitAttributeTargetSpecifier(node);
        public override void VisitAwaitExpression(AwaitExpressionSyntax node) => base.VisitAwaitExpression(node);
        public override void VisitBadDirectiveTrivia(BadDirectiveTriviaSyntax node) => base.VisitBadDirectiveTrivia(node);
        public override void VisitBaseExpression(BaseExpressionSyntax node) => base.VisitBaseExpression(node);
        public override void VisitBaseList(BaseListSyntax node) => base.VisitBaseList(node);
        public override void VisitBinaryExpression(BinaryExpressionSyntax node)
        {
            base.Visit(node.Left);
            _builder.Append(' ');
            _builder.Append(node.OperatorToken);
            _builder.Append(' ');
            base.Visit(node.Right);
        }

        public override void VisitBinaryPattern(BinaryPatternSyntax node) => base.VisitBinaryPattern(node);
        public override void VisitBlock(BlockSyntax Node)
        {
            _builder.Append('{');
            base.VisitBlock(Node);
            _builder.Append('}');
        }

        public override void VisitBracketedArgumentList(BracketedArgumentListSyntax node) => base.VisitBracketedArgumentList(node);
        public override void VisitBracketedParameterList(BracketedParameterListSyntax node) => base.VisitBracketedParameterList(node);
        public override void VisitBreakStatement(BreakStatementSyntax node)
        {
            _builder.Append("break;");
        }

        public override void VisitCasePatternSwitchLabel(CasePatternSwitchLabelSyntax node) => base.VisitCasePatternSwitchLabel(node);
        public override void VisitCaseSwitchLabel(CaseSwitchLabelSyntax node) => base.VisitCaseSwitchLabel(node);
        public override void VisitCastExpression(CastExpressionSyntax node)
        {
            if (!CastIsLegal(node))
            {

            }

            _builder.Append('(');
            _builder.Append(node.Type.ToString());
            _builder.Append(')');
            base.Visit(node.Expression);
        }

        private bool CastIsLegal(CastExpressionSyntax node)
        {
            return true;
        }

        public override void VisitCatchClause(CatchClauseSyntax node) => base.VisitCatchClause(node);
        public override void VisitCatchDeclaration(CatchDeclarationSyntax node) => base.VisitCatchDeclaration(node);
        public override void VisitCatchFilterClause(CatchFilterClauseSyntax node) => base.VisitCatchFilterClause(node);
        public override void VisitCheckedExpression(CheckedExpressionSyntax node) => base.VisitCheckedExpression(node);
        public override void VisitCheckedStatement(CheckedStatementSyntax node) => base.VisitCheckedStatement(node);
        public override void VisitClassDeclaration(ClassDeclarationSyntax node) => base.VisitClassDeclaration(node);
        public override void VisitClassOrStructConstraint(ClassOrStructConstraintSyntax node) => base.VisitClassOrStructConstraint(node);
        public override void VisitCompilationUnit(CompilationUnitSyntax node) => base.VisitCompilationUnit(node);
        public override void VisitConditionalAccessExpression(ConditionalAccessExpressionSyntax node) => base.VisitConditionalAccessExpression(node);
        public override void VisitConditionalExpression(ConditionalExpressionSyntax node)
        {
            base.Visit(node.Condition);
            _builder.Append('?');
            base.Visit(node.WhenTrue);
            _builder.Append(':');
            base.Visit(node.WhenFalse);
        }

        public override void VisitConstantPattern(ConstantPatternSyntax node) => base.VisitConstantPattern(node);
        public override void VisitConstructorConstraint(ConstructorConstraintSyntax node) => base.VisitConstructorConstraint(node);
        public override void VisitConstructorDeclaration(ConstructorDeclarationSyntax node) => base.VisitConstructorDeclaration(node);
        public override void VisitConstructorInitializer(ConstructorInitializerSyntax node) => base.VisitConstructorInitializer(node);
        public override void VisitContinueStatement(ContinueStatementSyntax node) => base.VisitContinueStatement(node);
        public override void VisitConversionOperatorDeclaration(ConversionOperatorDeclarationSyntax node) => base.VisitConversionOperatorDeclaration(node);
        public override void VisitConversionOperatorMemberCref(ConversionOperatorMemberCrefSyntax node) => base.VisitConversionOperatorMemberCref(node);
        public override void VisitCrefBracketedParameterList(CrefBracketedParameterListSyntax node) => base.VisitCrefBracketedParameterList(node);
        public override void VisitCrefParameter(CrefParameterSyntax node) => base.VisitCrefParameter(node);
        public override void VisitCrefParameterList(CrefParameterListSyntax node) => base.VisitCrefParameterList(node);
        public override void VisitDeclarationExpression(DeclarationExpressionSyntax node) => base.VisitDeclarationExpression(node);
        public override void VisitDeclarationPattern(DeclarationPatternSyntax node) => base.VisitDeclarationPattern(node);
        public override void VisitDefaultConstraint(DefaultConstraintSyntax node) => base.VisitDefaultConstraint(node);
        public override void VisitDefaultExpression(DefaultExpressionSyntax node) => base.VisitDefaultExpression(node);
        public override void VisitDefaultSwitchLabel(DefaultSwitchLabelSyntax node) => base.VisitDefaultSwitchLabel(node);
        public override void VisitDefineDirectiveTrivia(DefineDirectiveTriviaSyntax node) => base.VisitDefineDirectiveTrivia(node);
        public override void VisitDelegateDeclaration(DelegateDeclarationSyntax node) => base.VisitDelegateDeclaration(node);
        public override void VisitDestructorDeclaration(DestructorDeclarationSyntax node) => base.VisitDestructorDeclaration(node);
        public override void VisitDiscardDesignation(DiscardDesignationSyntax node) => base.VisitDiscardDesignation(node);
        public override void VisitDiscardPattern(DiscardPatternSyntax node) => base.VisitDiscardPattern(node);
        public override void VisitDocumentationCommentTrivia(DocumentationCommentTriviaSyntax node) => base.VisitDocumentationCommentTrivia(node);
        public override void VisitDoStatement(DoStatementSyntax node) => base.VisitDoStatement(node);
        public override void VisitElementAccessExpression(ElementAccessExpressionSyntax node) => base.VisitElementAccessExpression(node);
        public override void VisitElementBindingExpression(ElementBindingExpressionSyntax node) => base.VisitElementBindingExpression(node);
        public override void VisitElifDirectiveTrivia(ElifDirectiveTriviaSyntax node) => base.VisitElifDirectiveTrivia(node);
        public override void VisitElseClause(ElseClauseSyntax node)
        {
            _builder.Append("else");
            base.Visit(node);
        }
        public override void VisitElseDirectiveTrivia(ElseDirectiveTriviaSyntax node) => base.VisitElseDirectiveTrivia(node);
        public override void VisitEmptyStatement(EmptyStatementSyntax node) => base.VisitEmptyStatement(node);
        public override void VisitEndIfDirectiveTrivia(EndIfDirectiveTriviaSyntax node) => base.VisitEndIfDirectiveTrivia(node);
        public override void VisitEndRegionDirectiveTrivia(EndRegionDirectiveTriviaSyntax node) => base.VisitEndRegionDirectiveTrivia(node);
        public override void VisitEnumDeclaration(EnumDeclarationSyntax node) => base.VisitEnumDeclaration(node);
        public override void VisitEnumMemberDeclaration(EnumMemberDeclarationSyntax node) => base.VisitEnumMemberDeclaration(node);
        public override void VisitEqualsValueClause(EqualsValueClauseSyntax node) => base.VisitEqualsValueClause(node);
        public override void VisitErrorDirectiveTrivia(ErrorDirectiveTriviaSyntax node) => base.VisitErrorDirectiveTrivia(node);
        public override void VisitEventDeclaration(EventDeclarationSyntax node) => base.VisitEventDeclaration(node);
        public override void VisitEventFieldDeclaration(EventFieldDeclarationSyntax node) => base.VisitEventFieldDeclaration(node);
        public override void VisitExplicitInterfaceSpecifier(ExplicitInterfaceSpecifierSyntax node) => base.VisitExplicitInterfaceSpecifier(node);
        public override void VisitExpressionStatement(ExpressionStatementSyntax node)
        {
            base.VisitExpressionStatement(node);
            _builder.Append(node.SemicolonToken.ToString());
        }

        public override void VisitExternAliasDirective(ExternAliasDirectiveSyntax node) => base.VisitExternAliasDirective(node);
        public override void VisitFieldDeclaration(FieldDeclarationSyntax node) => base.VisitFieldDeclaration(node);
        public override void VisitFinallyClause(FinallyClauseSyntax node) => base.VisitFinallyClause(node);
        public override void VisitFixedStatement(FixedStatementSyntax node) => base.VisitFixedStatement(node);
        public override void VisitForEachStatement(ForEachStatementSyntax node) => base.VisitForEachStatement(node);
        public override void VisitForEachVariableStatement(ForEachVariableStatementSyntax node) => base.VisitForEachVariableStatement(node);
        public override void VisitForStatement(ForStatementSyntax node)
        {
            base.VisitForStatement(node);
        }

        public override void VisitFromClause(FromClauseSyntax node) => base.VisitFromClause(node);
        public override void VisitFunctionPointerCallingConvention(FunctionPointerCallingConventionSyntax node) => base.VisitFunctionPointerCallingConvention(node);
        public override void VisitFunctionPointerParameter(FunctionPointerParameterSyntax node) => base.VisitFunctionPointerParameter(node);
        public override void VisitFunctionPointerParameterList(FunctionPointerParameterListSyntax node) => base.VisitFunctionPointerParameterList(node);
        public override void VisitFunctionPointerType(FunctionPointerTypeSyntax node) => base.VisitFunctionPointerType(node);
        public override void VisitFunctionPointerUnmanagedCallingConvention(FunctionPointerUnmanagedCallingConventionSyntax node) => base.VisitFunctionPointerUnmanagedCallingConvention(node);
        public override void VisitFunctionPointerUnmanagedCallingConventionList(FunctionPointerUnmanagedCallingConventionListSyntax node) => base.VisitFunctionPointerUnmanagedCallingConventionList(node);
        public override void VisitGenericName(GenericNameSyntax node) => base.VisitGenericName(node);
        public override void VisitGlobalStatement(GlobalStatementSyntax node) => base.VisitGlobalStatement(node);
        public override void VisitGotoStatement(GotoStatementSyntax node) => base.VisitGotoStatement(node);
        public override void VisitGroupClause(GroupClauseSyntax node) => base.VisitGroupClause(node);
        public override void VisitIdentifierName(IdentifierNameSyntax node)
        {
            // if (TryGetWellKnownIdentifier(node, out WellKnownIdentifier identifier))
            // {
            //     switch (identifier)
            //     {
            //
            //     }
            // }
            // _builder.Append(node.ToString());
            // base.VisitIdentifierName(node);
        }

        public override void VisitIfDirectiveTrivia(IfDirectiveTriviaSyntax node) => base.VisitIfDirectiveTrivia(node);
        public override void VisitIfStatement(IfStatementSyntax node)
        {
            _builder.Append("if");
            _builder.Append('(');
            base.Visit(node.Condition);
            _builder.Append(')');
            base.Visit(node.Statement);
            base.Visit(node.Else);
        }

        public override void VisitImplicitArrayCreationExpression(ImplicitArrayCreationExpressionSyntax node) => base.VisitImplicitArrayCreationExpression(node);
        public override void VisitImplicitElementAccess(ImplicitElementAccessSyntax node) => base.VisitImplicitElementAccess(node);
        public override void VisitImplicitObjectCreationExpression(ImplicitObjectCreationExpressionSyntax node) => base.VisitImplicitObjectCreationExpression(node);
        public override void VisitImplicitStackAllocArrayCreationExpression(ImplicitStackAllocArrayCreationExpressionSyntax node) => base.VisitImplicitStackAllocArrayCreationExpression(node);
        public override void VisitIncompleteMember(IncompleteMemberSyntax node) => base.VisitIncompleteMember(node);
        public override void VisitIndexerDeclaration(IndexerDeclarationSyntax node) => base.VisitIndexerDeclaration(node);
        public override void VisitIndexerMemberCref(IndexerMemberCrefSyntax node) => base.VisitIndexerMemberCref(node);
        public override void VisitInitializerExpression(InitializerExpressionSyntax node) => base.VisitInitializerExpression(node);
        public override void VisitInterfaceDeclaration(InterfaceDeclarationSyntax node) => base.VisitInterfaceDeclaration(node);
        public override void VisitInterpolatedStringExpression(InterpolatedStringExpressionSyntax node) => base.VisitInterpolatedStringExpression(node);
        public override void VisitInterpolatedStringText(InterpolatedStringTextSyntax node) => base.VisitInterpolatedStringText(node);
        public override void VisitInterpolation(InterpolationSyntax node) => base.VisitInterpolation(node);
        public override void VisitInterpolationAlignmentClause(InterpolationAlignmentClauseSyntax node) => base.VisitInterpolationAlignmentClause(node);
        public override void VisitInterpolationFormatClause(InterpolationFormatClauseSyntax node) => base.VisitInterpolationFormatClause(node);
        public override void VisitInvocationExpression(InvocationExpressionSyntax node)
        {
            base.Visit(node.Expression);
            _builder.Append('(');
            base.Visit(node.ArgumentList);
            _builder.Append(')');
        }
        public override void VisitIsPatternExpression(IsPatternExpressionSyntax node) => base.VisitIsPatternExpression(node);
        public override void VisitJoinClause(JoinClauseSyntax node) => base.VisitJoinClause(node);
        public override void VisitJoinIntoClause(JoinIntoClauseSyntax node) => base.VisitJoinIntoClause(node);
        public override void VisitLabeledStatement(LabeledStatementSyntax node) => base.VisitLabeledStatement(node);
        public override void VisitLeadingTrivia(SyntaxToken token) => base.VisitLeadingTrivia(token);
        public override void VisitLetClause(LetClauseSyntax node) => base.VisitLetClause(node);
        public override void VisitLineDirectiveTrivia(LineDirectiveTriviaSyntax node) => base.VisitLineDirectiveTrivia(node);
        public override void VisitLiteralExpression(LiteralExpressionSyntax node)
        {
            if (_model.GetConstantValue(node) is { HasValue: true } constant)
            {
                _builder.Append(constant.Value);
            }
        }
        public override void VisitLoadDirectiveTrivia(LoadDirectiveTriviaSyntax node) => base.VisitLoadDirectiveTrivia(node);
        public override void VisitLocalDeclarationStatement(LocalDeclarationStatementSyntax node) => base.VisitLocalDeclarationStatement(node);
        public override void VisitLocalFunctionStatement(LocalFunctionStatementSyntax node) => base.VisitLocalFunctionStatement(node);
        public override void VisitLockStatement(LockStatementSyntax node) => base.VisitLockStatement(node);
        public override void VisitMakeRefExpression(MakeRefExpressionSyntax node) => base.VisitMakeRefExpression(node);
        public override void VisitMemberAccessExpression(MemberAccessExpressionSyntax node)
        {
            if (_model.GetSymbolInfo(node).Symbol is IMethodSymbol symbol
                && symbol.TryGetAttribute(ShaderStrings.IntrinsicAttribute, _context.Compilation, out var intrinsic))
            {
                _builder.Append((string?)intrinsic.ConstructorArguments[0].Value ?? symbol.Name.ToLowerInvariant());
            }
            else
            {
                _builder.Append(node.Name.ToString());
                base.VisitMemberAccessExpression(node);
            }
        }

        public override void VisitMemberBindingExpression(MemberBindingExpressionSyntax node) => base.VisitMemberBindingExpression(node);
        public override void VisitMethodDeclaration(MethodDeclarationSyntax node) => base.VisitMethodDeclaration(node);
        public override void VisitNameColon(NameColonSyntax node) => base.VisitNameColon(node);
        public override void VisitNameEquals(NameEqualsSyntax node) => base.VisitNameEquals(node);
        public override void VisitNameMemberCref(NameMemberCrefSyntax node) => base.VisitNameMemberCref(node);
        public override void VisitNamespaceDeclaration(NamespaceDeclarationSyntax node) => base.VisitNamespaceDeclaration(node);
        public override void VisitNullableDirectiveTrivia(NullableDirectiveTriviaSyntax node) => base.VisitNullableDirectiveTrivia(node);
        public override void VisitNullableType(NullableTypeSyntax node) => base.VisitNullableType(node);
        public override void VisitObjectCreationExpression(ObjectCreationExpressionSyntax node)
        {
            foreach (var field in node.Initializer?.Expressions ?? default)
            {
                if (field is AssignmentExpressionSyntax assignment)
                {
                    if (assignment.Left is IdentifierNameSyntax name)
                    {
                    }
                }
            }
        }
        public override void VisitOmittedArraySizeExpression(OmittedArraySizeExpressionSyntax node) => base.VisitOmittedArraySizeExpression(node);
        public override void VisitOmittedTypeArgument(OmittedTypeArgumentSyntax node) => base.VisitOmittedTypeArgument(node);
        public override void VisitOperatorDeclaration(OperatorDeclarationSyntax node) => base.VisitOperatorDeclaration(node);
        public override void VisitOperatorMemberCref(OperatorMemberCrefSyntax node) => base.VisitOperatorMemberCref(node);
        public override void VisitOrderByClause(OrderByClauseSyntax node) => base.VisitOrderByClause(node);
        public override void VisitOrdering(OrderingSyntax node) => base.VisitOrdering(node);
        public override void VisitParameter(ParameterSyntax node) => base.VisitParameter(node);
        public override void VisitParameterList(ParameterListSyntax node) => base.VisitParameterList(node);
        public override void VisitParenthesizedExpression(ParenthesizedExpressionSyntax node)
        {
            _builder.Append(node.OpenParenToken);
            Visit(node.Expression);
            _builder.Append(node.CloseParenToken);
        }
        public override void VisitParenthesizedLambdaExpression(ParenthesizedLambdaExpressionSyntax node) => base.VisitParenthesizedLambdaExpression(node);
        public override void VisitParenthesizedPattern(ParenthesizedPatternSyntax node) => base.VisitParenthesizedPattern(node);
        public override void VisitParenthesizedVariableDesignation(ParenthesizedVariableDesignationSyntax node) => base.VisitParenthesizedVariableDesignation(node);
        public override void VisitPointerType(PointerTypeSyntax node) => base.VisitPointerType(node);
        public override void VisitPositionalPatternClause(PositionalPatternClauseSyntax node) => base.VisitPositionalPatternClause(node);
        public override void VisitPostfixUnaryExpression(PostfixUnaryExpressionSyntax node) => base.VisitPostfixUnaryExpression(node);
        public override void VisitPragmaChecksumDirectiveTrivia(PragmaChecksumDirectiveTriviaSyntax node) => base.VisitPragmaChecksumDirectiveTrivia(node);
        public override void VisitPragmaWarningDirectiveTrivia(PragmaWarningDirectiveTriviaSyntax node) => base.VisitPragmaWarningDirectiveTrivia(node);
        public override void VisitPredefinedType(PredefinedTypeSyntax node) => base.VisitPredefinedType(node);
        public override void VisitPrefixUnaryExpression(PrefixUnaryExpressionSyntax node)
        {
            _builder.Append(node.OperatorToken);
            Visit(node.Operand);
        }
        public override void VisitPrimaryConstructorBaseType(PrimaryConstructorBaseTypeSyntax node) => base.VisitPrimaryConstructorBaseType(node);
        public override void VisitPropertyDeclaration(PropertyDeclarationSyntax node) => base.VisitPropertyDeclaration(node);
        public override void VisitPropertyPatternClause(PropertyPatternClauseSyntax node) => base.VisitPropertyPatternClause(node);
        public override void VisitQualifiedCref(QualifiedCrefSyntax node) => base.VisitQualifiedCref(node);
        public override void VisitQualifiedName(QualifiedNameSyntax node) => base.VisitQualifiedName(node);
        public override void VisitQueryBody(QueryBodySyntax node) => base.VisitQueryBody(node);
        public override void VisitQueryContinuation(QueryContinuationSyntax node) => base.VisitQueryContinuation(node);
        public override void VisitQueryExpression(QueryExpressionSyntax node) => base.VisitQueryExpression(node);
        public override void VisitRangeExpression(RangeExpressionSyntax node) => base.VisitRangeExpression(node);
        public override void VisitRecordDeclaration(RecordDeclarationSyntax node) => base.VisitRecordDeclaration(node);
        public override void VisitRecursivePattern(RecursivePatternSyntax node) => base.VisitRecursivePattern(node);
        public override void VisitReferenceDirectiveTrivia(ReferenceDirectiveTriviaSyntax node) => base.VisitReferenceDirectiveTrivia(node);
        public override void VisitRefExpression(RefExpressionSyntax node) => base.VisitRefExpression(node);
        public override void VisitRefType(RefTypeSyntax node) => base.VisitRefType(node);
        public override void VisitRefTypeExpression(RefTypeExpressionSyntax node) => base.VisitRefTypeExpression(node);
        public override void VisitRefValueExpression(RefValueExpressionSyntax node) => base.VisitRefValueExpression(node);
        public override void VisitRegionDirectiveTrivia(RegionDirectiveTriviaSyntax node) => base.VisitRegionDirectiveTrivia(node);
        public override void VisitRelationalPattern(RelationalPatternSyntax node) => base.VisitRelationalPattern(node);
        public override void VisitReturnStatement(ReturnStatementSyntax node) => base.VisitReturnStatement(node);
        public override void VisitSelectClause(SelectClauseSyntax node) => base.VisitSelectClause(node);
        public override void VisitShebangDirectiveTrivia(ShebangDirectiveTriviaSyntax node) => base.VisitShebangDirectiveTrivia(node);
        public override void VisitSimpleBaseType(SimpleBaseTypeSyntax node) => base.VisitSimpleBaseType(node);
        public override void VisitSimpleLambdaExpression(SimpleLambdaExpressionSyntax node) => base.VisitSimpleLambdaExpression(node);
        public override void VisitSingleVariableDesignation(SingleVariableDesignationSyntax node) => base.VisitSingleVariableDesignation(node);
        public override void VisitSizeOfExpression(SizeOfExpressionSyntax node) => base.VisitSizeOfExpression(node);
        public override void VisitSkippedTokensTrivia(SkippedTokensTriviaSyntax node) => base.VisitSkippedTokensTrivia(node);
        public override void VisitStackAllocArrayCreationExpression(StackAllocArrayCreationExpressionSyntax node) => base.VisitStackAllocArrayCreationExpression(node);
        public override void VisitStructDeclaration(StructDeclarationSyntax node)
        {
            _builder.Append("struct");
            _builder.Append(' ');
            _builder.Append(node.Identifier);
            _builder.Append('{');
            foreach (var member in node.Members)
            {
                Visit(member);
            }
            _builder.Append('}');
        }
        public override void VisitSubpattern(SubpatternSyntax node) => base.VisitSubpattern(node);
        public override void VisitSwitchExpression(SwitchExpressionSyntax node) => base.VisitSwitchExpression(node);
        public override void VisitSwitchExpressionArm(SwitchExpressionArmSyntax node) => base.VisitSwitchExpressionArm(node);
        public override void VisitSwitchSection(SwitchSectionSyntax node) => base.VisitSwitchSection(node);
        public override void VisitSwitchStatement(SwitchStatementSyntax node) => base.VisitSwitchStatement(node);
        public override void VisitThisExpression(ThisExpressionSyntax node) => base.VisitThisExpression(node);
        public override void VisitThrowExpression(ThrowExpressionSyntax node) => base.VisitThrowExpression(node);
        public override void VisitThrowStatement(ThrowStatementSyntax node) => base.VisitThrowStatement(node);
        public override void VisitToken(SyntaxToken token) => base.VisitToken(token);
        public override void VisitTrailingTrivia(SyntaxToken token) => base.VisitTrailingTrivia(token);
        public override void VisitTrivia(SyntaxTrivia trivia) => base.VisitTrivia(trivia);
        public override void VisitTryStatement(TryStatementSyntax node) => base.VisitTryStatement(node);
        public override void VisitTupleElement(TupleElementSyntax node) => base.VisitTupleElement(node);
        public override void VisitTupleExpression(TupleExpressionSyntax node) => base.VisitTupleExpression(node);
        public override void VisitTupleType(TupleTypeSyntax node) => base.VisitTupleType(node);
        public override void VisitTypeArgumentList(TypeArgumentListSyntax node) => base.VisitTypeArgumentList(node);
        public override void VisitTypeConstraint(TypeConstraintSyntax node) => base.VisitTypeConstraint(node);
        public override void VisitTypeCref(TypeCrefSyntax node) => base.VisitTypeCref(node);
        public override void VisitTypeOfExpression(TypeOfExpressionSyntax node) => base.VisitTypeOfExpression(node);
        public override void VisitTypeParameter(TypeParameterSyntax node) => base.VisitTypeParameter(node);
        public override void VisitTypeParameterConstraintClause(TypeParameterConstraintClauseSyntax node) => base.VisitTypeParameterConstraintClause(node);
        public override void VisitTypeParameterList(TypeParameterListSyntax node) => base.VisitTypeParameterList(node);
        public override void VisitTypePattern(TypePatternSyntax node) => base.VisitTypePattern(node);
        public override void VisitUnaryPattern(UnaryPatternSyntax node) => base.VisitUnaryPattern(node);
        public override void VisitUndefDirectiveTrivia(UndefDirectiveTriviaSyntax node) => base.VisitUndefDirectiveTrivia(node);
        public override void VisitUnsafeStatement(UnsafeStatementSyntax node) => base.VisitUnsafeStatement(node);
        public override void VisitUsingDirective(UsingDirectiveSyntax node) => base.VisitUsingDirective(node);
        public override void VisitUsingStatement(UsingStatementSyntax node) => base.VisitUsingStatement(node);
        public override void VisitVariableDeclaration(VariableDeclarationSyntax node) => base.VisitVariableDeclaration(node);
        public override void VisitVariableDeclarator(VariableDeclaratorSyntax node) => base.VisitVariableDeclarator(node);
        public override void VisitVarPattern(VarPatternSyntax node) => base.VisitVarPattern(node);
        public override void VisitWarningDirectiveTrivia(WarningDirectiveTriviaSyntax node) => base.VisitWarningDirectiveTrivia(node);
        public override void VisitWhenClause(WhenClauseSyntax node) => base.VisitWhenClause(node);
        public override void VisitWhileStatement(WhileStatementSyntax node) => base.VisitWhileStatement(node);
        public override void VisitWithExpression(WithExpressionSyntax node) => base.VisitWithExpression(node);
    }
}
