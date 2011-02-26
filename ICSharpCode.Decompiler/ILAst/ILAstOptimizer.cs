using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using ICSharpCode.Decompiler.FlowAnalysis;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Decompiler.ControlFlow
{
	public enum ILAstOptimizationStep
	{
		SplitToMovableBlocks,
		ShortCircuits,
		FindLoops,
		FindConditions,
		FlattenNestedMovableBlocks,
		SimpleGotoRemoval,
		RemoveDeadLabels,
		HandleArrayInitializers,
		TypeInference,
		None
	}
	
	public class ILAstOptimizer
	{
		Dictionary<ILLabel, ControlFlowNode> labelToCfNode = new Dictionary<ILLabel, ControlFlowNode>();
		
		public void Optimize(DecompilerContext context, ILBlock method, ILAstOptimizationStep abortBeforeStep = ILAstOptimizationStep.None)
		{
			if (abortBeforeStep == ILAstOptimizationStep.SplitToMovableBlocks) return;
			foreach(ILBlock block in method.GetSelfAndChildrenRecursive<ILBlock>().ToList()) {
				SplitToBasicBlocks(block);
			}
			
			if (abortBeforeStep == ILAstOptimizationStep.ShortCircuits) return;
			OptimizeShortCircuits(method);
			
			if (abortBeforeStep == ILAstOptimizationStep.FindLoops) return;
			foreach(ILBlock block in method.GetSelfAndChildrenRecursive<ILBlock>().ToList()) {
				ControlFlowGraph graph;
				graph = BuildGraph(block.Body, (ILLabel)block.EntryGoto.Operand);
				graph.ComputeDominance(context.CancellationToken);
				graph.ComputeDominanceFrontier();
				block.Body = FindLoops(new HashSet<ControlFlowNode>(graph.Nodes.Skip(3)), graph.EntryPoint, false);
			}
			
			if (abortBeforeStep == ILAstOptimizationStep.FindConditions) return;
			foreach(ILBlock block in method.GetSelfAndChildrenRecursive<ILBlock>().ToList()) {
				ControlFlowGraph graph;
				graph = BuildGraph(block.Body, (ILLabel)block.EntryGoto.Operand);
				// TODO: Fix
				if (graph == null)
					continue;
				graph.ComputeDominance(context.CancellationToken);
				graph.ComputeDominanceFrontier();
				block.Body = FindConditions(new HashSet<ControlFlowNode>(graph.Nodes.Skip(3)), graph.EntryPoint);
			}
			
			if (abortBeforeStep == ILAstOptimizationStep.FlattenNestedMovableBlocks) return;
			FlattenBasicBlocks(method);
			
			if (abortBeforeStep == ILAstOptimizationStep.SimpleGotoRemoval) return;
			SimpleGotoRemoval(method);
			
			if (abortBeforeStep == ILAstOptimizationStep.RemoveDeadLabels) return;
			RemoveDeadLabels(method);
			
			if (abortBeforeStep == ILAstOptimizationStep.HandleArrayInitializers) return;
			ArrayInitializers.Transform(method);
			
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
					
					// Insert split
					if (currNode is ILLabel ||
					    lastNode is ILTryCatchBlock ||
					    currNode is ILTryCatchBlock ||
					    (lastNode is ILExpression) && ((ILExpression)lastNode).IsBranch() ||
					    (currNode is ILExpression) && (((ILExpression)currNode).IsBranch() && ((ILExpression)currNode).Code.CanFallThough() && basicBlock.Body.Count > 0))
					{
						ILBasicBlock lastBlock = basicBlock;
						basicBlock = new ILBasicBlock();
						basicBlocks.Add(basicBlock);
						
						if (currNode is ILLabel) {
							// Insert as entry label
							basicBlock.EntryLabel = (ILLabel)currNode;
						} else {
							basicBlock.EntryLabel = new ILLabel() { Name = "Block_" + (nextBlockIndex++) };
							basicBlock.Body.Add(currNode);
						}
						
						// Explicit branch from one block to other
						// (unless the last expression was unconditional branch)
						if (!(lastNode is ILExpression) || ((ILExpression)lastNode).Code.CanFallThough()) {
							lastBlock.FallthoughGoto = new ILExpression(ILCode.Br, basicBlock.EntryLabel);
						}
					} else {
						basicBlock.Body.Add(currNode);						
					}
				}
			}
			
			block.Body = basicBlocks;
			return;
		}
		
		void OptimizeShortCircuits(ILBlock method)
		{
			AnalyseLabels(method);
			
			foreach(ILBlock block in method.GetSelfAndChildrenRecursive<ILBlock>().ToList()) {
				bool modified;
				do {
					modified = false;
					for (int i = 0; i < block.Body.Count;) {
						if (TrySimplifyShortCircuit(block.Body, (ILBasicBlock)block.Body[i])) {
							modified = true;
						} else {
							i++;
						}
					}
				} while(modified);
			}
		}
		
		Dictionary<ILLabel, int> labelGlobalRefCount;
		Dictionary<ILLabel, ILBasicBlock> labelToBasicBlock;
		
		void AnalyseLabels(ILBlock method)
		{
			labelGlobalRefCount = new Dictionary<ILLabel, int>();
			foreach(ILLabel target in method.GetSelfAndChildrenRecursive<ILExpression>().SelectMany(e => e.GetBranchTargets())) {
				if (!labelGlobalRefCount.ContainsKey(target))
					labelGlobalRefCount[target] = 0;
				labelGlobalRefCount[target]++;
			}
			
			labelToBasicBlock = new Dictionary<ILLabel, ILBasicBlock>();
			foreach(ILBasicBlock bb in method.GetSelfAndChildrenRecursive<ILBasicBlock>()) {
				foreach(ILLabel label in bb.GetChildren().OfType<ILLabel>()) {
					labelToBasicBlock[label] = bb;
				}
			}
		}
		
		bool IsConditionalBranch(ILBasicBlock bb, ref ILExpression branchExpr, ref ILLabel trueLabel, ref ILLabel falseLabel)
		{
			if (bb.Body.Count == 1) {
				branchExpr = bb.Body[0] as ILExpression;
				if (branchExpr != null && branchExpr.Operand is ILLabel && branchExpr.Arguments.Count > 0) {
					trueLabel  = (ILLabel)branchExpr.Operand;
					falseLabel = (ILLabel)((ILExpression)bb.FallthoughGoto).Operand;
					return true;
				}
			}
			return false;
		}
		
		// scope is modified if successful
		bool TrySimplifyShortCircuit(List<ILNode> scope, ILBasicBlock head)
		{
			Debug.Assert(scope.Contains(head));
			
			ILExpression branchExpr = null;
			ILLabel trueLabel = null;
			ILLabel falseLabel = null;
			if(IsConditionalBranch(head, ref branchExpr, ref trueLabel, ref falseLabel)) {
				for (int pass = 0; pass < 2; pass++) {
					
					// On the second pass, swap labels and negate expression of the first branch
					// It is slightly ugly, but much better then copy-pasting this whole block
					ILLabel nextLabel   = (pass == 0) ? trueLabel  : falseLabel;
					ILLabel otherLablel = (pass == 0) ? falseLabel : trueLabel;
					bool    negate      = (pass == 1);
					
					ILBasicBlock nextBasicBlock = labelToBasicBlock[nextLabel];
					ILExpression nextBranchExpr = null;
					ILLabel nextTrueLablel = null;
					ILLabel nextFalseLabel = null;
					if (scope.Contains(nextBasicBlock) &&
					    nextBasicBlock != head &&
					    labelGlobalRefCount[nextBasicBlock.EntryLabel] == 1 &&
					    IsConditionalBranch(nextBasicBlock, ref nextBranchExpr, ref nextTrueLablel, ref nextFalseLabel) &&
					    (otherLablel == nextFalseLabel || otherLablel == nextTrueLablel))
					{
						// We are using the branches as expressions now, so do not keep their labels alive
						branchExpr.Operand = null;
						nextBranchExpr.Operand = null;
						
						// Create short cicuit branch
						if (otherLablel == nextFalseLabel) {
							head.Body[0] = new ILExpression(ILCode.BrLogicAnd, nextTrueLablel, negate ? new ILExpression(ILCode.LogicNot, null, branchExpr) : branchExpr, nextBranchExpr);
						} else {
							head.Body[0] = new ILExpression(ILCode.BrLogicOr, nextTrueLablel, negate ? branchExpr : new ILExpression(ILCode.LogicNot, null, branchExpr), nextBranchExpr);
						}
						head.FallthoughGoto = new ILExpression(ILCode.Br, nextFalseLabel);
						
						// Remove the inlined branch from scope
						labelGlobalRefCount[nextBasicBlock.EntryLabel] = 0;
						if (!scope.Remove(nextBasicBlock))
							throw new Exception("Element not found");
						
						return true;
					}
				}
			}
			return false;
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
			
			// Do not modify entry data
			scope = new HashSet<ControlFlowNode>(scope);
			
			Queue<ControlFlowNode> agenda  = new Queue<ControlFlowNode>();
			agenda.Enqueue(entryPoint);
			while(agenda.Count > 0) {
				ControlFlowNode node = agenda.Dequeue();
				
				if (scope.Contains(node)
				    && node.DominanceFrontier.Contains(node)
				    && (node != entryPoint || !excludeEntryPoint))
				{
					HashSet<ControlFlowNode> loopContents = FindLoopContent(scope, node);
					
					ILWhileLoop loop = new ILWhileLoop();
					
					ILBasicBlock basicBlock = node.UserData as ILBasicBlock;
					ILExpression branchExpr = null;
					ILLabel trueLabel = null;
					ILLabel falseLabel = null;
					if(basicBlock != null && IsConditionalBranch(basicBlock, ref branchExpr, ref trueLabel, ref falseLabel)) {
						loopContents.Remove(node);
						scope.Remove(node);
						branchExpr.Operand = null;  // Do not keep label alive
						
						// Use loop to implement condition
						loop.Condition      = branchExpr;
						loop.PreLoopLabel   = basicBlock.EntryLabel;
						loop.PostLoopGoto   = new ILExpression(ILCode.Br, falseLabel);
						loop.BodyBlock      = new ILBlock() { EntryGoto = new ILExpression(ILCode.Br, trueLabel) };
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
			scope.Clear();
			
			return result;
		}
		
		List<ILNode> FindConditions(HashSet<ControlFlowNode> scope, ControlFlowNode entryNode)
		{
			List<ILNode> result = new List<ILNode>();
			
			// Do not modify entry data
			scope = new HashSet<ControlFlowNode>(scope);
			
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
							
							ILLabel[] caseLabels = (ILLabel[])condBranch.Operand;
							
							// The labels will not be used - kill them
							condBranch.Operand = null;
							
							ILSwitch ilSwitch = new ILSwitch() {
								Condition = condBranch,
								DefaultGoto = block.FallthoughGoto
							};
							result.Add(new ILBasicBlock() {
								EntryLabel = block.EntryLabel,  // Keep the entry label
								Body = { ilSwitch }
							});

							// Remove the item so that it is not picked up as content
							if (!scope.Remove(node))
								throw new Exception("Item is not in set");
							
							// Pull in code of cases
							ControlFlowNode fallTarget = null;
							labelToCfNode.TryGetValue((ILLabel)block.FallthoughGoto.Operand, out fallTarget);
							
							HashSet<ControlFlowNode> frontiers = new HashSet<ControlFlowNode>();
							if (fallTarget != null)
								frontiers.UnionWith(fallTarget.DominanceFrontier);
							
							foreach(ILLabel condLabel in caseLabels) {
								ControlFlowNode condTarget = null;
								labelToCfNode.TryGetValue(condLabel, out condTarget);
								if (condTarget != null)
									frontiers.UnionWith(condTarget.DominanceFrontier);
							}
							
							foreach(ILLabel condLabel in caseLabels) {
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
						}
						
						// Two-way branch
						ILExpression branchExpr = null;
						ILLabel trueLabel = null;
						ILLabel falseLabel = null;
						if(IsConditionalBranch(block, ref branchExpr, ref trueLabel, ref falseLabel)) {
							
							// The branch label will not be used - kill it
							branchExpr.Operand = null;
							
							// Convert the basic block to ILCondition
							ILCondition ilCond = new ILCondition() {
								Condition  = branchExpr,
								TrueBlock  = new ILBlock() { EntryGoto = new ILExpression(ILCode.Br, trueLabel) },
								FalseBlock = new ILBlock() { EntryGoto = new ILExpression(ILCode.Br, falseLabel) }
							};
							result.Add(new ILBasicBlock() {
									EntryLabel = block.EntryLabel,  // Keep the entry label
									Body = { ilCond }
							});
							
							// Remove the item immediately so that it is not picked up as content
							if (!scope.Remove(node))
								throw new Exception("Item is not in set");
							
							ControlFlowNode trueTarget = null;
							labelToCfNode.TryGetValue(trueLabel, out trueTarget);
							ControlFlowNode falseTarget = null;
							labelToCfNode.TryGetValue(falseLabel, out falseTarget);
							
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
							
							if (scope.Count == 0) {
								// We have removed the whole scope - eliminte one of the condition bodies
								int trueSize = ilCond.TrueBlock.GetSelfAndChildrenRecursive<ILNode>().Count();
								int falseSize = ilCond.FalseBlock.GetSelfAndChildrenRecursive<ILNode>().Count();
								
								// The block are protected
								Debug.Assert(ilCond.TrueBlock.EntryGoto != null);
								Debug.Assert(ilCond.FalseBlock.EntryGoto != null);
								
								if (falseSize > trueSize) {
									// Move the false body out
									result.AddRange(ilCond.FalseBlock.Body);
									ilCond.FalseBlock.Body.Clear();
								} else {
									// Move the true body out
									result.AddRange(ilCond.TrueBlock.Body);
									ilCond.TrueBlock.Body.Clear();
								}
							}
							
							// If true body is empty, swap bodies.
							// Might happend because there was not any to start with or we moved it out.
							if (ilCond.TrueBlock.Body.Count == 0 && ilCond.FalseBlock.Body.Count > 0) {
								ILBlock tmp = ilCond.TrueBlock;
								ilCond.TrueBlock = ilCond.FalseBlock;
								ilCond.FalseBlock = tmp;
								ilCond.Condition = new ILExpression(ILCode.LogicNot, null, ilCond.Condition);
							}
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
		
		static HashSet<ControlFlowNode> FindDominatedNodes(HashSet<ControlFlowNode> scope, ControlFlowNode head)
		{
			HashSet<ControlFlowNode> agenda = new HashSet<ControlFlowNode>();
			HashSet<ControlFlowNode> result = new HashSet<ControlFlowNode>();
			agenda.Add(head);
			
			while(agenda.Count > 0) {
				ControlFlowNode addNode = agenda.First();
				agenda.Remove(addNode);
				
				if (scope.Contains(addNode) && head.Dominates(addNode) && result.Add(addNode)) {
					foreach (var successor in addNode.Successors) {
						agenda.Add(successor);
					}
				}
			}
			
			return result;
		}
		
		static HashSet<ControlFlowNode> FindLoopContent(HashSet<ControlFlowNode> scope, ControlFlowNode head)
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
