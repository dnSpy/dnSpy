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

using Mono.Cecil;

namespace ICSharpCode.Decompiler.ILAst
{
	public class SimpleControlFlow
	{
		Dictionary<ILLabel, int> labelGlobalRefCount = new Dictionary<ILLabel, int>();
		Dictionary<ILLabel, ILBasicBlock> labelToBasicBlock = new Dictionary<ILLabel, ILBasicBlock>();
		
		DecompilerContext context;
		TypeSystem typeSystem;
		
		public SimpleControlFlow(DecompilerContext context, ILBlock method)
		{
			this.context = context;
			this.typeSystem = context.CurrentMethod.Module.TypeSystem;
			
			foreach(ILLabel target in method.GetSelfAndChildrenRecursive<ILExpression>(e => e.IsBranch()).SelectMany(e => e.GetBranchTargets())) {
				labelGlobalRefCount[target] = labelGlobalRefCount.GetOrDefault(target) + 1;
			}
			foreach(ILBasicBlock bb in method.GetSelfAndChildrenRecursive<ILBasicBlock>()) {
				foreach(ILLabel label in bb.GetChildren().OfType<ILLabel>()) {
					labelToBasicBlock[label] = bb;
				}
			}
		}
		
		public bool SimplifyTernaryOperator(List<ILNode> body, ILBasicBlock head, int pos)
		{
			Debug.Assert(body.Contains(head));
			
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
			
			if (head.MatchLastAndBr(ILCode.Brtrue, out trueLabel, out condExpr, out falseLabel) &&
			    labelGlobalRefCount[trueLabel] == 1 &&
			    labelGlobalRefCount[falseLabel] == 1 &&
			    ((labelToBasicBlock[trueLabel].MatchSingleAndBr(ILCode.Stloc, out trueLocVar, out trueExpr, out trueFall) &&
			      labelToBasicBlock[falseLabel].MatchSingleAndBr(ILCode.Stloc, out falseLocVar, out falseExpr, out falseFall) &&
			      trueLocVar == falseLocVar && trueFall == falseFall) ||
			     (labelToBasicBlock[trueLabel].MatchSingle(ILCode.Ret, out unused, out trueExpr) &&
			      labelToBasicBlock[falseLabel].MatchSingle(ILCode.Ret, out unused, out falseExpr))) &&
			    body.Contains(labelToBasicBlock[trueLabel]) &&
			    body.Contains(labelToBasicBlock[falseLabel])
			   )
			{
				bool isStloc = trueLocVar != null;
				ILCode opCode = isStloc ? ILCode.Stloc : ILCode.Ret;
				TypeReference retType = isStloc ? trueLocVar.Type : this.context.CurrentMethod.ReturnType;
				bool retTypeIsBoolean = TypeAnalysis.IsBoolean(retType);
				int leftBoolVal;
				int rightBoolVal;
				ILExpression newExpr;
				// a ? true:false  is equivalent to  a
				// a ? false:true  is equivalent to  !a
				// a ? true : b    is equivalent to  a || b
				// a ? b : true    is equivalent to  !a || b
				// a ? b : false   is equivalent to  a && b
				// a ? false : b   is equivalent to  !a && b
				if (retTypeIsBoolean &&
				    trueExpr.Match(ILCode.Ldc_I4, out leftBoolVal) &&
				    falseExpr.Match(ILCode.Ldc_I4, out rightBoolVal) &&
				    ((leftBoolVal != 0 && rightBoolVal == 0) || (leftBoolVal == 0 && rightBoolVal != 0))
				   )
				{
					// It can be expressed as trivilal expression
					if (leftBoolVal != 0) {
						newExpr = condExpr;
					} else {
						newExpr = new ILExpression(ILCode.LogicNot, null, condExpr) { InferredType = typeSystem.Boolean };
					}
				} else if ((retTypeIsBoolean || TypeAnalysis.IsBoolean(falseExpr.InferredType)) && trueExpr.Match(ILCode.Ldc_I4, out leftBoolVal) && (leftBoolVal == 0 || leftBoolVal == 1)) {
					// It can be expressed as logical expression
					if (leftBoolVal != 0) {
						newExpr = MakeLeftAssociativeShortCircuit(ILCode.LogicOr, condExpr, falseExpr);
					} else {
						newExpr = MakeLeftAssociativeShortCircuit(ILCode.LogicAnd, new ILExpression(ILCode.LogicNot, null, condExpr), falseExpr);
					}
				} else if ((retTypeIsBoolean || TypeAnalysis.IsBoolean(trueExpr.InferredType)) && falseExpr.Match(ILCode.Ldc_I4, out rightBoolVal) && (rightBoolVal == 0 || rightBoolVal == 1)) {
					// It can be expressed as logical expression
					if (rightBoolVal != 0) {
						newExpr = MakeLeftAssociativeShortCircuit(ILCode.LogicOr, new ILExpression(ILCode.LogicNot, null, condExpr), trueExpr);
					} else {
						newExpr = MakeLeftAssociativeShortCircuit(ILCode.LogicAnd, condExpr, trueExpr);
					}
				} else {
					// Ternary operator tends to create long complicated return statements
					if (opCode == ILCode.Ret)
						return false;
					
					// Only simplify generated variables
					if (opCode == ILCode.Stloc && !trueLocVar.IsGenerated)
						return false;
					
					// Create ternary expression
					newExpr = new ILExpression(ILCode.TernaryOp, null, condExpr, trueExpr, falseExpr);
				}
				
				head.Body.RemoveTail(ILCode.Brtrue, ILCode.Br);
				head.Body.Add(new ILExpression(opCode, trueLocVar, newExpr));
				if (isStloc)
					head.Body.Add(new ILExpression(ILCode.Br, trueFall));
				
				// Remove the old basic blocks
				body.RemoveOrThrow(labelToBasicBlock[trueLabel]);
				body.RemoveOrThrow(labelToBasicBlock[falseLabel]);
				
				return true;
			}
			return false;
		}
		
