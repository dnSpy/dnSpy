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
using System.Diagnostics;
using System.Linq;
using ICSharpCode.Decompiler.FlowAnalysis;
using ICSharpCode.NRefactory.Utils;
using dnlib.DotNet;
using dnlib.DotNet.Emit;

namespace ICSharpCode.Decompiler.ILAst
{
	public enum ILAstOptimizationStep
	{
		RemoveRedundantCode,
		ReduceBranchInstructionSet,
		InlineVariables,
		CopyPropagation,
		YieldReturn,
		AsyncAwait,
		PropertyAccessInstructions,
		SplitToMovableBlocks,
		TypeInference,
		HandlePointerArithmetic,
		SimplifyShortCircuit,
		SimplifyTernaryOperator,
		SimplifyNullCoalescing,
		JoinBasicBlocks,
		SimplifyLogicNot,
		SimplifyShiftOperators,
		TypeConversionSimplifications,
		SimplifyLdObjAndStObj,
		SimplifyCustomShortCircuit,
		SimplifyLiftedOperators,
		TransformArrayInitializers,
		TransformMultidimensionalArrayInitializers,
		TransformObjectInitializers,
		MakeAssignmentExpression,
		IntroducePostIncrement,
		InlineExpressionTreeParameterDeclarations,
		InlineVariables2,
		FindLoops,
		FindConditions,
		FlattenNestedMovableBlocks,
		RemoveEndFinally,
		RemoveRedundantCode2,
		GotoRemoval,
		DuplicateReturns,
		GotoRemoval2,
		ReduceIfNesting,
		InlineVariables3,
		CachedDelegateInitialization,
		IntroduceFixedStatements,
		RecombineVariables,
		TypeInference2,
		RemoveRedundantCode3,
		None
	}
	
	public partial class ILAstOptimizer
	{
		int nextLabelIndex = 0;
		
		DecompilerContext context;
		ICorLibTypes corLib;
		ILBlock method;
		
