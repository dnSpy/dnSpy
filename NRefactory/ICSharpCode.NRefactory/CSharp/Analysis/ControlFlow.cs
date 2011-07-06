// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

using ICSharpCode.NRefactory.CSharp.Resolver;
using ICSharpCode.NRefactory.TypeSystem;

namespace ICSharpCode.NRefactory.CSharp.Analysis
{
	/// <summary>
	/// Represents a node in the control flow graph of a C# method.
	/// </summary>
	public class ControlFlowNode
	{
		public readonly Statement PreviousStatement;
		public readonly Statement NextStatement;
		
		public readonly ControlFlowNodeType Type;
		
		public readonly List<ControlFlowEdge> Outgoing = new List<ControlFlowEdge>();
		public readonly List<ControlFlowEdge> Incoming = new List<ControlFlowEdge>();
		
		public ControlFlowNode(Statement previousStatement, Statement nextStatement, ControlFlowNodeType type)
		{
			if (previousStatement == null && nextStatement == null)
				throw new ArgumentException("previousStatement and nextStatement must not be both null");
			this.PreviousStatement = previousStatement;
			this.NextStatement = nextStatement;
			this.Type = type;
		}
	}
	
	public enum ControlFlowNodeType
	{
		/// <summary>
		/// Unknown node type
		/// </summary>
		None,
		/// <summary>
		/// Node in front of a statement
		/// </summary>
		StartNode,
		/// <summary>
		/// Node between two statements
		/// </summary>
		BetweenStatements,
		/// <summary>
		/// Node at the end of a statement list
		/// </summary>
		EndNode,
		/// <summary>
		/// Node representing the position before evaluating the condition of a loop.
		/// </summary>
		LoopCondition
	}
	
	public class ControlFlowEdge
	{
		public readonly ControlFlowNode From;
		public readonly ControlFlowNode To;
		public readonly ControlFlowEdgeType Type;
		
		List<TryCatchStatement> jumpOutOfTryFinally;
		
		public ControlFlowEdge(ControlFlowNode from, ControlFlowNode to, ControlFlowEdgeType type)
		{
			if (from == null)
				throw new ArgumentNullException("from");
			if (to == null)
				throw new ArgumentNullException("to");
			this.From = from;
			this.To = to;
			this.Type = type;
		}
		
		internal void AddJumpOutOfTryFinally(TryCatchStatement tryFinally)
		{
			if (jumpOutOfTryFinally == null)
				jumpOutOfTryFinally = new List<TryCatchStatement>();
			jumpOutOfTryFinally.Add(tryFinally);
		}
		
		/// <summary>
		/// Gets whether this control flow edge is leaving any try-finally statements.
		/// </summary>
		public bool IsLeavingTryFinally {
			get { return jumpOutOfTryFinally != null; }
		}
		
		/// <summary>
		/// Gets the try-finally statements that this control flow edge is leaving.
		/// </summary>
		public IEnumerable<TryCatchStatement> TryFinallyStatements {
			get { return jumpOutOfTryFinally ?? Enumerable.Empty<TryCatchStatement>(); }
		}
	}
	
	public enum ControlFlowEdgeType
	{
		/// <summary>
		/// Regular control flow.
		/// </summary>
		Normal,
		/// <summary>
		/// Conditional control flow (edge taken if condition is true)
		/// </summary>
		ConditionTrue,
		/// <summary>
		/// Conditional control flow (edge taken if condition is false)
		/// </summary>
		ConditionFalse,
		/// <summary>
		/// A jump statement (goto, goto case, break or continue)
		/// </summary>
		Jump
	}
	
	/// <summary>
	/// Constructs the control flow graph for C# statements.
	/// </summary>
	public class ControlFlowGraphBuilder
	{
		// Written according to the reachability rules in the C# spec (§8.1 End points and reachability)
		
		protected virtual ControlFlowNode CreateNode(Statement previousStatement, Statement nextStatement, ControlFlowNodeType type)
		{
			return new ControlFlowNode(previousStatement, nextStatement, type);
		}
		