		public bool SimplifyNullCoalescing(List<ILNode> body, ILBasicBlock head, int pos)
		{
			// ...
			// v = ldloc(leftVar)
			// brtrue(endBBLabel, ldloc(leftVar))
			// br(rightBBLabel)
			//
			// rightBBLabel:
			// v = rightExpr
			// br(endBBLabel)
			// ...
			// =>
			// ...
			// v = NullCoalescing(ldloc(leftVar), rightExpr)
			// br(endBBLabel)
			
			ILVariable v, v2;
			ILExpression leftExpr, leftExpr2;
			ILVariable leftVar;
			ILLabel endBBLabel, endBBLabel2;
			ILLabel rightBBLabel;
			ILBasicBlock rightBB;
			ILExpression rightExpr;
			if (head.Body.Count >= 3 &&
			    head.Body[head.Body.Count - 3].Match(ILCode.Stloc, out v, out leftExpr) &&
			    leftExpr.Match(ILCode.Ldloc, out leftVar) &&
			    head.MatchLastAndBr(ILCode.Brtrue, out endBBLabel, out leftExpr2, out rightBBLabel) &&
			    leftExpr2.MatchLdloc(leftVar) &&
			    labelToBasicBlock.TryGetValue(rightBBLabel, out rightBB) &&
			    rightBB.MatchSingleAndBr(ILCode.Stloc, out v2, out rightExpr, out endBBLabel2) &&
			    v == v2 &&
			    endBBLabel == endBBLabel2 &&
			    labelGlobalRefCount.GetOrDefault(rightBBLabel) == 1 &&
			    body.Contains(rightBB)
			   )
			{
				head.Body.RemoveTail(ILCode.Stloc, ILCode.Brtrue, ILCode.Br);
				head.Body.Add(new ILExpression(ILCode.Stloc, v, new ILExpression(ILCode.NullCoalescing, null, leftExpr, rightExpr)));
				head.Body.Add(new ILExpression(ILCode.Br, endBBLabel));
				
				body.RemoveOrThrow(labelToBasicBlock[rightBBLabel]);
				return true;
			}
			return false;
		}
		
