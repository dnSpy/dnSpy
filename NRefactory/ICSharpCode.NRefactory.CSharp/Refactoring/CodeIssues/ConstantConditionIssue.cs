// 
// ConstantConditionIssue.cs
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

namespace ICSharpCode.NRefactory.CSharp.Refactoring
{
	[IssueDescription ("Condition is always 'true' or always 'false'",
					   Description = "Condition is always 'true' or always 'false'.",
					   Category = IssueCategories.CodeQualityIssues,
					   Severity = Severity.Warning,
					   IssueMarker = IssueMarker.Underline)]
	public class ConstantConditionIssue : ICodeIssueProvider
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

			public override void VisitConditionalExpression (ConditionalExpression conditionalExpression)
			{
				base.VisitConditionalExpression (conditionalExpression);

				CheckCondition (conditionalExpression.Condition);
			}

			public override void VisitIfElseStatement (IfElseStatement ifElseStatement)
			{
				base.VisitIfElseStatement (ifElseStatement);

				CheckCondition (ifElseStatement.Condition);
			}

			public override void VisitWhileStatement (WhileStatement whileStatement)
			{
				base.VisitWhileStatement (whileStatement);

				CheckCondition (whileStatement.Condition);
			}

			public override void VisitDoWhileStatement (DoWhileStatement doWhileStatement)
			{
				base.VisitDoWhileStatement (doWhileStatement);

				CheckCondition (doWhileStatement.Condition);
			}

			public override void VisitForStatement (ForStatement forStatement)
			{
				base.VisitForStatement (forStatement);

				CheckCondition (forStatement.Condition);
			}

			void CheckCondition (Expression condition)
			{
				if (condition is PrimitiveExpression)
					return;

				var resolveResult = ctx.Resolve (condition);
				if (!(resolveResult.IsCompileTimeConstant && resolveResult.ConstantValue is bool))
					return;

				var value = (bool)resolveResult.ConstantValue;
				var conditionalExpr = condition.Parent as ConditionalExpression;
				var ifElseStatement = condition.Parent as IfElseStatement;
				var valueStr = value.ToString ().ToLower ();

				CodeAction action;
				if (conditionalExpr != null) {
					var replaceExpr = value ? conditionalExpr.TrueExpression : conditionalExpr.FalseExpression;
					action = new CodeAction (
						string.Format (ctx.TranslateString ("Replace '?:' with '{0}' branch"), valueStr),
						script => script.Replace (conditionalExpr, replaceExpr.Clone ()));
				} else if (ifElseStatement != null) {
					action = new CodeAction (
						string.Format (ctx.TranslateString ("Replace 'if' with '{0}' branch"), valueStr),
						script => {
							var statement = value ? ifElseStatement.TrueStatement : ifElseStatement.FalseStatement;
							var blockStatement = statement as BlockStatement;
							if (statement.IsNull || (blockStatement != null && blockStatement.Statements.Count == 0)) {
								script.Remove (ifElseStatement);
								return;
							}

							TextLocation start, end;
							if (blockStatement != null) {
								start = blockStatement.Statements.FirstOrNullObject ().StartLocation;
								end = blockStatement.Statements.LastOrNullObject ().EndLocation;
							} else {
								start = statement.StartLocation;
								end = statement.EndLocation;
							}
							RemoveText (script, ifElseStatement.StartLocation, start);
							RemoveText (script, end, ifElseStatement.EndLocation);
							script.FormatText (ifElseStatement.Parent);
						});
				} else {
					action = new CodeAction (
						string.Format (ctx.TranslateString ("Replace expression with '{0}'"), valueStr),
						script => script.Replace (condition, new PrimitiveExpression (value)));
				}
				AddIssue (condition, string.Format (ctx.TranslateString ("Condition is always '{0}'"), valueStr), 
					new [] { action });
			}

			void RemoveText (Script script, TextLocation start, TextLocation end)
			{
				var startOffset = script.GetCurrentOffset (start);
				var endOffset = script.GetCurrentOffset (end);
				if (startOffset < endOffset)
					script.RemoveText (startOffset, endOffset - startOffset);
			}
		}
	}
}
