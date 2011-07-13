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
using System.Diagnostics;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace ICSharpCode.Decompiler.ILAst
{
	public class GotoRemoval
	{
		Dictionary<ILNode, ILNode> parent = new Dictionary<ILNode, ILNode>();
		Dictionary<ILNode, ILNode> nextSibling = new Dictionary<ILNode, ILNode>();
		
		public void RemoveGotos(ILBlock method)
		{
			// Build the navigation data
			parent[method] = null;
			foreach (ILNode node in method.GetSelfAndChildrenRecursive<ILNode>()) {
				ILNode previousChild = null;
				foreach (ILNode child in node.GetChildren()) {
					if (parent.ContainsKey(child))
						throw new Exception("The following expression is linked from several locations: " + child.ToString());
					parent[child] = node;
					if (previousChild != null)
						nextSibling[previousChild] = child;
					previousChild = child;
				}
				if (previousChild != null)
					nextSibling[previousChild] = null;
			}
			
			// Simplify gotos
			bool modified;
			do {
				modified = false;
				foreach (ILExpression gotoExpr in method.GetSelfAndChildrenRecursive<ILExpression>(e => e.Code == ILCode.Br || e.Code == ILCode.Leave)) {
					modified |= TrySimplifyGoto(gotoExpr);
				}
			} while(modified);
			
			RemoveRedundantCode(method);
		}
		
		public static void RemoveRedundantCode(ILBlock method)
		{
			// Remove dead lables and nops
			HashSet<ILLabel> liveLabels = new HashSet<ILLabel>(method.GetSelfAndChildrenRecursive<ILExpression>(e => e.IsBranch()).SelectMany(e => e.GetBranchTargets()));
			foreach(ILBlock block in method.GetSelfAndChildrenRecursive<ILBlock>()) {
				block.Body = block.Body.Where(n => !n.Match(ILCode.Nop) && !(n is ILLabel && !liveLabels.Contains((ILLabel)n))).ToList();
			}
			
			// Remove redundant continue
			foreach(ILWhileLoop loop in method.GetSelfAndChildrenRecursive<ILWhileLoop>()) {
				var body = loop.BodyBlock.Body;
				if (body.Count > 0 && body.Last().Match(ILCode.LoopContinue)) {
					body.RemoveAt(body.Count - 1);
				}
			}
			
			// Remove redundant break at the end of case
			// Remove redundant case blocks altogether
			foreach(ILSwitch ilSwitch in method.GetSelfAndChildrenRecursive<ILSwitch>()) {
				foreach(ILBlock ilCase in ilSwitch.CaseBlocks) {
					Debug.Assert(ilCase.EntryGoto == null);
					
					int count = ilCase.Body.Count;
					if (count >= 2) {
						if (ilCase.Body[count - 2].IsUnconditionalControlFlow() &&
						    ilCase.Body[count - 1].Match(ILCode.LoopOrSwitchBreak)) 
						{
							ilCase.Body.RemoveAt(count - 1);
						}
					}
				}
				
				var defaultCase = ilSwitch.CaseBlocks.SingleOrDefault(cb => cb.Values == null);
				// If there is no default block, remove empty case blocks
				if (defaultCase == null || (defaultCase.Body.Count == 1 && defaultCase.Body.Single().Match(ILCode.LoopOrSwitchBreak))) {
					ilSwitch.CaseBlocks.RemoveAll(b => b.Body.Count == 1 && b.Body.Single().Match(ILCode.LoopOrSwitchBreak));
				}
			}
			
			// Remove redundant return at the end of method
			if (method.Body.Count > 0 && method.Body.Last().Match(ILCode.Ret) && ((ILExpression)method.Body.Last()).Arguments.Count == 0) {
				method.Body.RemoveAt(method.Body.Count - 1);
			}
			
			// Remove unreachable return statements
			bool modified = false;
			foreach(ILBlock block in method.GetSelfAndChildrenRecursive<ILBlock>()) {
				for (int i = 0; i < block.Body.Count - 1;) {
					if (block.Body[i].IsUnconditionalControlFlow() && block.Body[i+1].Match(ILCode.Ret)) {
						modified = true;
						block.Body.RemoveAt(i+1);
					} else {
						i++;
					}
				}
			}
			if (modified) {
				// More removals might be possible
				new GotoRemoval().RemoveGotos(method);
			}
		}
		
		IEnumerable<ILNode> GetParents(ILNode node)
		{
			ILNode current = node;
			while(true) {
				current = parent[current];
				if (current == null)
					yield break;
				yield return current;
			}
		}
		
		bool TrySimplifyGoto(ILExpression gotoExpr)
		{
			Debug.Assert(gotoExpr.Code == ILCode.Br || gotoExpr.Code == ILCode.Leave);
			Debug.Assert(gotoExpr.Prefixes == null);
			Debug.Assert(gotoExpr.Operand != null);
			
			ILNode target = Enter(gotoExpr, new HashSet<ILNode>());
			if (target == null)
				return false;
			
			// The gotoExper is marked as visited because we do not want to
			// walk over node which we plan to modify
			
			// The simulated path always has to start in the same try-block
			// in other for the same finally blocks to be executed.
			
			if (target == Exit(gotoExpr, new HashSet<ILNode>() { gotoExpr })) {
				gotoExpr.Code = ILCode.Nop;
				gotoExpr.Operand = null;
				if (target is ILExpression)
					((ILExpression)target).ILRanges.AddRange(gotoExpr.ILRanges);
				gotoExpr.ILRanges.Clear();
				return true;
			}
			
			ILNode breakBlock = GetParents(gotoExpr).FirstOrDefault(n => n is ILWhileLoop || n is ILSwitch);
			if (breakBlock != null && target == Exit(breakBlock, new HashSet<ILNode>() { gotoExpr })) {
				gotoExpr.Code = ILCode.LoopOrSwitchBreak;
				gotoExpr.Operand = null;
				return true;
			}
			
			ILNode continueBlock = GetParents(gotoExpr).FirstOrDefault(n => n is ILWhileLoop);
			if (continueBlock != null && target == Enter(continueBlock, new HashSet<ILNode>() { gotoExpr })) {
				gotoExpr.Code = ILCode.LoopContinue;
				gotoExpr.Operand = null;
				return true;
			}
			
			return false;
		}
		
		/// <summary>
		/// Get the first expression to be excecuted if the instruction pointer is at the start of the given node.
		/// Try blocks may not be entered in any way.  If possible, the try block is returned as the node to be executed.
		/// </summary>
		ILNode Enter(ILNode node, HashSet<ILNode> visitedNodes)
		{
			if (node == null)
				throw new ArgumentNullException();
			
			if (!visitedNodes.Add(node))
				return null;  // Infinite loop
			
			ILLabel label = node as ILLabel;
			if (label != null) {
				return Exit(label, visitedNodes);
			}
			
			ILExpression expr = node as ILExpression;
			if (expr != null) {
				if (expr.Code == ILCode.Br || expr.Code == ILCode.Leave) {
					ILLabel target = (ILLabel)expr.Operand;
					// Early exit - same try-block
					if (GetParents(expr).OfType<ILTryCatchBlock>().FirstOrDefault() == GetParents(target).OfType<ILTryCatchBlock>().FirstOrDefault())
						return Enter(target, visitedNodes);
					// Make sure we are not entering any try-block
					var srcTryBlocks = GetParents(expr).OfType<ILTryCatchBlock>().Reverse().ToList();
					var dstTryBlocks = GetParents(target).OfType<ILTryCatchBlock>().Reverse().ToList();
					// Skip blocks that we are already in
					int i = 0;
					while(i < srcTryBlocks.Count && i < dstTryBlocks.Count && srcTryBlocks[i] == dstTryBlocks[i]) i++;
					if (i == dstTryBlocks.Count) {
						return Enter(target, visitedNodes);
					} else {
						ILTryCatchBlock dstTryBlock = dstTryBlocks[i];
						// Check that the goto points to the start
						ILTryCatchBlock current = dstTryBlock;
						while(current != null) {
							foreach(ILNode n in current.TryBlock.Body) {
								if (n is ILLabel) {
									if (n == target)
										return dstTryBlock;
								} else if (!n.Match(ILCode.Nop)) {
									current = n as ILTryCatchBlock;
									break;
								}
							}
						}
						return null;
					}
				} else if (expr.Code == ILCode.Nop) {
					return Exit(expr, visitedNodes);
				} else if (expr.Code == ILCode.LoopOrSwitchBreak) {
					ILNode breakBlock = GetParents(expr).First(n => n is ILWhileLoop || n is ILSwitch);
					return Exit(breakBlock, new HashSet<ILNode>() { expr });
				} else if (expr.Code == ILCode.LoopContinue) {
					ILNode continueBlock = GetParents(expr).First(n => n is ILWhileLoop);
					return Enter(continueBlock, new HashSet<ILNode>() { expr });
				} else {
					return expr;
				}
			}
			
			ILBlock block = node as ILBlock;
			if (block != null) {
				if (block.EntryGoto != null) {
					return Enter(block.EntryGoto, visitedNodes);
				} else if (block.Body.Count > 0) {
					return Enter(block.Body[0], visitedNodes);
				} else {
					return Exit(block, visitedNodes);
				}
			}
			
			ILCondition cond = node as ILCondition;
			if (cond != null) {
				return cond.Condition;
			}
			
			ILWhileLoop loop = node as ILWhileLoop;
			if (loop != null) {
				if (loop.Condition != null) {
					return loop.Condition;
				} else {
					return Enter(loop.BodyBlock, visitedNodes);
				}
			}
			
			ILTryCatchBlock tryCatch = node as ILTryCatchBlock;
			if (tryCatch != null) {
				return tryCatch;
			}
			
			ILSwitch ilSwitch = node as ILSwitch;
			if (ilSwitch != null) {
				return ilSwitch.Condition;
			}
			
			throw new NotSupportedException(node.GetType().ToString());
		}
		
		/// <summary>
		/// Get the first expression to be excecuted if the instruction pointer is at the end of the given node
		/// </summary>
		ILNode Exit(ILNode node, HashSet<ILNode> visitedNodes)
		{
			if (node == null)
				throw new ArgumentNullException();
			
			ILNode nodeParent = parent[node];
			if (nodeParent == null)
				return null;  // Exited main body
			
			if (nodeParent is ILBlock) {
				ILNode nextNode = nextSibling[node];
				if (nextNode != null) {
					return Enter(nextNode, visitedNodes);
				} else {
					return Exit(nodeParent, visitedNodes);
				}
			}
			
			if (nodeParent is ILCondition) {
				return Exit(nodeParent, visitedNodes);
			}
			
			if (nodeParent is ILTryCatchBlock) {
				// Finally blocks are completely ignored.
				// We rely on the fact that try blocks can not be entered.
				return Exit(nodeParent, visitedNodes);
			}
			
			if (nodeParent is ILSwitch) {
				return null;  // Implicit exit from switch is not allowed
			}
			
			if (nodeParent is ILWhileLoop) {
				return Enter(nodeParent, visitedNodes);
			}
			
			throw new NotSupportedException(nodeParent.GetType().ToString());
		}
	}
}