		public bool SimplifyShortCircuit(List<ILNode> body, ILBasicBlock head, int pos)
		{
			Debug.Assert(body.Contains(head));
			
			ILExpression condExpr;
			ILLabel trueLabel;
			ILLabel falseLabel;
			if(head.MatchLastAndBr(ILCode.Brtrue, out trueLabel, out condExpr, out falseLabel)) {
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
					if (body.Contains(nextBasicBlock) &&
					    nextBasicBlock != head &&
					    labelGlobalRefCount[(ILLabel)nextBasicBlock.Body.First()] == 1 &&
					    nextBasicBlock.MatchSingleAndBr(ILCode.Brtrue, out nextTrueLablel, out nextCondExpr, out nextFalseLabel) &&
					    (otherLablel == nextFalseLabel || otherLablel == nextTrueLablel))
					{
						// Create short cicuit branch
						ILExpression logicExpr;
						if (otherLablel == nextFalseLabel) {
							logicExpr = MakeLeftAssociativeShortCircuit(ILCode.LogicAnd, negate ? new ILExpression(ILCode.LogicNot, null, condExpr) : condExpr, nextCondExpr);
						} else {
							logicExpr = MakeLeftAssociativeShortCircuit(ILCode.LogicOr, negate ? condExpr : new ILExpression(ILCode.LogicNot, null, condExpr), nextCondExpr);
						}
						head.Body.RemoveTail(ILCode.Brtrue, ILCode.Br);
						head.Body.Add(new ILExpression(ILCode.Brtrue, nextTrueLablel, logicExpr));
						head.Body.Add(new ILExpression(ILCode.Br, nextFalseLabel));
						
						// Remove the inlined branch from scope
						body.RemoveOrThrow(nextBasicBlock);
						
						return true;
					}
				}
			}
			return false;
		}
		
