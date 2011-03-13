// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ICSharpCode.NRefactory.CSharp.Resolver;

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
		/// The variable is definitely unassigned.
		/// </summary>
		DefinitelyUnassigned,
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
		readonly DefiniteAssignmentVisitor visitor = new DefiniteAssignmentVisitor();
		readonly List<ControlFlowNode> allNodes = new List<ControlFlowNode>();
		readonly Dictionary<Statement, ControlFlowNode> beginNodeDict = new Dictionary<Statement, ControlFlowNode>();
		readonly Dictionary<Statement, ControlFlowNode> endNodeDict = new Dictionary<Statement, ControlFlowNode>();
		Dictionary<ControlFlowNode, DefiniteAssignmentStatus> nodeStatus = new Dictionary<ControlFlowNode, DefiniteAssignmentStatus>();
		Dictionary<ControlFlowEdge, DefiniteAssignmentStatus> edgeStatus = new Dictionary<ControlFlowEdge, DefiniteAssignmentStatus>();
		
		string variableName;
		List<IdentifierExpression> unassignedVariableUses = new List<IdentifierExpression>();
		
		Queue<ControlFlowNode> nodesWithModifiedInput = new Queue<ControlFlowNode>();
		
		public DefiniteAssignmentAnalysis(Statement rootStatement)
		{
			visitor.analysis = this;
			ControlFlowGraphBuilder b = new ControlFlowGraphBuilder();
			allNodes.AddRange(b.BuildControlFlowGraph(rootStatement));
			foreach (AstNode descendant in rootStatement.Descendants) {
				// Anonymous methods have separate control flow graphs, but we also need to analyze those.
				AnonymousMethodExpression ame = descendant as AnonymousMethodExpression;
				if (ame != null)
					allNodes.AddRange(b.BuildControlFlowGraph(ame.Body));
				LambdaExpression lambda = descendant as LambdaExpression;
				if (lambda != null && lambda.Body is Statement)
					allNodes.AddRange(b.BuildControlFlowGraph((Statement)lambda.Body));
			}
			// Verify that we created nodes for all statements:
			Debug.Assert(!rootStatement.DescendantsAndSelf.OfType<Statement>().Except(allNodes.Select(n => n.NextStatement)).Any());
			// Now register the nodes in the dictionaries:
			foreach (ControlFlowNode node in allNodes) {
				if (node.Type == ControlFlowNodeType.StartNode || node.Type == ControlFlowNodeType.BetweenStatements)
					beginNodeDict.Add(node.NextStatement, node);
				if (node.Type == ControlFlowNodeType.BetweenStatements || node.Type == ControlFlowNodeType.EndNode)
					endNodeDict.Add(node.PreviousStatement, node);
			}
		}
		
		public void Analyze(string variable, DefiniteAssignmentStatus initialStatus = DefiniteAssignmentStatus.DefinitelyUnassigned)
		{
			this.variableName = variable;
			// Reset the status:
			unassignedVariableUses.Clear();
			foreach (ControlFlowNode node in allNodes) {
				nodeStatus[node] = DefiniteAssignmentStatus.CodeUnreachable;
				foreach (ControlFlowEdge edge in node.Outgoing)
					edgeStatus[edge] = DefiniteAssignmentStatus.CodeUnreachable;
			}
			
			ChangeNodeStatus(allNodes[0], DefiniteAssignmentStatus.DefinitelyUnassigned);
			// Iterate as long as the input status of some nodes is changing:
			while (nodesWithModifiedInput.Count > 0) {
				ControlFlowNode node = nodesWithModifiedInput.Dequeue();
				DefiniteAssignmentStatus inputStatus = DefiniteAssignmentStatus.CodeUnreachable;
				foreach (ControlFlowEdge edge in node.Incoming) {
					inputStatus = MergeStatus(inputStatus, edgeStatus[edge]);
				}
				ChangeNodeStatus(node, inputStatus);
			}
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
		
		void ChangeNodeStatus(ControlFlowNode node, DefiniteAssignmentStatus inputStatus)
		{
			if (nodeStatus[node] == inputStatus)
				return;
			nodeStatus[node] = inputStatus;
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
						outputStatus = CleanSpecialValues(node.NextStatement.AcceptVisitor(visitor, inputStatus));
					}
					break;
				case ControlFlowNodeType.EndNode:
					outputStatus = inputStatus;
					if (node.PreviousStatement.Role == TryCatchStatement.FinallyBlockRole
					    && (outputStatus == DefiniteAssignmentStatus.DefinitelyAssigned || outputStatus == DefiniteAssignmentStatus.PotentiallyAssigned))
					{
						TryCatchStatement tryFinally = (TryCatchStatement)node.PreviousStatement.Parent;
						// Changing the status on a finally block potentially changes the status of all edges leaving that finally block:
						foreach (ControlFlowEdge edge in allNodes.SelectMany(n => n.Outgoing)) {
							DefiniteAssignmentStatus s = edgeStatus[edge];
							if (s == DefiniteAssignmentStatus.DefinitelyUnassigned || s == DefiniteAssignmentStatus.PotentiallyAssigned) {
								ChangeEdgeStatus(edge, outputStatus);
							}
						}
					}
					break;
				case ControlFlowNodeType.LoopCondition:
					ForeachStatement foreachStmt = node.NextStatement as ForeachStatement;
					if (foreachStmt != null) {
						outputStatus = CleanSpecialValues(foreachStmt.InExpression.AcceptVisitor(visitor, inputStatus));
						if (foreachStmt.VariableName == this.variableName)
							outputStatus = DefiniteAssignmentStatus.DefinitelyAssigned;
						break;
					} else {
						Debug.Assert(node.NextStatement is IfElseStatement || node.NextStatement is WhileStatement || node.NextStatement is ForStatement || node.NextStatement is DoWhileStatement);
						Expression condition = node.NextStatement.GetChildByRole(AstNode.Roles.Condition);
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
			// Ensure that status can change only in one direction:
			// CodeUnreachable -> PotentiallyAssigned -> Definitely[Un]Assigned
			// Going against this direction indicates a bug and could cause infinite loops.
			switch (newStatus) {
				case DefiniteAssignmentStatus.PotentiallyAssigned:
					if (oldStatus != DefiniteAssignmentStatus.CodeUnreachable)
						throw new InvalidOperationException("Invalid state transition");
					break;
				case DefiniteAssignmentStatus.DefinitelyUnassigned:
				case DefiniteAssignmentStatus.DefinitelyAssigned:
					if (!(oldStatus == DefiniteAssignmentStatus.CodeUnreachable || oldStatus == DefiniteAssignmentStatus.PotentiallyAssigned))
						throw new InvalidOperationException("Invalid state transition");
					break;
				case DefiniteAssignmentStatus.CodeUnreachable:
					throw new InvalidOperationException("Invalid state transition");
				default:
					throw new InvalidOperationException("Invalid value for DefiniteAssignmentStatus");
			}
			edgeStatus[edge] = newStatus;
			nodesWithModifiedInput.Enqueue(edge.To);
		}
		
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
				foreach (AstNode child in node.Children) {
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
					right.AcceptVisitor(this, initialStatus);
					return DefiniteAssignmentStatus.DefinitelyAssigned;
				} else {
					DefiniteAssignmentStatus status = left.AcceptVisitor(this, initialStatus);
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
					else if (afterRight == DefiniteAssignmentStatus.DefinitelyUnassigned)
						return afterRight;
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
					else if (afterRight == DefiniteAssignmentStatus.DefinitelyUnassigned)
						return DefiniteAssignmentStatus.DefinitelyUnassigned;
					else
						return DefiniteAssignmentStatus.PotentiallyAssigned;
				} else if (binaryOperatorExpression.Operator == BinaryOperatorType.NullCoalescing) {
					// C# 4.0 spec: §5.3.3.27 Definite assignment for ?? expressions
					ConstantResolveResult crr = analysis.EvaluateConstant(binaryOperatorExpression.Left);
					if (crr != null && crr.ConstantValue == null)
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
					DefiniteAssignmentStatus afterFalse = conditionalExpression.TrueExpression.AcceptVisitor(this, beforeFalse);
					return MergeStatus(CleanSpecialValues(afterTrue), CleanSpecialValues(afterFalse));
				}
			}
			
			public override DefiniteAssignmentStatus VisitAnonymousMethodExpression(AnonymousMethodExpression anonymousMethodExpression, DefiniteAssignmentStatus data)
			{
				BlockStatement body = anonymousMethodExpression.Body;
				foreach (ControlFlowNode node in analysis.allNodes) {
					if (node.NextStatement == body)
						analysis.ChangeNodeStatus(node, data);
				}
				return data;
			}
			
			public override DefiniteAssignmentStatus VisitLambdaExpression(LambdaExpression lambdaExpression, DefiniteAssignmentStatus data)
			{
				Statement body = lambdaExpression.Body as Statement;
				if (body != null) {
					foreach (ControlFlowNode node in analysis.allNodes) {
						if (node.NextStatement == body)
							analysis.ChangeNodeStatus(node, data);
					}
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
		}
	}
}
