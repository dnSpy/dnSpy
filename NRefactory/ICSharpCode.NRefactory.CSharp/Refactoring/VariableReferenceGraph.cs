// 
// VariableReferenceGraph.cs
// 
// Author:
//      Mansheng Yang <lightyang0@gmail.com>
// 
// Copyright (c) 2012 Mansheng Yang <lightyang0@gmail.com>
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

using System.Collections.Generic;
using System.Linq;
using ICSharpCode.NRefactory.CSharp.Analysis;
using ICSharpCode.NRefactory.CSharp.Resolver;
using System.Threading;

namespace ICSharpCode.NRefactory.CSharp.Refactoring
{
	class VariableReferenceNode
	{
		public IList<AstNode> References {
			get;
			private set;
		}

		public IList<VariableReferenceNode> NextNodes {
			get;
			private set;
		}

		public IList<VariableReferenceNode> PreviousNodes {
			get;
			private set;
		}

		public VariableReferenceNode ()
		{
			References = new List<AstNode> ();
			NextNodes = new List<VariableReferenceNode> ();
			PreviousNodes = new List<VariableReferenceNode> ();
		}

		public void AddNextNode (VariableReferenceNode node)
		{
			if (node == null)
				return;
			NextNodes.Add (node);
			node.PreviousNodes.Add (this);
		}
	}

	class VariableReferenceGraphBuilder
	{
		ControlFlowGraphBuilder cfgBuilder = new ControlFlowGraphBuilder ();
		CfgVariableReferenceNodeBuilder cfgVrNodeBuilder;
		BaseRefactoringContext ctx;

		public VariableReferenceGraphBuilder(BaseRefactoringContext ctx)
		{
			this.ctx = ctx;
			cfgVrNodeBuilder = new CfgVariableReferenceNodeBuilder (this);
		}

		public VariableReferenceNode Build (ISet<AstNode> references, CSharpAstResolver resolver,
			Expression expression)
		{
			return ExpressionNodeCreationVisitor.CreateNode (references, resolver, new [] { expression });
		}

		public VariableReferenceNode Build (Statement statement, ISet<AstNode> references,
			ISet<Statement> refStatements, BaseRefactoringContext context)
		{
			var cfg = cfgBuilder.BuildControlFlowGraph (statement, context.Resolver, context.CancellationToken);
			if (cfg.Count == 0)
				return new VariableReferenceNode ();
			return cfgVrNodeBuilder.Build (cfg [0], references, refStatements, context.Resolver);
		}

		public VariableReferenceNode Build (Statement statement, ISet<AstNode> references,
		                                           ISet<Statement> refStatements, CSharpAstResolver resolver, CancellationToken cancellationToken = default(CancellationToken))
		{
			var cfg = cfgBuilder.BuildControlFlowGraph (statement, resolver, cancellationToken);
			if (cfg.Count == 0)
				return new VariableReferenceNode();
			return cfgVrNodeBuilder.Build (cfg [0], references, refStatements, resolver);
		}

		class GetExpressionsVisitor : DepthFirstAstVisitor<IEnumerable<Expression>>
		{

			public override IEnumerable<Expression> VisitIfElseStatement (IfElseStatement ifElseStatement)
			{
				yield return ifElseStatement.Condition;
			}

			public override IEnumerable<Expression> VisitSwitchStatement (SwitchStatement switchStatement)
			{
				yield return switchStatement.Expression;
			}

			public override IEnumerable<Expression> VisitForStatement (ForStatement forStatement)
			{
				yield return forStatement.Condition;
			}

			public override IEnumerable<Expression> VisitDoWhileStatement (DoWhileStatement doWhileStatement)
			{
				yield return doWhileStatement.Condition;
			}

			public override IEnumerable<Expression> VisitWhileStatement (WhileStatement whileStatement)
			{
				yield return whileStatement.Condition;
			}

			public override IEnumerable<Expression> VisitForeachStatement (ForeachStatement foreachStatement)
			{
				yield return foreachStatement.InExpression;
			}

			public override IEnumerable<Expression> VisitExpressionStatement (ExpressionStatement expressionStatement)
			{
				yield return expressionStatement.Expression;
			}

			public override IEnumerable<Expression> VisitLockStatement (LockStatement lockStatement)
			{
				yield return lockStatement.Expression;
			}

			public override IEnumerable<Expression> VisitReturnStatement (ReturnStatement returnStatement)
			{
				yield return returnStatement.Expression;
			}

			public override IEnumerable<Expression> VisitThrowStatement (ThrowStatement throwStatement)
			{
				yield return throwStatement.Expression;
			}

