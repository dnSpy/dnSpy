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
						forStatement.EmbeddedStatement.Children.RemoveAt(0);
						forStatement.EmbeddedStatement.Children.RemoveAt(0);
						gotoStmt.NodeLabel.ReferenceCount--;
						forStatement.Condition = condition.Condition;
					}
				}
			}
			return null;
		}
	}
}
