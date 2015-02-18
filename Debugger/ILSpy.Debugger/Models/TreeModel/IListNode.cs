// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)
using ICSharpCode.NRefactory.CSharp;

namespace ICSharpCode.ILSpy.Debugger.Models.TreeModel
{
	internal class IListNode : TreeNode
	{
		Expression targetObject;
		int count;
		
		public IListNode(Expression targetObject)
		{
			this.targetObject = targetObject;
			
			this.Name = "IList";
			this.count = Utils.GetIListCount(this.targetObject);
			this.ChildNodes = Utils.LazyGetItemsOfIList(this.targetObject);
		}
		
		public override bool HasChildNodes {
			get { return this.count > 0; }
		}
	}
}
