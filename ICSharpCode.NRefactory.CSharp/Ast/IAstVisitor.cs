// Copyright (c) AlphaSierraPapa for the SharpDevelop Team
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

using System;

namespace ICSharpCode.NRefactory.CSharp
{
	/// <summary>
	/// AST visitor.
	/// </summary>
	public interface IAstVisitor
	{
		void VisitAnonymousMethodExpression(AnonymousMethodExpression anonymousMethodExpression);
		void VisitUndocumentedExpression(UndocumentedExpression undocumentedExpression);
		void VisitArrayCreateExpression(ArrayCreateExpression arrayCreateExpression);
		void VisitArrayInitializerExpression(ArrayInitializerExpression arrayInitializerExpression);
		void VisitAsExpression(AsExpression asExpression);
		void VisitAssignmentExpression(AssignmentExpression assignmentExpression);
		void VisitBaseReferenceExpression(BaseReferenceExpression baseReferenceExpression);
		void VisitBinaryOperatorExpression(BinaryOperatorExpression binaryOperatorExpression);
		void VisitCastExpression(CastExpression castExpression);
		void VisitCheckedExpression(CheckedExpression checkedExpression);
		void VisitConditionalExpression(ConditionalExpression conditionalExpression);
		void VisitDefaultValueExpression(DefaultValueExpression defaultValueExpression);
		void VisitDirectionExpression(DirectionExpression directionExpression);
		void VisitIdentifierExpression(IdentifierExpression identifierExpression);
		void VisitIndexerExpression(IndexerExpression indexerExpression);
		void VisitInvocationExpression(InvocationExpression invocationExpression);
		void VisitIsExpression(IsExpression isExpression);
		void VisitLambdaExpression(LambdaExpression lambdaExpression);
		void VisitMemberReferenceExpression(MemberReferenceExpression memberReferenceExpression);
		void VisitNamedArgumentExpression(NamedArgumentExpression namedArgumentExpression);
		void VisitNamedExpression(NamedExpression namedExpression);
		void VisitNullReferenceExpression(NullReferenceExpression nullReferenceExpression);
		void VisitObjectCreateExpression(ObjectCreateExpression objectCreateExpression);
		void VisitAnonymousTypeCreateExpression(AnonymousTypeCreateExpression anonymousTypeCreateExpression);
		void VisitParenthesizedExpression(ParenthesizedExpression parenthesizedExpression);
		void VisitPointerReferenceExpression(PointerReferenceExpression pointerReferenceExpression);
		void VisitPrimitiveExpression(PrimitiveExpression primitiveExpression);
		void VisitSizeOfExpression(SizeOfExpression sizeOfExpression);
		void VisitStackAllocExpression(StackAllocExpression stackAllocExpression);
		void VisitThisReferenceExpression(ThisReferenceExpression thisReferenceExpression);
		void VisitTypeOfExpression(TypeOfExpression typeOfExpression);
		void VisitTypeReferenceExpression(TypeReferenceExpression typeReferenceExpression);
		void VisitUnaryOperatorExpression(UnaryOperatorExpression unaryOperatorExpression);
		void VisitUncheckedExpression(UncheckedExpression uncheckedExpression);
		void VisitEmptyExpression (EmptyExpression emptyExpression);
		
		void VisitQueryExpression(QueryExpression queryExpression);
		void VisitQueryContinuationClause(QueryContinuationClause queryContinuationClause);
		void VisitQueryFromClause(QueryFromClause queryFromClause);
		void VisitQueryLetClause(QueryLetClause queryLetClause);
		void VisitQueryWhereClause(QueryWhereClause queryWhereClause);
		void VisitQueryJoinClause(QueryJoinClause queryJoinClause);
		void VisitQueryOrderClause(QueryOrderClause queryOrderClause);
		void VisitQueryOrdering(QueryOrdering queryOrdering);
		void VisitQuerySelectClause(QuerySelectClause querySelectClause);
		void VisitQueryGroupClause(QueryGroupClause queryGroupClause);
		
