// 
// CompareBooleanWithTrueOrFalseIssue.cs
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
using ICSharpCode.NRefactory.PatternMatching;
using ICSharpCode.NRefactory.TypeSystem;

namespace ICSharpCode.NRefactory.CSharp.Refactoring
{
	[IssueDescription ("Comparison of boolean with 'true' or 'false'",
					   Description = "Comparison of a boolean value with 'true' or 'false' constant.",
					   Category = IssueCategories.Redundancies,
					   Severity = Severity.Warning,
					   IssueMarker = IssueMarker.Underline)]
	public class CompareBooleanWithTrueOrFalseIssue : ICodeIssueProvider
	{
		public IEnumerable<CodeIssue> GetIssues (BaseRefactoringContext context)
		{
			return new GatherVisitor (context).GetIssues ();
		}

		class GatherVisitor : GatherVisitorBase
		{
			static readonly Pattern pattern = new Choice {
				PatternHelper.CommutativeOperator(
					new NamedNode ("const", new Choice { new PrimitiveExpression(true), new PrimitiveExpression(false) }),
					BinaryOperatorType.Equality, new AnyNode("expr")),
				PatternHelper.CommutativeOperator(
					new NamedNode ("const", new Choice { new PrimitiveExpression(true), new PrimitiveExpression(false) }),
					BinaryOperatorType.InEquality, new AnyNode("expr")),
			};

			static InsertParenthesesVisitor insertParenthesesVisitor = new InsertParenthesesVisitor ();

			public GatherVisitor (BaseRefactoringContext ctx)
				: base (ctx)
			{
			}

			public override void VisitBinaryOperatorExpression (BinaryOperatorExpression binaryOperatorExpression)
			{
				base.VisitBinaryOperatorExpression (binaryOperatorExpression);

				var match = pattern.Match (binaryOperatorExpression);
				if (!match.Success)
					return;
				var expr = match.Get<Expression> ("expr").First ();
				// check if expr is of boolean type
				var exprType = ctx.Resolve (expr).Type.GetDefinition ();
				if (exprType == null || exprType.KnownTypeCode != KnownTypeCode.Boolean)
					return;

				AddIssue (binaryOperatorExpression, ctx.TranslateString ("Simplify boolean comparison"), scrpit => {
					var boolConstant = (bool)match.Get<PrimitiveExpression> ("const").First ().Value;
					if ((binaryOperatorExpression.Operator == BinaryOperatorType.InEquality && boolConstant) ||
						(binaryOperatorExpression.Operator == BinaryOperatorType.Equality && !boolConstant)) {
						expr = new UnaryOperatorExpression (UnaryOperatorType.Not, expr.Clone());
						expr.AcceptVisitor (insertParenthesesVisitor);
					}
					scrpit.Replace (binaryOperatorExpression, expr);
				});
			}
		}
	}
}
