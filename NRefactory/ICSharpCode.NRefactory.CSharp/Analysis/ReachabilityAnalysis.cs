// Copyright (c) AlphaSierraPapa for the SharpDevelop Team
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
using System.Threading;
using ICSharpCode.NRefactory.CSharp.Resolver;
using ICSharpCode.NRefactory.CSharp.TypeSystem;
using ICSharpCode.NRefactory.Semantics;

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
		
		private ReachabilityAnalysis() {}
		
		public static ReachabilityAnalysis Create(Statement statement, CSharpAstResolver resolver = null, CancellationToken cancellationToken = default(CancellationToken))
		{
			var cfgBuilder = new ControlFlowGraphBuilder();
			var cfg = cfgBuilder.BuildControlFlowGraph(statement, resolver, cancellationToken);
			return Create(cfg, cancellationToken);
		}
		
		internal static ReachabilityAnalysis Create(Statement statement, Func<AstNode, CancellationToken, ResolveResult> resolver, CSharpTypeResolveContext typeResolveContext, CancellationToken cancellationToken)
		{
			var cfgBuilder = new ControlFlowGraphBuilder();
			var cfg = cfgBuilder.BuildControlFlowGraph(statement, resolver, typeResolveContext, cancellationToken);
			return Create(cfg, cancellationToken);
		}
		
		public static ReachabilityAnalysis Create(IList<ControlFlowNode> controlFlowGraph, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (controlFlowGraph == null)
				throw new ArgumentNullException("controlFlowGraph");
			ReachabilityAnalysis ra = new ReachabilityAnalysis();
			ra.stack.Push(controlFlowGraph[0]);
			while (ra.stack.Count > 0) {
				cancellationToken.ThrowIfCancellationRequested();
				ra.MarkReachable(ra.stack.Pop());
			}
			ra.stack = null;
			ra.visitedNodes = null;
			return ra;
		}
		
		void MarkReachable(ControlFlowNode node)
		{
			if (node.PreviousStatement != null)
				reachableEndPoints.Add(node.PreviousStatement);
			if (node.NextStatement != null)
				reachableStatements.Add(node.NextStatement);
			foreach (var edge in node.Outgoing) {
				if (visitedNodes.Add(edge.To))
					stack.Push(edge.To);
			}
		}
		
		public bool IsReachable(Statement statement)
		{
			return reachableStatements.Contains(statement);
		}
		
		public bool IsEndpointReachable(Statement statement)
		{
			return reachableEndPoints.Contains(statement);
		}
	}
}
