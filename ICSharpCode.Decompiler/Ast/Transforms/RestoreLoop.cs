using System;
using System.Linq;
using ICSharpCode.NRefactory.Ast;
using ICSharpCode.NRefactory.Visitors;
using Ast = ICSharpCode.NRefactory.Ast;

namespace Decompiler.Transforms.Ast
{
	public class RestoreLoop: AbstractAstTransformer
	{
		public override object VisitForStatement(ForStatement forStatement, object data)
		{
			base.VisitForStatement(forStatement, data);
			
			// Restore loop initializer
			if (forStatement.Initializers.Count == 0) {
				LocalVariableDeclaration varDeclr = forStatement.Previous() as LocalVariableDeclaration;
				if (varDeclr != null) {
					varDeclr.ReplaceWith(Statement.Null);
					forStatement.Initializers.Add(varDeclr);
				}
			}
			
			// Restore loop condition
			if (forStatement.Condition.IsNull &&
				forStatement.EmbeddedStatement.Children.Count >= 3)
			{
				IfElseStatement  condition = forStatement.EmbeddedStatement.Children[0] as IfElseStatement;
				BreakStatement   breakStmt = forStatement.EmbeddedStatement.Children[1] as BreakStatement;
				LabelStatement   label     = forStatement.EmbeddedStatement.Children[2] as LabelStatement;
				if (condition != null && breakStmt != null && label != null &&
				    condition.TrueStatement.Count == 1)
				{
					GotoStatement gotoStmt = condition.TrueStatement[0] as GotoStatement;
					if (gotoStmt != null && gotoStmt.Label == label.Label) {
						condition.Remove();
						breakStmt.Remove();
						forStatement.Condition = condition.Condition;
					}
				}
			}
			
			// Restore loop condition (version 2)
			if (forStatement.Condition.IsNull) {
				IfElseStatement condition = forStatement.EmbeddedStatement.Children.First() as IfElseStatement;
				if (condition != null &&
				    condition.TrueStatement.Count == 1 &&
				    condition.TrueStatement[0] is BlockStatement &&
				    condition.TrueStatement[0].Children.Count == 1 &&
				    condition.TrueStatement[0].Children.First() is BreakStatement &&
				    condition.FalseStatement.Count == 1 &&
				    condition.FalseStatement[0] is BlockStatement &&
				    condition.FalseStatement[0].Children.Count == 0)
				{
					condition.Remove();
					forStatement.Condition = new UnaryOperatorExpression(condition.Condition, UnaryOperatorType.Not);
				}
			}
			
			// Restore loop iterator
			if (forStatement.EmbeddedStatement.Children.Count > 0 &&
			    forStatement.Iterator.Count == 0)
			{
				ExpressionStatement lastStmt = forStatement.EmbeddedStatement.Children.Last() as ExpressionStatement;
				if (lastStmt != null &&
				    (lastStmt.Expression is AssignmentExpression || lastStmt.Expression is UnaryOperatorExpression)) {
					lastStmt.Remove();
					forStatement.Iterator.Add(lastStmt);
				}
			}
			
			return null;
		}
	}
}