		public void Optimize(DecompilerContext context, ILBlock method, ILAstOptimizationStep abortBeforeStep = ILAstOptimizationStep.None)
		{
			this.context = context;
			this.corLib = context.CurrentMethod.Module.CorLibTypes;
			this.method = method;
			
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
			AsyncDecompiler.RunStep1(context, method);
			
			if (abortBeforeStep == ILAstOptimizationStep.AsyncAwait) return;
			AsyncDecompiler.RunStep2(context, method);
			
			if (abortBeforeStep == ILAstOptimizationStep.PropertyAccessInstructions) return;
			IntroducePropertyAccessInstructions(method);
			
			if (abortBeforeStep == ILAstOptimizationStep.SplitToMovableBlocks) return;
			foreach(ILBlock block in method.GetSelfAndChildrenRecursive<ILBlock>()) {
				SplitToBasicBlocks(block);
			}
			
			if (abortBeforeStep == ILAstOptimizationStep.TypeInference) return;
			// Types are needed for the ternary operator optimization
			TypeAnalysis.Run(context, method);
			
			if (abortBeforeStep == ILAstOptimizationStep.HandlePointerArithmetic) return;
			HandlePointerArithmetic(method);

			foreach(ILBlock block in method.GetSelfAndChildrenRecursive<ILBlock>()) {
				bool modified;
				do {
					modified = false;
					
					if (abortBeforeStep == ILAstOptimizationStep.SimplifyShortCircuit) return;
					modified |= block.RunOptimization(new SimpleControlFlow(context, method).SimplifyShortCircuit);
					
					if (abortBeforeStep == ILAstOptimizationStep.SimplifyTernaryOperator) return;
					modified |= block.RunOptimization(new SimpleControlFlow(context, method).SimplifyTernaryOperator);
					
					if (abortBeforeStep == ILAstOptimizationStep.SimplifyNullCoalescing) return;
					modified |= block.RunOptimization(new SimpleControlFlow(context, method).SimplifyNullCoalescing);
					
					if (abortBeforeStep == ILAstOptimizationStep.JoinBasicBlocks) return;
					modified |= block.RunOptimization(new SimpleControlFlow(context, method).JoinBasicBlocks);

					if (abortBeforeStep == ILAstOptimizationStep.SimplifyLogicNot) return;
					modified |= block.RunOptimization(SimplifyLogicNot);

					if (abortBeforeStep == ILAstOptimizationStep.SimplifyShiftOperators) return;
					modified |= block.RunOptimization(SimplifyShiftOperators);

					if (abortBeforeStep == ILAstOptimizationStep.TypeConversionSimplifications) return;
					modified |= block.RunOptimization(TypeConversionSimplifications);
					
					if (abortBeforeStep == ILAstOptimizationStep.SimplifyLdObjAndStObj) return;
					modified |= block.RunOptimization(SimplifyLdObjAndStObj);
					
					if (abortBeforeStep == ILAstOptimizationStep.SimplifyCustomShortCircuit) return;
					modified |= block.RunOptimization(new SimpleControlFlow(context, method).SimplifyCustomShortCircuit);

					if (abortBeforeStep == ILAstOptimizationStep.SimplifyLiftedOperators) return;
					modified |= block.RunOptimization(SimplifyLiftedOperators);
					
					if (abortBeforeStep == ILAstOptimizationStep.TransformArrayInitializers) return;
					modified |= block.RunOptimization(TransformArrayInitializers);

					if (abortBeforeStep == ILAstOptimizationStep.TransformMultidimensionalArrayInitializers) return;
					modified |= block.RunOptimization(TransformMultidimensionalArrayInitializers);
					
					if (abortBeforeStep == ILAstOptimizationStep.TransformObjectInitializers) return;
					modified |= block.RunOptimization(TransformObjectInitializers);
					
					if (abortBeforeStep == ILAstOptimizationStep.MakeAssignmentExpression) return;
					if (context.Settings.MakeAssignmentExpressions) {
						modified |= block.RunOptimization(MakeAssignmentExpression);
					}
					modified |= block.RunOptimization(MakeCompoundAssignments);
					
					if (abortBeforeStep == ILAstOptimizationStep.IntroducePostIncrement) return;
					if (context.Settings.IntroduceIncrementAndDecrement) {
						modified |= block.RunOptimization(IntroducePostIncrement);
					}
					
					if (abortBeforeStep == ILAstOptimizationStep.InlineExpressionTreeParameterDeclarations) return;
					if (context.Settings.ExpressionTrees) {
						modified |= block.RunOptimization(InlineExpressionTreeParameterDeclarations);
					}
					
					if (abortBeforeStep == ILAstOptimizationStep.InlineVariables2) return;
					modified |= new ILInlining(method).InlineAllInBlock(block);
					new ILInlining(method).CopyPropagation();
					
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
			
			if (abortBeforeStep == ILAstOptimizationStep.RemoveEndFinally) return;
			RemoveEndFinally(method);
			
			if (abortBeforeStep == ILAstOptimizationStep.RemoveRedundantCode2) return;
			RemoveRedundantCode(method);
			
			if (abortBeforeStep == ILAstOptimizationStep.GotoRemoval) return;
			new GotoRemoval().RemoveGotos(method);
			
			if (abortBeforeStep == ILAstOptimizationStep.DuplicateReturns) return;
			DuplicateReturnStatements(method);
			
			if (abortBeforeStep == ILAstOptimizationStep.GotoRemoval2) return;
			new GotoRemoval().RemoveGotos(method);
			
			if (abortBeforeStep == ILAstOptimizationStep.ReduceIfNesting) return;
			ReduceIfNesting(method);
			
			if (abortBeforeStep == ILAstOptimizationStep.InlineVariables3) return;
			// The 2nd inlining pass is necessary because DuplicateReturns and the introduction of ternary operators
			// open up additional inlining possibilities.
			new ILInlining(method).InlineAllVariables();
			
			if (abortBeforeStep == ILAstOptimizationStep.CachedDelegateInitialization) return;
			if (context.Settings.AnonymousMethods) {
				foreach(ILBlock block in method.GetSelfAndChildrenRecursive<ILBlock>()) {
					for (int i = 0; i < block.Body.Count; i++) {
						// TODO: Move before loops
						CachedDelegateInitializationWithField(block, ref i);
						CachedDelegateInitializationWithLocal(block, ref i);
					}
				}
			}
			
			if (abortBeforeStep == ILAstOptimizationStep.IntroduceFixedStatements) return;
			// we need post-order traversal, not pre-order, for "fixed" to work correctly
			foreach (ILBlock block in TreeTraversal.PostOrder<ILNode>(method, n => n.GetChildren()).OfType<ILBlock>()) {
				for (int i = block.Body.Count - 1; i >= 0; i--) {
					// TODO: Move before loops
					if (i < block.Body.Count)
						IntroduceFixedStatements(block, block.Body, i);
				}
			}
			
			if (abortBeforeStep == ILAstOptimizationStep.RecombineVariables) return;
			RecombineVariables(method);
			
			if (abortBeforeStep == ILAstOptimizationStep.TypeInference2) return;
			TypeAnalysis.Reset(method);
			TypeAnalysis.Run(context, method);
			
			if (abortBeforeStep == ILAstOptimizationStep.RemoveRedundantCode3) return;
			GotoRemoval.RemoveRedundantCode(method);
			
			// ReportUnassignedILRanges(method);
		}
		
		/// <summary>
		/// Removes redundatant Br, Nop, Dup, Pop
		/// Ignore arguments of 'leave'
		/// </summary>
		/// <param name="method"></param>
		internal static void RemoveRedundantCode(ILBlock method)
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
						ILNode prev = newBody.Count > 0 ? newBody[newBody.Count - 1] : null;
						ILNode label = null;
						ILNode br = body[i];
						// Ignore the branch
						if (labelRefCount[target] == 1) {
							label = body[i + 1];
							i++;  // Ignore the label as well
						}
						ILNode next = i + 1 < body.Count ? body[i + 1] : null;
						Utils.AddILRangesTryPreviousFirst(br, prev, next, block);
						if (label != null)
							Utils.AddILRangesTryPreviousFirst(label, prev, next, block);
					} else if (body[i].Match(ILCode.Nop)){
						// Ignore nop
						Utils.NopMergeILRanges(block, newBody, i);
					} else if (body[i].Match(ILCode.Pop, out popExpr)) {
						ILVariable v;
						if (!popExpr.Match(ILCode.Ldloc, out v))
							throw new Exception("Pop should have just ldloc at this stage");
						// Best effort to move the ILRange to previous statement
						ILVariable prevVar;
						ILExpression prevExpr;
						if (i - 1 >= 0 && body[i - 1].Match(ILCode.Stloc, out prevVar, out prevExpr) && prevVar == v)
							prevExpr.ILRanges.AddRange(((ILExpression)body[i]).ILRanges);
						else
							Utils.AddILRangesTryPreviousFirst(newBody, body, i, block);
						// Ignore pop
					} else {
						ILLabel label = body[i] as ILLabel;
						if (label != null) {
							if (labelRefCount.GetOrDefault(label) > 0)
								newBody.Add(label);
							else
								Utils.LabelMergeILRanges(block, newBody, i);
						} else {
							newBody.Add(body[i]);
						}
					}
				}
				block.Body = newBody;
			}
			
