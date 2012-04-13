// Copyright (c) 2011 AlphaSierraPapa for the SharpDevelop Team
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

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
		
		readonly DecompilerContext context;
		
		uint nextLabelIndex = 0;
		
		public LoopsAndConditions(DecompilerContext context)
		{
			this.context = context;
		}
		
		public void FindLoops(ILBlock block)
		{
			if (block.Body.Count > 0) {
				ControlFlowGraph graph;
				graph = BuildGraph(block.Body, (ILLabel)block.EntryGoto.Operand);
				graph.ComputeDominance(context.CancellationToken);
				graph.ComputeDominanceFrontier();
				block.Body = FindLoops(new HashSet<ControlFlowNode>(graph.Nodes.Skip(3)), graph.EntryPoint, false);
			}
		}
		
		public void FindConditions(ILBlock block)
		{
			if (block.Body.Count > 0) {
				ControlFlowGraph graph;
				graph = BuildGraph(block.Body, (ILLabel)block.EntryGoto.Operand);
				graph.ComputeDominance(context.CancellationToken);
				graph.ComputeDominanceFrontier();
				block.Body = FindConditions(new HashSet<ControlFlowNode>(graph.Nodes.Skip(3)), graph.EntryPoint);
			}
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
			foreach(ILBasicBlock node in nodes) {
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
			foreach(ILBasicBlock node in nodes) {
				ControlFlowNode source = astNodeToCfNode[node];
				
				// Find all branches
				foreach(ILLabel target in node.GetSelfAndChildrenRecursive<ILExpression>(e => e.IsBranch()).SelectMany(e => e.GetBranchTargets())) {
					ControlFlowNode destination;
					// Labels which are out of out scope will not be in the collection
					// Insert self edge only if we are sure we are a loop
					if (labelToCfNode.TryGetValue(target, out destination) && (destination != source || target == node.Body.FirstOrDefault())) {
						ControlFlowEdge edge = new ControlFlowEdge(source, destination, JumpType.Normal);
						source.Outgoing.Add(edge);
						destination.Incoming.Add(edge);
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
					// It has to be just brtrue - any preceding code would introduce goto
					if(basicBlock.MatchSingleAndBr(ILCode.Brtrue, out trueLabel, out condExpr, out falseLabel))
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
							if (loopContents.Contains(falseTarget) || falseTarget == node)
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
							
							// Use loop to implement the brtrue
							basicBlock.Body.RemoveTail(ILCode.Brtrue, ILCode.Br);
							basicBlock.Body.Add(new ILWhileLoop() {
								Condition = condExpr,
								BodyBlock = new ILBlock() {
									EntryGoto = new ILExpression(ILCode.Br, trueLabel),
									Body = FindLoops(loopContents, node, false)
								}
							});
							basicBlock.Body.Add(new ILExpression(ILCode.Br, falseLabel));
							result.Add(basicBlock);
							
							scope.ExceptWith(loopContents);
						}
					}
					
					// Fallback method: while(true)
					if (scope.Contains(node)) {
						result.Add(new ILBasicBlock() {
							Body = new List<ILNode>() {
								new ILLabel() { Name = "Loop_" + (nextLabelIndex++) },
								new ILWhileLoop() {
									BodyBlock = new ILBlock() {
										EntryGoto = new ILExpression(ILCode.Br, (ILLabel)basicBlock.Body.First()),
										Body = FindLoops(loopContents, node, true)
									}
								},
							},
						});
						
						scope.ExceptWith(loopContents);
					}
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
			
			Stack<ControlFlowNode> agenda  = new Stack<ControlFlowNode>();
			agenda.Push(entryNode);
			while(agenda.Count > 0) {
				ControlFlowNode node = agenda.Pop();
				
				// Find a block that represents a simple condition
				if (scope.Contains(node)) {
					
					ILBasicBlock block = (ILBasicBlock)node.UserData;
					
					{
						// Switch
						ILLabel[] caseLabels;
						ILExpression switchArg;
						ILLabel fallLabel;
						if (block.MatchLastAndBr(ILCode.Switch, out caseLabels, out switchArg, out fallLabel)) {
							
							// Replace the switch code with ILSwitch
							ILSwitch ilSwitch = new ILSwitch() { Condition = switchArg };
							block.Body.RemoveTail(ILCode.Switch, ILCode.Br);
							block.Body.Add(ilSwitch);
							block.Body.Add(new ILExpression(ILCode.Br, fallLabel));
							result.Add(block);

							// Remove the item so that it is not picked up as content
							scope.RemoveOrThrow(node);
							
							// Find the switch offset
							int addValue = 0;
							List<ILExpression> subArgs;
							if (ilSwitch.Condition.Match(ILCode.Sub, out subArgs) && subArgs[1].Match(ILCode.Ldc_I4, out addValue)) {
								ilSwitch.Condition = subArgs[0];
							}
							
							// Pull in code of cases
							ControlFlowNode fallTarget = null;
							labelToCfNode.TryGetValue(fallLabel, out fallTarget);
							
							HashSet<ControlFlowNode> frontiers = new HashSet<ControlFlowNode>();
							if (fallTarget != null)
								frontiers.UnionWith(fallTarget.DominanceFrontier.Except(new [] { fallTarget }));
							
							foreach(ILLabel condLabel in caseLabels) {
								ControlFlowNode condTarget = null;
								labelToCfNode.TryGetValue(condLabel, out condTarget);
								if (condTarget != null)
									frontiers.UnionWith(condTarget.DominanceFrontier.Except(new [] { condTarget }));
							}
							
							for (int i = 0; i < caseLabels.Length; i++) {
								ILLabel condLabel = caseLabels[i];
								
								// Find or create new case block
								ILSwitch.CaseBlock caseBlock = ilSwitch.CaseBlocks.FirstOrDefault(b => b.EntryGoto.Operand == condLabel);
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
										caseBlock.Body.Add(new ILBasicBlock() {
											Body = {
												new ILLabel() { Name = "SwitchBreak_" + (nextLabelIndex++) },
												new ILExpression(ILCode.LoopOrSwitchBreak, null)
											}
										});
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
									block.Body.RemoveTail(ILCode.Br);
									
									scope.ExceptWith(content);
									caseBlock.Body.AddRange(FindConditions(content, fallTarget));
									// Add explicit break which should not be used by default, but the goto removal might decide to use it
									caseBlock.Body.Add(new ILBasicBlock() {
										Body = {
											new ILLabel() { Name = "SwitchBreak_" + (nextLabelIndex++) },
											new ILExpression(ILCode.LoopOrSwitchBreak, null)
										}
									});
								}
							}
						}
						
						// Two-way branch
						ILExpression condExpr;
						ILLabel trueLabel;
						ILLabel falseLabel;
						if(block.MatchLastAndBr(ILCode.Brtrue, out trueLabel, out condExpr, out falseLabel)) {
							
							// Swap bodies since that seems to be the usual C# order
							ILLabel temp = trueLabel;
							trueLabel = falseLabel;
							falseLabel = temp;
							condExpr = new ILExpression(ILCode.LogicNot, null, condExpr);
							
							// Convert the brtrue to ILCondition
							ILCondition ilCond = new ILCondition() {
								Condition  = condExpr,
								TrueBlock  = new ILBlock() { EntryGoto = new ILExpression(ILCode.Br, trueLabel) },
								FalseBlock = new ILBlock() { EntryGoto = new ILExpression(ILCode.Br, falseLabel) }
							};
							block.Body.RemoveTail(ILCode.Brtrue, ILCode.Br);
							block.Body.Add(ilCond);
							result.Add(block);
							
							// Remove the item immediately so that it is not picked up as content
							scope.RemoveOrThrow(node);
							
							ControlFlowNode trueTarget = null;
							labelToCfNode.TryGetValue(trueLabel, out trueTarget);
							ControlFlowNode falseTarget = null;
							labelToCfNode.TryGetValue(falseLabel, out falseTarget);
							
							// Pull in the conditional code
							if (trueTarget != null && HasSingleEdgeEnteringBlock(trueTarget)) {
								HashSet<ControlFlowNode> content = FindDominatedNodes(scope, trueTarget);
								scope.ExceptWith(content);
								ilCond.TrueBlock.Body.AddRange(FindConditions(content, trueTarget));
							}
							if (falseTarget != null && HasSingleEdgeEnteringBlock(falseTarget)) {
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

				// depth-first traversal of dominator tree
				for (int i = node.DominatorTreeChildren.Count - 1; i >= 0; i--) {
					agenda.Push(node.DominatorTreeChildren[i]);
				}
			}
			
			// Add whatever is left
			foreach(var node in scope) {
				result.Add((ILNode)node.UserData);
			}
			
			return result;
		}
		
		static bool HasSingleEdgeEnteringBlock(ControlFlowNode node)
		{
			return node.Incoming.Count(edge => !node.Dominates(edge.Source)) == 1;
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
