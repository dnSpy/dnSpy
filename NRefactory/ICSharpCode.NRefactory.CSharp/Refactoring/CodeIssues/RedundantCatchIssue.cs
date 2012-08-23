//
// RedundantCatchIssue.cs
//
// Author:
//       Simon Lindgren <simon.n.lindgren@gmail.com>
//
// Copyright (c) 2012 Simon Lindgren
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

namespace ICSharpCode.NRefactory.CSharp.Refactoring
{
	[IssueDescription("A catch clause containing a single empty throw is redundant",
	                  Description = "Warns about catch clauses that only rethrows the exception.",
	                  Category = IssueCategories.Redundancies,
	                  Severity = Severity.Hint,
	                  IssueMarker = IssueMarker.GrayOut)]
	public class RedundantCatchIssue : ICodeIssueProvider
	{
		public IEnumerable<CodeIssue> GetIssues(BaseRefactoringContext context)
		{
			return new GatherVisitor(context).GetIssues();
		}
		
		class GatherVisitor : GatherVisitorBase
		{
			public GatherVisitor(BaseRefactoringContext context) : base (context)
			{
			}

			public override void VisitTryCatchStatement(TryCatchStatement tryCatchStatement)
			{
				var redundantCatchClauses = new List<CatchClause>();
				bool hasNonRedundantCatch = false;
				foreach (var catchClause in tryCatchStatement.CatchClauses) {
					if (IsRedundant(catchClause)) {
						redundantCatchClauses.Add(catchClause);
					} else {
						hasNonRedundantCatch = true;
					}
				}

				if (hasNonRedundantCatch || !tryCatchStatement.FinallyBlock.IsNull) {
					AddIssuesForClauses(redundantCatchClauses);
				} else {
					AddIssueForTryCatchStatement(tryCatchStatement);
				}
			}

			void AddIssuesForClauses(List<CatchClause> redundantCatchClauses)
			{
				var allCatchClausesMessage = ctx.TranslateString("Remove all '{0}' redundant catches");
				var removeAllRedundantClausesAction = new CodeAction(allCatchClausesMessage, script => {
					foreach (var redundantCatchClause in redundantCatchClauses) {
						script.Remove(redundantCatchClause);
					}
				});
				var singleCatchClauseMessage = ctx.TranslateString("Remove redundant catch clause");
				var redundantCatchClauseMessage = ctx.TranslateString("A catch clause containing a single empty throw statement is redundant.");
				foreach (var redundantCatchClause in redundantCatchClauses) {
					var closureLocalCatchClause = redundantCatchClause;
					var removeRedundantClauseAction = new CodeAction(singleCatchClauseMessage, script => {
						script.Remove(closureLocalCatchClause);
					});
					var actions = new List<CodeAction>();
					actions.Add(removeRedundantClauseAction);
					if (redundantCatchClauses.Count > 1) {
						actions.Add(removeAllRedundantClausesAction);
					}
					AddIssue(closureLocalCatchClause, redundantCatchClauseMessage, actions);
				}
			}

			void AddIssueForTryCatchStatement(TryCatchStatement tryCatchStatement)
			{
				var lastCatch = tryCatchStatement.CatchClauses.LastOrNullObject();
				if (lastCatch.IsNull)
					return;

				var removeTryCatchMessage = ctx.TranslateString("Remove try statement");

				var removeTryStatementAction = new CodeAction(removeTryCatchMessage, script => {
					var statements = tryCatchStatement.TryBlock.Statements;
					if (statements.Count == 1 || tryCatchStatement.Parent is BlockStatement) {
						foreach (var statement in statements) {
							script.InsertAfter(tryCatchStatement.PrevSibling, statement.Clone());
						}
						script.Remove(tryCatchStatement);
					} else {
						var blockStatement = new BlockStatement();
						foreach (var statement in statements) {
							blockStatement.Statements.Add(statement.Clone());
						}
						script.Replace(tryCatchStatement, blockStatement);
					}
					// The replace and insert script functions does not format these things well on their own
					script.FormatText(tryCatchStatement.Parent);
				});

				var fixes = new [] {
					removeTryStatementAction
				};
				AddIssue(tryCatchStatement.TryBlock.EndLocation, lastCatch.EndLocation, removeTryCatchMessage, fixes);
			}

			bool IsRedundant(CatchClause catchClause)
			{
				var firstStatement = catchClause.Body.Statements.FirstOrNullObject();
				if (firstStatement.IsNull) {
					return false;
				}
				var throwStatement = firstStatement as ThrowStatement;
				if (throwStatement == null) {
					return false;
				}
				return throwStatement.Expression.IsNull;
			}
		}
	}
}

