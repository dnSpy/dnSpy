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
using ICSharpCode.NRefactory.Semantics;
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.TypeSystem.Implementation;
using ICSharpCode.NRefactory.Utils;

namespace ICSharpCode.NRefactory.CSharp.Analysis
{
	/// <summary>
	/// Represents the definite assignment status of a variable at a specific location.
	/// </summary>
	public enum DefiniteAssignmentStatus
	{
		/// <summary>
		/// The variable might be assigned or unassigned.
		/// </summary>
		PotentiallyAssigned,
		/// <summary>
		/// The variable is definitely assigned.
		/// </summary>
		DefinitelyAssigned,
		/// <summary>
		/// The variable is definitely assigned iff the expression results in the value 'true'.
		/// </summary>
		AssignedAfterTrueExpression,
		/// <summary>
		/// The variable is definitely assigned iff the expression results in the value 'false'.
		/// </summary>
		AssignedAfterFalseExpression,
		/// <summary>
		/// The code is unreachable.
		/// </summary>
		CodeUnreachable
	}
	
	/// <summary>
	/// Implements the C# definite assignment analysis (C# 4.0 Spec: §5.3 Definite assignment)
	/// </summary>
	public class DefiniteAssignmentAnalysis
	{
		sealed class DefiniteAssignmentNode : ControlFlowNode
		{
			public int Index;
			public DefiniteAssignmentStatus NodeStatus;
			
			public DefiniteAssignmentNode(Statement previousStatement, Statement nextStatement, ControlFlowNodeType type)
				: base(previousStatement, nextStatement, type)
			{
			}
		}
		
		sealed class DerivedControlFlowGraphBuilder : ControlFlowGraphBuilder
		{
			protected override ControlFlowNode CreateNode(Statement previousStatement, Statement nextStatement, ControlFlowNodeType type)
			{
				return new DefiniteAssignmentNode(previousStatement, nextStatement, type);
			}
		}
		
		readonly DefiniteAssignmentVisitor visitor = new DefiniteAssignmentVisitor();
		readonly List<DefiniteAssignmentNode> allNodes = new List<DefiniteAssignmentNode>();
		readonly Dictionary<Statement, DefiniteAssignmentNode> beginNodeDict = new Dictionary<Statement, DefiniteAssignmentNode>();
		readonly Dictionary<Statement, DefiniteAssignmentNode> endNodeDict = new Dictionary<Statement, DefiniteAssignmentNode>();
		readonly Dictionary<Statement, DefiniteAssignmentNode> conditionNodeDict = new Dictionary<Statement, DefiniteAssignmentNode>();
		readonly CSharpAstResolver resolver;
		Dictionary<ControlFlowEdge, DefiniteAssignmentStatus> edgeStatus = new Dictionary<ControlFlowEdge, DefiniteAssignmentStatus>();
		
		string variableName;
		List<IdentifierExpression> unassignedVariableUses = new List<IdentifierExpression>();
		int analyzedRangeStart, analyzedRangeEnd;
		CancellationToken analysisCancellationToken;
		
		Queue<DefiniteAssignmentNode> nodesWithModifiedInput = new Queue<DefiniteAssignmentNode>();
		
		public DefiniteAssignmentAnalysis(Statement rootStatement, CancellationToken cancellationToken)
			: this(rootStatement,
			       new CSharpAstResolver(new CSharpResolver(MinimalCorlib.Instance.CreateCompilation()), rootStatement),
			       cancellationToken)
		{
		}
		