		public bool SimplifyCustomShortCircuit(List<ILNode> body, ILBasicBlock head, int pos)
		{
			Debug.Assert(body.Contains(head));
			
			// --- looking for the following pattern ---
			// stloc(targetVar, leftVar)
			// brtrue(exitLabel, call(op_False, leftVar)
			// br(followingBlock)
			//
			// FollowingBlock:
			// stloc(targetVar, call(op_BitwiseAnd, leftVar, rightExpression))
			// br(exitLabel)
			// ---
			
			if (head.Body.Count < 3)
				return false;
			
			// looking for:
			// stloc(targetVar, leftVar)
			ILVariable targetVar;
			ILExpression targetVarInitExpr;
			if (!head.Body[head.Body.Count - 3].Match(ILCode.Stloc, out targetVar, out targetVarInitExpr))
				return false;
			
			ILVariable leftVar;
			if (!targetVarInitExpr.Match(ILCode.Ldloc, out leftVar))
				return false;
			
			// looking for:
			// brtrue(exitLabel, call(op_False, leftVar)
			// br(followingBlock)
			ILExpression callExpr;
			ILLabel exitLabel;
			ILLabel followingBlock;
			if(!head.MatchLastAndBr(ILCode.Brtrue, out exitLabel, out callExpr, out followingBlock))
				return false;
			
			if (labelGlobalRefCount[followingBlock] > 1)
				return false;
			
			MethodReference opFalse;
			ILExpression opFalseArg;
			if (!callExpr.Match(ILCode.Call, out opFalse, out opFalseArg))
				return false;
			
			// ignore operators other than op_False and op_True
			if (opFalse.Name != "op_False" && opFalse.Name != "op_True")
				return false;
			
			if (!opFalseArg.MatchLdloc(leftVar))
				return false;
			
			ILBasicBlock followingBasicBlock = labelToBasicBlock[followingBlock];
			
			// FollowingBlock:
			// stloc(targetVar, call(op_BitwiseAnd, leftVar, rightExpression))
			// br(exitLabel)
			ILVariable _targetVar;
			ILExpression opBitwiseCallExpr;
			ILLabel _exitLabel;
			if (!followingBasicBlock.MatchSingleAndBr(ILCode.Stloc, out _targetVar, out opBitwiseCallExpr, out _exitLabel))
				return false;
			
			if (_targetVar != targetVar || exitLabel != _exitLabel)
				return false;
			
			MethodReference opBitwise;
			ILExpression leftVarExpression;
			ILExpression rightExpression;
			if (!opBitwiseCallExpr.Match(ILCode.Call, out opBitwise, out leftVarExpression, out rightExpression))
				return false;
			
			if (!leftVarExpression.MatchLdloc(leftVar))
				return false;
			
			// ignore operators other than op_BitwiseAnd and op_BitwiseOr
			if (opBitwise.Name != "op_BitwiseAnd" && opBitwise.Name != "op_BitwiseOr")
				return false;
			
			// insert:
			// stloc(targetVar, LogicAnd(C::op_BitwiseAnd, leftVar, rightExpression)
			// br(exitLabel)
			ILCode op = opBitwise.Name == "op_BitwiseAnd" ? ILCode.LogicAnd : ILCode.LogicOr;
			
			if (op == ILCode.LogicAnd && opFalse.Name != "op_False")
				return false;
			
			if (op == ILCode.LogicOr && opFalse.Name != "op_True")
				return false;
			
			ILExpression shortCircuitExpr = MakeLeftAssociativeShortCircuit(op, opFalseArg, rightExpression);
			shortCircuitExpr.Operand = opBitwise;
			
			head.Body.RemoveTail(ILCode.Stloc, ILCode.Brtrue, ILCode.Br);
			head.Body.Add(new ILExpression(ILCode.Stloc, targetVar, shortCircuitExpr));
			head.Body.Add(new ILExpression(ILCode.Br, exitLabel));
			body.Remove(followingBasicBlock);
			
			return true;
		}
		
		ILExpression MakeLeftAssociativeShortCircuit(ILCode code, ILExpression left, ILExpression right)
		{
			// Assuming that the inputs are already left associative
			if (right.Match(code)) {
				// Find the leftmost logical expression
				ILExpression current = right;
				while(current.Arguments[0].Match(code))
					current = current.Arguments[0];
				current.Arguments[0] = new ILExpression(code, null, left, current.Arguments[0]) { InferredType = typeSystem.Boolean };
				return right;
			} else {
				return new ILExpression(code, null, left, right) { InferredType = typeSystem.Boolean };
			}
		}
		
		public bool JoinBasicBlocks(List<ILNode> body, ILBasicBlock head, int pos)
		{
			ILLabel nextLabel;
			ILBasicBlock nextBB;
			if (!head.Body.ElementAtOrDefault(head.Body.Count - 2).IsConditionalControlFlow() &&
			    head.Body.Last().Match(ILCode.Br, out nextLabel) &&
			    labelGlobalRefCount[nextLabel] == 1 &&
			    labelToBasicBlock.TryGetValue(nextLabel, out nextBB) &&
			    body.Contains(nextBB) &&
			    nextBB.Body.First() == nextLabel &&
			    !nextBB.Body.OfType<ILTryCatchBlock>().Any()
			   )
			{
				head.Body.RemoveTail(ILCode.Br);
				nextBB.Body.RemoveAt(0);  // Remove label
				head.Body.AddRange(nextBB.Body);
				
				body.RemoveOrThrow(nextBB);
				return true;
			}
			return false;
		}
	}
}
