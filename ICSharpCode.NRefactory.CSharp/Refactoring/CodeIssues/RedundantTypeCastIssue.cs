// 
// RedundantTypeCastIssue.cs
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
	[IssueDescription ("Redundant type cast",
						Description = "Redundant type cast.",
						Category = IssueCategories.Redundancies,
						Severity = Severity.Warning,
						IssueMarker = IssueMarker.GrayOut)]
	public class RedundantTypeCastIssue : ICodeIssueProvider
	{
		public IEnumerable<CodeIssue> GetIssues (BaseRefactoringContext context)
		{
			return new GatherVisitor (context).GetIssues ();
		}

		class GatherVisitor : GatherVisitorBase
		{
			public GatherVisitor (BaseRefactoringContext ctx)
				: base (ctx)
			{
			}

			public override void VisitCastExpression (CastExpression castExpression)
			{
				base.VisitCastExpression (castExpression);

				CheckTypeCast (castExpression, castExpression.Expression, castExpression.StartLocation, 
					castExpression.Expression.StartLocation);
			}

			public override void VisitAsExpression (AsExpression asExpression)
			{
				base.VisitAsExpression (asExpression);

				CheckTypeCast (asExpression, asExpression.Expression, asExpression.Expression.EndLocation,
					asExpression.EndLocation);
			}

			IType GetExpectedType (Expression typeCastNode)
			{
				var memberRefExpr = typeCastNode.Parent as MemberReferenceExpression;
				if (memberRefExpr != null) {
					var invocationExpr = memberRefExpr.Parent as InvocationExpression;
					if (invocationExpr != null && invocationExpr.Target == memberRefExpr) {
						var invocationResolveResult = ctx.Resolve (invocationExpr) as InvocationResolveResult;
						if (invocationResolveResult != null)
							return invocationResolveResult.Member.DeclaringType;
					} else {
						var memberResolveResult = ctx.Resolve (memberRefExpr) as MemberResolveResult;
						if (memberResolveResult != null)
							return memberResolveResult.Member.DeclaringType;
					}
				}
				return ctx.GetExpectedType (typeCastNode);
			}

			void AddIssue (Expression typeCastNode, Expression expr, TextLocation start, TextLocation end)
			{
				AddIssue (start, end, ctx.TranslateString ("Remove redundant type cast"),
					script => script.Replace (typeCastNode, expr.Clone ()));
			}

			void CheckTypeCast (Expression typeCastNode, Expression expr, TextLocation castStart, TextLocation castEnd)
			{
				while (typeCastNode.Parent != null && typeCastNode.Parent is ParenthesizedExpression)
					typeCastNode = (Expression)typeCastNode.Parent;

				var expectedType = GetExpectedType (typeCastNode);
				var exprType = ctx.Resolve (expr).Type;
				if (exprType.GetAllBaseTypes ().Any (t => t.Equals(expectedType)))
					AddIssue (typeCastNode, expr, castStart, castEnd);
			}
		}
	}
}