		protected virtual ControlFlowEdge CreateEdge(ControlFlowNode from, ControlFlowNode to, ControlFlowEdgeType type)
		{
			return new ControlFlowEdge(from, to, type);
		}
		
		Statement rootStatement;
		ResolveVisitor resolveVisitor;
		List<ControlFlowNode> nodes;
		Dictionary<string, ControlFlowNode> labels;
		List<ControlFlowNode> gotoStatements;
		
		public IList<ControlFlowNode> BuildControlFlowGraph(Statement statement, ITypeResolveContext context)
		{
			return BuildControlFlowGraph(statement, context, CancellationToken.None);
		}
		
		public IList<ControlFlowNode> BuildControlFlowGraph(Statement statement, ITypeResolveContext context, CancellationToken cancellationToken)
		{
			return BuildControlFlowGraph(statement, new ResolveVisitor(
				new CSharpResolver(context, cancellationToken),
				null, ConstantModeResolveVisitorNavigator.Skip));
		}
		
		public IList<ControlFlowNode> BuildControlFlowGraph(Statement statement, ResolveVisitor resolveVisitor)
		{
			if (statement == null)
				throw new ArgumentNullException("statement");
			if (resolveVisitor == null)
				throw new ArgumentNullException("resolveVisitor");
			
			NodeCreationVisitor nodeCreationVisitor = new NodeCreationVisitor();
			nodeCreationVisitor.builder = this;
			try {
				this.nodes = new List<ControlFlowNode>();
				this.labels = new Dictionary<string, ControlFlowNode>();
				this.gotoStatements = new List<ControlFlowNode>();
				this.rootStatement = statement;
				this.resolveVisitor = resolveVisitor;
				ControlFlowNode entryPoint = CreateStartNode(statement);
				statement.AcceptVisitor(nodeCreationVisitor, entryPoint);
				
				// Resolve goto statements:
				foreach (ControlFlowNode gotoStmt in gotoStatements) {
					string label = ((GotoStatement)gotoStmt.NextStatement).Label;
					ControlFlowNode labelNode;
					if (labels.TryGetValue(label, out labelNode))
						nodeCreationVisitor.Connect(gotoStmt, labelNode, ControlFlowEdgeType.Jump);
				}
				
				AnnotateLeaveEdgesWithTryFinallyBlocks();
				
				return nodes;
			} finally {
				this.nodes = null;
				this.labels = null;
				this.gotoStatements = null;
				this.rootStatement = null;
				this.resolveVisitor = null;
			}
		}
		
		void AnnotateLeaveEdgesWithTryFinallyBlocks()
		{
			foreach (ControlFlowEdge edge in nodes.SelectMany(n => n.Outgoing)) {
				if (edge.Type != ControlFlowEdgeType.Jump) {
					// Only jumps are potential candidates for leaving try-finally blocks.
					// Note that the regular edges leaving try or catch blocks are already annotated by the visitor.
					continue;
				}
				Statement gotoStatement = edge.From.NextStatement;
				Debug.Assert(gotoStatement is GotoStatement || gotoStatement is GotoDefaultStatement || gotoStatement is GotoCaseStatement || gotoStatement is BreakStatement || gotoStatement is ContinueStatement);
				Statement targetStatement = edge.To.PreviousStatement ?? edge.To.NextStatement;
				if (gotoStatement.Parent == targetStatement.Parent)
					continue;
				HashSet<TryCatchStatement> targetParentTryCatch = new HashSet<TryCatchStatement>(targetStatement.Ancestors.OfType<TryCatchStatement>());
				for (AstNode node = gotoStatement.Parent; node != null; node = node.Parent) {
					TryCatchStatement leftTryCatch = node as TryCatchStatement;
					if (leftTryCatch != null) {
						if (targetParentTryCatch.Contains(leftTryCatch))
							break;
						if (!leftTryCatch.FinallyBlock.IsNull)
							edge.AddJumpOutOfTryFinally(leftTryCatch);
					}
				}
			}
		}
		
		#region Create*Node
		ControlFlowNode CreateStartNode(Statement statement)
		{
			ControlFlowNode node = CreateNode(null, statement, ControlFlowNodeType.StartNode);
			nodes.Add(node);
			return node;
		}
		
