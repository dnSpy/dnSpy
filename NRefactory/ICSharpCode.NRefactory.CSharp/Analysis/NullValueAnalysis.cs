// 
// NullValueAnalysis.cs
//  
// Author:
//       Luís Reis <luiscubal@gmail.com>
// 
// Copyright (c) 2013 Luís Reis
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
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Text;
using ICSharpCode.NRefactory.CSharp.Resolver;
using ICSharpCode.NRefactory.Semantics;
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.CSharp.Refactoring;
using ICSharpCode.NRefactory.PatternMatching;
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.Utils;

namespace ICSharpCode.NRefactory.CSharp.Analysis
{
	public class NullValueAnalysis
	{
		sealed class VariableStatusInfo : IEquatable<VariableStatusInfo>, IEnumerable<KeyValuePair<string, NullValueStatus>>
		{
			readonly Dictionary<string, NullValueStatus> VariableStatus = new Dictionary<string, NullValueStatus>();

			public NullValueStatus this[string name]
			{
				get {
					NullValueStatus status;
					if (VariableStatus.TryGetValue(name, out status)) {
						return status;
					}
					return NullValueStatus.UnreachableOrInexistent;
				}
				set {
					if (value == NullValueStatus.UnreachableOrInexistent) {
						VariableStatus.Remove(name);
					} else {
						VariableStatus [name] = value;
					}
				}
			}

			/// <summary>
			/// Modifies the variable state to consider a new incoming path
			/// </summary>
			/// <returns><c>true</c>, if the state has changed, <c>false</c> otherwise.</returns>
			/// <param name="incomingState">The variable state of the incoming path</param>
			public bool ReceiveIncoming(VariableStatusInfo incomingState)
			{
				bool changed = false;
				var listOfVariables = VariableStatus.Keys.Concat(incomingState.VariableStatus.Keys).ToList();
				foreach (string variable in listOfVariables)
				{
					var newValue = CombineStatus(this [variable], incomingState [variable]);
					if (this [variable] != newValue) {
						this [variable] = newValue;
						changed = true;
					}
				}

				return changed;
			}

			public static NullValueStatus CombineStatus(NullValueStatus oldValue, NullValueStatus incomingValue)
			{
				if (oldValue == NullValueStatus.Error || incomingValue == NullValueStatus.Error)
					return NullValueStatus.Error;

				if (oldValue == NullValueStatus.UnreachableOrInexistent ||
				    oldValue == NullValueStatus.Unassigned)
					return incomingValue;

				if (incomingValue == NullValueStatus.Unassigned) {
					return NullValueStatus.Unassigned;
				}

				if (oldValue == NullValueStatus.CapturedUnknown || incomingValue == NullValueStatus.CapturedUnknown) {
					//TODO: Check if this is right
					return NullValueStatus.CapturedUnknown;
				}

				if (oldValue == NullValueStatus.Unknown) {
					return NullValueStatus.Unknown;
				}

				if (oldValue == NullValueStatus.DefinitelyNull) {
					return incomingValue == NullValueStatus.DefinitelyNull ?
						NullValueStatus.DefinitelyNull : NullValueStatus.PotentiallyNull;
				}

				if (oldValue == NullValueStatus.DefinitelyNotNull) {
					if (incomingValue == NullValueStatus.Unknown)
						return NullValueStatus.Unknown;
					if (incomingValue == NullValueStatus.DefinitelyNotNull)
						return NullValueStatus.DefinitelyNotNull;
					return NullValueStatus.PotentiallyNull;
				}

				Debug.Assert(oldValue == NullValueStatus.PotentiallyNull);
				return NullValueStatus.PotentiallyNull;
			}

			public bool HasVariable(string variable) {
				return VariableStatus.ContainsKey(variable);
			}

			public VariableStatusInfo Clone() {
				var clone = new VariableStatusInfo();
				foreach (var item in VariableStatus) {
					clone.VariableStatus.Add(item.Key, item.Value);
				}
				return clone;
			}

			public override bool Equals(object obj)
			{
				return Equals(obj as VariableStatusInfo);
			}

			public bool Equals(VariableStatusInfo obj)
			{
				if (obj == null) {
					return false;
				}

				if (VariableStatus.Count != obj.VariableStatus.Count)
					return false;

				return VariableStatus.All(item => item.Value == obj[item.Key]);
			}

			public override int GetHashCode()
			{
				//STUB
				return VariableStatus.Count.GetHashCode();
			}

			public static bool operator ==(VariableStatusInfo obj1, VariableStatusInfo obj2) {
				return object.ReferenceEquals(obj1, null) ?
					object.ReferenceEquals(obj2, null) : obj1.Equals(obj2);
			}

			public static bool operator !=(VariableStatusInfo obj1, VariableStatusInfo obj2) {
				return !(obj1 == obj2);
			}

			public IEnumerator<KeyValuePair<string, NullValueStatus>> GetEnumerator()
			{
				return VariableStatus.GetEnumerator();
			}

			IEnumerator IEnumerable.GetEnumerator()
			{
				return GetEnumerator();
			}

			public override string ToString()
			{
				var builder = new StringBuilder("[");
				foreach (var item in this) {
					builder.Append(item.Key);
					builder.Append("=");
					builder.Append(item.Value);
				}
				builder.Append("]");
				return builder.ToString();
			}
		}

		sealed class NullAnalysisNode : ControlFlowNode
		{
			public readonly VariableStatusInfo VariableState = new VariableStatusInfo();
			public bool Visited { get; private set; }

			public NullAnalysisNode(Statement previousStatement, Statement nextStatement, ControlFlowNodeType type)
				: base(previousStatement, nextStatement, type)
			{
			}

			public bool ReceiveIncoming(VariableStatusInfo incomingState)
			{
				bool changed = VariableState.ReceiveIncoming(incomingState);
				if (!Visited) {
					Visited = true;
					return true;
				}
				return changed;
			}
		}

		sealed class NullAnalysisGraphBuilder : ControlFlowGraphBuilder
		{
			protected override ControlFlowNode CreateNode(Statement previousStatement, Statement nextStatement, ControlFlowNodeType type)
			{
				return new NullAnalysisNode(previousStatement, nextStatement, type);
			}
		}

		class PendingNode : IEquatable<PendingNode> {
			internal readonly NullAnalysisNode nodeToVisit;
			internal readonly VariableStatusInfo statusInfo;
			internal readonly ComparableList<NullAnalysisNode> pendingTryFinallyNodes;
			internal readonly NullAnalysisNode nodeAfterFinally;

			internal PendingNode(NullAnalysisNode nodeToVisit, VariableStatusInfo statusInfo)
				: this(nodeToVisit, statusInfo, new ComparableList<NullAnalysisNode>(), null)
			{
			}

			public PendingNode(NullAnalysisNode nodeToVisit, VariableStatusInfo statusInfo, ComparableList<NullAnalysisNode> pendingFinallyNodes, NullAnalysisNode nodeAfterFinally)
			{
				this.nodeToVisit = nodeToVisit;
				this.statusInfo = statusInfo;
				this.pendingTryFinallyNodes = pendingFinallyNodes;
				this.nodeAfterFinally = nodeAfterFinally;
			}

			public override bool Equals(object obj)
			{
				return Equals(obj as PendingNode);
			}

			public bool Equals(PendingNode obj) {
				if (obj == null) return false;

				if (nodeToVisit != obj.nodeToVisit) return false;
				if (statusInfo != obj.statusInfo) return false;
				if (pendingTryFinallyNodes != obj.pendingTryFinallyNodes) return false;
				if (nodeAfterFinally != obj.nodeAfterFinally) return false;

				return true;
			}

			public override int GetHashCode()
			{
				return nodeToVisit.GetHashCode() ^
					statusInfo.GetHashCode() ^
					pendingTryFinallyNodes.GetHashCode() ^
					(nodeAfterFinally == null ? 0 : nodeAfterFinally.GetHashCode());
			}
		}

		readonly BaseRefactoringContext context;
		readonly NullAnalysisVisitor visitor;
		List<NullAnalysisNode> allNodes;
		readonly HashSet<PendingNode> nodesToVisit = new HashSet<PendingNode>();
		Dictionary<Statement, NullAnalysisNode> nodeBeforeStatementDict;
		Dictionary<Statement, NullAnalysisNode> nodeAfterStatementDict;
		readonly Dictionary<Expression, NullValueStatus> expressionResult = new Dictionary<Expression, NullValueStatus>();

		public NullValueAnalysis(BaseRefactoringContext context, MethodDeclaration methodDeclaration, CancellationToken cancellationToken)
			: this(context, methodDeclaration.Body, methodDeclaration.Parameters, cancellationToken)
		{
		}

		readonly IEnumerable<ParameterDeclaration> parameters;
		readonly Statement rootStatement;

		readonly CancellationToken cancellationToken;

		public NullValueAnalysis(BaseRefactoringContext context, Statement rootStatement, IEnumerable<ParameterDeclaration> parameters, CancellationToken cancellationToken)
		{
			if (rootStatement == null)
				throw new ArgumentNullException("rootStatement");
			if (context == null)
				throw new ArgumentNullException("context");

			this.context = context;
			this.rootStatement = rootStatement;
			this.parameters = parameters;
			this.visitor = new NullAnalysisVisitor(this);
			this.cancellationToken = cancellationToken;
		}

		/// <summary>
		/// Sets the local variable value.
		/// This method does not change anything if identifier does not refer to a local variable.
		/// Do not use this in variable declarations since resolving the variable won't work yet.
		/// </summary>
		/// <returns><c>true</c>, if local variable value was set, <c>false</c> otherwise.</returns>
		/// <param name="data">The variable status data to change.</param>
		/// <param name="identifierNode">The identifier to set.</param>
		/// <param name="identifierName">The name of the identifier to set.</param>
		/// <param name="value">The value to set the identifier.</param>
		bool SetLocalVariableValue (VariableStatusInfo data, AstNode identifierNode, string identifierName, NullValueStatus value) {
			var resolveResult = context.Resolve(identifierNode);
			if (resolveResult is LocalResolveResult) {
				if (data [identifierName] != NullValueStatus.CapturedUnknown) {
					data [identifierName] = value;

					return true;
				}
			}
			return false;
		}

		bool SetLocalVariableValue (VariableStatusInfo data, IdentifierExpression identifierExpression, NullValueStatus value) {
			return SetLocalVariableValue(data, identifierExpression, identifierExpression.Identifier, value);
		}

		bool SetLocalVariableValue (VariableStatusInfo data, Identifier identifier, NullValueStatus value) {
			return SetLocalVariableValue(data, identifier, identifier.Name, value);
		}

		void SetupNode(NullAnalysisNode node)
		{
			foreach (var parameter in parameters) {
				var resolveResult = context.Resolve(parameter.Type);
				node.VariableState[parameter.Name] = GetInitialVariableStatus(resolveResult);
			}

			nodesToVisit.Add(new PendingNode(node, node.VariableState));
		}

		static bool IsTypeNullable(IType type)
		{
			return type.IsReferenceType == true || type.FullName == "System.Nullable";
		}

		public bool IsParametersAreUninitialized {
			get;
			set;
		}

		NullValueStatus GetInitialVariableStatus(ResolveResult resolveResult)
		{
			var typeResolveResult = resolveResult as TypeResolveResult;
			if (typeResolveResult == null) {
				return NullValueStatus.Error;
			}
			var type = typeResolveResult.Type;
			if (type.IsReferenceType == null) {
				return NullValueStatus.Error;
			}
			if (!IsParametersAreUninitialized)
				return NullValueStatus.DefinitelyNotNull;
			return IsTypeNullable(type) ? NullValueStatus.PotentiallyNull : NullValueStatus.DefinitelyNotNull;
		}

