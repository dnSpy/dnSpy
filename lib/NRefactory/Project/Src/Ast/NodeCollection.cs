// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="David Srbecký" email="dsrbecky@gmail.com"/>
//     <version>$Revision$</version>
// </file>

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ICSharpCode.NRefactory.Ast
{
	public class NodeCollection: Collection<INode>
	{
		public event EventHandler<NodeEventArgs> Added;
		public event EventHandler<NodeEventArgs> Removed;
		
		protected virtual void OnAdded(NodeEventArgs e)
		{
			if (Added != null) {
				Added(this, e);
			}
		}
		
		protected virtual void OnRemoved(NodeEventArgs e)
		{
			if (Removed != null) {
				Removed(this, e);
			}
		}
		
		protected override void ClearItems()
		{
			while(this.Count > 0) {
				this.RemoveFirst();
			}
		}
		
		protected override void InsertItem(int index, INode item)
		{
			base.InsertItem(index, item);
			OnAdded(new NodeEventArgs(item));
		}
		
		protected override void RemoveItem(int index)
		{
			INode removedNode = this[index];
			base.RemoveItem(index);
			OnRemoved(new NodeEventArgs(removedNode));
		}
		
		protected override void SetItem(int index, INode item)
		{
			base.RemoveItem(index);
			base.InsertItem(index, item);
		}
		
		#region Convenience methods
		
		public INode First {
			get {
				if (this.Count > 0) {
					return this[0];
				} else {
					return null;
				}
			}
		}
		
		public INode Last {
			get {
				if (this.Count > 0) {
					return this[this.Count - 1];
				} else {
					return null;
				}
			}
		}
		
		public void AddRange(IEnumerable<INode> items)
		{
			foreach(INode item in items) {
				this.Add(item);
			}
		}
		
		public void RemoveFirst()
		{
			this.RemoveAt(0);
		}
		
		public void RemoveLast()
		{
			this.RemoveAt(this.Count - 1);
		}
		
		#endregion
	}
	
	public class NodeEventArgs: EventArgs
	{
		INode inode;
		
		public INode Node {
			get { return inode; }
		}
		
		public NodeEventArgs(INode inode)
		{
			this.inode = inode;
		}
	}
}