			public override IEnumerable<Expression> VisitUsingStatement (UsingStatement usingStatement)
			{
				var expr = usingStatement.ResourceAcquisition as Expression;
				if (expr != null)
					return new [] { expr };

				return usingStatement.ResourceAcquisition.AcceptVisitor (this);
			}

			public override IEnumerable<Expression> VisitVariableDeclarationStatement (
				VariableDeclarationStatement variableDeclarationStatement)
			{
				return variableDeclarationStatement.Variables.Select (v => v.Initializer);
			}

			public override IEnumerable<Expression> VisitYieldReturnStatement (YieldReturnStatement yieldReturnStatement)
			{
				yield return yieldReturnStatement.Expression;
			}

			public override IEnumerable<Expression> VisitBlockStatement(BlockStatement blockStatement)
			{
				yield break;
			}
		}

		class CfgVariableReferenceNodeBuilder
		{
			readonly VariableReferenceGraphBuilder variableReferenceGraphBuilder;
			GetExpressionsVisitor getExpr = new GetExpressionsVisitor ();

			ISet<AstNode> references;
			ISet<Statement> refStatements;
			CSharpAstResolver resolver;
			Dictionary<ControlFlowNode, VariableReferenceNode> nodeDict;

			public CfgVariableReferenceNodeBuilder(VariableReferenceGraphBuilder variableReferenceGraphBuilder)
			{
				this.variableReferenceGraphBuilder = variableReferenceGraphBuilder;
			}

			public VariableReferenceNode Build (ControlFlowNode startNode, ISet<AstNode> references,
				ISet<Statement> refStatements, CSharpAstResolver resolver)
			{
				this.references = references;
				this.refStatements = refStatements;
				this.resolver = resolver;
				nodeDict = new Dictionary<ControlFlowNode, VariableReferenceNode> ();
				return AddNode (startNode);
			}

			static bool IsValidControlFlowNode (ControlFlowNode node)
			{
				if (node.NextStatement == null)
					return false;
				if (node.Type == ControlFlowNodeType.LoopCondition) {
					if (node.NextStatement is ForeachStatement)
						return false;
				} else {
					if (node.NextStatement is WhileStatement || node.NextStatement is DoWhileStatement ||
						node.NextStatement is ForStatement)
						return false;
				}
				return true;
			}

			VariableReferenceNode GetStatementEndNode (VariableReferenceNode currentNode, Statement statement)
			{
				var expressions = statement.AcceptVisitor (getExpr);
				VariableReferenceNode endNode;
				ExpressionNodeCreationVisitor.CreateNode (references, resolver, expressions, currentNode, out endNode);
				return endNode;
			}

			VariableReferenceNode AddNode (ControlFlowNode startNode)
			{
				var node = new VariableReferenceNode ();
				var cfNode = startNode;
				while (true) {
					if (variableReferenceGraphBuilder.ctx.CancellationToken.IsCancellationRequested)
						return null;
					if (nodeDict.ContainsKey (cfNode)) {
						node.AddNextNode (nodeDict [cfNode]);
						break;
					}
					// create a new node for fork point
					if (cfNode.Incoming.Count > 1 || cfNode.Outgoing.Count > 1) {
						nodeDict [cfNode] = node;

						var forkNode = new VariableReferenceNode ();
						node.AddNextNode (forkNode);
						node = forkNode;
					}
					nodeDict [cfNode] = node;

					if (IsValidControlFlowNode (cfNode) && refStatements.Contains (cfNode.NextStatement)) {
						node = GetStatementEndNode (node, cfNode.NextStatement);
					}

					if (cfNode.Outgoing.Count == 1) {
						cfNode = cfNode.Outgoing [0].To;
					} else {
						foreach (var e in cfNode.Outgoing) {
							node.AddNextNode (AddNode (e.To));
						}
						break;
					}
				}
				VariableReferenceNode result;
				if (!nodeDict.TryGetValue (startNode, out result))
					return new VariableReferenceNode ();
				return result;
			}
		}

		class ExpressionNodeCreationVisitor : DepthFirstAstVisitor
		{
			VariableReferenceNode startNode;
			VariableReferenceNode endNode;
			ISet<AstNode> references;
			CSharpAstResolver resolver;

			ExpressionNodeCreationVisitor (ISet<AstNode> references, CSharpAstResolver resolver,
				VariableReferenceNode startNode)
			{
				this.references = references;
				this.resolver = resolver;
				this.startNode = this.endNode = startNode ?? new VariableReferenceNode ();
			}

			public static VariableReferenceNode CreateNode (ISet<AstNode> references, CSharpAstResolver resolver,
				params Expression [] expressions)
			{
				VariableReferenceNode endNode;
				return CreateNode (references, resolver, expressions, null, out endNode);
			}