		public void Analyze()
		{
			var cfgBuilder = new NullAnalysisGraphBuilder();
			allNodes = cfgBuilder.BuildControlFlowGraph(rootStatement, cancellationToken).Cast<NullAnalysisNode>().ToList();
			nodeBeforeStatementDict = allNodes.Where(node => node.Type == ControlFlowNodeType.StartNode || node.Type == ControlFlowNodeType.BetweenStatements)
				.ToDictionary(node => node.NextStatement);
			nodeAfterStatementDict = allNodes.Where(node => node.Type == ControlFlowNodeType.BetweenStatements || node.Type == ControlFlowNodeType.EndNode)
				.ToDictionary(node => node.PreviousStatement);

			foreach (var node in allNodes) {
				if (node.Type == ControlFlowNodeType.StartNode && node.NextStatement == rootStatement) {
					Debug.Assert(!nodesToVisit.Any());

					SetupNode(node);
				}
			}

			while (nodesToVisit.Any()) {
				var nodeToVisit = nodesToVisit.First();
				nodesToVisit.Remove(nodeToVisit);

				Visit(nodeToVisit);
			}
		}

		int visits = 0;

		public int NodeVisits
		{
			get {
				return visits;
			}
		}

		void Visit(PendingNode nodeInfo)
		{
			cancellationToken.ThrowIfCancellationRequested();

			var node = nodeInfo.nodeToVisit;
			var statusInfo = nodeInfo.statusInfo;

			visits++;
			if (visits > 100) {
				//Visiting way too often, let's enter fast mode
				//Fast mode is slighly less accurate but visits each node less times
				nodesToVisit.RemoveWhere(candidate => candidate.nodeToVisit == nodeInfo.nodeToVisit &&
				                         candidate.pendingTryFinallyNodes.Equals(nodeInfo.pendingTryFinallyNodes) &&
				                         candidate.nodeAfterFinally == nodeInfo.nodeAfterFinally);
				statusInfo = node.VariableState;
			}

			var nextStatement = node.NextStatement;
			VariableStatusInfo outgoingStatusInfo = statusInfo;
			VisitorResult result = null;

			if (nextStatement != null && (!(nextStatement is DoWhileStatement) || node.Type == ControlFlowNodeType.LoopCondition)) {
				result = nextStatement.AcceptVisitor(visitor, statusInfo);
				if (result == null) {
					Console.WriteLine("Failure in {0}", nextStatement);
					throw new InvalidOperationException();
				}

				outgoingStatusInfo = result.Variables;
			}

			if ((result == null || !result.ThrowsException) && node.Outgoing.Any()) {
				var tryFinallyStatement = nextStatement as TryCatchStatement;

				foreach (var outgoingEdge in node.Outgoing) {
					VariableStatusInfo edgeInfo;
					edgeInfo = outgoingStatusInfo.Clone();

					if (node.Type == ControlFlowNodeType.EndNode) {
						var previousBlock = node.PreviousStatement as BlockStatement;
						if (previousBlock != null) {
							//We're leaving a block statement.
							//As such, we'll remove the variables that were declared *in* the loop
							//This helps GetVariableStatusAfter/BeforeStatement be more accurate
							//and prevents some redundant revisiting.

							foreach (var variableInitializer in previousBlock.Statements
							         .OfType<VariableDeclarationStatement>()
							         .SelectMany(declaration => declaration.Variables)) {

								edgeInfo [variableInitializer.Name] = NullValueStatus.UnreachableOrInexistent;
							}
						}
					}

					if (tryFinallyStatement != null) {
						//With the exception of try statements, this needs special handling:
						//we'll set all changed variables to Unknown or CapturedUnknown
						if (outgoingEdge.To.NextStatement == tryFinallyStatement.FinallyBlock) {
							foreach (var identifierExpression in tryFinallyStatement.TryBlock.Descendants.OfType<IdentifierExpression>()) {
								//TODO: Investigate CaptureUnknown
								SetLocalVariableValue(edgeInfo, identifierExpression, NullValueStatus.Unknown);
							}
						} else {
							var clause = tryFinallyStatement.CatchClauses
								.FirstOrDefault(candidateClause => candidateClause.Body == outgoingEdge.To.NextStatement);

							if (clause != null) {
								SetLocalVariableValue(edgeInfo, clause.VariableNameToken, NullValueStatus.DefinitelyNotNull);

								foreach (var identifierExpression in tryFinallyStatement.TryBlock.Descendants.OfType<IdentifierExpression>()) {
									//TODO: Investigate CaptureUnknown
									SetLocalVariableValue(edgeInfo, identifierExpression, NullValueStatus.Unknown);
								}
							}
						}
					}

					if (result != null) {
						switch (outgoingEdge.Type) {
							case ControlFlowEdgeType.ConditionTrue:
								if (result.KnownBoolResult == false) {
									//No need to explore this path -- expression is known to be false
									continue;
								}
								edgeInfo = result.TruePathVariables;
								break;
							case ControlFlowEdgeType.ConditionFalse:
								if (result.KnownBoolResult == true) {
									//No need to explore this path -- expression is known to be true
									continue;
								}
								edgeInfo = result.FalsePathVariables;
								break;
						}
					}

					if (outgoingEdge.IsLeavingTryFinally) {
						var nodeAfterFinally = (NullAnalysisNode)outgoingEdge.To;
						var finallyNodes = outgoingEdge.TryFinallyStatements.Select(tryFinally => nodeBeforeStatementDict [tryFinally.FinallyBlock]).ToList();
						var nextNode = finallyNodes.First();
						var remainingFinallyNodes = new ComparableList<NullAnalysisNode>(finallyNodes.Skip(1));
						//We have to visit the node even if ReceiveIncoming returns false
						//since the finallyNodes/nodeAfterFinally might be different even if the values of variables are the same -- and they need to be visited either way!
						//TODO 1: Is there any point in visiting the finally statement here?
						//TODO 2: Do we need the ReceiveIncoming at all?
						nextNode.ReceiveIncoming(edgeInfo);
						nodesToVisit.Add(new PendingNode(nextNode, edgeInfo, remainingFinallyNodes, nodeAfterFinally));
					} else {
						var outgoingNode = (NullAnalysisNode)outgoingEdge.To;
						if (outgoingNode.ReceiveIncoming(edgeInfo)) {
							nodesToVisit.Add(new PendingNode(outgoingNode, edgeInfo));
						}
					}
				}
			} else {
				//We found a return/throw/yield break or some other termination node
				var finallyBlockStarts = nodeInfo.pendingTryFinallyNodes;
				var nodeAfterFinally = nodeInfo.nodeAfterFinally;

				if (finallyBlockStarts.Any()) {
					var nextNode = finallyBlockStarts.First();
					if (nextNode.ReceiveIncoming(outgoingStatusInfo))
						nodesToVisit.Add(new PendingNode(nextNode, outgoingStatusInfo, new ComparableList<NullAnalysisNode>(finallyBlockStarts.Skip(1)), nodeInfo.nodeAfterFinally));
				} else if (nodeAfterFinally != null && nodeAfterFinally.ReceiveIncoming(outgoingStatusInfo)) {
					nodesToVisit.Add(new PendingNode(nodeAfterFinally, outgoingStatusInfo));
				} else {
					//Maybe we finished a try/catch/finally statement the "normal" way (no direct jumps)
					//so let's check that case
					var statement = node.PreviousStatement ?? node.NextStatement;
					Debug.Assert(statement != null);
					var parent = statement.GetParent<Statement>();
					var parentTryCatch = parent as TryCatchStatement;
					if (parentTryCatch != null) {
						var nextNode = nodeAfterStatementDict [parentTryCatch];
						if (nextNode.ReceiveIncoming(outgoingStatusInfo)) {
							nodesToVisit.Add(new PendingNode(nextNode, outgoingStatusInfo));
						}
					}
				}
			}
		}

		public NullValueStatus GetExpressionResult(Expression expr)
		{
			if (expr == null)
				throw new ArgumentNullException("expr");

			NullValueStatus info;
			if (expressionResult.TryGetValue(expr, out info)) {
				return info;
			}

			return NullValueStatus.UnreachableOrInexistent;
		}

		public NullValueStatus GetVariableStatusBeforeStatement(Statement stmt, string variableName)
		{
			if (stmt == null)
				throw new ArgumentNullException("stmt");
			if (variableName == null)
				throw new ArgumentNullException("variableName");

			NullAnalysisNode node;
			if (nodeBeforeStatementDict.TryGetValue(stmt, out node)) {
				return node.VariableState [variableName];
			}

			return NullValueStatus.UnreachableOrInexistent;
		}

		public NullValueStatus GetVariableStatusAfterStatement(Statement stmt, string variableName)
		{
			if (stmt == null)
				throw new ArgumentNullException("stmt");
			if (variableName == null)
				throw new ArgumentNullException("variableName");

			NullAnalysisNode node;
			if (nodeAfterStatementDict.TryGetValue(stmt, out node)) {
				return node.VariableState [variableName];
			}

			return NullValueStatus.UnreachableOrInexistent;
		}

		class ConditionalBranchInfo
		{
			/// <summary>
			/// True if the variable is null for the true path, false if it is false for the true path.
			/// </summary>
			public Dictionary<string, bool> TrueResultVariableNullStates = new Dictionary<string, bool>();
			/// <summary>
			/// True if the variable is null for the false path, false if it is false for the false path.
			/// </summary>
			public Dictionary<string, bool> FalseResultVariableNullStates = new Dictionary<string, bool>();
		}

		class VisitorResult
		{
			/// <summary>
			/// Indicates the return value of the expression.
			/// </summary>
			/// <remarks>
			/// Only applicable for expressions.
			/// </remarks>
			public NullValueStatus NullableReturnResult;

			/// <summary>
			/// Indicates the value of each item in an array or linq query.
			/// </summary>
			public NullValueStatus EnumeratedValueResult;

			/// <summary>
			/// Information that indicates the restrictions to add
			/// when branching.
			/// </summary>
			/// <remarks>
			/// Used in if/else statements, conditional expressions and
			/// while statements.
			/// </remarks>
			public ConditionalBranchInfo ConditionalBranchInfo;

			/// <summary>
			/// The state of the variables after the expression is executed.
			/// </summary>
			public VariableStatusInfo Variables;

			/// <summary>
			/// The expression is known to be invalid and trigger an error
			/// (e.g. a NullReferenceException)
			/// </summary>
			public bool ThrowsException;

			/// <summary>
			/// The known bool result of an expression.
			/// </summary>
			public bool? KnownBoolResult;

			public static VisitorResult ForEnumeratedValue(VariableStatusInfo variables, NullValueStatus itemValues)
			{
				var result = new VisitorResult();
				result.NullableReturnResult = NullValueStatus.DefinitelyNotNull;
				result.EnumeratedValueResult = itemValues;
				result.Variables = variables.Clone();
				return result;
			}

			public static VisitorResult ForValue(VariableStatusInfo variables, NullValueStatus returnValue)
			{
				var result = new VisitorResult();
				result.NullableReturnResult = returnValue;
				result.Variables = variables.Clone();
				return result;
			}

