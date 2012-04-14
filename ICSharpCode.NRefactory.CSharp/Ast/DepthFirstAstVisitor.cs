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
	public abstract class DepthFirstAstVisitor : IAstVisitor
	{
		protected virtual void VisitChildren (AstNode node)
		{
			AstNode next;
			for (var child = node.FirstChild; child != null; child = next) {
				// Store next to allow the loop to continue
				// if the visitor removes/replaces child.
				next = child.NextSibling;
				child.AcceptVisitor (this);
			}
		}
		
		public virtual void VisitCompilationUnit (CompilationUnit unit)
		{
			VisitChildren (unit);
		}
		
		public virtual void VisitComment(Comment comment)
		{
			VisitChildren(comment);
		}

		public virtual void VisitNewLine(NewLineNode newLineNode)
		{
			VisitChildren(newLineNode);
		}

		public virtual void VisitWhitespace(WhitespaceNode whitespaceNode)
		{
			VisitChildren(whitespaceNode);
		}

		public virtual void VisitText(TextNode textNode)
		{
			VisitChildren(textNode);
		}

		public virtual void VisitDocumentationReference (DocumentationReference documentationReference)
		{
			VisitChildren (documentationReference);
		}
		
		public virtual void VisitPreProcessorDirective (PreProcessorDirective preProcessorDirective)
		{
			VisitChildren (preProcessorDirective);
		}

		public virtual void VisitIdentifier (Identifier identifier)
		{
			VisitChildren (identifier);
		}
		
		public virtual void VisitCSharpTokenNode (CSharpTokenNode token)
		{
			VisitChildren (token);
		}
		
		public virtual void VisitPrimitiveType (PrimitiveType primitiveType)
		{
			VisitChildren (primitiveType);
		}
		
		public virtual void VisitComposedType (ComposedType composedType)
		{
			VisitChildren (composedType);
		}
		
		public virtual void VisitSimpleType(SimpleType simpleType)
		{
			VisitChildren (simpleType);
		}
		
		public virtual void VisitMemberType(MemberType memberType)
		{
			VisitChildren (memberType);
		}
		
		public virtual void VisitAttribute (Attribute attribute)
		{
			VisitChildren (attribute);
		}
		
		public virtual void VisitAttributeSection (AttributeSection attributeSection)
		{
			VisitChildren (attributeSection);
		}
		
		public virtual void VisitDelegateDeclaration (DelegateDeclaration delegateDeclaration)
		{
			VisitChildren (delegateDeclaration);
		}
		
		public virtual void VisitNamespaceDeclaration (NamespaceDeclaration namespaceDeclaration)
		{
			VisitChildren (namespaceDeclaration);
		}
		
		public virtual void VisitTypeDeclaration (TypeDeclaration typeDeclaration)
		{
			VisitChildren (typeDeclaration);
		}
		
		public virtual void VisitTypeParameterDeclaration (TypeParameterDeclaration typeParameterDeclaration)
		{
			VisitChildren (typeParameterDeclaration);
		}
		
		public virtual void VisitEnumMemberDeclaration (EnumMemberDeclaration enumMemberDeclaration)
		{
			VisitChildren (enumMemberDeclaration);
		}
		
		public virtual void VisitUsingDeclaration (UsingDeclaration usingDeclaration)
		{
			VisitChildren (usingDeclaration);
		}
		
		public virtual void VisitUsingAliasDeclaration (UsingAliasDeclaration usingDeclaration)
		{
			VisitChildren (usingDeclaration);
		}
		
		public virtual void VisitExternAliasDeclaration(ExternAliasDeclaration externAliasDeclaration)
		{
			VisitChildren (externAliasDeclaration);
		}
		
		public virtual void VisitConstructorDeclaration (ConstructorDeclaration constructorDeclaration)
		{
			VisitChildren (constructorDeclaration);
		}
		
		public virtual void VisitConstructorInitializer (ConstructorInitializer constructorInitializer)
		{
			VisitChildren (constructorInitializer);
		}
		
		public virtual void VisitDestructorDeclaration (DestructorDeclaration destructorDeclaration)
		{
			VisitChildren (destructorDeclaration);
		}
		
		public virtual void VisitEventDeclaration (EventDeclaration eventDeclaration)
		{
			VisitChildren (eventDeclaration);
		}
		
		public virtual void VisitCustomEventDeclaration (CustomEventDeclaration eventDeclaration)
		{
			VisitChildren (eventDeclaration);
		}
		
		public virtual void VisitFieldDeclaration (FieldDeclaration fieldDeclaration)
		{
			VisitChildren (fieldDeclaration);
		}
		
		public virtual void VisitFixedFieldDeclaration (FixedFieldDeclaration fixedFieldDeclaration)
		{
			VisitChildren (fixedFieldDeclaration);
		}
		
		public virtual void VisitFixedVariableInitializer (FixedVariableInitializer fixedVariableInitializer)
		{
			VisitChildren (fixedVariableInitializer);
		}
		
		public virtual void VisitIndexerDeclaration (IndexerDeclaration indexerDeclaration)
		{
			VisitChildren (indexerDeclaration);
		}
		
		public virtual void VisitMethodDeclaration (MethodDeclaration methodDeclaration)
		{
			VisitChildren (methodDeclaration);
		}
		
		public virtual void VisitOperatorDeclaration (OperatorDeclaration operatorDeclaration)
		{
			VisitChildren (operatorDeclaration);
		}
		
		public virtual void VisitPropertyDeclaration (PropertyDeclaration propertyDeclaration)
		{
			VisitChildren (propertyDeclaration);
		}
		
		public virtual void VisitAccessor (Accessor accessor)
		{
			VisitChildren (accessor);
		}
		
		public virtual void VisitVariableInitializer (VariableInitializer variableInitializer)
		{
			VisitChildren (variableInitializer);
		}
		
		public virtual void VisitParameterDeclaration (ParameterDeclaration parameterDeclaration)
		{
			VisitChildren (parameterDeclaration);
		}
		
		public virtual void VisitConstraint (Constraint constraint)
		{
			VisitChildren (constraint);
		}
		
		public virtual void VisitBlockStatement (BlockStatement blockStatement)
		{
			VisitChildren (blockStatement);
		}
		
		public virtual void VisitExpressionStatement (ExpressionStatement expressionStatement)
		{
			VisitChildren (expressionStatement);
		}
		
		public virtual void VisitBreakStatement (BreakStatement breakStatement)
		{
			VisitChildren (breakStatement);
		}
		
		public virtual void VisitCheckedStatement (CheckedStatement checkedStatement)
		{
			VisitChildren (checkedStatement);
		}
		
		public virtual void VisitContinueStatement (ContinueStatement continueStatement)
		{
			VisitChildren (continueStatement);
		}
		
		public virtual void VisitDoWhileStatement (DoWhileStatement doWhileStatement)
		{
			VisitChildren (doWhileStatement);
		}
		
		public virtual void VisitEmptyStatement (EmptyStatement emptyStatement)
		{
			VisitChildren (emptyStatement);
		}
		
		public virtual void VisitFixedStatement (FixedStatement fixedStatement)
		{
			VisitChildren (fixedStatement);
		}
		
		public virtual void VisitForeachStatement (ForeachStatement foreachStatement)
		{
			VisitChildren (foreachStatement);
		}
		
		public virtual void VisitForStatement (ForStatement forStatement)
		{
			VisitChildren (forStatement);
		}
		
		public virtual void VisitGotoCaseStatement (GotoCaseStatement gotoCaseStatement)
		{
			VisitChildren (gotoCaseStatement);
		}
		
		public virtual void VisitGotoDefaultStatement (GotoDefaultStatement gotoDefaultStatement)
		{
			VisitChildren (gotoDefaultStatement);
		}
		
		public virtual void VisitGotoStatement (GotoStatement gotoStatement)
		{
			VisitChildren (gotoStatement);
		}
		
		public virtual void VisitIfElseStatement (IfElseStatement ifElseStatement)
		{
			VisitChildren (ifElseStatement);
		}
		
		public virtual void VisitLabelStatement (LabelStatement labelStatement)
		{
			VisitChildren (labelStatement);
		}
		
		public virtual void VisitLockStatement (LockStatement lockStatement)
		{
			VisitChildren (lockStatement);
		}
		
		public virtual void VisitReturnStatement (ReturnStatement returnStatement)
		{
			VisitChildren (returnStatement);
		}
		
		public virtual void VisitSwitchStatement (SwitchStatement switchStatement)
		{
			VisitChildren (switchStatement);
		}
		
		public virtual void VisitSwitchSection (SwitchSection switchSection)
		{
			VisitChildren (switchSection);
		}
		
		public virtual void VisitCaseLabel (CaseLabel caseLabel)
		{
			VisitChildren (caseLabel);
		}
		
		public virtual void VisitThrowStatement (ThrowStatement throwStatement)
		{
			VisitChildren (throwStatement);
		}
		
		public virtual void VisitTryCatchStatement (TryCatchStatement tryCatchStatement)
		{
			VisitChildren (tryCatchStatement);
		}
		
		public virtual void VisitCatchClause (CatchClause catchClause)
		{
			VisitChildren (catchClause);
		}
		
		public virtual void VisitUncheckedStatement (UncheckedStatement uncheckedStatement)
		{
			VisitChildren (uncheckedStatement);
		}
		
		public virtual void VisitUnsafeStatement (UnsafeStatement unsafeStatement)
		{
			VisitChildren (unsafeStatement);
		}
		
		public virtual void VisitUsingStatement (UsingStatement usingStatement)
		{
			VisitChildren (usingStatement);
		}
		
		public virtual void VisitVariableDeclarationStatement (VariableDeclarationStatement variableDeclarationStatement)
		{
			VisitChildren (variableDeclarationStatement);
		}
		
		public virtual void VisitWhileStatement (WhileStatement whileStatement)
		{
			VisitChildren (whileStatement);
		}
		
		public virtual void VisitYieldBreakStatement (YieldBreakStatement yieldBreakStatement)
		{
			VisitChildren (yieldBreakStatement);
		}
		
		public virtual void VisitYieldReturnStatement (YieldReturnStatement yieldReturnStatement)
		{
			VisitChildren (yieldReturnStatement);
		}
		
		public virtual void VisitAnonymousMethodExpression (AnonymousMethodExpression anonymousMethodExpression)
		{
			VisitChildren (anonymousMethodExpression);
		}
		
		public virtual void VisitLambdaExpression (LambdaExpression lambdaExpression)
		{
			VisitChildren (lambdaExpression);
		}
		
		public virtual void VisitAssignmentExpression (AssignmentExpression assignmentExpression)
		{
			VisitChildren (assignmentExpression);
		}
		
		public virtual void VisitBaseReferenceExpression (BaseReferenceExpression baseReferenceExpression)
		{
			VisitChildren (baseReferenceExpression);
		}
		
		public virtual void VisitBinaryOperatorExpression (BinaryOperatorExpression binaryOperatorExpression)
		{
			VisitChildren (binaryOperatorExpression);
		}
		
		public virtual void VisitCastExpression (CastExpression castExpression)
		{
			VisitChildren (castExpression);
		}
		
		public virtual void VisitCheckedExpression (CheckedExpression checkedExpression)
		{
			VisitChildren (checkedExpression);
		}
		
		public virtual void VisitConditionalExpression (ConditionalExpression conditionalExpression)
		{
			VisitChildren (conditionalExpression);
		}
		
		public virtual void VisitIdentifierExpression (IdentifierExpression identifierExpression)
		{
			VisitChildren (identifierExpression);
		}
		
		public virtual void VisitIndexerExpression (IndexerExpression indexerExpression)
		{
			VisitChildren (indexerExpression);
		}
		
		public virtual void VisitInvocationExpression (InvocationExpression invocationExpression)
		{
			VisitChildren (invocationExpression);
		}
		
		public virtual void VisitDirectionExpression (DirectionExpression directionExpression)
		{
			VisitChildren (directionExpression);
		}
		
		public virtual void VisitMemberReferenceExpression (MemberReferenceExpression memberReferenceExpression)
		{
			VisitChildren (memberReferenceExpression);
		}
		
		public virtual void VisitNullReferenceExpression (NullReferenceExpression nullReferenceExpression)
		{
			VisitChildren (nullReferenceExpression);
		}
		
		public virtual void VisitObjectCreateExpression (ObjectCreateExpression objectCreateExpression)
		{
			VisitChildren (objectCreateExpression);
		}
		
		public virtual void VisitAnonymousTypeCreateExpression(AnonymousTypeCreateExpression anonymousTypeCreateExpression)
		{
			VisitChildren (anonymousTypeCreateExpression);
		}
		
		public virtual void VisitArrayCreateExpression (ArrayCreateExpression arrayCreateExpression)
		{
			VisitChildren (arrayCreateExpression);
		}
		
		public virtual void VisitParenthesizedExpression (ParenthesizedExpression parenthesizedExpression)
		{
			VisitChildren (parenthesizedExpression);
		}
		
		public virtual void VisitPointerReferenceExpression (PointerReferenceExpression pointerReferenceExpression)
		{
			VisitChildren (pointerReferenceExpression);
		}
		
		public virtual void VisitPrimitiveExpression(PrimitiveExpression primitiveExpression)
		{
			VisitChildren (primitiveExpression);
		}
		
		public virtual void VisitSizeOfExpression (SizeOfExpression sizeOfExpression)
		{
			VisitChildren (sizeOfExpression);
		}
		
		public virtual void VisitStackAllocExpression (StackAllocExpression stackAllocExpression)
		{
			VisitChildren (stackAllocExpression);
		}
		
		public virtual void VisitThisReferenceExpression(ThisReferenceExpression thisReferenceExpression)
		{
			VisitChildren (thisReferenceExpression);
		}
		
		public virtual void VisitTypeOfExpression (TypeOfExpression typeOfExpression)
		{
			VisitChildren (typeOfExpression);
		}
		
		public virtual void VisitTypeReferenceExpression(TypeReferenceExpression typeReferenceExpression)
		{
			VisitChildren (typeReferenceExpression);
		}
		
		public virtual void VisitUnaryOperatorExpression (UnaryOperatorExpression unaryOperatorExpression)
		{
			VisitChildren (unaryOperatorExpression);
		}
		
		public virtual void VisitUncheckedExpression (UncheckedExpression uncheckedExpression)
		{
			VisitChildren (uncheckedExpression);
		}
		
		public virtual void VisitQueryExpression(QueryExpression queryExpression)
		{
			VisitChildren (queryExpression);
		}
		
		public virtual void VisitQueryContinuationClause(QueryContinuationClause queryContinuationClause)
		{
			VisitChildren (queryContinuationClause);
		}
		
		public virtual void VisitQueryFromClause(QueryFromClause queryFromClause)
		{
			VisitChildren (queryFromClause);
		}
		
		public virtual void VisitQueryLetClause(QueryLetClause queryLetClause)
		{
			VisitChildren (queryLetClause);
		}
		
		public virtual void VisitQueryWhereClause(QueryWhereClause queryWhereClause)
		{
			VisitChildren (queryWhereClause);
		}
		
		public virtual void VisitQueryJoinClause(QueryJoinClause queryJoinClause)
		{
			VisitChildren (queryJoinClause);
		}
		
		public virtual void VisitQueryOrderClause(QueryOrderClause queryOrderClause)
		{
			VisitChildren (queryOrderClause);
		}
		
		public virtual void VisitQueryOrdering(QueryOrdering queryOrdering)
		{
			VisitChildren (queryOrdering);
		}
		
		public virtual void VisitQuerySelectClause(QuerySelectClause querySelectClause)
		{
			VisitChildren (querySelectClause);
		}
		
		public virtual void VisitQueryGroupClause(QueryGroupClause queryGroupClause)
		{
			VisitChildren (queryGroupClause);
		}
		
		public virtual void VisitAsExpression (AsExpression asExpression)
		{
			VisitChildren (asExpression);
		}
		
		public virtual void VisitIsExpression (IsExpression isExpression)
		{
			VisitChildren (isExpression);
		}
		
		public virtual void VisitDefaultValueExpression (DefaultValueExpression defaultValueExpression)
		{
			VisitChildren (defaultValueExpression);
		}
		
		public virtual void VisitUndocumentedExpression (UndocumentedExpression undocumentedExpression)
		{
			VisitChildren (undocumentedExpression);
		}
		
		public virtual void VisitArrayInitializerExpression (ArrayInitializerExpression arrayInitializerExpression)
		{
			VisitChildren (arrayInitializerExpression);
		}
		
		public virtual void VisitArraySpecifier (ArraySpecifier arraySpecifier)
		{
			VisitChildren (arraySpecifier);
		}
		
		public virtual void VisitNamedArgumentExpression (NamedArgumentExpression namedArgumentExpression)
		{
			VisitChildren (namedArgumentExpression);
		}
		
		public virtual void VisitNamedExpression (NamedExpression namedExpression)
		{
			VisitChildren (namedExpression);
		}
		
		public virtual void VisitEmptyExpression (EmptyExpression emptyExpression)
		{
			VisitChildren (emptyExpression);
		}
		
		public virtual void VisitPatternPlaceholder(AstNode placeholder, PatternMatching.Pattern pattern)
		{
			VisitChildren (placeholder);
		}
	}
	
	/// <summary>
	/// AST visitor with a default implementation that visits all node depth-first.
	/// </summary>
	public abstract class DepthFirstAstVisitor<T> : IAstVisitor<T>
	{
		protected virtual T VisitChildren (AstNode node)
		{
			AstNode next;
			for (var child = node.FirstChild; child != null; child = next) {
				// Store next to allow the loop to continue
				// if the visitor removes/replaces child.
				next = child.NextSibling;
				child.AcceptVisitor (this);
			}
			return default (T);
		}
		
		public virtual T VisitCompilationUnit (CompilationUnit unit)
		{
			return VisitChildren (unit);
		}
		
		public virtual T VisitComment (Comment comment)
		{
			return VisitChildren (comment);
		}
		
		public virtual T VisitNewLine(NewLineNode newLineNode)
		{
			return VisitChildren(newLineNode);
		}
		
		public virtual T VisitWhitespace(WhitespaceNode whitespaceNode)
		{
			return VisitChildren(whitespaceNode);
		}

		public virtual T VisitText(TextNode textNode)
		{
			return VisitChildren(textNode);
		}

		public virtual T VisitDocumentationReference (DocumentationReference documentationReference)
		{
			return VisitChildren (documentationReference);
		}
		
		public virtual T VisitPreProcessorDirective (PreProcessorDirective preProcessorDirective)
		{
			return VisitChildren (preProcessorDirective);
		}

		public virtual T VisitIdentifier (Identifier identifier)
		{
			return VisitChildren (identifier);
		}
		
		public virtual T VisitCSharpTokenNode (CSharpTokenNode token)
		{
			return VisitChildren (token);
		}
		
		public virtual T VisitPrimitiveType (PrimitiveType primitiveType)
		{
			return VisitChildren (primitiveType);
		}
		
		public virtual T VisitComposedType (ComposedType composedType)
		{
			return VisitChildren (composedType);
		}
		
		public virtual T VisitSimpleType(SimpleType simpleType)
		{
			return VisitChildren (simpleType);
		}
		
		public virtual T VisitMemberType(MemberType memberType)
		{
			return VisitChildren (memberType);
		}
		
		public virtual T VisitAttribute (Attribute attribute)
		{
			return VisitChildren (attribute);
		}
		
		public virtual T VisitAttributeSection (AttributeSection attributeSection)
		{
			return VisitChildren (attributeSection);
		}
		
		public virtual T VisitDelegateDeclaration (DelegateDeclaration delegateDeclaration)
		{
			return VisitChildren (delegateDeclaration);
		}
		
		public virtual T VisitNamespaceDeclaration (NamespaceDeclaration namespaceDeclaration)
		{
			return VisitChildren (namespaceDeclaration);
		}
		
		public virtual T VisitTypeDeclaration (TypeDeclaration typeDeclaration)
		{
			return VisitChildren (typeDeclaration);
		}
		
		public virtual T VisitTypeParameterDeclaration (TypeParameterDeclaration typeParameterDeclaration)
		{
			return VisitChildren (typeParameterDeclaration);
		}
		
		public virtual T VisitEnumMemberDeclaration (EnumMemberDeclaration enumMemberDeclaration)
		{
			return VisitChildren (enumMemberDeclaration);
		}
		
		public virtual T VisitUsingDeclaration (UsingDeclaration usingDeclaration)
		{
			return VisitChildren (usingDeclaration);
		}
		
		public virtual T VisitUsingAliasDeclaration (UsingAliasDeclaration usingDeclaration)
		{
			return VisitChildren (usingDeclaration);
		}
		
		public virtual T VisitExternAliasDeclaration(ExternAliasDeclaration externAliasDeclaration)
		{
			return VisitChildren (externAliasDeclaration);
		}
		
		public virtual T VisitConstructorDeclaration (ConstructorDeclaration constructorDeclaration)
		{
			return VisitChildren (constructorDeclaration);
		}
		
		public virtual T VisitConstructorInitializer (ConstructorInitializer constructorInitializer)
		{
			return VisitChildren (constructorInitializer);
		}
		
		public virtual T VisitDestructorDeclaration (DestructorDeclaration destructorDeclaration)
		{
			return VisitChildren (destructorDeclaration);
		}
		
		public virtual T VisitEventDeclaration (EventDeclaration eventDeclaration)
		{
			return VisitChildren (eventDeclaration);
		}
		
		public virtual T VisitCustomEventDeclaration (CustomEventDeclaration eventDeclaration)
		{
			return VisitChildren (eventDeclaration);
		}
		
		public virtual T VisitFieldDeclaration (FieldDeclaration fieldDeclaration)
		{
			return VisitChildren (fieldDeclaration);
		}
		
		public virtual T VisitFixedFieldDeclaration (FixedFieldDeclaration fixedFieldDeclaration)
		{
			return VisitChildren (fixedFieldDeclaration);
		}
		
		public virtual T VisitFixedVariableInitializer (FixedVariableInitializer fixedVariableInitializer)
		{
			return VisitChildren (fixedVariableInitializer);
		}
		
		public virtual T VisitIndexerDeclaration (IndexerDeclaration indexerDeclaration)
		{
			return VisitChildren (indexerDeclaration);
		}
		
		public virtual T VisitMethodDeclaration (MethodDeclaration methodDeclaration)
		{
			return VisitChildren (methodDeclaration);
		}
		
		public virtual T VisitOperatorDeclaration (OperatorDeclaration operatorDeclaration)
		{
			return VisitChildren (operatorDeclaration);
		}
		
		public virtual T VisitPropertyDeclaration (PropertyDeclaration propertyDeclaration)
		{
			return VisitChildren (propertyDeclaration);
		}
		
		public virtual T VisitAccessor (Accessor accessor)
		{
			return VisitChildren (accessor);
		}
		
		public virtual T VisitVariableInitializer (VariableInitializer variableInitializer)
		{
			return VisitChildren (variableInitializer);
		}
		
		public virtual T VisitParameterDeclaration (ParameterDeclaration parameterDeclaration)
		{
			return VisitChildren (parameterDeclaration);
		}
		
		public virtual T VisitConstraint (Constraint constraint)
		{
			return VisitChildren (constraint);
		}
		
		public virtual T VisitBlockStatement (BlockStatement blockStatement)
		{
			return VisitChildren (blockStatement);
		}
		
		public virtual T VisitExpressionStatement (ExpressionStatement expressionStatement)
		{
			return VisitChildren (expressionStatement);
		}
		
		public virtual T VisitBreakStatement (BreakStatement breakStatement)
		{
			return VisitChildren (breakStatement);
		}
		
		public virtual T VisitCheckedStatement (CheckedStatement checkedStatement)
		{
			return VisitChildren (checkedStatement);
		}
		
		public virtual T VisitContinueStatement (ContinueStatement continueStatement)
		{
			return VisitChildren (continueStatement);
		}
		
		public virtual T VisitDoWhileStatement (DoWhileStatement doWhileStatement)
		{
			return VisitChildren (doWhileStatement);
		}
		
		public virtual T VisitEmptyStatement (EmptyStatement emptyStatement)
		{
			return VisitChildren (emptyStatement);
		}
		
		public virtual T VisitFixedStatement (FixedStatement fixedStatement)
		{
			return VisitChildren (fixedStatement);
		}
		
		public virtual T VisitForeachStatement (ForeachStatement foreachStatement)
		{
			return VisitChildren (foreachStatement);
		}
		
		public virtual T VisitForStatement (ForStatement forStatement)
		{
			return VisitChildren (forStatement);
		}
		
		public virtual T VisitGotoCaseStatement (GotoCaseStatement gotoCaseStatement)
		{
			return VisitChildren (gotoCaseStatement);
		}
		
		public virtual T VisitGotoDefaultStatement (GotoDefaultStatement gotoDefaultStatement)
		{
			return VisitChildren (gotoDefaultStatement);
		}
		
		public virtual T VisitGotoStatement (GotoStatement gotoStatement)
		{
			return VisitChildren (gotoStatement);
		}
		
		public virtual T VisitIfElseStatement (IfElseStatement ifElseStatement)
		{
			return VisitChildren (ifElseStatement);
		}
		
		public virtual T VisitLabelStatement (LabelStatement labelStatement)
		{
			return VisitChildren (labelStatement);
		}
		
		public virtual T VisitLockStatement (LockStatement lockStatement)
		{
			return VisitChildren (lockStatement);
		}
		
		public virtual T VisitReturnStatement (ReturnStatement returnStatement)
		{
			return VisitChildren (returnStatement);
		}
		
		public virtual T VisitSwitchStatement (SwitchStatement switchStatement)
		{
			return VisitChildren (switchStatement);
		}
		
		public virtual T VisitSwitchSection (SwitchSection switchSection)
		{
			return VisitChildren (switchSection);
		}
		
		public virtual T VisitCaseLabel (CaseLabel caseLabel)
		{
			return VisitChildren (caseLabel);
		}
		
		public virtual T VisitThrowStatement (ThrowStatement throwStatement)
		{
			return VisitChildren (throwStatement);
		}
		
		public virtual T VisitTryCatchStatement (TryCatchStatement tryCatchStatement)
		{
			return VisitChildren (tryCatchStatement);
		}
		
		public virtual T VisitCatchClause (CatchClause catchClause)
		{
			return VisitChildren (catchClause);
		}
		
		public virtual T VisitUncheckedStatement (UncheckedStatement uncheckedStatement)
		{
			return VisitChildren (uncheckedStatement);
		}
		
		public virtual T VisitUnsafeStatement (UnsafeStatement unsafeStatement)
		{
			return VisitChildren (unsafeStatement);
		}
		
		public virtual T VisitUsingStatement (UsingStatement usingStatement)
		{
			return VisitChildren (usingStatement);
		}
		
		public virtual T VisitVariableDeclarationStatement (VariableDeclarationStatement variableDeclarationStatement)
		{
			return VisitChildren (variableDeclarationStatement);
		}
		
		public virtual T VisitWhileStatement (WhileStatement whileStatement)
		{
			return VisitChildren (whileStatement);
		}
		
		public virtual T VisitYieldBreakStatement (YieldBreakStatement yieldBreakStatement)
		{
			return VisitChildren (yieldBreakStatement);
		}
		
		public virtual T VisitYieldReturnStatement (YieldReturnStatement yieldReturnStatement)
		{
			return VisitChildren (yieldReturnStatement);
		}
		
		public virtual T VisitAnonymousMethodExpression (AnonymousMethodExpression anonymousMethodExpression)
		{
			return VisitChildren (anonymousMethodExpression);
		}
		
		public virtual T VisitLambdaExpression (LambdaExpression lambdaExpression)
		{
			return VisitChildren (lambdaExpression);
		}
		
		public virtual T VisitAssignmentExpression (AssignmentExpression assignmentExpression)
		{
			return VisitChildren (assignmentExpression);
		}
		
		public virtual T VisitBaseReferenceExpression (BaseReferenceExpression baseReferenceExpression)
		{
			return VisitChildren (baseReferenceExpression);
		}
		
		public virtual T VisitBinaryOperatorExpression (BinaryOperatorExpression binaryOperatorExpression)
		{
			return VisitChildren (binaryOperatorExpression);
		}
		
		public virtual T VisitCastExpression (CastExpression castExpression)
		{
			return VisitChildren (castExpression);
		}
		
		public virtual T VisitCheckedExpression (CheckedExpression checkedExpression)
		{
			return VisitChildren (checkedExpression);
		}
		
		public virtual T VisitConditionalExpression (ConditionalExpression conditionalExpression)
		{
			return VisitChildren (conditionalExpression);
		}
		
		public virtual T VisitIdentifierExpression (IdentifierExpression identifierExpression)
		{
			return VisitChildren (identifierExpression);
		}
		
		public virtual T VisitIndexerExpression (IndexerExpression indexerExpression)
		{
			return VisitChildren (indexerExpression);
		}
		
		public virtual T VisitInvocationExpression (InvocationExpression invocationExpression)
		{
			return VisitChildren (invocationExpression);
		}
		
		public virtual T VisitDirectionExpression (DirectionExpression directionExpression)
		{
			return VisitChildren (directionExpression);
		}
		
		public virtual T VisitMemberReferenceExpression (MemberReferenceExpression memberReferenceExpression)
		{
			return VisitChildren (memberReferenceExpression);
		}
		
		public virtual T VisitNullReferenceExpression (NullReferenceExpression nullReferenceExpression)
		{
			return VisitChildren (nullReferenceExpression);
		}
		
		public virtual T VisitObjectCreateExpression (ObjectCreateExpression objectCreateExpression)
		{
			return VisitChildren (objectCreateExpression);
		}
		
		public virtual T VisitAnonymousTypeCreateExpression(AnonymousTypeCreateExpression anonymousTypeCreateExpression)
		{
			return VisitChildren (anonymousTypeCreateExpression);
		}
		
		public virtual T VisitArrayCreateExpression (ArrayCreateExpression arrayCreateExpression)
		{
			return VisitChildren (arrayCreateExpression);
		}
		
		public virtual T VisitParenthesizedExpression (ParenthesizedExpression parenthesizedExpression)
		{
			return VisitChildren (parenthesizedExpression);
		}
		
		public virtual T VisitPointerReferenceExpression (PointerReferenceExpression pointerReferenceExpression)
		{
			return VisitChildren (pointerReferenceExpression);
		}
		
		public virtual T VisitPrimitiveExpression(PrimitiveExpression primitiveExpression)
		{
			return VisitChildren (primitiveExpression);
		}
		
		public virtual T VisitSizeOfExpression (SizeOfExpression sizeOfExpression)
		{
			return VisitChildren (sizeOfExpression);
		}
		
		public virtual T VisitStackAllocExpression (StackAllocExpression stackAllocExpression)
		{
			return VisitChildren (stackAllocExpression);
		}
		
		public virtual T VisitThisReferenceExpression(ThisReferenceExpression thisReferenceExpression)
		{
			return VisitChildren (thisReferenceExpression);
		}
		
		public virtual T VisitTypeOfExpression (TypeOfExpression typeOfExpression)
		{
			return VisitChildren (typeOfExpression);
		}
		
		public virtual T VisitTypeReferenceExpression(TypeReferenceExpression typeReferenceExpression)
		{
			return VisitChildren (typeReferenceExpression);
		}
		
		public virtual T VisitUnaryOperatorExpression (UnaryOperatorExpression unaryOperatorExpression)
		{
			return VisitChildren (unaryOperatorExpression);
		}
		
		public virtual T VisitUncheckedExpression (UncheckedExpression uncheckedExpression)
		{
			return VisitChildren (uncheckedExpression);
		}
		
		public virtual T VisitQueryExpression(QueryExpression queryExpression)
		{
			return VisitChildren (queryExpression);
		}
		
		public virtual T VisitQueryContinuationClause(QueryContinuationClause queryContinuationClause)
		{
			return VisitChildren (queryContinuationClause);
		}
		
		public virtual T VisitQueryFromClause(QueryFromClause queryFromClause)
		{
			return VisitChildren (queryFromClause);
		}
		
		public virtual T VisitQueryLetClause(QueryLetClause queryLetClause)
		{
			return VisitChildren (queryLetClause);
		}
		
		public virtual T VisitQueryWhereClause(QueryWhereClause queryWhereClause)
		{
			return VisitChildren (queryWhereClause);
		}
		
		public virtual T VisitQueryJoinClause(QueryJoinClause queryJoinClause)
		{
			return VisitChildren (queryJoinClause);
		}
		
		public virtual T VisitQueryOrderClause(QueryOrderClause queryOrderClause)
		{
			return VisitChildren (queryOrderClause);
		}
		
		public virtual T VisitQueryOrdering(QueryOrdering queryOrdering)
		{
			return VisitChildren (queryOrdering);
		}
		
		public virtual T VisitQuerySelectClause(QuerySelectClause querySelectClause)
		{
			return VisitChildren (querySelectClause);
		}
		
		public virtual T VisitQueryGroupClause(QueryGroupClause queryGroupClause)
		{
			return VisitChildren (queryGroupClause);
		}
		
		public virtual T VisitAsExpression (AsExpression asExpression)
		{
			return VisitChildren (asExpression);
		}
		
		public virtual T VisitIsExpression (IsExpression isExpression)
		{
			return VisitChildren (isExpression);
		}
		
		public virtual T VisitDefaultValueExpression (DefaultValueExpression defaultValueExpression)
		{
			return VisitChildren (defaultValueExpression);
		}
		
		public virtual T VisitUndocumentedExpression (UndocumentedExpression undocumentedExpression)
		{
			return VisitChildren (undocumentedExpression);
		}
		
		public virtual T VisitArrayInitializerExpression (ArrayInitializerExpression arrayInitializerExpression)
		{
			return VisitChildren (arrayInitializerExpression);
		}
		
		public virtual T VisitArraySpecifier (ArraySpecifier arraySpecifier)
		{
			return VisitChildren (arraySpecifier);
		}
		
		public virtual T VisitNamedArgumentExpression (NamedArgumentExpression namedArgumentExpression)
		{
			return VisitChildren (namedArgumentExpression);
		}
		
		public virtual T VisitNamedExpression (NamedExpression namedExpression)
		{
			return VisitChildren (namedExpression);
		}
		
		public virtual T VisitEmptyExpression (EmptyExpression emptyExpression)
		{
			return VisitChildren (emptyExpression);
		}
		
		public virtual T VisitPatternPlaceholder(AstNode placeholder, PatternMatching.Pattern pattern)
		{
			return VisitChildren (placeholder);
		}
	}
	
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
		
		public virtual S VisitNewLine(NewLineNode newLineNode, T data)
		{
			return VisitChildren(newLineNode, data);
		}

		public virtual S VisitWhitespace(WhitespaceNode whitespaceNode, T data)
		{
			return VisitChildren(whitespaceNode, data);
		}

		public virtual S VisitText(TextNode textNode, T data)
		{
			return VisitChildren(textNode, data);
		}

		public virtual S VisitDocumentationReference (DocumentationReference documentationReference, T data)
		{
			return VisitChildren (documentationReference, data);
		}
		
		public virtual S VisitPreProcessorDirective (PreProcessorDirective preProcessorDirective, T data)
		{
			return VisitChildren (preProcessorDirective, data);
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
