// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Linq;

using ICSharpCode.Decompiler.FlowAnalysis;

namespace ICSharpCode.Decompiler.ILAst
{
	/// <summary>
	/// Description of LoopsAndConditions.
	/// </summary>
	public class LoopsAndConditions
	{
		Dictionary<ILLabel, ControlFlowNode> labelToCfNode = new Dictionary<ILLabel, ControlFlowNode>();
		
		DecompilerContext context;
		
		uint nextLabelIndex = 0;
		
		public LoopsAndConditions(DecompilerContext context)
		{
			this.context = context;
		}
		
		public void FindLoops(ILBlock block)
		{
			ControlFlowGraph graph;
			graph = BuildGraph(block.Body, (ILLabel)block.EntryGoto.Operand);
			graph.ComputeDominance(context.CancellationToken);
			graph.ComputeDominanceFrontier();
			block.Body = FindLoops(new HashSet<ControlFlowNode>(graph.Nodes.Skip(3)), graph.EntryPoint, false);
		}
		
		public void FindConditions(ILBlock block)
		{
			ControlFlowGraph graph;
			graph = BuildGraph(block.Body, (ILLabel)block.EntryGoto.Operand);
			graph.ComputeDominance(context.CancellationToken);
			graph.ComputeDominanceFrontier();
			block.Body = FindConditions(new HashSet<ControlFlowNode>(graph.Nodes.Skip(3)), graph.EntryPoint);
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
				
				// If the node is a loop header
				if (scope.Contains(node)
				    && node.DominanceFrontier.Contains(node)
				    && (node != entryPoint || !excludeEntryPoint))
				{
					HashSet<ControlFlowNode> loopContents = FindLoopContent(scope, node);
					
					// If the first expression is a loop condition
					ILBasicBlock basicBlock = (ILBasicBlock)node.UserData;
					ILExpression condExpr;
					ILLabel trueLabel;
					ILLabel falseLabel;
					if(basicBlock.Match(ILCode.Brtrue, out trueLabel, out condExpr, out falseLabel))
					{
						ControlFlowNode trueTarget;
						labelToCfNode.TryGetValue(trueLabel, out trueTarget);
						ControlFlowNode falseTarget;
						labelToCfNode.TryGetValue(falseLabel, out falseTarget);
						
						// If one point inside the loop and the other outside
						if ((!loopContents.Contains(trueTarget) && loopContents.Contains(falseTarget)) ||
						    (loopContents.Contains(trueTarget) && !loopContents.Contains(falseTarget)) )
						{
							loopContents.RemoveOrThrow(node);
							scope.RemoveOrThrow(node);
							
							// If false means enter the loop
							if (loopContents.Contains(falseTarget))
							{
								// Negate the condition
								condExpr = new ILExpression(ILCode.LogicNot, null, condExpr);
								ILLabel tmp = trueLabel;
								trueLabel = falseLabel;
								falseLabel = tmp;
							}
							
							ControlFlowNode postLoopTarget;
							labelToCfNode.TryGetValue(falseLabel, out postLoopTarget);
							if (postLoopTarget != null) {
								// Pull more nodes into the loop
								HashSet<ControlFlowNode> postLoopContents = FindDominatedNodes(scope, postLoopTarget);
								var pullIn = scope.Except(postLoopContents).Where(n => node.Dominates(n));
								loopContents.UnionWith(pullIn);
							}
							
							// Use loop to implement the condition
							result.Add(new ILBasicBlock() {
							           	EntryLabel = basicBlock.EntryLabel,
							           	Body = new List<ILNode>() {
							           		new ILWhileLoop() {
							           			Condition = condExpr,
							           			BodyBlock = new ILBlock() {
							           				EntryGoto = new ILExpression(ILCode.Br, trueLabel),
							           				Body = FindLoops(loopContents, node, true)
							           			}
							           		},
							           		new ILExpression(ILCode.Br, falseLabel)
							           	},
							           	FallthoughGoto = null
							           });
						}
					}
					
					// Fallback method: while(true)
					if (scope.Contains(node)) {
						result.Add(new ILBasicBlock() {
						           	EntryLabel = new ILLabel() { Name = "Loop_" + (nextLabelIndex++) },
						           	Body = new List<ILNode>() {
						           		new ILWhileLoop() {
						           			BodyBlock = new ILBlock() {
						           				EntryGoto = new ILExpression(ILCode.Br, basicBlock.EntryLabel),
						           				Body = FindLoops(loopContents, node, true)
						           			}
						           		},
						           	},
						           	FallthoughGoto = null
						           });
					}
					
					// Move the content into loop block
					scope.ExceptWith(loopContents);
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
						ILLabel[] caseLabels;
						List<ILExpression> switchArgs;
						if (condBranch.Match(ILCode.Switch, out caseLabels, out switchArgs)) {
							
							ILSwitch ilSwitch = new ILSwitch() { Condition = switchArgs.Single() };
							ILBasicBlock newBB = new ILBasicBlock() {
								EntryLabel = block.EntryLabel,  // Keep the entry label
								Body = { ilSwitch },
								FallthoughGoto = block.FallthoughGoto
							};
							result.Add(newBB);

							// Remove the item so that it is not picked up as content
							scope.RemoveOrThrow(node);
							
							// Find the switch offset
							int addValue = 0;
							List<ILExpression> subArgs;
							if (ilSwitch.Condition.Match(ILCode.Sub, out subArgs) && subArgs[1].Match(ILCode.Ldc_I4, out addValue)) {
								ilSwitch.Condition = subArgs[0];
							}
							
							// Pull in code of cases
							ILLabel fallLabel = (ILLabel)block.FallthoughGoto.Operand;
							ControlFlowNode fallTarget = null;
							labelToCfNode.TryGetValue(fallLabel, out fallTarget);
							
							HashSet<ControlFlowNode> frontiers = new HashSet<ControlFlowNode>();
							if (fallTarget != null)
								frontiers.UnionWith(fallTarget.DominanceFrontier);
							
							foreach(ILLabel condLabel in caseLabels) {
								ControlFlowNode condTarget = null;
								labelToCfNode.TryGetValue(condLabel, out condTarget);
								if (condTarget != null)
									frontiers.UnionWith(condTarget.DominanceFrontier);
							}
							
							for (int i = 0; i < caseLabels.Length; i++) {
								ILLabel condLabel = caseLabels[i];
								
								// Find or create new case block
								ILSwitch.CaseBlock caseBlock = ilSwitch.CaseBlocks.Where(b => b.EntryGoto.Operand == condLabel).FirstOrDefault();
								if (caseBlock == null) {
									caseBlock = new ILSwitch.CaseBlock() {
										Values = new List<int>(),
										EntryGoto = new ILExpression(ILCode.Br, condLabel)
									};
									ilSwitch.CaseBlocks.Add(caseBlock);
									
									ControlFlowNode condTarget = null;
									labelToCfNode.TryGetValue(condLabel, out condTarget);
									if (condTarget != null && !frontiers.Contains(condTarget)) {
										HashSet<ControlFlowNode> content = FindDominatedNodes(scope, condTarget);
										scope.ExceptWith(content);
										caseBlock.Body.AddRange(FindConditions(content, condTarget));
										// Add explicit break which should not be used by default, but the goto removal might decide to use it
										caseBlock.Body.Add(new ILBasicBlock() { Body = { new ILExpression(ILCode.LoopOrSwitchBreak, null) } });
									}
								}
								caseBlock.Values.Add(i + addValue);
							}
							
							// Heuristis to determine if we want to use fallthough as default case
							if (fallTarget != null && !frontiers.Contains(fallTarget)) {
								HashSet<ControlFlowNode> content = FindDominatedNodes(scope, fallTarget);
								if (content.Any()) {
									var caseBlock = new ILSwitch.CaseBlock() { EntryGoto = new ILExpression(ILCode.Br, fallLabel) };
									ilSwitch.CaseBlocks.Add(caseBlock);
									newBB.FallthoughGoto = null;
									
									scope.ExceptWith(content);
									caseBlock.Body.AddRange(FindConditions(content, fallTarget));
									// Add explicit break which should not be used by default, but the goto removal might decide to use it
									caseBlock.Body.Add(new ILBasicBlock() { Body = { new ILExpression(ILCode.LoopOrSwitchBreak, null) } });
								}
							}
						}
						
						// Two-way branch
						ILExpression condExpr;
						ILLabel trueLabel;
						ILLabel falseLabel;
						if(block.Match(ILCode.Brtrue, out trueLabel, out condExpr, out falseLabel)) {
							
							// Swap bodies since that seems to be the usual C# order
							ILLabel temp = trueLabel;
							trueLabel = falseLabel;
							falseLabel = temp;
							condExpr = new ILExpression(ILCode.LogicNot, null, condExpr);
							
							// Convert the basic block to ILCondition
							ILCondition ilCond = new ILCondition() {
								Condition  = condExpr,
								TrueBlock  = new ILBlock() { EntryGoto = new ILExpression(ILCode.Br, trueLabel) },
								FalseBlock = new ILBlock() { EntryGoto = new ILExpression(ILCode.Br, falseLabel) }
							};
							result.Add(new ILBasicBlock() {
							           	EntryLabel = block.EntryLabel,  // Keep the entry label
							           	Body = { ilCond }
							           });
							
							// Remove the item immediately so that it is not picked up as content
							scope.RemoveOrThrow(node);
							
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
			var viaBackEdges = head.Predecessors.Where(p => head.Dominates(p));
			HashSet<ControlFlowNode> agenda = new HashSet<ControlFlowNode>(viaBackEdges);
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
	}
}