			public static VisitorResult ForBoolValue(VariableStatusInfo variables, bool newValue)
			{
				var result = new VisitorResult();
				result.NullableReturnResult = NullValueStatus.DefinitelyNotNull; //Bool expressions are never null
				result.KnownBoolResult = newValue;
				result.Variables = variables.Clone();
				return result;
			}

			public static VisitorResult ForException(VariableStatusInfo variables) {
				var result = new VisitorResult();
				result.NullableReturnResult = NullValueStatus.UnreachableOrInexistent;
				result.ThrowsException = true;
				result.Variables = variables.Clone();
				return result;
			}

			public VisitorResult Negated {
				get {
					var result = new VisitorResult();
					if (NullableReturnResult.IsDefiniteValue()) {
						result.NullableReturnResult = NullableReturnResult == NullValueStatus.DefinitelyNull
							? NullValueStatus.DefinitelyNotNull : NullValueStatus.DefinitelyNull;
					} else {
						result.NullableReturnResult = NullableReturnResult;
					}
					result.Variables = Variables.Clone();
					result.KnownBoolResult = !KnownBoolResult;
					if (ConditionalBranchInfo != null) {
						result.ConditionalBranchInfo = new ConditionalBranchInfo();
						foreach (var item in ConditionalBranchInfo.TrueResultVariableNullStates) {
							result.ConditionalBranchInfo.FalseResultVariableNullStates [item.Key] = item.Value;
						}
						foreach (var item in ConditionalBranchInfo.FalseResultVariableNullStates) {
							result.ConditionalBranchInfo.TrueResultVariableNullStates [item.Key] = item.Value;
						}
					}
					return result;
				}
			}

			public VariableStatusInfo TruePathVariables {
				get {
					var variables = Variables.Clone();
					if (ConditionalBranchInfo != null) {
						foreach (var item in ConditionalBranchInfo.TrueResultVariableNullStates) {
							variables [item.Key] = item.Value ? NullValueStatus.DefinitelyNull : NullValueStatus.DefinitelyNotNull;
						}
					}
					return variables;
				}
			}

			public VariableStatusInfo FalsePathVariables {
				get {
					var variables = Variables.Clone();
					if (ConditionalBranchInfo != null) {
						foreach (var item in ConditionalBranchInfo.FalseResultVariableNullStates) {
							variables [item.Key] = item.Value ? NullValueStatus.DefinitelyNull : NullValueStatus.DefinitelyNotNull;
						}
					}
					return variables;
				}
			}

			public static VisitorResult AndOperation(VisitorResult tentativeLeftResult, VisitorResult tentativeRightResult)
			{
				var result = new VisitorResult();
				result.KnownBoolResult = tentativeLeftResult.KnownBoolResult & tentativeRightResult.KnownBoolResult;

				var trueTruePath = tentativeRightResult.TruePathVariables;
				var trueFalsePath = tentativeRightResult.FalsePathVariables;
				var falsePath = tentativeLeftResult.FalsePathVariables;

				var trueVariables = trueTruePath;

				var falseVariables = trueFalsePath.Clone();
				falseVariables.ReceiveIncoming(falsePath);
				result.Variables = trueVariables.Clone();
				result.Variables.ReceiveIncoming(falseVariables);

				result.ConditionalBranchInfo = new ConditionalBranchInfo();

				foreach (var variable in trueVariables) {
					if (!variable.Value.IsDefiniteValue())
						continue;

					string variableName = variable.Key;

					if (variable.Value != result.Variables[variableName]) {
						bool isNull = variable.Value == NullValueStatus.DefinitelyNull;
						result.ConditionalBranchInfo.TrueResultVariableNullStates.Add(variableName, isNull);
					}
				}

				foreach (var variable in falseVariables) {
					if (!variable.Value.IsDefiniteValue())
						continue;

					string variableName = variable.Key;

					if (variable.Value != result.Variables [variableName]) {
						bool isNull = variable.Value == NullValueStatus.DefinitelyNull;
						result.ConditionalBranchInfo.FalseResultVariableNullStates.Add(variableName, isNull);
					}
				}

				return result;
			}

			public static VisitorResult OrOperation(VisitorResult tentativeLeftResult, VisitorResult tentativeRightResult)
			{
				return VisitorResult.AndOperation(tentativeLeftResult.Negated, tentativeRightResult.Negated).Negated;
			}
		}

		class NullAnalysisVisitor : DepthFirstAstVisitor<VariableStatusInfo, VisitorResult>
		{
			NullValueAnalysis analysis;

			public NullAnalysisVisitor(NullValueAnalysis analysis) {
				this.analysis = analysis;
			}

			protected override VisitorResult VisitChildren(AstNode node, VariableStatusInfo data)
			{
				Debug.Fail("Missing override for " + node.GetType().Name);
				return VisitorResult.ForValue(data, NullValueStatus.Unknown);
			}
			
			public override VisitorResult VisitNullNode(AstNode nullNode, VariableStatusInfo data)
			{
				// can occur due to syntax errors
				return VisitorResult.ForValue(data, NullValueStatus.Unknown);
			}
			
			public override VisitorResult VisitEmptyStatement(EmptyStatement emptyStatement, VariableStatusInfo data)
			{
				return VisitorResult.ForValue(data, NullValueStatus.Unknown);
			}

			public override VisitorResult VisitBlockStatement(BlockStatement blockStatement, VariableStatusInfo data)
			{
				//We'll visit the child statements later (we'll visit each one directly from the CFG)
				//As such this is mostly a dummy node.
				return new VisitorResult { Variables = data };
			}

			public override VisitorResult VisitVariableDeclarationStatement(VariableDeclarationStatement variableDeclarationStatement, VariableStatusInfo data)
			{
				foreach (var variable in variableDeclarationStatement.Variables) {
					var result = variable.AcceptVisitor(this, data);
					if (result.ThrowsException)
						return result;
					data = result.Variables;
				}

				return VisitorResult.ForValue(data, NullValueStatus.Unknown);
			}

			public override VisitorResult VisitVariableInitializer(VariableInitializer variableInitializer, VariableStatusInfo data)
			{
				if (variableInitializer.Initializer.IsNull) {
					data = data.Clone();
					data[variableInitializer.Name] = NullValueStatus.Unassigned;
				} else {
					var result = variableInitializer.Initializer.AcceptVisitor(this, data);
					if (result.ThrowsException)
						return result;
					data = result.Variables.Clone();
					data[variableInitializer.Name] = result.NullableReturnResult;
				}

				return VisitorResult.ForValue(data, data [variableInitializer.Name]);
			}

			public override VisitorResult VisitIfElseStatement(IfElseStatement ifElseStatement, VariableStatusInfo data)
			{
				//We'll visit the true/false statements later (directly from the CFG)
				return ifElseStatement.Condition.AcceptVisitor(this, data);
			}

			public override VisitorResult VisitWhileStatement(WhileStatement whileStatement, VariableStatusInfo data)
			{
				return whileStatement.Condition.AcceptVisitor(this, data);
			}

			public override VisitorResult VisitDoWhileStatement(DoWhileStatement doWhileStatement, VariableStatusInfo data)
			{
				return doWhileStatement.Condition.AcceptVisitor(this, data);
			}

			public override VisitorResult VisitForStatement(ForStatement forStatement, VariableStatusInfo data)
			{
				//The initializers, the embedded statement and the iterators aren't visited here
				//because they have their own CFG nodes.
				if (forStatement.Condition.IsNull)
					return VisitorResult.ForValue(data, NullValueStatus.Unknown);
				return forStatement.Condition.AcceptVisitor(this, data);
			}

			public override VisitorResult VisitForeachStatement(ForeachStatement foreachStatement, VariableStatusInfo data)
			{
				var newVariable = foreachStatement.VariableNameToken;
				var inExpressionResult = foreachStatement.InExpression.AcceptVisitor(this, data);
				if (inExpressionResult.ThrowsException)
					return inExpressionResult;

				var newData = inExpressionResult.Variables.Clone();

				var resolveResult = analysis.context.Resolve(foreachStatement.VariableNameToken) as LocalResolveResult;
				if (resolveResult != null) {
					//C# 5.0 changed the meaning of foreach so that each iteration declares a new variable
					//as such, the variable is "uncaptured" only for C# >= 5.0
					if (analysis.context.Supports(new Version(5, 0)) || data[newVariable.Name] != NullValueStatus.CapturedUnknown) {
						newData[newVariable.Name] = NullValueAnalysis.IsTypeNullable(resolveResult.Type) ? inExpressionResult.EnumeratedValueResult : NullValueStatus.DefinitelyNotNull;
					}
				}

				return VisitorResult.ForValue(newData, NullValueStatus.Unknown);
			}

			public override VisitorResult VisitUsingStatement(UsingStatement usingStatement, VariableStatusInfo data)
			{
				return usingStatement.ResourceAcquisition.AcceptVisitor(this, data);
			}

			public override VisitorResult VisitFixedStatement(FixedStatement fixedStatement, VariableStatusInfo data)
			{
				foreach (var variable in fixedStatement.Variables) {
					var result = variable.AcceptVisitor(this, data);
					if (result.ThrowsException)
						return result;
					data = result.Variables;
				}

				return VisitorResult.ForValue(data, NullValueStatus.Unknown);
			}

			public override VisitorResult VisitSwitchStatement(SwitchStatement switchStatement, VariableStatusInfo data)
			{
				//We could do better than this, but it would require special handling outside the visitor
				//so for now, for simplicity, we'll just take the easy way

				var tentativeResult = switchStatement.Expression.AcceptVisitor(this, data);
				if (tentativeResult.ThrowsException) {
					return tentativeResult;
				}

				foreach (var section in switchStatement.SwitchSections) {
					//No need to check for ThrowsException, since it will always be false (see VisitSwitchSection)
					section.AcceptVisitor(this, tentativeResult.Variables);
				}

				return VisitorResult.ForValue(tentativeResult.Variables, NullValueStatus.Unknown);
			}

			public override VisitorResult VisitSwitchSection(SwitchSection switchSection, VariableStatusInfo data)
			{
				return VisitorResult.ForValue(data, NullValueStatus.Unknown);
			}

			public override VisitorResult VisitExpressionStatement(ExpressionStatement expressionStatement, VariableStatusInfo data)
			{
				return expressionStatement.Expression.AcceptVisitor(this, data);
			}

			public override VisitorResult VisitReturnStatement(ReturnStatement returnStatement, VariableStatusInfo data)
			{
				if (returnStatement.Expression.IsNull)
					return VisitorResult.ForValue(data, NullValueStatus.Unknown);
				return returnStatement.Expression.AcceptVisitor(this, data);
			}

			public override VisitorResult VisitTryCatchStatement(TryCatchStatement tryCatchStatement, VariableStatusInfo data)
			{
				//The needs special treatment in the analyser itself
				return VisitorResult.ForValue(data, NullValueStatus.Unknown);
			}

			public override VisitorResult VisitBreakStatement(BreakStatement breakStatement, VariableStatusInfo data)
			{
				return VisitorResult.ForValue(data, NullValueStatus.Unknown);
			}

			public override VisitorResult VisitContinueStatement(ContinueStatement continueStatement, VariableStatusInfo data)
			{
				return VisitorResult.ForValue(data, NullValueStatus.Unknown);
			}

			public override VisitorResult VisitGotoStatement(GotoStatement gotoStatement, VariableStatusInfo data)
			{
				return VisitorResult.ForValue(data, NullValueStatus.Unknown);
			}

