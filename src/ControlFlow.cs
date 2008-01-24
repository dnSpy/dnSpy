using System;
using System.Collections;
using System.Collections.Generic;

namespace Decompiler
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
		Node parent;
		Node headChild;
		Set<Node> childs = new Set<Node>();
		Set<Node> predecessors = new Set<Node>();
		Set<Node> successors = new Set<Node>();
		
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
		}
	}
	
	public class BasicBlock: Node
	{
		int id;
		List<StackExpression> body = new List<StackExpression>();
		
		public int Id {
			get { return id; }
		}
		
		public List<StackExpression> Body {
			get { return body; }
		}
		
		public BasicBlock(Node parent, int id): base(parent)
		{
			this.id = id;
		}
		
		public override string ToString()
		{
			return string.Format("BasicBlock {0}", id, body.Count);
		}
	}
	
	public enum BasicBlockSetType {
		MethodBody,
		Acyclic,
		Loop,
	}
	
	// TODO: Split into two classes?
	public class BasicBlockSet: Node
	{
		BasicBlockSetType type;
		
		public BasicBlockSetType Type {
			get { return type; }
		}
		
		BasicBlockSet(Node parent): base(parent)
		{
			
		}
		
		// TODO: Add links between the generated BasicBlocks
		public BasicBlockSet(StackExpressionCollection exprs): base(null)
		{
			if (exprs.Count == 0) throw new ArgumentException("Count == 0", "exprs");
			
			this.type = BasicBlockSetType.MethodBody;
			
			BasicBlock basicBlock = null;
			int basicBlockId = 1;
			for(int i = 0; i < exprs.Count; i++) {
				// Start new basic block if
				//  - this is first expression
				//  - last expression was branch
				//  - this expression is branch target
				if (i == 0 || exprs[i - 1].BranchTarget != null || exprs[i].BranchesHere.Count > 0){
					basicBlock = new BasicBlock(this, basicBlockId++);
					this.Childs.Add(basicBlock);
				}
				basicBlock.Body.Add(exprs[i]);
			}
			
			this.HeadChild = this.Childs[0];
		}
		
		public void Optimize()
		{
			bool optimized;
			do {
				optimized = false;
				foreach(Node child in this.Childs) {
					if (child.Predecessors.Count == 1) {
						Node predecessor = child.Predecessors[0];
						MergeNodes(predecessor, child);
						optimized = true;
						break; // Collection was modified; restart
					}
				}
			} while (optimized);
		}
		
		static void MergeNodes(Node head, Node tail)
		{
			if (head == null) throw new ArgumentNullException("head");
			if (tail == null) throw new ArgumentNullException("tail");
			if (head.Parent != tail.Parent) throw new ArgumentException("different parents");
			
			Node container = head.Parent;
			BasicBlockSet mergedNode = new BasicBlockSet(container);
			
			// Get type
			if (tail.Successors.Contains(head)) {
				mergedNode.type = BasicBlockSetType.Loop;
			} else {
				mergedNode.type = BasicBlockSetType.Acyclic;
			}
			
			BasicBlockSet headAsSet = head as BasicBlockSet;
			BasicBlockSet tailAsSet = tail as BasicBlockSet;
			
			// Add head
			if (head is BasicBlock) {
				mergedNode.HeadChild = head;
				mergedNode.Childs.Add(head);
			} else if (headAsSet != null && headAsSet.type == BasicBlockSetType.Acyclic) {
				mergedNode.HeadChild = headAsSet.HeadChild;
				mergedNode.Childs.AddRange(headAsSet.Childs);
			} else if (headAsSet != null && headAsSet.type == BasicBlockSetType.Loop) {
				mergedNode.HeadChild = headAsSet;
				mergedNode.Childs.Add(headAsSet);
			} else {
				throw new Exception("Invalid head");
			}
			
			// Add tail
			if (tail is BasicBlock) {
				mergedNode.Childs.Add(tail);
			} else if (tailAsSet != null && tailAsSet.type == BasicBlockSetType.Acyclic) {
				mergedNode.Childs.AddRange(tailAsSet.Childs);
			} else if (tailAsSet != null && tailAsSet.type == BasicBlockSetType.Loop) {
				mergedNode.Childs.Add(tailAsSet);
			} else {
				throw new Exception("Invalid tail");
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
			
			// Remove the old nodes and add the merged node
			container.Childs.Remove(head);
			container.Childs.Remove(tail);
			container.Childs.Add(mergedNode);
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
	}
}