		void VisitAttribute(Attribute attribute);
		void VisitAttributeSection(AttributeSection attributeSection);
		void VisitDelegateDeclaration(DelegateDeclaration delegateDeclaration);
		void VisitNamespaceDeclaration(NamespaceDeclaration namespaceDeclaration);
		void VisitTypeDeclaration(TypeDeclaration typeDeclaration);
		void VisitUsingAliasDeclaration(UsingAliasDeclaration usingAliasDeclaration);
		void VisitUsingDeclaration(UsingDeclaration usingDeclaration);
		void VisitExternAliasDeclaration(ExternAliasDeclaration externAliasDeclaration);
		
		void VisitBlockStatement(BlockStatement blockStatement);
		void VisitBreakStatement(BreakStatement breakStatement);
		void VisitCheckedStatement(CheckedStatement checkedStatement);
		void VisitContinueStatement(ContinueStatement continueStatement);
		void VisitDoWhileStatement(DoWhileStatement doWhileStatement);
		void VisitEmptyStatement(EmptyStatement emptyStatement);
		void VisitExpressionStatement(ExpressionStatement expressionStatement);
		void VisitFixedStatement(FixedStatement fixedStatement);
		void VisitForeachStatement(ForeachStatement foreachStatement);
		void VisitForStatement(ForStatement forStatement);
		void VisitGotoCaseStatement(GotoCaseStatement gotoCaseStatement);
		void VisitGotoDefaultStatement(GotoDefaultStatement gotoDefaultStatement);
		void VisitGotoStatement(GotoStatement gotoStatement);
		void VisitIfElseStatement(IfElseStatement ifElseStatement);
		void VisitLabelStatement(LabelStatement labelStatement);
		void VisitLockStatement(LockStatement lockStatement);
		void VisitReturnStatement(ReturnStatement returnStatement);
		void VisitSwitchStatement(SwitchStatement switchStatement);
		void VisitSwitchSection(SwitchSection switchSection);
		void VisitCaseLabel(CaseLabel caseLabel);
		void VisitThrowStatement(ThrowStatement throwStatement);
		void VisitTryCatchStatement(TryCatchStatement tryCatchStatement);
		void VisitCatchClause(CatchClause catchClause);
		void VisitUncheckedStatement(UncheckedStatement uncheckedStatement);
		void VisitUnsafeStatement(UnsafeStatement unsafeStatement);
		void VisitUsingStatement(UsingStatement usingStatement);
		void VisitVariableDeclarationStatement(VariableDeclarationStatement variableDeclarationStatement);
		void VisitWhileStatement(WhileStatement whileStatement);
		void VisitYieldBreakStatement(YieldBreakStatement yieldBreakStatement);
		void VisitYieldReturnStatement(YieldReturnStatement yieldReturnStatement);
		
		void VisitAccessor(Accessor accessor);
		void VisitConstructorDeclaration(ConstructorDeclaration constructorDeclaration);
		void VisitConstructorInitializer(ConstructorInitializer constructorInitializer);
		void VisitDestructorDeclaration(DestructorDeclaration destructorDeclaration);
		void VisitEnumMemberDeclaration(EnumMemberDeclaration enumMemberDeclaration);
		void VisitEventDeclaration(EventDeclaration eventDeclaration);
		void VisitCustomEventDeclaration(CustomEventDeclaration customEventDeclaration);
		void VisitFieldDeclaration(FieldDeclaration fieldDeclaration);
		void VisitIndexerDeclaration(IndexerDeclaration indexerDeclaration);
		void VisitMethodDeclaration(MethodDeclaration methodDeclaration);
		void VisitOperatorDeclaration(OperatorDeclaration operatorDeclaration);
		void VisitParameterDeclaration(ParameterDeclaration parameterDeclaration);
		void VisitPropertyDeclaration(PropertyDeclaration propertyDeclaration);
		void VisitVariableInitializer(VariableInitializer variableInitializer);
		void VisitFixedFieldDeclaration(FixedFieldDeclaration fixedFieldDeclaration);
		void VisitFixedVariableInitializer(FixedVariableInitializer fixedVariableInitializer);
		
