// 
// NegateRelationalExpressionAction.cs
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

namespace ICSharpCode.NRefactory.CSharp.Refactoring
{
	[ContextAction ("Negate an relational expression", Description = "Negate an relational expression.")]
	public class NegateRelationalExpressionAction : SpecializedCodeAction<BinaryOperatorExpression>
	{

		protected override CodeAction GetAction (RefactoringContext context, BinaryOperatorExpression node)
		{
			var newOp = CSharpUtil.NegateRelationalOperator (node.Operator);
			if (newOp != BinaryOperatorType.Any && node.OperatorToken.Contains (context.Location)) {
				var operatorToken = BinaryOperatorExpression.GetOperatorRole (node.Operator).Token;
				return new CodeAction (string.Format (context.TranslateString ("Negate '{0}'"), operatorToken),
					script => {
						var expr = new BinaryOperatorExpression (node.Left.Clone (), newOp, node.Right.Clone ());
						script.Replace (node, expr);
					});
			}
			return null;
		}

	}
}