			// Ignore arguments of 'leave'
			foreach (ILExpression expr in method.GetSelfAndChildrenRecursive<ILExpression>(e => e.Code == ILCode.Leave)) {
				if (expr.Arguments.Any(arg => !arg.Match(ILCode.Ldloc)))
					throw new Exception("Leave should have just ldloc at this stage");
				foreach (var arg in expr.Arguments)
					expr.ILRanges.AddRange(arg.GetSelfAndChildrenRecursiveILRanges());
				expr.Arguments.Clear();
			}
			
			// 'dup' removal
			foreach (ILExpression expr in method.GetSelfAndChildrenRecursive<ILExpression>()) {
				for (int i = 0; i < expr.Arguments.Count; i++) {
					ILExpression child;
					if (expr.Arguments[i].Match(ILCode.Dup, out child)) {
						child.ILRanges.AddRange(expr.Arguments[i].AllILRanges);
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
					ILCode op;
					switch(expr.Code) {
						case ILCode.Switch:
						case ILCode.Brtrue:
							expr.Arguments.Single().ILRanges.AddRange(expr.ILRanges);
							expr.ILRanges.Clear();
							continue;
							case ILCode.Brfalse:  op = ILCode.LogicNot; break;
							case ILCode.Beq:      op = ILCode.Ceq; break;
							case ILCode.Bne_Un:   op = ILCode.Cne; break;
							case ILCode.Bgt:      op = ILCode.Cgt; break;
							case ILCode.Bgt_Un:   op = ILCode.Cgt_Un; break;
							case ILCode.Ble:      op = ILCode.Cle; break;
							case ILCode.Ble_Un:   op = ILCode.Cle_Un; break;
							case ILCode.Blt:      op = ILCode.Clt; break;
							case ILCode.Blt_Un:   op = ILCode.Clt_Un; break;
							case ILCode.Bge:	    op = ILCode.Cge; break;
							case ILCode.Bge_Un:   op = ILCode.Cge_Un; break;
						default:
							continue;
					}
					var newExpr = new ILExpression(op, null, expr.Arguments);
					block.Body[i] = new ILExpression(ILCode.Brtrue, expr.Operand, newExpr);
					newExpr.ILRanges.AddRange(expr.ILRanges);
				}
			}
		}
		
		/// <summary>
		/// Converts call and callvirt instructions that read/write properties into CallGetter/CallSetter instructions.
		/// 
		/// CallGetter/CallSetter is used to allow the ILAst to represent "while ((SomeProperty = value) != null)".
		/// 
		/// Also simplifies 'newobj(SomeDelegate, target, ldvirtftn(F, target))' to 'newobj(SomeDelegate, target, ldvirtftn(F))'
		/// </summary>
		void IntroducePropertyAccessInstructions(ILNode node)
		{
			ILExpression parentExpr = node as ILExpression;
			if (parentExpr != null) {
				for (int i = 0; i < parentExpr.Arguments.Count; i++) {
					ILExpression expr = parentExpr.Arguments[i];
					IntroducePropertyAccessInstructions(expr);
					IntroducePropertyAccessInstructions(expr, parentExpr, i);
				}
			} else {
				foreach (ILNode child in node.GetChildren()) {
					IntroducePropertyAccessInstructions(child);
					ILExpression expr = child as ILExpression;
					if (expr != null) {
						IntroducePropertyAccessInstructions(expr, null, -1);
					}
				}
			}
		}
		
