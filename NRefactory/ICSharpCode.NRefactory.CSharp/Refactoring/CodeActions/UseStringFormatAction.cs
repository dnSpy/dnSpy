// 
// UseStringFormatAction.cs
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
using System.Text;
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.PatternMatching;

namespace ICSharpCode.NRefactory.CSharp.Refactoring
{
	[ContextAction ("Use string.Format()",
					Description = "Convert concatenation of strings and objects to string.Format()")]
	public class UseStringFormatAction : ICodeActionProvider
	{

		public IEnumerable<CodeAction> GetActions (RefactoringContext context)
		{
			// NOTE: @, multiple occurance

			var node = context.GetNode ();
			while (node != null && !IsStringConcatenation(context, node as BinaryOperatorExpression))
				node = node.Parent;

			if (node == null)
				yield break;

			var expr = (BinaryOperatorExpression)node;
			var parent = expr.Parent as BinaryOperatorExpression;
			while (parent != null && parent.Operator == BinaryOperatorType.Add) {
				expr = parent;
				parent = expr.Parent as BinaryOperatorExpression;
			}

			yield return new CodeAction (context.TranslateString ("Use string.Format()"),
				script =>
				{
					var format = new StringBuilder ();
					var stringType = new PrimitiveType ("string");
					var formatInvocation = new InvocationExpression (
						new MemberReferenceExpression (new TypeReferenceExpression (stringType), "Format"));
					var formatLiteral = new PrimitiveExpression ("");
					var counter = 0;
					var verbatim = false;
					var arguments = new List<Expression> ();

					format.Append ('"');
					formatInvocation.Arguments.Add (formatLiteral);
					foreach (var item in GetConcatItems (context, expr)) {
						if (IsStringLiteral (item)) {
							var stringLiteral = (PrimitiveExpression)item;

							if (stringLiteral.LiteralValue [0] == '@') {
								verbatim = true;
								format.Append (stringLiteral.LiteralValue, 2, stringLiteral.LiteralValue.Length - 3);
							} else {
								format.Append (stringLiteral.LiteralValue, 1, stringLiteral.LiteralValue.Length - 2);
							}
						} else {
							var index = IndexOf (arguments, item);
							if (index == -1) {
								// new item
								formatInvocation.Arguments.Add (item.Clone ());
								arguments.Add (item);
								format.Append ("{" + counter++ + "}");
							} else {
								// existing item
								format.Append ("{" + index + "}");
							}
						}
					}
					format.Append ('"');
					if (verbatim)
						format.Insert (0, '@');
					formatLiteral.LiteralValue = format.ToString ();
					script.Replace (expr, formatInvocation);
				});
		}

		static int IndexOf	(IList<Expression> arguments, Expression item)
		{
			for (int i = 0; i < arguments.Count; i++) {
				if (item.Match (arguments [i]).Success)
					return i;
			}
			return -1;
		}

		static IEnumerable<Expression> GetConcatItems (RefactoringContext context, BinaryOperatorExpression expr)
		{
			var leftExpr = expr.Left as BinaryOperatorExpression;
			if (IsStringConcatenation(context, leftExpr)) {
				foreach (var item in GetConcatItems (context, leftExpr))
					yield return item;
			} else {
				yield return expr.Left;
			}

			var rightExpr = expr.Right as BinaryOperatorExpression;
			if (IsStringConcatenation(context, rightExpr)) {
				foreach (var item in GetConcatItems (context, rightExpr))
					yield return item;
			} else {
				yield return expr.Right;
			}
		}

		static bool IsStringConcatenation (RefactoringContext context, BinaryOperatorExpression expr)
		{
			if (expr == null || expr.Operator != BinaryOperatorType.Add)
				return false;
			var typeDef = context.Resolve (expr).Type.GetDefinition();
			return typeDef != null && typeDef.KnownTypeCode == KnownTypeCode.String;
		}

		static bool IsStringLiteral (AstNode node)
		{
			var expr = node as PrimitiveExpression;
			return expr != null && expr.Value is string;
		}
	}
}
