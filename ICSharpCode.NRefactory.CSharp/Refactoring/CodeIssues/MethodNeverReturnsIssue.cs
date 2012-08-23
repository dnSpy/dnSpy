// 
// MethodNeverReturnsIssue.cs
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
using ICSharpCode.NRefactory.CSharp.Analysis;

namespace ICSharpCode.NRefactory.CSharp.Refactoring
{
	[IssueDescription ("Method never returns",
		Description = "Method does not reach its end or a 'return' statement by any of possible execution paths.",
		Category = IssueCategories.CodeQualityIssues,
		Severity = Severity.Warning,
		IssueMarker = IssueMarker.Underline)]
	public class MethodNeverReturnsIssue : ICodeIssueProvider
	{
		public IEnumerable<CodeIssue> GetIssues (BaseRefactoringContext context)
		{
			return new GatherVisitor (context).GetIssues ();
		}

		class GatherVisitor : GatherVisitorBase
		{
			static readonly ControlFlowGraphBuilder cfgBuilder = new ControlFlowGraphBuilder ();

			public GatherVisitor(BaseRefactoringContext ctx)
				: base (ctx)
			{
			}

			public override void VisitMethodDeclaration (MethodDeclaration methodDeclaration)
			{
				base.VisitMethodDeclaration (methodDeclaration);

				// partial method
				if (methodDeclaration.Body.IsNull)
					return;

				var cfg = cfgBuilder.BuildControlFlowGraph (methodDeclaration.Body, ctx.Resolver,
					ctx.CancellationToken);
				var stack = new Stack<ControlFlowNode> ();
				var visitedNodes = new HashSet<ControlFlowNode> ();
				stack.Push (cfg [0]);
				while (stack.Count > 0) {
					var node = stack.Pop ();

					// reach method's end
					if (node.PreviousStatement == methodDeclaration.Body)
						return;
					// reach a return statement
					if (node.NextStatement is ReturnStatement ||
						node.NextStatement is ThrowStatement)
						return;

					foreach (var edge in node.Outgoing) {
						if (visitedNodes.Add(edge.To))
							stack.Push(edge.To);
					}
				}

				AddIssue (methodDeclaration.NameToken, 
					ctx.TranslateString ("Method never reaches its end or a 'return' statement."));
			}
		}
	}
}