		void IntroducePropertyAccessInstructions(ILExpression expr, ILExpression parentExpr, int posInParent)
		{
			if (expr.Code == ILCode.Call || expr.Code == ILCode.Callvirt) {
				IMethod cecilMethod = (IMethod)expr.Operand;
				var declType = cecilMethod.DeclaringType as dnlib.DotNet.TypeSpec;
				var declArrayType = declType == null ? null : declType.TypeSig.RemovePinnedAndModifiers() as ArraySigBase;
				if (declArrayType != null) {
					switch (cecilMethod.Name) {
						case "Get":
							expr.Code = ILCode.CallGetter;
							break;
						case "Set":
							expr.Code = ILCode.CallSetter;
							break;
						case "Address":
							ByRefSig brt = cecilMethod.MethodSig.GetRetType() as ByRefSig;
							if (brt != null) {
								IMethod getMethod = new MemberRefUser(cecilMethod.Module, "Get", cecilMethod.MethodSig == null ? null : cecilMethod.MethodSig.Clone(), declArrayType.ToTypeDefOrRef());
								if (getMethod.MethodSig != null)
									getMethod.MethodSig.RetType = declArrayType.Next;
								expr.Operand = getMethod;
							}
							expr.Code = ILCode.CallGetter;
							if (parentExpr != null) {
								parentExpr.Arguments[posInParent] = new ILExpression(ILCode.AddressOf, null, expr);
							}
							break;
					}
				} else {
					MethodDef cecilMethodDef = cecilMethod.Resolve();
					if (cecilMethodDef != null) {
						if (cecilMethodDef.IsGetter)
							expr.Code = (expr.Code == ILCode.Call) ? ILCode.CallGetter : ILCode.CallvirtGetter;
						else if (cecilMethodDef.IsSetter)
							expr.Code = (expr.Code == ILCode.Call) ? ILCode.CallSetter : ILCode.CallvirtSetter;
					}
				}
			} else if (expr.Code == ILCode.Newobj && expr.Arguments.Count == 2) {
				// Might be 'newobj(SomeDelegate, target, ldvirtftn(F, target))'.
				ILVariable target;
				if (expr.Arguments[0].Match(ILCode.Ldloc, out target)
					&& expr.Arguments[1].Code == ILCode.Ldvirtftn
					&& expr.Arguments[1].Arguments.Count == 1
					&& expr.Arguments[1].Arguments[0].MatchLdloc(target))
				{
					// Remove the 'target' argument from the ldvirtftn instruction.
					// It's not needed in the translation to C#, and needs to be eliminated so that the target expression
					// can be inlined.
					expr.Arguments[1].ILRanges.AddRange(expr.Arguments[1].Arguments[0].GetSelfAndChildrenRecursiveILRanges());
					expr.Arguments[1].Arguments.Clear();
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
			
			ILLabel entryLabel = block.Body.FirstOrDefault() as ILLabel ?? new ILLabel() { Name = "Block_" + (nextLabelIndex++) };
			ILBasicBlock basicBlock = new ILBasicBlock();
			basicBlocks.Add(basicBlock);
			basicBlock.Body.Add(entryLabel);
			block.EntryGoto = new ILExpression(ILCode.Br, entryLabel);
			
			if (block.Body.Count > 0) {
				if (block.Body[0] != entryLabel)
					basicBlock.Body.Add(block.Body[0]);
				
				for (int i = 1; i < block.Body.Count; i++) {
					ILNode lastNode = block.Body[i - 1];
					ILNode currNode = block.Body[i];
					
					// Start a new basic block if necessary
					if (currNode is ILLabel ||
						currNode is ILTryCatchBlock || // Counts as label
						lastNode.IsConditionalControlFlow() ||
						lastNode.IsUnconditionalControlFlow())
					{
						// Try to reuse the label
						ILLabel label = currNode as ILLabel ?? new ILLabel() { Name = "Block_" + (nextLabelIndex++).ToString() };
						
						// Terminate the last block
						if (!lastNode.IsUnconditionalControlFlow()) {
							// Explicit branch from one block to other
							basicBlock.Body.Add(new ILExpression(ILCode.Br, label));
						}
						
						// Start the new block
						basicBlock = new ILBasicBlock();
						basicBlocks.Add(basicBlock);
						basicBlock.Body.Add(label);
						
						// Add the node to the basic block
						if (currNode != label)
							basicBlock.Body.Add(currNode);
					} else {
						basicBlock.Body.Add(currNode);
					}
				}
			}
			
			block.Body = basicBlocks;
			return;
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
									block.Body[i] = new ILExpression(ILCode.Ret, null).WithILRanges(block.Body[i].GetSelfAndChildrenRecursiveILRanges());
								} else if (retArgs.Single().Match(ILCode.Ldloc, out locVar)) {
									block.Body[i] = new ILExpression(ILCode.Ret, null, new ILExpression(ILCode.Ldloc, locVar)).WithILRanges(block.Body[i].GetSelfAndChildrenRecursiveILRanges());
								} else if (retArgs.Single().Match(ILCode.Ldc_I4, out constValue)) {
									block.Body[i] = new ILExpression(ILCode.Ret, null, new ILExpression(ILCode.Ldc_I4, constValue)).WithILRanges(block.Body[i].GetSelfAndChildrenRecursiveILRanges());
								}
							}
						} else {
							if (method.Body.Count > 0 && method.Body.Last() == targetLabel) {
								// It exits the main method - so it is same as return;
								block.Body[i] = new ILExpression(ILCode.Ret, null).WithILRanges(block.Body[i].GetSelfAndChildrenRecursiveILRanges());
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
				ILBasicBlock prevChildAsBB = null;
				List<ILNode> flatBody = new List<ILNode>();
				foreach (ILNode child in block.GetChildren()) {
					FlattenBasicBlocks(child);
					ILBasicBlock childAsBB = child as ILBasicBlock;
					if (childAsBB != null) {
						if (!(childAsBB.Body.FirstOrDefault() is ILLabel))
							throw new Exception("Basic block has to start with a label. \n" + childAsBB.ToString());
						if (childAsBB.Body.LastOrDefault() is ILExpression && !childAsBB.Body.LastOrDefault().IsUnconditionalControlFlow())
							throw new Exception("Basci block has to end with unconditional control flow. \n" + childAsBB.ToString());
						if (flatBody.Count > 0)
							flatBody[flatBody.Count - 1].EndILRanges.AddRange(childAsBB.ILRanges);
						else
							block.ILRanges.AddRange(childAsBB.ILRanges);
						flatBody.AddRange(childAsBB.GetChildren());
						prevChildAsBB = childAsBB;
					} else {
						flatBody.Add(child);
						if (prevChildAsBB != null)
							child.ILRanges.AddRange(prevChildAsBB.EndILRanges);
						prevChildAsBB = null;
					}
				}
				block.EntryGoto = null;
				block.Body = flatBody;
				if (prevChildAsBB != null)
					block.EndILRanges.AddRange(prevChildAsBB.EndILRanges);
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
		/// Replace endfinally with jump to the end of the finally block
		/// </summary>
		void RemoveEndFinally(ILBlock method)
		{
			// Go thought the list in reverse so that we do the nested blocks first
			var list = method.GetSelfAndChildrenRecursive<ILTryCatchBlock>(tc => tc.FinallyBlock != null);
			for (int j = list.Count - 1; j >= 0; j--) {
				var tryCatch = list[j];
				ILLabel label = new ILLabel() { Name = "EndFinally_" + nextLabelIndex++ };
				tryCatch.FinallyBlock.Body.Add(label);
				foreach(var block in tryCatch.FinallyBlock.GetSelfAndChildrenRecursive<ILBlock>()) {
					for (int i = 0; i < block.Body.Count; i++) {
						if (block.Body[i].Match(ILCode.Endfinally)) {
							block.Body[i] = new ILExpression(ILCode.Br, label).WithILRanges(block.Body[i].GetSelfAndChildrenRecursiveILRanges());
						}
					}
				}
			}
		}
		
		/// <summary>
		/// Reduce the nesting of conditions.
		/// It should be done on flat data that already had most gotos removed
		/// </summary>
		void ReduceIfNesting(ILNode node)
		{
			ILBlock block = node as ILBlock;
			if (block != null) {
				for (int i = 0; i < block.Body.Count; i++) {
					ILCondition cond = block.Body[i] as ILCondition;
					if (cond != null) {
						bool trueExits = cond.TrueBlock.Body.LastOrDefault().IsUnconditionalControlFlow();
						bool falseExits = cond.FalseBlock.Body.LastOrDefault().IsUnconditionalControlFlow();
						
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
					ReduceIfNesting(child);
			}
		}
		
		void RecombineVariables(ILBlock method)
		{
			// Recombine variables that were split when the ILAst was created
			// This ensures that a single IL variable is a single C# variable (gets assigned only one name)
			// The DeclareVariables transformation might then split up the C# variable again if it is used indendently in two separate scopes.
			Dictionary<Local, ILVariable> dict = new Dictionary<Local, ILVariable>();
			ReplaceVariables(
				method,
				delegate(ILVariable v) {
					if (v.OriginalVariable == null)
						return v;
					ILVariable combinedVariable;
					if (!dict.TryGetValue(v.OriginalVariable, out combinedVariable)) {
						dict.Add(v.OriginalVariable, v);
						combinedVariable = v;
					}
					return combinedVariable;
				});
		}

		void HandlePointerArithmetic(ILNode method)
		{
			foreach (ILExpression expr in method.GetSelfAndChildrenRecursive<ILExpression>()) {
				List<ILExpression> args = expr.Arguments;
				switch (expr.Code) {
					case ILCode.Localloc:
					{
						PtrSig type = expr.InferredType as PtrSig;
						if (type != null) {
							ILExpression arg0 = args[0];
							ILExpression expr2 = expr;
							DivideOrMultiplyBySize(ref expr2, ref arg0, type.Next, true);
							// expr shouldn't change
							if (expr2 != expr)
								throw new InvalidOperationException();
							args[0] = arg0;
						}
						break;
					}
					case ILCode.Add:
					case ILCode.Add_Ovf:
					case ILCode.Add_Ovf_Un:
					{
						ILExpression arg0 = args[0];
						ILExpression arg1 = args[1];
						if (expr.InferredType is PtrSig) {
							if (arg0.ExpectedType is PtrSig) {
								DivideOrMultiplyBySize(ref arg0, ref arg1, ((PtrSig)expr.InferredType).Next, true);
							} else if (arg1.ExpectedType is PtrSig)
								DivideOrMultiplyBySize(ref arg1, ref arg0, ((PtrSig)expr.InferredType).Next, true);
						}
						args[0] = arg0;
						args[1] = arg1;
						break;
					}
					case ILCode.Sub:
					case ILCode.Sub_Ovf:
					case ILCode.Sub_Ovf_Un:
					{
						ILExpression arg0 = args[0];
						ILExpression arg1 = args[1];
						if (expr.InferredType is PtrSig) {
							if (arg0.ExpectedType is PtrSig && !(arg1.InferredType is PtrSig))
								DivideOrMultiplyBySize(ref arg0, ref arg1, ((PtrSig)expr.InferredType).Next, true);
						}
						args[0] = arg0;
						args[1] = arg1;
						break;
					}
					case ILCode.Conv_I8:
					{
						ILExpression arg0 = args[0];
						// conv.i8(div:intptr(p0 - p1))
						if (arg0.Code == ILCode.Div && arg0.InferredType.RemovePinnedAndModifiers().GetElementType() == ElementType.I)
						{
							ILExpression dividend = arg0.Arguments[0];
							if (dividend.InferredType.RemovePinnedAndModifiers().GetElementType() == ElementType.I &&
								(dividend.Code == ILCode.Sub || dividend.Code == ILCode.Sub_Ovf || dividend.Code == ILCode.Sub_Ovf_Un))
							{
								PtrSig pointerType0 = dividend.Arguments[0].InferredType as PtrSig;
								PtrSig pointerType1 = dividend.Arguments[1].InferredType as PtrSig;

								if (pointerType0 != null && pointerType1 != null) {
									if (pointerType0.Next.RemovePinnedAndModifiers().GetElementType() == ElementType.Void ||
										!new SigComparer().Equals(pointerType0.Next, pointerType1.Next)) {
										pointerType0 = pointerType1 = new PtrSig(corLib.Byte);
										dividend.Arguments[0] = Cast(dividend.Arguments[0], pointerType0);
										dividend.Arguments[1] = Cast(dividend.Arguments[1], pointerType1);
									}

									DivideOrMultiplyBySize(ref dividend, ref arg0, pointerType0.Next, false);
									// dividend shouldn't change
									if (args[0].Arguments[0] != dividend)
										throw new InvalidOperationException();
								}
							}
						}
						args[0] = arg0;
						break;
					}
				}
			}
		}

		static ILExpression UnwrapIntPtrCast(ILExpression expr)
		{
			if (expr.Code != ILCode.Conv_I && expr.Code != ILCode.Conv_U)
				return expr;

			ILExpression arg = expr.Arguments[0];
			switch (arg.InferredType.GetElementType()) {
				case ElementType.U1:
				case ElementType.I1:
				case ElementType.U2:
				case ElementType.I2:
				case ElementType.U4:
				case ElementType.I4:
				case ElementType.U8:
				case ElementType.I8:
					arg.ILRanges.AddRange(expr.ILRanges);
					return arg;
			}

			return expr;
		}

		static ILExpression Cast(ILExpression expr, TypeSig type)
		{
			return new ILExpression(ILCode.Castclass, type.ToTypeDefOrRef(), expr)
			{
				InferredType = type,
				ExpectedType = type
			};
		}

		void DivideOrMultiplyBySize(ref ILExpression pointerExpr, ref ILExpression adjustmentExpr, TypeSig elementType, bool divide)
		{
			adjustmentExpr = UnwrapIntPtrCast(adjustmentExpr);

			ILExpression sizeOfExpression;
			switch (TypeAnalysis.GetInformationAmount(elementType)) {
				case 0: // System.Void
					pointerExpr = Cast(pointerExpr, new PtrSig(corLib.Byte));
					goto case 1;
				case 1:
				case 8:
					sizeOfExpression = new ILExpression(ILCode.Ldc_I4, 1);
					break;
				case 16:
					sizeOfExpression = new ILExpression(ILCode.Ldc_I4, 2);
					break;
				case 32:
					sizeOfExpression = new ILExpression(ILCode.Ldc_I4, 4);
					break;
				case 64:
					sizeOfExpression = new ILExpression(ILCode.Ldc_I4, 8);
					break;
				default:
					sizeOfExpression = new ILExpression(ILCode.Sizeof, elementType.ToTypeDefOrRef());
					break;
			}

			if (divide && (adjustmentExpr.Code == ILCode.Mul || adjustmentExpr.Code == ILCode.Mul_Ovf || adjustmentExpr.Code == ILCode.Mul_Ovf_Un) ||
				!divide && (adjustmentExpr.Code == ILCode.Div || adjustmentExpr.Code == ILCode.Div_Un)) {
				ILExpression mulArg = adjustmentExpr.Arguments[1];
				if (mulArg.Code == sizeOfExpression.Code && sizeOfExpression.Operand.Equals(mulArg.Operand)) {
					var arg = adjustmentExpr.Arguments[0];
					arg.ILRanges.AddRange(adjustmentExpr.ILRanges);
					arg.ILRanges.AddRange(mulArg.GetSelfAndChildrenRecursiveILRanges());
					adjustmentExpr = UnwrapIntPtrCast(arg);
					return;
				}
			}

			if (adjustmentExpr.Code == sizeOfExpression.Code) {
				if (sizeOfExpression.Operand.Equals(adjustmentExpr.Operand)) {
					adjustmentExpr = new ILExpression(ILCode.Ldc_I4, 1).WithILRanges(adjustmentExpr.GetSelfAndChildrenRecursiveILRanges());
					return;
				}

				if (adjustmentExpr.Code == ILCode.Ldc_I4) {
					int offsetInBytes = (int)adjustmentExpr.Operand;
					int elementSize = (int)sizeOfExpression.Operand;

					if (offsetInBytes % elementSize != 0) {
						pointerExpr = Cast(pointerExpr, new PtrSig(corLib.Byte));
						return;
					}

					adjustmentExpr.Operand = offsetInBytes / elementSize;
					return;
				}
			}

			if (!(sizeOfExpression.Code == ILCode.Ldc_I4 && (int)sizeOfExpression.Operand == 1))
				adjustmentExpr = new ILExpression(divide ? ILCode.Div_Un : ILCode.Mul, null, adjustmentExpr, sizeOfExpression);
		}
		
		public static void ReplaceVariables(ILNode node, Func<ILVariable, ILVariable> variableMapping)
		{
			ILExpression expr = node as ILExpression;
			if (expr != null) {
				ILVariable v = expr.Operand as ILVariable;
				if (v != null)
					expr.Operand = variableMapping(v);
				foreach (ILExpression child in expr.Arguments)
					ReplaceVariables(child, variableMapping);
			} else {
				var catchBlock = node as ILTryCatchBlock.CatchBlock;
				if (catchBlock != null && catchBlock.ExceptionVariable != null) {
					catchBlock.ExceptionVariable = variableMapping(catchBlock.ExceptionVariable);
				}
				
				foreach (ILNode child in node.GetChildren())
					ReplaceVariables(child, variableMapping);
			}
		}
		
		void ReportUnassignedILRanges(ILBlock method)
		{
			var unassigned = ILRange.Invert(method.GetSelfAndChildrenRecursive<ILExpression>().SelectMany(e => e.ILRanges), context.CurrentMethod.Body.GetCodeSize()).ToList();
			if (unassigned.Count > 0)
				Debug.WriteLine(string.Format("Unassigned ILRanges for {0}.{1}: {2}", this.context.CurrentMethod.DeclaringType.Name, this.context.CurrentMethod.Name, string.Join(", ", unassigned.Select(r => r.ToString()))));
		}
	}
	
	public static class ILAstOptimizerExtensionMethods
	{
		/// <summary>
		/// Perform one pass of a given optimization on this block.
		/// This block must consist of only basicblocks.
		/// </summary>
		public static bool RunOptimization(this ILBlock block, Func<List<ILNode>, ILBasicBlock, int, bool> optimization)
		{
			bool modified = false;
			List<ILNode> body = block.Body;
			for (int i = body.Count - 1; i >= 0; i--) {
				if (i < body.Count && optimization(body, (ILBasicBlock)body[i], i)) {
					modified = true;
				}
			}
			return modified;
		}
		
		public static bool RunOptimization(this ILBlock block, Func<ILBlockBase, List<ILNode>, ILExpression, int, bool> optimization)
		{
			bool modified = false;
			foreach (ILBasicBlock bb in block.Body) {
				for (int i = bb.Body.Count - 1; i >= 0; i--) {
					ILExpression expr = bb.Body.ElementAtOrDefault(i) as ILExpression;
					if (expr != null && optimization(bb, bb.Body, expr, i)) {
						modified = true;
					}
				}
			}
			return modified;
		}
		
		public static bool IsConditionalControlFlow(this ILNode node)
		{
			ILExpression expr = node as ILExpression;
			return expr != null && expr.Code.IsConditionalControlFlow();
		}
		
		public static bool IsUnconditionalControlFlow(this ILNode node)
		{
			ILExpression expr = node as ILExpression;
			return expr != null && expr.Code.IsUnconditionalControlFlow();
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
				case ILCode.Ldstr:
				case ILCode.Ldnull:
				case ILCode.Ldc_I4:
				case ILCode.Ldc_I8:
				case ILCode.Ldc_R4:
				case ILCode.Ldc_R8:
				case ILCode.Ldc_Decimal:
					return true;
				default:
					return false;
			}
		}
		
		public static bool IsStoreToArray(this ILCode code)
		{
			switch (code) {
				case ILCode.Stelem:
				case ILCode.Stelem_I:
				case ILCode.Stelem_I1:
				case ILCode.Stelem_I2:
				case ILCode.Stelem_I4:
				case ILCode.Stelem_I8:
				case ILCode.Stelem_R4:
				case ILCode.Stelem_R8:
				case ILCode.Stelem_Ref:
					return true;
				default:
					return false;
			}
		}
		
		public static bool IsLoadFromArray(this ILCode code)
		{
			switch (code) {
				case ILCode.Ldelem:
				case ILCode.Ldelem_I:
				case ILCode.Ldelem_I1:
				case ILCode.Ldelem_I2:
				case ILCode.Ldelem_I4:
				case ILCode.Ldelem_I8:
				case ILCode.Ldelem_U1:
				case ILCode.Ldelem_U2:
				case ILCode.Ldelem_U4:
				case ILCode.Ldelem_R4:
				case ILCode.Ldelem_R8:
				case ILCode.Ldelem_Ref:
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
				case ILCode.Callvirt:
					// property getters can't be expression statements, but all other method calls can be
					IMethod mr = (IMethod)expr.Operand;
					return !mr.Name.StartsWith("get_", StringComparison.Ordinal);
				case ILCode.CallSetter:
				case ILCode.CallvirtSetter:
				case ILCode.Newobj:
				case ILCode.Newarr:
				case ILCode.Stloc:
				case ILCode.Stobj:
				case ILCode.Stsfld:
				case ILCode.Stfld:
				case ILCode.Stind_Ref:
				case ILCode.Stelem:
				case ILCode.Stelem_I:
				case ILCode.Stelem_I1:
				case ILCode.Stelem_I2:
				case ILCode.Stelem_I4:
				case ILCode.Stelem_I8:
				case ILCode.Stelem_R4:
				case ILCode.Stelem_R8:
				case ILCode.Stelem_Ref:
					return true;
				default:
					return false;
			}
		}
		
		public static ILExpression WithILRanges(this ILExpression expr, IEnumerable<ILRange> ilranges)
		{
			expr.ILRanges.AddRange(ilranges);
			return expr;
		}
		
		public static ILNode[] RemoveTail(this List<ILNode> body, params ILCode[] codes)
		{
			for (int i = 0; i < codes.Length; i++) {
				if (((ILExpression)body[body.Count - codes.Length + i]).Code != codes[i])
					throw new Exception("Tailing code does not match expected.");
			}
			var list = new ILNode[codes.Length];
			for (int i = 0; i < codes.Length; i++)
				list[i] = body[body.Count - codes.Length + i];
			body.RemoveRange(body.Count - codes.Length, codes.Length);
			return list;
		}
		
		public static V GetOrDefault<K,V>(this Dictionary<K, V> dict, K key)
		{
			V ret;
			dict.TryGetValue(key, out ret);
			return ret;
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
		
		public static bool ContainsReferenceTo(this ILExpression expr, ILVariable v)
		{
			if (expr.Operand == v)
				return true;
			foreach (var arg in expr.Arguments) {
				if (ContainsReferenceTo(arg, v))
					return true;
			}
			return false;
		}
	}
}
