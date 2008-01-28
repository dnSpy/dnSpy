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
			if (Options.ReduceLoops) {
				OptimizeLoops();
			}
			if (Options.ReduceConditonals) {
				OptimizeConditions();
			}
		}
		
		public void OptimizeLoops()
		{
		Reset:
			foreach(Node child in this.Childs) {
				if (child.Predecessors.Count == 1) {
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
				Options.NotifyReducingGraph();
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
				foreach(Node alsoReachable in reachableNodes[i].Successors) {
					// Do not go though the head child
					if (alsoReachable != this.Parent.HeadChild) {
						reachableNodes.Add(alsoReachable);
					}
				}
			}
			return reachableNodes;
		}
		
		public void OptimizeConditions()
		{
			foreach(Node child in this.Childs) {
				if (child is Loop) {
					child.OptimizeConditions();
				}
			}
			
			Node conditionNode = this.HeadChild;
			while(conditionNode != null) {
				// Keep looking for some conditional block
				if (conditionNode is BasicBlock && ((BasicBlock)conditionNode).IsConditionalBranch) {
					// Found start of conditional
					OptimizeIf((BasicBlock)conditionNode);
					// Restart
					conditionNode = this.HeadChild;
					continue;
				} else if (conditionNode.Successors.Count > 0) {
					// Keep looking down
					conditionNode = conditionNode.Successors[0];
					if (conditionNode == this.HeadChild) {
						return;
					}
					continue; // Next
				} else {
					return; // End of block
				}
			}
		}
		
		public static void OptimizeIf(BasicBlock condition)
		{
			Node trueStart = condition.FloatUpToNeighbours(condition.FallThroughBasicBlock);
			Node falseStart = condition.FloatUpToNeighbours(condition.BranchBasicBlock);
			
			NodeCollection trueReachable = trueStart != null ? trueStart.GetReachableNodes() : NodeCollection.Empty;
			NodeCollection falseReachable = falseStart != null ? falseStart.GetReachableNodes() : NodeCollection.Empty;
			NodeCollection commonReachable = NodeCollection.Intersect(trueReachable, falseReachable);
			
			NodeCollection trueNodes = trueReachable.Clone();
			trueNodes.RemoveRange(commonReachable);
			NodeCollection falseNodes = falseReachable.Clone();
			falseNodes.RemoveRange(commonReachable);
			
			// Replace the basic block with condition node
			Options.NotifyReducingGraph();
			Node conditionParent = condition.Parent;
			int conditionIndex = condition.Index; 
			ConditionalNode conditionalNode = new ConditionalNode(condition);
			conditionalNode.MoveTo(conditionParent, conditionIndex);
			
			Options.NotifyReducingGraph();
			trueNodes.MoveTo(conditionalNode.TrueBody);
			
			// We can exit the 'true' part of Loop or MethodBody conviently using 'break' or 'return'
			if (commonReachable.Count > 0 && (conditionalNode.Parent is Loop || conditionalNode.Parent is MethodBodyGraph)) {
				Options.NotifyReducingGraph();
				falseNodes.MoveTo(conditionalNode.FalseBody);
			}
			
			// Optimize the created subtrees
			conditionalNode.TrueBody.OptimizeConditions();
			conditionalNode.FalseBody.OptimizeConditions();
		}
	}
}
