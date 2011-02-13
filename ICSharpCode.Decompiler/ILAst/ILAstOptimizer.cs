using System;
using System.Collections.Generic;
using System.Linq;
using ICSharpCode.Decompiler.FlowAnalysis;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Decompiler.Rocks;

namespace Decompiler.ControlFlow
{
	public class ILAstOptimizer
	{
		public void Optimize(List<ILNode> ast)
		{
			ILLabel entryLabel;
			List<ILTryCatchBlock> tryCatchBlocks = ast.OfType<ILTryCatchBlock>().ToList();
			
			ControlFlowGraph graph;
			
			ast = SplitToMovableBlocks(ast, out entryLabel);
			
			graph = BuildGraph(ast, entryLabel);
			graph.ComputeDominance();
			graph.ComputeDominanceFrontier();
			ast = FindLoops(new HashSet<ControlFlowNode>(graph.Nodes.Skip(3)), graph.EntryPoint);

			graph = BuildGraph(ast, entryLabel);
			graph.ComputeDominance();
			graph.ComputeDominanceFrontier();
			ast = FindConditions(new HashSet<ControlFlowNode>(graph.Nodes.Skip(3)), graph.EntryPoint);
			
			// Recursively optimze try-cath blocks
			foreach(ILTryCatchBlock tryCatchBlock in tryCatchBlocks) {
				Optimize(tryCatchBlock.TryBlock.Body);
				foreach(ILTryCatchBlock.CatchBlock catchBlock in tryCatchBlock.CatchBlocks) {
					Optimize(catchBlock.Body);
				}
				Optimize(tryCatchBlock.FinallyBlock.Body);
			}
		}
		
		int nextLabelIndex = 0;
		
		/// <summary>
		/// Group input into a set of blocks that can be later arbitraliby schufled.
		/// The method adds necessary branches to make control flow between blocks
		/// explicit and thus order independent.
		/// </summary>
		List<ILNode> SplitToMovableBlocks(List<ILNode> ast, out ILLabel entryLabel)
		{
			List<ILNode> blocks = new List<ILNode>();
			
			ILBlock block = new ILBlock();
			blocks.Add(block);
			entryLabel = new ILLabel() { Name = "Block_" + (nextLabelIndex++) };
			block.Body.Add(entryLabel);
			
			if (ast.Count == 0)
				return blocks;
			block.Body.Add(ast[0]);
			
			for (int i = 1; i < ast.Count; i++) {
				ILNode lastNode = ast[i - 1];
				ILNode currNode = ast[i];
				
				// Insert split
				if ((currNode is ILLabel && !(lastNode is ILLabel)) ||
					lastNode is ILTryCatchBlock ||
					currNode is ILTryCatchBlock ||
				    (lastNode is ILExpression) && ((ILExpression)lastNode).OpCode.IsBranch() ||
				    (currNode is ILExpression) && ((ILExpression)currNode).OpCode.IsBranch())
				{
					ILBlock lastBlock = block;
					block = new ILBlock();
					blocks.Add(block);
					
					// Explicit branch from one block to other
					// (unless the last expression was unconditional branch)
					if (!(lastNode is ILExpression) || ((ILExpression)lastNode).OpCode.CanFallThough()) {
						ILLabel blockLabel = new ILLabel() { Name = "Block_" + (nextLabelIndex++) };
						lastBlock.Body.Add(new ILExpression(OpCodes.Br_S, blockLabel));
						block.Body.Add(blockLabel);
					}
				}
				
				block.Body.Add(currNode);
			}
			
			return blocks;
		}
		
		ControlFlowGraph BuildGraph(List<ILNode> nodes, ILLabel entryLabel)
		{
			int index = 0;
			List<ControlFlowNode> cfNodes = new List<ControlFlowNode>();
			ControlFlowNode entryPoint = new ControlFlowNode(index++, 0, ControlFlowNodeType.EntryPoint);
			cfNodes.Add(entryPoint);
			ControlFlowNode regularExit = new ControlFlowNode(index++, -1, ControlFlowNodeType.RegularExit);
			cfNodes.Add(regularExit);
			ControlFlowNode exceptionalExit = new ControlFlowNode(index++, -1, ControlFlowNodeType.ExceptionalExit);
			cfNodes.Add(exceptionalExit);
			
			// Create graph nodes
			Dictionary<ILLabel, ControlFlowNode> labelToCfNode = new Dictionary<ILLabel, ControlFlowNode>();
			Dictionary<ILNode, ControlFlowNode>  astNodeToCfNode = new Dictionary<ILNode, ControlFlowNode>();
			foreach(ILNode node in nodes) {
				ControlFlowNode cfNode = new ControlFlowNode(index++, -1, ControlFlowNodeType.Normal);
				cfNodes.Add(cfNode);
				astNodeToCfNode[node] = cfNode;
				cfNode.UserData = node;
				
				// Find all contained labels
				foreach(ILLabel label in node.GetChildrenRecursive<ILLabel>()) {
					labelToCfNode[label] = cfNode;
				}
			}
			
			// Entry endge
			ControlFlowNode entryNode = labelToCfNode[entryLabel];
			ControlFlowEdge entryEdge = new ControlFlowEdge(entryPoint, entryNode, JumpType.Normal);
			entryPoint.Outgoing.Add(entryEdge);
			entryNode.Incoming.Add(entryEdge);
			
			// Create edges
			foreach(ILNode node in nodes) {
				ControlFlowNode source = astNodeToCfNode[node];
				
				// Find all branches
				foreach(ILExpression child in node.GetChildrenRecursive<ILExpression>()) {
					IEnumerable<ILLabel> targets = child.GetBranchTargets();
					if (targets != null) {
						foreach(ILLabel target in targets) {
							ControlFlowNode destination;
							// Labels which are out of out scope will not be int the collection
							if (labelToCfNode.TryGetValue(target, out destination) && destination != source) {
								ControlFlowEdge edge = new ControlFlowEdge(source, destination, JumpType.Normal);
								source.Outgoing.Add(edge);
								destination.Incoming.Add(edge);
							}
						}
					}
				}
			}
			
			return new ControlFlowGraph(cfNodes.ToArray());
		}
		
