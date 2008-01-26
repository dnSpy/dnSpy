using System;
using System.Collections;
using System.Collections.Generic;

namespace Decompiler.ControlFlow
{
	public class NodeCollection: System.Collections.ObjectModel.Collection<Node>
	{
		public void AddRange(IEnumerable<Node> items)
		{
			foreach(Node item in items) {
				this.Add(item);
			}
		}
		
		protected override void InsertItem(int index, Node item)
		{
			if (!this.Contains(item)) {
				base.InsertItem(index, item);
			}
		}
		
		public bool ContainsRecursive(Node node)
		{
			if (this.Contains(node)) {
				return true;
			}
			foreach(Node item in this.Items) {
				if (item.Childs.ContainsRecursive(node)) {
					return true;
				}
			}
			return false;
		}
		
		public Node FindContainer(Node node)
		{
			foreach(Node item in this.Items) {
				if (item == node || item.Childs.ContainsRecursive(node)) {
					return item;
				}
			}
			return null;
		}
	}
	
	public abstract class Node
	{
		public static int NextNodeID = 1;
		
		int id;
		Node parent;
		Node headChild;
		NodeCollection childs = new NodeCollection();
		NodeCollection predecessors = new NodeCollection();
		NodeCollection successors = new NodeCollection();
		
		public int ID {
			get { return id; }
		}
		
		public Node Parent {
			get { return parent; }
			set { parent = value; }
		}
		
		public Node HeadChild {
			get { return headChild; }
			protected set { headChild = value; }
		}
		
		public NodeCollection Childs {
			get { return childs; }
		}
		
		public NodeCollection Predecessors {
			get { return predecessors; }
		}
		
		public NodeCollection Successors {
			get { return successors; }
		}
		
		public Node NextNode {
			get {
				if (this.Parent == null) throw new Exception("Does not have a parent");
				int myIndex = this.Parent.Childs.IndexOf(this);
				int index = myIndex + 1;
				if (0 <= index && index < this.Parent.Childs.Count) {
					return this.Parent.Childs[index];
				} else {
					return null;
				}
			}
		}
		
		public string Label {
			get {
				return this.GetType().Name + "_" + ID;
			}
		}
		
		public Node(Node parent)
		{
			this.parent = parent;
			this.id = NextNodeID++;
		}
		
		void GetBasicBlockSuccessors(NodeCollection accumulator)
		{
			BasicBlock me = this as BasicBlock;
			if (me != null) {
				if (me.FallThroughBasicBlock != null) {
					accumulator.Add(me.FallThroughBasicBlock);
				}
				if (me.BranchBasicBlock != null) {
					accumulator.Add(me.BranchBasicBlock);
				}
			} else {
				foreach(Node child in this.Childs) {
					child.GetBasicBlockSuccessors(accumulator);
				}
			}
		}
		
		public void RebuildNodeLinks()
		{
			foreach(Node child in this.Childs) {
				NodeCollection successorBasicBlocks = new NodeCollection();
				child.GetBasicBlockSuccessors(successorBasicBlocks);
				NodeCollection successorNodes = new NodeCollection();
				foreach(Node successorBasicBlock in successorBasicBlocks) {
					Node container = this.Childs.FindContainer(successorBasicBlock);
					if (container != null) {
						successorNodes.Add(container);
					}
				}
				// Remove self link
				if (successorNodes.Contains(child)) {
					successorNodes.Remove(child);
				}
				foreach(Node target in successorNodes) {
					child.Successors.Add(target);
					target.Predecessors.Add(child);
				}
			}
		}
		
		public void Optimize()
		{
			for(int i = 0; i < this.Childs.Count;) {
				Node child = childs[i];
				if (child.Predecessors.Count == 1) {
					if (Options.ReduceGraph-- <= 0) return;
					MergeChilds(child.Predecessors[0], child);
					i = 0; // Restart
				} else {
					i++; // Next
				}
			}
			// If it result is single acyclic node, eliminate it
			if (this.Childs.Count == 1 && this.Childs[0] is AcyclicGraph) {
				this.headChild = this.Childs[0].HeadChild;
				this.childs = this.Childs[0].Childs;
				this.UpdateParentOfChilds();
			}
		}
		