		public DefiniteAssignmentAnalysis(Statement rootStatement, CSharpAstResolver resolver, CancellationToken cancellationToken)
		{
			if (rootStatement == null)
				throw new ArgumentNullException("rootStatement");
			if (resolver == null)
				throw new ArgumentNullException("resolver");
			this.resolver = resolver;
			
			visitor.analysis = this;
			DerivedControlFlowGraphBuilder cfgBuilder = new DerivedControlFlowGraphBuilder();
			if (resolver.TypeResolveContext.Compilation.MainAssembly.UnresolvedAssembly is MinimalCorlib) {
				cfgBuilder.EvaluateOnlyPrimitiveConstants = true;
			}
			allNodes.AddRange(cfgBuilder.BuildControlFlowGraph(rootStatement, resolver, cancellationToken).Cast<DefiniteAssignmentNode>());
			for (int i = 0; i < allNodes.Count; i++) {
				DefiniteAssignmentNode node = allNodes[i];
				node.Index = i; // assign numbers to the nodes
				if (node.Type == ControlFlowNodeType.StartNode || node.Type == ControlFlowNodeType.BetweenStatements) {
					// Anonymous methods have separate control flow graphs, but we also need to analyze those.
					// Iterate backwards so that anonymous methods are inserted in the correct order
					for (AstNode child = node.NextStatement.LastChild; child != null; child = child.PrevSibling) {
						InsertAnonymousMethods(i + 1, child, cfgBuilder, cancellationToken);
					}
				}
				// Now register the node in the dictionaries:
				if (node.Type == ControlFlowNodeType.StartNode || node.Type == ControlFlowNodeType.BetweenStatements)
					beginNodeDict.Add(node.NextStatement, node);
				if (node.Type == ControlFlowNodeType.BetweenStatements || node.Type == ControlFlowNodeType.EndNode)
					endNodeDict.Add(node.PreviousStatement, node);
				if (node.Type == ControlFlowNodeType.LoopCondition)
					conditionNodeDict.Add(node.NextStatement, node);
			}
			// Verify that we created nodes for all statements:
			Debug.Assert(!rootStatement.DescendantsAndSelf.OfType<Statement>().Except(allNodes.Select(n => n.NextStatement)).Any());
			// Verify that we put all nodes into the dictionaries:
			Debug.Assert(rootStatement.DescendantsAndSelf.OfType<Statement>().All(stmt => beginNodeDict.ContainsKey(stmt)));
			Debug.Assert(rootStatement.DescendantsAndSelf.OfType<Statement>().All(stmt => endNodeDict.ContainsKey(stmt)));
			
			this.analyzedRangeStart = 0;
			this.analyzedRangeEnd = allNodes.Count - 1;
		}
		
		void InsertAnonymousMethods(int insertPos, AstNode node, ControlFlowGraphBuilder cfgBuilder, CancellationToken cancellationToken)
		{
			// Ignore any statements, as those have their own ControlFlowNode and get handled separately
			if (node is Statement)
				return;
			AnonymousMethodExpression ame = node as AnonymousMethodExpression;
			if (ame != null) {
				allNodes.InsertRange(insertPos, cfgBuilder.BuildControlFlowGraph(ame.Body, resolver, cancellationToken).Cast<DefiniteAssignmentNode>());
				return;
			}
			LambdaExpression lambda = node as LambdaExpression;
			if (lambda != null && lambda.Body is Statement) {
				allNodes.InsertRange(insertPos, cfgBuilder.BuildControlFlowGraph((Statement)lambda.Body, resolver, cancellationToken).Cast<DefiniteAssignmentNode>());
				return;
			}
			// Descend into child expressions
			// Iterate backwards so that anonymous methods are inserted in the correct order
			for (AstNode child = node.LastChild; child != null; child = child.PrevSibling) {
				InsertAnonymousMethods(insertPos, child, cfgBuilder, cancellationToken);
			}
		}
		
		/// <summary>
		/// Gets the unassigned usages of the previously analyzed variable.
		/// </summary>
		public IList<IdentifierExpression> UnassignedVariableUses {
			get {
				return unassignedVariableUses.AsReadOnly();
			}
		}
		
		/// <summary>
		/// Sets the range of statements to be analyzed.
		/// This method can be used to restrict the analysis to only a part of the method.
		/// Only the control flow paths that are fully contained within the selected part will be analyzed.
		/// </summary>
		/// <remarks>By default, both 'start' and 'end' are inclusive.</remarks>
		public void SetAnalyzedRange(Statement start, Statement end, bool startInclusive = true, bool endInclusive = true)
		{
			var dictForStart = startInclusive ? beginNodeDict : endNodeDict;
			var dictForEnd = endInclusive ? endNodeDict : beginNodeDict;
			Debug.Assert(dictForStart.ContainsKey(start) && dictForEnd.ContainsKey(end));
			int startIndex = dictForStart[start].Index;
			int endIndex = dictForEnd[end].Index;
			if (startIndex > endIndex)
				throw new ArgumentException("The start statement must be lexically preceding the end statement");
			this.analyzedRangeStart = startIndex;
			this.analyzedRangeEnd = endIndex;
		}
		
