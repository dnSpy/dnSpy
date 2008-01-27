using System;
using System.Collections;
using System.Collections.Generic;

namespace Decompiler.ControlFlow
{
	public abstract partial class Node
	{
		public static int NextNodeID = 1;
		
		int id;
		Node parent;
		NodeCollection childs = new NodeCollection();
		
		// Structural and linking cache
		NodeCollection basicBlocks_cache = null;
		NodeCollection predecessors_cache = null;
		NodeCollection successors_cache = null;
		
		public int ID {
			get { return id; }
		}
		
		public Node Parent {
			get { return parent; }
		}
		
		public Node HeadChild {
			get {
				if (this.Childs.Count > 0) {
					return this.Childs[0];
				} else {
					return null;
				}
			}
		}
		
		public NodeCollection Childs {
			get {
				return childs;
			}
		}
		
		/// <summary> All basic blocks within the scope of this node (inclusive) </summary>
		public NodeCollection BasicBlocks {
			get {
				if (basicBlocks_cache == null) {
					NodeCollection basicBlocks = new NodeCollection();
					
					if (this is BasicBlock) {
						basicBlocks.Add(this);
					}
					foreach(Node child in this.Childs) {
						basicBlocks.AddRange(child.BasicBlocks);
					}
					
					basicBlocks_cache = basicBlocks;
				}
				return basicBlocks_cache;
			}
		}
		
		NodeCollection FloatUpToNeighbours(IEnumerable<BasicBlock> basicBlocks)
		{
			NodeCollection neighbours = new NodeCollection();
			if (this.Parent != null) {
				foreach(BasicBlock basicBlock in basicBlocks) {
					Node targetNode = FloatUpToNeighbours(basicBlock);
					// The target is outside the scope of the parent node
					if (targetNode == null) continue;
					// This child is a loop
					if (targetNode == this) continue;
					// We found a target in our scope
					neighbours.Add(targetNode);
				}
			}
			return neighbours;
		}
		
		Node FloatUpToNeighbours(BasicBlock basicBlock)
		{
			// Find neighbour coresponding to the basickBlock
			Node targetNode = basicBlock;
			while(targetNode != null && targetNode.Parent != this.Parent) {
				targetNode = targetNode.Parent;
			}
			return targetNode;
		}
		
		public NodeCollection Predecessors {
			get {
				if (predecessors_cache == null) {
					List<BasicBlock> basicBlockPredecessors = new List<BasicBlock>();
					foreach(BasicBlock basicBlock in this.BasicBlocks) {
						foreach(BasicBlock basicBlockPredecessor in basicBlock.BasicBlockPredecessors) {
							basicBlockPredecessors.Add(basicBlockPredecessor);
						}
					}
					
					predecessors_cache = FloatUpToNeighbours(basicBlockPredecessors);
				}
				return predecessors_cache;
			}
		}
		
		public NodeCollection Successors {
			get {
				if (successors_cache == null) {
					List<BasicBlock> basicBlockSuccessors = new List<BasicBlock>();
					foreach(BasicBlock basicBlock in this.BasicBlocks) {
						foreach(BasicBlock basicBlockSuccessor in basicBlock.BasicBlockSuccessors) {
							basicBlockSuccessors.Add(basicBlockSuccessor);
						}
					}
					
					successors_cache = FloatUpToNeighbours(basicBlockSuccessors);
				}
				return successors_cache;
			}
		}
		
		int Index {
			get {
				if (this.Parent == null) throw new Exception("Does not have a parent");
				return this.Parent.Childs.IndexOf(this);
			}
		}
		
		public Node NextNode {
			get {
				int index = this.Index + 1;
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
		
		public string Description {
			get {
				return ToString();
			}
		}
		
		protected Node()
		{
			this.id = NextNodeID++;
			this.Childs.Added += delegate(object sender, NodeEventArgs e) {
				if (e.Node.Parent != null) {
					throw new Exception("Node is already assigned to other parent");
				}
				e.Node.parent = this;
				NotifyChildsChanged();
			};
			this.Childs.Removed += delegate(object sender, NodeEventArgs e) {
				e.Node.parent = null;
				NotifyChildsChanged();
			};
		}
		
		void NotifyChildsChanged()
		{
			this.basicBlocks_cache = null;
			foreach(Node child in this.Childs) {
				child.predecessors_cache = null;
				child.successors_cache = null;
			}
			if (this.Parent != null) {
				this.Parent.NotifyChildsChanged();
			}
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
				sb.Append(" ");
			}
			
			if (sb[sb.Length - 1] == '(') {
				sb.Length -= 1;
			} else if (sb[sb.Length - 1] == ' ') {
				sb.Length -= 1;
				sb.Append(")");
			}
			return sb.ToString();
		}
	}
}
