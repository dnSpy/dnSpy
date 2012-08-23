// 
// ObservableAstVisitor.cs
//
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2011 Novell, Inc (http://www.novell.com)
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;

namespace ICSharpCode.NRefactory.CSharp
{
	public class ObservableAstVisitor : IAstVisitor
	{
		void Visit<T>(Action<T> enter, Action<T> leave, T node) where T : AstNode
		{
			if (enter != null)
				enter(node);
			AstNode next;
			for (var child = node.FirstChild; child != null; child = next) {
				// Store next to allow the loop to continue
				// if the visitor removes/replaces children.
				next = child.NextSibling;
				child.AcceptVisitor (this);
			}
			if (leave != null)
				leave(node);
		}
		
		public event Action<SyntaxTree> EnterSyntaxTree, LeaveSyntaxTree;
		
		void IAstVisitor.VisitSyntaxTree(SyntaxTree unit)
		{
			Visit(EnterSyntaxTree, LeaveSyntaxTree, unit);
		}
		
		public event Action<Comment> EnterComment, LeaveComment;
		
		void IAstVisitor.VisitComment(Comment comment)
		{
			Visit(EnterComment, LeaveComment, comment);
		}
		
		public event Action<NewLineNode> EnterNewLine, LeaveNewLine;
		
		void IAstVisitor.VisitNewLine(NewLineNode newLineNode)
		{
			Visit(EnterNewLine, LeaveNewLine, newLineNode);
		}
		
		public event Action<WhitespaceNode> EnterWhitespace, LeaveWhitespace;
		
		void IAstVisitor.VisitWhitespace(WhitespaceNode whitespace)
		{
			Visit(EnterWhitespace, LeaveWhitespace, whitespace);
		}
		
		public event Action<TextNode> EnterText, LeaveText;
		
		void IAstVisitor.VisitText(TextNode textNode)
		{
			Visit(EnterText, LeaveText, textNode);
		}
		
		public event Action<PreProcessorDirective> EnterPreProcessorDirective, LeavePreProcessorDirective;
		void IAstVisitor.VisitPreProcessorDirective(PreProcessorDirective preProcessorDirective)
		{
			Visit(EnterPreProcessorDirective, LeavePreProcessorDirective, preProcessorDirective);
		}
		
		public event Action<DocumentationReference> EnterDocumentationReference, LeaveDocumentationReference;
		
		void IAstVisitor.VisitDocumentationReference(DocumentationReference documentationReference)
		{
			Visit(EnterDocumentationReference, LeaveDocumentationReference, documentationReference);
		}
		
		public event Action<Identifier> EnterIdentifier, LeaveIdentifier;
		
		void IAstVisitor.VisitIdentifier(Identifier identifier)
		{
			Visit(EnterIdentifier, LeaveIdentifier, identifier);
		}
		
		public event Action<CSharpTokenNode> EnterCSharpTokenNode, LeaveCSharpTokenNode;
		
		void IAstVisitor.VisitCSharpTokenNode(CSharpTokenNode token)
		{
			Visit(EnterCSharpTokenNode, LeaveCSharpTokenNode, token);
		}
		
		public event Action<PrimitiveType> EnterPrimitiveType, LeavePrimitiveType;
		
		void IAstVisitor.VisitPrimitiveType(PrimitiveType primitiveType)
		{
			Visit(EnterPrimitiveType, LeavePrimitiveType, primitiveType);
		}
		
		public event Action<ComposedType> EnterComposedType, LeaveComposedType;
		
		void IAstVisitor.VisitComposedType(ComposedType composedType)
		{
			Visit(EnterComposedType, LeaveComposedType, composedType);
		}
		
		public event Action<SimpleType> EnterSimpleType, LeaveSimpleType;
		
		void IAstVisitor.VisitSimpleType(SimpleType simpleType)
		{
			Visit(EnterSimpleType, LeaveSimpleType, simpleType);
		}
		
		public event Action<MemberType> EnterMemberType, LeaveMemberType;
		
		void IAstVisitor.VisitMemberType(MemberType memberType)
		{
			Visit(EnterMemberType, LeaveMemberType, memberType);
		}
		
		public event Action<Attribute> EnterAttribute, LeaveAttribute;
		
		void IAstVisitor.VisitAttribute(Attribute attribute)
		{
			Visit(EnterAttribute, LeaveAttribute, attribute);
		}
		
		public event Action<AttributeSection> EnterAttributeSection, LeaveAttributeSection;
		
		void IAstVisitor.VisitAttributeSection(AttributeSection attributeSection)
		{
			Visit(EnterAttributeSection, LeaveAttributeSection, attributeSection);
		}
		
		public event Action<DelegateDeclaration> EnterDelegateDeclaration, LeaveDelegateDeclaration;
		
		void IAstVisitor.VisitDelegateDeclaration(DelegateDeclaration delegateDeclaration)
		{
			Visit(EnterDelegateDeclaration, LeaveDelegateDeclaration, delegateDeclaration);
		}
		
		public event Action<NamespaceDeclaration> EnterNamespaceDeclaration, LeaveNamespaceDeclaration;
		
		void IAstVisitor.VisitNamespaceDeclaration(NamespaceDeclaration namespaceDeclaration)
		{
			Visit(EnterNamespaceDeclaration, LeaveNamespaceDeclaration, namespaceDeclaration);
		}
		
		public event Action<TypeDeclaration> EnterTypeDeclaration, LeaveTypeDeclaration;
		
		void IAstVisitor.VisitTypeDeclaration(TypeDeclaration typeDeclaration)
		{
			Visit(EnterTypeDeclaration, LeaveTypeDeclaration, typeDeclaration);
		}
		
		public event Action<TypeParameterDeclaration> EnterTypeParameterDeclaration, LeaveTypeParameterDeclaration;
		
		void IAstVisitor.VisitTypeParameterDeclaration(TypeParameterDeclaration typeParameterDeclaration)
		{
			Visit(EnterTypeParameterDeclaration, LeaveTypeParameterDeclaration, typeParameterDeclaration);
		}
		
		public event Action<EnumMemberDeclaration> EnterEnumMemberDeclaration, LeaveEnumMemberDeclaration;
		
		void IAstVisitor.VisitEnumMemberDeclaration(EnumMemberDeclaration enumMemberDeclaration)
		{
			Visit(EnterEnumMemberDeclaration, LeaveEnumMemberDeclaration, enumMemberDeclaration);
		}
		
		public event Action<UsingDeclaration> EnterUsingDeclaration, LeaveUsingDeclaration;
		
		void IAstVisitor.VisitUsingDeclaration(UsingDeclaration usingDeclaration)
		{
			Visit(EnterUsingDeclaration, LeaveUsingDeclaration, usingDeclaration);
		}
		
		public event Action<UsingAliasDeclaration> EnterUsingAliasDeclaration, LeaveUsingAliasDeclaration;
		
		void IAstVisitor.VisitUsingAliasDeclaration(UsingAliasDeclaration usingDeclaration)
		{
			Visit(EnterUsingAliasDeclaration, LeaveUsingAliasDeclaration, usingDeclaration);
		}
		
		public event Action<ExternAliasDeclaration> EnterExternAliasDeclaration, LeaveExternAliasDeclaration;
		
		void IAstVisitor.VisitExternAliasDeclaration(ExternAliasDeclaration externAliasDeclaration)
		{
			Visit(EnterExternAliasDeclaration, LeaveExternAliasDeclaration, externAliasDeclaration);
		}
		
		public event Action<ConstructorDeclaration> EnterConstructorDeclaration, LeaveConstructorDeclaration;
		
		void IAstVisitor.VisitConstructorDeclaration(ConstructorDeclaration constructorDeclaration)
		{
			Visit(EnterConstructorDeclaration, LeaveConstructorDeclaration, constructorDeclaration);
		}
		
		public event Action<ConstructorInitializer> EnterConstructorInitializer, LeaveConstructorInitializer;
		
		void IAstVisitor.VisitConstructorInitializer(ConstructorInitializer constructorInitializer)
		{
			Visit(EnterConstructorInitializer, LeaveConstructorInitializer, constructorInitializer);
		}
		
		public event Action<DestructorDeclaration> EnterDestructorDeclaration, LeaveDestructorDeclaration;
		
		void IAstVisitor.VisitDestructorDeclaration(DestructorDeclaration destructorDeclaration)
		{
			Visit(EnterDestructorDeclaration, LeaveDestructorDeclaration, destructorDeclaration);
		}
		
		public event Action<EventDeclaration> EnterEventDeclaration, LeaveEventDeclaration;
		
		void IAstVisitor.VisitEventDeclaration(EventDeclaration eventDeclaration)
		{
			Visit(EnterEventDeclaration, LeaveEventDeclaration, eventDeclaration);
		}
		
		public event Action<CustomEventDeclaration> EnterCustomEventDeclaration, LeaveCustomEventDeclaration;
		
		void IAstVisitor.VisitCustomEventDeclaration(CustomEventDeclaration eventDeclaration)
		{
			Visit(EnterCustomEventDeclaration, LeaveCustomEventDeclaration, eventDeclaration);
		}
		
		public event Action<FieldDeclaration> EnterFieldDeclaration, LeaveFieldDeclaration;
		
		void IAstVisitor.VisitFieldDeclaration(FieldDeclaration fieldDeclaration)
		{
			Visit(EnterFieldDeclaration, LeaveFieldDeclaration, fieldDeclaration);
		}
		
		public event Action<FixedFieldDeclaration> EnterFixedFieldDeclaration, LeaveFixedFieldDeclaration;
		
		void IAstVisitor.VisitFixedFieldDeclaration(FixedFieldDeclaration fixedFieldDeclaration)
		{
			Visit(EnterFixedFieldDeclaration, LeaveFixedFieldDeclaration, fixedFieldDeclaration);
		}
		
		public event Action<FixedVariableInitializer> EnterFixedVariableInitializer, LeaveFixedVariableInitializer;
		
		void IAstVisitor.VisitFixedVariableInitializer(FixedVariableInitializer fixedVariableInitializer)
		{
			Visit(EnterFixedVariableInitializer, LeaveFixedVariableInitializer, fixedVariableInitializer);
		}
		
		public event Action<IndexerDeclaration> EnterIndexerDeclaration, LeaveIndexerDeclaration;
		
		void IAstVisitor.VisitIndexerDeclaration(IndexerDeclaration indexerDeclaration)
		{
			Visit(EnterIndexerDeclaration, LeaveIndexerDeclaration, indexerDeclaration);
		}
		
		public event Action<MethodDeclaration> EnterMethodDeclaration, LeaveMethodDeclaration;
		
		void IAstVisitor.VisitMethodDeclaration(MethodDeclaration methodDeclaration)
		{
			Visit(EnterMethodDeclaration, LeaveMethodDeclaration, methodDeclaration);
		}
		
		public event Action<OperatorDeclaration> EnterOperatorDeclaration, LeaveOperatorDeclaration;
		
		void IAstVisitor.VisitOperatorDeclaration(OperatorDeclaration operatorDeclaration)
		{
			Visit(EnterOperatorDeclaration, LeaveOperatorDeclaration, operatorDeclaration);
		}
		
		public event Action<PropertyDeclaration> EnterPropertyDeclaration, LeavePropertyDeclaration;
		
		void IAstVisitor.VisitPropertyDeclaration(PropertyDeclaration propertyDeclaration)
		{
			Visit(EnterPropertyDeclaration, LeavePropertyDeclaration, propertyDeclaration);
		}
		
		public event Action<Accessor> EnterAccessor, LeaveAccessor;
		
		void IAstVisitor.VisitAccessor(Accessor accessor)
		{
			Visit(EnterAccessor, LeaveAccessor, accessor);
		}
		
		public event Action<VariableInitializer> EnterVariableInitializer, LeaveVariableInitializer;
		
		void IAstVisitor.VisitVariableInitializer(VariableInitializer variableInitializer)
		{
			Visit(EnterVariableInitializer, LeaveVariableInitializer, variableInitializer);
		}
		
		public event Action<ParameterDeclaration> EnterParameterDeclaration, LeaveParameterDeclaration;
		
		void IAstVisitor.VisitParameterDeclaration(ParameterDeclaration parameterDeclaration)
		{
			Visit(EnterParameterDeclaration, LeaveParameterDeclaration, parameterDeclaration);
		}
		
		public event Action<Constraint> EnterConstraint, LeaveConstraint;
		
		void IAstVisitor.VisitConstraint(Constraint constraint)
		{
			Visit(EnterConstraint, LeaveConstraint, constraint);
		}
		
		public event Action<BlockStatement> EnterBlockStatement, LeaveBlockStatement;
		
		void IAstVisitor.VisitBlockStatement(BlockStatement blockStatement)
		{
			Visit(EnterBlockStatement, LeaveBlockStatement, blockStatement);
		}
		
		public event Action<ExpressionStatement> EnterExpressionStatement, LeaveExpressionStatement;
		
		void IAstVisitor.VisitExpressionStatement(ExpressionStatement expressionStatement)
		{
			Visit(EnterExpressionStatement, LeaveExpressionStatement, expressionStatement);
		}
		
		public event Action<BreakStatement> EnterBreakStatement, LeaveBreakStatement;
		
		void IAstVisitor.VisitBreakStatement(BreakStatement breakStatement)
		{
			Visit(EnterBreakStatement, LeaveBreakStatement, breakStatement);
		}
		
		public event Action<CheckedStatement> EnterCheckedStatement, LeaveCheckedStatement;
		
		void IAstVisitor.VisitCheckedStatement(CheckedStatement checkedStatement)
		{
			Visit(EnterCheckedStatement, LeaveCheckedStatement, checkedStatement);
		}
		
		public event Action<ContinueStatement> EnterContinueStatement, LeaveContinueStatement;
		
		void IAstVisitor.VisitContinueStatement(ContinueStatement continueStatement)
		{
			Visit(EnterContinueStatement, LeaveContinueStatement, continueStatement);
		}
		
		public event Action<DoWhileStatement> EnterDoWhileStatement, LeaveDoWhileStatement;
		
		void IAstVisitor.VisitDoWhileStatement(DoWhileStatement doWhileStatement)
		{
			Visit(EnterDoWhileStatement, LeaveDoWhileStatement, doWhileStatement);
		}
		
		public event Action<EmptyStatement> EnterEmptyStatement, LeaveEmptyStatement;
		
		void IAstVisitor.VisitEmptyStatement(EmptyStatement emptyStatement)
		{
			Visit(EnterEmptyStatement, LeaveEmptyStatement, emptyStatement);
		}
		
		public event Action<FixedStatement> EnterFixedStatement, LeaveFixedStatement;
		
		void IAstVisitor.VisitFixedStatement(FixedStatement fixedStatement)
		{
			Visit(EnterFixedStatement, LeaveFixedStatement, fixedStatement);
		}
		
		public event Action<ForeachStatement> EnterForeachStatement, LeaveForeachStatement;
		
		void IAstVisitor.VisitForeachStatement(ForeachStatement foreachStatement)
		{
			Visit(EnterForeachStatement, LeaveForeachStatement, foreachStatement);
		}
		
		public event Action<ForStatement> EnterForStatement, LeaveForStatement;
		
		void IAstVisitor.VisitForStatement(ForStatement forStatement)
		{
			Visit(EnterForStatement, LeaveForStatement, forStatement);
		}
		
		public event Action<GotoCaseStatement> EnterGotoCaseStatement, LeaveGotoCaseStatement;
		
		void IAstVisitor.VisitGotoCaseStatement(GotoCaseStatement gotoCaseStatement)
		{
			Visit(EnterGotoCaseStatement, LeaveGotoCaseStatement, gotoCaseStatement);
		}
		
		public event Action<GotoDefaultStatement> EnterGotoDefaultStatement, LeaveGotoDefaultStatement;
		
		void IAstVisitor.VisitGotoDefaultStatement(GotoDefaultStatement gotoDefaultStatement)
		{
			Visit(EnterGotoDefaultStatement, LeaveGotoDefaultStatement, gotoDefaultStatement);
		}
		
		public event Action<GotoStatement> EnterGotoStatement, LeaveGotoStatement;
		
		void IAstVisitor.VisitGotoStatement(GotoStatement gotoStatement)
		{
			Visit(EnterGotoStatement, LeaveGotoStatement, gotoStatement);
		}
		
		public event Action<IfElseStatement> EnterIfElseStatement, LeaveIfElseStatement;
		
		void IAstVisitor.VisitIfElseStatement(IfElseStatement ifElseStatement)
		{
			Visit(EnterIfElseStatement, LeaveIfElseStatement, ifElseStatement);
		}
		
		public event Action<LabelStatement> EnterLabelStatement, LeaveLabelStatement;
		
		void IAstVisitor.VisitLabelStatement(LabelStatement labelStatement)
		{
			Visit(EnterLabelStatement, LeaveLabelStatement, labelStatement);
		}
		
		public event Action<LockStatement> EnterLockStatement, LeaveLockStatement;
		
		void IAstVisitor.VisitLockStatement(LockStatement lockStatement)
		{
			Visit(EnterLockStatement, LeaveLockStatement, lockStatement);
		}
		
		public event Action<ReturnStatement> EnterReturnStatement, LeaveReturnStatement;
		
		void IAstVisitor.VisitReturnStatement(ReturnStatement returnStatement)
		{
			Visit(EnterReturnStatement, LeaveReturnStatement, returnStatement);
		}
		
		public event Action<SwitchStatement> EnterSwitchStatement, LeaveSwitchStatement;
		
		void IAstVisitor.VisitSwitchStatement(SwitchStatement switchStatement)
		{
			Visit(EnterSwitchStatement, LeaveSwitchStatement, switchStatement);
		}
		
		public event Action<SwitchSection> EnterSwitchSection, LeaveSwitchSection;
		
		void IAstVisitor.VisitSwitchSection(SwitchSection switchSection)
		{
			Visit(EnterSwitchSection, LeaveSwitchSection, switchSection);
		}
		
		public event Action<CaseLabel> EnterCaseLabel, LeaveCaseLabel;
		
		void IAstVisitor.VisitCaseLabel(CaseLabel caseLabel)
		{
			Visit(EnterCaseLabel, LeaveCaseLabel, caseLabel);
		}
		
		public event Action<ThrowStatement> EnterThrowStatement, LeaveThrowStatement;
		
		void IAstVisitor.VisitThrowStatement(ThrowStatement throwStatement)
		{
			Visit(EnterThrowStatement, LeaveThrowStatement, throwStatement);
		}
		
		public event Action<TryCatchStatement> EnterTryCatchStatement, LeaveTryCatchStatement;
		
		void IAstVisitor.VisitTryCatchStatement(TryCatchStatement tryCatchStatement)
		{
			Visit(EnterTryCatchStatement, LeaveTryCatchStatement, tryCatchStatement);
		}
		
		public event Action<CatchClause> EnterCatchClause, LeaveCatchClause;
		
		void IAstVisitor.VisitCatchClause(CatchClause catchClause)
		{
			Visit(EnterCatchClause, LeaveCatchClause, catchClause);
		}
		
		public event Action<UncheckedStatement> EnterUncheckedStatement, LeaveUncheckedStatement;
		
		void IAstVisitor.VisitUncheckedStatement(UncheckedStatement uncheckedStatement)
		{
			Visit(EnterUncheckedStatement, LeaveUncheckedStatement, uncheckedStatement);
		}
		
		public event Action<UnsafeStatement> EnterUnsafeStatement, LeaveUnsafeStatement;
		
		void IAstVisitor.VisitUnsafeStatement(UnsafeStatement unsafeStatement)
		{
			Visit(EnterUnsafeStatement, LeaveUnsafeStatement, unsafeStatement);
		}
		
		public event Action<UsingStatement> EnterUsingStatement, LeaveUsingStatement;
		
		void IAstVisitor.VisitUsingStatement(UsingStatement usingStatement)
		{
			Visit(EnterUsingStatement, LeaveUsingStatement, usingStatement);
		}
		
		public event Action<VariableDeclarationStatement> EnterVariableDeclarationStatement, LeaveVariableDeclarationStatement;
		
		void IAstVisitor.VisitVariableDeclarationStatement(VariableDeclarationStatement variableDeclarationStatement)
		{
			Visit(EnterVariableDeclarationStatement, LeaveVariableDeclarationStatement, variableDeclarationStatement);
		}
		
		public event Action<WhileStatement> EnterWhileStatement, LeaveWhileStatement;
		
		void IAstVisitor.VisitWhileStatement(WhileStatement whileStatement)
		{
			Visit(EnterWhileStatement, LeaveWhileStatement, whileStatement);
		}
		
		public event Action<YieldBreakStatement> EnterYieldBreakStatement, LeaveYieldBreakStatement;
		
		void IAstVisitor.VisitYieldBreakStatement(YieldBreakStatement yieldBreakStatement)
		{
			Visit(EnterYieldBreakStatement, LeaveYieldBreakStatement, yieldBreakStatement);
		}
		
		public event Action<YieldReturnStatement> EnterYieldReturnStatement, LeaveYieldReturnStatement;
		
		void IAstVisitor.VisitYieldReturnStatement(YieldReturnStatement yieldStatement)
		{
			Visit(EnterYieldReturnStatement, LeaveYieldReturnStatement, yieldStatement);
		}
		
		public event Action<AnonymousMethodExpression> EnterAnonymousMethodExpression, LeaveAnonymousMethodExpression;
		
		void IAstVisitor.VisitAnonymousMethodExpression(AnonymousMethodExpression anonymousMethodExpression)
		{
			Visit(EnterAnonymousMethodExpression, LeaveAnonymousMethodExpression, anonymousMethodExpression);
		}
		
		public event Action<LambdaExpression> EnterLambdaExpression, LeaveLambdaExpression;
		
		void IAstVisitor.VisitLambdaExpression(LambdaExpression lambdaExpression)
		{
			Visit(EnterLambdaExpression, LeaveLambdaExpression, lambdaExpression);
		}
		
		public event Action<AssignmentExpression> EnterAssignmentExpression, LeaveAssignmentExpression;
		
		void IAstVisitor.VisitAssignmentExpression(AssignmentExpression assignmentExpression)
		{
			Visit(EnterAssignmentExpression, LeaveAssignmentExpression, assignmentExpression);
		}
		
		public event Action<BaseReferenceExpression> EnterBaseReferenceExpression, LeaveBaseReferenceExpression;
		
		void IAstVisitor.VisitBaseReferenceExpression(BaseReferenceExpression baseReferenceExpression)
		{
			Visit(EnterBaseReferenceExpression, LeaveBaseReferenceExpression, baseReferenceExpression);
		}
		
		public event Action<BinaryOperatorExpression> EnterBinaryOperatorExpression, LeaveBinaryOperatorExpression;
		
		void IAstVisitor.VisitBinaryOperatorExpression(BinaryOperatorExpression binaryOperatorExpression)
		{
			Visit(EnterBinaryOperatorExpression, LeaveBinaryOperatorExpression, binaryOperatorExpression);
		}
		
		public event Action<CastExpression> EnterCastExpression, LeaveCastExpression;
		
		void IAstVisitor.VisitCastExpression(CastExpression castExpression)
		{
			Visit(EnterCastExpression, LeaveCastExpression, castExpression);
		}
		
		public event Action<CheckedExpression> EnterCheckedExpression, LeaveCheckedExpression;
		
		void IAstVisitor.VisitCheckedExpression(CheckedExpression checkedExpression)
		{
			Visit(EnterCheckedExpression, LeaveCheckedExpression, checkedExpression);
		}
		
		public event Action<ConditionalExpression> EnterConditionalExpression, LeaveConditionalExpression;
		
		void IAstVisitor.VisitConditionalExpression(ConditionalExpression conditionalExpression)
		{
			Visit(EnterConditionalExpression, LeaveConditionalExpression, conditionalExpression);
		}
		
		public event Action<IdentifierExpression> EnterIdentifierExpression, LeaveIdentifierExpression;
		
		void IAstVisitor.VisitIdentifierExpression(IdentifierExpression identifierExpression)
		{
			Visit(EnterIdentifierExpression, LeaveIdentifierExpression, identifierExpression);
		}
		
		public event Action<IndexerExpression> EnterIndexerExpression, LeaveIndexerExpression;
		
		void IAstVisitor.VisitIndexerExpression(IndexerExpression indexerExpression)
		{
			Visit(EnterIndexerExpression, LeaveIndexerExpression, indexerExpression);
		}
		
		public event Action<InvocationExpression> EnterInvocationExpression, LeaveInvocationExpression;
		
		void IAstVisitor.VisitInvocationExpression(InvocationExpression invocationExpression)
		{
			Visit(EnterInvocationExpression, LeaveInvocationExpression, invocationExpression);
		}
		
		public event Action<DirectionExpression> EnterDirectionExpression, LeaveDirectionExpression;
		
		void IAstVisitor.VisitDirectionExpression(DirectionExpression directionExpression)
		{
			Visit(EnterDirectionExpression, LeaveDirectionExpression, directionExpression);
		}
		
		public event Action<MemberReferenceExpression> EnterMemberReferenceExpression, LeaveMemberReferenceExpression;
		
		void IAstVisitor.VisitMemberReferenceExpression(MemberReferenceExpression memberReferenceExpression)
		{
			Visit(EnterMemberReferenceExpression, LeaveMemberReferenceExpression, memberReferenceExpression);
		}
		
		public event Action<NullReferenceExpression> EnterNullReferenceExpression, LeaveNullReferenceExpression;
		
		void IAstVisitor.VisitNullReferenceExpression(NullReferenceExpression nullReferenceExpression)
		{
			Visit(EnterNullReferenceExpression, LeaveNullReferenceExpression, nullReferenceExpression);
		}
		
		public event Action<ObjectCreateExpression> EnterObjectCreateExpression, LeaveObjectCreateExpression;
		
		void IAstVisitor.VisitObjectCreateExpression(ObjectCreateExpression objectCreateExpression)
		{
			Visit(EnterObjectCreateExpression, LeaveObjectCreateExpression, objectCreateExpression);
		}
		
		public event Action<AnonymousTypeCreateExpression> EnterAnonymousTypeCreateExpression, LeaveAnonymousTypeCreateExpression;
		
		void IAstVisitor.VisitAnonymousTypeCreateExpression(AnonymousTypeCreateExpression anonymousTypeCreateExpression)
		{
			Visit(EnterAnonymousTypeCreateExpression, LeaveAnonymousTypeCreateExpression, anonymousTypeCreateExpression);
		}
		
		public event Action<ArrayCreateExpression> EnterArrayCreateExpression, LeaveArrayCreateExpression;
		
		void IAstVisitor.VisitArrayCreateExpression(ArrayCreateExpression arraySCreateExpression)
		{
			Visit(EnterArrayCreateExpression, LeaveArrayCreateExpression, arraySCreateExpression);
		}
		
		public event Action<ParenthesizedExpression> EnterParenthesizedExpression, LeaveParenthesizedExpression;
		
		void IAstVisitor.VisitParenthesizedExpression(ParenthesizedExpression parenthesizedExpression)
		{
			Visit(EnterParenthesizedExpression, LeaveParenthesizedExpression, parenthesizedExpression);
		}
		
		public event Action<PointerReferenceExpression> EnterPointerReferenceExpression, LeavePointerReferenceExpression;
		
		void IAstVisitor.VisitPointerReferenceExpression(PointerReferenceExpression pointerReferenceExpression)
		{
			Visit(EnterPointerReferenceExpression, LeavePointerReferenceExpression, pointerReferenceExpression);
		}
		
		public event Action<PrimitiveExpression> EnterPrimitiveExpression, LeavePrimitiveExpression;
		
		void IAstVisitor.VisitPrimitiveExpression(PrimitiveExpression primitiveExpression)
		{
			Visit(EnterPrimitiveExpression, LeavePrimitiveExpression, primitiveExpression);
		}
		
		public event Action<SizeOfExpression> EnterSizeOfExpression, LeaveSizeOfExpression;
		
		void IAstVisitor.VisitSizeOfExpression(SizeOfExpression sizeOfExpression)
		{
			Visit(EnterSizeOfExpression, LeaveSizeOfExpression, sizeOfExpression);
		}
		
		public event Action<StackAllocExpression> EnterStackAllocExpression, LeaveStackAllocExpression;
		
		void IAstVisitor.VisitStackAllocExpression(StackAllocExpression stackAllocExpression)
		{
			Visit(EnterStackAllocExpression, LeaveStackAllocExpression, stackAllocExpression);
		}
		
		public event Action<ThisReferenceExpression> EnterThisReferenceExpression, LeaveThisReferenceExpression;
		
		void IAstVisitor.VisitThisReferenceExpression(ThisReferenceExpression thisReferenceExpression)
		{
			Visit(EnterThisReferenceExpression, LeaveThisReferenceExpression, thisReferenceExpression);
		}
		
		public event Action<TypeOfExpression> EnterTypeOfExpression, LeaveTypeOfExpression;
		
		void IAstVisitor.VisitTypeOfExpression(TypeOfExpression typeOfExpression)
		{
			Visit(EnterTypeOfExpression, LeaveTypeOfExpression, typeOfExpression);
		}
		
		public event Action<TypeReferenceExpression> EnterTypeReferenceExpression, LeaveTypeReferenceExpression;
		
		void IAstVisitor.VisitTypeReferenceExpression(TypeReferenceExpression typeReferenceExpression)
		{
			Visit(EnterTypeReferenceExpression, LeaveTypeReferenceExpression, typeReferenceExpression);
		}
		
		public event Action<UnaryOperatorExpression> EnterUnaryOperatorExpression, LeaveUnaryOperatorExpression;
		
		void IAstVisitor.VisitUnaryOperatorExpression(UnaryOperatorExpression unaryOperatorExpression)
		{
			Visit(EnterUnaryOperatorExpression, LeaveUnaryOperatorExpression, unaryOperatorExpression);
		}
		
		public event Action<UncheckedExpression> EnterUncheckedExpression, LeaveUncheckedExpression;
		
		void IAstVisitor.VisitUncheckedExpression(UncheckedExpression uncheckedExpression)
		{
			Visit(EnterUncheckedExpression, LeaveUncheckedExpression, uncheckedExpression);
		}
		
		public event Action<QueryExpression> EnterQueryExpression, LeaveQueryExpression;
		
		void IAstVisitor.VisitQueryExpression(QueryExpression queryExpression)
		{
			Visit(EnterQueryExpression, LeaveQueryExpression, queryExpression);
		}
		
		public event Action<QueryContinuationClause> EnterQueryContinuationClause, LeaveQueryContinuationClause;
		
		void IAstVisitor.VisitQueryContinuationClause(QueryContinuationClause queryContinuationClause)
		{
			Visit(EnterQueryContinuationClause, LeaveQueryContinuationClause, queryContinuationClause);
		}
		
		public event Action<QueryFromClause> EnterQueryFromClause, LeaveQueryFromClause;
		
		void IAstVisitor.VisitQueryFromClause(QueryFromClause queryFromClause)
		{
			Visit(EnterQueryFromClause, LeaveQueryFromClause, queryFromClause);
		}
		
		public event Action<QueryLetClause> EnterQueryLetClause, LeaveQueryLetClause;
		
		void IAstVisitor.VisitQueryLetClause(QueryLetClause queryLetClause)
		{
			Visit(EnterQueryLetClause, LeaveQueryLetClause, queryLetClause);
		}
		
		public event Action<QueryWhereClause> EnterQueryWhereClause, LeaveQueryWhereClause;
		
		void IAstVisitor.VisitQueryWhereClause(QueryWhereClause queryWhereClause)
		{
			Visit(EnterQueryWhereClause, LeaveQueryWhereClause, queryWhereClause);
		}
		
		public event Action<QueryJoinClause> EnterQueryJoinClause, LeaveQueryJoinClause;
		
		void IAstVisitor.VisitQueryJoinClause(QueryJoinClause queryJoinClause)
		{
			Visit(EnterQueryJoinClause, LeaveQueryJoinClause, queryJoinClause);
		}
		
		public event Action<QueryOrderClause> EnterQueryOrderClause, LeaveQueryOrderClause;
		
		void IAstVisitor.VisitQueryOrderClause(QueryOrderClause queryOrderClause)
		{
			Visit(EnterQueryOrderClause, LeaveQueryOrderClause, queryOrderClause);
		}
		
		public event Action<QueryOrdering> EnterQueryOrdering, LeaveQueryOrdering;
		
		void IAstVisitor.VisitQueryOrdering(QueryOrdering queryOrdering)
		{
			Visit(EnterQueryOrdering, LeaveQueryOrdering, queryOrdering);
		}
		
		public event Action<QuerySelectClause> EnterQuerySelectClause, LeaveQuerySelectClause;
		
		void IAstVisitor.VisitQuerySelectClause(QuerySelectClause querySelectClause)
		{
			Visit(EnterQuerySelectClause, LeaveQuerySelectClause, querySelectClause);
		}
		
		public event Action<QueryGroupClause> EnterQueryGroupClause, LeaveQueryGroupClause;
		
		void IAstVisitor.VisitQueryGroupClause(QueryGroupClause queryGroupClause)
		{
			Visit(EnterQueryGroupClause, LeaveQueryGroupClause, queryGroupClause);
		}
		
		public event Action<AsExpression> EnterAsExpression, LeaveAsExpression;
		
		void IAstVisitor.VisitAsExpression(AsExpression asExpression)
		{
			Visit(EnterAsExpression, LeaveAsExpression, asExpression);
		}
		
		public event Action<IsExpression> EnterIsExpression, LeaveIsExpression;
		
		void IAstVisitor.VisitIsExpression(IsExpression isExpression)
		{
			Visit(EnterIsExpression, LeaveIsExpression, isExpression);
		}
		
		public event Action<DefaultValueExpression> EnterDefaultValueExpression, LeaveDefaultValueExpression;
		
		void IAstVisitor.VisitDefaultValueExpression(DefaultValueExpression defaultValueExpression)
		{
			Visit(EnterDefaultValueExpression, LeaveDefaultValueExpression, defaultValueExpression);
		}
		
		public event Action<UndocumentedExpression> EnterUndocumentedExpression, LeaveUndocumentedExpression;
		
		void IAstVisitor.VisitUndocumentedExpression(UndocumentedExpression undocumentedExpression)
		{
			Visit(EnterUndocumentedExpression, LeaveUndocumentedExpression, undocumentedExpression);
		}
		
		public event Action<ArrayInitializerExpression> EnterArrayInitializerExpression, LeaveArrayInitializerExpression;
		
		void IAstVisitor.VisitArrayInitializerExpression(ArrayInitializerExpression arrayInitializerExpression)
		{
			Visit(EnterArrayInitializerExpression, LeaveArrayInitializerExpression, arrayInitializerExpression);
		}
		
		public event Action<ArraySpecifier> EnterArraySpecifier, LeaveArraySpecifier;
		
		void IAstVisitor.VisitArraySpecifier(ArraySpecifier arraySpecifier)
		{
			Visit(EnterArraySpecifier, LeaveArraySpecifier, arraySpecifier);
		}
		
		public event Action<NamedArgumentExpression> EnterNamedArgumentExpression, LeaveNamedArgumentExpression;
		
		void IAstVisitor.VisitNamedArgumentExpression(NamedArgumentExpression namedArgumentExpression)
		{
			Visit(EnterNamedArgumentExpression, LeaveNamedArgumentExpression, namedArgumentExpression);
		}
		
		public event Action<NamedExpression> EnterNamedExpression, LeaveNamedExpression;
		
		void IAstVisitor.VisitNamedExpression(NamedExpression namedExpression)
		{
			Visit(EnterNamedExpression, LeaveNamedExpression, namedExpression);
		}
		
		public event Action<EmptyExpression> EnterEmptyExpression, LeaveEmptyExpression;
		
		void IAstVisitor.VisitEmptyExpression(EmptyExpression emptyExpression)
		{
			Visit(EnterEmptyExpression, LeaveEmptyExpression, emptyExpression);
		}
		
		void IAstVisitor.VisitPatternPlaceholder(AstNode placeholder, PatternMatching.Pattern pattern)
		{
		}
	}
}
