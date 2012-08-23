// 
// MultipleEnumerationIssue.cs
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

using System;
using System.Collections.Generic;
using System.Linq;
using ICSharpCode.NRefactory.CSharp.Resolver;
using ICSharpCode.NRefactory.Semantics;
using ICSharpCode.NRefactory.TypeSystem;

namespace ICSharpCode.NRefactory.CSharp.Refactoring
{
	[IssueDescription ("Possible mutiple enumeration of IEnuemrable",
					   Description = "Possible multiple enumeration of IEnumerable.",
					   Category = IssueCategories.CodeQualityIssues,
					   Severity = Severity.Warning,
					   IssueMarker = IssueMarker.Underline)]
	public class MultipleEnumerationIssue : ICodeIssueProvider
	{
		public IEnumerable<CodeIssue> GetIssues (BaseRefactoringContext context)
		{
			var unit = context.RootNode as SyntaxTree;
			if (unit == null)
				return Enumerable.Empty<CodeIssue> ();

			return new GatherVisitor (context, unit).GetIssues ();
		}

		class AnalysisStatementCollector : DepthFirstAstVisitor
		{
			List<Statement> statements;
			AstNode variableDecl;

			AnalysisStatementCollector (AstNode variableDecl)
			{
				this.variableDecl = variableDecl;
			}

			IList<Statement> GetStatements ()
			{
				if (statements != null)
					return statements;

				statements = new List<Statement> ();
				var parent = variableDecl.Parent;
				while (parent != null) {
					if (parent is BlockStatement || parent is MethodDeclaration ||
						parent is AnonymousMethodExpression || parent is LambdaExpression) {
						parent.AcceptVisitor (this);
						if (parent is BlockStatement)
							statements.Add ((BlockStatement)parent);
						break;
					}
					parent = parent.Parent;
				}
				return statements;
			}

			public override void VisitMethodDeclaration (MethodDeclaration methodDeclaration)
			{
				statements.Add (methodDeclaration.Body);

				base.VisitMethodDeclaration (methodDeclaration);
			}

			public override void VisitAnonymousMethodExpression (AnonymousMethodExpression anonymousMethodExpression)
			{
				statements.Add (anonymousMethodExpression.Body);

				base.VisitAnonymousMethodExpression (anonymousMethodExpression);
			}

			public override void VisitLambdaExpression (LambdaExpression lambdaExpression)
			{
				var body = lambdaExpression.Body as BlockStatement;
				if (body != null)
					statements.Add (body);

				base.VisitLambdaExpression (lambdaExpression);
			}

			public static IList<Statement> Collect (AstNode variableDecl)
			{
				return new AnalysisStatementCollector (variableDecl).GetStatements ();
			}
		}

		class GatherVisitor : GatherVisitorBase
		{
			static FindReferences refFinder = new FindReferences ();

			SyntaxTree unit;
			HashSet<AstNode> collectedAstNodes;

			public GatherVisitor (BaseRefactoringContext ctx, SyntaxTree unit)
				: base (ctx)
			{
				this.unit = unit;
				this.collectedAstNodes = new HashSet<AstNode> ();
			}

			void AddIssue (AstNode node)
			{
				if (collectedAstNodes.Add (node))
					AddIssue (node, ctx.TranslateString ("Possible multiple enumeration of IEnumerable"));
			}

			void AddIssues (IEnumerable<AstNode> nodes)
			{
				foreach (var node in nodes)
					AddIssue (node);
			}

			public override void VisitParameterDeclaration (ParameterDeclaration parameterDeclaration)
			{
				base.VisitParameterDeclaration (parameterDeclaration);

				var resolveResult = ctx.Resolve (parameterDeclaration) as LocalResolveResult;
				CollectIssues (parameterDeclaration, resolveResult);
			}

			public override void VisitVariableInitializer (VariableInitializer variableInitializer)
			{
				base.VisitVariableInitializer (variableInitializer);

				var resolveResult = ctx.Resolve (variableInitializer) as LocalResolveResult;
				CollectIssues (variableInitializer, resolveResult);
			}

