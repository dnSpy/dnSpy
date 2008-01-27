using System;
using System.Collections.Generic;

using Ast = ICSharpCode.NRefactory.Ast;
using Decompiler.ControlFlow;

namespace ICSharpCode.NRefactory.Ast
{
	public class MyBlockStatement: BlockStatement
	{
		ChildrenCollection wrapper;
		
		public class ChildrenCollection: System.Collections.ObjectModel.Collection<INode>
		{
			MyBlockStatement myStmt;
			
			public void AddRange(IEnumerable<INode> items)
			{
				foreach(INode node in items) {
					Add(node);
				}
			}
			
			protected override void InsertItem(int index, INode item)
			{
				item.Parent = myStmt;
				base.InsertItem(index, item);
			}
			
			protected override void SetItem(int index, INode item)
			{
				item.Parent = myStmt;
				base.SetItem(index, item);
			}
			
			public ChildrenCollection(MyBlockStatement myStmt, IList<INode> nodes): base(nodes)
			{
				this.myStmt = myStmt;
			}
		}
		
		public new ChildrenCollection Children {
			get {
				return wrapper;
			}
		}
		
		public MyBlockStatement()
		{
			this.wrapper = new ChildrenCollection(this, base.Children);
		}
	}
}