		ControlFlowNode CreateSpecialNode(Statement statement, ControlFlowNodeType type, bool addToNodeList = true)
		{
			ControlFlowNode node = CreateNode(null, statement, type);
			if (addToNodeList)
				nodes.Add(node);
			return node;
		}
		
		ControlFlowNode CreateEndNode(Statement statement, bool addToNodeList = true)
		{
			Statement nextStatement;
			if (statement == rootStatement) {
				nextStatement = null;
			} else {
				// Find the next statement in the same role:
				AstNode next = statement;
				do {
					next = next.NextSibling;
				} while (next != null && next.Role != statement.Role);
				nextStatement = next as Statement;
			}
			ControlFlowNodeType type = nextStatement != null ? ControlFlowNodeType.BetweenStatements : ControlFlowNodeType.EndNode;
			ControlFlowNode node = CreateNode(statement, nextStatement, type);
			if (addToNodeList)
				nodes.Add(node);
			return node;
		}
		#endregion
		
		#region Constant evaluation
		/// <summary>
		/// Gets/Sets whether to handle only primitive expressions as constants (no complex expressions like "a + b").
		/// </summary>
		public bool EvaluateOnlyPrimitiveConstants { get; set; }
		
		/// <summary>
		/// Evaluates an expression.
		/// </summary>
		/// <returns>The constant value of the expression; or null if the expression is not a constant.</returns>
		ConstantResolveResult EvaluateConstant(Expression expr)
		{
			if (EvaluateOnlyPrimitiveConstants) {
				if (!(expr is PrimitiveExpression || expr is NullReferenceExpression))
					return null;
			}
			return resolveVisitor.Resolve(expr) as ConstantResolveResult;
		}
		
		/// <summary>
		/// Evaluates an expression.
		/// </summary>
		/// <returns>The value of the constant boolean expression; or null if the value is not a constant boolean expression.</returns>
		bool? EvaluateCondition(Expression expr)
		{
			ConstantResolveResult rr = EvaluateConstant(expr);
			if (rr != null)
				return rr.ConstantValue as bool?;
			else
				return null;
		}
		
		bool AreEqualConstants(ConstantResolveResult c1, ConstantResolveResult c2)
		{
			if (c1 == null || c2 == null)
				return false;
			CSharpResolver r = new CSharpResolver(resolveVisitor.TypeResolveContext, resolveVisitor.CancellationToken);
			ResolveResult c = r.ResolveBinaryOperator(BinaryOperatorType.Equality, c1, c2);
			return c.IsCompileTimeConstant && (c.ConstantValue as bool?) == true;
		}
		#endregion
		
		sealed class NodeCreationVisitor : DepthFirstAstVisitor<ControlFlowNode, ControlFlowNode>
		{
			// 'data' parameter: input control flow node (start of statement being visited)
			// Return value: result control flow node (end of statement being visited)
			
			internal ControlFlowGraphBuilder builder;
			Stack<ControlFlowNode> breakTargets = new Stack<ControlFlowNode>();
			Stack<ControlFlowNode> continueTargets = new Stack<ControlFlowNode>();
			List<ControlFlowNode> gotoCaseOrDefault = new List<ControlFlowNode>();
			
			internal ControlFlowEdge Connect(ControlFlowNode from, ControlFlowNode to, ControlFlowEdgeType type = ControlFlowEdgeType.Normal)
			{
				ControlFlowEdge edge = builder.CreateEdge(from, to, type);
				from.Outgoing.Add(edge);
				to.Incoming.Add(edge);
				return edge;
			}
			
			/// <summary>
			/// Creates an end node for <c>stmt</c> and connects <c>from</c> with the new node.
			/// </summary>
			ControlFlowNode CreateConnectedEndNode(Statement stmt, ControlFlowNode from)
			{
				ControlFlowNode newNode = builder.CreateEndNode(stmt);
				Connect(from, newNode);
				return newNode;
			}
			
			protected override ControlFlowNode VisitChildren(AstNode node, ControlFlowNode data)
			{
				// We have overrides for all possible expressions and should visit expressions only.
				throw new NotImplementedException();
			}
			
