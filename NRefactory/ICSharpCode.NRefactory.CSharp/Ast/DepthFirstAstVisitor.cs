// 
// IAstVisitor.cs
//
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2010 Novell, Inc (http://www.novell.com)
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
	/// <summary>
	/// AST visitor with a default implementation that visits all node depth-first.
	/// </summary>
	public abstract class DepthFirstAstVisitor<T, S> : IAstVisitor<T, S>
	{
		protected virtual S VisitChildren (AstNode node, T data)
		{
			AstNode next;
			for (var child = node.FirstChild; child != null; child = next) {
				// Store next to allow the loop to continue
				// if the visitor removes/replaces child.
				next = child.NextSibling;
				child.AcceptVisitor (this, data);
			}
			return default (S);
		}
		
		public virtual S VisitCompilationUnit (CompilationUnit unit, T data)
		{
			return VisitChildren (unit, data);
		}
		
		public virtual S VisitComment (Comment comment, T data)
		{
			return VisitChildren (comment, data);
		}
		
		public virtual S VisitIdentifier (Identifier identifier, T data)
		{
			return VisitChildren (identifier, data);
		}
		
		public virtual S VisitCSharpTokenNode (CSharpTokenNode token, T data)
		{
			return VisitChildren (token, data);
		}
		
		public virtual S VisitPrimitiveType (PrimitiveType primitiveType, T data)
		{
			return VisitChildren (primitiveType, data);
		}
		
		public virtual S VisitComposedType (ComposedType composedType, T data)
		{
			return VisitChildren (composedType, data);
		}
		
		public virtual S VisitSimpleType(SimpleType simpleType, T data)
		{
			return VisitChildren (simpleType, data);
		}
		
		public virtual S VisitMemberType(MemberType memberType, T data)
		{
			return VisitChildren (memberType, data);
		}
		
		public virtual S VisitAttribute (Attribute attribute, T data)
		{
			return VisitChildren (attribute, data);
		}
		
		public virtual S VisitAttributeSection (AttributeSection attributeSection, T data)
		{
			return VisitChildren (attributeSection, data);
		}
		
		public virtual S VisitDelegateDeclaration (DelegateDeclaration delegateDeclaration, T data)
		{
			return VisitChildren (delegateDeclaration, data);
		}
		
		public virtual S VisitNamespaceDeclaration (NamespaceDeclaration namespaceDeclaration, T data)
		{
			return VisitChildren (namespaceDeclaration, data);
		}
		
		public virtual S VisitTypeDeclaration (TypeDeclaration typeDeclaration, T data)
		{
			return VisitChildren (typeDeclaration, data);
		}
		
		public virtual S VisitTypeParameterDeclaration (TypeParameterDeclaration typeParameterDeclaration, T data)
		{
			return VisitChildren (typeParameterDeclaration, data);
		}
		
		public virtual S VisitEnumMemberDeclaration (EnumMemberDeclaration enumMemberDeclaration, T data)
		{
			return VisitChildren (enumMemberDeclaration, data);
		}
		
		public virtual S VisitUsingDeclaration (UsingDeclaration usingDeclaration, T data)
		{
			return VisitChildren (usingDeclaration, data);
		}
		
		public virtual S VisitUsingAliasDeclaration (UsingAliasDeclaration usingDeclaration, T data)
		{
			return VisitChildren (usingDeclaration, data);
		}
		
		public virtual S VisitExternAliasDeclaration(ExternAliasDeclaration externAliasDeclaration, T data)
		{
			return VisitChildren (externAliasDeclaration, data);
		}
		
		public virtual S VisitConstructorDeclaration (ConstructorDeclaration constructorDeclaration, T data)
		{
			return VisitChildren (constructorDeclaration, data);
		}
		
		public virtual S VisitConstructorInitializer (ConstructorInitializer constructorInitializer, T data)
		{
			return VisitChildren (constructorInitializer, data);
		}
		
		public virtual S VisitDestructorDeclaration (DestructorDeclaration destructorDeclaration, T data)
		{
			return VisitChildren (destructorDeclaration, data);
		}
		
		public virtual S VisitEventDeclaration (EventDeclaration eventDeclaration, T data)
		{
			return VisitChildren (eventDeclaration, data);
		}
		
		public virtual S VisitCustomEventDeclaration (CustomEventDeclaration eventDeclaration, T data)
		{
			return VisitChildren (eventDeclaration, data);
		}
		
		public virtual S VisitFieldDeclaration (FieldDeclaration fieldDeclaration, T data)
		{
			return VisitChildren (fieldDeclaration, data);
		}
		
		public virtual S VisitFixedFieldDeclaration (FixedFieldDeclaration fixedFieldDeclaration, T data)
		{
			return VisitChildren (fixedFieldDeclaration, data);
		}
		
		public virtual S VisitFixedVariableInitializer (FixedVariableInitializer fixedVariableInitializer, T data)
		{
			return VisitChildren (fixedVariableInitializer, data);
		}
		
		public virtual S VisitIndexerDeclaration (IndexerDeclaration indexerDeclaration, T data)
		{
			return VisitChildren (indexerDeclaration, data);
		}
		
		public virtual S VisitMethodDeclaration (MethodDeclaration methodDeclaration, T data)
		{
			return VisitChildren (methodDeclaration, data);
		}
		
		public virtual S VisitOperatorDeclaration (OperatorDeclaration operatorDeclaration, T data)
		{
			return VisitChildren (operatorDeclaration, data);
		}
		
		public virtual S VisitPropertyDeclaration (PropertyDeclaration propertyDeclaration, T data)
		{
			return VisitChildren (propertyDeclaration, data);
		}
		
		public virtual S VisitAccessor (Accessor accessor, T data)
		{
			return VisitChildren (accessor, data);
		}
		
		public virtual S VisitVariableInitializer (VariableInitializer variableInitializer, T data)
		{
			return VisitChildren (variableInitializer, data);
		}
		
		public virtual S VisitParameterDeclaration (ParameterDeclaration parameterDeclaration, T data)
		{
			return VisitChildren (parameterDeclaration, data);
		}
		
		public virtual S VisitConstraint (Constraint constraint, T data)
		{
			return VisitChildren (constraint, data);
		}
		
		public virtual S VisitBlockStatement (BlockStatement blockStatement, T data)
		{
			return VisitChildren (blockStatement, data);
		}
		
		public virtual S VisitExpressionStatement (ExpressionStatement expressionStatement, T data)
		{
			return VisitChildren (expressionStatement, data);
		}
		
		public virtual S VisitBreakStatement (BreakStatement breakStatement, T data)
		{
			return VisitChildren (breakStatement, data);
		}
		
		public virtual S VisitCheckedStatement (CheckedStatement checkedStatement, T data)
		{
			return VisitChildren (checkedStatement, data);
		}
		
		public virtual S VisitContinueStatement (ContinueStatement continueStatement, T data)
		{
			return VisitChildren (continueStatement, data);
		}
		
		public virtual S VisitDoWhileStatement (DoWhileStatement doWhileStatement, T data)
		{
			return VisitChildren (doWhileStatement, data);
		}
		
		public virtual S VisitEmptyStatement (EmptyStatement emptyStatement, T data)
		{
			return VisitChildren (emptyStatement, data);
		}
		
		public virtual S VisitFixedStatement (FixedStatement fixedStatement, T data)
		{
			return VisitChildren (fixedStatement, data);
		}
		
		public virtual S VisitForeachStatement (ForeachStatement foreachStatement, T data)
		{
			return VisitChildren (foreachStatement, data);
		}
		
		public virtual S VisitForStatement (ForStatement forStatement, T data)
		{
			return VisitChildren (forStatement, data);
		}
		
		public virtual S VisitGotoCaseStatement (GotoCaseStatement gotoCaseStatement, T data)
		{
			return VisitChildren (gotoCaseStatement, data);
		}
		
		public virtual S VisitGotoDefaultStatement (GotoDefaultStatement gotoDefaultStatement, T data)
		{
			return VisitChildren (gotoDefaultStatement, data);
		}
		
		public virtual S VisitGotoStatement (GotoStatement gotoStatement, T data)
		{
			return VisitChildren (gotoStatement, data);
		}
		
		public virtual S VisitIfElseStatement (IfElseStatement ifElseStatement, T data)
		{
			return VisitChildren (ifElseStatement, data);
		}
		
		public virtual S VisitLabelStatement (LabelStatement labelStatement, T data)
		{
			return VisitChildren (labelStatement, data);
		}
		
		public virtual S VisitLockStatement (LockStatement lockStatement, T data)
		{
			return VisitChildren (lockStatement, data);
		}
		
		public virtual S VisitReturnStatement (ReturnStatement returnStatement, T data)
		{
			return VisitChildren (returnStatement, data);
		}
		
		public virtual S VisitSwitchStatement (SwitchStatement switchStatement, T data)
		{
			return VisitChildren (switchStatement, data);
		}
		
		public virtual S VisitSwitchSection (SwitchSection switchSection, T data)
		{
			return VisitChildren (switchSection, data);
		}
		
		public virtual S VisitCaseLabel (CaseLabel caseLabel, T data)
		{
			return VisitChildren (caseLabel, data);
		}
		
		public virtual S VisitThrowStatement (ThrowStatement throwStatement, T data)
		{
			return VisitChildren (throwStatement, data);
		}
		
		public virtual S VisitTryCatchStatement (TryCatchStatement tryCatchStatement, T data)
		{
			return VisitChildren (tryCatchStatement, data);
		}
		
		public virtual S VisitCatchClause (CatchClause catchClause, T data)
		{
			return VisitChildren (catchClause, data);
		}
		
		public virtual S VisitUncheckedStatement (UncheckedStatement uncheckedStatement, T data)
		{
			return VisitChildren (uncheckedStatement, data);
		}
		
		public virtual S VisitUnsafeStatement (UnsafeStatement unsafeStatement, T data)
		{
			return VisitChildren (unsafeStatement, data);
		}
		
		public virtual S VisitUsingStatement (UsingStatement usingStatement, T data)
		{
			return VisitChildren (usingStatement, data);
		}
		
		public virtual S VisitVariableDeclarationStatement (VariableDeclarationStatement variableDeclarationStatement, T data)
		{
			return VisitChildren (variableDeclarationStatement, data);
		}
		
		public virtual S VisitWhileStatement (WhileStatement whileStatement, T data)
		{
			return VisitChildren (whileStatement, data);
		}
		
		public virtual S VisitYieldBreakStatement (YieldBreakStatement yieldBreakStatement, T data)
		{
			return VisitChildren (yieldBreakStatement, data);
		}
		
		public virtual S VisitYieldReturnStatement (YieldReturnStatement yieldReturnStatement, T data)
		{
			return VisitChildren (yieldReturnStatement, data);
		}
		
		public virtual S VisitAnonymousMethodExpression (AnonymousMethodExpression anonymousMethodExpression, T data)
		{
			return VisitChildren (anonymousMethodExpression, data);
		}
		
		public virtual S VisitLambdaExpression (LambdaExpression lambdaExpression, T data)
		{
			return VisitChildren (lambdaExpression, data);
		}
		
		public virtual S VisitAssignmentExpression (AssignmentExpression assignmentExpression, T data)
		{
			return VisitChildren (assignmentExpression, data);
		}
		
		public virtual S VisitBaseReferenceExpression (BaseReferenceExpression baseReferenceExpression, T data)
		{
			return VisitChildren (baseReferenceExpression, data);
		}
		
		public virtual S VisitBinaryOperatorExpression (BinaryOperatorExpression binaryOperatorExpression, T data)
		{
			return VisitChildren (binaryOperatorExpression, data);
		}
		
		public virtual S VisitCastExpression (CastExpression castExpression, T data)
		{
			return VisitChildren (castExpression, data);
		}
		
		public virtual S VisitCheckedExpression (CheckedExpression checkedExpression, T data)
		{
			return VisitChildren (checkedExpression, data);
		}
		
		public virtual S VisitConditionalExpression (ConditionalExpression conditionalExpression, T data)
		{
			return VisitChildren (conditionalExpression, data);
		}
		
		public virtual S VisitIdentifierExpression (IdentifierExpression identifierExpression, T data)
		{
			return VisitChildren (identifierExpression, data);
		}
		
		public virtual S VisitIndexerExpression (IndexerExpression indexerExpression, T data)
		{
			return VisitChildren (indexerExpression, data);
		}
		
		public virtual S VisitInvocationExpression (InvocationExpression invocationExpression, T data)
		{
			return VisitChildren (invocationExpression, data);
		}
		
		public virtual S VisitDirectionExpression (DirectionExpression directionExpression, T data)
		{
			return VisitChildren (directionExpression, data);
		}
		
		public virtual S VisitMemberReferenceExpression (MemberReferenceExpression memberReferenceExpression, T data)
		{
			return VisitChildren (memberReferenceExpression, data);
		}
		
		public virtual S VisitNullReferenceExpression (NullReferenceExpression nullReferenceExpression, T data)
		{
			return VisitChildren (nullReferenceExpression, data);
		}
		
		public virtual S VisitObjectCreateExpression (ObjectCreateExpression objectCreateExpression, T data)
		{
			return VisitChildren (objectCreateExpression, data);
		}
		
		public virtual S VisitAnonymousTypeCreateExpression(AnonymousTypeCreateExpression anonymousTypeCreateExpression, T data)
		{
			return VisitChildren (anonymousTypeCreateExpression, data);
		}
		
		public virtual S VisitArrayCreateExpression (ArrayCreateExpression arrayCreateExpression, T data)
		{
			return VisitChildren (arrayCreateExpression, data);
		}
		
		public virtual S VisitParenthesizedExpression (ParenthesizedExpression parenthesizedExpression, T data)
		{
			return VisitChildren (parenthesizedExpression, data);
		}
		
		public virtual S VisitPointerReferenceExpression (PointerReferenceExpression pointerReferenceExpression, T data)
		{
			return VisitChildren (pointerReferenceExpression, data);
		}
		
		public virtual S VisitPrimitiveExpression(PrimitiveExpression primitiveExpression, T data)
		{
			return VisitChildren (primitiveExpression, data);
		}
		
		public virtual S VisitSizeOfExpression (SizeOfExpression sizeOfExpression, T data)
		{
			return VisitChildren (sizeOfExpression, data);
		}
		
		public virtual S VisitStackAllocExpression (StackAllocExpression stackAllocExpression, T data)
		{
			return VisitChildren (stackAllocExpression, data);
		}
		
		public virtual S VisitThisReferenceExpression(ThisReferenceExpression thisReferenceExpression, T data)
		{
			return VisitChildren (thisReferenceExpression, data);
		}
		
		public virtual S VisitTypeOfExpression (TypeOfExpression typeOfExpression, T data)
		{
			return VisitChildren (typeOfExpression, data);
		}
		
		public virtual S VisitTypeReferenceExpression(TypeReferenceExpression typeReferenceExpression, T data)
		{
			return VisitChildren (typeReferenceExpression, data);
		}
		
		public virtual S VisitUnaryOperatorExpression (UnaryOperatorExpression unaryOperatorExpression, T data)
		{
			return VisitChildren (unaryOperatorExpression, data);
		}
		
		public virtual S VisitUncheckedExpression (UncheckedExpression uncheckedExpression, T data)
		{
			return VisitChildren (uncheckedExpression, data);
		}
		
		public virtual S VisitQueryExpression(QueryExpression queryExpression, T data)
		{
			return VisitChildren (queryExpression, data);
		}
		
		public virtual S VisitQueryContinuationClause(QueryContinuationClause queryContinuationClause, T data)
		{
			return VisitChildren (queryContinuationClause, data);
		}
		
		public virtual S VisitQueryFromClause(QueryFromClause queryFromClause, T data)
		{
			return VisitChildren (queryFromClause, data);
		}
		
		public virtual S VisitQueryLetClause(QueryLetClause queryLetClause, T data)
		{
			return VisitChildren (queryLetClause, data);
		}
		
		public virtual S VisitQueryWhereClause(QueryWhereClause queryWhereClause, T data)
		{
			return VisitChildren (queryWhereClause, data);
		}
		
		public virtual S VisitQueryJoinClause(QueryJoinClause queryJoinClause, T data)
		{
			return VisitChildren (queryJoinClause, data);
		}
		
		public virtual S VisitQueryOrderClause(QueryOrderClause queryOrderClause, T data)
		{
			return VisitChildren (queryOrderClause, data);
		}
		
		public virtual S VisitQueryOrdering(QueryOrdering queryOrdering, T data)
		{
			return VisitChildren (queryOrdering, data);
		}
		
		public virtual S VisitQuerySelectClause(QuerySelectClause querySelectClause, T data)
		{
			return VisitChildren (querySelectClause, data);
		}
		
		public virtual S VisitQueryGroupClause(QueryGroupClause queryGroupClause, T data)
		{
			return VisitChildren (queryGroupClause, data);
		}
		
		public virtual S VisitAsExpression (AsExpression asExpression, T data)
		{
			return VisitChildren (asExpression, data);
		}
		
		public virtual S VisitIsExpression (IsExpression isExpression, T data)
		{
			return VisitChildren (isExpression, data);
		}
		
		public virtual S VisitDefaultValueExpression (DefaultValueExpression defaultValueExpression, T data)
		{
			return VisitChildren (defaultValueExpression, data);
		}
		
		public virtual S VisitUndocumentedExpression (UndocumentedExpression undocumentedExpression, T data)
		{
			return VisitChildren (undocumentedExpression, data);
		}
		
		public virtual S VisitArrayInitializerExpression (ArrayInitializerExpression arrayInitializerExpression, T data)
		{
			return VisitChildren (arrayInitializerExpression, data);
		}
		
		public virtual S VisitArraySpecifier (ArraySpecifier arraySpecifier, T data)
		{
			return VisitChildren (arraySpecifier, data);
		}
		
		public virtual S VisitNamedArgumentExpression (NamedArgumentExpression namedArgumentExpression, T data)
		{
			return VisitChildren (namedArgumentExpression, data);
		}
		
		public virtual S VisitNamedExpression (NamedExpression namedExpression, T data)
		{
			return VisitChildren (namedExpression, data);
		}
		
		public virtual S VisitEmptyExpression (EmptyExpression emptyExpression, T data)
		{
			return VisitChildren (emptyExpression, data);
		}
		
		public virtual S VisitPatternPlaceholder(AstNode placeholder, PatternMatching.Pattern pattern, T data)
		{
			return VisitChildren (placeholder, data);
		}
	}
}