			public static VariableReferenceNode CreateNode (ISet<AstNode> references, CSharpAstResolver resolver,
				IEnumerable<Expression> expressions, VariableReferenceNode startNode, out VariableReferenceNode endNode)
			{
				startNode = startNode ?? new VariableReferenceNode ();
				endNode = startNode;
				if (expressions != null) {
					foreach (var expr in expressions) {
						var visitor = CreateVisitor(references, resolver, expr, endNode);
						endNode = visitor.endNode;
					}
				}
				return startNode;
			}

			static ExpressionNodeCreationVisitor CreateVisitor (ISet<AstNode> references, CSharpAstResolver resolver,
				Expression rootExpr, VariableReferenceNode startNode = null, VariableReferenceNode nextNode = null)
			{
				var visitor = new ExpressionNodeCreationVisitor (references, resolver, startNode);
				rootExpr.AcceptVisitor (visitor);
				if (nextNode != null)
					visitor.endNode.AddNextNode (nextNode);
				return visitor;
			}

			static VariableReferenceNode CreateNode (ISet<AstNode> references, CSharpAstResolver resolver, 
				Expression rootExpr, VariableReferenceNode startNode = null, VariableReferenceNode nextNode = null)
			{
				return CreateVisitor (references, resolver, rootExpr, startNode, nextNode).startNode;
			}

			#region Skipped Expressions
			public override void VisitAnonymousMethodExpression (AnonymousMethodExpression anonymousMethodExpression)
			{
			}

			public override void VisitLambdaExpression (LambdaExpression lambdaExpression)
			{
			}

			public override void VisitBaseReferenceExpression (BaseReferenceExpression baseReferenceExpression)
			{
			}

			public override void VisitNullReferenceExpression (NullReferenceExpression nullReferenceExpression)
			{
			}

			public override void VisitPrimitiveExpression (PrimitiveExpression primitiveExpression)
			{
			}

			public override void VisitSizeOfExpression (SizeOfExpression sizeOfExpression)
			{
			}

			public override void VisitThisReferenceExpression (ThisReferenceExpression thisReferenceExpression)
			{
			}

			public override void VisitTypeOfExpression (TypeOfExpression typeOfExpression)
			{
			}

			public override void VisitTypeReferenceExpression (TypeReferenceExpression typeReferenceExpression)
			{
			}

			public override void VisitUndocumentedExpression (UndocumentedExpression undocumentedExpression)
			{
			}

			public override void VisitDefaultValueExpression (DefaultValueExpression defaultValueExpression)
			{
			}

			#endregion

			public override void VisitAssignmentExpression (AssignmentExpression assignmentExpression)
			{
				assignmentExpression.Right.AcceptVisitor (this);
				assignmentExpression.Left.AcceptVisitor (this);
			}

			public override void VisitBinaryOperatorExpression (BinaryOperatorExpression binaryOperatorExpression)
			{
				binaryOperatorExpression.Left.AcceptVisitor (this);
				binaryOperatorExpression.Right.AcceptVisitor (this);
			}

			public override void VisitCastExpression (CastExpression castExpression)
			{
				castExpression.Expression.AcceptVisitor (this);
			}

			public override void VisitCheckedExpression (CheckedExpression checkedExpression)
			{
				checkedExpression.Expression.AcceptVisitor (this);
			}

			public override void VisitConditionalExpression (ConditionalExpression conditionalExpression)
			{
				conditionalExpression.Condition.AcceptVisitor (this);
				var resolveResult = resolver.Resolve (conditionalExpression.Condition);
				if (resolveResult.ConstantValue is bool) {
					if ((bool) resolveResult.ConstantValue)
						conditionalExpression.TrueExpression.AcceptVisitor (this);
					else
						conditionalExpression.FalseExpression.AcceptVisitor (this);
					return;
				}
				var nextEndNode = new VariableReferenceNode ();
				var trueNode = CreateNode (references, resolver, conditionalExpression.TrueExpression, null, 
					nextEndNode);
				var falseNode = CreateNode (references, resolver, conditionalExpression.FalseExpression, null, 
					nextEndNode);
				endNode.AddNextNode (trueNode);
				endNode.AddNextNode (falseNode);
				endNode = nextEndNode;
			}

			public override void VisitIdentifierExpression (IdentifierExpression identifierExpression)
			{
				if (references.Contains (identifierExpression))
					endNode.References.Add (identifierExpression);
			}

			public override void VisitIndexerExpression (IndexerExpression indexerExpression)
			{
				indexerExpression.Target.AcceptVisitor (this);
				foreach (var arg in indexerExpression.Arguments)
					arg.AcceptVisitor (this);
			}

