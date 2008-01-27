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
			INode lastStmt = blockStatement.Children[blockStatement.Children.Count - 1];
			// End of loop
			if (lastStmt is ContinueStatement && 
			    blockStatement.Parent is DoLoopStatement)
			{
				blockStatement.Children.Remove(lastStmt);
				return null;
			}
			// End of method
			if (lastStmt is ReturnStatement && 
			    blockStatement.Parent is MethodDeclaration &&
			    ((ReturnStatement)lastStmt).Expression.IsNull)
			{
				blockStatement.Children.Remove(lastStmt);
				return null;
			}
			// End of if body
			if (lastStmt is GotoStatement &&
			    blockStatement.Parent is IfElseStatement)
			{
				INode ifParent = blockStatement.Parent.Parent;
				int ifIndex = ifParent.Children.IndexOf(blockStatement.Parent);
				if (ifIndex + 1 < ifParent.Children.Count) {
					MyLabelStatement nextNodeAsLabel = ifParent.Children[ifIndex + 1] as MyLabelStatement;
					if (nextNodeAsLabel != null) {
						if (nextNodeAsLabel.NodeLabel == ((MyGotoStatement)lastStmt).NodeLabel) {
							((MyGotoStatement)lastStmt).NodeLabel.ReferenceCount--;
							blockStatement.Children.Remove(lastStmt);
						}
					}
				}
				
				return null;
			}
			
			return null;
		}
		
		public override object VisitGotoStatement(GotoStatement gotoStatement, object data)
		{
			MyGotoStatement myGoto = (MyGotoStatement)gotoStatement;
			if (gotoStatement.Parent == null) return null;
			int index = gotoStatement.Parent.Children.IndexOf(gotoStatement);
			if (index + 1 < gotoStatement.Parent.Children.Count) {
				INode nextStmt = gotoStatement.Parent.Children[index + 1];
				MyLabelStatement myLabel = nextStmt as MyLabelStatement;
				if (myLabel != null && myLabel.NodeLabel == myGoto.NodeLabel) {
					myGoto.NodeLabel.ReferenceCount--;
					RemoveCurrentNode();
				}
			}
			return null;
		}
	}
}
