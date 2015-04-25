// Copyright (c) 2010-2013 AlphaSierraPapa for the SharpDevelop Team
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
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ICSharpCode.NRefactory.CSharp.Resolver;
using ICSharpCode.NRefactory.CSharp.TypeSystem;
using ICSharpCode.NRefactory.Semantics;
using ICSharpCode.NRefactory.CSharp;

namespace ICSharpCode.NRefactory.CSharp.Analysis
{
	/// <summary>
	/// Statement reachability analysis.
	/// </summary>
	public sealed class ReachabilityAnalysis
	{
		HashSet<Statement> reachableStatements = new HashSet<Statement>();
		HashSet<Statement> reachableEndPoints = new HashSet<Statement>();
		HashSet<ControlFlowNode> visitedNodes = new HashSet<ControlFlowNode>();
		Stack<ControlFlowNode> stack = new Stack<ControlFlowNode>();
		RecursiveDetectorVisitor recursiveDetectorVisitor = null;
		
		private ReachabilityAnalysis() {}
		
		public static ReachabilityAnalysis Create(Statement statement, CSharpAstResolver resolver = null, RecursiveDetectorVisitor recursiveDetectorVisitor = null, CancellationToken cancellationToken = default(CancellationToken))
		{
			var cfgBuilder = new ControlFlowGraphBuilder();
			var cfg = cfgBuilder.BuildControlFlowGraph(statement, resolver, cancellationToken);
			return Create(cfg, recursiveDetectorVisitor, cancellationToken);
		}
		
		internal static ReachabilityAnalysis Create(Statement statement, Func<AstNode, CancellationToken, ResolveResult> resolver, CSharpTypeResolveContext typeResolveContext, CancellationToken cancellationToken)
		{
			var cfgBuilder = new ControlFlowGraphBuilder();
			var cfg = cfgBuilder.BuildControlFlowGraph(statement, resolver, typeResolveContext, cancellationToken);
			return Create(cfg, null, cancellationToken);
		}
		
		public static ReachabilityAnalysis Create(IList<ControlFlowNode> controlFlowGraph, RecursiveDetectorVisitor recursiveDetectorVisitor = null, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (controlFlowGraph == null)
				throw new ArgumentNullException("controlFlowGraph");
			ReachabilityAnalysis ra = new ReachabilityAnalysis();
			ra.recursiveDetectorVisitor = recursiveDetectorVisitor;
			// Analysing a null node can result in an empty control flow graph
			if (controlFlowGraph.Count > 0) {
				ra.stack.Push(controlFlowGraph[0]);
				while (ra.stack.Count > 0) {
					cancellationToken.ThrowIfCancellationRequested();
					ra.MarkReachable(ra.stack.Pop());
				}
			}
			ra.stack = null;
			ra.visitedNodes = null;
			return ra;
		}
		
		void MarkReachable(ControlFlowNode node)
		{
			if (node.PreviousStatement != null) {
				if (node.PreviousStatement is LabelStatement) {
					reachableStatements.Add(node.PreviousStatement);
				}
				reachableEndPoints.Add(node.PreviousStatement);
			}
			if (node.NextStatement != null) {
				reachableStatements.Add(node.NextStatement);
				if (IsRecursive(node.NextStatement)) {
					return;
				}
			}
			foreach (var edge in node.Outgoing) {
				if (visitedNodes.Add(edge.To))
					stack.Push(edge.To);
			}
		}

		bool IsRecursive(Statement statement)
		{
			return recursiveDetectorVisitor != null && statement.AcceptVisitor(recursiveDetectorVisitor);
		}
		
		public IEnumerable<Statement> ReachableStatements {
			get { return reachableStatements; }
		}
		
		public bool IsReachable(Statement statement)
		{
			return reachableStatements.Contains(statement);
		}
		
		public bool IsEndpointReachable(Statement statement)
		{
			return reachableEndPoints.Contains(statement);
		}

		public class RecursiveDetectorVisitor : DepthFirstAstVisitor<bool>
		{
			public override bool VisitConditionalExpression(ConditionalExpression conditionalExpression)
			{
				if (conditionalExpression.Condition.AcceptVisitor(this))
					return true;

				if (!conditionalExpression.TrueExpression.AcceptVisitor(this))
					return false;

				return conditionalExpression.FalseExpression.AcceptVisitor(this);
			}

			public override bool VisitBinaryOperatorExpression(BinaryOperatorExpression binaryOperatorExpression)
			{
				if (binaryOperatorExpression.Operator == BinaryOperatorType.NullCoalescing) {
					return binaryOperatorExpression.Left.AcceptVisitor(this);
				}
				return base.VisitBinaryOperatorExpression(binaryOperatorExpression);
			}

			public override bool VisitIfElseStatement(IfElseStatement ifElseStatement)
			{
				if (ifElseStatement.Condition.AcceptVisitor(this))
					return true;

				if (!ifElseStatement.TrueStatement.AcceptVisitor(this))
					return false;

				//No need to worry about null ast nodes, since AcceptVisitor will just
				//return false in those cases
				return ifElseStatement.FalseStatement.AcceptVisitor(this);
			}

			public override bool VisitForeachStatement(ForeachStatement foreachStatement)
			{
				//Even if the body is always recursive, the function may stop if the collection
				// is empty.
				return foreachStatement.InExpression.AcceptVisitor(this);
			}

			public override bool VisitForStatement(ForStatement forStatement)
			{
				if (forStatement.Initializers.Any(initializer => initializer.AcceptVisitor(this)))
					return true;

				return forStatement.Condition.AcceptVisitor(this);
			}

			public override bool VisitSwitchStatement(SwitchStatement switchStatement)
			{
				if (switchStatement.Expression.AcceptVisitor(this)) {
					return true;
				}

				bool foundDefault = false;
				foreach (var section in switchStatement.SwitchSections) {
					foundDefault = foundDefault || section.CaseLabels.Any(label => label.Expression.IsNull);
					if (!section.AcceptVisitor(this))
						return false;
				}

				return foundDefault;
			}

			public override bool VisitBlockStatement(BlockStatement blockStatement)
			{
				//If the block has a recursive statement, then that statement will be visited
				//individually by the CFG construction algorithm later.
				return false;
			}

			protected override bool VisitChildren(AstNode node)
			{
				return VisitNodeList(node.Children);
			}

			bool VisitNodeList(IEnumerable<AstNode> nodes) {
				return nodes.Any(node => node.AcceptVisitor(this));
			}

			public override bool VisitQueryExpression(QueryExpression queryExpression)
			{
				//We only care about the first from clause because:
				//in "from x in Method() select x", Method() might be recursive
				//but in "from x in Bar() from y in Method() select x + y", even if Method() is recursive
				//Bar might still be empty.
				var queryFromClause = queryExpression.Clauses.OfType<QueryFromClause>().FirstOrDefault();
				if (queryFromClause == null)
					return true;
				return queryFromClause.AcceptVisitor(this);
			}
		}
	}
}
