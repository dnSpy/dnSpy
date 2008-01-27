using System;
using System.Collections.Generic;

namespace Decompiler.ControlFlow
{
	public class NodeEventArgs: EventArgs
	{
		Node node;
		
		public Node Node {
			get { return node; }
		}
		
		public NodeEventArgs(Node node)
		{
			this.node = node;
		}
	}
	
	public class NodeCollection: System.Collections.ObjectModel.Collection<Node>
	{
		public event EventHandler<NodeEventArgs> Added;
		public event EventHandler<NodeEventArgs> Removed;
		
		protected virtual void OnAdded(Node node)
		{
			if (Added != null) {
				Added(this, new NodeEventArgs(node));
			}
		}
		
		protected virtual void OnRemoved(Node node)
		{
			if (Removed != null) {
				Removed(this, new NodeEventArgs(node));
			}
		}
		
		protected override void ClearItems()
		{
			while(this.Count > 0) {
				this.RemoveAt(this.Count - 1);
			}
		}
		
		protected override void InsertItem(int index, Node item)
		{
			if (!this.Contains(item)) {
				base.InsertItem(index, item);
			}
			OnAdded(item);
		}
		
		protected override void RemoveItem(int index)
		{
			Node node = this[index];
			base.RemoveItem(index);
			OnRemoved(node);
		}
		
		protected override void SetItem(int index, Node item)
		{
			this.RemoveAt(index);
			this.Insert(index, item);
		}
		
		
		public void AddRange(IEnumerable<Node> items)
		{
			foreach(Node item in items) {
				this.Add(item);
			}
		}
		
		public void RemoveRange(IEnumerable<Node> items)
		{
			foreach(Node item in items) {
				this.Remove(item);
			}
		}
		
		public void MoveTo(Node newNode)
		{
			while(this.Count > 0) {
				this[0].MoveTo(newNode);
			}
		}
		
		public void MoveTo(Node newNode, int index)
		{
			while(this.Count > 0) {
				this[0].MoveTo(newNode, index);
				index++;
			}
		}
		
		public static NodeCollection Intersection(NodeCollection collectionA, NodeCollection collectionB)
		{
			NodeCollection result = new NodeCollection();
			foreach(Node a in collectionA) {
				if (collectionB.Contains(a)) {
					result.Add(a);
				}
			}
			return result;
		}
		
		public override string ToString()
		{
			return string.Format("{0} Count = {1}", typeof(NodeCollection).Name, this.Count);
		}
	}
}
