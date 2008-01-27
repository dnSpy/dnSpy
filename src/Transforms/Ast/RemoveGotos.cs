using System;

using ICSharpCode.NRefactory.Ast;
using ICSharpCode.NRefactory.Visitors;

namespace Decompiler.Transforms.Ast
{
	public class RemoveGotos: AbstractAstTransformer
	{
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