		void VisitCompilationUnit(CompilationUnit compilationUnit);
		void VisitSimpleType(SimpleType simpleType);
		void VisitMemberType(MemberType memberType);
		void VisitComposedType(ComposedType composedType);
		void VisitArraySpecifier(ArraySpecifier arraySpecifier);
		void VisitPrimitiveType(PrimitiveType primitiveType);
		
		void VisitComment(Comment comment);
		void VisitNewLine(NewLineNode newLineNode);
		void VisitWhitespace(WhitespaceNode whitespaceNode);
		void VisitText(TextNode textNode);
		void VisitPreProcessorDirective(PreProcessorDirective preProcessorDirective);
		void VisitDocumentationReference(DocumentationReference documentationReference);
		
		void VisitTypeParameterDeclaration(TypeParameterDeclaration typeParameterDeclaration);
		void VisitConstraint(Constraint constraint);
		void VisitCSharpTokenNode(CSharpTokenNode cSharpTokenNode);
		void VisitIdentifier(Identifier identifier);
		
		void VisitPatternPlaceholder(AstNode placeholder, PatternMatching.Pattern pattern);
	}
	
	/// <summary>
	/// AST visitor.
	/// </summary>
	public interface IAstVisitor<out S>
	{
		S VisitAnonymousMethodExpression(AnonymousMethodExpression anonymousMethodExpression);
		S VisitUndocumentedExpression(UndocumentedExpression undocumentedExpression);
		S VisitArrayCreateExpression(ArrayCreateExpression arrayCreateExpression);
		S VisitArrayInitializerExpression(ArrayInitializerExpression arrayInitializerExpression);
		S VisitAsExpression(AsExpression asExpression);
		S VisitAssignmentExpression(AssignmentExpression assignmentExpression);
		S VisitBaseReferenceExpression(BaseReferenceExpression baseReferenceExpression);
		S VisitBinaryOperatorExpression(BinaryOperatorExpression binaryOperatorExpression);
		S VisitCastExpression(CastExpression castExpression);
		S VisitCheckedExpression(CheckedExpression checkedExpression);
		S VisitConditionalExpression(ConditionalExpression conditionalExpression);
		S VisitDefaultValueExpression(DefaultValueExpression defaultValueExpression);
		S VisitDirectionExpression(DirectionExpression directionExpression);
		S VisitIdentifierExpression(IdentifierExpression identifierExpression);
		S VisitIndexerExpression(IndexerExpression indexerExpression);
		S VisitInvocationExpression(InvocationExpression invocationExpression);
		S VisitIsExpression(IsExpression isExpression);
		S VisitLambdaExpression(LambdaExpression lambdaExpression);
		S VisitMemberReferenceExpression(MemberReferenceExpression memberReferenceExpression);
		S VisitNamedArgumentExpression(NamedArgumentExpression namedArgumentExpression);
		S VisitNamedExpression(NamedExpression namedExpression);
		S VisitNullReferenceExpression(NullReferenceExpression nullReferenceExpression);
		S VisitObjectCreateExpression(ObjectCreateExpression objectCreateExpression);
		S VisitAnonymousTypeCreateExpression(AnonymousTypeCreateExpression anonymousTypeCreateExpression);
		S VisitParenthesizedExpression(ParenthesizedExpression parenthesizedExpression);
		S VisitPointerReferenceExpression(PointerReferenceExpression pointerReferenceExpression);
		S VisitPrimitiveExpression(PrimitiveExpression primitiveExpression);
		S VisitSizeOfExpression(SizeOfExpression sizeOfExpression);
		S VisitStackAllocExpression(StackAllocExpression stackAllocExpression);
		S VisitThisReferenceExpression(ThisReferenceExpression thisReferenceExpression);
		S VisitTypeOfExpression(TypeOfExpression typeOfExpression);
		S VisitTypeReferenceExpression(TypeReferenceExpression typeReferenceExpression);
		S VisitUnaryOperatorExpression(UnaryOperatorExpression unaryOperatorExpression);
		S VisitUncheckedExpression(UncheckedExpression uncheckedExpression);
		S VisitEmptyExpression (EmptyExpression emptyExpression);
		
