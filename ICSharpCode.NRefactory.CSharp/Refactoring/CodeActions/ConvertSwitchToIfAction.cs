// 
// ConvertSwitchToIfAction.cs
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
	[ContextAction ("Convert 'switch' to 'if'", Description = "Convert 'switch' statement to 'if' statement")]
	public class ConvertSwitchToIfAction : SpecializedCodeAction<SwitchStatement>
	{
		static readonly InsertParenthesesVisitor insertParenthesesVisitor = new InsertParenthesesVisitor ();

		protected override CodeAction GetAction (RefactoringContext context, SwitchStatement node)
		{
			if (!node.SwitchToken.Contains (context.Location))
				return null;

			// empty switch
			if (node.SwitchSections.Count == 0)
				return null;

			// switch with default only
			if (node.SwitchSections.First ().CaseLabels.Any (label => label.Expression.IsNull))
				return null;

			// check non-trailing breaks
			foreach (var switchSection in node.SwitchSections) {
				var lastStatement = switchSection.Statements.LastOrDefault ();
				var finder = new NonTrailingBreakFinder (lastStatement as BreakStatement);
				if (switchSection.AcceptVisitor (finder))
					return null;
			}
			
			return new CodeAction (context.TranslateString ("Convert 'switch' to 'if'"),
				script =>
				{
					IfElseStatement ifStatement = null;
					IfElseStatement currentStatement = null;
					foreach (var switchSection in node.SwitchSections) {
						var condition = CollectCondition (node.Expression, switchSection.CaseLabels);
						var bodyStatement = new BlockStatement ();
						var lastStatement = switchSection.Statements.LastOrDefault ();
						foreach (var statement in switchSection.Statements) {
							// skip trailing break
							if (statement == lastStatement && statement is BreakStatement)
								continue;
							bodyStatement.Add (statement.Clone ());
						}

						// default -> else
						if (condition == null) {
							currentStatement.FalseStatement = bodyStatement;
							break;
						}
						var elseIfStatement = new IfElseStatement (condition, bodyStatement);
						if (ifStatement == null)
							ifStatement = elseIfStatement;
						else
							currentStatement.FalseStatement = elseIfStatement;
						currentStatement = elseIfStatement;
					}
					script.Replace (node, ifStatement);
					script.FormatText (ifStatement);
				});
		}

		static Expression CollectCondition(Expression switchExpr, AstNodeCollection<CaseLabel> caseLabels)
		{
			// default
			if (caseLabels.Count == 0 || caseLabels.Any (label => label.Expression.IsNull))
				return null;

			var conditionList = caseLabels.Select (
				label => new BinaryOperatorExpression (switchExpr.Clone (), BinaryOperatorType.Equality, label.Expression.Clone ()))
				.ToArray ();

			// insert necessary parentheses
			foreach (var expr in conditionList)
				expr.AcceptVisitor (insertParenthesesVisitor);

			if (conditionList.Length == 1)
				return conditionList [0];

			// combine case labels into an conditional or expression
			BinaryOperatorExpression condition = null;
			BinaryOperatorExpression currentCondition = null;
			for (int i = 0; i < conditionList.Length - 1; i++) {
				var newCondition = new BinaryOperatorExpression
				{
					Operator = BinaryOperatorType.ConditionalOr,
					Left = conditionList[i]
				};
				if (currentCondition == null)
					condition = newCondition;
				else
					currentCondition.Right = newCondition;
				currentCondition = newCondition;
			}
			currentCondition.Right = conditionList [conditionList.Length - 1];

			return condition;
		}
		
		class NonTrailingBreakFinder : DepthFirstAstVisitor<bool>
		{
			BreakStatement trailingBreakStatement;

			public NonTrailingBreakFinder (BreakStatement trailingBreak)
			{
				trailingBreakStatement = trailingBreak;
			}

			protected override bool VisitChildren (AstNode node)
			{
				return node.Children.Any (child => child.AcceptVisitor (this));
			}

			public override bool VisitBreakStatement (BreakStatement breakStatement)
			{
				return breakStatement != trailingBreakStatement;
			}
		}
	}
}
