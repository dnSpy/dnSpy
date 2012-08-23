//
// ExceptionRethrowIssue.cs
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
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.Semantics;

namespace ICSharpCode.NRefactory.CSharp.Refactoring
{
	[IssueDescription("A throw statement throws the caught exception by passing it explicitly",
	                  Description = "Finds throws that throws the caught exception and therefore should be empty.",
	                  Category = IssueCategories.CodeQualityIssues,
	                  Severity = Severity.Warning)]
	public class ExceptionRethrowIssue : ICodeIssueProvider
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
			
			public override void VisitCatchClause(CatchClause catchClause)
			{
				base.VisitCatchClause(catchClause);
				var exceptionResolveResult = ctx.Resolve(catchClause.VariableNameToken) as LocalResolveResult;
				if (exceptionResolveResult == null)
					return;

				var catchVisitor = new CatchClauseVisitor(ctx, exceptionResolveResult.Variable);
				catchClause.Body.AcceptVisitor(catchVisitor);

				foreach (var throwStatement in catchVisitor.OffendingThrows) {
					var localThrowStatement = throwStatement;
					var title = ctx.TranslateString("The exception is rethrown with explicit usage of the variable");
					var action = new CodeAction(ctx.TranslateString("Change to 'throw;'"), script => {
						script.Replace(localThrowStatement, new ThrowStatement());
					});
					AddIssue(localThrowStatement, title, new [] { action });
				}
			}
		}
		
		class CatchClauseVisitor : DepthFirstAstVisitor
		{
			BaseRefactoringContext ctx;

			IVariable parameter;

			bool variableWritten = false;

			public CatchClauseVisitor(BaseRefactoringContext context, IVariable parameter)
			{
				ctx = context;
				this.parameter = parameter;
				OffendingThrows = new List<ThrowStatement>();
			}

			public IList<ThrowStatement> OffendingThrows { get; private set; }

			void HandlePotentialWrite (Expression expression)
			{
				var variableResolveResult = ctx.Resolve(expression) as LocalResolveResult;
				if (variableResolveResult == null)
					return;
				variableWritten |= variableResolveResult.Equals(parameter);
			}

			public override void VisitAssignmentExpression(AssignmentExpression assignmentExpression)
			{
				base.VisitAssignmentExpression(assignmentExpression);

				var variableResolveResult = ctx.Resolve(assignmentExpression.Left) as LocalResolveResult;
				if (variableResolveResult == null)
					return;
				variableWritten |= variableResolveResult.Variable.Equals(parameter);
			}

			public override void VisitDirectionExpression(DirectionExpression directionExpression)
			{
				base.VisitDirectionExpression(directionExpression);

				HandlePotentialWrite(directionExpression);
			}

			public override void VisitThrowStatement(ThrowStatement throwStatement)
			{
				base.VisitThrowStatement(throwStatement);

				if (variableWritten)
					return;

				var argumentResolveResult = ctx.Resolve(throwStatement.Expression) as LocalResolveResult;
				if (argumentResolveResult == null)
					return;
				if (parameter.Equals(argumentResolveResult.Variable))
					OffendingThrows.Add(throwStatement);
			}
		}
		
	}
}

