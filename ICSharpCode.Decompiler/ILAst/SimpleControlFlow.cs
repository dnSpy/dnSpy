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

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using dnlib.DotNet;

namespace ICSharpCode.Decompiler.ILAst {
	public class SimpleControlFlow
	{
		readonly Dictionary<ILLabel, int> labelGlobalRefCount = new Dictionary<ILLabel, int>();
		readonly Dictionary<ILLabel, ILBasicBlock> labelToBasicBlock = new Dictionary<ILLabel, ILBasicBlock>();
		readonly List<ILExpression> List_ILExpression = new List<ILExpression>();
		readonly List<ILBasicBlock> List_ILBasicBlock = new List<ILBasicBlock>();

		DecompilerContext context;
		ICorLibTypes corLib;
		
		public SimpleControlFlow(DecompilerContext context, ILBlock method)
		{
			Initialize(context, method);
		}

		public void Initialize(DecompilerContext context, ILBlock method)
		{
			this.labelGlobalRefCount.Clear();
			this.labelToBasicBlock.Clear();
			this.context = context;
			this.corLib = context.CurrentMethod.Module.CorLibTypes;
			
			foreach (var e in method.GetSelfAndChildrenRecursive<ILExpression>(List_ILExpression, e => e.IsBranch())) {
				foreach (var target in e.GetBranchTargets())
					labelGlobalRefCount[target] = labelGlobalRefCount.GetOrDefault(target) + 1;
			}
			foreach(ILBasicBlock bb in method.GetSelfAndChildrenRecursive<ILBasicBlock>(List_ILBasicBlock)) {
				int index = 0;
				for (;;) {
					var node = bb.GetNext(ref index);
					if (node == null)
						break;
					var label = node as ILLabel;
					if (label == null)
						continue;
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
				TypeSig retType = isStloc ? trueLocVar.Type : this.context.CurrentMethod.ReturnType;
				bool retTypeIsBoolean = retType.GetElementType() == ElementType.Boolean;
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
						newExpr = new ILExpression(ILCode.LogicNot, null, condExpr) { InferredType = corLib.Boolean };
					}
				} else if ((retTypeIsBoolean || falseExpr.InferredType.GetElementType() == ElementType.Boolean) && trueExpr.Match(ILCode.Ldc_I4, out leftBoolVal) && (leftBoolVal == 0 || leftBoolVal == 1)) {
					// It can be expressed as logical expression
					if (leftBoolVal != 0) {
						newExpr = MakeLeftAssociativeShortCircuit(ILCode.LogicOr, condExpr, falseExpr);
					} else {
						newExpr = MakeLeftAssociativeShortCircuit(ILCode.LogicAnd, new ILExpression(ILCode.LogicNot, null, condExpr), falseExpr);
					}
				} else if ((retTypeIsBoolean || trueExpr.InferredType.GetElementType() == ElementType.Boolean) && falseExpr.Match(ILCode.Ldc_I4, out rightBoolVal) && (rightBoolVal == 0 || rightBoolVal == 1)) {
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
					if (opCode == ILCode.Stloc && !trueLocVar.GeneratedByDecompiler)
						return false;
					
					// Create ternary expression
					newExpr = new ILExpression(ILCode.TernaryOp, null, condExpr, trueExpr, falseExpr);
				}
				
				var tail = head.Body.RemoveTail(ILCode.Brtrue, ILCode.Br);
				var listNodes = new List<ILNode>();
				var newExprNodes = newExpr.GetSelfAndChildrenRecursive<ILNode>(listNodes).ToArray();
				foreach (var node in labelToBasicBlock[trueLabel].GetSelfAndChildrenRecursive<ILNode>(listNodes).Except(newExprNodes))
					newExpr.ILRanges.AddRange(node.AllILRanges);
				foreach (var node in labelToBasicBlock[falseLabel].GetSelfAndChildrenRecursive<ILNode>(listNodes).Except(newExprNodes))
					newExpr.ILRanges.AddRange(node.AllILRanges);
				newExpr.ILRanges.AddRange(tail[0].ILRanges);
				tail[1].AddSelfAndChildrenRecursiveILRanges(newExpr.ILRanges);

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
				var tail = head.Body.RemoveTail(ILCode.Stloc, ILCode.Brtrue, ILCode.Br);
				ILExpression nullCoal, stloc;
				head.Body.Add(stloc = new ILExpression(ILCode.Stloc, v, nullCoal = new ILExpression(ILCode.NullCoalescing, null, leftExpr, rightExpr)));
				head.Body.Add(new ILExpression(ILCode.Br, endBBLabel));
				tail[0].AddSelfAndChildrenRecursiveILRanges(stloc.ILRanges);
				tail[1].AddSelfAndChildrenRecursiveILRanges(nullCoal.ILRanges);
				tail[2].AddSelfAndChildrenRecursiveILRanges(rightExpr.ILRanges);	// br (to rightBB)
				rightExpr.ILRanges.AddRange(rightBB.AllILRanges);
				rightBB.Body[0].AddSelfAndChildrenRecursiveILRanges(rightExpr.ILRanges);	// label
				rightExpr.ILRanges.AddRange(rightBB.Body[1].ILRanges);      // stloc: no recursive add
				rightBB.Body[2].AddSelfAndChildrenRecursiveILRanges(rightExpr.ILRanges);	// br
				
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
						var tail = head.Body.RemoveTail(ILCode.Brtrue, ILCode.Br);
						nextCondExpr.ILRanges.AddRange(tail[0].ILRanges);	// brtrue
						nextCondExpr.ILRanges.AddRange(nextBasicBlock.ILRanges);
						nextBasicBlock.Body[0].AddSelfAndChildrenRecursiveILRanges(nextCondExpr.ILRanges);	// label
						nextCondExpr.ILRanges.AddRange(nextBasicBlock.Body[1].ILRanges);	// brtrue

						head.Body.Add(new ILExpression(ILCode.Brtrue, nextTrueLablel, logicExpr));
						ILExpression brFalseLbl;
						head.Body.Add(brFalseLbl = new ILExpression(ILCode.Br, nextFalseLabel));
						nextBasicBlock.Body[2].AddSelfAndChildrenRecursiveILRanges(brFalseLbl.ILRanges);	// br
						brFalseLbl.ILRanges.AddRange(nextBasicBlock.EndILRanges);
						tail[1].AddSelfAndChildrenRecursiveILRanges(brFalseLbl.ILRanges); // br
						
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
			
			IMethod opFalse;
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
			
			IMethod opBitwise;
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
			
			var tail = head.Body.RemoveTail(ILCode.Stloc, ILCode.Brtrue, ILCode.Br);
			//TODO: Keep tail's ILRanges
			//TODO: Keep ILRanges of other things that are removed by this method
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
				current.Arguments[0].AddSelfAndChildrenRecursiveILRanges(current.ILRanges);
				current.Arguments[0] = new ILExpression(code, null, left, current.Arguments[0]) { InferredType = corLib.Boolean };
				return right;
			} else {
				return new ILExpression(code, null, left, right) { InferredType = corLib.Boolean };
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
				var tail = head.Body.RemoveTail(ILCode.Br);
				tail[0].AddSelfAndChildrenRecursiveILRanges(nextBB.ILRanges);
				nextBB.Body[0].AddSelfAndChildrenRecursiveILRanges(nextBB.ILRanges);
				nextBB.Body.RemoveAt(0);  // Remove label
				if (head.Body.Count > 0)
					head.Body[head.Body.Count - 1].EndILRanges.AddRange(nextBB.ILRanges);
				else
					head.ILRanges.AddRange(nextBB.ILRanges);
				head.EndILRanges.AddRange(nextBB.EndILRanges);
				head.Body.AddRange(nextBB.Body);
				
				body.RemoveOrThrow(nextBB);
				return true;
			}
			return false;
		}
	}
}
