//
// CallToObjectEqualsViaBaseIssue.cs
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
using System.Linq;

namespace ICSharpCode.NRefactory.CSharp.Refactoring
{
	[IssueDescription("Call to base.Equals resolves to Object.Equals, which is reference equality",
	                  Description = "Finds potentially erroneous calls to Object.Equals.",
	                  Category = IssueCategories.CodeQualityIssues,
	                  Severity = Severity.Warning)]
	public class CallToObjectEqualsViaBaseIssue : ICodeIssueProvider
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

			public override void VisitInvocationExpression(InvocationExpression invocationExpression)
			{
				base.VisitInvocationExpression(invocationExpression);

				if (invocationExpression.Arguments.Count != 1) {
					return;
				}
				var memberExpression = invocationExpression.Target as MemberReferenceExpression;
				if (memberExpression == null || !(memberExpression.Target is BaseReferenceExpression)) {
					return;
				}
				var resolveResult = ctx.Resolve(invocationExpression) as InvocationResolveResult;
				if (resolveResult == null || !resolveResult.Member.DeclaringTypeDefinition.IsKnownType(KnownTypeCode.Object)) {
					return;
				}
				var title = ctx.TranslateString("Call to base.Equals resolves to Object.Equals, which is reference equality");
				AddIssue(invocationExpression, title, GetActions(invocationExpression));
			}

			IEnumerable<CodeAction> GetActions(InvocationExpression invocationExpression)
			{
				yield return new CodeAction(ctx.TranslateString("Change invocation to call Object.ReferenceEquals"), script => {
					var args = Enumerable.Concat(new [] { new ThisReferenceExpression() }, invocationExpression.Arguments.Select(arg => arg.Clone()));
					var newInvocation = MakeInvocation("object.ReferenceEquals", args);
					script.Replace(invocationExpression, newInvocation);
				});
				yield return new CodeAction(ctx.TranslateString("Remove 'base.'"), script => {
					var newInvocation = MakeInvocation("Equals", invocationExpression.Arguments.Select(arg => arg.Clone()));
					script.Replace(invocationExpression, newInvocation);
				});
			}

			static InvocationExpression MakeInvocation(string memberName, IEnumerable<Expression> unClonedArguments)
			{
				return new InvocationExpression(new IdentifierExpression(memberName), unClonedArguments);
			}
		}
	}
}

