// 
// MergeNestedIfAction.cs
//  
// Author:
//       Mansheng Yang <lightyang0@gmail.com>
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

using System.Linq;

namespace ICSharpCode.NRefactory.CSharp.Refactoring
{
	[ContextAction ("Merge nested 'if'", Description = "Merge two nested 'if' statements.")]
	public class MergeNestedIfAction : SpecializedCodeAction<IfElseStatement>
	{
		static readonly InsertParenthesesVisitor insertParenthesesVisitor = new InsertParenthesesVisitor ();

		protected override CodeAction GetAction (RefactoringContext context, IfElseStatement node)
		{
			if (!node.IfToken.Contains (context.Location))
				return null;

			IfElseStatement outerIfStatement;
			IfElseStatement innerIfStatement = GetInnerIfStatement (node);
			if (innerIfStatement != null) {
				if (!innerIfStatement.FalseStatement.IsNull)
					return null;
				outerIfStatement = node;
			} else {
				outerIfStatement = GetOuterIfStatement (node);
				if (outerIfStatement == null || !outerIfStatement.FalseStatement.IsNull)
					return null;
				innerIfStatement = node;
			}

			return new CodeAction (context.TranslateString ("Merge nested 'if's"),
				script =>
				{
					var mergedIfStatement = new IfElseStatement
					{
						Condition = new BinaryOperatorExpression (outerIfStatement.Condition.Clone (),
																  BinaryOperatorType.ConditionalAnd, 
																  innerIfStatement.Condition.Clone ()),
						TrueStatement = innerIfStatement.TrueStatement.Clone ()
					};
					mergedIfStatement.Condition.AcceptVisitor (insertParenthesesVisitor);
					script.Replace (outerIfStatement, mergedIfStatement);
				});
		}

		static IfElseStatement GetOuterIfStatement (IfElseStatement node)
		{
			var outerIf = node.Parent as IfElseStatement;
			if (outerIf != null)
				return outerIf;

			var blockStatement = node.Parent as BlockStatement;
			while (blockStatement != null && blockStatement.Statements.Count == 1) {
				outerIf = blockStatement.Parent as IfElseStatement;
				if (outerIf != null)
					return outerIf;
				blockStatement = blockStatement.Parent as BlockStatement;
			}

			return null;
		}

		static IfElseStatement GetInnerIfStatement (IfElseStatement node)
		{
			if (!node.FalseStatement.IsNull)
				return null;

			var innerIf = node.TrueStatement as IfElseStatement;
			if (innerIf != null)
				return innerIf;

			var blockStatement = node.TrueStatement as BlockStatement;
			while (blockStatement != null && blockStatement.Statements.Count == 1) {
				innerIf = blockStatement.Statements.First () as IfElseStatement;
				if (innerIf != null)
					return innerIf;
				blockStatement = blockStatement.Statements.First () as BlockStatement;
			}

			return null;
		}
	}
}
