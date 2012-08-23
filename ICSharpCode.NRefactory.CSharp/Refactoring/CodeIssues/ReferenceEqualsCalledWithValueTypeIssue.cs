// 
// ReferenceEqualsCalledWithValueTypeIssue.cs
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
using ICSharpCode.NRefactory.Semantics;
using ICSharpCode.NRefactory.TypeSystem;

namespace ICSharpCode.NRefactory.CSharp.Refactoring
{
	[IssueDescription ("'Object.ReferenceEquals' is always false because it is called with value type",
					   Description = "'Object.ReferenceEquals' is always false because it is called with value type.",
					   Category = IssueCategories.CodeQualityIssues,
					   Severity = Severity.Warning,
					   IssueMarker = IssueMarker.Underline)]
	public class ReferenceEqualsCalledWithValueTypeIssue : ICodeIssueProvider
	{
		public IEnumerable<CodeIssue> GetIssues (BaseRefactoringContext context)
		{
			return new GatherVisitor (context).GetIssues ();
		}

		class GatherVisitor : GatherVisitorBase
		{
			public GatherVisitor(BaseRefactoringContext ctx)
				: base (ctx)
			{
			}

			public override void VisitInvocationExpression (InvocationExpression invocationExpression)
			{
				base.VisitInvocationExpression (invocationExpression);

				var resolveResult = ctx.Resolve (invocationExpression) as InvocationResolveResult;
				if (resolveResult == null || 
					resolveResult.Member.DeclaringTypeDefinition == null ||
					resolveResult.Member.DeclaringTypeDefinition.KnownTypeCode != KnownTypeCode.Object ||
					resolveResult.Member.Name != "ReferenceEquals" ||
					invocationExpression.Arguments.All(arg => ctx.Resolve(arg).Type.IsReferenceType ?? true))
					return;

				var action = new CodeAction (ctx.TranslateString ("Use Equals()"),
					script => script.Replace (invocationExpression.Target, new MemberReferenceExpression (
						new TypeReferenceExpression (new PrimitiveType ("object")), "Equals")));
				AddIssue (invocationExpression,
					ctx.TranslateString ("'Object.ReferenceEquals' is always false because it is called with value type"),
					new [] { action });
			}
		}
	}
}