			public override VisitorResult VisitGotoCaseStatement(GotoCaseStatement gotoCaseStatement, VariableStatusInfo data)
			{
				return VisitorResult.ForValue(data, NullValueStatus.Unknown);
			}

			public override VisitorResult VisitGotoDefaultStatement(GotoDefaultStatement gotoDefaultStatement, VariableStatusInfo data)
			{
				return VisitorResult.ForValue(data, NullValueStatus.Unknown);
			}

			public override VisitorResult VisitLabelStatement(LabelStatement labelStatement, VariableStatusInfo data)
			{
				return VisitorResult.ForValue(data, NullValueStatus.Unknown);
			}

			public override VisitorResult VisitUnsafeStatement(UnsafeStatement unsafeStatement, VariableStatusInfo data)
			{
				return VisitorResult.ForValue(data, NullValueStatus.Unknown);
			}

			public override VisitorResult VisitLockStatement(LockStatement lockStatement, VariableStatusInfo data)
			{
				var expressionResult = lockStatement.Expression.AcceptVisitor(this, data);
				if (expressionResult.ThrowsException)
					return expressionResult;

				if (expressionResult.NullableReturnResult == NullValueStatus.DefinitelyNull) {
					return VisitorResult.ForException(expressionResult.Variables);
				}

				var identifier = CSharpUtil.GetInnerMostExpression(lockStatement.Expression) as IdentifierExpression;
				if (identifier != null) {
					var identifierValue = expressionResult.Variables [identifier.Identifier];
					if (identifierValue != NullValueStatus.CapturedUnknown) {
						var newVariables = expressionResult.Variables.Clone();
						analysis.SetLocalVariableValue(newVariables, identifier, NullValueStatus.DefinitelyNotNull);

						return VisitorResult.ForValue(newVariables, NullValueStatus.Unknown);
					}
				}

				return VisitorResult.ForValue(expressionResult.Variables, NullValueStatus.Unknown);
			}

			public override VisitorResult VisitThrowStatement(ThrowStatement throwStatement, VariableStatusInfo data)
			{
				if (throwStatement.Expression.IsNull)
					return VisitorResult.ForValue(data, NullValueStatus.DefinitelyNotNull);
				return throwStatement.Expression.AcceptVisitor(this, data);
			}

			public override VisitorResult VisitYieldBreakStatement(YieldBreakStatement yieldBreakStatement, VariableStatusInfo data)
			{
				return VisitorResult.ForValue(data, NullValueStatus.Unknown);
			}

			public override VisitorResult VisitYieldReturnStatement(YieldReturnStatement yieldReturnStatement, VariableStatusInfo data)
			{
				return yieldReturnStatement.Expression.AcceptVisitor(this, data);
			}

			public override VisitorResult VisitCheckedStatement(CheckedStatement checkedStatement, VariableStatusInfo data)
			{
				return VisitorResult.ForValue(data, NullValueStatus.Unknown);
			}

			public override VisitorResult VisitUncheckedStatement(UncheckedStatement uncheckedStatement, VariableStatusInfo data)
			{
				return VisitorResult.ForValue(data, NullValueStatus.Unknown);
			}

			void RegisterExpressionResult(Expression expression, NullValueStatus expressionResult)
			{
				NullValueStatus oldStatus;
				if (analysis.expressionResult.TryGetValue(expression, out oldStatus)) {
					analysis.expressionResult[expression] = VariableStatusInfo.CombineStatus(analysis.expressionResult[expression], expressionResult);
				}
				else {
					analysis.expressionResult[expression] = expressionResult;
				}
			}

			VisitorResult HandleExpressionResult(Expression expression, VariableStatusInfo dataAfterExpression, NullValueStatus expressionResult) {
				RegisterExpressionResult(expression, expressionResult);

				return VisitorResult.ForValue(dataAfterExpression, expressionResult);
			}

			VisitorResult HandleExpressionResult(Expression expression, VariableStatusInfo dataAfterExpression, bool expressionResult) {
				RegisterExpressionResult(expression, NullValueStatus.DefinitelyNotNull);

				return VisitorResult.ForBoolValue(dataAfterExpression, expressionResult);
			}

			VisitorResult HandleExpressionResult(Expression expression, VisitorResult result) {
				RegisterExpressionResult(expression, result.NullableReturnResult);

				return result;
			}

			public override VisitorResult VisitAssignmentExpression(AssignmentExpression assignmentExpression, VariableStatusInfo data)
			{
				var tentativeResult = assignmentExpression.Left.AcceptVisitor(this, data);
				if (tentativeResult.ThrowsException)
					return HandleExpressionResult(assignmentExpression, tentativeResult);
				tentativeResult = assignmentExpression.Right.AcceptVisitor(this, tentativeResult.Variables);
				if (tentativeResult.ThrowsException)
					return HandleExpressionResult(assignmentExpression, tentativeResult);

				var leftIdentifier = assignmentExpression.Left as IdentifierExpression;
				if (leftIdentifier != null) {
					var resolveResult = analysis.context.Resolve(leftIdentifier);
					if (resolveResult.IsError) {
						return HandleExpressionResult(assignmentExpression, data, NullValueStatus.Error);
					}

					if (resolveResult is LocalResolveResult) {
						var result = new VisitorResult();
						result.NullableReturnResult = tentativeResult.NullableReturnResult;
						result.Variables = tentativeResult.Variables.Clone();
						var oldValue = result.Variables [leftIdentifier.Identifier];

						if (assignmentExpression.Operator == AssignmentOperatorType.Assign ||
						    oldValue == NullValueStatus.Unassigned ||
						    oldValue == NullValueStatus.DefinitelyNotNull ||
						    tentativeResult.NullableReturnResult == NullValueStatus.Error ||
						    tentativeResult.NullableReturnResult == NullValueStatus.Unknown) {
							analysis.SetLocalVariableValue(result.Variables, leftIdentifier, tentativeResult.NullableReturnResult);
						} else {
							if (oldValue == NullValueStatus.DefinitelyNull) {
								//Do nothing --it'll remain null
							} else {
								analysis.SetLocalVariableValue(result.Variables, leftIdentifier, NullValueStatus.PotentiallyNull);
							}
						}

						return HandleExpressionResult(assignmentExpression, result);
					}
				}

				return HandleExpressionResult(assignmentExpression, tentativeResult);
			}

			public override VisitorResult VisitIdentifierExpression(IdentifierExpression identifierExpression, VariableStatusInfo data)
			{
				var resolveResult = analysis.context.Resolve(identifierExpression);
				if (resolveResult.IsError) {
					return HandleExpressionResult(identifierExpression, data, NullValueStatus.Error);
				}
				var local = resolveResult as LocalResolveResult;
				if (local != null) {
					var value = data [local.Variable.Name];
					if (value == NullValueStatus.CapturedUnknown)
						value = NullValueStatus.Unknown;
					return HandleExpressionResult(identifierExpression, data, value);
				}
				if (resolveResult.IsCompileTimeConstant) {
					object value = resolveResult.ConstantValue;
					if (value == null) {
						return HandleExpressionResult(identifierExpression, data, NullValueStatus.DefinitelyNull);
					}
					var boolValue = value as bool?;
					if (boolValue != null) {
						return VisitorResult.ForBoolValue(data, (bool)boolValue);
					}
					return HandleExpressionResult(identifierExpression, data, NullValueStatus.DefinitelyNotNull);
				}

				var memberResolveResult = resolveResult as MemberResolveResult;

				var returnValue = GetFieldReturnValue(memberResolveResult, data);

				return HandleExpressionResult(identifierExpression, data, returnValue);
			}

			public override VisitorResult VisitDefaultValueExpression(DefaultValueExpression defaultValueExpression, VariableStatusInfo data)
			{
				var resolveResult = analysis.context.Resolve(defaultValueExpression);
				if (resolveResult.IsError) {
					return HandleExpressionResult(defaultValueExpression, data, NullValueStatus.Unknown);
				}

				Debug.Assert(resolveResult.IsCompileTimeConstant);

				var status = resolveResult.ConstantValue == null  && resolveResult.Type.IsReferenceType != false ? NullValueStatus.DefinitelyNull : NullValueStatus.DefinitelyNotNull;
				return HandleExpressionResult(defaultValueExpression, data, status);
			}

			public override VisitorResult VisitNullReferenceExpression(NullReferenceExpression nullReferenceExpression, VariableStatusInfo data)
			{
				return HandleExpressionResult(nullReferenceExpression, data, NullValueStatus.DefinitelyNull);
			}

			public override VisitorResult VisitPrimitiveExpression(PrimitiveExpression primitiveExpression, VariableStatusInfo data)
			{
				return HandleExpressionResult(primitiveExpression, data, NullValueStatus.DefinitelyNotNull);
			}

			public override VisitorResult VisitParenthesizedExpression(ParenthesizedExpression parenthesizedExpression, VariableStatusInfo data)
			{
				return HandleExpressionResult(parenthesizedExpression, parenthesizedExpression.Expression.AcceptVisitor(this, data));
			}

			public override VisitorResult VisitConditionalExpression(ConditionalExpression conditionalExpression, VariableStatusInfo data)
			{
				var tentativeBaseResult = conditionalExpression.Condition.AcceptVisitor(this, data);
				if (tentativeBaseResult.ThrowsException)
					return HandleExpressionResult(conditionalExpression, tentativeBaseResult);

				var conditionResolveResult = analysis.context.Resolve(conditionalExpression.Condition);

				if (tentativeBaseResult.KnownBoolResult == true || true.Equals(conditionResolveResult.ConstantValue)) {
					return HandleExpressionResult(conditionalExpression, conditionalExpression.TrueExpression.AcceptVisitor(this, tentativeBaseResult.TruePathVariables));
				}
				if (tentativeBaseResult.KnownBoolResult == false || false.Equals(conditionResolveResult.ConstantValue)) {
					return HandleExpressionResult(conditionalExpression, conditionalExpression.FalseExpression.AcceptVisitor(this, tentativeBaseResult.FalsePathVariables));
				}

				//No known bool result
				var trueCaseResult = conditionalExpression.TrueExpression.AcceptVisitor(this, tentativeBaseResult.TruePathVariables);
				if (trueCaseResult.ThrowsException) {
					//We know that the true case will never be completed, then the right case is the only possible route.
					return HandleExpressionResult(conditionalExpression, conditionalExpression.FalseExpression.AcceptVisitor(this, tentativeBaseResult.FalsePathVariables));
				}
				var falseCaseResult = conditionalExpression.FalseExpression.AcceptVisitor(this, tentativeBaseResult.FalsePathVariables);
				if (falseCaseResult.ThrowsException) {
					return HandleExpressionResult(conditionalExpression, trueCaseResult.Variables, true);
				}

				return HandleExpressionResult(conditionalExpression, VisitorResult.OrOperation(trueCaseResult, falseCaseResult));
			}