			public override ControlFlowNode VisitBlockStatement(BlockStatement blockStatement, ControlFlowNode data)
			{
				// C# 4.0 spec: §8.2 Blocks
				ControlFlowNode childNode = HandleStatementList(blockStatement.Statements, data);
				return CreateConnectedEndNode(blockStatement, childNode);
			}
			
			ControlFlowNode HandleStatementList(AstNodeCollection<Statement> statements, ControlFlowNode source)
			{
				ControlFlowNode childNode = null;
				foreach (Statement stmt in statements) {
					if (childNode == null) {
						childNode = builder.CreateStartNode(stmt);
						if (source != null)
							Connect(source, childNode);
					}
					Debug.Assert(childNode.NextStatement == stmt);
					childNode = stmt.AcceptVisitor(this, childNode);
					Debug.Assert(childNode.PreviousStatement == stmt);
				}
				return childNode ?? source;
			}
			
			public override ControlFlowNode VisitEmptyStatement(EmptyStatement emptyStatement, ControlFlowNode data)
			{
				return CreateConnectedEndNode(emptyStatement, data);
			}
			
			public override ControlFlowNode VisitLabelStatement(LabelStatement labelStatement, ControlFlowNode data)
			{
				ControlFlowNode end = CreateConnectedEndNode(labelStatement, data);
				builder.labels[labelStatement.Label] = end;
				return end;
			}
			
			public override ControlFlowNode VisitVariableDeclarationStatement(VariableDeclarationStatement variableDeclarationStatement, ControlFlowNode data)
			{
				return CreateConnectedEndNode(variableDeclarationStatement, data);
			}
			
			public override ControlFlowNode VisitExpressionStatement(ExpressionStatement expressionStatement, ControlFlowNode data)
			{
				return CreateConnectedEndNode(expressionStatement, data);
			}
			
			public override ControlFlowNode VisitIfElseStatement(IfElseStatement ifElseStatement, ControlFlowNode data)
			{
				bool? cond = builder.EvaluateCondition(ifElseStatement.Condition);
				ControlFlowNode trueBegin = builder.CreateStartNode(ifElseStatement.TrueStatement);
				if (cond != false)
					Connect(data, trueBegin, ControlFlowEdgeType.ConditionTrue);
				ControlFlowNode trueEnd = ifElseStatement.TrueStatement.AcceptVisitor(this, trueBegin);
				ControlFlowNode falseEnd;
				if (ifElseStatement.FalseStatement.IsNull) {
					falseEnd = null;
				} else {
					ControlFlowNode falseBegin = builder.CreateStartNode(ifElseStatement.FalseStatement);
					if (cond != true)
						Connect(data, falseBegin, ControlFlowEdgeType.ConditionFalse);
					falseEnd = ifElseStatement.FalseStatement.AcceptVisitor(this, falseBegin);
				}
				ControlFlowNode end = builder.CreateEndNode(ifElseStatement);
				Connect(trueEnd, end);
				if (falseEnd != null) {
					Connect(falseEnd, end);
				} else if (cond != true) {
					Connect(data, end, ControlFlowEdgeType.ConditionFalse);
				}
				return end;
			}
			
