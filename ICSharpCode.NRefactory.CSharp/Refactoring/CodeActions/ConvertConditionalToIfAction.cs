// 
// ConvertConditionalToIfAction.cs
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
using System.Collections.Generic;
using System.Linq;

namespace ICSharpCode.NRefactory.CSharp.Refactoring.CodeActions
{
	[ContextAction ("Convert '?:' to 'if'", Description = "Convert '?:' operator to 'if' statement.")]
	public class ConvertConditionalToIfAction : ICodeActionProvider
	{
		public IEnumerable<CodeAction> GetActions (RefactoringContext context)
		{
			// return
			var returnStatement = context.GetNode<ReturnStatement> ();
			if (returnStatement != null) {
				var action = GetAction (context, returnStatement, returnStatement.Expression,
					expr => new Statement[]
					{
						new IfElseStatement (expr.Condition.Clone (), 
											 new ReturnStatement (expr.TrueExpression.Clone ()),
											 new ReturnStatement (expr.FalseExpression.Clone ()))
					});
				if (action != null)
					yield return action;
				yield break;
			}

			// assignment
			var assignment = context.GetNode<AssignmentExpression> ();
			if (assignment != null) {
				var statement = assignment.Parent as ExpressionStatement;
				if (statement == null)
					yield break;

				// statement is in for initializer/iterator
				if (statement.Parent is ForStatement &&
					statement != ((ForStatement)statement.Parent).EmbeddedStatement)
					yield break;

				// statement is in using resource-acquisition
				if (statement.Parent is UsingStatement &&
					statement != ((UsingStatement)statement.Parent).EmbeddedStatement)
					yield break;

				var action = GetAction (context, statement, assignment.Right,
					expr => new Statement [] { ConvertAssignment (assignment.Left, assignment.Operator, expr) });
				if (action != null)
					yield return action;

				yield break;
			}

			// variable initializer
			var initializer = context.GetNode<VariableInitializer> ();
			if (initializer != null && initializer.Parent is VariableDeclarationStatement &&
				initializer.Parent.Parent is BlockStatement) {

				var variableDecl = (VariableDeclarationStatement)initializer.Parent;
				var newVariableDecl = (VariableDeclarationStatement)variableDecl.Clone ();
				foreach (var variable in newVariableDecl.Variables) {
					if (variable.Name == initializer.Name)
						variable.Initializer = Expression.Null;
				}

				var action = GetAction (context, variableDecl, initializer.Initializer,
					expr => new Statement []
					{
						newVariableDecl,
						ConvertAssignment (new IdentifierExpression (initializer.Name), AssignmentOperatorType.Assign,
										   expr)
					});
				if (action != null)
					yield return action;
			}
		}

		CodeAction GetAction (RefactoringContext context, Statement originalStatement, Expression conditionalCandidate, 
							  Func<ConditionalExpression, IEnumerable<Statement>> getReplacement)
		{
			var conditionalExpr = GetConditionalExpression (conditionalCandidate);
			if (conditionalExpr == null || 
				!(conditionalExpr.QuestionMarkToken.Contains(context.Location) ||
				  conditionalExpr.ColonToken.Contains(context.Location)))
				return null;

			return new CodeAction (context.TranslateString ("Convert '?:' to 'if'"),
				script =>
				{
					foreach (var node in getReplacement (conditionalExpr))
						script.InsertBefore (originalStatement, node);
					script.Remove (originalStatement);
				});
		}

		static ConditionalExpression GetConditionalExpression (Expression expr)
		{
			while (expr != null) {
				if (expr is ConditionalExpression)
					return (ConditionalExpression)expr;

				var parenthesizedExpr = expr as ParenthesizedExpression;
				if (parenthesizedExpr == null)
					break;
				expr = parenthesizedExpr.Expression;
			}
			return null;
		}

		static IfElseStatement ConvertAssignment (Expression target, AssignmentOperatorType op,
												  ConditionalExpression conditionalExpr)
		{
			var trueAssignment = new AssignmentExpression (target.Clone (), op,
														   conditionalExpr.TrueExpression.Clone ());
			var falseAssignment = new AssignmentExpression (target.Clone (), op,
															conditionalExpr.FalseExpression.Clone ());
			return new IfElseStatement (conditionalExpr.Condition.Clone (),
										new ExpressionStatement (trueAssignment),
										new ExpressionStatement (falseAssignment));
		}
	}
}
