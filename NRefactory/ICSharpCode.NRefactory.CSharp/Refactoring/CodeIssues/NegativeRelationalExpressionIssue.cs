// 
// NegativeRelationalExpressionIssue.cs
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
using ICSharpCode.NRefactory.TypeSystem;

namespace ICSharpCode.NRefactory.CSharp.Refactoring
{
	[IssueDescription ("Simplify negative relational expression",
					   Description = "Simplify negative relational expression",
					   Category = IssueCategories.Improvements,
					   Severity = Severity.Suggestion,
					   IssueMarker = IssueMarker.Underline)]
	public class NegativeRelationalExpressionIssue : ICodeIssueProvider
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

			bool IsFloatingPoint (AstNode node)
			{
				var typeDef = ctx.Resolve (node).Type.GetDefinition ();
				return typeDef != null &&
					(typeDef.KnownTypeCode == KnownTypeCode.Single || typeDef.KnownTypeCode == KnownTypeCode.Double);
			}

			public override void VisitUnaryOperatorExpression (UnaryOperatorExpression unaryOperatorExpression)
			{
				base.VisitUnaryOperatorExpression (unaryOperatorExpression);

				if (unaryOperatorExpression.Operator != UnaryOperatorType.Not)
					return;

				var expr = unaryOperatorExpression.Expression;
				while (expr != null && expr is ParenthesizedExpression)
					expr = ((ParenthesizedExpression)expr).Expression;

				var binaryOperatorExpr = expr as BinaryOperatorExpression;
				if (binaryOperatorExpr == null)
					return;

				var negatedOp = CSharpUtil.NegateRelationalOperator(binaryOperatorExpr.Operator);
				if (negatedOp == BinaryOperatorType.Any)
					return;

				if (IsFloatingPoint (binaryOperatorExpr.Left) || IsFloatingPoint (binaryOperatorExpr.Right))
					return;

				AddIssue (unaryOperatorExpression, ctx.TranslateString ("Simplify negative relational expression"),
					script => script.Replace (unaryOperatorExpression,
						new BinaryOperatorExpression (binaryOperatorExpr.Left.Clone (), negatedOp,
					          	binaryOperatorExpr.Right.Clone ())));
			}
		}
	}
}