			public override ControlFlowNode VisitSwitchStatement(SwitchStatement switchStatement, ControlFlowNode data)
			{
				// First, figure out which switch section will get called (if the expression is constant):
				ConstantResolveResult constant = builder.EvaluateConstant(switchStatement.Expression);
				SwitchSection defaultSection = null;
				SwitchSection sectionMatchedByConstant = null;
				foreach (SwitchSection section in switchStatement.SwitchSections) {
					foreach (CaseLabel label in section.CaseLabels) {
						if (label.Expression.IsNull) {
							defaultSection = section;
						} else if (constant != null) {
							ConstantResolveResult labelConstant = builder.EvaluateConstant(label.Expression);
							if (builder.AreEqualConstants(constant, labelConstant))
								sectionMatchedByConstant = section;
						}
					}
				}
				if (constant != null && sectionMatchedByConstant == null)
					sectionMatchedByConstant = defaultSection;
				
				int gotoCaseOrDefaultInOuterScope = gotoCaseOrDefault.Count;
				
				ControlFlowNode end = builder.CreateEndNode(switchStatement, addToNodeList: false);
				breakTargets.Push(end);
				foreach (SwitchSection section in switchStatement.SwitchSections) {
					if (constant == null || section == sectionMatchedByConstant) {
						HandleStatementList(section.Statements, data);
					} else {
						// This section is unreachable: pass null to HandleStatementList.
						HandleStatementList(section.Statements, null);
					}
					// Don't bother connecting the ends of the sections: the 'break' statement takes care of that.
				}
				breakTargets.Pop();
				if (defaultSection == null && sectionMatchedByConstant == null) {
					Connect(data, end);
				}
				
				if (gotoCaseOrDefault.Count > gotoCaseOrDefaultInOuterScope) {
					// Resolve 'goto case' statements:
					throw new NotImplementedException();
				}
				
				builder.nodes.Add(end);
				return end;
			}
			
			public override ControlFlowNode VisitGotoCaseStatement(GotoCaseStatement gotoCaseStatement, ControlFlowNode data)
			{
				gotoCaseOrDefault.Add(data);
				return builder.CreateEndNode(gotoCaseStatement);
			}
			
			public override ControlFlowNode VisitGotoDefaultStatement(GotoDefaultStatement gotoDefaultStatement, ControlFlowNode data)
			{
				gotoCaseOrDefault.Add(data);
				return builder.CreateEndNode(gotoDefaultStatement);
			}
			
			public override ControlFlowNode VisitWhileStatement(WhileStatement whileStatement, ControlFlowNode data)
			{
				// <data> <condition> while (cond) { <bodyStart> embeddedStmt; <bodyEnd> } <end>
				ControlFlowNode end = builder.CreateEndNode(whileStatement, addToNodeList: false);
				ControlFlowNode conditionNode = builder.CreateSpecialNode(whileStatement, ControlFlowNodeType.LoopCondition);
				breakTargets.Push(end);
				continueTargets.Push(conditionNode);
				
				Connect(data, conditionNode);
				
				bool? cond = builder.EvaluateCondition(whileStatement.Condition);
				ControlFlowNode bodyStart = builder.CreateStartNode(whileStatement.EmbeddedStatement);
				if (cond != false)
					Connect(conditionNode, bodyStart, ControlFlowEdgeType.ConditionTrue);
				ControlFlowNode bodyEnd = whileStatement.EmbeddedStatement.AcceptVisitor(this, bodyStart);
				Connect(bodyEnd, conditionNode);
				if (cond != true)
					Connect(conditionNode, end, ControlFlowEdgeType.ConditionFalse);
				
				breakTargets.Pop();
				continueTargets.Pop();
				builder.nodes.Add(end);
				return end;
			}
			
			public override ControlFlowNode VisitDoWhileStatement(DoWhileStatement doWhileStatement, ControlFlowNode data)
			{
				// <data> do { <bodyStart> embeddedStmt; <bodyEnd>} <condition> while(cond); <end>
				ControlFlowNode end = builder.CreateEndNode(doWhileStatement, addToNodeList: false);
				ControlFlowNode conditionNode = builder.CreateSpecialNode(doWhileStatement, ControlFlowNodeType.LoopCondition, addToNodeList: false);
				breakTargets.Push(end);
				continueTargets.Push(conditionNode);
				
				ControlFlowNode bodyStart = builder.CreateStartNode(doWhileStatement.EmbeddedStatement);
				Connect(data, bodyStart);
				ControlFlowNode bodyEnd = doWhileStatement.EmbeddedStatement.AcceptVisitor(this, bodyStart);
				Connect(bodyEnd, conditionNode);
				
				bool? cond = builder.EvaluateCondition(doWhileStatement.Condition);
				if (cond != false)
					Connect(conditionNode, bodyStart, ControlFlowEdgeType.ConditionTrue);
				if (cond != true)
					Connect(conditionNode, end, ControlFlowEdgeType.ConditionFalse);
				
				breakTargets.Pop();
				continueTargets.Pop();
				builder.nodes.Add(conditionNode);
				builder.nodes.Add(end);
				return end;
			}
			