		S VisitQueryExpression(QueryExpression queryExpression);
		S VisitQueryContinuationClause(QueryContinuationClause queryContinuationClause);
		S VisitQueryFromClause(QueryFromClause queryFromClause);
		S VisitQueryLetClause(QueryLetClause queryLetClause);
		S VisitQueryWhereClause(QueryWhereClause queryWhereClause);
		S VisitQueryJoinClause(QueryJoinClause queryJoinClause);
		S VisitQueryOrderClause(QueryOrderClause queryOrderClause);
		S VisitQueryOrdering(QueryOrdering queryOrdering);
		S VisitQuerySelectClause(QuerySelectClause querySelectClause);
		S VisitQueryGroupClause(QueryGroupClause queryGroupClause);
		
		S VisitAttribute(Attribute attribute);
		S VisitAttributeSection(AttributeSection attributeSection);
		S VisitDelegateDeclaration(DelegateDeclaration delegateDeclaration);
		S VisitNamespaceDeclaration(NamespaceDeclaration namespaceDeclaration);
		S VisitTypeDeclaration(TypeDeclaration typeDeclaration);
		S VisitUsingAliasDeclaration(UsingAliasDeclaration usingAliasDeclaration);
		S VisitUsingDeclaration(UsingDeclaration usingDeclaration);
		S VisitExternAliasDeclaration(ExternAliasDeclaration externAliasDeclaration);
		
		S VisitBlockStatement(BlockStatement blockStatement);
		S VisitBreakStatement(BreakStatement breakStatement);
		S VisitCheckedStatement(CheckedStatement checkedStatement);
		S VisitContinueStatement(ContinueStatement continueStatement);
		S VisitDoWhileStatement(DoWhileStatement doWhileStatement);
		S VisitEmptyStatement(EmptyStatement emptyStatement);
		S VisitExpressionStatement(ExpressionStatement expressionStatement);
		S VisitFixedStatement(FixedStatement fixedStatement);
		S VisitForeachStatement(ForeachStatement foreachStatement);
		S VisitForStatement(ForStatement forStatement);
		S VisitGotoCaseStatement(GotoCaseStatement gotoCaseStatement);
		S VisitGotoDefaultStatement(GotoDefaultStatement gotoDefaultStatement);
		S VisitGotoStatement(GotoStatement gotoStatement);
		S VisitIfElseStatement(IfElseStatement ifElseStatement);
		S VisitLabelStatement(LabelStatement labelStatement);
		S VisitLockStatement(LockStatement lockStatement);
		S VisitReturnStatement(ReturnStatement returnStatement);
		S VisitSwitchStatement(SwitchStatement switchStatement);
		S VisitSwitchSection(SwitchSection switchSection);
		S VisitCaseLabel(CaseLabel caseLabel);
		S VisitThrowStatement(ThrowStatement throwStatement);
		S VisitTryCatchStatement(TryCatchStatement tryCatchStatement);
		S VisitCatchClause(CatchClause catchClause);
		S VisitUncheckedStatement(UncheckedStatement uncheckedStatement);
		S VisitUnsafeStatement(UnsafeStatement unsafeStatement);
		S VisitUsingStatement(UsingStatement usingStatement);
		S VisitVariableDeclarationStatement(VariableDeclarationStatement variableDeclarationStatement);
		S VisitWhileStatement(WhileStatement whileStatement);
		S VisitYieldBreakStatement(YieldBreakStatement yieldBreakStatement);
		S VisitYieldReturnStatement(YieldReturnStatement yieldReturnStatement);
		