			public override VisitorResult VisitBinaryOperatorExpression(BinaryOperatorExpression binaryOperatorExpression, VariableStatusInfo data)
			{
				//Let's not evaluate the sides just yet because of ??, && and ||

				//We'll register the results here (with HandleExpressionResult)
				//so each Visit*Expression won't have to do it itself
				switch (binaryOperatorExpression.Operator) {
					case BinaryOperatorType.ConditionalAnd:
						return HandleExpressionResult(binaryOperatorExpression, VisitConditionalAndExpression(binaryOperatorExpression, data));
					case BinaryOperatorType.ConditionalOr:
						return HandleExpressionResult(binaryOperatorExpression, VisitConditionalOrExpression(binaryOperatorExpression, data));
					case BinaryOperatorType.NullCoalescing:
						return HandleExpressionResult(binaryOperatorExpression, VisitNullCoalescing(binaryOperatorExpression, data));
					case BinaryOperatorType.Equality:
						return HandleExpressionResult(binaryOperatorExpression, VisitEquality(binaryOperatorExpression, data));
					case BinaryOperatorType.InEquality:
						return HandleExpressionResult(binaryOperatorExpression, VisitEquality(binaryOperatorExpression, data).Negated);
					default:
						return HandleExpressionResult(binaryOperatorExpression, VisitOtherBinaryExpression(binaryOperatorExpression, data));
				}
			}

			VisitorResult VisitOtherBinaryExpression(BinaryOperatorExpression binaryOperatorExpression, VariableStatusInfo data)
			{
				var leftTentativeResult = binaryOperatorExpression.Left.AcceptVisitor(this, data);
				if (leftTentativeResult.ThrowsException)
					return leftTentativeResult;
				var rightTentativeResult = binaryOperatorExpression.Right.AcceptVisitor(this, leftTentativeResult.Variables);
				if (rightTentativeResult.ThrowsException)
					return rightTentativeResult;

				//TODO: Assuming operators are not overloaded by users
				// (or, if they are, that they retain similar behavior to the default ones)

				switch (binaryOperatorExpression.Operator) {
					case BinaryOperatorType.LessThan:
					case BinaryOperatorType.GreaterThan:
						//Operations < and > with nulls always return false
						//Those same operations will other values may or may not return false
						if (leftTentativeResult.NullableReturnResult == NullValueStatus.DefinitelyNull &&
							rightTentativeResult.NullableReturnResult == NullValueStatus.DefinitelyNull) {
							return VisitorResult.ForBoolValue(rightTentativeResult.Variables, false);
						}
						//We don't know what the value is, but we know that both true and false are != null.
						return VisitorResult.ForValue(rightTentativeResult.Variables, NullValueStatus.DefinitelyNotNull);
					case BinaryOperatorType.LessThanOrEqual:
					case BinaryOperatorType.GreaterThanOrEqual:
						if (leftTentativeResult.NullableReturnResult == NullValueStatus.DefinitelyNull) {
							if (rightTentativeResult.NullableReturnResult == NullValueStatus.DefinitelyNull)
								return VisitorResult.ForBoolValue(rightTentativeResult.Variables, true);
							if (rightTentativeResult.NullableReturnResult == NullValueStatus.DefinitelyNotNull)
								return VisitorResult.ForBoolValue(rightTentativeResult.Variables, false);
						} else if (leftTentativeResult.NullableReturnResult == NullValueStatus.DefinitelyNotNull) {
							if (rightTentativeResult.NullableReturnResult == NullValueStatus.DefinitelyNull)
								return VisitorResult.ForBoolValue(rightTentativeResult.Variables, false);
						}

						return VisitorResult.ForValue(rightTentativeResult.Variables, NullValueStatus.Unknown);
					default:
						//Anything else: null + anything == anything + null == null.
						//not null + not null = not null
						if (leftTentativeResult.NullableReturnResult == NullValueStatus.DefinitelyNull) {
							return VisitorResult.ForValue(rightTentativeResult.Variables, NullValueStatus.DefinitelyNull);
						}
						if (leftTentativeResult.NullableReturnResult == NullValueStatus.DefinitelyNotNull) {
							if (rightTentativeResult.NullableReturnResult == NullValueStatus.DefinitelyNull)
								return VisitorResult.ForValue(rightTentativeResult.Variables, NullValueStatus.DefinitelyNull);
							if (rightTentativeResult.NullableReturnResult == NullValueStatus.DefinitelyNotNull)
								return VisitorResult.ForValue(rightTentativeResult.Variables, NullValueStatus.DefinitelyNotNull);
						}

						return VisitorResult.ForValue(rightTentativeResult.Variables, NullValueStatus.Unknown);
				}
			}

			VisitorResult WithVariableValue(VisitorResult result, IdentifierExpression identifier, bool isNull)
			{
				var localVariableResult = analysis.context.Resolve(identifier) as LocalResolveResult;
				if (localVariableResult != null) {
					result.ConditionalBranchInfo.TrueResultVariableNullStates[identifier.Identifier] = isNull;
					if (isNull) {
						result.ConditionalBranchInfo.FalseResultVariableNullStates[identifier.Identifier] = false;
					}
				}
				return result;
			}

			VisitorResult VisitEquality(BinaryOperatorExpression binaryOperatorExpression, VariableStatusInfo data)
			{
				//TODO: Should this check for user operators?

				var tentativeLeftResult = binaryOperatorExpression.Left.AcceptVisitor(this, data);
				if (tentativeLeftResult.ThrowsException)
					return tentativeLeftResult;
				var tentativeRightResult = binaryOperatorExpression.Right.AcceptVisitor(this, tentativeLeftResult.Variables);
				if (tentativeRightResult.ThrowsException)
					return tentativeRightResult;

				if (tentativeLeftResult.KnownBoolResult != null && tentativeLeftResult.KnownBoolResult == tentativeRightResult.KnownBoolResult) {
					return VisitorResult.ForBoolValue(tentativeRightResult.Variables, true);
				}

				if (tentativeLeftResult.KnownBoolResult != null && tentativeLeftResult.KnownBoolResult == !tentativeRightResult.KnownBoolResult) {
					return VisitorResult.ForBoolValue(tentativeRightResult.Variables, false);
				}

				if (tentativeLeftResult.NullableReturnResult.IsDefiniteValue()) {
					if (tentativeRightResult.NullableReturnResult.IsDefiniteValue()) {
						if (tentativeLeftResult.NullableReturnResult == NullValueStatus.DefinitelyNull || tentativeRightResult.NullableReturnResult == NullValueStatus.DefinitelyNull) {
							return VisitorResult.ForBoolValue(tentativeRightResult.Variables, tentativeLeftResult.NullableReturnResult == tentativeRightResult.NullableReturnResult);
						}
					}
				}

				var result = new VisitorResult();
				result.Variables = tentativeRightResult.Variables;
				result.NullableReturnResult = NullValueStatus.Unknown;
				result.ConditionalBranchInfo = new ConditionalBranchInfo();

				if (tentativeRightResult.NullableReturnResult.IsDefiniteValue()) {
					var identifier = CSharpUtil.GetInnerMostExpression(binaryOperatorExpression.Left) as IdentifierExpression;

					if (identifier != null) {
						bool isNull = (tentativeRightResult.NullableReturnResult == NullValueStatus.DefinitelyNull);

						WithVariableValue(result, identifier, isNull);
					}
				}

				if (tentativeLeftResult.NullableReturnResult.IsDefiniteValue()) {
					var identifier = CSharpUtil.GetInnerMostExpression(binaryOperatorExpression.Right) as IdentifierExpression;

					if (identifier != null) {
						bool isNull = (tentativeLeftResult.NullableReturnResult == NullValueStatus.DefinitelyNull);

						WithVariableValue(result, identifier, isNull);
					}
				}

				return result;
			}

			VisitorResult VisitConditionalAndExpression(BinaryOperatorExpression binaryOperatorExpression, VariableStatusInfo data)
			{
				var tentativeLeftResult = binaryOperatorExpression.Left.AcceptVisitor(this, data);
				if (tentativeLeftResult.KnownBoolResult == false || tentativeLeftResult.ThrowsException) {
					return tentativeLeftResult;
				}

				var truePath = tentativeLeftResult.TruePathVariables;
				var tentativeRightResult = binaryOperatorExpression.Right.AcceptVisitor(this, truePath);
				if (tentativeRightResult.ThrowsException) {
					//If the true path throws an exception, then the only way for the expression to complete
					//successfully is if the left expression is false
					return VisitorResult.ForBoolValue(tentativeLeftResult.FalsePathVariables, false);
				}

				return VisitorResult.AndOperation(tentativeLeftResult, tentativeRightResult);
			}

			VisitorResult VisitConditionalOrExpression(BinaryOperatorExpression binaryOperatorExpression, VariableStatusInfo data)
			{
				var tentativeLeftResult = binaryOperatorExpression.Left.AcceptVisitor(this, data);
				if (tentativeLeftResult.KnownBoolResult == true || tentativeLeftResult.ThrowsException) {
					return tentativeLeftResult;
				}

				var falsePath = tentativeLeftResult.FalsePathVariables;
				var tentativeRightResult = binaryOperatorExpression.Right.AcceptVisitor(this, falsePath);
				if (tentativeRightResult.ThrowsException) {
					//If the false path throws an exception, then the only way for the expression to complete
					//successfully is if the left expression is true
					return VisitorResult.ForBoolValue(tentativeLeftResult.TruePathVariables, true);
				}

				return VisitorResult.OrOperation(tentativeLeftResult, tentativeRightResult);
			}

			VisitorResult VisitNullCoalescing(BinaryOperatorExpression binaryOperatorExpression, VariableStatusInfo data)
			{
				var leftTentativeResult = binaryOperatorExpression.Left.AcceptVisitor(this, data);
				if (leftTentativeResult.NullableReturnResult == NullValueStatus.DefinitelyNotNull || leftTentativeResult.ThrowsException) {
					return leftTentativeResult;
				}

				//If the right side is found, then the left side is known to be null
				var newData = leftTentativeResult.Variables;
				var leftIdentifier = CSharpUtil.GetInnerMostExpression(binaryOperatorExpression.Left) as IdentifierExpression;
				if (leftIdentifier != null) {
					newData = newData.Clone();
					analysis.SetLocalVariableValue(newData, leftIdentifier, NullValueStatus.DefinitelyNull);
				}

				var rightTentativeResult = binaryOperatorExpression.Right.AcceptVisitor(this, newData);
				if (rightTentativeResult.ThrowsException) {
					//This means the left expression was not null all along (or else the expression will throw an exception)

					if (leftIdentifier != null) {
						newData = newData.Clone();
						analysis.SetLocalVariableValue(newData, leftIdentifier, NullValueStatus.DefinitelyNotNull);
						return VisitorResult.ForValue(newData, NullValueStatus.DefinitelyNotNull);
					}

					return VisitorResult.ForValue(leftTentativeResult.Variables, NullValueStatus.DefinitelyNotNull);
				}

				var mergedVariables = rightTentativeResult.Variables;
				var nullValue = rightTentativeResult.NullableReturnResult;

				if (leftTentativeResult.NullableReturnResult != NullValueStatus.DefinitelyNull) {
					mergedVariables = mergedVariables.Clone();
					mergedVariables.ReceiveIncoming(leftTentativeResult.Variables);
					if (nullValue == NullValueStatus.DefinitelyNull) {
						nullValue = NullValueStatus.PotentiallyNull;
					}
				}

				return VisitorResult.ForValue(mergedVariables, nullValue);
			}

			public override VisitorResult VisitUnaryOperatorExpression(UnaryOperatorExpression unaryOperatorExpression, VariableStatusInfo data)
			{
				//TODO: Again, what to do when overloaded operators are found?

				var tentativeResult = unaryOperatorExpression.Expression.AcceptVisitor(this, data);
				if (tentativeResult.ThrowsException)
					return HandleExpressionResult(unaryOperatorExpression, tentativeResult);

				if (unaryOperatorExpression.Operator == UnaryOperatorType.Not) {
					return HandleExpressionResult(unaryOperatorExpression, tentativeResult.Negated);
				}
				return HandleExpressionResult(unaryOperatorExpression, tentativeResult);
			}