		public void Analyze(string variable, DefiniteAssignmentStatus initialStatus = DefiniteAssignmentStatus.PotentiallyAssigned, CancellationToken cancellationToken = default(CancellationToken))
		{
			this.analysisCancellationToken = cancellationToken;
			this.variableName = variable;
			try {
				// Reset the status:
				unassignedVariableUses.Clear();
				foreach (DefiniteAssignmentNode node in allNodes) {
					node.NodeStatus = DefiniteAssignmentStatus.CodeUnreachable;
					foreach (ControlFlowEdge edge in node.Outgoing)
						edgeStatus[edge] = DefiniteAssignmentStatus.CodeUnreachable;
				}
				
				ChangeNodeStatus(allNodes[analyzedRangeStart], initialStatus);
				// Iterate as long as the input status of some nodes is changing:
				while (nodesWithModifiedInput.Count > 0) {
					DefiniteAssignmentNode node = nodesWithModifiedInput.Dequeue();
					DefiniteAssignmentStatus inputStatus = DefiniteAssignmentStatus.CodeUnreachable;
					foreach (ControlFlowEdge edge in node.Incoming) {
						inputStatus = MergeStatus(inputStatus, edgeStatus[edge]);
					}
					ChangeNodeStatus(node, inputStatus);
				}
			} finally {
				this.analysisCancellationToken = CancellationToken.None;
				this.variableName = null;
			}
		}
		
		public DefiniteAssignmentStatus GetStatusBefore(Statement statement)
		{
			return beginNodeDict[statement].NodeStatus;
		}
		
		public DefiniteAssignmentStatus GetStatusAfter(Statement statement)
		{
			return endNodeDict[statement].NodeStatus;
		}
		
		public DefiniteAssignmentStatus GetStatusBeforeLoopCondition(Statement statement)
		{
			return conditionNodeDict[statement].NodeStatus;
		}
		
