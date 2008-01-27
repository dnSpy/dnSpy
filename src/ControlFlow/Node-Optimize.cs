using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace Decompiler.ControlFlow
{
	public abstract partial class Node
	{
		public void Optimize()
		{
			OptimizeLoops();
			OptimizeIf();
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
			NodeCollection reachableNodes = new NodeCollection();
			reachableNodes.Add(this);
			for(int i = 0; i < reachableNodes.Count; i++) {
				reachableNodes.AddRange(reachableNodes[i].Successors);
			}
			return reachableNodes;
		}
		
		public void OptimizeIf()
		{
			Node conditionNode = this.HeadChild;
			// Find conditionNode (the start)
			while(true) {
				if (conditionNode is BasicBlock && conditionNode.Successors.Count == 2) {
					// Found if start
					OptimizeIf((BasicBlock)conditionNode);
					return;
				} else if (conditionNode.Successors.Count == 1) {
					conditionNode = conditionNode.Successors[0];
					continue; // Next
				} else {
					return; // Just give up
				}
			}
		}
		
		public static void OptimizeIf(BasicBlock condition)
		{
			Node trueStart = condition.FloatUpToNeighbours(condition.FallThroughBasicBlock);
			Node falseStart = condition.FloatUpToNeighbours(condition.BranchBasicBlock);
			Debug.Assert(trueStart != null);
			Debug.Assert(falseStart != null);
			Debug.Assert(trueStart != falseStart);
			
			NodeCollection trueReachable = trueStart.GetReachableNodes();
			NodeCollection falseReachable = falseStart.GetReachableNodes();
			NodeCollection commonReachable = NodeCollection.Intersect(trueReachable, falseReachable);
			
			NodeCollection trueNodes = trueReachable.Clone();
			trueNodes.RemoveRange(commonReachable);
			NodeCollection falseNodes = falseReachable.Clone();
			falseNodes.RemoveRange(commonReachable);
			
			// Replace the basic block with condition node
			if (Options.ReduceGraph-- <= 0) return;
			Node conditionParent = condition.Parent;
			int conditionIndex = condition.Index; 
			ConditionalNode conditionalNode = new ConditionalNode(condition);
			conditionalNode.MoveTo(conditionParent, conditionIndex);
			
			if (Options.ReduceGraph-- <= 0) return;
			trueNodes.MoveTo(conditionalNode.TrueBody);
			
			if (Options.ReduceGraph-- <= 0) return;
			falseNodes.MoveTo(conditionalNode.FalseBody);
		}
	}
}