			public override void VisitInvocationExpression (InvocationExpression invocationExpression)
			{
				invocationExpression.Target.AcceptVisitor (this);
				var outArguments = new List<Expression> ();
				foreach (var arg in invocationExpression.Arguments) {
					var directionExpr = arg as DirectionExpression;
					if (directionExpr != null && directionExpr.FieldDirection == FieldDirection.Out) {
						outArguments.Add (directionExpr);
						continue;
					}
					arg.AcceptVisitor (this);
				}
				foreach (var arg in outArguments)
					arg.AcceptVisitor (this);
			}

			public override void VisitDirectionExpression (DirectionExpression directionExpression)
			{
				directionExpression.Expression.AcceptVisitor (this);
			}

			public override void VisitMemberReferenceExpression (MemberReferenceExpression memberReferenceExpression)
			{
				memberReferenceExpression.Target.AcceptVisitor (this);
			}

			public override void VisitObjectCreateExpression (ObjectCreateExpression objectCreateExpression)
			{
				foreach (var arg in objectCreateExpression.Arguments)
					arg.AcceptVisitor (this);
				objectCreateExpression.Initializer.AcceptVisitor (this);
			}

			public override void VisitAnonymousTypeCreateExpression (
				AnonymousTypeCreateExpression anonymousTypeCreateExpression)
			{
				foreach (var init in anonymousTypeCreateExpression.Initializers)
					init.AcceptVisitor (this);
			}

			public override void VisitArrayCreateExpression (ArrayCreateExpression arrayCreateExpression)
			{
				foreach (var arg in arrayCreateExpression.Arguments)
					arg.AcceptVisitor (this);
				arrayCreateExpression.Initializer.AcceptVisitor (this);
			}

			public override void VisitParenthesizedExpression (ParenthesizedExpression parenthesizedExpression)
			{
				parenthesizedExpression.Expression.AcceptVisitor (this);
			}

			public override void VisitPointerReferenceExpression (PointerReferenceExpression pointerReferenceExpression)
			{
				pointerReferenceExpression.Target.AcceptVisitor (this);
			}

			public override void VisitStackAllocExpression (StackAllocExpression stackAllocExpression)
			{
				stackAllocExpression.CountExpression.AcceptVisitor (this);
			}

			public override void VisitUnaryOperatorExpression (UnaryOperatorExpression unaryOperatorExpression)
			{
				unaryOperatorExpression.Expression.AcceptVisitor (this);
			}

			public override void VisitUncheckedExpression (UncheckedExpression uncheckedExpression)
			{
				uncheckedExpression.Expression.AcceptVisitor (this);
			}

			public override void VisitAsExpression (AsExpression asExpression)
			{
				asExpression.Expression.AcceptVisitor (this);
			}

			public override void VisitIsExpression (IsExpression isExpression)
			{
				isExpression.Expression.AcceptVisitor (this);
			}

			public override void VisitArrayInitializerExpression (ArrayInitializerExpression arrayInitializerExpression)
			{
				foreach (var element in arrayInitializerExpression.Elements)
					element.AcceptVisitor (this);
			}

			public override void VisitNamedArgumentExpression (NamedArgumentExpression namedArgumentExpression)
			{
				namedArgumentExpression.Expression.AcceptVisitor (this);
			}

			public override void VisitNamedExpression (NamedExpression namedExpression)
			{
				namedExpression.Expression.AcceptVisitor (this);
			}

			public override void VisitQueryExpression (QueryExpression queryExpression)
			{
				foreach (var clause in queryExpression.Clauses)
					clause.AcceptVisitor (this);
			}

			#region Query Clauses

			public override void VisitQueryContinuationClause (QueryContinuationClause queryContinuationClause)
			{
				queryContinuationClause.PrecedingQuery.AcceptVisitor (this);
			}

			public override void VisitQueryFromClause (QueryFromClause queryFromClause)
			{
				queryFromClause.Expression.AcceptVisitor (this);
			}

			public override void VisitQueryJoinClause (QueryJoinClause queryJoinClause)
			{
				queryJoinClause.InExpression.AcceptVisitor (this);
			}

			public override void VisitQueryLetClause (QueryLetClause queryLetClause)
			{
			}

			public override void VisitQueryWhereClause (QueryWhereClause queryWhereClause)
			{
			}

			public override void VisitQueryOrderClause (QueryOrderClause queryOrderClause)
			{
			}

			public override void VisitQueryOrdering (QueryOrdering queryOrdering)
			{
			}

			public override void VisitQuerySelectClause (QuerySelectClause querySelectClause)
			{
			}

			public override void VisitQueryGroupClause (QueryGroupClause queryGroupClause)
			{
			}

			#endregion
		}
	}
}