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
	public class ObservableAstVisitor<T, S>: IAstVisitor<T, S>
	{
		S VisitChildren (AstNode node, T data)
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
		
		public event Action<CompilationUnit, T> CompilationUnitVisited;

		S IAstVisitor<T, S>.VisitCompilationUnit (CompilationUnit unit, T data)
		{
			var handler = CompilationUnitVisited;
			if (handler != null)
				handler (unit, data);
			return VisitChildren (unit, data);
		}
		
		public event Action<Comment, T> CommentVisited;

		S IAstVisitor<T, S>.VisitComment (Comment comment, T data)
		{
			var handler = CommentVisited;
			if (handler != null)
				handler (comment, data);
			return VisitChildren (comment, data);
		}
		
		public event Action<Identifier, T> IdentifierVisited;

		S IAstVisitor<T, S>.VisitIdentifier (Identifier identifier, T data)
		{
			var handler = IdentifierVisited;
			if (handler != null)
				handler (identifier, data);
			return VisitChildren (identifier, data);
		}
		
		public event Action<CSharpTokenNode, T> CSharpTokenNodeVisited;

		S IAstVisitor<T, S>.VisitCSharpTokenNode (CSharpTokenNode token, T data)
		{
			var handler = CSharpTokenNodeVisited;
			if (handler != null)
				handler (token, data);
			return VisitChildren (token, data);
		}
		
		public event Action<PrimitiveType, T> PrimitiveTypeVisited;

		S IAstVisitor<T, S>.VisitPrimitiveType (PrimitiveType primitiveType, T data)
		{
			var handler = PrimitiveTypeVisited;
			if (handler != null)
				handler (primitiveType, data);
			return VisitChildren (primitiveType, data);
		}
		
		public event Action<ComposedType, T> ComposedTypeVisited;

		S IAstVisitor<T, S>.VisitComposedType (ComposedType composedType, T data)
		{
			var handler = ComposedTypeVisited;
			if (handler != null)
				handler (composedType, data);
			return VisitChildren (composedType, data);
		}
		
		public event Action<SimpleType, T> SimpleTypeVisited;

		S IAstVisitor<T, S>.VisitSimpleType (SimpleType simpleType, T data)
		{
			var handler = SimpleTypeVisited;
			if (handler != null)
				handler (simpleType, data);
			return VisitChildren (simpleType, data);
		}
		
		public event Action<MemberType, T> MemberTypeVisited;

		S IAstVisitor<T, S>.VisitMemberType (MemberType memberType, T data)
		{
			var handler = MemberTypeVisited;
			if (handler != null)
				handler (memberType, data);
			return VisitChildren (memberType, data);
		}
		
		public event Action<Attribute, T> AttributeVisited;

		S IAstVisitor<T, S>.VisitAttribute (Attribute attribute, T data)
		{
			var handler = AttributeVisited;
			if (handler != null)
				handler (attribute, data);
			return VisitChildren (attribute, data);
		}
		
		public event Action<AttributeSection, T> AttributeSectionVisited;

		S IAstVisitor<T, S>.VisitAttributeSection (AttributeSection attributeSection, T data)
		{
			var handler = AttributeSectionVisited;
			if (handler != null)
				handler (attributeSection, data);
			return VisitChildren (attributeSection, data);
		}
		
		public event Action<DelegateDeclaration, T> DelegateDeclarationVisited;

		S IAstVisitor<T, S>.VisitDelegateDeclaration (DelegateDeclaration delegateDeclaration, T data)
		{
			var handler = DelegateDeclarationVisited;
			if (handler != null)
				handler (delegateDeclaration, data);
			return VisitChildren (delegateDeclaration, data);
		}
		
		public event Action<NamespaceDeclaration, T> NamespaceDeclarationVisited;

		S IAstVisitor<T, S>.VisitNamespaceDeclaration (NamespaceDeclaration namespaceDeclaration, T data)
		{
			var handler = NamespaceDeclarationVisited;
			if (handler != null)
				handler (namespaceDeclaration, data);
			return VisitChildren (namespaceDeclaration, data);
		}
		
		public event Action<TypeDeclaration, T> TypeDeclarationVisited;

		S IAstVisitor<T, S>.VisitTypeDeclaration (TypeDeclaration typeDeclaration, T data)
		{
			var handler = TypeDeclarationVisited;
			if (handler != null)
				handler (typeDeclaration, data);
			return VisitChildren (typeDeclaration, data);
		}
		
		public event Action<TypeParameterDeclaration, T> TypeParameterDeclarationVisited;

		S IAstVisitor<T, S>.VisitTypeParameterDeclaration (TypeParameterDeclaration typeParameterDeclaration, T data)
		{
			var handler = TypeParameterDeclarationVisited;
			if (handler != null)
				handler (typeParameterDeclaration, data);
			return VisitChildren (typeParameterDeclaration, data);
		}
		
		public event Action<EnumMemberDeclaration, T> EnumMemberDeclarationVisited;

		S IAstVisitor<T, S>.VisitEnumMemberDeclaration (EnumMemberDeclaration enumMemberDeclaration, T data)
		{
			var handler = EnumMemberDeclarationVisited;
			if (handler != null)
				handler (enumMemberDeclaration, data);
			return VisitChildren (enumMemberDeclaration, data);
		}
		
		public event Action<UsingDeclaration, T> UsingDeclarationVisited;

		S IAstVisitor<T, S>.VisitUsingDeclaration (UsingDeclaration usingDeclaration, T data)
		{
			var handler = UsingDeclarationVisited;
			if (handler != null)
				handler (usingDeclaration, data);
			return VisitChildren (usingDeclaration, data);
		}
		
		public event Action<UsingAliasDeclaration, T> UsingAliasDeclarationVisited;

		S IAstVisitor<T, S>.VisitUsingAliasDeclaration (UsingAliasDeclaration usingDeclaration, T data)
		{
			var handler = UsingAliasDeclarationVisited;
			if (handler != null)
				handler (usingDeclaration, data);
			return VisitChildren (usingDeclaration, data);
		}
		
		public event Action<ExternAliasDeclaration, T> ExternAliasDeclarationVisited;

		S IAstVisitor<T, S>.VisitExternAliasDeclaration (ExternAliasDeclaration externAliasDeclaration, T data)
		{
			var handler = ExternAliasDeclarationVisited;
			if (handler != null)
				handler (externAliasDeclaration, data);
			return VisitChildren (externAliasDeclaration, data);
		}
		
		public event Action<ConstructorDeclaration, T> ConstructorDeclarationVisited;

		S IAstVisitor<T, S>.VisitConstructorDeclaration (ConstructorDeclaration constructorDeclaration, T data)
		{
			var handler = ConstructorDeclarationVisited;
			if (handler != null)
				handler (constructorDeclaration, data);
			return VisitChildren (constructorDeclaration, data);
		}
		
		public event Action<ConstructorInitializer, T> ConstructorInitializerVisited;

		S IAstVisitor<T, S>.VisitConstructorInitializer (ConstructorInitializer constructorInitializer, T data)
		{
			var handler = ConstructorInitializerVisited;
			if (handler != null)
				handler (constructorInitializer, data);
			return VisitChildren (constructorInitializer, data);
		}
		
		public event Action<DestructorDeclaration, T> DestructorDeclarationVisited;

		S IAstVisitor<T, S>.VisitDestructorDeclaration (DestructorDeclaration destructorDeclaration, T data)
		{
			var handler = DestructorDeclarationVisited;
			if (handler != null)
				handler (destructorDeclaration, data);
			return VisitChildren (destructorDeclaration, data);
		}
		
		public event Action<EventDeclaration, T> EventDeclarationVisited;

		S IAstVisitor<T, S>.VisitEventDeclaration (EventDeclaration eventDeclaration, T data)
		{
			var handler = EventDeclarationVisited;
			if (handler != null)
				handler (eventDeclaration, data);
			return VisitChildren (eventDeclaration, data);
		}
		
		public event Action<CustomEventDeclaration, T> CustomEventDeclarationVisited;

		S IAstVisitor<T, S>.VisitCustomEventDeclaration (CustomEventDeclaration eventDeclaration, T data)
		{
			var handler = CustomEventDeclarationVisited;
			if (handler != null)
				handler (eventDeclaration, data);
			return VisitChildren (eventDeclaration, data);
		}
		
		public event Action<FieldDeclaration, T> FieldDeclarationVisited;

		S IAstVisitor<T, S>.VisitFieldDeclaration (FieldDeclaration fieldDeclaration, T data)
		{
			var handler = FieldDeclarationVisited;
			if (handler != null)
				handler (fieldDeclaration, data);
			return VisitChildren (fieldDeclaration, data);
		}
		
		public event Action<FixedFieldDeclaration, T> FixedFieldDeclarationVisited;

		S IAstVisitor<T, S>.VisitFixedFieldDeclaration (FixedFieldDeclaration fixedFieldDeclaration, T data)
		{
			var handler = FixedFieldDeclarationVisited;
			if (handler != null)
				handler (fixedFieldDeclaration, data);
			return VisitChildren (fixedFieldDeclaration, data);
		}
		
		public event Action<FixedVariableInitializer, T> FixedVariableInitializerVisited;

		S IAstVisitor<T, S>.VisitFixedVariableInitializer (FixedVariableInitializer fixedVariableInitializer, T data)
		{
			var handler = FixedVariableInitializerVisited;
			if (handler != null)
				handler (fixedVariableInitializer, data);
			return VisitChildren (fixedVariableInitializer, data);
		}
		
		public event Action<IndexerDeclaration, T> IndexerDeclarationVisited;

		S IAstVisitor<T, S>.VisitIndexerDeclaration (IndexerDeclaration indexerDeclaration, T data)
		{
			var handler = IndexerDeclarationVisited;
			if (handler != null)
				handler (indexerDeclaration, data);
			return VisitChildren (indexerDeclaration, data);
		}
		
		public event Action<MethodDeclaration, T> MethodDeclarationVisited;

		S IAstVisitor<T, S>.VisitMethodDeclaration (MethodDeclaration methodDeclaration, T data)
		{
			var handler = MethodDeclarationVisited;
			if (handler != null)
				handler (methodDeclaration, data);
			return VisitChildren (methodDeclaration, data);
		}
		
		public event Action<OperatorDeclaration, T> OperatorDeclarationVisited;

		S IAstVisitor<T, S>.VisitOperatorDeclaration (OperatorDeclaration operatorDeclaration, T data)
		{
			var handler = OperatorDeclarationVisited;
			if (handler != null)
				handler (operatorDeclaration, data);
			return VisitChildren (operatorDeclaration, data);
		}
		
		public event Action<PropertyDeclaration, T> PropertyDeclarationVisited;

		S IAstVisitor<T, S>.VisitPropertyDeclaration (PropertyDeclaration propertyDeclaration, T data)
		{
			var handler = PropertyDeclarationVisited;
			if (handler != null)
				handler (propertyDeclaration, data);
			return VisitChildren (propertyDeclaration, data);
		}
		
		public event Action<Accessor, T> AccessorVisited;

		S IAstVisitor<T, S>.VisitAccessor (Accessor accessor, T data)
		{
			var handler = AccessorVisited;
			if (handler != null)
				handler (accessor, data);
			return VisitChildren (accessor, data);
		}
		
		public event Action<VariableInitializer, T> VariableInitializerVisited;

		S IAstVisitor<T, S>.VisitVariableInitializer (VariableInitializer variableInitializer, T data)
		{
			var handler = VariableInitializerVisited;
			if (handler != null)
				handler (variableInitializer, data);
			return VisitChildren (variableInitializer, data);
		}
		
		public event Action<ParameterDeclaration, T> ParameterDeclarationVisited;

		S IAstVisitor<T, S>.VisitParameterDeclaration (ParameterDeclaration parameterDeclaration, T data)
		{
			var handler = ParameterDeclarationVisited;
			if (handler != null)
				handler (parameterDeclaration, data);
			return VisitChildren (parameterDeclaration, data);
		}
		
		public event Action<Constraint, T> ConstraintVisited;

		S IAstVisitor<T, S>.VisitConstraint (Constraint constraint, T data)
		{
			var handler = ConstraintVisited;
			if (handler != null)
				handler (constraint, data);
			return VisitChildren (constraint, data);
		}
		
		public event Action<BlockStatement, T> BlockStatementVisited;

		S IAstVisitor<T, S>.VisitBlockStatement (BlockStatement blockStatement, T data)
		{
			var handler = BlockStatementVisited;
			if (handler != null)
				handler (blockStatement, data);
			return VisitChildren (blockStatement, data);
		}
		
		public event Action<ExpressionStatement, T> ExpressionStatementVisited;

		S IAstVisitor<T, S>.VisitExpressionStatement (ExpressionStatement expressionStatement, T data)
		{
			var handler = ExpressionStatementVisited;
			if (handler != null)
				handler (expressionStatement, data);
			return VisitChildren (expressionStatement, data);
		}
		
		public event Action<BreakStatement, T> BreakStatementVisited;

		S IAstVisitor<T, S>.VisitBreakStatement (BreakStatement breakStatement, T data)
		{
			var handler = BreakStatementVisited;
			if (handler != null)
				handler (breakStatement, data);
			return VisitChildren (breakStatement, data);
		}
		
		public event Action<CheckedStatement, T> CheckedStatementVisited;

		S IAstVisitor<T, S>.VisitCheckedStatement (CheckedStatement checkedStatement, T data)
		{
			var handler = CheckedStatementVisited;
			if (handler != null)
				handler (checkedStatement, data);
			return VisitChildren (checkedStatement, data);
		}
		
		public event Action<ContinueStatement, T> ContinueStatementVisited;

		S IAstVisitor<T, S>.VisitContinueStatement (ContinueStatement continueStatement, T data)
		{
			var handler = ContinueStatementVisited;
			if (handler != null)
				handler (continueStatement, data);
			return VisitChildren (continueStatement, data);
		}
		
		public event Action<DoWhileStatement, T> DoWhileStatementVisited;

		S IAstVisitor<T, S>.VisitDoWhileStatement (DoWhileStatement doWhileStatement, T data)
		{
			var handler = DoWhileStatementVisited;
			if (handler != null)
				handler (doWhileStatement, data);
			return VisitChildren (doWhileStatement, data);
		}
		
		public event Action<EmptyStatement, T> EmptyStatementVisited;

		S IAstVisitor<T, S>.VisitEmptyStatement (EmptyStatement emptyStatement, T data)
		{
			var handler = EmptyStatementVisited;
			if (handler != null)
				handler (emptyStatement, data);
			return VisitChildren (emptyStatement, data);
		}
		
		public event Action<FixedStatement, T> FixedStatementVisited;

		S IAstVisitor<T, S>.VisitFixedStatement (FixedStatement fixedStatement, T data)
		{
			var handler = FixedStatementVisited;
			if (handler != null)
				handler (fixedStatement, data);
			return VisitChildren (fixedStatement, data);
		}
		
		public event Action<ForeachStatement, T> ForeachStatementVisited;

		S IAstVisitor<T, S>.VisitForeachStatement (ForeachStatement foreachStatement, T data)
		{
			var handler = ForeachStatementVisited;
			if (handler != null)
				handler (foreachStatement, data);
			return VisitChildren (foreachStatement, data);
		}
		
		public event Action<ForStatement, T> ForStatementVisited;

		S IAstVisitor<T, S>.VisitForStatement (ForStatement forStatement, T data)
		{
			var handler = ForStatementVisited;
			if (handler != null)
				handler (forStatement, data);
			return VisitChildren (forStatement, data);
		}
		
		public event Action<GotoCaseStatement, T> GotoCaseStatementVisited;

		S IAstVisitor<T, S>.VisitGotoCaseStatement (GotoCaseStatement gotoCaseStatement, T data)
		{
			var handler = GotoCaseStatementVisited;
			if (handler != null)
				handler (gotoCaseStatement, data);
			return VisitChildren (gotoCaseStatement, data);
		}
		
		public event Action<GotoDefaultStatement, T> GotoDefaultStatementVisited;

		S IAstVisitor<T, S>.VisitGotoDefaultStatement (GotoDefaultStatement gotoDefaultStatement, T data)
		{
			var handler = GotoDefaultStatementVisited;
			if (handler != null)
				handler (gotoDefaultStatement, data);
			return VisitChildren (gotoDefaultStatement, data);
		}
		
		public event Action<GotoStatement, T> GotoStatementVisited;

		S IAstVisitor<T, S>.VisitGotoStatement (GotoStatement gotoStatement, T data)
		{
			var handler = GotoStatementVisited;
			if (handler != null)
				handler (gotoStatement, data);
			return VisitChildren (gotoStatement, data);
		}
		
		public event Action<IfElseStatement, T> IfElseStatementVisited;

		S IAstVisitor<T, S>.VisitIfElseStatement (IfElseStatement ifElseStatement, T data)
		{
			var handler = IfElseStatementVisited;
			if (handler != null)
				handler (ifElseStatement, data);
			return VisitChildren (ifElseStatement, data);
		}
		
		public event Action<LabelStatement, T> LabelStatementVisited;

		S IAstVisitor<T, S>.VisitLabelStatement (LabelStatement labelStatement, T data)
		{
			var handler = LabelStatementVisited;
			if (handler != null)
				handler (labelStatement, data);
			return VisitChildren (labelStatement, data);
		}
		
		public event Action<LockStatement, T> LockStatementVisited;

		S IAstVisitor<T, S>.VisitLockStatement (LockStatement lockStatement, T data)
		{
			var handler = LockStatementVisited;
			if (handler != null)
				handler (lockStatement, data);
			return VisitChildren (lockStatement, data);
		}
		
		public event Action<ReturnStatement, T> ReturnStatementVisited;

		S IAstVisitor<T, S>.VisitReturnStatement (ReturnStatement returnStatement, T data)
		{
			var handler = ReturnStatementVisited;
			if (handler != null)
				handler (returnStatement, data);
			return VisitChildren (returnStatement, data);
		}
		
		public event Action<SwitchStatement, T> SwitchStatementVisited;

		S IAstVisitor<T, S>.VisitSwitchStatement (SwitchStatement switchStatement, T data)
		{
			var handler = SwitchStatementVisited;
			if (handler != null)
				handler (switchStatement, data);
			return VisitChildren (switchStatement, data);
		}
		
		public event Action<SwitchSection, T> SwitchSectionVisited;

		S IAstVisitor<T, S>.VisitSwitchSection (SwitchSection switchSection, T data)
		{
			var handler = SwitchSectionVisited;
			if (handler != null)
				handler (switchSection, data);
			return VisitChildren (switchSection, data);
		}
		
		public event Action<CaseLabel, T> CaseLabelVisited;

		S IAstVisitor<T, S>.VisitCaseLabel (CaseLabel caseLabel, T data)
		{
			var handler = CaseLabelVisited;
			if (handler != null)
				handler (caseLabel, data);
			return VisitChildren (caseLabel, data);
		}
		
		public event Action<ThrowStatement, T> ThrowStatementVisited;

		S IAstVisitor<T, S>.VisitThrowStatement (ThrowStatement throwStatement, T data)
		{
			var handler = ThrowStatementVisited;
			if (handler != null)
				handler (throwStatement, data);
			return VisitChildren (throwStatement, data);
		}
		
		public event Action<TryCatchStatement, T> TryCatchStatementVisited;

		S IAstVisitor<T, S>.VisitTryCatchStatement (TryCatchStatement tryCatchStatement, T data)
		{
			var handler = TryCatchStatementVisited;
			if (handler != null)
				handler (tryCatchStatement, data);
			return VisitChildren (tryCatchStatement, data);
		}
		
		public event Action<CatchClause, T> CatchClauseVisited;

		S IAstVisitor<T, S>.VisitCatchClause (CatchClause catchClause, T data)
		{
			var handler = CatchClauseVisited;
			if (handler != null)
				handler (catchClause, data);
			return VisitChildren (catchClause, data);
		}
		
		public event Action<UncheckedStatement, T> UncheckedStatementVisited;

		S IAstVisitor<T, S>.VisitUncheckedStatement (UncheckedStatement uncheckedStatement, T data)
		{
			var handler = UncheckedStatementVisited;
			if (handler != null)
				handler (uncheckedStatement, data);
			return VisitChildren (uncheckedStatement, data);
		}
		
		public event Action<UnsafeStatement, T> UnsafeStatementVisited;

		S IAstVisitor<T, S>.VisitUnsafeStatement (UnsafeStatement unsafeStatement, T data)
		{
			var handler = UnsafeStatementVisited;
			if (handler != null)
				handler (unsafeStatement, data);
			return VisitChildren (unsafeStatement, data);
		}
		
		public event Action<UsingStatement, T> UsingStatementVisited;

		S IAstVisitor<T, S>.VisitUsingStatement (UsingStatement usingStatement, T data)
		{
			var handler = UsingStatementVisited;
			if (handler != null)
				handler (usingStatement, data);
			return VisitChildren (usingStatement, data);
		}
		
		public event Action<VariableDeclarationStatement, T> VariableDeclarationStatementVisited;

		S IAstVisitor<T, S>.VisitVariableDeclarationStatement (VariableDeclarationStatement variableDeclarationStatement, T data)
		{
			var handler = VariableDeclarationStatementVisited;
			if (handler != null)
				handler (variableDeclarationStatement, data);
			return VisitChildren (variableDeclarationStatement, data);
		}
		
		public event Action<WhileStatement, T> WhileStatementVisited;

		S IAstVisitor<T, S>.VisitWhileStatement (WhileStatement whileStatement, T data)
		{
			var handler = WhileStatementVisited;
			if (handler != null)
				handler (whileStatement, data);
			return VisitChildren (whileStatement, data);
		}
		
		public event Action<YieldBreakStatement, T> YieldBreakStatementVisited;

		S IAstVisitor<T, S>.VisitYieldBreakStatement (YieldBreakStatement yieldBreakStatement, T data)
		{
			var handler = YieldBreakStatementVisited;
			if (handler != null)
				handler (yieldBreakStatement, data);
			return VisitChildren (yieldBreakStatement, data);
		}
		
		public event Action<YieldStatement, T> YieldStatementVisited;

		S IAstVisitor<T, S>.VisitYieldStatement (YieldStatement yieldStatement, T data)
		{
			var handler = YieldStatementVisited;
			if (handler != null)
				handler (yieldStatement, data);
			return VisitChildren (yieldStatement, data);
		}
		
		public event Action<AnonymousMethodExpression, T> AnonymousMethodExpressionVisited;

		S IAstVisitor<T, S>.VisitAnonymousMethodExpression (AnonymousMethodExpression anonymousMethodExpression, T data)
		{
			var handler = AnonymousMethodExpressionVisited;
			if (handler != null)
				handler (anonymousMethodExpression, data);
			return VisitChildren (anonymousMethodExpression, data);
		}
		
		public event Action<LambdaExpression, T> LambdaExpressionVisited;

		S IAstVisitor<T, S>.VisitLambdaExpression (LambdaExpression lambdaExpression, T data)
		{
			var handler = LambdaExpressionVisited;
			if (handler != null)
				handler (lambdaExpression, data);
			return VisitChildren (lambdaExpression, data);
		}
		
		public event Action<AssignmentExpression, T> AssignmentExpressionVisited;

		S IAstVisitor<T, S>.VisitAssignmentExpression (AssignmentExpression assignmentExpression, T data)
		{
			var handler = AssignmentExpressionVisited;
			if (handler != null)
				handler (assignmentExpression, data);
			return VisitChildren (assignmentExpression, data);
		}
		
		public event Action<BaseReferenceExpression, T> BaseReferenceExpressionVisited;

		S IAstVisitor<T, S>.VisitBaseReferenceExpression (BaseReferenceExpression baseReferenceExpression, T data)
		{
			var handler = BaseReferenceExpressionVisited;
			if (handler != null)
				handler (baseReferenceExpression, data);
			return VisitChildren (baseReferenceExpression, data);
		}
		
		public event Action<BinaryOperatorExpression, T> BinaryOperatorExpressionVisited;

		S IAstVisitor<T, S>.VisitBinaryOperatorExpression (BinaryOperatorExpression binaryOperatorExpression, T data)
		{
			var handler = BinaryOperatorExpressionVisited;
			if (handler != null)
				handler (binaryOperatorExpression, data);
			return VisitChildren (binaryOperatorExpression, data);
		}
		
		public event Action<CastExpression, T> CastExpressionVisited;

		S IAstVisitor<T, S>.VisitCastExpression (CastExpression castExpression, T data)
		{
			var handler = CastExpressionVisited;
			if (handler != null)
				handler (castExpression, data);
			return VisitChildren (castExpression, data);
		}
		
		public event Action<CheckedExpression, T> CheckedExpressionVisited;

		S IAstVisitor<T, S>.VisitCheckedExpression (CheckedExpression checkedExpression, T data)
		{
			var handler = CheckedExpressionVisited;
			if (handler != null)
				handler (checkedExpression, data);
			return VisitChildren (checkedExpression, data);
		}
		
		public event Action<ConditionalExpression, T> ConditionalExpressionVisited;

		S IAstVisitor<T, S>.VisitConditionalExpression (ConditionalExpression conditionalExpression, T data)
		{
			var handler = ConditionalExpressionVisited;
			if (handler != null)
				handler (conditionalExpression, data);
			return VisitChildren (conditionalExpression, data);
		}
		
		public event Action<IdentifierExpression, T> IdentifierExpressionVisited;

		S IAstVisitor<T, S>.VisitIdentifierExpression (IdentifierExpression identifierExpression, T data)
		{
			var handler = IdentifierExpressionVisited;
			if (handler != null)
				handler (identifierExpression, data);
			return VisitChildren (identifierExpression, data);
		}
		
		public event Action<IndexerExpression, T> IndexerExpressionVisited;

		S IAstVisitor<T, S>.VisitIndexerExpression (IndexerExpression indexerExpression, T data)
		{
			var handler = IndexerExpressionVisited;
			if (handler != null)
				handler (indexerExpression, data);
			return VisitChildren (indexerExpression, data);
		}
		
		public event Action<InvocationExpression, T> InvocationExpressionVisited;

		S IAstVisitor<T, S>.VisitInvocationExpression (InvocationExpression invocationExpression, T data)
		{
			var handler = InvocationExpressionVisited;
			if (handler != null)
				handler (invocationExpression, data);
			return VisitChildren (invocationExpression, data);
		}
		
		public event Action<DirectionExpression, T> DirectionExpressionVisited;

		S IAstVisitor<T, S>.VisitDirectionExpression (DirectionExpression directionExpression, T data)
		{
			var handler = DirectionExpressionVisited;
			if (handler != null)
				handler (directionExpression, data);
			return VisitChildren (directionExpression, data);
		}
		
		public event Action<MemberReferenceExpression, T> MemberReferenceExpressionVisited;

		S IAstVisitor<T, S>.VisitMemberReferenceExpression (MemberReferenceExpression memberReferenceExpression, T data)
		{
			var handler = MemberReferenceExpressionVisited;
			if (handler != null)
				handler (memberReferenceExpression, data);
			return VisitChildren (memberReferenceExpression, data);
		}
		
		public event Action<NullReferenceExpression, T> NullReferenceExpressionVisited;

		S IAstVisitor<T, S>.VisitNullReferenceExpression (NullReferenceExpression nullReferenceExpression, T data)
		{
			var handler = NullReferenceExpressionVisited;
			if (handler != null)
				handler (nullReferenceExpression, data);
			return VisitChildren (nullReferenceExpression, data);
		}
		
		public event Action<ObjectCreateExpression, T> ObjectCreateExpressionVisited;

		S IAstVisitor<T, S>.VisitObjectCreateExpression (ObjectCreateExpression objectCreateExpression, T data)
		{
			var handler = ObjectCreateExpressionVisited;
			if (handler != null)
				handler (objectCreateExpression, data);
			return VisitChildren (objectCreateExpression, data);
		}
		
		public event Action<AnonymousTypeCreateExpression, T> AnonymousTypeCreateExpressionVisited;

		S IAstVisitor<T, S>.VisitAnonymousTypeCreateExpression (AnonymousTypeCreateExpression anonymousTypeCreateExpression, T data)
		{
			var handler = AnonymousTypeCreateExpressionVisited;
			if (handler != null)
				handler (anonymousTypeCreateExpression, data);
			return VisitChildren (anonymousTypeCreateExpression, data);
		}
		
		public event Action<ArrayCreateExpression, T> ArrayCreateExpressionVisited;

		S IAstVisitor<T, S>.VisitArrayCreateExpression (ArrayCreateExpression arraySCreateExpression, T data)
		{
			var handler = ArrayCreateExpressionVisited;
			if (handler != null)
				handler (arraySCreateExpression, data);
			return VisitChildren (arraySCreateExpression, data);
		}
		
		public event Action<ParenthesizedExpression, T> ParenthesizedExpressionVisited;

		S IAstVisitor<T, S>.VisitParenthesizedExpression (ParenthesizedExpression parenthesizedExpression, T data)
		{
			var handler = ParenthesizedExpressionVisited;
			if (handler != null)
				handler (parenthesizedExpression, data);
			return VisitChildren (parenthesizedExpression, data);
		}
		
		public event Action<PointerReferenceExpression, T> PointerReferenceExpressionVisited;

		S IAstVisitor<T, S>.VisitPointerReferenceExpression (PointerReferenceExpression pointerReferenceExpression, T data)
		{
			var handler = PointerReferenceExpressionVisited;
			if (handler != null)
				handler (pointerReferenceExpression, data);
			return VisitChildren (pointerReferenceExpression, data);
		}
		
		public event Action<PrimitiveExpression, T> PrimitiveExpressionVisited;

		S IAstVisitor<T, S>.VisitPrimitiveExpression (PrimitiveExpression primitiveExpression, T data)
		{
			var handler = PrimitiveExpressionVisited;
			if (handler != null)
				handler (primitiveExpression, data);
			return VisitChildren (primitiveExpression, data);
		}
		
		public event Action<SizeOfExpression, T> SizeOfExpressionVisited;

		S IAstVisitor<T, S>.VisitSizeOfExpression (SizeOfExpression sizeOfExpression, T data)
		{
			var handler = SizeOfExpressionVisited;
			if (handler != null)
				handler (sizeOfExpression, data);
			return VisitChildren (sizeOfExpression, data);
		}
		
		public event Action<StackAllocExpression, T> StackAllocExpressionVisited;

		S IAstVisitor<T, S>.VisitStackAllocExpression (StackAllocExpression stackAllocExpression, T data)
		{
			var handler = StackAllocExpressionVisited;
			if (handler != null)
				handler (stackAllocExpression, data);
			return VisitChildren (stackAllocExpression, data);
		}
		
		public event Action<ThisReferenceExpression, T> ThisReferenceExpressionVisited;

		S IAstVisitor<T, S>.VisitThisReferenceExpression (ThisReferenceExpression thisReferenceExpression, T data)
		{
			var handler = ThisReferenceExpressionVisited;
			if (handler != null)
				handler (thisReferenceExpression, data);
			return VisitChildren (thisReferenceExpression, data);
		}
		
		public event Action<TypeOfExpression, T> TypeOfExpressionVisited;

		S IAstVisitor<T, S>.VisitTypeOfExpression (TypeOfExpression typeOfExpression, T data)
		{
			var handler = TypeOfExpressionVisited;
			if (handler != null)
				handler (typeOfExpression, data);
			return VisitChildren (typeOfExpression, data);
		}
		
		public event Action<TypeReferenceExpression, T> TypeReferenceExpressionVisited;

		S IAstVisitor<T, S>.VisitTypeReferenceExpression (TypeReferenceExpression typeReferenceExpression, T data)
		{
			var handler = TypeReferenceExpressionVisited;
			if (handler != null)
				handler (typeReferenceExpression, data);
			return VisitChildren (typeReferenceExpression, data);
		}
		
		public event Action<UnaryOperatorExpression, T> UnaryOperatorExpressionVisited;

		S IAstVisitor<T, S>.VisitUnaryOperatorExpression (UnaryOperatorExpression unaryOperatorExpression, T data)
		{
			var handler = UnaryOperatorExpressionVisited;
			if (handler != null)
				handler (unaryOperatorExpression, data);
			return VisitChildren (unaryOperatorExpression, data);
		}
		
		public event Action<UncheckedExpression, T> UncheckedExpressionVisited;

		S IAstVisitor<T, S>.VisitUncheckedExpression (UncheckedExpression uncheckedExpression, T data)
		{
			var handler = UncheckedExpressionVisited;
			if (handler != null)
				handler (uncheckedExpression, data);
			return VisitChildren (uncheckedExpression, data);
		}
		
		public event Action<QueryExpression, T> QueryExpressionVisited;

		S IAstVisitor<T, S>.VisitQueryExpression (QueryExpression queryExpression, T data)
		{
			var handler = QueryExpressionVisited;
			if (handler != null)
				handler (queryExpression, data);
			return VisitChildren (queryExpression, data);
		}
		
		public event Action<QueryContinuationClause, T> QueryContinuationClauseVisited;

		S IAstVisitor<T, S>.VisitQueryContinuationClause (QueryContinuationClause queryContinuationClause, T data)
		{
			var handler = QueryContinuationClauseVisited;
			if (handler != null)
				handler (queryContinuationClause, data);
			return VisitChildren (queryContinuationClause, data);
		}
		
		public event Action<QueryFromClause, T> QueryFromClauseVisited;

		S IAstVisitor<T, S>.VisitQueryFromClause (QueryFromClause queryFromClause, T data)
		{
			var handler = QueryFromClauseVisited;
			if (handler != null)
				handler (queryFromClause, data);
			return VisitChildren (queryFromClause, data);
		}
		
		public event Action<QueryLetClause, T> QueryLetClauseVisited;

		S IAstVisitor<T, S>.VisitQueryLetClause (QueryLetClause queryLetClause, T data)
		{
			var handler = QueryLetClauseVisited;
			if (handler != null)
				handler (queryLetClause, data);
			return VisitChildren (queryLetClause, data);
		}
		
		public event Action<QueryWhereClause, T> QueryWhereClauseVisited;

		S IAstVisitor<T, S>.VisitQueryWhereClause (QueryWhereClause queryWhereClause, T data)
		{
			var handler = QueryWhereClauseVisited;
			if (handler != null)
				handler (queryWhereClause, data);
			return VisitChildren (queryWhereClause, data);
		}
		
		public event Action<QueryJoinClause, T> QueryJoinClauseVisited;

		S IAstVisitor<T, S>.VisitQueryJoinClause (QueryJoinClause queryJoinClause, T data)
		{
			var handler = QueryJoinClauseVisited;
			if (handler != null)
				handler (queryJoinClause, data);
			return VisitChildren (queryJoinClause, data);
		}
		
		public event Action<QueryOrderClause, T> QueryOrderClauseVisited;

		S IAstVisitor<T, S>.VisitQueryOrderClause (QueryOrderClause queryOrderClause, T data)
		{
			var handler = QueryOrderClauseVisited;
			if (handler != null)
				handler (queryOrderClause, data);
			return VisitChildren (queryOrderClause, data);
		}
		
		public event Action<QueryOrdering, T> QueryOrderingVisited;

		S IAstVisitor<T, S>.VisitQueryOrdering (QueryOrdering queryOrdering, T data)
		{
			var handler = QueryOrderingVisited;
			if (handler != null)
				handler (queryOrdering, data);
			return VisitChildren (queryOrdering, data);
		}
		
		public event Action<QuerySelectClause, T> QuerySelectClauseVisited;

		S IAstVisitor<T, S>.VisitQuerySelectClause (QuerySelectClause querySelectClause, T data)
		{
			var handler = QuerySelectClauseVisited;
			if (handler != null)
				handler (querySelectClause, data);
			return VisitChildren (querySelectClause, data);
		}
		
		public event Action<QueryGroupClause, T> QueryGroupClauseVisited;

		S IAstVisitor<T, S>.VisitQueryGroupClause (QueryGroupClause queryGroupClause, T data)
		{
			var handler = QueryGroupClauseVisited;
			if (handler != null)
				handler (queryGroupClause, data);
			return VisitChildren (queryGroupClause, data);
		}
		
		public event Action<AsExpression, T> AsExpressionVisited;

		S IAstVisitor<T, S>.VisitAsExpression (AsExpression asExpression, T data)
		{
			var handler = AsExpressionVisited;
			if (handler != null)
				handler (asExpression, data);
			return VisitChildren (asExpression, data);
		}
		
		public event Action<IsExpression, T> IsExpressionVisited;

		S IAstVisitor<T, S>.VisitIsExpression (IsExpression isExpression, T data)
		{
			var handler = IsExpressionVisited;
			if (handler != null)
				handler (isExpression, data);
			return VisitChildren (isExpression, data);
		}
		
		public event Action<DefaultValueExpression, T> DefaultValueExpressionVisited;

		S IAstVisitor<T, S>.VisitDefaultValueExpression (DefaultValueExpression defaultValueExpression, T data)
		{
			var handler = DefaultValueExpressionVisited;
			if (handler != null)
				handler (defaultValueExpression, data);
			return VisitChildren (defaultValueExpression, data);
		}
		
		public event Action<UndocumentedExpression, T> UndocumentedExpressionVisited;

		S IAstVisitor<T, S>.VisitUndocumentedExpression (UndocumentedExpression undocumentedExpression, T data)
		{
			var handler = UndocumentedExpressionVisited;
			if (handler != null)
				handler (undocumentedExpression, data);
			return VisitChildren (undocumentedExpression, data);
		}
		
		public event Action<ArrayInitializerExpression, T> ArrayInitializerExpressionVisited;

		S IAstVisitor<T, S>.VisitArrayInitializerExpression (ArrayInitializerExpression arrayInitializerExpression, T data)
		{
			var handler = ArrayInitializerExpressionVisited;
			if (handler != null)
				handler (arrayInitializerExpression, data);
			return VisitChildren (arrayInitializerExpression, data);
		}
		
		public event Action<ArraySpecifier, T> ArraySpecifierVisited;

		S IAstVisitor<T, S>.VisitArraySpecifier (ArraySpecifier arraySpecifier, T data)
		{
			var handler = ArraySpecifierVisited;
			if (handler != null)
				handler (arraySpecifier, data);
			return VisitChildren (arraySpecifier, data);
		}
		
		public event Action<NamedArgumentExpression, T> NamedArgumentExpressionVisited;

		S IAstVisitor<T, S>.VisitNamedArgumentExpression (NamedArgumentExpression namedArgumentExpression, T data)
		{
			var handler = NamedArgumentExpressionVisited;
			if (handler != null)
				handler (namedArgumentExpression, data);
			return VisitChildren (namedArgumentExpression, data);
		}
		
		public event Action<EmptyExpression, T> EmptyExpressionVisited;

		S IAstVisitor<T, S>.VisitEmptyExpression (EmptyExpression emptyExpression, T data)
		{
			var handler = EmptyExpressionVisited;
			if (handler != null)
				handler (emptyExpression, data);
			return VisitChildren (emptyExpression, data);
		}
		
		S IAstVisitor<T, S>.VisitPatternPlaceholder (AstNode placeholder, PatternMatching.Pattern pattern, T data)
		{
			return VisitChildren (placeholder, data);
		}
	}
}