			static bool IsAssignment (AstNode node)
			{
				var assignment = node.Parent as AssignmentExpression;
				if (assignment != null)
					return assignment.Left == node;

				var direction = node.Parent as DirectionExpression;
				if (direction != null)
					return direction.FieldDirection == FieldDirection.Out && direction.Expression == node;

				return false;
			}

			bool IsEnumeration (AstNode node)
			{
				var foreachStatement = node.Parent as ForeachStatement;
				if (foreachStatement != null && foreachStatement.InExpression == node) {
					return true;
				}

				var memberRef = node.Parent as MemberReferenceExpression;
				if (memberRef != null && memberRef.Target == node) {
					var invocation = memberRef.Parent as InvocationExpression;
					if (invocation == null || invocation.Target != memberRef)
						return false;

					var methodGroup = ctx.Resolve (memberRef) as MethodGroupResolveResult;
					if (methodGroup == null)
						return false;

					var method = methodGroup.Methods.FirstOrDefault ();
					if (method != null) {
						var declaringTypeDef = method.DeclaringTypeDefinition;
						if (declaringTypeDef != null && declaringTypeDef.KnownTypeCode == KnownTypeCode.Object)
							return false;
					}
					return true;
				}

				return false;
			}

			HashSet<AstNode> references;
			HashSet<Statement> refStatements;
			HashSet<LambdaExpression> lambdaExpressions;

			HashSet<VariableReferenceNode> visitedNodes;
			HashSet<VariableReferenceNode> collectedNodes;
			Dictionary<VariableReferenceNode, int> nodeDegree; // number of enumerations a node can reach

			void FindReferences (AstNode variableDecl, IVariable variable)
			{
				references = new HashSet<AstNode> ();
				refStatements = new HashSet<Statement> ();
				lambdaExpressions = new HashSet<LambdaExpression> ();

				refFinder.FindLocalReferences (variable, ctx.UnresolvedFile, unit, ctx.Compilation,
					(astNode, resolveResult) => {
						if (astNode == variableDecl)
							return;

						var parent = astNode.Parent;
						while (!(parent == null || parent is Statement || parent is LambdaExpression))
							parent = parent.Parent;
						if (parent == null)
							return;

						// lambda expression with expression body, should be analyzed separately
						var expr = parent as LambdaExpression;
						if (expr != null) {
							if (IsAssignment (astNode) || IsEnumeration (astNode)) {
								references.Add (astNode);
								lambdaExpressions.Add (expr);
							}
							return;
						}

						var statement = (Statement)parent;
						if (IsAssignment (astNode) || IsEnumeration (astNode)) {
							references.Add (astNode);
							refStatements.Add (statement);
						}
					}, ctx.CancellationToken);
			}

			void CollectIssues (AstNode variableDecl, LocalResolveResult resolveResult)
			{
				if (resolveResult == null)
					return;
				var type = resolveResult.Type;
				var typeDef = type.GetDefinition ();
				if (typeDef == null ||
				    (typeDef.KnownTypeCode != KnownTypeCode.IEnumerable &&
				     typeDef.KnownTypeCode != KnownTypeCode.IEnumerableOfT))
					return;

				FindReferences (variableDecl, resolveResult.Variable);

				var statements = AnalysisStatementCollector.Collect (variableDecl);
				foreach (var statement in statements) {
					var vrNode = VariableReferenceGraphBuilder.Build (statement, references, refStatements, ctx);
					FindMultipleEnumeration (vrNode);
				}
				foreach (var lambda in lambdaExpressions) {
					var vrNode = VariableReferenceGraphBuilder.Build (references, ctx.Resolver, (Expression)lambda.Body);
					FindMultipleEnumeration (vrNode);
				}
			}

