// 
// AccessToClosureIssue.cs
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
using ICSharpCode.NRefactory.Semantics;
using ICSharpCode.NRefactory.TypeSystem;

namespace ICSharpCode.NRefactory.CSharp.Refactoring
{
	public abstract class AccessToClosureIssue : ICodeIssueProvider
	{
		static FindReferences refFinder = new FindReferences ();
		static ControlFlowGraphBuilder cfgBuilder = new ControlFlowGraphBuilder ();

		public string Title
		{ get; private set; }

		protected AccessToClosureIssue (string title)
		{
			Title = title;
		}

		public IEnumerable<CodeIssue> GetIssues (BaseRefactoringContext context)
		{
			var unit = context.RootNode as SyntaxTree;
			if (unit == null)
				return Enumerable.Empty<CodeIssue> ();
			return new GatherVisitor (context, unit, this).GetIssues ();
		}

		protected virtual bool IsTargetVariable (IVariable variable)
		{
			return true;
		}

		protected abstract NodeKind GetNodeKind (AstNode node);

		protected virtual bool CanReachModification (ControlFlowNode node, Statement start,
												     IDictionary<Statement, IList<Node>> modifications)
		{
			return node.NextStatement != null && node.NextStatement != start &&
				   modifications.ContainsKey (node.NextStatement);
		}

		protected abstract IEnumerable<CodeAction> GetFixes (BaseRefactoringContext context, Node env, 
															 string variableName);

		#region GatherVisitor

		class GatherVisitor : GatherVisitorBase
		{
			SyntaxTree unit;
			string title;
			AccessToClosureIssue issueProvider;

			public GatherVisitor (BaseRefactoringContext context, SyntaxTree unit,
								  AccessToClosureIssue issueProvider)
				: base (context)
			{
				this.title = context.TranslateString (issueProvider.Title);
				this.unit = unit;
				this.issueProvider = issueProvider;
			}

			public override void VisitVariableInitializer (VariableInitializer variableInitializer)
			{
				var variableDecl = variableInitializer.Parent as VariableDeclarationStatement;
				if (variableDecl != null)
					CheckVariable (((LocalResolveResult)ctx.Resolve (variableInitializer)).Variable, 
								   variableDecl.GetParent<Statement> ());
				base.VisitVariableInitializer (variableInitializer);
			}

			public override void VisitForeachStatement (ForeachStatement foreachStatement)
			{
				CheckVariable (((LocalResolveResult)ctx.Resolve (foreachStatement.VariableNameToken)).Variable,
							   foreachStatement);
				base.VisitForeachStatement (foreachStatement);
			}

			public override void VisitParameterDeclaration (ParameterDeclaration parameterDeclaration)
			{
				var parent = parameterDeclaration.Parent;
				Statement body = null;
				if (parent is MethodDeclaration) {
					body = ((MethodDeclaration)parent).Body;
				} else if (parent is AnonymousMethodExpression) {
					body = ((AnonymousMethodExpression)parent).Body;
				} else if (parent is LambdaExpression) {
					body = ((LambdaExpression)parent).Body as Statement;
				} else if (parent is ConstructorDeclaration) {
					body = ((ConstructorDeclaration)parent).Body;
				} else if (parent is OperatorDeclaration) {
					body = ((OperatorDeclaration)parent).Body;
				}
				if (body != null)
					CheckVariable (((LocalResolveResult)ctx.Resolve (parameterDeclaration)).Variable, body);
				base.VisitParameterDeclaration (parameterDeclaration);
			}

			void FindLocalReferences (IVariable variable, FoundReferenceCallback callback)
			{
				refFinder.FindLocalReferences (variable, ctx.UnresolvedFile, unit, ctx.Compilation, callback, 
											   ctx.CancellationToken);
			}

			void CheckVariable (IVariable variable, Statement env)
			{
				if (!issueProvider.IsTargetVariable (variable))
					return;

				var root = new Environment (env, env);
				var envLookup = new Dictionary<AstNode, Environment> ();
				envLookup [env] = root;

				FindLocalReferences (variable, (astNode, resolveResult) => 
					AddNode (envLookup, new Node (astNode, issueProvider.GetNodeKind (astNode))));

				root.SortChildren ();
				CollectIssues (root, variable.Name);
			}