			public override ControlFlowNode VisitForStatement(ForStatement forStatement, ControlFlowNode data)
			{
				data = HandleStatementList(forStatement.Initializers, data);
				// for (initializers <data>; <condition>cond; <iteratorStart>iterators<iteratorEnd>) { <bodyStart> embeddedStmt; <bodyEnd> } <end>
				ControlFlowNode end = builder.CreateEndNode(forStatement, addToNodeList: false);
				ControlFlowNode conditionNode = builder.CreateSpecialNode(forStatement, ControlFlowNodeType.LoopCondition);
				Connect(data, conditionNode);
				
				int iteratorStartNodeID = builder.nodes.Count;
				ControlFlowNode iteratorEnd = HandleStatementList(forStatement.Iterators, null);
				ControlFlowNode iteratorStart;
				if (iteratorEnd != null) {
					iteratorStart = builder.nodes[iteratorStartNodeID];
					Connect(iteratorEnd, conditionNode);
				} else {
					iteratorStart = conditionNode;
				}
				
				breakTargets.Push(end);
				continueTargets.Push(iteratorStart);
				
				ControlFlowNode bodyStart = builder.CreateStartNode(forStatement.EmbeddedStatement);
				ControlFlowNode bodyEnd = forStatement.EmbeddedStatement.AcceptVisitor(this, bodyStart);
				Connect(bodyEnd, iteratorStart);
				
				breakTargets.Pop();
				continueTargets.Pop();
				
				bool? cond = forStatement.Condition.IsNull ? true : builder.EvaluateCondition(forStatement.Condition);
				if (cond != false)
					Connect(conditionNode, bodyStart, ControlFlowEdgeType.ConditionTrue);
				if (cond != true)
					Connect(conditionNode, end, ControlFlowEdgeType.ConditionFalse);
				
				builder.nodes.Add(end);
				return end;
			}
			
			ControlFlowNode HandleEmbeddedStatement(Statement embeddedStatement, ControlFlowNode source)
			{
				if (embeddedStatement == null || embeddedStatement.IsNull)
					return source;
				ControlFlowNode bodyStart = builder.CreateStartNode(embeddedStatement);
				if (source != null)
					Connect(source, bodyStart);
				return embeddedStatement.AcceptVisitor(this, bodyStart);
			}
			
			public override ControlFlowNode VisitForeachStatement(ForeachStatement foreachStatement, ControlFlowNode data)
			{
				// <data> foreach (<condition>...) { <bodyStart>embeddedStmt<bodyEnd> } <end>
				ControlFlowNode end = builder.CreateEndNode(foreachStatement, addToNodeList: false);
				ControlFlowNode conditionNode = builder.CreateSpecialNode(foreachStatement, ControlFlowNodeType.LoopCondition);
				Connect(data, conditionNode);
				
				breakTargets.Push(end);
				continueTargets.Push(conditionNode);
				
				ControlFlowNode bodyEnd = HandleEmbeddedStatement(foreachStatement.EmbeddedStatement, conditionNode);
				Connect(bodyEnd, conditionNode);
				
				breakTargets.Pop();
				continueTargets.Pop();
				
				Connect(conditionNode, end);
				builder.nodes.Add(end);
				return end;
			}
			
			public override ControlFlowNode VisitBreakStatement(BreakStatement breakStatement, ControlFlowNode data)
			{
				if (breakTargets.Count > 0)
					Connect(data, breakTargets.Peek(), ControlFlowEdgeType.Jump);
				return builder.CreateEndNode(breakStatement);
			}
			
			public override ControlFlowNode VisitContinueStatement(ContinueStatement continueStatement, ControlFlowNode data)
			{
				if (continueTargets.Count > 0)
					Connect(data, continueTargets.Peek(), ControlFlowEdgeType.Jump);
				return builder.CreateEndNode(continueStatement);
			}
			
			public override ControlFlowNode VisitGotoStatement(GotoStatement gotoStatement, ControlFlowNode data)
			{
				builder.gotoStatements.Add(data);
				return builder.CreateEndNode(gotoStatement);
			}
			