			/// <summary>
			/// split references in the specified node into sub nodes according to the value they uses
			/// </summary>
			/// <param name="node">node to split</param>
			/// <returns>list of sub nodes</returns>
			static IList<VariableReferenceNode> SplitNode (VariableReferenceNode node)
			{
				var subNodes = new List<VariableReferenceNode> ();
				// find indices of all assignments in node and use them to split references
				var assignmentIndices = new List<int> { -1 };
				for (int i = 0; i < node.References.Count; i++) {
					if (IsAssignment (node.References [i]))
						assignmentIndices.Add (i);
				}
				assignmentIndices.Add (node.References.Count);
				for (int i = 0; i < assignmentIndices.Count - 1; i++) {
					var index1 = assignmentIndices [i];
					var index2 = assignmentIndices [i + 1];
					if (index1 + 1 >= index2)
						continue;
					var subNode = new VariableReferenceNode ();
					for (int refIndex = index1 + 1; refIndex < index2; refIndex++)
						subNode.References.Add (node.References [refIndex]);
					subNodes.Add (subNode);
				}
				if (subNodes.Count == 0)
					subNodes.Add (new VariableReferenceNode ());

				var firstNode = subNodes [0];
				foreach (var prevNode in node.PreviousNodes) {
					prevNode.NextNodes.Remove (node);
					// connect two nodes if the first ref is not an assignment
					if (firstNode.References.FirstOrDefault () == node.References.FirstOrDefault ())
						prevNode.NextNodes.Add (firstNode);
				}

				var lastNode = subNodes [subNodes.Count - 1];
				foreach (var nextNode in node.NextNodes) {
					nextNode.PreviousNodes.Remove (node);
					lastNode.AddNextNode (nextNode);
				}

				return subNodes;
			}

			/// <summary>
			/// convert a variable reference graph starting from the specified node to an assignment usage graph,
			/// in which nodes are connect if and only if they contains references using the same assigned value
			/// </summary>
			/// <param name="startNode">starting node of the variable reference graph</param>
			/// <returns>
			/// list of VariableReferenceNode, each of which is a starting node of a sub-graph in which references all
			/// use the same assigned value
			/// </returns>
			static IEnumerable<VariableReferenceNode> GetAssignmentUsageGraph (VariableReferenceNode startNode)
			{
				var graph = new List<VariableReferenceNode> ();
				var visited = new HashSet<VariableReferenceNode> ();
				var stack = new Stack<VariableReferenceNode> ();
				stack.Push (startNode);
				while (stack.Count > 0) {
					var node = stack.Pop ();
					if (!visited.Add (node))
						continue;

					var nodes = SplitNode (node);
					graph.AddRange (nodes);
					foreach (var addedNode in nodes)
						visited.Add (addedNode);

					foreach (var nextNode in nodes.Last ().NextNodes)
						stack.Push (nextNode);
				}
				return graph;
			}

			void FindMultipleEnumeration (VariableReferenceNode startNode)
			{
				var vrg = GetAssignmentUsageGraph (startNode);
				visitedNodes = new HashSet<VariableReferenceNode> ();
				collectedNodes = new HashSet<VariableReferenceNode> ();

				// degree of a node is the number of references that can be reached by the node
				nodeDegree = new Dictionary<VariableReferenceNode, int> ();

				foreach (var node in vrg) {
					if (node.References.Count == 0 || !visitedNodes.Add (node))
						continue;
					ProcessNode (node);
					if (nodeDegree [node] > 1)
						collectedNodes.Add (node);
				}
				foreach (var node in collectedNodes)
					AddIssues (node.References);
			}

			void ProcessNode (VariableReferenceNode node)
			{
				var degree = nodeDegree [node] = 0;
				foreach (var nextNode in node.NextNodes) {
					collectedNodes.Add (nextNode);
					if (visitedNodes.Add (nextNode))
						ProcessNode (nextNode);
					degree = Math.Max (degree, nodeDegree [nextNode]);
				}
				nodeDegree [node] = degree + node.References.Count;
			}
		}
	}
}
