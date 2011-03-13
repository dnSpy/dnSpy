using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ICSharpCode.Decompiler.FlowAnalysis;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.CSharp;

namespace ICSharpCode.Decompiler.ILAst
{
	public enum ILAstOptimizationStep
	{
		RemoveRedundantCode,
		ReduceBranchInstructionSet,
		InlineVariables,
		CopyPropagation,
		YieldReturn,
		SplitToMovableBlocks,
		TypeInference,
		PeepholeOptimizations,
		SimplifyShortCircuit,
		SimplifyTernaryOperator,
		SimplifyNullCoalescing,
		MoreSimplifyPasses,
		FindLoops,
		FindConditions,
		FlattenNestedMovableBlocks,
		GotoRemoval,
		DuplicateReturns,
		FlattenIfStatements,
		InlineVariables2,
		PeepholeTransforms,
		TypeInference2,
		None
	}
	
	public class ILAstOptimizer
	{
		int nextLabelIndex = 0;
		
		DecompilerContext context;
		TypeSystem typeSystem;
		
		public void Optimize(DecompilerContext context, ILBlock method, ILAstOptimizationStep abortBeforeStep = ILAstOptimizationStep.None)
		{
			this.context = context;
			this.typeSystem = context.CurrentMethod.Module.TypeSystem;
			
			if (abortBeforeStep == ILAstOptimizationStep.RemoveRedundantCode) return;
			RemoveRedundantCode(method);
			
			if (abortBeforeStep == ILAstOptimizationStep.ReduceBranchInstructionSet) return;
			foreach(ILBlock block in method.GetSelfAndChildrenRecursive<ILBlock>()) {
				ReduceBranchInstructionSet(block);
			}
			// ReduceBranchInstructionSet runs before inlining because the non-aggressive inlining heuristic
			// looks at which type of instruction consumes the inlined variable.
			
			if (abortBeforeStep == ILAstOptimizationStep.InlineVariables) return;
			// Works better after simple goto removal because of the following debug pattern: stloc X; br Next; Next:; ldloc X
			ILInlining inlining1 = new ILInlining(method);
			inlining1.InlineAllVariables();
			
			if (abortBeforeStep == ILAstOptimizationStep.CopyPropagation) return;
			inlining1.CopyPropagation();
			
			if (abortBeforeStep == ILAstOptimizationStep.YieldReturn) return;
			YieldReturnDecompiler.Run(context, method);
			
			if (abortBeforeStep == ILAstOptimizationStep.SplitToMovableBlocks) return;
			foreach(ILBlock block in method.GetSelfAndChildrenRecursive<ILBlock>()) {
				SplitToBasicBlocks(block);
			}
			
			if (abortBeforeStep == ILAstOptimizationStep.TypeInference) return;
			// Types are needed for the ternary operator optimization
			TypeAnalysis.Run(context, method);
			
			if (abortBeforeStep == ILAstOptimizationStep.PeepholeOptimizations) return;
			AnalyseLabels(method);
			foreach(ILBlock block in method.GetSelfAndChildrenRecursive<ILBlock>()) {
				bool modified;
				do {
					modified = false;
					
					if (abortBeforeStep == ILAstOptimizationStep.SimplifyShortCircuit) return;
					modified |= block.RunPeepholeOptimization(TrySimplifyShortCircuit);
					
					if (abortBeforeStep == ILAstOptimizationStep.SimplifyTernaryOperator) return;
					modified |= block.RunPeepholeOptimization(TrySimplifyTernaryOperator);
					
					if (abortBeforeStep == ILAstOptimizationStep.SimplifyNullCoalescing) return;
					modified |= block.RunPeepholeOptimization(TrySimplifyNullCoalescing);
					
					if (abortBeforeStep == ILAstOptimizationStep.MoreSimplifyPasses) return;
				} while(modified);
			}
			
			if (abortBeforeStep == ILAstOptimizationStep.FindLoops) return;
			foreach(ILBlock block in method.GetSelfAndChildrenRecursive<ILBlock>()) {
				new LoopsAndConditions(context).FindLoops(block);
			}
			
			if (abortBeforeStep == ILAstOptimizationStep.FindConditions) return;
			foreach(ILBlock block in method.GetSelfAndChildrenRecursive<ILBlock>()) {
				new LoopsAndConditions(context).FindConditions(block);
			}
			
			if (abortBeforeStep == ILAstOptimizationStep.FlattenNestedMovableBlocks) return;
			FlattenBasicBlocks(method);
			
			if (abortBeforeStep == ILAstOptimizationStep.GotoRemoval) return;
			RemoveRedundantCode(method);
			new GotoRemoval().RemoveGotos(method);
			
			if (abortBeforeStep == ILAstOptimizationStep.DuplicateReturns) return;
			DuplicateReturnStatements(method);
			
			if (abortBeforeStep == ILAstOptimizationStep.FlattenIfStatements) return;
			FlattenIfStatements(method);
			
			if (abortBeforeStep == ILAstOptimizationStep.InlineVariables2) return;
			// The 2nd inlining pass is necessary because DuplicateReturns and the introduction of ternary operators
			// open up additional inlining possibilities.
			new ILInlining(method).InlineAllVariables();
			
			foreach (ILExpression expr in method.GetSelfAndChildrenRecursive<ILExpression>()) {
				expr.InferredType = null;
				expr.ExpectedType = null;
			}
			
			if (abortBeforeStep == ILAstOptimizationStep.PeepholeTransforms) return;
			PeepholeTransforms.Run(context, method);
			
			if (abortBeforeStep == ILAstOptimizationStep.TypeInference2) return;
			TypeAnalysis.Run(context, method);
			
			GotoRemoval.RemoveRedundantCode(method);
			
			// ReportUnassignedILRanges(method);
		}
		
