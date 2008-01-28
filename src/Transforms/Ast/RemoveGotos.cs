using System;

using ICSharpCode.NRefactory.Ast;
using ICSharpCode.NRefactory.Visitors;

namespace Decompiler.Transforms.Ast
{
	public class RemoveGotos: AbstractAstTransformer
	{
		public override object VisitBlockStatement(BlockStatement blockStatement, object data)
		{
			base.VisitBlockStatement(blockStatement, data);
			
			// Remove redundant jump at the end of block
			INode lastStmt = blockStatement.Children.Last;
			// End of while loop
			if (lastStmt is ContinueStatement && 
			    blockStatement.Parent is DoLoopStatement)
			{
				lastStmt.Remove();
				return null;
			}
			// End of for loop
			if (lastStmt is ContinueStatement && 
			    blockStatement.Parent is ForStatement)
			{
				lastStmt.Remove();
				return null;
			}
			// End of method
			if (lastStmt is ReturnStatement && 
			    blockStatement.Parent is MethodDeclaration &&
			    ((ReturnStatement)lastStmt).Expression.IsNull)
			{
				lastStmt.Remove();
				return null;
			}
			// End of if body
			if (lastStmt is GotoStatement &&
			    blockStatement.Parent is IfElseStatement)
			{
				MyLabelStatement nextNodeAsLabel = blockStatement.Parent.Next() as MyLabelStatement;
				if (nextNodeAsLabel != null) {
					if (nextNodeAsLabel.NodeLabel == ((MyGotoStatement)lastStmt).NodeLabel) {
						((MyGotoStatement)lastStmt).NodeLabel.ReferenceCount--;
						lastStmt.Remove();
						return null;
					}
				}
			}
			
			return null;
		}
		
		public override object VisitGotoStatement(GotoStatement gotoStatement, object data)
		{
			MyGotoStatement myGoto = (MyGotoStatement)gotoStatement;
			MyLabelStatement followingLabel = myGoto.Next() as MyLabelStatement;
			if (followingLabel != null && followingLabel.NodeLabel == myGoto.NodeLabel) {
				myGoto.NodeLabel.ReferenceCount--;
				RemoveCurrentNode();
			}
			return null;
		}
	}
}