		/// <summary>
		/// Exports the CFG. This method is intended to help debugging issues related to definite assignment.
		/// </summary>
		public GraphVizGraph ExportGraph()
		{
			GraphVizGraph g = new GraphVizGraph();
			g.Title = "DefiniteAssignment - " + variableName;
			for (int i = 0; i < allNodes.Count; i++) {
				string name = "#" + i + " = " + allNodes[i].NodeStatus.ToString() + Environment.NewLine;
				switch (allNodes[i].Type) {
					case ControlFlowNodeType.StartNode:
					case ControlFlowNodeType.BetweenStatements:
						name += allNodes[i].NextStatement.ToString();
						break;
					case ControlFlowNodeType.EndNode:
						name += "End of " + allNodes[i].PreviousStatement.ToString();
						break;
					case ControlFlowNodeType.LoopCondition:
						name += "Condition in " + allNodes[i].NextStatement.ToString();
						break;
					default:
						name += allNodes[i].Type.ToString();
						break;
				}
				g.AddNode(new GraphVizNode(i) { label = name });
				foreach (ControlFlowEdge edge in allNodes[i].Outgoing) {
					GraphVizEdge ge = new GraphVizEdge(i, ((DefiniteAssignmentNode)edge.To).Index);
					if (edgeStatus.Count > 0)
						ge.label = edgeStatus[edge].ToString();
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
		
		static DefiniteAssignmentStatus MergeStatus(DefiniteAssignmentStatus a, DefiniteAssignmentStatus b)
		{
			// The result will be DefinitelyAssigned if at least one incoming edge is DefinitelyAssigned and all others are unreachable.
			// The result will be DefinitelyUnassigned if at least one incoming edge is DefinitelyUnassigned and all others are unreachable.
			// The result will be Unreachable if all incoming edges are unreachable.
			// Otherwise, the result will be PotentiallyAssigned.
			
			if (a == b)
				return a;
			else if (a == DefiniteAssignmentStatus.CodeUnreachable)
				return b;
			else if (b == DefiniteAssignmentStatus.CodeUnreachable)
				return a;
			else
				return DefiniteAssignmentStatus.PotentiallyAssigned;
		}
		
		void ChangeNodeStatus (DefiniteAssignmentNode node, DefiniteAssignmentStatus inputStatus)
		{
			if (node.NodeStatus == inputStatus)
				return;
			node.NodeStatus = inputStatus;
			DefiniteAssignmentStatus outputStatus;
			switch (node.Type) {
			case ControlFlowNodeType.StartNode:
			case ControlFlowNodeType.BetweenStatements:
				if (node.NextStatement is IfElseStatement) {
					// Handle if-else as a condition node
						goto case ControlFlowNodeType.LoopCondition;
				}
				if (inputStatus == DefiniteAssignmentStatus.DefinitelyAssigned) {
					// There isn't any way to un-assign variables, so we don't have to check the expression
					// if the status already is definitely assigned.
					outputStatus = DefiniteAssignmentStatus.DefinitelyAssigned;
				} else {
					outputStatus = CleanSpecialValues (node.NextStatement.AcceptVisitor (visitor, inputStatus));
				}
				break;
			case ControlFlowNodeType.EndNode:
				outputStatus = inputStatus;
				if (node.PreviousStatement.Role == TryCatchStatement.FinallyBlockRole
					&& (outputStatus == DefiniteAssignmentStatus.DefinitelyAssigned || outputStatus == DefiniteAssignmentStatus.PotentiallyAssigned)) {
					TryCatchStatement tryFinally = (TryCatchStatement)node.PreviousStatement.Parent;
					// Changing the status on a finally block potentially changes the status of all edges leaving that finally block:
					foreach (ControlFlowEdge edge in allNodes.SelectMany(n => n.Outgoing)) {
						if (edge.IsLeavingTryFinally && edge.TryFinallyStatements.Contains (tryFinally)) {
							DefiniteAssignmentStatus s = edgeStatus [edge];
							if (s == DefiniteAssignmentStatus.PotentiallyAssigned) {
								ChangeEdgeStatus (edge, outputStatus);
							}
						}
					}
				}
				break;
			case ControlFlowNodeType.LoopCondition:
				ForeachStatement foreachStmt = node.NextStatement as ForeachStatement;
				if (foreachStmt != null) {
					outputStatus = CleanSpecialValues (foreachStmt.InExpression.AcceptVisitor (visitor, inputStatus));
					if (foreachStmt.VariableName == this.variableName)
						outputStatus = DefiniteAssignmentStatus.DefinitelyAssigned;
					break;
				} else {
					Debug.Assert (node.NextStatement is IfElseStatement || node.NextStatement is WhileStatement || node.NextStatement is ForStatement || node.NextStatement is DoWhileStatement);
					Expression condition = node.NextStatement.GetChildByRole (Roles.Condition);
						if (condition.IsNull)
							outputStatus = inputStatus;
						else
							outputStatus = condition.AcceptVisitor(visitor, inputStatus);
						foreach (ControlFlowEdge edge in node.Outgoing) {
							if (edge.Type == ControlFlowEdgeType.ConditionTrue && outputStatus == DefiniteAssignmentStatus.AssignedAfterTrueExpression) {
								ChangeEdgeStatus(edge, DefiniteAssignmentStatus.DefinitelyAssigned);
							} else if (edge.Type == ControlFlowEdgeType.ConditionFalse && outputStatus == DefiniteAssignmentStatus.AssignedAfterFalseExpression) {
								ChangeEdgeStatus(edge, DefiniteAssignmentStatus.DefinitelyAssigned);
							} else {
								ChangeEdgeStatus(edge, CleanSpecialValues(outputStatus));
							}
						}
						return;
					}
				default:
					throw new InvalidOperationException();
			}
			foreach (ControlFlowEdge edge in node.Outgoing) {
				ChangeEdgeStatus(edge, outputStatus);
			}
		}
		
		void ChangeEdgeStatus(ControlFlowEdge edge, DefiniteAssignmentStatus newStatus)
		{
			DefiniteAssignmentStatus oldStatus = edgeStatus[edge];
			if (oldStatus == newStatus)
				return;
			// Ensure that status can cannot change back to CodeUnreachable after it once was reachable.
			// Also, don't ever use AssignedAfter... for statements.
			if (newStatus == DefiniteAssignmentStatus.CodeUnreachable
			    || newStatus == DefiniteAssignmentStatus.AssignedAfterFalseExpression
			    || newStatus == DefiniteAssignmentStatus.AssignedAfterTrueExpression)
			{
				throw new InvalidOperationException();
			}
			// Note that the status can change from DefinitelyAssigned
			// back to PotentiallyAssigned as unreachable input edges are
			// discovered to be reachable.
			
			edgeStatus[edge] = newStatus;
			DefiniteAssignmentNode targetNode = (DefiniteAssignmentNode)edge.To;
			if (analyzedRangeStart <= targetNode.Index && targetNode.Index <= analyzedRangeEnd) {
				// TODO: potential optimization: visit previously unreachable nodes with higher priority
				// (e.g. use Deque and enqueue previously unreachable nodes at the front, but
				// other nodes at the end)
				nodesWithModifiedInput.Enqueue(targetNode);
			}
		}
		
		/// <summary>
		/// Evaluates an expression.
		/// </summary>
		/// <returns>The constant value of the expression; or null if the expression is not a constant.</returns>
		ResolveResult EvaluateConstant(Expression expr)
		{
			return resolver.Resolve(expr, analysisCancellationToken);
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
		
		static DefiniteAssignmentStatus CleanSpecialValues(DefiniteAssignmentStatus status)
		{
			if (status == DefiniteAssignmentStatus.AssignedAfterTrueExpression)
				return DefiniteAssignmentStatus.PotentiallyAssigned;
			else if (status == DefiniteAssignmentStatus.AssignedAfterFalseExpression)
				return DefiniteAssignmentStatus.PotentiallyAssigned;
			else
				return status;
		}
		
		sealed class DefiniteAssignmentVisitor : DepthFirstAstVisitor<DefiniteAssignmentStatus, DefiniteAssignmentStatus>
		{
			internal DefiniteAssignmentAnalysis analysis;
			
			// The general approach for unknown nodes is to pass the status through all child nodes in order
			protected override DefiniteAssignmentStatus VisitChildren(AstNode node, DefiniteAssignmentStatus data)
			{
				// the special values are valid as output only, not as input
				Debug.Assert(data == CleanSpecialValues(data));
				DefiniteAssignmentStatus status = data;
				for (AstNode child = node.FirstChild; child != null; child = child.NextSibling) {
					analysis.analysisCancellationToken.ThrowIfCancellationRequested();
					
					Debug.Assert(!(child is Statement)); // statements are visited with the CFG, not with the visitor pattern
					status = child.AcceptVisitor(this, status);
					status = CleanSpecialValues(status);
				}
				return status;
			}
			
			#region Statements
			// For statements, the visitor only describes the effect of the statement itself;
			// we do not consider the effect of any nested statements.
			// This is done because the nested statements will be reached using the control flow graph.
			
			// In fact, these methods are present so that the default logic in VisitChildren does not try to visit the nested statements.
			
			public override DefiniteAssignmentStatus VisitBlockStatement(BlockStatement blockStatement, DefiniteAssignmentStatus data)
			{
				return data;
			}
			
			public override DefiniteAssignmentStatus VisitCheckedStatement(CheckedStatement checkedStatement, DefiniteAssignmentStatus data)
			{
				return data;
			}
			
			public override DefiniteAssignmentStatus VisitUncheckedStatement(UncheckedStatement uncheckedStatement, DefiniteAssignmentStatus data)
			{
				return data;
			}
			
			// ExpressionStatement handled by default logic
			// VariableDeclarationStatement handled by default logic
			
			public override DefiniteAssignmentStatus VisitVariableInitializer(VariableInitializer variableInitializer, DefiniteAssignmentStatus data)
			{
				if (variableInitializer.Initializer.IsNull) {
					return data;
				} else {
					DefiniteAssignmentStatus status = variableInitializer.Initializer.AcceptVisitor(this, data);
					if (variableInitializer.Name == analysis.variableName)
						return DefiniteAssignmentStatus.DefinitelyAssigned;
					else
						return status;
				}
			}
			
			// IfStatement not handled by visitor, but special-cased in the code consuming the control flow graph
			
			public override DefiniteAssignmentStatus VisitSwitchStatement(SwitchStatement switchStatement, DefiniteAssignmentStatus data)
			{
				return switchStatement.Expression.AcceptVisitor(this, data);
			}
			
			public override DefiniteAssignmentStatus VisitWhileStatement(WhileStatement whileStatement, DefiniteAssignmentStatus data)
			{
				return data; // condition is handled by special condition CFG node
			}
			
			public override DefiniteAssignmentStatus VisitDoWhileStatement(DoWhileStatement doWhileStatement, DefiniteAssignmentStatus data)
			{
				return data; // condition is handled by special condition CFG node
			}
			
			public override DefiniteAssignmentStatus VisitForStatement(ForStatement forStatement, DefiniteAssignmentStatus data)
			{
				return data; // condition is handled by special condition CFG node; initializer and iterator statements are handled by CFG
			}
			
			// Break/Continue/Goto: handled by default logic
			
			// ThrowStatement: handled by default logic (just visit the expression)
			// ReturnStatement: handled by default logic (just visit the expression)
			
			public override DefiniteAssignmentStatus VisitTryCatchStatement(TryCatchStatement tryCatchStatement, DefiniteAssignmentStatus data)
			{
				return data; // no special logic when entering the try-catch-finally statement
				// TODO: where to put the special logic when exiting the try-finally statement?
			}
			
			public override DefiniteAssignmentStatus VisitForeachStatement(ForeachStatement foreachStatement, DefiniteAssignmentStatus data)
			{
				return data; // assignment of the foreach loop variable is done when handling the condition node
			}
			
			public override DefiniteAssignmentStatus VisitUsingStatement(UsingStatement usingStatement, DefiniteAssignmentStatus data)
			{
				if (usingStatement.ResourceAcquisition is Expression)
					return usingStatement.ResourceAcquisition.AcceptVisitor(this, data);
				else
					return data; // don't handle resource acquisition statements, as those are connected in the control flow graph
			}
			
			public override DefiniteAssignmentStatus VisitLockStatement(LockStatement lockStatement, DefiniteAssignmentStatus data)
			{
				return lockStatement.Expression.AcceptVisitor(this, data);
			}
			
			// Yield statements use the default logic
			
			public override DefiniteAssignmentStatus VisitUnsafeStatement(UnsafeStatement unsafeStatement, DefiniteAssignmentStatus data)
			{
				return data;
			}
			
			public override DefiniteAssignmentStatus VisitFixedStatement(FixedStatement fixedStatement, DefiniteAssignmentStatus data)
			{
				DefiniteAssignmentStatus status = data;
				foreach (var variable in fixedStatement.Variables)
					status = variable.AcceptVisitor(this, status);
				return status;
			}
			#endregion
			
			#region Expressions
			public override DefiniteAssignmentStatus VisitDirectionExpression(DirectionExpression directionExpression, DefiniteAssignmentStatus data)
			{
				if (directionExpression.FieldDirection == FieldDirection.Out) {
					return HandleAssignment(directionExpression.Expression, null, data);
				} else {
					// use default logic for 'ref'
					return VisitChildren(directionExpression, data);
				}
			}
			
			public override DefiniteAssignmentStatus VisitAssignmentExpression(AssignmentExpression assignmentExpression, DefiniteAssignmentStatus data)
			{
				if (assignmentExpression.Operator == AssignmentOperatorType.Assign) {
					return HandleAssignment(assignmentExpression.Left, assignmentExpression.Right, data);
				} else {
					// use default logic for compound assignment operators
					return VisitChildren(assignmentExpression, data);
				}
			}
			
			DefiniteAssignmentStatus HandleAssignment(Expression left, Expression right, DefiniteAssignmentStatus initialStatus)
			{
				IdentifierExpression ident = left as IdentifierExpression;
				if (ident != null && ident.Identifier == analysis.variableName) {
					// right==null is special case when handling 'out' expressions
					if (right != null)
						right.AcceptVisitor(this, initialStatus);
					return DefiniteAssignmentStatus.DefinitelyAssigned;
				} else {
					DefiniteAssignmentStatus status = left.AcceptVisitor(this, initialStatus);
					if (right != null)
						status = right.AcceptVisitor(this, CleanSpecialValues(status));
					return CleanSpecialValues(status);
				}
			}
			
			public override DefiniteAssignmentStatus VisitParenthesizedExpression(ParenthesizedExpression parenthesizedExpression, DefiniteAssignmentStatus data)
			{
				// Don't use the default logic here because we don't want to clean up the special values.
				return parenthesizedExpression.Expression.AcceptVisitor(this, data);
			}
			
			public override DefiniteAssignmentStatus VisitCheckedExpression(CheckedExpression checkedExpression, DefiniteAssignmentStatus data)
			{
				return checkedExpression.Expression.AcceptVisitor(this, data);
			}
			
			public override DefiniteAssignmentStatus VisitUncheckedExpression(UncheckedExpression uncheckedExpression, DefiniteAssignmentStatus data)
			{
				return uncheckedExpression.Expression.AcceptVisitor(this, data);
			}
			
			public override DefiniteAssignmentStatus VisitBinaryOperatorExpression(BinaryOperatorExpression binaryOperatorExpression, DefiniteAssignmentStatus data)
			{
				if (binaryOperatorExpression.Operator == BinaryOperatorType.ConditionalAnd) {
					// Handle constant left side of && expressions (not in the C# spec, but done by the MS compiler)
					bool? cond = analysis.EvaluateCondition(binaryOperatorExpression.Left);
					if (cond == true)
						return binaryOperatorExpression.Right.AcceptVisitor(this, data); // right operand gets evaluated unconditionally
					else if (cond == false)
						return data; // right operand never gets evaluated
					// C# 4.0 spec: §5.3.3.24 Definite Assignment for && expressions
					DefiniteAssignmentStatus afterLeft = binaryOperatorExpression.Left.AcceptVisitor(this, data);
					DefiniteAssignmentStatus beforeRight;
					if (afterLeft == DefiniteAssignmentStatus.AssignedAfterTrueExpression)
						beforeRight = DefiniteAssignmentStatus.DefinitelyAssigned;
					else if (afterLeft == DefiniteAssignmentStatus.AssignedAfterFalseExpression)
						beforeRight = DefiniteAssignmentStatus.PotentiallyAssigned;
					else
						beforeRight = afterLeft;
					DefiniteAssignmentStatus afterRight = binaryOperatorExpression.Right.AcceptVisitor(this, beforeRight);
					if (afterLeft == DefiniteAssignmentStatus.DefinitelyAssigned)
						return DefiniteAssignmentStatus.DefinitelyAssigned;
					else if (afterRight == DefiniteAssignmentStatus.DefinitelyAssigned && afterLeft == DefiniteAssignmentStatus.AssignedAfterFalseExpression)
						return DefiniteAssignmentStatus.DefinitelyAssigned;
					else if (afterRight == DefiniteAssignmentStatus.DefinitelyAssigned || afterRight == DefiniteAssignmentStatus.AssignedAfterTrueExpression)
						return DefiniteAssignmentStatus.AssignedAfterTrueExpression;
					else if (afterLeft == DefiniteAssignmentStatus.AssignedAfterFalseExpression && afterRight == DefiniteAssignmentStatus.AssignedAfterFalseExpression)
						return DefiniteAssignmentStatus.AssignedAfterFalseExpression;
					else
						return DefiniteAssignmentStatus.PotentiallyAssigned;
				} else if (binaryOperatorExpression.Operator == BinaryOperatorType.ConditionalOr) {
					// C# 4.0 spec: §5.3.3.25 Definite Assignment for || expressions
					bool? cond = analysis.EvaluateCondition(binaryOperatorExpression.Left);
					if (cond == false)
						return binaryOperatorExpression.Right.AcceptVisitor(this, data); // right operand gets evaluated unconditionally
					else if (cond == true)
						return data; // right operand never gets evaluated
					DefiniteAssignmentStatus afterLeft = binaryOperatorExpression.Left.AcceptVisitor(this, data);
					DefiniteAssignmentStatus beforeRight;
					if (afterLeft == DefiniteAssignmentStatus.AssignedAfterTrueExpression)
						beforeRight = DefiniteAssignmentStatus.PotentiallyAssigned;
					else if (afterLeft == DefiniteAssignmentStatus.AssignedAfterFalseExpression)
						beforeRight = DefiniteAssignmentStatus.DefinitelyAssigned;
					else
						beforeRight = afterLeft;
					DefiniteAssignmentStatus afterRight = binaryOperatorExpression.Right.AcceptVisitor(this, beforeRight);
					if (afterLeft == DefiniteAssignmentStatus.DefinitelyAssigned)
						return DefiniteAssignmentStatus.DefinitelyAssigned;
					else if (afterRight == DefiniteAssignmentStatus.DefinitelyAssigned && afterLeft == DefiniteAssignmentStatus.AssignedAfterTrueExpression)
						return DefiniteAssignmentStatus.DefinitelyAssigned;
					else if (afterRight == DefiniteAssignmentStatus.DefinitelyAssigned || afterRight == DefiniteAssignmentStatus.AssignedAfterFalseExpression)
						return DefiniteAssignmentStatus.AssignedAfterFalseExpression;
					else if (afterLeft == DefiniteAssignmentStatus.AssignedAfterTrueExpression && afterRight == DefiniteAssignmentStatus.AssignedAfterTrueExpression)
						return DefiniteAssignmentStatus.AssignedAfterTrueExpression;
					else
						return DefiniteAssignmentStatus.PotentiallyAssigned;
				} else if (binaryOperatorExpression.Operator == BinaryOperatorType.NullCoalescing) {
					// C# 4.0 spec: §5.3.3.27 Definite assignment for ?? expressions
					ResolveResult crr = analysis.EvaluateConstant(binaryOperatorExpression.Left);
					if (crr != null && crr.IsCompileTimeConstant && crr.ConstantValue == null)
						return binaryOperatorExpression.Right.AcceptVisitor(this, data);
					DefiniteAssignmentStatus status = CleanSpecialValues(binaryOperatorExpression.Left.AcceptVisitor(this, data));
					binaryOperatorExpression.Right.AcceptVisitor(this, status);
					return status;
				} else {
					// use default logic for other operators
					return VisitChildren(binaryOperatorExpression, data);
				}
			}
			
			public override DefiniteAssignmentStatus VisitUnaryOperatorExpression(UnaryOperatorExpression unaryOperatorExpression, DefiniteAssignmentStatus data)
			{
				if (unaryOperatorExpression.Operator == UnaryOperatorType.Not) {
					// C# 4.0 spec: §5.3.3.26 Definite assignment for ! expressions
					DefiniteAssignmentStatus status = unaryOperatorExpression.Expression.AcceptVisitor(this, data);
					if (status == DefiniteAssignmentStatus.AssignedAfterFalseExpression)
						return DefiniteAssignmentStatus.AssignedAfterTrueExpression;
					else if (status == DefiniteAssignmentStatus.AssignedAfterTrueExpression)
						return DefiniteAssignmentStatus.AssignedAfterFalseExpression;
					else
						return status;
				} else {
					// use default logic for other operators
					return VisitChildren(unaryOperatorExpression, data);
				}
			}
			
			public override DefiniteAssignmentStatus VisitConditionalExpression(ConditionalExpression conditionalExpression, DefiniteAssignmentStatus data)
			{
				// C# 4.0 spec: §5.3.3.28 Definite assignment for ?: expressions
				bool? cond = analysis.EvaluateCondition(conditionalExpression.Condition);
				if (cond == true) {
					return conditionalExpression.TrueExpression.AcceptVisitor(this, data);
				} else if (cond == false) {
					return conditionalExpression.FalseExpression.AcceptVisitor(this, data);
				} else {
					DefiniteAssignmentStatus afterCondition = conditionalExpression.Condition.AcceptVisitor(this, data);
					
					DefiniteAssignmentStatus beforeTrue, beforeFalse;
					if (afterCondition == DefiniteAssignmentStatus.AssignedAfterTrueExpression) {
						beforeTrue = DefiniteAssignmentStatus.DefinitelyAssigned;
						beforeFalse = DefiniteAssignmentStatus.PotentiallyAssigned;
					} else if (afterCondition == DefiniteAssignmentStatus.AssignedAfterFalseExpression) {
						beforeTrue = DefiniteAssignmentStatus.PotentiallyAssigned;
						beforeFalse = DefiniteAssignmentStatus.DefinitelyAssigned;
					} else {
						beforeTrue = afterCondition;
						beforeFalse = afterCondition;
					}
					
					DefiniteAssignmentStatus afterTrue = conditionalExpression.TrueExpression.AcceptVisitor(this, beforeTrue);
					DefiniteAssignmentStatus afterFalse = conditionalExpression.FalseExpression.AcceptVisitor(this, beforeFalse);
					return MergeStatus(CleanSpecialValues(afterTrue), CleanSpecialValues(afterFalse));
				}
			}
			
			public override DefiniteAssignmentStatus VisitAnonymousMethodExpression(AnonymousMethodExpression anonymousMethodExpression, DefiniteAssignmentStatus data)
			{
				BlockStatement body = anonymousMethodExpression.Body;
				analysis.ChangeNodeStatus(analysis.beginNodeDict[body], data);
				return data;
			}
			
			public override DefiniteAssignmentStatus VisitLambdaExpression(LambdaExpression lambdaExpression, DefiniteAssignmentStatus data)
			{
				Statement body = lambdaExpression.Body as Statement;
				if (body != null) {
					analysis.ChangeNodeStatus(analysis.beginNodeDict[body], data);
				} else {
					lambdaExpression.Body.AcceptVisitor(this, data);
				}
				return data;
			}
			
			public override DefiniteAssignmentStatus VisitIdentifierExpression(IdentifierExpression identifierExpression, DefiniteAssignmentStatus data)
			{
				if (data != DefiniteAssignmentStatus.DefinitelyAssigned
				    && identifierExpression.Identifier == analysis.variableName && identifierExpression.TypeArguments.Count == 0)
				{
					analysis.unassignedVariableUses.Add(identifierExpression);
				}
				return data;
			}
			#endregion
		}
	}
}
