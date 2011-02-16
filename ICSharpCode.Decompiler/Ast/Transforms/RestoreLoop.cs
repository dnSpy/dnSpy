using System;
using System.Linq;
using ICSharpCode.NRefactory.CSharp;

namespace Decompiler.Transforms.Ast
{
	public class RestoreLoop: DepthFirstAstVisitor<object, object>
	{
		public override object VisitForStatement(ForStatement forStatement, object data)
		{
			base.VisitForStatement(forStatement, data);
			
			// Restore loop initializer
			if (!forStatement.Initializers.Any()) {
				VariableDeclarationStatement varDeclr = forStatement.PrevSibling as VariableDeclarationStatement;
				if (varDeclr != null) {
					varDeclr.ReplaceWith(Statement.Null);
					forStatement.Initializers = new Statement[] { varDeclr };
				}
			}
			
			// Restore loop condition
			if (forStatement.Condition.IsNull &&
			    forStatement.EmbeddedStatement.Children.Count() >= 3)
			{
				IfElseStatement  condition = forStatement.EmbeddedStatement.Children.First() as IfElseStatement;
				BreakStatement   breakStmt = forStatement.EmbeddedStatement.Children.Skip(1).First() as BreakStatement;
				LabelStatement   label     = forStatement.EmbeddedStatement.Children.Skip(2).First() as LabelStatement;
				if (condition != null && breakStmt != null && label != null &&
				    condition.TrueStatement.Children.Count() == 1)
				{
					GotoStatement gotoStmt = condition.TrueStatement.FirstChild as GotoStatement;
					if (gotoStmt != null && gotoStmt.Label == label.Label) {
						condition.Remove();
						breakStmt.Remove();
						forStatement.Condition = condition.Condition;
					}
				}
			}
			
			// Restore loop condition (version 2)
			if (forStatement.Condition.IsNull) {
				IfElseStatement condition = forStatement.EmbeddedStatement.FirstChild as IfElseStatement;
				if (condition != null &&
				    condition.TrueStatement.Children.Any() &&
				    condition.TrueStatement.FirstChild is BlockStatement &&
				    condition.TrueStatement.Children.Count() == 1 &&
				    condition.TrueStatement.FirstChild.FirstChild is BreakStatement &&
				    condition.FalseStatement.Children.Any() &&
				    condition.FalseStatement.FirstChild is BlockStatement &&
				    condition.FalseStatement.Children.Count() == 0)
				{
					condition.Remove();
					forStatement.Condition = new UnaryOperatorExpression() { Expression = condition.Condition, Operator = UnaryOperatorType.Not };
				}
			}
			
			// Restore loop iterator
			if (forStatement.EmbeddedStatement.Children.Any() &&
			    !forStatement.Iterators.Any())
			{
				ExpressionStatement lastStmt = forStatement.EmbeddedStatement.LastChild as ExpressionStatement;
				if (lastStmt != null &&
				    (lastStmt.Expression is AssignmentExpression || lastStmt.Expression is UnaryOperatorExpression)) {
					lastStmt.Remove();
					forStatement.Iterators = new Statement[] { lastStmt };
				}
			}
			
			return null;
		}
	}
}
