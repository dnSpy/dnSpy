// 
// RedundantElseIssue.cs
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

namespace ICSharpCode.NRefactory.CSharp.Refactoring
{
	[IssueDescription ("Redundant 'else' keyword",
						Description = "Redundant 'else' keyword.",
						Category = IssueCategories.Redundancies,
						Severity = Severity.Warning,
						IssueMarker = IssueMarker.GrayOut)]
	public class RedundantElseIssue : ICodeIssueProvider
	{
		public IEnumerable<CodeIssue> GetIssues (BaseRefactoringContext context)
		{
			return new GatherVisitor (context).GetIssues ();
		}

		class GatherVisitor : GatherVisitorBase
		{
			public GatherVisitor (BaseRefactoringContext ctx)
				: base(ctx)
			{
			}

			bool HasRundundantElse(IfElseStatement ifElseStatement)
			{
				if (ifElseStatement.FalseStatement.IsNull)
					return false;
				var blockStatement = ifElseStatement.FalseStatement as BlockStatement;
				if (blockStatement != null && blockStatement.Statements.Count == 0)
					return true;
				var reachability = ctx.CreateReachabilityAnalysis (ifElseStatement.TrueStatement);
				return !reachability.IsEndpointReachable (ifElseStatement.TrueStatement);
			}

			public override void VisitIfElseStatement (IfElseStatement ifElseStatement)
			{
				base.VisitIfElseStatement (ifElseStatement);

				if (HasRundundantElse(ifElseStatement)) {
					AddIssue (ifElseStatement.ElseToken, ctx.TranslateString ("Remove redundant 'else'"),
						script =>
						{
							int start = script.GetCurrentOffset(ifElseStatement.ElseToken.GetPrevNode ().EndLocation);
							int end;

							var blockStatement = ifElseStatement.FalseStatement as BlockStatement;
							if (blockStatement != null) {
								if (blockStatement.Statements.Count == 0) {
									// remove empty block
									end = script.GetCurrentOffset (blockStatement.LBraceToken.StartLocation);
									script.Remove (blockStatement);
								} else {
									// remove block braces
									end = script.GetCurrentOffset (blockStatement.LBraceToken.EndLocation);
									script.Remove (blockStatement.RBraceToken);
								}
							} else {
								end = script.GetCurrentOffset(ifElseStatement.ElseToken.EndLocation);
							}
							if (end > start)
								script.RemoveText (start, end - start);

							script.FormatText (ifElseStatement.Parent);
						});
				}
			}
		}
	}
}