		S VisitAccessor(Accessor accessor);
		S VisitConstructorDeclaration(ConstructorDeclaration constructorDeclaration);
		S VisitConstructorInitializer(ConstructorInitializer constructorInitializer);
		S VisitDestructorDeclaration(DestructorDeclaration destructorDeclaration);
		S VisitEnumMemberDeclaration(EnumMemberDeclaration enumMemberDeclaration);
		S VisitEventDeclaration(EventDeclaration eventDeclaration);
		S VisitCustomEventDeclaration(CustomEventDeclaration customEventDeclaration);
		S VisitFieldDeclaration(FieldDeclaration fieldDeclaration);
		S VisitIndexerDeclaration(IndexerDeclaration indexerDeclaration);
		S VisitMethodDeclaration(MethodDeclaration methodDeclaration);
		S VisitOperatorDeclaration(OperatorDeclaration operatorDeclaration);
		S VisitParameterDeclaration(ParameterDeclaration parameterDeclaration);
		S VisitPropertyDeclaration(PropertyDeclaration propertyDeclaration);
		S VisitVariableInitializer(VariableInitializer variableInitializer);
		S VisitFixedFieldDeclaration(FixedFieldDeclaration fixedFieldDeclaration);
		S VisitFixedVariableInitializer(FixedVariableInitializer fixedVariableInitializer);
		
		S VisitCompilationUnit(CompilationUnit compilationUnit);
		S VisitSimpleType(SimpleType simpleType);
		S VisitMemberType(MemberType memberType);
		S VisitComposedType(ComposedType composedType);
		S VisitArraySpecifier(ArraySpecifier arraySpecifier);
		S VisitPrimitiveType(PrimitiveType primitiveType);
		
		S VisitComment(Comment comment);
		S VisitWhitespace(WhitespaceNode whitespaceNode);
		S VisitText(TextNode textNode);
		S VisitNewLine(NewLineNode newLineNode);
		S VisitPreProcessorDirective(PreProcessorDirective preProcessorDirective);
		S VisitDocumentationReference(DocumentationReference documentationReference);
		
		S VisitTypeParameterDeclaration(TypeParameterDeclaration typeParameterDeclaration);
		S VisitConstraint(Constraint constraint);
		S VisitCSharpTokenNode(CSharpTokenNode cSharpTokenNode);
		S VisitIdentifier(Identifier identifier);
		
		S VisitPatternPlaceholder(AstNode placeholder, PatternMatching.Pattern pattern);
	}
	
	/// <summary>
	/// AST visitor.
	/// </summary>
	public interface IAstVisitor<in T, out S>
	{
		S VisitAnonymousMethodExpression(AnonymousMethodExpression anonymousMethodExpression, T data);
		S VisitUndocumentedExpression(UndocumentedExpression undocumentedExpression, T data);
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
		S VisitNamedExpression(NamedExpression namedExpression, T data);
		S VisitNullReferenceExpression(NullReferenceExpression nullReferenceExpression, T data);
		S VisitObjectCreateExpression(ObjectCreateExpression objectCreateExpression, T data);
		S VisitAnonymousTypeCreateExpression(AnonymousTypeCreateExpression anonymousTypeCreateExpression, T data);
		S VisitParenthesizedExpression(ParenthesizedExpression parenthesizedExpression, T data);
		S VisitPointerReferenceExpression(PointerReferenceExpression pointerReferenceExpression, T data);
		S VisitPrimitiveExpression(PrimitiveExpression primitiveExpression, T data);
		S VisitSizeOfExpression(SizeOfExpression sizeOfExpression, T data);
		S VisitStackAllocExpression(StackAllocExpression stackAllocExpression, T data);
		S VisitThisReferenceExpression(ThisReferenceExpression thisReferenceExpression, T data);
		S VisitTypeOfExpression(TypeOfExpression typeOfExpression, T data);
		S VisitTypeReferenceExpression(TypeReferenceExpression typeReferenceExpression, T data);
		S VisitUnaryOperatorExpression(UnaryOperatorExpression unaryOperatorExpression, T data);
		S VisitUncheckedExpression(UncheckedExpression uncheckedExpression, T data);
		S VisitEmptyExpression (EmptyExpression emptyExpression, T data);
		