		static List<ILNode> FindLoops(HashSet<ControlFlowNode> body, ControlFlowNode entryPoint)
		{
			List<ILNode> result = new List<ILNode>();
			
			Queue<ControlFlowNode> agenda  = new Queue<ControlFlowNode>();
			agenda.Enqueue(entryPoint);
			while(agenda.Count > 0) {
				ControlFlowNode node = agenda.Dequeue();
				
				if (body.Contains(node)
			    		&& node.DominanceFrontier.Contains(node)
			    		&& node != entryPoint)
				{
					HashSet<ControlFlowNode> loopContents = new HashSet<ControlFlowNode>();
					FindLoopContents(body, loopContents, node, node);
					
					// Move the content into loop block
					body.ExceptWith(loopContents);
					result.Add(new ILLoop() { ContentBlock = new ILBlock(FindLoops(loopContents, node)) });
				}

				// Using the dominator tree should ensure we find the the widest loop first
				foreach(var child in node.DominatorTreeChildren) {
					agenda.Enqueue(child);
				}
			}
			
			// Add whatever is left
			foreach(var node in body) {
				result.Add((ILNode)node.UserData);
			}
			
			return result;
		}
		
		static void FindLoopContents(HashSet<ControlFlowNode> body, HashSet<ControlFlowNode> loopContents, ControlFlowNode loopHead, ControlFlowNode addNode)
		{
			if (body.Contains(addNode) && loopHead.Dominates(addNode) && loopContents.Add(addNode)) {
				foreach (var edge in addNode.Incoming) {
					FindLoopContents(body, loopContents, loopHead, edge.Source);
				}
			}
		}
		
		static HashSet<ControlFlowNode> FindDominatedNodes(HashSet<ControlFlowNode> body, ControlFlowNode head)
		{
			var exitNodes = head.DominanceFrontier.SelectMany(n => n.Predecessors);
			HashSet<ControlFlowNode> agenda = new HashSet<ControlFlowNode>(exitNodes);
			HashSet<ControlFlowNode> result = new HashSet<ControlFlowNode>();
			
			while(agenda.Count > 0) {
				ControlFlowNode addNode = agenda.First();
				agenda.Remove(addNode);
			
				if (body.Contains(addNode) && head.Dominates(addNode) && result.Add(addNode)) {
					foreach (var predecessor in addNode.Predecessors) {
						agenda.Add(predecessor);
					}
				}
			}
			result.Add(head);
			
			return result;
		}
		
		static List<ILNode> FindConditions(HashSet<ControlFlowNode> body, ControlFlowNode entryNode)
		{
			List<ILNode> result = new List<ILNode>();
			
			Queue<ControlFlowNode> agenda  = new Queue<ControlFlowNode>();
			agenda.Enqueue(entryNode);
			while(agenda.Count > 0) {
				ControlFlowNode node = agenda.Dequeue();
				
				if (body.Contains(node) && node.Outgoing.Count == 2) {
					ILCondition condition = new ILCondition() {
					    ConditionBlock = new ILBlock((ILNode)node.UserData)
					};
					HashSet<ControlFlowNode> frontiers = new HashSet<ControlFlowNode>();
					frontiers.UnionWith(node.Outgoing[0].Target.DominanceFrontier);
					frontiers.UnionWith(node.Outgoing[1].Target.DominanceFrontier);
					if (!frontiers.Contains(node.Outgoing[0].Target)) {
						HashSet<ControlFlowNode> content1 = FindDominatedNodes(body, node.Outgoing[0].Target);
						body.ExceptWith(content1);
					    condition.Block1 = new ILBlock(FindConditions(content1, node.Outgoing[0].Target));
					}
					if (!frontiers.Contains(node.Outgoing[1].Target)) {
						HashSet<ControlFlowNode> content2 = FindDominatedNodes(body, node.Outgoing[1].Target);
						body.ExceptWith(content2);
					    condition.Block2 = new ILBlock(FindConditions(content2, node.Outgoing[1].Target));
					}
					result.Add(condition);
				}

				// Using the dominator tree should ensure we find the the widest loop first
				foreach(var child in node.DominatorTreeChildren) {
					agenda.Enqueue(child);
				}
			}
			
			// Add whatever is left
			foreach(var node in body) {
				result.Add((ILNode)node.UserData);
			}
			
			return result;
		}
		
		/*
		
		public enum ShortCircuitOperator
		{
			LeftAndRight,
			LeftOrRight,
			NotLeftAndRight,
			NotLeftOrRight,
		}
		
		static bool TryOptimizeShortCircuit(Node head)
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
		
		*/
	}
}