			public override VisitorResult VisitInvocationExpression(InvocationExpression invocationExpression, VariableStatusInfo data)
			{
				//TODO: Handle some common methods such as string.IsNullOrEmpty

				var targetResult = invocationExpression.Target.AcceptVisitor(this, data);
				if (targetResult.ThrowsException)
					return HandleExpressionResult(invocationExpression, targetResult);

				data = targetResult.Variables;

				var methodResolveResult = analysis.context.Resolve(invocationExpression) as CSharpInvocationResolveResult;

				List<VisitorResult> parameterResults = new List<VisitorResult>();

				foreach (var argumentToHandle in invocationExpression.Arguments.Select((argument, parameterIndex) => new { argument, parameterIndex })) {
					var argument = argumentToHandle.argument;
					var parameterIndex = argumentToHandle.parameterIndex;

					var result = argument.AcceptVisitor(this, data);
					if (result.ThrowsException)
						return HandleExpressionResult(invocationExpression, result);
					parameterResults.Add(result);

					var namedArgument = argument as NamedArgumentExpression;
					
					var directionExpression = (namedArgument == null ? argument : namedArgument.Expression) as DirectionExpression;
					if (directionExpression != null && methodResolveResult != null) {
						var identifier = directionExpression.Expression as IdentifierExpression;
						if (identifier != null) {
							//out and ref parameters do *NOT* capture the variable (since they must stop changing it by the time they return)
							var identifierResolveResult = analysis.context.Resolve(identifier) as LocalResolveResult;
							if (identifierResolveResult != null && IsTypeNullable(identifierResolveResult.Type)) {
								data = data.Clone();

								FixParameter(argument, methodResolveResult.Member.Parameters, parameterIndex, identifier, data);
							}
						}


						continue;
					}

					data = result.Variables;
				}

				var identifierExpression = CSharpUtil.GetInnerMostExpression(invocationExpression.Target) as IdentifierExpression;
				if (identifierExpression != null) {
					if (targetResult.NullableReturnResult == NullValueStatus.DefinitelyNull) {
						return HandleExpressionResult(invocationExpression, VisitorResult.ForException(data));
					}

					var descendentIdentifiers = invocationExpression.Arguments.SelectMany(argument => argument.DescendantsAndSelf).OfType<IdentifierExpression>();
					if (!descendentIdentifiers.Any(identifier => identifier.Identifier == identifierExpression.Identifier)) {
						//TODO: We can make this check better (see VisitIndexerExpression for more details)
						data = data.Clone();
						analysis.SetLocalVariableValue(data, identifierExpression, NullValueStatus.DefinitelyNotNull);
					}
				}

				return HandleExpressionResult(invocationExpression, GetMethodVisitorResult(methodResolveResult, data, parameterResults));
			}

			static VisitorResult GetMethodVisitorResult(CSharpInvocationResolveResult methodResolveResult, VariableStatusInfo data, List<VisitorResult> parameterResults)
			{
				if (methodResolveResult == null)
					return VisitorResult.ForValue(data, NullValueStatus.Unknown);

				var method = methodResolveResult.Member as IMethod;
				if (method != null) {
					if (method.GetAttribute(new FullTypeName(AnnotationNames.AssertionMethodAttribute)) != null) {
						var assertionParameters = method.Parameters.Select((parameter, index) => new { index, parameter })
							.Select(parameter => new { parameter.index, parameter.parameter, attributes = parameter.parameter.Attributes.Where(attribute => attribute.AttributeType.FullName == AnnotationNames.AssertionConditionAttribute).ToList() })
							.Where(parameter => parameter.attributes.Count() == 1)
							.Select(parameter => new { parameter.index, parameter.parameter, attribute = parameter.attributes[0] })
							.ToList();

						//Unclear what should be done if there are multiple assertion conditions
						if (assertionParameters.Count() == 1) {
							Debug.Assert(methodResolveResult.Arguments.Count == parameterResults.Count);

							var assertionParameter = assertionParameters [0];
							VisitorResult assertionParameterResult = null;

							object intendedResult = true;
							var positionalArgument = assertionParameter.attribute.PositionalArguments.FirstOrDefault() as MemberResolveResult;
							if (positionalArgument != null && positionalArgument.Type.FullName == AnnotationNames.AssertionConditionTypeAttribute) {
								switch (positionalArgument.Member.FullName) {
									case AnnotationNames.AssertionConditionTypeIsTrue:
										intendedResult = true;
										break;
									case AnnotationNames.AssertionConditionTypeIsFalse:
										intendedResult = false;
										break;
									case AnnotationNames.AssertionConditionTypeIsNull:
										intendedResult = null;
										break;
									case AnnotationNames.AssertionConditionTypeIsNotNull:
										intendedResult = "<not-null>";
										break;
								}
							}

							int parameterIndex = assertionParameter.index;
							if (assertionParameter.index < methodResolveResult.Arguments.Count && !(methodResolveResult.Arguments [assertionParameter.index] is NamedArgumentResolveResult)) {
								//Use index
								assertionParameterResult = parameterResults [assertionParameter.index];
							} else {
								//Use named argument
								int? nameIndex = methodResolveResult.Arguments.Select((argument, index) => new { argument, index})
									.Where(argument => {
										var namedArgument = argument.argument as NamedArgumentResolveResult;
										return namedArgument != null && namedArgument.ParameterName == assertionParameter.parameter.Name;
									}).Select(argument => (int?)argument.index).FirstOrDefault();

								if (nameIndex != null) {
									parameterIndex = nameIndex.Value;
									assertionParameterResult = parameterResults [nameIndex.Value];
								} else if (assertionParameter.parameter.IsOptional) {
									//Try to use default value

									if (intendedResult is string) {
										if (assertionParameter.parameter.ConstantValue == null) {
											return VisitorResult.ForException(data);
										}
									} else {
										if (!object.Equals(assertionParameter.parameter.ConstantValue, intendedResult)) {
											return VisitorResult.ForException(data);
										}
									}
								} else {
									//The parameter was not specified, yet it is not optional?
									return VisitorResult.ForException(data);
								}
							}

							//Now check assertion
							if (assertionParameterResult != null) {
								if (intendedResult is bool) {
									if (assertionParameterResult.KnownBoolResult == !(bool)intendedResult) {
										return VisitorResult.ForException(data);
									}

									data = (bool)intendedResult ? assertionParameterResult.TruePathVariables : assertionParameterResult.FalsePathVariables;
								} else {
									bool shouldBeNull = intendedResult == null;

									if (assertionParameterResult.NullableReturnResult == (shouldBeNull ? NullValueStatus.DefinitelyNotNull : NullValueStatus.DefinitelyNull)) {
										return VisitorResult.ForException(data);
									}

									var parameterResolveResult = methodResolveResult.Arguments [parameterIndex];

									LocalResolveResult localVariableResult = null;

									var conversionResolveResult = parameterResolveResult as ConversionResolveResult;
									if (conversionResolveResult != null) {
										if (!IsTypeNullable(conversionResolveResult.Type)) {
											if (intendedResult == null) {
												return VisitorResult.ForException(data);
											}
										} else {
											localVariableResult = conversionResolveResult.Input as LocalResolveResult;
										}
									} else {
										localVariableResult = parameterResolveResult as LocalResolveResult;
									}

									if (localVariableResult != null && data[localVariableResult.Variable.Name] != NullValueStatus.CapturedUnknown) {
										data = data.Clone();
										data [localVariableResult.Variable.Name] = shouldBeNull ? NullValueStatus.DefinitelyNull : NullValueStatus.DefinitelyNotNull;
									}
								}
							}
						}
					}
				}

				bool isNullable = IsTypeNullable(methodResolveResult.Type);
				if (!isNullable) {
					return VisitorResult.ForValue(data, NullValueStatus.DefinitelyNotNull);
				}

				if (method != null)
					return VisitorResult.ForValue(data, GetNullableStatus(method));

				return VisitorResult.ForValue(data, GetNullableStatus(methodResolveResult.TargetResult.Type.GetDefinition()));
			}

			static NullValueStatus GetNullableStatus(IEntity entity)
			{
				if (entity.DeclaringType != null && entity.DeclaringType.Kind == TypeKind.Delegate) {
					//Handle Delegate.Invoke method
					return GetNullableStatus(entity.DeclaringTypeDefinition);
				}
				
				return GetNullableStatus(fullTypeName => entity.GetAttribute(new FullTypeName(fullTypeName)));
			}

			static NullValueStatus GetNullableStatus(IParameter parameter)
			{
				return GetNullableStatus(fullTypeName => parameter.Attributes.FirstOrDefault(attribute => attribute.AttributeType.FullName == fullTypeName));
			}

			static NullValueStatus GetNullableStatus(Func<string, IAttribute> attributeGetter)
			{
				if (attributeGetter(AnnotationNames.NotNullAttribute) != null) {
					return NullValueStatus.DefinitelyNotNull;
				}
				if (attributeGetter(AnnotationNames.CanBeNullAttribute) != null) {
					return NullValueStatus.PotentiallyNull;
				}
				return NullValueStatus.Unknown;
			}

			public override VisitorResult VisitMemberReferenceExpression(MemberReferenceExpression memberReferenceExpression, VariableStatusInfo data)
			{
				var targetResult = memberReferenceExpression.Target.AcceptVisitor(this, data);
				if (targetResult.ThrowsException)
					return HandleExpressionResult(memberReferenceExpression, targetResult);

				var variables = targetResult.Variables;

				var memberResolveResult = analysis.context.Resolve(memberReferenceExpression) as MemberResolveResult;

				var targetIdentifier = CSharpUtil.GetInnerMostExpression(memberReferenceExpression.Target) as IdentifierExpression;
				if (targetIdentifier != null) {
					if (memberResolveResult == null) {
						var invocation = memberReferenceExpression.Parent as InvocationExpression;
						if (invocation != null) {
							memberResolveResult = analysis.context.Resolve(invocation) as MemberResolveResult;
						}
					}

					if (memberResolveResult != null && memberResolveResult.Member.FullName != "System.Nullable.HasValue") {
						var method = memberResolveResult.Member as IMethod;
						if (method == null || !method.IsExtensionMethod) {
							if (targetResult.NullableReturnResult == NullValueStatus.DefinitelyNull) {
								return HandleExpressionResult(memberReferenceExpression, VisitorResult.ForException(variables));
							}
							if (variables [targetIdentifier.Identifier] != NullValueStatus.CapturedUnknown) {
								variables = variables.Clone();
								analysis.SetLocalVariableValue(variables, targetIdentifier, NullValueStatus.DefinitelyNotNull);
							}
						}
					}
				}

				var returnValue = GetFieldReturnValue(memberResolveResult, data);
				return HandleExpressionResult(memberReferenceExpression, variables, returnValue);
			}

			static NullValueStatus GetFieldReturnValue(MemberResolveResult memberResolveResult, VariableStatusInfo data)
			{
				bool isNullable = memberResolveResult == null || IsTypeNullable(memberResolveResult.Type);
				if (!isNullable) {
					return NullValueStatus.DefinitelyNotNull;
				}

				if (memberResolveResult != null) {
					return GetNullableStatus(memberResolveResult.Member);
				}

				return NullValueStatus.Unknown;
			}

			public override VisitorResult VisitTypeReferenceExpression(TypeReferenceExpression typeReferenceExpression, VariableStatusInfo data)
			{
				return HandleExpressionResult(typeReferenceExpression, data, NullValueStatus.Unknown);

			}

