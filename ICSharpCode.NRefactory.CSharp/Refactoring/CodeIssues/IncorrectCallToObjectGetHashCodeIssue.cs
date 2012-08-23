//
// IncorrectCallToObjectGetHashCodeIssue.cs
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
using System.Collections.Generic;
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.Semantics;

namespace ICSharpCode.NRefactory.CSharp.Refactoring
{
	[IssueDescription("Call resolves to Object.GetHashCode, which is reference based",
	                   Description = "Finds calls to Object.GetHashCode inside overridden GetHashCode.",
	                   Category = IssueCategories.CodeQualityIssues,
	                   Severity = Severity.Warning)]
	public class IncorrectCallToObjectGetHashCodeIssue : ICodeIssueProvider
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

			public override void VisitMethodDeclaration(MethodDeclaration methodDeclaration)
			{
				// Check that this declaration is a GetHashCode override, _then_ continue traversing

				if (methodDeclaration.Name != "GetHashCode") {
					return;
				}
				if (!methodDeclaration.Modifiers.HasFlag(Modifiers.Override)) {
					return;
				}

				base.VisitMethodDeclaration(methodDeclaration);
			}

			public override void VisitInvocationExpression(InvocationExpression invocationExpression)
			{
				base.VisitInvocationExpression(invocationExpression);

				var resolveResult = ctx.Resolve(invocationExpression) as InvocationResolveResult;
				if (resolveResult == null || !(resolveResult.TargetResult is ThisResolveResult) ||
				    !resolveResult.Member.DeclaringTypeDefinition.IsKnownType(KnownTypeCode.Object)) {
					return;
				}
				AddIssue(invocationExpression, ctx.TranslateString("Call resolves to Object.GetHashCode, which is reference based"));
			}
		}
	}
}