		void UpdateParentOfChilds()
		{
			foreach(Node child in this.Childs) {
				child.Parent = this;
			}
		}
		
		static void MergeChilds(Node head, Node tail)
		{
			if (head == null) throw new ArgumentNullException("head");
			if (tail == null) throw new ArgumentNullException("tail");
			if (head.Parent != tail.Parent) throw new ArgumentException("different parents");
			
			Node container = head.Parent;
			Node mergedNode;
			// Get type
			if (tail.Successors.Contains(head)) {
				mergedNode = new Loop(container);
			} else {
				mergedNode = new AcyclicGraph(container);
			}
			
			// Add head
			if (head is BasicBlock) {
				mergedNode.HeadChild = head;
				mergedNode.Childs.Add(head);
			} else if (head is AcyclicGraph) {
				mergedNode.HeadChild = ((AcyclicGraph)head).HeadChild;
				mergedNode.Childs.AddRange(((AcyclicGraph)head).Childs);
			} else if (head is Loop) {
				mergedNode.HeadChild = head;
				mergedNode.Childs.Add(head);
			} else {
				throw new Exception("Invalid head type");
			}
			
			// Add tail
			if (tail is BasicBlock) {
				mergedNode.Childs.Add(tail);
			} else if (tail is AcyclicGraph) {
				mergedNode.Childs.AddRange(((AcyclicGraph)tail).Childs);
			} else if (tail is Loop) {
				mergedNode.Childs.Add(tail);
			} else {
				throw new Exception("Invalid tail type");
			}
			
			mergedNode.UpdateParentOfChilds();
			
			// Remove links between the head and tail
			if (head.Successors.Contains(tail)) {
				head.Successors.Remove(tail);
				tail.Predecessors.Remove(head);
			}
			if (tail.Successors.Contains(head)) {
				tail.Successors.Remove(head);
				head.Predecessors.Remove(tail);
			}
			
			Relink(head, mergedNode);
			Relink(tail, mergedNode);
			
			mergedNode.RebuildNodeLinks();
			
			// Remove the old nodes and add the merged node - replace head with the merged node
			container.Childs.Remove(tail);
			int headIndex = container.Childs.IndexOf(head);
			container.Childs.Remove(head);
			container.Childs.Insert(headIndex, mergedNode);
		}
		
		static void Relink(Node node, Node target)
		{
			// Relink all neighbours to the target node
			foreach(Node predecessor in node.Predecessors) {
				predecessor.Successors.Remove(node);
				predecessor.Successors.Add(target);
			}
			foreach(Node successor in node.Successors) {
				successor.Predecessors.Remove(node);
				successor.Predecessors.Add(target);
			}
			
			// Move our pointers to the target node
			target.Predecessors.AddRange(node.Predecessors);
			target.Successors.AddRange(node.Successors);
			node.Predecessors.Clear();
			node.Successors.Clear();
		}
		
		public override string ToString()
		{
			System.Text.StringBuilder sb = new System.Text.StringBuilder();
			sb.Append(this.GetType().Name);
			sb.Append(" ");
			sb.Append(ID);
			sb.Append(" ");
			
			sb.Append("(");
			
			if (this.Predecessors.Count > 0) {
				sb.Append("Predecessors:");
				bool isFirst = true;
				foreach(Node predecessor in this.Predecessors) {
					if (isFirst) {
						isFirst = false;
					} else {
						sb.Append(",");
					}
					sb.Append(predecessor.ID);
				}
				sb.Append(" ");
			}
			
			if (this.Successors.Count > 0) {
				sb.Append("Successors:");
				bool isFirst = true;
				foreach(Node successor in this.Successors) {
					if (isFirst) {
						isFirst = false;
					} else {
						sb.Append(",");
					}
					sb.Append(successor.ID);
				}
				sb.Append(" ");
			}
			
			if (this.Parent != null) {
				sb.Append("Parent:");
				sb.Append(this.Parent.ID);
			}
			sb.Append(" ");
			
			if (this.HeadChild != null) {
				sb.Append("Head:");
				sb.Append(this.HeadChild.ID);
			}
			sb.Append(" ");
			
			sb.Length = sb.Length - 1;
			sb.Append(")");
			return sb.ToString();
		}
	}
}