			void FixParameter(Expression argument, IList<IParameter> parameters, int parameterIndex, IdentifierExpression identifier, VariableStatusInfo data)
			{
				NullValueStatus newValue = NullValueStatus.Unknown;
				if (argument is NamedArgumentExpression) {
					var namedResolveResult = analysis.context.Resolve(argument) as NamedArgumentResolveResult;
					if (namedResolveResult != null) {
						newValue = GetNullableStatus(namedResolveResult.Parameter);
					}
				}
				else {
					var parameter = parameters[parameterIndex];
					newValue = GetNullableStatus(parameter);
				}
				analysis.SetLocalVariableValue(data, identifier, newValue);
			}

			public override VisitorResult VisitObjectCreateExpression(ObjectCreateExpression objectCreateExpression, VariableStatusInfo data)
			{
				foreach (var argumentToHandle in objectCreateExpression.Arguments.Select((argument, parameterIndex) => new { argument, parameterIndex })) {
					var argument = argumentToHandle.argument;
					var parameterIndex = argumentToHandle.parameterIndex;

					var namedArgument = argument as NamedArgumentExpression;

					var directionExpression = (namedArgument == null ? argument : namedArgument.Expression) as DirectionExpression;
					if (directionExpression != null) {
						var identifier = directionExpression.Expression as IdentifierExpression;
						if (identifier != null && data [identifier.Identifier] != NullValueStatus.CapturedUnknown) {
							//out and ref parameters do *NOT* capture the variable (since they must stop changing it by the time they return)
							data = data.Clone();

							var constructorResolveResult = analysis.context.Resolve(objectCreateExpression) as CSharpInvocationResolveResult;
							if (constructorResolveResult != null)
								FixParameter(argument, constructorResolveResult.Member.Parameters, parameterIndex, identifier, data);
						}
						continue;
					}

					var argumentResult = argument.AcceptVisitor(this, data);
					if (argumentResult.ThrowsException)
						return argumentResult;

					data = argumentResult.Variables;
				}

				//Constructors never return null
				return HandleExpressionResult(objectCreateExpression, data, NullValueStatus.DefinitelyNotNull);
			}

			public override VisitorResult VisitArrayCreateExpression(ArrayCreateExpression arrayCreateExpression, VariableStatusInfo data)
			{
				foreach (var argument in arrayCreateExpression.Arguments) {
					var result = argument.AcceptVisitor(this, data);
					if (result.ThrowsException)
						return result;
					data = result.Variables.Clone();
				}

				if (arrayCreateExpression.Initializer.IsNull) {
					return HandleExpressionResult(arrayCreateExpression, data, NullValueStatus.DefinitelyNotNull);
				}

				return HandleExpressionResult(arrayCreateExpression, arrayCreateExpression.Initializer.AcceptVisitor(this, data));
			}

			public override VisitorResult VisitArrayInitializerExpression(ArrayInitializerExpression arrayInitializerExpression, VariableStatusInfo data)
			{
				if (arrayInitializerExpression.IsSingleElement) {
					return HandleExpressionResult(arrayInitializerExpression, arrayInitializerExpression.Elements.Single().AcceptVisitor(this, data));
				}
				if (!arrayInitializerExpression.Elements.Any()) {
					//Empty array
					return HandleExpressionResult(arrayInitializerExpression, VisitorResult.ForValue(data, NullValueStatus.Unknown));
				}

				NullValueStatus enumeratedValue = NullValueStatus.UnreachableOrInexistent;
				foreach (var element in arrayInitializerExpression.Elements) {
					var result = element.AcceptVisitor(this, data);
					if (result.ThrowsException)
						return result;
					data = result.Variables.Clone();
					enumeratedValue = VariableStatusInfo.CombineStatus(enumeratedValue, result.NullableReturnResult);

				}
				return HandleExpressionResult(arrayInitializerExpression, VisitorResult.ForEnumeratedValue(data, enumeratedValue));
			}

			public override VisitorResult VisitAnonymousTypeCreateExpression(AnonymousTypeCreateExpression anonymousTypeCreateExpression, VariableStatusInfo data)
			{
				foreach (var initializer in anonymousTypeCreateExpression.Initializers) {
					var result = initializer.AcceptVisitor(this, data);
					if (result.ThrowsException)
						return result;
					data = result.Variables;
				}

				return HandleExpressionResult(anonymousTypeCreateExpression, data, NullValueStatus.DefinitelyNotNull);
			}

			public override VisitorResult VisitLambdaExpression(LambdaExpression lambdaExpression, VariableStatusInfo data)
			{
				var newData = data.Clone();

				var identifiers = lambdaExpression.Descendants.OfType<IdentifierExpression>();
				foreach (var identifier in identifiers) {
					//Check if it is in a "change-null-state" context
					//For instance, x++ does not change the null state
					//but `x = y` does.
					if (identifier.Parent is AssignmentExpression && identifier.Role == AssignmentExpression.LeftRole) {
						var parent = (AssignmentExpression)identifier.Parent;
						if (parent.Operator != AssignmentOperatorType.Assign) {
							continue;
						}
					} else {
						//No other context matters
						//Captured variables are never passed by reference (out/ref)
						continue;
					}

					//At this point, we know there's a good chance the variable has been changed
					var identifierResolveResult = analysis.context.Resolve(identifier) as LocalResolveResult;
					if (identifierResolveResult != null && IsTypeNullable(identifierResolveResult.Type)) {
						analysis.SetLocalVariableValue(newData, identifier, NullValueStatus.CapturedUnknown);
					}
				}

				//The lambda itself is known not to be null
				return HandleExpressionResult(lambdaExpression, newData, NullValueStatus.DefinitelyNotNull);
			}

			public override VisitorResult VisitAnonymousMethodExpression(AnonymousMethodExpression anonymousMethodExpression, VariableStatusInfo data)
			{
				var newData = data.Clone();

				var identifiers = anonymousMethodExpression.Descendants.OfType<IdentifierExpression>();
				foreach (var identifier in identifiers) {
					//Check if it is in a "change-null-state" context
					//For instance, x++ does not change the null state
					//but `x = y` does.
					if (identifier.Parent is AssignmentExpression && identifier.Role == AssignmentExpression.LeftRole) {
						var parent = (AssignmentExpression)identifier.Parent;
						if (parent.Operator != AssignmentOperatorType.Assign) {
							continue;
						}
					} else {
						//No other context matters
						//Captured variables are never passed by reference (out/ref)
						continue;
					}

					//At this point, we know there's a good chance the variable has been changed
					var identifierResolveResult = analysis.context.Resolve(identifier) as LocalResolveResult;
					if (identifierResolveResult != null && IsTypeNullable(identifierResolveResult.Type)) {
						analysis.SetLocalVariableValue(newData, identifier, NullValueStatus.CapturedUnknown);
					}
				}

				//The anonymous method itself is known not to be null
				return HandleExpressionResult(anonymousMethodExpression, newData, NullValueStatus.DefinitelyNotNull);
			}


			public override VisitorResult VisitNamedExpression(NamedExpression namedExpression, VariableStatusInfo data)
			{
				return HandleExpressionResult(namedExpression, namedExpression.Expression.AcceptVisitor(this, data));
			}

			public override VisitorResult VisitAsExpression(AsExpression asExpression, VariableStatusInfo data)
			{
				var tentativeResult = asExpression.Expression.AcceptVisitor(this, data);
				if (tentativeResult.ThrowsException)
					return tentativeResult;

				NullValueStatus result;
				if (tentativeResult.NullableReturnResult == NullValueStatus.DefinitelyNull) {
					result = NullValueStatus.DefinitelyNull;
				} else {
					var asResolveResult = analysis.context.Resolve(asExpression) as CastResolveResult;
					if (asResolveResult == null ||
					    asResolveResult.IsError ||
					    asResolveResult.Input.Type.Kind == TypeKind.Unknown ||
					    asResolveResult.Type.Kind == TypeKind.Unknown) {

						result = NullValueStatus.Unknown;
					} else {
						var conversion = new CSharpConversions(analysis.context.Compilation);
						var foundConversion = conversion.ExplicitConversion(asResolveResult.Input.Type, asResolveResult.Type);

						if (foundConversion == Conversion.None) {
							result = NullValueStatus.DefinitelyNull;
						} else if (foundConversion == Conversion.IdentityConversion) {
							result = tentativeResult.NullableReturnResult;
						} else {
							result = NullValueStatus.PotentiallyNull;
						}
					}
				}
				return HandleExpressionResult(asExpression, tentativeResult.Variables, result);
			}

			public override VisitorResult VisitCastExpression(CastExpression castExpression, VariableStatusInfo data)
			{
				var tentativeResult = castExpression.Expression.AcceptVisitor(this, data);
				if (tentativeResult.ThrowsException)
					return tentativeResult;

				NullValueStatus result;
				if (tentativeResult.NullableReturnResult == NullValueStatus.DefinitelyNull) {
					result = NullValueStatus.DefinitelyNull;
				} else {
					result = NullValueStatus.Unknown;
				}

				VariableStatusInfo variables = tentativeResult.Variables;

				var resolveResult = analysis.context.Resolve(castExpression) as CastResolveResult;
				if (resolveResult != null && !IsTypeNullable(resolveResult.Type)) {
					if (result == NullValueStatus.DefinitelyNull) {
						return HandleExpressionResult(castExpression, VisitorResult.ForException(tentativeResult.Variables));
					}

					var identifierExpression = CSharpUtil.GetInnerMostExpression(castExpression.Expression) as IdentifierExpression;
					if (identifierExpression != null) {
						var currentValue = variables [identifierExpression.Identifier];
						if (currentValue != NullValueStatus.CapturedUnknown &&
						    currentValue != NullValueStatus.UnreachableOrInexistent &&
						    currentValue != NullValueStatus.DefinitelyNotNull) {
							//DefinitelyNotNull is included in this list because if that's the status
							// then we don't need to change anything

							variables = variables.Clone();
							variables [identifierExpression.Identifier] = NullValueStatus.DefinitelyNotNull;
						}
					}

					result = NullValueStatus.DefinitelyNotNull;
				}

				return HandleExpressionResult(castExpression, variables, result);
			}

			public override VisitorResult VisitIsExpression(IsExpression isExpression, VariableStatusInfo data)
			{
				var tentativeResult = isExpression.Expression.AcceptVisitor(this, data);
				if (tentativeResult.ThrowsException)
					return tentativeResult;

				//TODO: Consider, for instance: new X() is X. The result is known to be true, so we can use KnownBoolValue
				return HandleExpressionResult(isExpression, tentativeResult.Variables, NullValueStatus.DefinitelyNotNull);
			}

			public override VisitorResult VisitDirectionExpression(DirectionExpression directionExpression, VariableStatusInfo data)
			{
				return HandleExpressionResult(directionExpression, directionExpression.Expression.AcceptVisitor(this, data));
			}

			public override VisitorResult VisitCheckedExpression(CheckedExpression checkedExpression, VariableStatusInfo data)
			{
				return HandleExpressionResult(checkedExpression, checkedExpression.Expression.AcceptVisitor(this, data));
			}

			public override VisitorResult VisitUncheckedExpression(UncheckedExpression uncheckedExpression, VariableStatusInfo data)
			{
				return HandleExpressionResult(uncheckedExpression, uncheckedExpression.Expression.AcceptVisitor(this, data));
			}

