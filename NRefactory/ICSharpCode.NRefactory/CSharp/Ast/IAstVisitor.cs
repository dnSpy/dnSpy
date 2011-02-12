// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;

namespace ICSharpCode.NRefactory.CSharp
{
	/// <summary>
	/// AST visitor.
	/// </summary>
	public interface AstVisitor<in T, out S>
	{
		S VisitAnonymousMethodExpression(AnonymousMethodExpression anonymousMethodExpression, T data);
		S VisitArgListExpression(ArgListExpression argListExpression, T data);
		S VisitArrayCreateExpression(ArrayCreateExpression arrayCreateExpression, T data);
		S VisitArrayInitializerExpression(ArrayInitializerExpression arrayInitializerExpression, T data);
		S VisitAsExpression(AsExpression asExpression, T data);
		S VisitAssignmentExpression(AssignmentExpression assignmentExpression, T data);
		S VisitBaseReferenceExpression(BaseReferenceExpression baseReferenceExpression, T data);
		S VisitBinaryOperatorExpression(BinaryOperatorExpression binaryOperatorExpression, T data);
		S VisitCastExpression(CastExpression castExpression, T data);
		S VisitCheckedExpression(CheckedExpression checkedExpression, T data);
		S VisitConditionalExpression(ConditionalExpression conditionalExpression, T data);
		S VisitDefaultValueExpression(DefaultValueExpression defaultValueExpression, T data);
		S VisitDirectionExpression(DirectionExpression directionExpression, T data);
		S VisitIdentifierExpression(IdentifierExpression identifierExpression, T data);
		S VisitIndexerExpression(IndexerExpression indexerExpression, T data);
		S VisitInvocationExpression(InvocationExpression invocationExpression, T data);
		S VisitIsExpression(IsExpression isExpression, T data);
		S VisitLambdaExpression(LambdaExpression lambdaExpression, T data);
		S VisitMemberReferenceExpression(MemberReferenceExpression memberReferenceExpression, T data);
		S VisitNamedArgumentExpression(NamedArgumentExpression namedArgumentExpression, T data);
		S VisitNullReferenceExpression(NullReferenceExpression nullReferenceExpression, T data);
		S VisitObjectCreateExpression(ObjectCreateExpression objectCreateExpression, T data);
		S VisitParenthesizedExpression(ParenthesizedExpression parenthesizedExpression, T data);
		S VisitPointerReferenceExpression(PointerReferenceExpression pointerReferenceExpression, T data);
		S VisitPrimitiveExpression(PrimitiveExpression primitiveExpression, T data);
		S VisitSizeOfExpression(SizeOfExpression sizeOfExpression, T data);
		S VisitStackAllocExpression(StackAllocExpression stackAllocExpression, T data);
		S VisitThisReferenceExpression(ThisReferenceExpression thisReferenceExpression, T data);
		S VisitTypeOfExpression(TypeOfExpression typeOfExpression, T data);
		S VisitUnaryOperatorExpression(UnaryOperatorExpression unaryOperatorExpression, T data);
		S VisitUncheckedExpression(UncheckedExpression uncheckedExpression, T data);
		
		S VisitAttribute(Attribute attribute, T data);
		S VisitAttributeSection(AttributeSection attributeSection, T data);
		S VisitDelegateDeclaration(DelegateDeclaration delegateDeclaration, T data);
		S VisitNamespaceDeclaration(NamespaceDeclaration namespaceDeclaration, T data);
		S VisitTypeDeclaration(TypeDeclaration typeDeclaration, T data);
		S VisitUsingAliasDeclaration(UsingAliasDeclaration usingAliasDeclaration, T data);
		S VisitUsingDeclaration(UsingDeclaration usingDeclaration, T data);
		
		S VisitBlockStatement(BlockStatement blockStatement, T data);
		S VisitBreakStatement(BreakStatement breakStatement, T data);
		S VisitCheckedStatement(CheckedStatement checkedStatement, T data);
		S VisitContinueStatement(ContinueStatement continueStatement, T data);
		S VisitEmptyStatement(EmptyStatement emptyStatement, T data);
		S VisitExpressionStatement(ExpressionStatement expressionStatement, T data);
		S VisitFixedStatement(FixedStatement fixedStatement, T data);
		S VisitForeachStatement(ForeachStatement foreachStatement, T data);
		S VisitForStatement(ForStatement forStatement, T data);
		S VisitGotoStatement(GotoStatement gotoStatement, T data);
		S VisitIfElseStatement(IfElseStatement ifElseStatement, T data);
		S VisitLabelStatement(LabelStatement labelStatement, T data);
		S VisitLockStatement(LockStatement lockStatement, T data);
		S VisitReturnStatement(ReturnStatement returnStatement, T data);
		S VisitSwitchStatement(SwitchStatement switchStatement, T data);
		S VisitSwitchSection(SwitchSection switchSection, T data);
		S VisitCaseLabel(CaseLabel caseLabel, T data);
		S VisitThrowStatement(ThrowStatement throwStatement, T data);
		S VisitTryCatchStatement(TryCatchStatement tryCatchStatement, T data);
		S VisitCatchClause(CatchClause catchClause, T data);
		S VisitUncheckedStatement(UncheckedStatement uncheckedStatement, T data);
		S VisitUnsafeStatement(UnsafeStatement unsafeStatement, T data);
		S VisitUsingStatement(UsingStatement usingStatement, T data);
		S VisitVariableDeclarationStatement(VariableDeclarationStatement variableDeclarationStatement, T data);
		S VisitWhileStatement(WhileStatement whileStatement, T data);
		S VisitYieldStatement(YieldStatement yieldStatement, T data);
		
		S VisitAccessor(Accessor accessor, T data);
		S VisitConstructorDeclaration(ConstructorDeclaration constructorDeclaration, T data);
		S VisitConstructorInitializer(ConstructorInitializer constructorInitializer, T data);
		S VisitDestructorDeclaration(DestructorDeclaration destructorDeclaration, T data);
		S VisitEnumMemberDeclaration(EnumMemberDeclaration enumMemberDeclaration, T data);
		S VisitEventDeclaration(EventDeclaration eventDeclaration, T data);
		S VisitCustomEventDeclaration(CustomEventDeclaration customEventDeclaration, T data);
		S VisitFieldDeclaration(FieldDeclaration fieldDeclaration, T data);
		S VisitIndexerDeclaration(IndexerDeclaration indexerDeclaration, T data);
		S VisitMethodDeclaration(MethodDeclaration methodDeclaration, T data);
		S VisitOperatorDeclaration(OperatorDeclaration operatorDeclaration, T data);
		S VisitParameterDeclaration(ParameterDeclaration parameterDeclaration, T data);
		S VisitPropertyDeclaration(PropertyDeclaration propertyDeclaration, T data);
		S VisitVariableInitializer(VariableInitializer variableInitializer, T data);
		
		S VisitCompilationUnit(CompilationUnit compilationUnit, T data);
		S VisitSimpleType(SimpleType simpleType, T data);
		S VisitMemberType(MemberType memberType, T data);
		S VisitComposedType(ComposedType composedType, T data);
		S VisitArraySpecifier(ArraySpecifier arraySpecifier, T data);
		S VisitPrimitiveType(PrimitiveType primitiveType, T data);
		
		S VisitComment(Comment comment, T data);
		S VisitTypeParameterDeclaration(TypeParameterDeclaration typeParameterDeclaration, T data);
		S VisitConstraint(Constraint constraint, T data);
		S VisitCSharpTokenNode(CSharpTokenNode cSharpTokenNode, T data);
		S VisitIdentifier(Identifier identifier, T data);
	}
}
