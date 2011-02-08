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
				OptimizeShortCircuits();
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
		
		public void OptimizeShortCircuits()
		{
			foreach(Node child in this.Childs) {
				if (child is Loop) {
					child.OptimizeShortCircuits();
				}
			}
			
		Reset:
			foreach(Node child in this.Childs) {
				if (TryOptimizeShortCircuit(child)) {
					goto Reset;
				}
			}
		}
		
		public static bool TryOptimizeShortCircuit(Node head)
		{
			if ((head is BasicBlock) &&
			    (head as BasicBlock).BranchBasicBlock != null &&
			    (head as BasicBlock).FallThroughBasicBlock != null) {
				head.Parent.MergeChilds<SimpleBranch>(head);
				return true;
			}
			
			Branch top = head as Branch;
			if (top == null) return false;
			
			Branch left = head.FloatUpToNeighbours(top.TrueSuccessor) as Branch;
			Branch right = head.FloatUpToNeighbours(top.FalseSuccessor) as Branch;
			
			// A & B
			if (left != null && 
			    left.Predecessors.Count == 1 &&
			    left.FalseSuccessor == top.FalseSuccessor) {
				ShortCircuitBranch scBranch = top.Parent.MergeChilds<ShortCircuitBranch>(top, left);
				scBranch.Operator = ShortCircuitOperator.LeftAndRight;
				return true;
			}
			
			// ~A | B
			if (left != null && 
			    left.Predecessors.Count == 1 &&
			    left.TrueSuccessor == top.FalseSuccessor) {
				ShortCircuitBranch scBranch = top.Parent.MergeChilds<ShortCircuitBranch>(top, left);
				scBranch.Operator = ShortCircuitOperator.NotLeftOrRight;
				return true;
			}
			
			// A | B
			if (right != null &&
			    right.Predecessors.Count == 1 &&
			    right.TrueSuccessor == top.TrueSuccessor) {
				ShortCircuitBranch scBranch = top.Parent.MergeChilds<ShortCircuitBranch>(top, right);
				scBranch.Operator = ShortCircuitOperator.LeftOrRight;
				return true;
			}
			
			// ~A & B
			if (right != null &&
			    right.Predecessors.Count == 1 &&
			    right.FalseSuccessor == top.TrueSuccessor) {
				ShortCircuitBranch scBranch = top.Parent.MergeChilds<ShortCircuitBranch>(top, right);
				scBranch.Operator = ShortCircuitOperator.NotLeftAndRight;
				return true;
			}
			
			return false;
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
				if (conditionNode is Branch) {
					// Found start of conditional
					OptimizeIf((Branch)conditionNode);
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
		
		public static void OptimizeIf(Branch condition)
		{
			Node trueStart = condition.FloatUpToNeighbours(condition.TrueSuccessor);
			Node falseStart = condition.FloatUpToNeighbours(condition.FalseSuccessor);
			
			NodeCollection trueReachable = trueStart != null ? trueStart.GetReachableNodes() : NodeCollection.Empty;
			NodeCollection falseReachable = falseStart != null ? falseStart.GetReachableNodes() : NodeCollection.Empty;
			NodeCollection commonReachable = NodeCollection.Intersect(trueReachable, falseReachable);
			
			NodeCollection trueNodes = trueReachable.Clone();
			trueNodes.RemoveRange(commonReachable);
			NodeCollection falseNodes = falseReachable.Clone();
			falseNodes.RemoveRange(commonReachable);
			
			// Replace the basic block with condition node
			Node conditionParent = condition.Parent;
			int conditionIndex = condition.Index; 
			ConditionalNode conditionalNode = new ConditionalNode(condition);
			conditionalNode.MoveTo(conditionParent, conditionIndex);
			
			// If there are no common nodes, let the 'true' block be the default
			if (commonReachable.Count > 0) {
				trueNodes.MoveTo(conditionalNode.TrueBody);
			}
			
			falseNodes.MoveTo(conditionalNode.FalseBody);
			
			// Optimize the created subtrees
			conditionalNode.TrueBody.OptimizeConditions();
			conditionalNode.FalseBody.OptimizeConditions();
		}
	}
}