			public override VisitorResult VisitThisReferenceExpression(ThisReferenceExpression thisReferenceExpression, VariableStatusInfo data)
			{
				return HandleExpressionResult(thisReferenceExpression, data, NullValueStatus.DefinitelyNotNull);
			}

			public override VisitorResult VisitIndexerExpression(IndexerExpression indexerExpression, VariableStatusInfo data)
			{
				var tentativeResult = indexerExpression.Target.AcceptVisitor(this, data);
				if (tentativeResult.ThrowsException)
					return tentativeResult;

				data = tentativeResult.Variables;

				foreach (var argument in indexerExpression.Arguments) {
					var result = argument.AcceptVisitor(this, data);
					if (result.ThrowsException)
						return result;
					data = result.Variables.Clone();
				}

				IdentifierExpression targetAsIdentifier = CSharpUtil.GetInnerMostExpression(indexerExpression.Target) as IdentifierExpression;
				if (targetAsIdentifier != null) {
					if (tentativeResult.NullableReturnResult == NullValueStatus.DefinitelyNull)
						return HandleExpressionResult(indexerExpression, VisitorResult.ForException(data));

					//If this doesn't cause an exception, then the target is not null
					//But we won't set it if it has been changed
					var descendentIdentifiers = indexerExpression.Arguments
						.SelectMany(argument => argument.DescendantsAndSelf).OfType<IdentifierExpression>();
					if (!descendentIdentifiers.Any(identifier => identifier.Identifier == targetAsIdentifier.Identifier)) {
						//TODO: this check might be improved to include more legitimate cases
						//A good check will necessarily have to consider captured variables
						data = data.Clone();
						analysis.SetLocalVariableValue(data, targetAsIdentifier, NullValueStatus.DefinitelyNotNull);
					}
				}

				var indexerResolveResult = analysis.context.Resolve(indexerExpression) as CSharpInvocationResolveResult;
				bool isNullable = indexerResolveResult == null || IsTypeNullable(indexerResolveResult.Type);
				
				var returnValue = isNullable ? NullValueStatus.Unknown : NullValueStatus.DefinitelyNotNull;
				return HandleExpressionResult(indexerExpression, data, returnValue);
			}

			public override VisitorResult VisitBaseReferenceExpression(BaseReferenceExpression baseReferenceExpression, VariableStatusInfo data)
			{
				return HandleExpressionResult(baseReferenceExpression, data, NullValueStatus.DefinitelyNotNull);
			}

			public override VisitorResult VisitTypeOfExpression(TypeOfExpression typeOfExpression, VariableStatusInfo data)
			{
				return HandleExpressionResult(typeOfExpression, data, NullValueStatus.DefinitelyNotNull);
			}

			public override VisitorResult VisitSizeOfExpression(SizeOfExpression sizeOfExpression, VariableStatusInfo data)
			{
				return HandleExpressionResult(sizeOfExpression, data, NullValueStatus.DefinitelyNotNull);
			}

			public override VisitorResult VisitPointerReferenceExpression(PointerReferenceExpression pointerReferenceExpression, VariableStatusInfo data)
			{
				var targetResult = pointerReferenceExpression.Target.AcceptVisitor(this, data);
				if (targetResult.ThrowsException)
					return targetResult;
				return HandleExpressionResult(pointerReferenceExpression, targetResult.Variables, NullValueStatus.DefinitelyNotNull);
			}

			public override VisitorResult VisitStackAllocExpression(StackAllocExpression stackAllocExpression, VariableStatusInfo data)
			{
				var countResult = stackAllocExpression.CountExpression.AcceptVisitor(this, data);
				if (countResult.ThrowsException)
					return countResult;
				return HandleExpressionResult(stackAllocExpression, countResult.Variables, NullValueStatus.DefinitelyNotNull);
			}

			public override VisitorResult VisitNamedArgumentExpression(NamedArgumentExpression namedArgumentExpression, VariableStatusInfo data)
			{
				return HandleExpressionResult(namedArgumentExpression, namedArgumentExpression.Expression.AcceptVisitor(this, data));
			}

			public override VisitorResult VisitUndocumentedExpression(UndocumentedExpression undocumentedExpression, VariableStatusInfo data)
			{
				throw new NotImplementedException();
			}

			public override VisitorResult VisitQueryExpression(QueryExpression queryExpression, VariableStatusInfo data)
			{
				VariableStatusInfo outgoingData = data.Clone();
				NullValueStatus? outgoingEnumeratedValue = null;
				var clauses = queryExpression.Clauses.ToList();

				var backtracingClauses = (from item in clauses.Select((clause, i) => new { clause, i })
				                                where item.clause is QueryFromClause || item.clause is QueryJoinClause || item.clause is QueryContinuationClause
				                                select item.i).ToList();

				var beforeClauseVariableStates = Enumerable.Range(0, clauses.Count).ToDictionary(clauseIndex => clauseIndex,
				                                                                                 clauseIndex => new VariableStatusInfo());
				var afterClauseVariableStates = Enumerable.Range(0, clauses.Count).ToDictionary(clauseIndex => clauseIndex,
				                                                                                clauseIndex => new VariableStatusInfo());

				VisitorResult lastValidResult = null;
				int currentClauseIndex = 0;
				for (;;) {
					VisitorResult result = null;
					QueryClause clause = null;
					bool backtrack = false;

					if (currentClauseIndex >= clauses.Count) {
						backtrack = true;
					} else {
						clause = clauses [currentClauseIndex];
						beforeClauseVariableStates [currentClauseIndex].ReceiveIncoming(data);
						result = clause.AcceptVisitor(this, data);
						data = result.Variables;
						lastValidResult = result;
						if (result.KnownBoolResult == false) {
							backtrack = true;
						}
						if (result.ThrowsException) {
							//Don't backtrack. Exceptions completely stop the query.
							break;
						}
						else {
							afterClauseVariableStates [currentClauseIndex].ReceiveIncoming(data);
						}
					}

					if (backtrack) {
						int? newIndex;
						for (;;) {
							newIndex = backtracingClauses.LastOrDefault(index => index < currentClauseIndex);
							if (newIndex == null) {
								//We've reached the end
								break;
							}

							currentClauseIndex = (int)newIndex + 1;

							if (!beforeClauseVariableStates[currentClauseIndex].ReceiveIncoming(lastValidResult.Variables)) {
								newIndex = null;
								break;
							}
						}

						if (newIndex == null) {
							break;
						}

					} else {
						if (clause is QuerySelectClause) {
							outgoingData.ReceiveIncoming(data);
							if (outgoingEnumeratedValue == null)
								outgoingEnumeratedValue = result.EnumeratedValueResult;
							else
								outgoingEnumeratedValue = VariableStatusInfo.CombineStatus(outgoingEnumeratedValue.Value, result.EnumeratedValueResult);
						}

						++currentClauseIndex;
					}
				}

				var finalData = new VariableStatusInfo();
				var endingClauseIndices = from item in clauses.Select((clause, i) => new { clause, i })
					let clause = item.clause
						where clause is QueryFromClause ||
					clause is QueryContinuationClause ||
					clause is QueryJoinClause ||
					clause is QuerySelectClause ||
					clause is QueryWhereClause
						select item.i;
				foreach (var clauseIndex in endingClauseIndices) {
					finalData.ReceiveIncoming(afterClauseVariableStates [clauseIndex]);
				}

				return VisitorResult.ForEnumeratedValue(finalData, outgoingEnumeratedValue ?? NullValueStatus.Unknown);
			}

			public override VisitorResult VisitQueryContinuationClause(QueryContinuationClause queryContinuationClause, VariableStatusInfo data)
			{
				return IntroduceVariableFromEnumeratedValue(queryContinuationClause.Identifier, queryContinuationClause.PrecedingQuery, data);
			}

			VisitorResult IntroduceVariableFromEnumeratedValue(string newVariable, Expression expression, VariableStatusInfo data)
			{
				var result = expression.AcceptVisitor(this, data);
				var newVariables = result.Variables.Clone();
				newVariables[newVariable] = result.EnumeratedValueResult;
				return VisitorResult.ForValue(newVariables, NullValueStatus.Unknown);
			}

			public override VisitorResult VisitQueryFromClause(QueryFromClause queryFromClause, VariableStatusInfo data)
			{
				return IntroduceVariableFromEnumeratedValue(queryFromClause.Identifier, queryFromClause.Expression, data);
			}

			public override VisitorResult VisitQueryJoinClause(QueryJoinClause queryJoinClause, VariableStatusInfo data)
			{
				//TODO: Check if this really works in weird edge-cases.
				var tentativeResult = IntroduceVariableFromEnumeratedValue(queryJoinClause.JoinIdentifier, queryJoinClause.InExpression, data);
				tentativeResult = queryJoinClause.OnExpression.AcceptVisitor(this, tentativeResult.Variables);
				tentativeResult = queryJoinClause.EqualsExpression.AcceptVisitor(this, tentativeResult.Variables);

				if (queryJoinClause.IsGroupJoin) {
					var newVariables = tentativeResult.Variables.Clone();
					analysis.SetLocalVariableValue(newVariables, queryJoinClause.IntoIdentifierToken, NullValueStatus.DefinitelyNotNull);
					return VisitorResult.ForValue(newVariables, NullValueStatus.Unknown);
				}

				return tentativeResult;
			}

			public override VisitorResult VisitQueryLetClause(QueryLetClause queryLetClause, VariableStatusInfo data)
			{
				var result = queryLetClause.Expression.AcceptVisitor(this, data);

				string newVariable = queryLetClause.Identifier;
				var newVariables = result.Variables.Clone();
				newVariables [newVariable] = result.NullableReturnResult;

				return VisitorResult.ForValue(newVariables, NullValueStatus.Unknown);
			}

			public override VisitorResult VisitQuerySelectClause(QuerySelectClause querySelectClause, VariableStatusInfo data)
			{
				var result = querySelectClause.Expression.AcceptVisitor(this, data);

				//The value of the expression in select becomes the "enumerated" value
				return VisitorResult.ForEnumeratedValue(result.Variables, result.NullableReturnResult);
			}

			public override VisitorResult VisitQueryWhereClause(QueryWhereClause queryWhereClause, VariableStatusInfo data)
			{
				var result = queryWhereClause.Condition.AcceptVisitor(this, data);

				return VisitorResult.ForEnumeratedValue(result.TruePathVariables, NullValueStatus.Unknown);
			}

			public override VisitorResult VisitQueryOrderClause(QueryOrderClause queryOrderClause, VariableStatusInfo data)
			{
				foreach (var ordering in queryOrderClause.Orderings) {
					data = ordering.AcceptVisitor(this, data).Variables;
				}

				return VisitorResult.ForValue(data, NullValueStatus.Unknown);
			}

			public override VisitorResult VisitQueryOrdering(QueryOrdering queryOrdering, VariableStatusInfo data)
			{
				return VisitorResult.ForValue(queryOrdering.Expression.AcceptVisitor(this, data).Variables, NullValueStatus.Unknown);
			}

			public override VisitorResult VisitQueryGroupClause(QueryGroupClause queryGroupClause, VariableStatusInfo data)
			{
				var projectionResult = queryGroupClause.Projection.AcceptVisitor(this, data);
				data = projectionResult.Variables;
				data = queryGroupClause.Key.AcceptVisitor(this, data).Variables;

				return VisitorResult.ForEnumeratedValue(data, projectionResult.NullableReturnResult);
			}
		}
	}
}