		/// <summary>
		/// Removes redundatant Br, Nop, Dup, Pop
		/// </summary>
		/// <param name="method"></param>
		void RemoveRedundantCode(ILBlock method)
		{
			Dictionary<ILLabel, int> labelRefCount = new Dictionary<ILLabel, int>();
			foreach (ILLabel target in method.GetSelfAndChildrenRecursive<ILExpression>(e => e.IsBranch()).SelectMany(e => e.GetBranchTargets())) {
				labelRefCount[target] = labelRefCount.GetOrDefault(target) + 1;
			}
			
			foreach(ILBlock block in method.GetSelfAndChildrenRecursive<ILBlock>()) {
				List<ILNode> body = block.Body;
				List<ILNode> newBody = new List<ILNode>(body.Count);
				for (int i = 0; i < body.Count; i++) {
					ILLabel target;
					ILExpression popExpr;
					if (body[i].Match(ILCode.Br, out target) && i+1 < body.Count && body[i+1] == target) {
						// Ignore the branch
						if (labelRefCount[target] == 1)
							i++;  // Ignore the label as well
					} else if (body[i].Match(ILCode.Nop)){
						// Ignore nop
					} else if (body[i].Match(ILCode.Pop, out popExpr)) {
						ILVariable v;
						if (!popExpr.Match(ILCode.Ldloc, out v))
							throw new Exception("Pop should have just ldloc at this stage");
						// Best effort to move the ILRange to previous statement
						ILVariable prevVar;
						ILExpression prevExpr;
						if (i - 1 >= 0 && body[i - 1].Match(ILCode.Stloc, out prevVar, out prevExpr) && prevVar == v)
							prevExpr.ILRanges.AddRange(((ILExpression)body[i]).ILRanges);
						// Ignore pop
					} else {
						newBody.Add(body[i]);
					}
				}
				block.Body = newBody;
			}
			
			// 'dup' removal
			foreach (ILExpression expr in method.GetSelfAndChildrenRecursive<ILExpression>()) {
				for (int i = 0; i < expr.Arguments.Count; i++) {
					ILExpression child;
					if (expr.Arguments[i].Match(ILCode.Dup, out child)) {
						child.ILRanges.AddRange(expr.Arguments[i].ILRanges);
						expr.Arguments[i] = child;
					}
				}
			}
		}
		
