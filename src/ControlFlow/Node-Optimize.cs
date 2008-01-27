using System;
using System.Collections;
using System.Collections.Generic;

namespace Decompiler.ControlFlow
{
	public abstract partial class Node
	{
		public void Optimize()
		{
			OptimizeLoops();
		}
		
		public void OptimizeLoops()
		{
		Reset:
			foreach(Node child in this.Childs) {
				if (child.Predecessors.Count == 1) {
					if (Options.ReduceGraph <= 0) return;
					Node predecessor = child.Predecessors[0];
					Node mergedNode;
					if (child.Successors.Contains(predecessor)) {
						mergedNode = MergeChilds<Loop>(predecessor, child);
					} else {
						mergedNode = MergeChilds<AcyclicGraph>(predecessor, child);
					}
					mergedNode.FalttenAcyclicChilds();
					goto Reset;
				}
			}
			// If the result is single acyclic node, eliminate it
			if (this.Childs.Count == 1 && this.HeadChild is AcyclicGraph) {
				if (Options.ReduceGraph-- <= 0) return;
				Node headChild = this.HeadChild;
				this.Childs.Remove(this.HeadChild);
				headChild.Childs.MoveTo(this);
			}
		}
		
		NodeCollection GetReachableNodes()
		{
			NodeCollection accumulator = new NodeCollection();
			AddReachableNode(accumulator, this);
			return accumulator;
		}
		
		static void AddReachableNode(NodeCollection accumulator, Node node)
		{
			if (!accumulator.Contains(node)) {
				accumulator.Add(node);
				foreach(Node successor in node.Successors) {
					AddReachableNode(accumulator, successor);
				}
			}
		}
		
		public void OptimizeIf()
		{
			Node conditionNode = this.HeadChild;
			// Find conditionNode (the start)
			while(true) {
				if (conditionNode is BasicBlock && conditionNode.Successors.Count == 2) {
					break; // Found
				} else if (conditionNode.Successors.Count == 1) {
					conditionNode = conditionNode.Successors[0];
					continue; // Next
				} else {
					return;
				}
			}
		}
	}
}
