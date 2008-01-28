// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Daniel Grunwald" email="daniel@danielgrunwald.de"/>
//     <version>$Revision$</version>
// </file>

using System;
using ICSharpCode.NRefactory.Ast;

namespace ICSharpCode.NRefactory.Visitors
{
	/// <summary>
	/// Base class for the conversion visitors.
	/// </summary>
	public class ConvertVisitorBase : AbstractAstTransformer
	{
		// inserting before current position is not allowed in a Transformer
		// but inserting after it is possible
		protected void InsertAfterSibling(INode sibling, INode newNode)
		{
			if (sibling == null || sibling.Parent == null) return;
			int index = sibling.Parent.Children.IndexOf(sibling);
			sibling.Parent.Children.Insert(index + 1, newNode);
			newNode.Parent = sibling.Parent;
		}
	}
}