		/// <summary>
		/// Reduces the branch codes to just br and brtrue.
		/// Moves ILRanges to the branch argument
		/// </summary>
		void ReduceBranchInstructionSet(ILBlock block)
		{
			for (int i = 0; i < block.Body.Count; i++) {
				ILExpression expr = block.Body[i] as ILExpression;
				if (expr != null && expr.Prefixes == null) {
					switch(expr.Code) {
						case ILCode.Switch:
						case ILCode.Brtrue:
							expr.Arguments.Single().ILRanges.AddRange(expr.ILRanges);
							expr.ILRanges.Clear();
							continue;
							case ILCode.__Brfalse:  block.Body[i] = new ILExpression(ILCode.Brtrue, expr.Operand, new ILExpression(ILCode.LogicNot, null, expr.Arguments.Single())); break;
							case ILCode.__Beq:      block.Body[i] = new ILExpression(ILCode.Brtrue, expr.Operand, new ILExpression(ILCode.Ceq, null, expr.Arguments)); break;
							case ILCode.__Bne_Un:   block.Body[i] = new ILExpression(ILCode.Brtrue, expr.Operand, new ILExpression(ILCode.LogicNot, null, new ILExpression(ILCode.Ceq, null, expr.Arguments))); break;
							case ILCode.__Bgt:      block.Body[i] = new ILExpression(ILCode.Brtrue, expr.Operand, new ILExpression(ILCode.Cgt, null, expr.Arguments)); break;
							case ILCode.__Bgt_Un:   block.Body[i] = new ILExpression(ILCode.Brtrue, expr.Operand, new ILExpression(ILCode.Cgt_Un, null, expr.Arguments)); break;
							case ILCode.__Ble:      block.Body[i] = new ILExpression(ILCode.Brtrue, expr.Operand, new ILExpression(ILCode.LogicNot, null, new ILExpression(ILCode.Cgt, null, expr.Arguments))); break;
							case ILCode.__Ble_Un:   block.Body[i] = new ILExpression(ILCode.Brtrue, expr.Operand, new ILExpression(ILCode.LogicNot, null, new ILExpression(ILCode.Cgt_Un, null, expr.Arguments))); break;
							case ILCode.__Blt:      block.Body[i] = new ILExpression(ILCode.Brtrue, expr.Operand, new ILExpression(ILCode.Clt, null, expr.Arguments)); break;
							case ILCode.__Blt_Un:   block.Body[i] = new ILExpression(ILCode.Brtrue, expr.Operand, new ILExpression(ILCode.Clt_Un, null, expr.Arguments)); break;
							case ILCode.__Bge:	    block.Body[i] = new ILExpression(ILCode.Brtrue, expr.Operand, new ILExpression(ILCode.LogicNot, null, new ILExpression(ILCode.Clt, null, expr.Arguments))); break;
							case ILCode.__Bge_Un:   block.Body[i] = new ILExpression(ILCode.Brtrue, expr.Operand, new ILExpression(ILCode.LogicNot, null, new ILExpression(ILCode.Clt_Un, null, expr.Arguments))); break;
						default:
							continue;
					}
					((ILExpression)block.Body[i]).Arguments.Single().ILRanges.AddRange(expr.ILRanges);
				}
			}
		}
		
