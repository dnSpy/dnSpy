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
using System.Diagnostics;
using System.Linq;
using System.Threading;
using ICSharpCode.NRefactory.CSharp.Resolver;
using ICSharpCode.NRefactory.CSharp.TypeSystem;
using ICSharpCode.NRefactory.Semantics;
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.TypeSystem.Implementation;
using ICSharpCode.NRefactory.Utils;

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
			cancellationToken.ThrowIfCancellationRequested();
			return new ControlFlowNode(previousStatement, nextStatement, type);
		}
		
		protected virtual ControlFlowEdge CreateEdge(ControlFlowNode from, ControlFlowNode to, ControlFlowEdgeType type)
		{
			cancellationToken.ThrowIfCancellationRequested();
			return new ControlFlowEdge(from, to, type);
		}
		
		Statement rootStatement;
		CSharpTypeResolveContext typeResolveContext;
		Func<AstNode, CancellationToken, ResolveResult> resolver;
		List<ControlFlowNode> nodes;
		Dictionary<string, ControlFlowNode> labels;
		List<ControlFlowNode> gotoStatements;
		CancellationToken cancellationToken;
		
		public IList<ControlFlowNode> BuildControlFlowGraph(Statement statement, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (statement == null)
				throw new ArgumentNullException("statement");
			CSharpResolver r = new CSharpResolver(MinimalCorlib.Instance.CreateCompilation());
			return BuildControlFlowGraph(statement, new CSharpAstResolver(r, statement), cancellationToken);
		}
		
		public IList<ControlFlowNode> BuildControlFlowGraph(Statement statement, CSharpAstResolver resolver, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (statement == null)
				throw new ArgumentNullException("statement");
			if (resolver == null)
				throw new ArgumentNullException("resolver");
			return BuildControlFlowGraph(statement, resolver.Resolve, resolver.TypeResolveContext, cancellationToken);
		}
		
		internal IList<ControlFlowNode> BuildControlFlowGraph(Statement statement, Func<AstNode, CancellationToken, ResolveResult> resolver, CSharpTypeResolveContext typeResolveContext, CancellationToken cancellationToken)
		{
			NodeCreationVisitor nodeCreationVisitor = new NodeCreationVisitor();
			nodeCreationVisitor.builder = this;
			try {
				this.nodes = new List<ControlFlowNode>();
				this.labels = new Dictionary<string, ControlFlowNode>();
				this.gotoStatements = new List<ControlFlowNode>();
				this.rootStatement = statement;
				this.resolver = resolver;
				this.typeResolveContext = typeResolveContext;
				this.cancellationToken = cancellationToken;
				
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
				this.resolver = null;
				this.typeResolveContext = null;
				this.cancellationToken = CancellationToken.None;
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
			if (statement.IsNull)
				return null;
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
		ResolveResult EvaluateConstant(Expression expr)
		{
			if (expr.IsNull)
				return null;
			if (EvaluateOnlyPrimitiveConstants) {
				if (!(expr is PrimitiveExpression || expr is NullReferenceExpression))
					return null;
			}
			return resolver(expr, cancellationToken);
		}
		
		/// <summary>
		/// Evaluates an expression.
		/// </summary>
		/// <returns>The value of the constant boolean expression; or null if the value is not a constant boolean expression.</returns>
		bool? EvaluateCondition(Expression expr)
		{
			ResolveResult rr = EvaluateConstant(expr);
			if (rr != null && rr.IsCompileTimeConstant)
				return rr.ConstantValue as bool?;
			else
				return null;
		}
		
		bool AreEqualConstants(ResolveResult c1, ResolveResult c2)
		{
			if (c1 == null || c2 == null || !c1.IsCompileTimeConstant || !c2.IsCompileTimeConstant)
				return false;
			CSharpResolver r = new CSharpResolver(typeResolveContext);
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
				if (from == null || to == null)
					return null;
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
				// We have overrides for all possible statements and should visit statements only.
				throw new NotSupportedException();
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
				
				ControlFlowNode falseBegin = builder.CreateStartNode(ifElseStatement.FalseStatement);
				if (cond != true)
					Connect(data, falseBegin, ControlFlowEdgeType.ConditionFalse);
				ControlFlowNode falseEnd = ifElseStatement.FalseStatement.AcceptVisitor(this, falseBegin);
				// (if no else statement exists, both falseBegin and falseEnd will be null)
				
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
				ResolveResult constant = builder.EvaluateConstant(switchStatement.Expression);
				SwitchSection defaultSection = null;
				SwitchSection sectionMatchedByConstant = null;
				foreach (SwitchSection section in switchStatement.SwitchSections) {
					foreach (CaseLabel label in section.CaseLabels) {
						if (label.Expression.IsNull) {
							defaultSection = section;
						} else if (constant != null && constant.IsCompileTimeConstant) {
							ResolveResult labelConstant = builder.EvaluateConstant(label.Expression);
							if (builder.AreEqualConstants(constant, labelConstant))
								sectionMatchedByConstant = section;
						}
					}
				}
				if (constant != null && constant.IsCompileTimeConstant && sectionMatchedByConstant == null)
					sectionMatchedByConstant = defaultSection;
				
				int gotoCaseOrDefaultInOuterScope = gotoCaseOrDefault.Count;
				List<ControlFlowNode> sectionStartNodes = new List<ControlFlowNode>();
				
				ControlFlowNode end = builder.CreateEndNode(switchStatement, addToNodeList: false);
				breakTargets.Push(end);
				foreach (SwitchSection section in switchStatement.SwitchSections) {
					int sectionStartNodeID = builder.nodes.Count;
					if (constant == null || !constant.IsCompileTimeConstant || section == sectionMatchedByConstant) {
						HandleStatementList(section.Statements, data);
					} else {
						// This section is unreachable: pass null to HandleStatementList.
						HandleStatementList(section.Statements, null);
					}
					// Don't bother connecting the ends of the sections: the 'break' statement takes care of that.
					
					// Store the section start node for 'goto case' statements.
					sectionStartNodes.Add(sectionStartNodeID < builder.nodes.Count ? builder.nodes[sectionStartNodeID] : null);
				}
				breakTargets.Pop();
				if (defaultSection == null && sectionMatchedByConstant == null) {
					Connect(data, end);
				}
				
				if (gotoCaseOrDefault.Count > gotoCaseOrDefaultInOuterScope) {
					// Resolve 'goto case' statements:
					for (int i = gotoCaseOrDefaultInOuterScope; i < gotoCaseOrDefault.Count; i++) {
						ControlFlowNode gotoCaseNode = gotoCaseOrDefault[i];
						GotoCaseStatement gotoCaseStatement = gotoCaseNode.NextStatement as GotoCaseStatement;
						ResolveResult gotoCaseConstant = null;
						if (gotoCaseStatement != null) {
							gotoCaseConstant = builder.EvaluateConstant(gotoCaseStatement.LabelExpression);
						}
						int targetSectionIndex = -1;
						int currentSectionIndex = 0;
						foreach (SwitchSection section in switchStatement.SwitchSections) {
							foreach (CaseLabel label in section.CaseLabels) {
								if (gotoCaseStatement != null) {
									// goto case
									if (!label.Expression.IsNull) {
										ResolveResult labelConstant = builder.EvaluateConstant(label.Expression);
										if (builder.AreEqualConstants(gotoCaseConstant, labelConstant))
											targetSectionIndex = currentSectionIndex;
									}
								} else {
									// goto default
									if (label.Expression.IsNull)
										targetSectionIndex = currentSectionIndex;
								}
							}
							currentSectionIndex++;
						}
						if (targetSectionIndex >= 0 && sectionStartNodes[targetSectionIndex] != null)
							Connect(gotoCaseNode, sectionStartNodes[targetSectionIndex], ControlFlowEdgeType.Jump);
						else
							Connect(gotoCaseNode, end, ControlFlowEdgeType.Jump);
					}
					gotoCaseOrDefault.RemoveRange(gotoCaseOrDefaultInOuterScope, gotoCaseOrDefault.Count - gotoCaseOrDefaultInOuterScope);
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
			
			public override ControlFlowNode VisitYieldReturnStatement(YieldReturnStatement yieldStatement, ControlFlowNode data)
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
		
		/// <summary>
		/// Debugging helper that exports a control flow graph.
		/// </summary>
		public static GraphVizGraph ExportGraph(IList<ControlFlowNode> nodes)
		{
			GraphVizGraph g = new GraphVizGraph();
			GraphVizNode[] n = new GraphVizNode[nodes.Count];
			Dictionary<ControlFlowNode, int> dict = new Dictionary<ControlFlowNode, int>();
			for (int i = 0; i < n.Length; i++) {
				dict.Add(nodes[i], i);
				n[i] = new GraphVizNode(i);
				string name = "#" + i + " = ";
				switch (nodes[i].Type) {
					case ControlFlowNodeType.StartNode:
					case ControlFlowNodeType.BetweenStatements:
						name += nodes[i].NextStatement.DebugToString();
						break;
					case ControlFlowNodeType.EndNode:
						name += "End of " + nodes[i].PreviousStatement.DebugToString();
						break;
					case ControlFlowNodeType.LoopCondition:
						name += "Condition in " + nodes[i].NextStatement.DebugToString();
						break;
					default:
						name += "?";
						break;
				}
				n[i].label = name;
				g.AddNode(n[i]);
			}
			for (int i = 0; i < n.Length; i++) {
				foreach (ControlFlowEdge edge in nodes[i].Outgoing) {
					GraphVizEdge ge = new GraphVizEdge(i, dict[edge.To]);
					if (edge.IsLeavingTryFinally)
						ge.style = "dashed";
					switch (edge.Type) {
						case ControlFlowEdgeType.ConditionTrue:
							ge.color = "green";
							break;
						case ControlFlowEdgeType.ConditionFalse:
							ge.color = "red";
							break;
						case ControlFlowEdgeType.Jump:
							ge.color = "blue";
							break;
					}
					g.AddEdge(ge);
				}
			}
			return g;
		}
	}
}
