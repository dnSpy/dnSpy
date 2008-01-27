using System;

using Ast = ICSharpCode.NRefactory.Ast;
using ICSharpCode.NRefactory.Ast;
using ICSharpCode.NRefactory.Visitors;

namespace Decompiler.Transforms.Ast
{
	public class RemoveDeadLabels: AbstractAstTransformer
	{
		public override object VisitLabelStatement(LabelStatement labelStatement, object data)
		{
			MyLabelStatement myLabel = (MyLabelStatement)labelStatement;
			if (myLabel.NodeLabel.ReferenceCount == 0) {
				RemoveCurrentNode();
			}
			return null;
		}
	}
}
