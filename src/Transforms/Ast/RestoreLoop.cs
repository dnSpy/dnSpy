using System;

using Ast = ICSharpCode.NRefactory.Ast;
using ICSharpCode.NRefactory.Ast;
using ICSharpCode.NRefactory.Visitors;

namespace Decompiler.Transforms.Ast
{
	public class RestoreLoop: AbstractAstTransformer
	{
		public override object VisitForStatement(ForStatement forStatement, object data)
		{
			base.VisitForStatement(forStatement, data);
			
			// Restore loop initializer
			if (forStatement.Initializers.Count == 0) {
				int myIndex = forStatement.Parent.Children.IndexOf(forStatement);
				if (myIndex - 1 >= 0) {
					LocalVariableDeclaration varDeclr = forStatement.Parent.Children[myIndex - 1] as LocalVariableDeclaration;
					if (varDeclr != null) {
						forStatement.Parent.Children[myIndex - 1] = Statement.Null;
						forStatement.Initializers.Add(varDeclr);
					}
				}
			}
			
			// Restore loop condition
			if (forStatement.Condition.IsNull &&
				forStatement.EmbeddedStatement.Children.Count >= 3)
			{
				IfElseStatement  condition = forStatement.EmbeddedStatement.Children[0] as IfElseStatement;
				BreakStatement   breakStmt = forStatement.EmbeddedStatement.Children[1] as BreakStatement;
				MyLabelStatement label     = forStatement.EmbeddedStatement.Children[2] as MyLabelStatement;
				if (condition != null &&
				    breakStmt != null &&
				    label != null &&
				    condition.TrueStatement.Count == 1)
				{
					MyGotoStatement gotoStmt = condition.TrueStatement[0] as MyGotoStatement;
					if (gotoStmt != null && gotoStmt.NodeLabel == label.NodeLabel) {
						forStatement.EmbeddedStatement.Children.Remove(condition);
						forStatement.EmbeddedStatement.Children.Remove(breakStmt);
						gotoStmt.NodeLabel.ReferenceCount--;
						forStatement.Condition = condition.Condition;
					}
				}
			}
			
			// Restore loop iterator
			if (forStatement.EmbeddedStatement.Children.Count > 0 &&
			    forStatement.Iterator.Count == 0)
			{
				ExpressionStatement lastStmt = forStatement.EmbeddedStatement.Children[forStatement.EmbeddedStatement.Children.Count - 1] as ExpressionStatement;
				if (lastStmt != null) {
					AssignmentExpression assign = lastStmt.Expression as AssignmentExpression;
					if (assign != null) {
						forStatement.EmbeddedStatement.Children.Remove(lastStmt);
						forStatement.Iterator.Add(lastStmt);
					}
				}
			}
			
			return null;
		}
	}
}