			void CollectIssues (Environment env, string variableName)
			{
				IList<ControlFlowNode> cfg = null;
				IDictionary<Statement, IList<Node>> modifications = null;

				if (env.Body != null) {
					cfg = cfgBuilder.BuildControlFlowGraph (env.Body);
					modifications = new Dictionary<Statement, IList<Node>> ();
					foreach (var node in env.Children) {
						if (node.Kind == NodeKind.Modification || node.Kind == NodeKind.ReferenceAndModification) {
							IList<Node> nodes;
							if (!modifications.TryGetValue (node.ContainingStatement, out nodes))
								modifications [node.ContainingStatement] = nodes = new List<Node> ();
							nodes.Add (node);
						}
					}
				}

				foreach (var child in env.GetChildEnvironments ()) {
					if (!child.IssueCollected && cfg != null && 
						CanReachModification (cfg, child, modifications))
						CollectAllIssues (child, variableName);

					CollectIssues (child, variableName);
				}
			}

			void CollectAllIssues (Environment env, string variableName)
			{
				var fixes = issueProvider.GetFixes (ctx, env, variableName).ToArray ();
				env.IssueCollected = true;

				foreach (var child in env.Children) {
					if (child is Environment) {
						CollectAllIssues ((Environment)child, variableName);
					} else {
						if (child.Kind != NodeKind.Modification)
							AddIssue (child.AstNode, title, fixes);
						// stop marking references after the variable is modified in current environment
						if (child.Kind != NodeKind.Reference)
							break;
					}
				}
			}

			void AddNode (IDictionary<AstNode, Environment> envLookup, Node node)
			{
				var astParent = node.AstNode.Parent;
				var path = new List<AstNode> ();
				while (astParent != null) {
					Environment parent;
					if (envLookup.TryGetValue (astParent, out parent)) {
						parent.Children.Add (node);
						return;
					}

					if (astParent is LambdaExpression) {
						parent = new Environment (astParent, ((LambdaExpression)astParent).Body as Statement);
					} else if (astParent is AnonymousMethodExpression) {
						parent = new Environment (astParent, ((AnonymousMethodExpression)astParent).Body);
					}

					path.Add (astParent);
					if (parent != null) {
						foreach (var astNode in path)
							envLookup [astNode] = parent;
						path.Clear ();

						parent.Children.Add (node);
						node = parent;
					}
					astParent = astParent.Parent;
				}
			}

			bool CanReachModification (IEnumerable<ControlFlowNode> cfg, Environment env,
									   IDictionary<Statement, IList<Node>> modifications)
			{
				if (modifications.Count == 0)
					return false;

				var start = env.ContainingStatement;
				if (modifications.ContainsKey (start) &&
					modifications [start].Any (v => v.AstNode.StartLocation > env.AstNode.EndLocation))
					return true;

				var stack = new Stack<ControlFlowNode> (cfg.Where (node => node.NextStatement == start));
				var visitedNodes = new HashSet<ControlFlowNode> (stack);
				while (stack.Count > 0) {
					var node = stack.Pop ();
					if (issueProvider.CanReachModification (node, start, modifications))
						return true;
					foreach (var edge in node.Outgoing) {
						if (visitedNodes.Add (edge.To))
							stack.Push (edge.To);
					}
				}
				return false;
			}

		}

		#endregion

		#region Node

		protected enum NodeKind
		{
			Reference,
			Modification,
			ReferenceAndModification,
			Environment,
		}

		protected class Node
		{
			public AstNode AstNode
			{ get; private set; }

			public NodeKind Kind
			{ get; private set; }

			public Statement ContainingStatement
			{ get; private set; }

			public Node (AstNode astNode, NodeKind kind)
			{
				AstNode = astNode;
				Kind = kind;
				ContainingStatement = astNode.GetParent<Statement> ();
			}

			public virtual IEnumerable<Node> GetAllReferences ()
			{
				yield return this;
			}
		}

		protected class Environment : Node
		{
			public Statement Body
			{ get; private set; }

			public bool IssueCollected
			{ get; set; }

			public List<Node> Children
			{ get; private set; }

			public Environment (AstNode astNode, Statement body)
				: base (astNode, NodeKind.Environment)
			{
				Body = body;
				Children = new List<Node> ();
			}

			public override IEnumerable<Node> GetAllReferences ()
			{
				return Children.SelectMany (child => child.GetAllReferences ());
			}

			public IEnumerable<Environment> GetChildEnvironments ()
			{
				return from child in Children
					   where child is Environment
					   select (Environment)child;
			}

			public void SortChildren ()
			{
				Children.Sort ((x, y) => x.AstNode.StartLocation.CompareTo(y.AstNode.StartLocation));
				foreach (var env in GetChildEnvironments ())
					env.SortChildren ();
			}
		}

		#endregion
	}
}