		/// <summary>
		/// Group input into a set of blocks that can be later arbitraliby schufled.
		/// The method adds necessary branches to make control flow between blocks
		/// explicit and thus order independent.
		/// </summary>
		void SplitToBasicBlocks(ILBlock block)
		{
			List<ILNode> basicBlocks = new List<ILNode>();
			
			ILBasicBlock basicBlock = new ILBasicBlock() {
				EntryLabel = new ILLabel() { Name = "Block_" + (nextLabelIndex++) }
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
							basicBlock.EntryLabel = new ILLabel() { Name = "Block_" + (nextLabelIndex++) };
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
			
			foreach (ILBasicBlock bb in basicBlocks) {
				if (bb.Body.Count > 0 &&
				    bb.Body.Last() is ILExpression &&
				    ((ILExpression)bb.Body.Last()).Code == ILCode.Br)
				{
					Debug.Assert(bb.FallthoughGoto == null);
					bb.FallthoughGoto = (ILExpression)bb.Body.Last();
					bb.Body.RemoveAt(bb.Body.Count - 1);
				}
			}
			
			block.Body = basicBlocks;
			return;
		}
		
		Dictionary<ILLabel, int> labelGlobalRefCount;
		Dictionary<ILLabel, ILBasicBlock> labelToBasicBlock;
		
		void AnalyseLabels(ILBlock method)
		{
			labelGlobalRefCount = new Dictionary<ILLabel, int>();
			foreach(ILLabel target in method.GetSelfAndChildrenRecursive<ILExpression>(e => e.IsBranch()).SelectMany(e => e.GetBranchTargets())) {
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
		
		// scope is modified if successful
		bool TrySimplifyTernaryOperator(List<ILNode> scope, ILBasicBlock head, int index)
		{
			Debug.Assert(scope.Contains(head));
			
			ILExpression condExpr;
			ILLabel trueLabel;
			ILLabel falseLabel;
			ILVariable trueLocVar = null;
			ILExpression trueExpr;
			ILLabel trueFall;
			ILVariable falseLocVar = null;
			ILExpression falseExpr;
			ILLabel falseFall;
			object unused;
			
			if (head.Match(ILCode.Brtrue, out trueLabel, out condExpr, out falseLabel) &&
			    labelGlobalRefCount[trueLabel] == 1 &&
			    labelGlobalRefCount[falseLabel] == 1 &&
			    ((labelToBasicBlock[trueLabel].Match(ILCode.Stloc, out trueLocVar, out trueExpr, out trueFall) &&
			      labelToBasicBlock[falseLabel].Match(ILCode.Stloc, out falseLocVar, out falseExpr, out falseFall)) ||
			     (labelToBasicBlock[trueLabel].Match(ILCode.Ret, out unused, out trueExpr, out trueFall) &&
			      labelToBasicBlock[falseLabel].Match(ILCode.Ret, out unused, out falseExpr, out falseFall))) &&
			    trueLocVar == falseLocVar &&
			    trueFall == falseFall &&
			    scope.Contains(labelToBasicBlock[trueLabel]) &&
			    scope.Contains(labelToBasicBlock[falseLabel])
			   )
			{
				ILCode opCode = trueLocVar != null ? ILCode.Stloc : ILCode.Ret;
				TypeReference retType = trueLocVar != null ? trueLocVar.Type : this.context.CurrentMethod.ReturnType;
				int leftBoolVal;
				int rightBoolVal;
				ILExpression newExpr;
				// a ? true:false  is equivalent to  a
				// a ? false:true  is equivalent to  !a
				// a ? true : b    is equivalent to  a || b
				// a ? b : true    is equivalent to  !a || b
				// a ? b : false   is equivalent to  a && b
				// a ? false : b   is equivalent to  !a && b
				if (retType == typeSystem.Boolean &&
				    trueExpr.Match(ILCode.Ldc_I4, out leftBoolVal) &&
				    falseExpr.Match(ILCode.Ldc_I4, out rightBoolVal) &&
				    ((leftBoolVal != 0 && rightBoolVal == 0) || (leftBoolVal == 0 && rightBoolVal != 0))
				   )
				{
					// It can be expressed as trivilal expression
					if (leftBoolVal != 0) {
						newExpr = condExpr;
					} else {
						newExpr = new ILExpression(ILCode.LogicNot, null, condExpr);
					}
				} else if (retType == typeSystem.Boolean && trueExpr.Match(ILCode.Ldc_I4, out leftBoolVal)) {
					// It can be expressed as logical expression
					if (leftBoolVal != 0) {
						newExpr = new ILExpression(ILCode.LogicOr, null, condExpr, falseExpr);
					} else {
						newExpr = new ILExpression(ILCode.LogicAnd, null, new ILExpression(ILCode.LogicNot, null, condExpr), falseExpr);
					}
				} else if (retType == typeSystem.Boolean && falseExpr.Match(ILCode.Ldc_I4, out rightBoolVal)) {
					// It can be expressed as logical expression
					if (rightBoolVal != 0) {
						newExpr = new ILExpression(ILCode.LogicOr, null, new ILExpression(ILCode.LogicNot, null, condExpr), trueExpr);
					} else {
						newExpr = new ILExpression(ILCode.LogicAnd, null, condExpr, trueExpr);
					}
				} else {
					// Ternary operator tends to create long complicated return statements					
					if (opCode == ILCode.Ret)
						return false;
					
					// Create ternary expression
					newExpr = new ILExpression(ILCode.TernaryOp, null, condExpr, trueExpr, falseExpr);
				}
				head.Body = new List<ILNode>() { new ILExpression(opCode, trueLocVar, newExpr) };
				head.FallthoughGoto = trueFall != null ? new ILExpression(ILCode.Br, trueFall) : null;
				
				// Remove the old basic blocks
				foreach(ILLabel deleteLabel in new [] { trueLabel, falseLabel }) {
					scope.RemoveOrThrow(labelToBasicBlock[deleteLabel]);
					labelGlobalRefCount.RemoveOrThrow(deleteLabel);
					labelToBasicBlock.RemoveOrThrow(deleteLabel);
				}
				
				return true;
			}
			return false;
		}
		
		bool TrySimplifyNullCoalescing(List<ILNode> scope, ILBasicBlock head, int index)
		{
			// ...
			// v = ldloc(leftVar)
			// br(condBBLabel)
			//
			// condBBLabel:
			// brtrue(endBBLabel, ldloc(leftVar))
			// br(rightBBLabel)
			//
			// rightBBLabel:
			// v = rightExpr
			// br(endBBLabel)
			//
			// endBBLabel:
			// ...
				
			ILVariable v, v2;
			ILExpression leftExpr, leftExpr2;
			ILVariable leftVar, leftVar2;
			ILLabel condBBLabel;
			ILBasicBlock condBB;
			ILLabel endBBLabel, endBBLabel2;
			ILLabel rightBBLabel;
			ILBasicBlock rightBB;
			ILExpression rightExpr;
			ILBasicBlock endBB;
			if (head.Body.LastOrDefault().Match(ILCode.Stloc, out v, out leftExpr) &&
			    leftExpr.Match(ILCode.Ldloc, out leftVar) &&
			    head.FallthoughGoto.Match(ILCode.Br, out condBBLabel) &&
			    labelToBasicBlock.TryGetValue(condBBLabel, out condBB) &&
			    condBB.Match(ILCode.Brtrue, out endBBLabel, out leftExpr2, out rightBBLabel) &&
			    leftExpr2.Match(ILCode.Ldloc, out leftVar2) &&
			    leftVar == leftVar2 &&
			    labelToBasicBlock.TryGetValue(rightBBLabel, out rightBB) &&
			    rightBB.Match(ILCode.Stloc, out v2, out rightExpr, out endBBLabel2) &&
			    v == v2 &&
			    endBBLabel == endBBLabel2 &&
			    labelToBasicBlock.TryGetValue(endBBLabel, out endBB) &&
			    labelGlobalRefCount.GetOrDefault(condBBLabel) == 1 &&
			    labelGlobalRefCount.GetOrDefault(rightBBLabel) == 1 &&
			    labelGlobalRefCount.GetOrDefault(endBBLabel) == 2 &&
			    scope.ContainsAll(condBB, rightBB, endBB)
			   )
			{
				head.Body[head.Body.Count - 1] = new ILExpression(ILCode.Stloc, v, new ILExpression(ILCode.NullCoalescing, null, leftExpr, rightExpr));
				head.FallthoughGoto = new ILExpression(ILCode.Br, endBBLabel);
				
				foreach(ILLabel deleteLabel in new [] { condBBLabel, rightBBLabel }) {
					scope.RemoveOrThrow(labelToBasicBlock[deleteLabel]);
					labelGlobalRefCount.RemoveOrThrow(deleteLabel);
					labelToBasicBlock.RemoveOrThrow(deleteLabel);
				}
				return true;
			}
			return false;
		}
		
		// scope is modified if successful
		bool TrySimplifyShortCircuit(List<ILNode> scope, ILBasicBlock head, int index)
		{
			Debug.Assert(scope.Contains(head));
			
			ILExpression condExpr;
			ILLabel trueLabel;
			ILLabel falseLabel;
			if(head.Match(ILCode.Brtrue, out trueLabel, out condExpr, out falseLabel)) {
				for (int pass = 0; pass < 2; pass++) {
					
					// On the second pass, swap labels and negate expression of the first branch
					// It is slightly ugly, but much better then copy-pasting this whole block
					ILLabel nextLabel   = (pass == 0) ? trueLabel  : falseLabel;
					ILLabel otherLablel = (pass == 0) ? falseLabel : trueLabel;
					bool    negate      = (pass == 1);
					
					ILBasicBlock nextBasicBlock = labelToBasicBlock[nextLabel];
					ILExpression nextCondExpr;
					ILLabel nextTrueLablel;
					ILLabel nextFalseLabel;
					if (scope.Contains(nextBasicBlock) &&
					    nextBasicBlock != head &&
					    labelGlobalRefCount[nextBasicBlock.EntryLabel] == 1 &&
					    nextBasicBlock.Match(ILCode.Brtrue, out nextTrueLablel, out nextCondExpr, out nextFalseLabel) &&
					    (otherLablel == nextFalseLabel || otherLablel == nextTrueLablel))
					{
						// Create short cicuit branch
						if (otherLablel == nextFalseLabel) {
							head.Body[0] = new ILExpression(ILCode.Brtrue, nextTrueLablel, new ILExpression(ILCode.LogicAnd, null, negate ? new ILExpression(ILCode.LogicNot, null, condExpr) : condExpr, nextCondExpr));
						} else {
							head.Body[0] = new ILExpression(ILCode.Brtrue, nextTrueLablel, new ILExpression(ILCode.LogicOr, null, negate ? condExpr : new ILExpression(ILCode.LogicNot, null, condExpr), nextCondExpr));
						}
						head.FallthoughGoto = new ILExpression(ILCode.Br, nextFalseLabel);
						
						// Remove the inlined branch from scope
						labelGlobalRefCount.RemoveOrThrow(nextBasicBlock.EntryLabel);
						labelToBasicBlock.RemoveOrThrow(nextBasicBlock.EntryLabel);
						scope.RemoveOrThrow(nextBasicBlock);
						
						return true;
					}
				}
			}
			return false;
		}
		
		void DuplicateReturnStatements(ILBlock method)
		{
			Dictionary<ILLabel, ILNode> nextSibling = new Dictionary<ILLabel, ILNode>();
			
			// Build navigation data
			foreach(ILBlock block in method.GetSelfAndChildrenRecursive<ILBlock>()) {
				for (int i = 0; i < block.Body.Count - 1; i++) {
					ILLabel curr = block.Body[i] as ILLabel;
					if (curr != null) {
						nextSibling[curr] = block.Body[i + 1];
					}
				}
			}
			
			// Duplicate returns
			foreach(ILBlock block in method.GetSelfAndChildrenRecursive<ILBlock>()) {
				for (int i = 0; i < block.Body.Count; i++) {
					ILLabel targetLabel;
					if (block.Body[i].Match(ILCode.Br, out targetLabel) || block.Body[i].Match(ILCode.Leave, out targetLabel)) {
						// Skip extra labels
						while(nextSibling.ContainsKey(targetLabel) && nextSibling[targetLabel] is ILLabel) {
							targetLabel = (ILLabel)nextSibling[targetLabel];
						}
						
						// Inline return statement
						ILNode target;
						List<ILExpression> retArgs;
						if (nextSibling.TryGetValue(targetLabel, out target)) {
							if (target.Match(ILCode.Ret, out retArgs)) {
								ILVariable locVar;
								object constValue;
								if (retArgs.Count == 0) {
									block.Body[i] = new ILExpression(ILCode.Ret, null);
								} else if (retArgs.Single().Match(ILCode.Ldloc, out locVar)) {
									block.Body[i] = new ILExpression(ILCode.Ret, null, new ILExpression(ILCode.Ldloc, locVar));
								} else if (retArgs.Single().Match(ILCode.Ldc_I4, out constValue)) {
									block.Body[i] = new ILExpression(ILCode.Ret, null, new ILExpression(ILCode.Ldc_I4, constValue));
								}
							}
						} else {
							if (method.Body.Count > 0 && method.Body.Last() == targetLabel) {
								// It exits the main method - so it is same as return;
								block.Body[i] = new ILExpression(ILCode.Ret, null);
							}
						}
					}
				}
			}
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
		
		/// <summary>
		/// Reduce the nesting of conditions.
		/// It should be done on flat data that already had most gotos removed
		/// </summary>
		void FlattenIfStatements(ILNode node)
		{
			ILBlock block = node as ILBlock;
			if (block != null) {
				for (int i = 0; i < block.Body.Count; i++) {
					ILCondition cond = block.Body[i] as ILCondition;
					if (cond != null) {
						bool trueExits = cond.TrueBlock.Body.Count > 0 && !cond.TrueBlock.Body.Last().CanFallThough();
						bool falseExits = cond.FalseBlock.Body.Count > 0 && !cond.FalseBlock.Body.Last().CanFallThough();
						
						if (trueExits) {
							// Move the false block after the condition
							block.Body.InsertRange(i + 1, cond.FalseBlock.GetChildren());
							cond.FalseBlock = new ILBlock();
						} else if (falseExits) {
							// Move the true block after the condition
							block.Body.InsertRange(i + 1, cond.TrueBlock.GetChildren());
							cond.TrueBlock = new ILBlock();
						}
						
						// Eliminate empty true block
						if (!cond.TrueBlock.GetChildren().Any() && cond.FalseBlock.GetChildren().Any()) {
							// Swap bodies
							ILBlock tmp = cond.TrueBlock;
							cond.TrueBlock = cond.FalseBlock;
							cond.FalseBlock = tmp;
							cond.Condition = new ILExpression(ILCode.LogicNot, null, cond.Condition);
						}
					}
				}
			}
			
			// We are changing the number of blocks so we use plain old recursion to get all blocks
			foreach(ILNode child in node.GetChildren()) {
				if (child != null && !(child is ILExpression))
					FlattenIfStatements(child);
			}
		}
		
		void ReportUnassignedILRanges(ILBlock method)
		{
			var unassigned = ILRange.Invert(method.GetSelfAndChildrenRecursive<ILExpression>().SelectMany(e => e.ILRanges), context.CurrentMethod.Body.CodeSize).ToList();
			if (unassigned.Count > 0)
				Debug.WriteLine(string.Format("Unassigned ILRanges for {0}.{1}: {2}", this.context.CurrentMethod.DeclaringType.Name, this.context.CurrentMethod.Name, string.Join(", ", unassigned.Select(r => r.ToString()))));
		}
	}
	
	public static class ILAstOptimizerExtensionMethods
	{
		public static bool Match(this ILNode node, ILCode code)
		{
			ILExpression expr = node as ILExpression;
			return expr != null && expr.Prefixes == null && expr.Code == code;
		}
		
		public static bool Match<T>(this ILNode node, ILCode code, out T operand)
		{
			ILExpression expr = node as ILExpression;
			if (expr != null && expr.Prefixes == null && expr.Code == code) {
				operand = (T)expr.Operand;
				Debug.Assert(expr.Arguments.Count == 0);
				return true;
			}
			operand = default(T);
			return false;
		}
		
		public static bool Match(this ILNode node, ILCode code, out List<ILExpression> args)
		{
			ILExpression expr = node as ILExpression;
			if (expr != null && expr.Prefixes == null && expr.Code == code) {
				Debug.Assert(expr.Operand == null);
				args = expr.Arguments;
				return true;
			}
			args = null;
			return false;
		}
		
		public static bool Match(this ILNode node, ILCode code, out ILExpression arg)
		{
			List<ILExpression> args;
			if (node.Match(code, out args) && args.Count == 1) {
				arg = args[0];
				return true;
			}
			arg = null;
			return false;
		}
		
		public static bool Match<T>(this ILNode node, ILCode code, out T operand, out List<ILExpression> args)
		{
			ILExpression expr = node as ILExpression;
			if (expr != null && expr.Prefixes == null && expr.Code == code) {
				operand = (T)expr.Operand;
				args = expr.Arguments;
				return true;
			}
			operand = default(T);
			args = null;
			return false;
		}
		
		public static bool Match<T>(this ILNode node, ILCode code, out T operand, out ILExpression arg)
		{
			List<ILExpression> args;
			if (node.Match(code, out operand, out args) && args.Count == 1) {
				arg = args[0];
				return true;
			}
			arg = null;
			return false;
		}
		
		public static bool Match<T>(this ILNode node, ILCode code, out T operand, out ILExpression arg1, out ILExpression arg2)
		{
			List<ILExpression> args;
			if (node.Match(code, out operand, out args) && args.Count == 2) {
				arg1 = args[0];
				arg2 = args[1];
				return true;
			}
			arg1 = null;
			arg2 = null;
			return false;
		}
		
		public static bool Match<T>(this ILBasicBlock bb, ILCode code, out T operand, out ILExpression arg, out ILLabel fallLabel)
		{
			if (bb.Body.Count == 1) {
				if (bb.Body[0].Match(code, out operand, out arg)) {
					fallLabel = bb.FallthoughGoto != null ? (ILLabel)bb.FallthoughGoto.Operand : null;
					return true;
				}
			}
			operand = default(T);
			arg = null;
			fallLabel = null;
			return false;
		}
		
		public static bool MatchThis(this ILNode node)
		{
			ParameterDefinition parDef;
			return node.Match(ILCode.Ldarg, out parDef) && parDef.Index == -1;
		}
		
		/// <summary>
		/// Perform one pass of a given optimization on this block.
		/// This block must consist of only basicblocks.
		/// </summary>
		public static bool RunPeepholeOptimization(this ILBlock block, Func<List<ILNode>, ILBasicBlock, int, bool> optimization)
		{
			bool modified = false;
			List<ILNode> body = block.Body;
			for (int i = 0; i < body.Count;) {
				if (optimization(body, (ILBasicBlock)body[i], i)) {
					modified = true;
					i = Math.Max(0, i - 1); // Go back one step
				} else {
					i++;
				}
			}
			return modified;
		}
		
		public static bool CanFallThough(this ILNode node)
		{
			ILExpression expr = node as ILExpression;
			if (expr != null) {
				return expr.Code.CanFallThough();
			}
			return true;
		}
		
		/// <summary>
		/// The expression has no effect on the program and can be removed 
		/// if its return value is not needed.
		/// </summary>
		public static bool HasNoSideEffects(this ILExpression expr)
		{
			// Remember that if expression can throw an exception, it is a side effect
			
			switch(expr.Code) {
				case ILCode.Ldloc:
				case ILCode.Ldloca:
				case ILCode.Ldarg:
				case ILCode.Ldstr:
				case ILCode.Ldnull:
				case ILCode.Ldc_I4:
				case ILCode.Ldc_I8:
				case ILCode.Ldc_R4:
				case ILCode.Ldc_R8:
					return true;
				default:
					return false;
			}
		}
		
		/// <summary>
		/// Can the expression be used as a statement in C#?
		/// </summary>
		public static bool CanBeExpressionStatement(this ILExpression expr)
		{
			switch(expr.Code) {
				case ILCode.Call:
				case ILCode.Calli:
				case ILCode.Callvirt:
				case ILCode.Newobj:
				case ILCode.Newarr:
					return true;
				default:
					return false;
			}
		}
		
		public static V GetOrDefault<K,V>(this Dictionary<K, V> dict, K key)
		{
			V ret;
			dict.TryGetValue(key, out ret);
			return ret;
		}
		
		public static bool ContainsAll<T>(this List<T> list, params T[] items)
		{
			foreach (T item in items) {
				if (!list.Contains(item))
					return false;
			}
			return true;
		}
		
		public static void RemoveOrThrow<T>(this ICollection<T> collection, T item)
		{
			if (!collection.Remove(item))
				throw new Exception("The item was not found in the collection");
		}
		
		public static void RemoveOrThrow<K,V>(this Dictionary<K,V> collection, K key)
		{
			if (!collection.Remove(key))
				throw new Exception("The key was not found in the dictionary");
		}
	}
}
