using System;
using System.Collections;
using System.Collections.Generic;

namespace Decompiler.ControlFlow
{
	public class Set<T>: System.Collections.ObjectModel.Collection<T>
	{
		public void AddRange(IEnumerable<T> items)
		{
			foreach(T item in items) {
				this.Add(item);
			}
		}
		
		protected override void InsertItem(int index, T item)
		{
			if (!this.Contains(item)) {
				base.InsertItem(index, item);
			}
		}
	}
	
	public abstract class Node
	{
		public static int NextNodeID = 1;
		
		int id;
		Node parent;
		Node headChild;
		Set<Node> childs = new Set<Node>();
		Set<Node> predecessors = new Set<Node>();
		Set<Node> successors = new Set<Node>();
		
		public int ID {
			get { return id; }
		}
		
		public Node Parent {
			get { return parent; }
		}
		
		public Node HeadChild {
			get { return headChild; }
			protected set { headChild = value; }
		}
		
		public Set<Node> Childs {
			get { return childs; }
		}
		
		public Set<Node> Predecessors {
			get { return predecessors; }
		}
		
		public Set<Node> Successors {
			get { return successors; }
		}
		
		public Node(Node parent)
		{
			this.parent = parent;
			this.id = NextNodeID++;
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
			
			if (this.Predecessors.Count > 0 || this.Successors.Count > 0) {
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
				}
				
				if (this.Predecessors.Count > 0 && this.Successors.Count > 0) {
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
				}
				sb.Append(")");
			}
			return sb.ToString();
		}
	}
}