			public override ControlFlowNode VisitReturnStatement(ReturnStatement returnStatement, ControlFlowNode data)
			{
				return builder.CreateEndNode(returnStatement); // end not connected with data
			}
			
			public override ControlFlowNode VisitThrowStatement(ThrowStatement throwStatement, ControlFlowNode data)
			{
				return builder.CreateEndNode(throwStatement); // end not connected with data
			}
			
			public override ControlFlowNode VisitTryCatchStatement(TryCatchStatement tryCatchStatement, ControlFlowNode data)
			{
				ControlFlowNode end = builder.CreateEndNode(tryCatchStatement, addToNodeList: false);
				var edge = Connect(HandleEmbeddedStatement(tryCatchStatement.TryBlock, data), end);
				if (!tryCatchStatement.FinallyBlock.IsNull)
					edge.AddJumpOutOfTryFinally(tryCatchStatement);
				foreach (CatchClause cc in tryCatchStatement.CatchClauses) {
					edge = Connect(HandleEmbeddedStatement(cc.Body, data), end);
					if (!tryCatchStatement.FinallyBlock.IsNull)
						edge.AddJumpOutOfTryFinally(tryCatchStatement);
				}
				if (!tryCatchStatement.FinallyBlock.IsNull) {
					// Don't connect the end of the try-finally block to anything.
					// Consumers of the CFG will have to special-case try-finally.
					HandleEmbeddedStatement(tryCatchStatement.FinallyBlock, data);
				}
				builder.nodes.Add(end);
				return end;
			}
			
			public override ControlFlowNode VisitCheckedStatement(CheckedStatement checkedStatement, ControlFlowNode data)
			{
				ControlFlowNode bodyEnd = HandleEmbeddedStatement(checkedStatement.Body, data);
				return CreateConnectedEndNode(checkedStatement, bodyEnd);
			}
			
			public override ControlFlowNode VisitUncheckedStatement(UncheckedStatement uncheckedStatement, ControlFlowNode data)
			{
				ControlFlowNode bodyEnd = HandleEmbeddedStatement(uncheckedStatement.Body, data);
				return CreateConnectedEndNode(uncheckedStatement, bodyEnd);
			}
			
			public override ControlFlowNode VisitLockStatement(LockStatement lockStatement, ControlFlowNode data)
			{
				ControlFlowNode bodyEnd = HandleEmbeddedStatement(lockStatement.EmbeddedStatement, data);
				return CreateConnectedEndNode(lockStatement, bodyEnd);
			}
			
			public override ControlFlowNode VisitUsingStatement(UsingStatement usingStatement, ControlFlowNode data)
			{
				data = HandleEmbeddedStatement(usingStatement.ResourceAcquisition as Statement, data);
				ControlFlowNode bodyEnd = HandleEmbeddedStatement(usingStatement.EmbeddedStatement, data);
				return CreateConnectedEndNode(usingStatement, bodyEnd);
			}
			
			public override ControlFlowNode VisitYieldStatement(YieldStatement yieldStatement, ControlFlowNode data)
			{
				return CreateConnectedEndNode(yieldStatement, data);
			}
			
			public override ControlFlowNode VisitYieldBreakStatement(YieldBreakStatement yieldBreakStatement, ControlFlowNode data)
			{
				return builder.CreateEndNode(yieldBreakStatement); // end not connected with data
			}
			
			public override ControlFlowNode VisitUnsafeStatement(UnsafeStatement unsafeStatement, ControlFlowNode data)
			{
				ControlFlowNode bodyEnd = HandleEmbeddedStatement(unsafeStatement.Body, data);
				return CreateConnectedEndNode(unsafeStatement, bodyEnd);
			}
			
			public override ControlFlowNode VisitFixedStatement(FixedStatement fixedStatement, ControlFlowNode data)
			{
				ControlFlowNode bodyEnd = HandleEmbeddedStatement(fixedStatement.EmbeddedStatement, data);
				return CreateConnectedEndNode(fixedStatement, bodyEnd);
			}
		}
	}
}
