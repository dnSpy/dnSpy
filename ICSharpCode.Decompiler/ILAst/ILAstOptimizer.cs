using System;
using System.Collections.Generic;
using System.Linq;
using ICSharpCode.Decompiler.FlowAnalysis;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Decompiler.ControlFlow
{
	public enum ILAstOptimizationStep
	{
		SplitToMovableBlocks,
		FindLoops,
		FindConditions,
		FlattenNestedMovableBlocks,
		SimpleGotoRemoval,
		RemoveDeadLabels,
		TypeInference,
		None
	}
	
	public class ILAstOptimizer
	{
		Dictionary<ILLabel, ControlFlowNode> labelToCfNode = new Dictionary<ILLabel, ControlFlowNode>();
		Dictionary<ILLabel, int> labelRefCount;
		
		public void Optimize(DecompilerContext context, ILBlock method, ILAstOptimizationStep abortBeforeStep = ILAstOptimizationStep.None)
		{
			if (abortBeforeStep == ILAstOptimizationStep.SplitToMovableBlocks) return;
			foreach(ILBlock block in method.GetSelfAndChildrenRecursive<ILBlock>().ToList()) {
				SplitToBasicBlocks(block);
			}
			
			if (abortBeforeStep == ILAstOptimizationStep.FindLoops) return;
			UpdateLabelRefCounts(method);
			foreach(ILBlock block in method.GetSelfAndChildrenRecursive<ILBlock>().ToList()) {
				ControlFlowGraph graph;
				graph = BuildGraph(block.Body, (ILLabel)block.EntryGoto.Operand);
				graph.ComputeDominance();
				graph.ComputeDominanceFrontier();
				block.Body = FindLoops(new HashSet<ControlFlowNode>(graph.Nodes.Skip(3)), graph.EntryPoint, false);
			}
			
			if (abortBeforeStep == ILAstOptimizationStep.FindConditions) return;
			UpdateLabelRefCounts(method);
			foreach(ILBlock block in method.GetSelfAndChildrenRecursive<ILBlock>().ToList()) {
				ControlFlowGraph graph;
				graph = BuildGraph(block.Body, (ILLabel)block.EntryGoto.Operand);
				// TODO: Fix
				if (graph == null)
					continue;
				graph.ComputeDominance();
				graph.ComputeDominanceFrontier();
				block.Body = FindConditions(new HashSet<ControlFlowNode>(graph.Nodes.Skip(3)), graph.EntryPoint);
			}
			
			if (abortBeforeStep == ILAstOptimizationStep.FlattenNestedMovableBlocks) return;
			FlattenBasicBlocks(method);
			
			if (abortBeforeStep == ILAstOptimizationStep.SimpleGotoRemoval) return;
			SimpleGotoRemoval(method);
			
			if (abortBeforeStep == ILAstOptimizationStep.RemoveDeadLabels) return;
			RemoveDeadLabels(method);
			
			if (abortBeforeStep == ILAstOptimizationStep.TypeInference) return;
			TypeAnalysis.Run(context, method);
		}
		
		int nextBlockIndex = 0;
		
		/// <summary>
		/// Group input into a set of blocks that can be later arbitraliby schufled.
		/// The method adds necessary branches to make control flow between blocks
		/// explicit and thus order independent.
		/// </summary>
		void SplitToBasicBlocks(ILBlock block)
		{
			// Remve no-ops
			// TODO: Assign the no-op range to someting
			block.Body = block.Body.Where(n => !(n is ILExpression && ((ILExpression)n).Code == ILCode.Nop)).ToList();
			
			List<ILNode> basicBlocks = new List<ILNode>();
			
			ILBasicBlock basicBlock = new ILBasicBlock() {
				EntryLabel = new ILLabel() { Name = "Block_" + (nextBlockIndex++) }
			};
			basicBlocks.Add(basicBlock);
			block.EntryGoto = new ILExpression(ILCode.Br, basicBlock.EntryLabel);
			
			if (block.Body.Count > 0) {
				basicBlock.Body.Add(block.Body[0]);
				
				for (int i = 1; i < block.Body.Count; i++) {
					ILNode lastNode = block.Body[i - 1];
					ILNode currNode = block.Body[i];
					
					bool added = false;
					
					// Insert split
					if (currNode is ILLabel ||
						lastNode is ILTryCatchBlock ||
						currNode is ILTryCatchBlock ||
					    (lastNode is ILExpression) && ((ILExpression)lastNode).IsBranch() ||
					    (currNode is ILExpression) && (((ILExpression)currNode).IsBranch() && basicBlock.Body.Count > 0))
					{
						ILBasicBlock lastBlock = basicBlock;
						basicBlock = new ILBasicBlock();
						basicBlocks.Add(basicBlock);
						if (currNode is ILLabel) {
							// Reuse the first label
							basicBlock.EntryLabel = (ILLabel)currNode;
							added = true;
						} else {
							basicBlock.EntryLabel = new ILLabel() { Name = "Block_" + (nextBlockIndex++) };
						}
						
						// Explicit branch from one block to other
						// (unless the last expression was unconditional branch)
						if (!(lastNode is ILExpression) || ((ILExpression)lastNode).Code.CanFallThough()) {
							lastBlock.FallthoughGoto = new ILExpression(ILCode.Br, basicBlock.EntryLabel);
						}
					}
					
					if (!added)
						basicBlock.Body.Add(currNode);
				}
			}
			
			block.Body = basicBlocks;
			return;
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
			labelToCfNode = new Dictionary<ILLabel, ControlFlowNode>();
			Dictionary<ILNode, ControlFlowNode> astNodeToCfNode = new Dictionary<ILNode, ControlFlowNode>();
			foreach(ILNode node in nodes) {
				ControlFlowNode cfNode = new ControlFlowNode(index++, -1, ControlFlowNodeType.Normal);
				cfNodes.Add(cfNode);
				astNodeToCfNode[node] = cfNode;
				cfNode.UserData = node;
				
				// Find all contained labels
				foreach(ILLabel label in node.GetSelfAndChildrenRecursive<ILLabel>()) {
					labelToCfNode[label] = cfNode;
				}
			}
			
			if (!labelToCfNode.ContainsKey(entryLabel))
				return null;
			
			// Entry endge
			ControlFlowNode entryNode = labelToCfNode[entryLabel];
			ControlFlowEdge entryEdge = new ControlFlowEdge(entryPoint, entryNode, JumpType.Normal);
			entryPoint.Outgoing.Add(entryEdge);
			entryNode.Incoming.Add(entryEdge);
			
			// Create edges
			foreach(ILNode node in nodes) {
				ControlFlowNode source = astNodeToCfNode[node];
				
				// Find all branches
				foreach(ILExpression child in node.GetSelfAndChildrenRecursive<ILExpression>()) {
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
		
		List<ILNode> FindLoops(HashSet<ControlFlowNode> scope, ControlFlowNode entryPoint, bool excludeEntryPoint)
		{
			List<ILNode> result = new List<ILNode>();
			
			Queue<ControlFlowNode> agenda  = new Queue<ControlFlowNode>();
			agenda.Enqueue(entryPoint);
			while(agenda.Count > 0) {
				ControlFlowNode node = agenda.Dequeue();
				
				if (scope.Contains(node)
			    		&& node.DominanceFrontier.Contains(node)
			    		&& (node != entryPoint || !excludeEntryPoint))
				{
					HashSet<ControlFlowNode> loopContents = FindDominatedNodes(scope, node);
					
					ILWhileLoop loop = new ILWhileLoop();
					
					ILCondition cond;
					HashSet<ControlFlowNode> condNodes;
					ILLabel condLabel;
					if (TryMatchCondition(loopContents, new ControlFlowNode[]{}, node, out cond, out condNodes, out condLabel)) {
						loopContents.ExceptWith(condNodes);
						scope.ExceptWith(condNodes);
						// Use loop to implement condition
						loop.Condition      = cond.Condition;
						loop.PreLoopLabel   = condLabel;
						loop.PostLoopGoto   = cond.FalseBlock.EntryGoto;
						loop.BodyBlock      = new ILBlock() { EntryGoto = cond.TrueBlock.EntryGoto };
					} else {
						// Give the block some explicit entry point
						ILLabel entryLabel  = new ILLabel() { Name = "Loop_" + (nextBlockIndex++) };
						loop.BodyBlock      = new ILBlock() { EntryGoto = new ILExpression(ILCode.Br, entryLabel) };
						((ILBasicBlock)node.UserData).Body.Insert(0, entryLabel);
					}
					loop.BodyBlock.Body = FindLoops(loopContents, node, true);
					
					// Move the content into loop block
					scope.ExceptWith(loopContents);
					result.Add(loop);
				}

				// Using the dominator tree should ensure we find the the widest loop first
				foreach(var child in node.DominatorTreeChildren) {
					agenda.Enqueue(child);
				}
			}
			
			// Add whatever is left
			foreach(var node in scope) {
				result.Add((ILNode)node.UserData);
			}
			
			return result;
		}
		
		void UpdateLabelRefCounts(ILBlock method)
		{
			labelRefCount = new Dictionary<ILLabel, int>();
			foreach(ILLabel target in method.GetSelfAndChildrenRecursive<ILExpression>().SelectMany(e => e.GetBranchTargets())) {
				if (!labelRefCount.ContainsKey(target))
					labelRefCount[target] = 0;
				labelRefCount[target]++;
			}
		}
		
		List<ILNode> FindConditions(HashSet<ControlFlowNode> scope, ControlFlowNode entryNode)
		{
			List<ILNode> result = new List<ILNode>();
			
			HashSet<ControlFlowNode> agenda  = new HashSet<ControlFlowNode>();
			agenda.Add(entryNode);
			while(agenda.Any()) {
				ControlFlowNode node = agenda.First();
				// Attempt for a good order
				while(agenda.Contains(node.ImmediateDominator)) {
					node = node.ImmediateDominator;
				}
				agenda.Remove(node);
				
				// Find a block that represents a simple condition
				if (scope.Contains(node)) {
					
					ILBasicBlock block = node.UserData as ILBasicBlock;
					
					if (block != null && block.Body.Count == 1) {
						
						ILExpression condBranch = block.Body[0] as ILExpression;
						
						// Switch
						if (condBranch != null && condBranch.Operand is ILLabel[] && condBranch.Arguments.Count > 0) {
							
							ILSwitch ilSwitch = new ILSwitch() {
								Condition = condBranch,
								DefaultGoto = block.FallthoughGoto
							};
							
							ControlFlowNode fallTarget = null;
							labelToCfNode.TryGetValue((ILLabel)block.FallthoughGoto.Operand, out fallTarget);
							
							HashSet<ControlFlowNode> frontiers = new HashSet<ControlFlowNode>();
							if (fallTarget != null)
								frontiers.UnionWith(fallTarget.DominanceFrontier);
							
							foreach(ILLabel condLabel in (ILLabel[])condBranch.Operand) {
								ControlFlowNode condTarget = null;
								labelToCfNode.TryGetValue(condLabel, out condTarget);
								if (condTarget != null)
									frontiers.UnionWith(condTarget.DominanceFrontier);
							}
							
							foreach(ILLabel condLabel in (ILLabel[])condBranch.Operand) {
								ControlFlowNode condTarget = null;
								labelToCfNode.TryGetValue(condLabel, out condTarget);
								
								ILBlock caseBlock = new ILBlock() {
									EntryGoto = new ILExpression(ILCode.Br, condLabel)
								};
								if (condTarget != null && !frontiers.Contains(condTarget)) {
									HashSet<ControlFlowNode> content = FindDominatedNodes(scope, condTarget);
									scope.ExceptWith(content);
									caseBlock.Body.AddRange(FindConditions(content, condTarget));
								}
								ilSwitch.CaseBlocks.Add(caseBlock);
							}
							
							// The labels will not be used - kill them
							condBranch.Operand = null;
							
							result.Add(new ILBasicBlock() {
								EntryLabel = block.EntryLabel,  // Keep the entry label
								Body = { ilSwitch }
							});
							scope.Remove(node);
						}
						
						// Two-way branch
						ILCondition ilCond;
						HashSet<ControlFlowNode> matchedNodes;
						ILLabel condEntryLabel;
						if (TryMatchCondition(scope, new ControlFlowNode[] {}, node, out ilCond, out matchedNodes, out condEntryLabel)) {
							
							// The branch labels will not be used - kill them
							foreach(ILExpression expr in ilCond.Condition.GetSelfAndChildrenRecursive<ILExpression>()) {
								if (expr.GetBranchTargets().Any()) {
									expr.Operand = null;
								}
							}
							
							ControlFlowNode trueTarget = null;
							labelToCfNode.TryGetValue((ILLabel)ilCond.TrueBlock.EntryGoto.Operand, out trueTarget);
							ControlFlowNode falseTarget = null;
							labelToCfNode.TryGetValue((ILLabel)ilCond.FalseBlock.EntryGoto.Operand, out falseTarget);
							
							// Pull in the conditional code
							HashSet<ControlFlowNode> frontiers = new HashSet<ControlFlowNode>();
							if (trueTarget != null)
								frontiers.UnionWith(trueTarget.DominanceFrontier);
							if (falseTarget != null)
								frontiers.UnionWith(falseTarget.DominanceFrontier);
							
							if (trueTarget != null && !frontiers.Contains(trueTarget)) {
								HashSet<ControlFlowNode> content = FindDominatedNodes(scope, trueTarget);
								scope.ExceptWith(content);
								ilCond.TrueBlock.Body.AddRange(FindConditions(content, trueTarget));
							}
							if (falseTarget != null && !frontiers.Contains(falseTarget)) {
								HashSet<ControlFlowNode> content = FindDominatedNodes(scope, falseTarget);
								scope.ExceptWith(content);
								ilCond.FalseBlock.Body.AddRange(FindConditions(content, falseTarget));
							}
							
							result.Add(new ILBasicBlock() {
								EntryLabel = condEntryLabel,  // Keep the entry label
								Body = { ilCond }
							});
							scope.ExceptWith(matchedNodes);
						}
					}
					
					// Add the node now so that we have good ordering
					if (scope.Contains(node)) {
						result.Add((ILNode)node.UserData);
						scope.Remove(node);
					}
				}

				// Using the dominator tree should ensure we find the the widest loop first
				foreach(var child in node.DominatorTreeChildren) {
					agenda.Add(child);
				}
			}
			
			// Add whatever is left
			foreach(var node in scope) {
				result.Add((ILNode)node.UserData);
			}
			
			return result;
		}
		
		bool TryMatchCondition(HashSet<ControlFlowNode> scope, IEnumerable<ControlFlowNode> scopeExcept, ControlFlowNode head, out ILCondition condition, out HashSet<ControlFlowNode> matchedNodes, out ILLabel entryLabel)
		{
			condition = null;
			matchedNodes = null;
			entryLabel = null;
			if (!scope.Contains(head) || scopeExcept.Contains(head))
				return false;
			
			ILBasicBlock basicBlock = head.UserData as ILBasicBlock;
			
			if (basicBlock == null || basicBlock.Body.Count != 1)
				return false;
			
			ILExpression condBranch = basicBlock.Body[0] as ILExpression;
			
			if (condBranch != null && condBranch.Operand is ILLabel && condBranch.Arguments.Count > 0) {
				
				// We have found a two-way condition
				condition = new ILCondition() {
				    Condition  = condBranch,
				    TrueBlock  = new ILBlock() { EntryGoto = new ILExpression(ILCode.Br, condBranch.Operand) },
				    FalseBlock = new ILBlock() { EntryGoto = new ILExpression(ILCode.Br, basicBlock.FallthoughGoto.Operand) }
				};
				// We are done with the node so "remove" it from scope
				scopeExcept  = scopeExcept.Union(new[] {head});
				matchedNodes = new HashSet<ControlFlowNode>() { head };
				entryLabel   = basicBlock.EntryLabel;
				
				// Optimize short-circut expressions
				while(true) {
					
					// Consider condition.TrueBlock
					{
						ILLabel nextLabel = (ILLabel)condition.TrueBlock.EntryGoto.Operand;
						ControlFlowNode nextTarget;
						labelToCfNode.TryGetValue(nextLabel, out nextTarget);				
						ILCondition nextCond;
						HashSet<ControlFlowNode> nextMatchedNodes;
						ILLabel nextEnteryLabel;
						if (nextTarget != null &&
							TryMatchCondition(scope, scopeExcept, nextTarget, out nextCond, out nextMatchedNodes, out nextEnteryLabel) &&
							labelRefCount[nextEnteryLabel] == 1)
						{
							if (condition.FalseBlock.EntryGoto.Operand == nextCond.FalseBlock.EntryGoto.Operand) {
						    		condition.Condition  = new ILExpression(ILCode.LogicAnd, null, condition.Condition, nextCond.Condition);
						    		condition.TrueBlock  = nextCond.TrueBlock;
						    		condition.FalseBlock = nextCond.FalseBlock;
						    		scopeExcept = scopeExcept.Union(nextMatchedNodes);
						    		matchedNodes.UnionWith(nextMatchedNodes);
						    		continue;
							}
							
							if (condition.FalseBlock.EntryGoto.Operand == nextCond.TrueBlock.EntryGoto.Operand) {
								condition.Condition  = new ILExpression(ILCode.LogicOr, null, new ILExpression(ILCode.LogicNot, null, condition.Condition), nextCond.Condition);
						    		condition.TrueBlock  = nextCond.TrueBlock;
						    		condition.FalseBlock = nextCond.FalseBlock;
						    		scopeExcept = scopeExcept.Union(nextMatchedNodes);
						    		matchedNodes.UnionWith(nextMatchedNodes);
						    		continue;
							}
						}
					}
					
					// Consider condition.FalseBlock
					{
						ILLabel nextLabel = (ILLabel)condition.FalseBlock.EntryGoto.Operand;
						ControlFlowNode nextTarget;
						labelToCfNode.TryGetValue(nextLabel, out nextTarget);				
						ILCondition nextCond;
						HashSet<ControlFlowNode> nextMatchedNodes;
						ILLabel nextEnteryLabel;
						if (nextTarget != null &&
							TryMatchCondition(scope, scopeExcept, nextTarget, out nextCond, out nextMatchedNodes, out nextEnteryLabel) &&
							labelRefCount[nextEnteryLabel] == 1)
						{
							if (condition.TrueBlock.EntryGoto.Operand == nextCond.FalseBlock.EntryGoto.Operand) {
								condition.Condition  = new ILExpression(ILCode.LogicAnd, null, new ILExpression(ILCode.LogicNot, null, condition.Condition), nextCond.Condition);
						    		condition.TrueBlock  = nextCond.TrueBlock;
						    		condition.FalseBlock = nextCond.FalseBlock;
						    		scopeExcept = scopeExcept.Union(nextMatchedNodes);
						    		matchedNodes.UnionWith(nextMatchedNodes);
						    		continue;
							}
							
							if (condition.TrueBlock.EntryGoto.Operand == nextCond.TrueBlock.EntryGoto.Operand) {
								condition.Condition  = new ILExpression(ILCode.LogicOr, null, condition.Condition, nextCond.Condition);
						    		condition.TrueBlock  = nextCond.TrueBlock;
						    		condition.FalseBlock = nextCond.FalseBlock;
						    		scopeExcept = scopeExcept.Union(nextMatchedNodes);
						    		matchedNodes.UnionWith(nextMatchedNodes);
						    		continue;
							}
						}
					}
					break;
				}
				return true;
			}
			return false;
		}
		
		static HashSet<ControlFlowNode> FindDominatedNodes(HashSet<ControlFlowNode> scope, ControlFlowNode head)
		{
			var exitNodes = head.DominanceFrontier.SelectMany(n => n.Predecessors);
			HashSet<ControlFlowNode> agenda = new HashSet<ControlFlowNode>(exitNodes);
			HashSet<ControlFlowNode> result = new HashSet<ControlFlowNode>();
			
			while(agenda.Count > 0) {
				ControlFlowNode addNode = agenda.First();
				agenda.Remove(addNode);
			
				if (scope.Contains(addNode) && head.Dominates(addNode) && result.Add(addNode)) {
					foreach (var predecessor in addNode.Predecessors) {
						agenda.Add(predecessor);
					}
				}
			}
			if (scope.Contains(head))
				result.Add(head);
			
			return result;
		}
		
		/// <summary>
		/// Flattens all nested basic blocks, except the the top level 'node' argument
		/// </summary>
		void FlattenBasicBlocks(ILNode node)
		{
			ILBlock block = node as ILBlock;
			if (block != null) {
				List<ILNode> flatBody = new List<ILNode>();
				foreach (ILNode child in block.GetChildren()) {
					FlattenBasicBlocks(child);
					if (child is ILBasicBlock) {
						flatBody.AddRange(child.GetChildren());
					} else {
						flatBody.Add(child);
					}
				}
				block.EntryGoto = null;
				block.Body = flatBody;
			} else if (node is ILExpression) {
				// Optimization - no need to check expressions
			} else if (node != null) {
				// Recursively find all ILBlocks
				foreach(ILNode child in node.GetChildren()) {
					FlattenBasicBlocks(child);
				}
			}
		}
		
		void SimpleGotoRemoval(ILBlock ast)
		{
			// TODO: Assign IL ranges from br to something else
			var blocks = ast.GetSelfAndChildrenRecursive<ILBlock>().ToList();
			foreach(ILBlock block in blocks) {
				for (int i = 0; i < block.Body.Count; i++) {
					ILExpression expr = block.Body[i] as ILExpression;
					// Uncoditional branch
					if (expr != null && (expr.Code == ILCode.Br)) {
						// Check that branch is followed by its label (allow multiple labels)
						for (int j = i + 1; j < block.Body.Count; j++) {
							ILLabel label = block.Body[j] as ILLabel;
							if (label == null)
								break;  // Can not optimize
							if (expr.Operand == label) {
								block.Body.RemoveAt(i);
								break;  // Branch removed
							}
						}
					}
				}
			}
		}
		
		void RemoveDeadLabels(ILBlock ast)
		{
			HashSet<ILLabel> liveLabels = new HashSet<ILLabel>(ast.GetSelfAndChildrenRecursive<ILExpression>().SelectMany(e => e.GetBranchTargets()));
			var blocks = ast.GetSelfAndChildrenRecursive<ILBlock>().ToList();
			foreach(ILBlock block in blocks) {
				for (int i = 0; i < block.Body.Count;) {
					ILLabel label = block.Body[i] as ILLabel;
					if (label != null && !liveLabels.Contains(label)) {
						block.Body.RemoveAt(i);
					} else {
						i++;
					}
				}
			}
		}
	}
}
