// 
// ConvertLambdaBodyExpressionToStatementAction.cs
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
	[ContextAction ("Converts expression of lambda body to statement",
					Description = "Converts expression of lambda body to statement")]
	public class ConvertLambdaBodyExpressionToStatementAction : SpecializedCodeAction<LambdaExpression>
	{

		protected override CodeAction GetAction (RefactoringContext context, LambdaExpression node)
		{
			if (!node.ArrowToken.Contains (context.Location))
				return null;

			var bodyExpr = node.Body as Expression;
			if (bodyExpr == null)
				return null;
			return new CodeAction (context.TranslateString ("Convert to lambda statement"),
				script =>
				{
					var body = new BlockStatement ();
					if (RequireReturnStatement (context, node)) {
						body.Add (new ReturnStatement (bodyExpr.Clone ()));
					} else {
						body.Add (new ExpressionStatement (bodyExpr.Clone ()));
					}
					script.Replace (bodyExpr, body);
				});
		}

		static bool RequireReturnStatement (RefactoringContext context, LambdaExpression lambda)
		{
			var type = LambdaHelper.GetLambdaReturnType (context, lambda);
			return type != null && type.ReflectionName != "System.Void";
		}
	}
}