		S VisitQueryExpression(QueryExpression queryExpression, T data);
		S VisitQueryContinuationClause(QueryContinuationClause queryContinuationClause, T data);
		S VisitQueryFromClause(QueryFromClause queryFromClause, T data);
		S VisitQueryLetClause(QueryLetClause queryLetClause, T data);
		S VisitQueryWhereClause(QueryWhereClause queryWhereClause, T data);
		S VisitQueryJoinClause(QueryJoinClause queryJoinClause, T data);
		S VisitQueryOrderClause(QueryOrderClause queryOrderClause, T data);
		S VisitQueryOrdering(QueryOrdering queryOrdering, T data);
		S VisitQuerySelectClause(QuerySelectClause querySelectClause, T data);
		S VisitQueryGroupClause(QueryGroupClause queryGroupClause, T data);
		
		S VisitAttribute(Attribute attribute, T data);
		S VisitAttributeSection(AttributeSection attributeSection, T data);
		S VisitDelegateDeclaration(DelegateDeclaration delegateDeclaration, T data);
		S VisitNamespaceDeclaration(NamespaceDeclaration namespaceDeclaration, T data);
		S VisitTypeDeclaration(TypeDeclaration typeDeclaration, T data);
		S VisitUsingAliasDeclaration(UsingAliasDeclaration usingAliasDeclaration, T data);
		S VisitUsingDeclaration(UsingDeclaration usingDeclaration, T data);
		S VisitExternAliasDeclaration(ExternAliasDeclaration externAliasDeclaration, T data);
		
		S VisitBlockStatement(BlockStatement blockStatement, T data);
		S VisitBreakStatement(BreakStatement breakStatement, T data);
		S VisitCheckedStatement(CheckedStatement checkedStatement, T data);
		S VisitContinueStatement(ContinueStatement continueStatement, T data);
		S VisitDoWhileStatement(DoWhileStatement doWhileStatement, T data);
		S VisitEmptyStatement(EmptyStatement emptyStatement, T data);
		S VisitExpressionStatement(ExpressionStatement expressionStatement, T data);
		S VisitFixedStatement(FixedStatement fixedStatement, T data);
		S VisitForeachStatement(ForeachStatement foreachStatement, T data);
		S VisitForStatement(ForStatement forStatement, T data);
		S VisitGotoCaseStatement(GotoCaseStatement gotoCaseStatement, T data);
		S VisitGotoDefaultStatement(GotoDefaultStatement gotoDefaultStatement, T data);
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
		S VisitYieldBreakStatement(YieldBreakStatement yieldBreakStatement, T data);
		S VisitYieldReturnStatement(YieldReturnStatement yieldReturnStatement, T data);
		
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
		S VisitFixedFieldDeclaration(FixedFieldDeclaration fixedFieldDeclaration, T data);
		S VisitFixedVariableInitializer(FixedVariableInitializer fixedVariableInitializer, T data);
		
		S VisitCompilationUnit(CompilationUnit compilationUnit, T data);
		S VisitSimpleType(SimpleType simpleType, T data);
		S VisitMemberType(MemberType memberType, T data);
		S VisitComposedType(ComposedType composedType, T data);
		S VisitArraySpecifier(ArraySpecifier arraySpecifier, T data);
		S VisitPrimitiveType(PrimitiveType primitiveType, T data);
		
		S VisitComment(Comment comment, T data);
		S VisitNewLine(NewLineNode newLineNode, T data);
		S VisitWhitespace(WhitespaceNode whitespaceNode, T data);
		S VisitText(TextNode textNode, T data);
		S VisitPreProcessorDirective(PreProcessorDirective preProcessorDirective, T data);
		S VisitDocumentationReference(DocumentationReference documentationReference, T data);
		
		S VisitTypeParameterDeclaration(TypeParameterDeclaration typeParameterDeclaration, T data);
		S VisitConstraint(Constraint constraint, T data);
		S VisitCSharpTokenNode(CSharpTokenNode cSharpTokenNode, T data);
		S VisitIdentifier(Identifier identifier, T data);
		
		S VisitPatternPlaceholder(AstNode placeholder, PatternMatching.Pattern pattern, T data);
	}
}
