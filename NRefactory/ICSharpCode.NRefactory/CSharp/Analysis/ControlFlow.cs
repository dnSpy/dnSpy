// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using ICSharpCode.NRefactory.CSharp.Resolver;

namespace ICSharpCode.NRefactory.CSharp.Analysis
{
	/// <summary>
	/// Represents a node in the control flow graph of a C# method.
	/// </summary>
	public class ControlFlowNode
	{
		public readonly Statement PreviousStatement;
		public readonly Statement NextStatement;
		
		public ControlFlowNodeType Type;
		
		public readonly List<ControlFlowNode> Predecessors = new List<ControlFlowNode>();
		public readonly List<ControlFlowNode> Successors = new List<ControlFlowNode>();
		
		public ControlFlowNode(Statement previousStatement, Statement nextStatement)
		{
			if (previousStatement == null && nextStatement == null)
				throw new ArgumentException("previousStatement and nextStatement must not be both null");
			this.PreviousStatement = previousStatement;
			this.NextStatement = nextStatement;
		}
		
		/// <summary>
		/// Creates a control flow edge from <c>source</c> to <c>this</c>.
		/// </summary>
		internal ControlFlowNode ConnectWith(ControlFlowNode source)
		{
			source.Successors.Add(this);
			this.Predecessors.Add(source);
			return this;
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
	
	public class ControlFlowGraphBuilder
	{
		protected virtual ControlFlowNode CreateNode(Statement previousStatement, Statement nextStatement)
		{
			return new ControlFlowNode(previousStatement, nextStatement);
		}
		
		List<ControlFlowNode> nodes;
		Dictionary<string, ControlFlowNode> labels;
		List<ControlFlowNode> gotoStatements;
		
		public ControlFlowNode[] BuildControlFlowGraph(BlockStatement block)
		{
			NodeCreationVisitor nodeCreationVisitor = new NodeCreationVisitor();
			nodeCreationVisitor.builder = this;
			try {
				nodes = new List<ControlFlowNode>();
				labels = new Dictionary<string, ControlFlowNode>();
				gotoStatements = new List<ControlFlowNode>();
				ControlFlowNode entryPoint = CreateStartNode(block);
				nodeCreationVisitor.VisitBlockStatement(block, entryPoint);
				
				// Resolve goto statements:
				foreach (ControlFlowNode gotoStmt in gotoStatements) {
					string label = ((GotoStatement)gotoStmt.NextStatement).Label;
					ControlFlowNode labelNode;
					if (labels.TryGetValue(label, out labelNode))
						labelNode.ConnectWith(gotoStmt);
				}
				
				return nodes.ToArray();
			} finally {
				nodes = null;
				labels = null;
				gotoStatements = null;
			}
		}
		
		ControlFlowNode CreateStartNode(Statement statement)
		{
			ControlFlowNode node = CreateNode(null, statement);
			node.Type = ControlFlowNodeType.StartNode;
			nodes.Add(node);
			return node;
		}
		
		ControlFlowNode CreateSpecialNode(Statement statement, ControlFlowNodeType type)
		{
			ControlFlowNode node = CreateNode(statement, null);
			node.Type = type;
			nodes.Add(node);
			return node;
		}
		
		ControlFlowNode CreateEndNode(Statement statement)
		{
			// Find the next statement in the same role:
			AstNode next = statement;
			do {
				next = next.NextSibling;
			} while (next != null && next.Role != statement.Role);
			
			Statement nextStatement = next as Statement;
			ControlFlowNode node = CreateNode(statement, nextStatement);
			node.Type = nextStatement != null ? ControlFlowNodeType.BetweenStatements : ControlFlowNodeType.EndNode;
			nodes.Add(node);
			return node;
		}
		
		#region Constant evaluation
		/// <summary>
		/// Evaluates an expression.
		/// </summary>
		/// <returns>The constant value of the expression; or null if the expression is not a constant.</returns>
		ConstantResolveResult EvaluateConstant(Expression expr)
		{
			return null; // TODO: implement this using the C# resolver
		}
		
		/// <summary>
		/// Evaluates an expression.
		/// </summary>
		/// <returns>The value of the constant boolean expression; or null if the value is not a constant boolean expression.</returns>
		bool? EvaluateCondition(Expression expr)
		{
			ConstantResolveResult crr = EvaluateConstant(expr);
			if (crr != null)
				return crr.ConstantValue as bool?;
			else
				return null;
		}
		
		bool AreEqualConstants(ConstantResolveResult c1, ConstantResolveResult c2)
		{
			return false; // TODO: implement this using the resolver's operator==
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
			
			protected override ControlFlowNode VisitChildren(AstNode node, ControlFlowNode data)
			{
				// We have overrides for all possible expressions and should visit expressions only.
				throw new NotImplementedException();
			}
			
			public override ControlFlowNode VisitBlockStatement(BlockStatement blockStatement, ControlFlowNode data)
			{
				// C# 4.0 spec: §8.2 Blocks
				ControlFlowNode childNode = HandleStatementList(blockStatement.Statements, data);
				return builder.CreateEndNode(blockStatement).ConnectWith(childNode);
			}
			
			ControlFlowNode HandleStatementList(AstNodeCollection<Statement> statements, ControlFlowNode source)
			{
				ControlFlowNode childNode = null;
				foreach (Statement stmt in statements) {
					if (childNode == null) {
						childNode = builder.CreateStartNode(stmt);
						childNode.ConnectWith(source);
					}
					Debug.Assert(childNode.NextStatement == stmt);
					childNode = stmt.AcceptVisitor(this, childNode);
					Debug.Assert(childNode.PreviousStatement == stmt);
				}
				return childNode ?? source;
			}
			
			public override ControlFlowNode VisitEmptyStatement(EmptyStatement emptyStatement, ControlFlowNode data)
			{
				return builder.CreateEndNode(emptyStatement).ConnectWith(data);
			}
			
			public override ControlFlowNode VisitLabelStatement(LabelStatement labelStatement, ControlFlowNode data)
			{
				ControlFlowNode end = builder.CreateEndNode(labelStatement);
				builder.labels[labelStatement.Label] = end;
				return end.ConnectWith(data);
			}
			
			public override ControlFlowNode VisitVariableDeclarationStatement(VariableDeclarationStatement variableDeclarationStatement, ControlFlowNode data)
			{
				return builder.CreateEndNode(variableDeclarationStatement).ConnectWith(data);
			}
			
			public override ControlFlowNode VisitExpressionStatement(ExpressionStatement expressionStatement, ControlFlowNode data)
			{
				return builder.CreateEndNode(expressionStatement).ConnectWith(data);
			}
			
			public override ControlFlowNode VisitIfElseStatement(IfElseStatement ifElseStatement, ControlFlowNode data)
			{
				bool? cond = builder.EvaluateCondition(ifElseStatement.Condition);
				ControlFlowNode trueBegin = builder.CreateStartNode(ifElseStatement.TrueStatement);
				if (cond != false)
					trueBegin.ConnectWith(data);
				ControlFlowNode trueEnd = ifElseStatement.TrueStatement.AcceptVisitor(this, trueBegin);
				ControlFlowNode falseEnd;
				if (ifElseStatement.FalseStatement.IsNull) {
					falseEnd = null;
				} else {
					ControlFlowNode falseBegin = builder.CreateStartNode(ifElseStatement.FalseStatement);
					if (cond != true)
						falseBegin.ConnectWith(data);
					falseEnd = ifElseStatement.FalseStatement.AcceptVisitor(this, falseBegin);
				}
				ControlFlowNode end = builder.CreateEndNode(ifElseStatement);
				end.ConnectWith(trueEnd);
				if (falseEnd != null) {
					end.ConnectWith(falseEnd);
				} else if (cond != true) {
					end.ConnectWith(data);
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
				
				ControlFlowNode end = builder.CreateEndNode(switchStatement);
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
					end.ConnectWith(data);
				}
				
				if (gotoCaseOrDefault.Count > gotoCaseOrDefaultInOuterScope) {
					// Resolve 'goto case' statements:
					throw new NotImplementedException();
				}
				
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
				ControlFlowNode end = builder.CreateEndNode(whileStatement);
				ControlFlowNode conditionNode = builder.CreateSpecialNode(whileStatement, ControlFlowNodeType.LoopCondition);
				breakTargets.Push(end);
				continueTargets.Push(conditionNode);
				
				conditionNode.ConnectWith(data);
				
				bool? cond = builder.EvaluateCondition(whileStatement.Condition);
				ControlFlowNode bodyStart = builder.CreateStartNode(whileStatement.EmbeddedStatement);
				if (cond != false)
					bodyStart.ConnectWith(conditionNode);
				ControlFlowNode bodyEnd = whileStatement.EmbeddedStatement.AcceptVisitor(this, bodyStart);
				conditionNode.ConnectWith(bodyEnd);
				if (cond != true)
					end.ConnectWith(conditionNode);
				
				breakTargets.Pop();
				continueTargets.Pop();
				return end;
			}
			
			public override ControlFlowNode VisitDoWhileStatement(DoWhileStatement doWhileStatement, ControlFlowNode data)
			{
				// <data> do { <bodyStart> embeddedStmt; <bodyEnd>} <condition> while(cond); <end>
				ControlFlowNode end = builder.CreateEndNode(doWhileStatement);
				ControlFlowNode conditionNode = builder.CreateSpecialNode(doWhileStatement, ControlFlowNodeType.LoopCondition);
				breakTargets.Push(end);
				continueTargets.Push(conditionNode);
				
				ControlFlowNode bodyStart = builder.CreateStartNode(doWhileStatement.EmbeddedStatement);
				bodyStart.ConnectWith(data);
				ControlFlowNode bodyEnd = doWhileStatement.EmbeddedStatement.AcceptVisitor(this, bodyStart);
				conditionNode.ConnectWith(bodyEnd);
				
				bool? cond = builder.EvaluateCondition(doWhileStatement.Condition);
				if (cond != false)
					bodyStart.ConnectWith(conditionNode);
				if (cond != true)
					end.ConnectWith(conditionNode);
				
				breakTargets.Pop();
				continueTargets.Pop();
				return end;
			}
			
			public override ControlFlowNode VisitForStatement(ForStatement forStatement, ControlFlowNode data)
			{
				data = HandleStatementList(forStatement.Initializers, data);
				// for (initializers <data>; <condition>cond; <iteratorStart>iterators<iteratorEnd>) { <bodyStart> embeddedStmt; <bodyEnd> } <end>
				ControlFlowNode end = builder.CreateEndNode(forStatement);
				ControlFlowNode conditionNode = builder.CreateSpecialNode(forStatement, ControlFlowNodeType.LoopCondition);
				conditionNode.ConnectWith(data);
				
				int iteratorStartNodeID = builder.nodes.Count;
				ControlFlowNode iteratorEnd = HandleStatementList(forStatement.Iterators, null);
				ControlFlowNode iteratorStart;
				if (iteratorEnd != null) {
					iteratorStart = builder.nodes[iteratorStartNodeID];
					iteratorEnd.ConnectWith(conditionNode);
				} else {
					iteratorStart = conditionNode;
				}
				
				breakTargets.Push(end);
				continueTargets.Push(iteratorStart);
				
				ControlFlowNode bodyStart = builder.CreateStartNode(forStatement.EmbeddedStatement);
				ControlFlowNode bodyEnd = forStatement.EmbeddedStatement.AcceptVisitor(this, bodyStart);
				iteratorStart.ConnectWith(bodyEnd);
				
				breakTargets.Pop();
				continueTargets.Pop();
				
				bool? cond = forStatement.Condition.IsNull ? true : builder.EvaluateCondition(forStatement.Condition);
				if (cond != false)
					bodyStart.ConnectWith(conditionNode);
				if (cond != true)
					end.ConnectWith(conditionNode);
				
				return end;
			}
			
			ControlFlowNode HandleEmbeddedStatement(Statement embeddedStatement, ControlFlowNode source)
			{
				if (embeddedStatement == null || embeddedStatement.IsNull)
					return source;
				ControlFlowNode bodyStart = builder.CreateStartNode(embeddedStatement);
				if (source != null)
					bodyStart.ConnectWith(source);
				return embeddedStatement.AcceptVisitor(this, bodyStart);
			}
			
			public override ControlFlowNode VisitForeachStatement(ForeachStatement foreachStatement, ControlFlowNode data)
			{
				// <data> foreach (<condition>...) { <bodyStart>embeddedStmt<bodyEnd> } <end>
				ControlFlowNode end = builder.CreateEndNode(foreachStatement);
				ControlFlowNode conditionNode = builder.CreateSpecialNode(foreachStatement, ControlFlowNodeType.LoopCondition);
				conditionNode.ConnectWith(data);
				
				breakTargets.Push(end);
				continueTargets.Push(conditionNode);
				
				ControlFlowNode bodyEnd = HandleEmbeddedStatement(foreachStatement.EmbeddedStatement, conditionNode);
				conditionNode.ConnectWith(bodyEnd);
				
				breakTargets.Pop();
				continueTargets.Pop();
				
				return end.ConnectWith(conditionNode);
			}
			
			public override ControlFlowNode VisitBreakStatement(BreakStatement breakStatement, ControlFlowNode data)
			{
				if (breakTargets.Count > 0)
					breakTargets.Peek().ConnectWith(data);
				return builder.CreateEndNode(breakStatement);
			}
			
			public override ControlFlowNode VisitContinueStatement(ContinueStatement continueStatement, ControlFlowNode data)
			{
				if (continueTargets.Count > 0)
					continueTargets.Peek().ConnectWith(data);
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
				ControlFlowNode end = builder.CreateEndNode(tryCatchStatement);
				end.ConnectWith(HandleEmbeddedStatement(tryCatchStatement.TryBlock, data));
				foreach (CatchClause cc in tryCatchStatement.CatchClauses) {
					end.ConnectWith(HandleEmbeddedStatement(cc.Body, data));
				}
				if (!tryCatchStatement.FinallyBlock.IsNull) {
					// Don't connect the end of the try-finally block to anything.
					// Consumers of the CFG will have to special-case try-finally.
					HandleEmbeddedStatement(tryCatchStatement.FinallyBlock, data);
				}
				return end;
			}
			
			public override ControlFlowNode VisitCheckedStatement(CheckedStatement checkedStatement, ControlFlowNode data)
			{
				ControlFlowNode bodyEnd = HandleEmbeddedStatement(checkedStatement.Body, data);
				return builder.CreateEndNode(checkedStatement).ConnectWith(bodyEnd);
			}
			
			public override ControlFlowNode VisitUncheckedStatement(UncheckedStatement uncheckedStatement, ControlFlowNode data)
			{
				ControlFlowNode bodyEnd = HandleEmbeddedStatement(uncheckedStatement.Body, data);
				return builder.CreateEndNode(uncheckedStatement).ConnectWith(bodyEnd);
			}
			
			public override ControlFlowNode VisitLockStatement(LockStatement lockStatement, ControlFlowNode data)
			{
				ControlFlowNode bodyEnd = HandleEmbeddedStatement(lockStatement.EmbeddedStatement, data);
				return builder.CreateEndNode(lockStatement).ConnectWith(bodyEnd);
			}
			
			public override ControlFlowNode VisitUsingStatement(UsingStatement usingStatement, ControlFlowNode data)
			{
				data = HandleEmbeddedStatement(usingStatement.ResourceAcquisition as Statement, data);
				ControlFlowNode bodyEnd = HandleEmbeddedStatement(usingStatement.EmbeddedStatement, data);
				return builder.CreateEndNode(usingStatement).ConnectWith(bodyEnd);
			}
			
			public override ControlFlowNode VisitYieldStatement(YieldStatement yieldStatement, ControlFlowNode data)
			{
				return builder.CreateEndNode(yieldStatement).ConnectWith(data);
			}
			
			public override ControlFlowNode VisitYieldBreakStatement(YieldBreakStatement yieldBreakStatement, ControlFlowNode data)
			{
				return builder.CreateEndNode(yieldBreakStatement); // end not connected with data
			}
			
			public override ControlFlowNode VisitUnsafeStatement(UnsafeStatement unsafeStatement, ControlFlowNode data)
			{
				ControlFlowNode bodyEnd = HandleEmbeddedStatement(unsafeStatement.Body, data);
				return builder.CreateEndNode(unsafeStatement).ConnectWith(bodyEnd);
			}
			
			public override ControlFlowNode VisitFixedStatement(FixedStatement fixedStatement, ControlFlowNode data)
			{
				ControlFlowNode bodyEnd = HandleEmbeddedStatement(fixedStatement.EmbeddedStatement, data);
				return builder.CreateEndNode(fixedStatement).ConnectWith(bodyEnd);
			}
		}
	}
}
