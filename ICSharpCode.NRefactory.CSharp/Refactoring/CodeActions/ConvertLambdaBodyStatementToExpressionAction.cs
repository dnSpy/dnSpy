// 
// ConvertLambdaBodyStatementToExpressionAction.cs
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
	[ContextAction ("Converts statement of lambda body to expression",
					Description = "Converts statement of lambda body to expression")]
	public class ConvertLambdaBodyStatementToExpressionAction : SpecializedCodeAction<LambdaExpression>
	{
		protected override CodeAction GetAction (RefactoringContext context, LambdaExpression node)
		{
			if (!node.ArrowToken.Contains (context.Location))
				return null;

			var blockStatement = node.Body as BlockStatement;
			if (blockStatement == null || blockStatement.Statements.Count > 1)
				return null;

			Expression expr;
			var returnStatement = blockStatement.Statements.FirstOrNullObject () as ReturnStatement;
			if (returnStatement != null) {
				expr = returnStatement.Expression;
			} else {
				var exprStatement = blockStatement.Statements.FirstOrNullObject () as ExpressionStatement;
				if (exprStatement == null)
					return null;
				expr = exprStatement.Expression;
			}
			
			return new CodeAction (context.TranslateString ("Convert to lambda expression"),
				script => script.Replace (blockStatement, expr.Clone ()));
		}
	}
}
