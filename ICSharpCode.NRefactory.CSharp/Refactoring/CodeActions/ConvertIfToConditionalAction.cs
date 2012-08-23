// 
// ConvertIfToConditionalAction.cs
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

using System;
using System.Linq;
using ICSharpCode.NRefactory.PatternMatching;

namespace ICSharpCode.NRefactory.CSharp.Refactoring.CodeActions
{
	[ContextAction ("Convert 'if' to '?:'", Description = "Convert 'if' statement to '?:' operator.")]
	public class ConvertIfToConditionalAction : SpecializedCodeAction <IfElseStatement>
	{
		static readonly InsertParenthesesVisitor insertParenthesesVisitor = new InsertParenthesesVisitor ();
		protected override CodeAction GetAction (RefactoringContext context, IfElseStatement ifElseStatement)
		{
			if (!ifElseStatement.IfToken.Contains (context.Location))
				return null;

			var convertAssignment = GetConvertAssignmentAction (context, ifElseStatement);
			if (convertAssignment != null)
				return convertAssignment;

			var convertReturn = GetConvertReturnAction (context, ifElseStatement);
			return convertReturn;
		}

		CodeAction GetConvertAssignmentAction (RefactoringContext context, IfElseStatement ifElseStatement)
		{
			Func<Statement, AssignmentExpression> extractAssignment =
				node =>
				{
					var exprStatement = node as ExpressionStatement;
					if (exprStatement != null)
						return exprStatement.Expression as AssignmentExpression;
					return	null;
				};
			return GetAction (context, ifElseStatement, extractAssignment,
				(assignment1, assignment2) =>
				{
					if (assignment1.Operator != assignment2.Operator || !assignment1.Left.Match (assignment2.Left).Success)
						return null;
					var conditionalExpr = new ConditionalExpression (ifElseStatement.Condition.Clone (),
																	 assignment1.Right.Clone (),
																	 assignment2.Right.Clone ());
					conditionalExpr.AcceptVisitor (insertParenthesesVisitor);

					var assignment = new AssignmentExpression (assignment1.Left.Clone (), assignment1.Operator,
															   conditionalExpr);
					return new ExpressionStatement (assignment);
				});
		}

		CodeAction GetConvertReturnAction (RefactoringContext context, IfElseStatement ifElseStatement)
		{
			Func<Statement, ReturnStatement> extractReturn = node => node as ReturnStatement;
			return GetAction (context, ifElseStatement, extractReturn,
				(return1, return2) =>
				{
					var conditionalExpr = new ConditionalExpression (ifElseStatement.Condition.Clone (),
																	 return1.Expression.Clone (),
																	 return2.Expression.Clone ());
					conditionalExpr.AcceptVisitor (insertParenthesesVisitor);
					return new ReturnStatement (conditionalExpr);
				},
				true);
		}

		CodeAction GetAction<T> (RefactoringContext context, IfElseStatement ifElseStatement,
								 Func<Statement, T> extractor, Func<T, T, Statement> getReplaceStatement,
								 bool findImplicitFalseStatement = false)
			where T : AstNode
		{

			var node1 = GetNode (ifElseStatement.TrueStatement, extractor);
			if (node1 == null)
				return null;

			var falseStatement = ifElseStatement.FalseStatement;
			// try next statement if there is no FalseStatement
			if (falseStatement.IsNull && findImplicitFalseStatement)
				falseStatement = ifElseStatement.NextSibling as Statement;

			var node2 = GetNode (falseStatement, extractor);
			if (node2 == null)
				return null;

			var replacement = getReplaceStatement (node1, node2);
			if (replacement == null)
				return null;

			return new CodeAction (context.TranslateString ("Convert 'if' to '?:'"),
				script =>
				{
					script.Replace (ifElseStatement, replacement);
					// remove implicit false statement
					if (falseStatement != ifElseStatement.FalseStatement)
						script.Remove (falseStatement);
				});
		}

		static T GetNode<T> (Statement node, Func<Statement, T> extract)
			where T : AstNode
		{
			var result = extract (node);
			if (result != null)
				return result;

			var blockStatement = node as BlockStatement;
			if (blockStatement != null && blockStatement.Statements.Count == 1)
				return GetNode (blockStatement.Statements.First (), extract);

			return null;
		}
	}
}
