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
using dnlib.DotNet;

namespace ICSharpCode.Decompiler.ILAst {
	/// <summary>
	/// Performs inlining transformations.
	/// </summary>
	public class ILInlining
	{
		ILBlock method;
		internal readonly Dictionary<ILVariable, int> numStloc  = new Dictionary<ILVariable, int>();
		internal readonly Dictionary<ILVariable, int> numLdloc  = new Dictionary<ILVariable, int>();
		internal readonly Dictionary<ILVariable, int> numLdloca = new Dictionary<ILVariable, int>();
		readonly List<ILBlock> list_ILBlock = new List<ILBlock>();
		readonly List<ILExpression> list_ILExpression = new List<ILExpression>();
		readonly List<ILNode> list_ILNode = new List<ILNode>();

		public ILInlining(ILBlock method)
		{
			Initialize(method);
		}

		public void Initialize(ILBlock method)
		{
			this.method = method;
			AnalyzeMethod();
		}

		void AnalyzeMethod()
		{
			numStloc.Clear();
			numLdloc.Clear();
			numLdloca.Clear();
			
			// Analyse the whole method
			AnalyzeNode(method);
		}
		
		/// <summary>
		/// For each variable reference, adds <paramref name="direction"/> to the num* dicts.
		/// Direction will be 1 for analysis, and -1 when removing a node from analysis.
		/// </summary>
		void AnalyzeNode(ILNode node, int direction = 1)
		{
			ILExpression expr = node as ILExpression;
			if (expr != null) {
				ILVariable locVar = expr.Operand as ILVariable;
				if (locVar != null) {
					if (expr.Code == ILCode.Stloc) {
						numStloc[locVar] = numStloc.GetOrDefault(locVar) + direction;
					} else if (expr.Code == ILCode.Ldloc) {
						numLdloc[locVar] = numLdloc.GetOrDefault(locVar) + direction;
					} else if (expr.Code == ILCode.Ldloca) {
						numLdloca[locVar] = numLdloca.GetOrDefault(locVar) + direction;
					} else {
						throw new NotSupportedException(expr.Code.ToString());
					}
				}
				foreach (ILExpression child in expr.Arguments)
					AnalyzeNode(child, direction);
			} else {
				var catchBlock = node as ILTryCatchBlock.CatchBlock;
				if (catchBlock != null && catchBlock.ExceptionVariable != null) {
					numStloc[catchBlock.ExceptionVariable] = numStloc.GetOrDefault(catchBlock.ExceptionVariable) + direction;
				}
				
				foreach (ILNode child in node.GetChildren())
					AnalyzeNode(child, direction);
			}
		}

		public bool InlineAllVariables()
		{
			bool modified = false;
			ILInlining i = GetILInlining(method);
			foreach (ILBlock block in method.GetSelfAndChildrenRecursive<ILBlock>(list_ILBlock))
				modified |= i.InlineAllInBlock(block);
			return modified;
		}

		ILInlining GetILInlining(ILBlock method)
		{
			if (cached_ILInlining == null)
				cached_ILInlining = new ILInlining(method);
			else
				cached_ILInlining.Initialize(method);
			return cached_ILInlining;
		}
		ILInlining cached_ILInlining;
		
		public bool InlineAllInBlock(ILBlock block)
		{
			bool modified = false;
			List<ILNode> body = block.Body;
			if (block is ILTryCatchBlock.CatchBlock && body.Count > 1) {
				ILVariable v = ((ILTryCatchBlock.CatchBlock)block).ExceptionVariable;
				if (v != null && v.GeneratedByDecompiler) {
					if (numLdloca.GetOrDefault(v) == 0 && numStloc.GetOrDefault(v) == 1 && numLdloc.GetOrDefault(v) == 1) {
						ILVariable v2;
						ILExpression ldException;
						if (body[0].Match(ILCode.Stloc, out v2, out ldException) && ldException.MatchLdloc(v)) {
							body[0].AddSelfAndChildrenRecursiveILRanges(((ILTryCatchBlock.CatchBlock)block).StlocILRanges);
							body.RemoveAt(0);
							((ILTryCatchBlock.CatchBlock)block).ExceptionVariable = v2;
							modified = true;
						}
					}
				}
			}
			for(int i = 0; i < body.Count - 1;) {
				ILVariable locVar;
				ILExpression expr;
				if (body[i].Match(ILCode.Stloc, out locVar, out expr) && InlineOneIfPossible(block, block.Body, i, aggressive: false)) {
					modified = true;
					i = Math.Max(0, i - 1); // Go back one step
				} else {
					i++;
				}
			}
			foreach(ILBasicBlock bb in body.OfType<ILBasicBlock>()) {
				modified |= InlineAllInBasicBlock(bb);
			}
			return modified;
		}
		
		public bool InlineAllInBasicBlock(ILBasicBlock bb)
		{
			bool modified = false;
			List<ILNode> body = bb.Body;
			for(int i = 0; i < body.Count;) {
				ILVariable locVar;
				ILExpression expr;
				if (body[i].Match(ILCode.Stloc, out locVar, out expr) && InlineOneIfPossible(bb, bb.Body, i, aggressive: false)) {
					modified = true;
					i = Math.Max(0, i - 1); // Go back one step
				} else {
					i++;
				}
			}
			return modified;
		}
		
		/// <summary>
		/// Inlines instructions before pos into block.Body[pos].
		/// </summary>
		/// <returns>The number of instructions that were inlined.</returns>
		public int InlineInto(ILBlockBase block, List<ILNode> body, int pos, bool aggressive)
		{
			if (pos >= body.Count)
				return 0;
			int count = 0;
			while (--pos >= 0) {
				ILExpression expr = body[pos] as ILExpression;
				if (expr == null || expr.Code != ILCode.Stloc)
					break;
				if (InlineOneIfPossible(block, body, pos, aggressive))
					count++;
				else
					break;
			}
			return count;
		}
		
		/// <summary>
		/// Aggressively inlines the stloc instruction at block.Body[pos] into the next instruction, if possible.
		/// If inlining was possible; we will continue to inline (non-aggressively) into the the combined instruction.
		/// </summary>
		/// <remarks>
		/// After the operation, pos will point to the new combined instruction.
		/// </remarks>
		public bool InlineIfPossible(ILBlockBase block, List<ILNode> body, ref int pos)
		{
			if (InlineOneIfPossible(block, body, pos, true)) {
				pos -= InlineInto(block, body, pos, false);
				return true;
			}
			return false;
		}
		
		/// <summary>
		/// Inlines the stloc instruction at block.Body[pos] into the next instruction, if possible.
		/// </summary>
		public bool InlineOneIfPossible(ILBlockBase block, List<ILNode> body, int pos, bool aggressive)
		{
			ILVariable v;
			ILExpression inlinedExpression;
			if (body[pos].Match(ILCode.Stloc, out v, out inlinedExpression) && !v.IsPinned) {
				if (InlineIfPossible(v, inlinedExpression, body.ElementAtOrDefault(pos+1), aggressive)) {
					// Assign the ranges of the stloc instruction:
					inlinedExpression.ILRanges.AddRange(body[pos].ILRanges);
					// Remove the stloc instruction:
					body.RemoveAt(pos);
					return true;
				} else if (numLdloc.GetOrDefault(v) == 0 && numLdloca.GetOrDefault(v) == 0) {
					// The variable is never loaded
					if (inlinedExpression.HasNoSideEffects()) {
						// Remove completely
						AnalyzeNode(body[pos], -1);
						Utils.AddILRanges(block, body, pos);
						body.RemoveAt(pos);
						return true;
					} else if (inlinedExpression.CanBeExpressionStatement() && v.GeneratedByDecompiler) {
						// Assign the ranges of the stloc instruction:
						inlinedExpression.ILRanges.AddRange(body[pos].ILRanges);
						// Remove the stloc, but keep the inner expression
						body[pos] = inlinedExpression;
						return true;
					}
				}
			}
			return false;
		}
		
		/// <summary>
		/// Inlines 'expr' into 'next', if possible.
		/// </summary>
		bool InlineIfPossible(ILVariable v, ILExpression inlinedExpression, ILNode next, bool aggressive)
		{
			// ensure the variable is accessed only a single time
			if (numStloc.GetOrDefault(v) != 1)
				return false;
			int ldloc = numLdloc.GetOrDefault(v);
			if (ldloc > 1 || ldloc + numLdloca.GetOrDefault(v) != 1)
				return false;
			
			if (next is ILCondition)
				next = ((ILCondition)next).Condition;
			else if (next is ILWhileLoop)
				next = ((ILWhileLoop)next).Condition;
			
			ILExpression parent;
			int pos;
			if (FindLoadInNext(next as ILExpression, v, inlinedExpression, out parent, out pos) == true) {
				if (ldloc == 0) {
					if (!IsGeneratedValueTypeTemporary((ILExpression)next, parent, pos, v, inlinedExpression))
						return false;
				} else {
					if (!aggressive && !v.GeneratedByDecompiler && !NonAggressiveInlineInto((ILExpression)next, parent, inlinedExpression))
						return false;
				}

				// Assign the ranges of the ldloc instruction:
				parent.Arguments[pos].AddSelfAndChildrenRecursiveILRanges(inlinedExpression.ILRanges);
				
				if (ldloc == 0) {
					// it was an ldloca instruction, so we need to use the pseudo-opcode 'addressof' so that the types
					// comes out correctly
					parent.Arguments[pos] = new ILExpression(ILCode.AddressOf, null, inlinedExpression);
				} else {
					parent.Arguments[pos] = inlinedExpression;
				}
				return true;
			}
			return false;
		}

		/// <summary>
		/// Is this a temporary variable generated by the C# compiler for instance method calls on value type values
		/// </summary>
		/// <param name="next">The next top-level expression</param>
		/// <param name="parent">The direct parent of the load within 'next'</param>
		/// <param name="pos">Index of the load within 'parent'</param>
		/// <param name="v">The variable being inlined.</param>
		/// <param name="inlinedExpression">The expression being inlined</param>
		bool IsGeneratedValueTypeTemporary(ILExpression next, ILExpression parent, int pos, ILVariable v, ILExpression inlinedExpression)
		{
			if (pos == 0 && v.Type != null && DnlibExtensions.IsValueType(v.Type)) {
				// Inlining a value type variable is allowed only if the resulting code will maintain the semantics
				// that the method is operating on a copy.
				// Thus, we have to disallow inlining of other locals, fields, array elements, dereferenced pointers
				switch (inlinedExpression.Code) {
					case ILCode.Ldloc:
					case ILCode.Stloc:
					case ILCode.CompoundAssignment:
					case ILCode.Ldelem:
					case ILCode.Ldelem_I:
					case ILCode.Ldelem_I1:
					case ILCode.Ldelem_I2:
					case ILCode.Ldelem_I4:
					case ILCode.Ldelem_I8:
					case ILCode.Ldelem_R4:
					case ILCode.Ldelem_R8:
					case ILCode.Ldelem_Ref:
					case ILCode.Ldelem_U1:
					case ILCode.Ldelem_U2:
					case ILCode.Ldelem_U4:
					case ILCode.Ldobj:
					case ILCode.Ldind_Ref:
						return false;
					case ILCode.Ldfld:
					case ILCode.Stfld:
					case ILCode.Ldsfld:
					case ILCode.Stsfld:
						// allow inlining field access only if it's a readonly field
						FieldDef f = ((IField)inlinedExpression.Operand).Resolve();
						if (!(f != null && f.IsInitOnly))
							return false;
						break;
					case ILCode.Call:
					case ILCode.CallGetter:
						// inlining runs both before and after IntroducePropertyAccessInstructions,
						// so we have to handle both 'call' and 'callgetter'
						IMethod mr = (IMethod)inlinedExpression.Operand;
						// ensure that it's not an multi-dimensional array getter
						TypeSig ts;
						if (mr.DeclaringType is TypeSpec && (ts = ((TypeSpec)mr.DeclaringType).TypeSig.RemovePinnedAndModifiers()) != null && ts.IsSingleOrMultiDimensionalArray)
							return false;
						goto case ILCode.Callvirt;
					case ILCode.Callvirt:
					case ILCode.CallvirtGetter:
						// don't inline foreach loop variables:
						mr = (IMethod)inlinedExpression.Operand;
						if (mr.Name == "get_Current" && mr.MethodSig != null && mr.MethodSig.HasThis)
							return false;
						break;
					case ILCode.Castclass:
					case ILCode.Unbox_Any:
						// These are valid, but might occur as part of a foreach loop variable.
						ILExpression arg = inlinedExpression.Arguments[0];
						if (arg.Code == ILCode.CallGetter || arg.Code == ILCode.CallvirtGetter || arg.Code == ILCode.Call || arg.Code == ILCode.Callvirt) {
							mr = (IMethod)arg.Operand;
							if (mr.Name == "get_Current" && mr.MethodSig != null && mr.MethodSig.HasThis)
								return false; // looks like a foreach loop variable, so don't inline it
						}
						break;
				}
				
				// inline the compiler-generated variable that are used when accessing a member on a value type:
				switch (parent.Code) {
					case ILCode.Call:
					case ILCode.CallGetter:
					case ILCode.CallSetter:
					case ILCode.Callvirt:
					case ILCode.CallvirtGetter:
					case ILCode.CallvirtSetter:
						IMethod mr = parent.Operand as IMethod;
						return mr == null || mr.MethodSig == null ? false : mr.MethodSig.HasThis;
					case ILCode.Stfld:
					case ILCode.Ldfld:
					case ILCode.Ldflda:
					case ILCode.Await:
						return true;
				}
			}
			return false;
		}
		
		/// <summary>
		/// Determines whether a variable should be inlined in non-aggressive mode, even though it is not a generated variable.
		/// </summary>
		/// <param name="next">The next top-level expression</param>
		/// <param name="parent">The direct parent of the load within 'next'</param>
		/// <param name="inlinedExpression">The expression being inlined</param>
		bool NonAggressiveInlineInto(ILExpression next, ILExpression parent, ILExpression inlinedExpression)
		{
			if (inlinedExpression.Code == ILCode.DefaultValue)
				return true;
			
			switch (next.Code) {
				case ILCode.Ret:
				case ILCode.Brtrue:
					return parent == next;
				case ILCode.Switch:
					return parent == next || (parent.Code == ILCode.Sub && parent == next.Arguments[0]);
				default:
					return false;
			}
		}
		
		/// <summary>
		/// Gets whether 'expressionBeingMoved' can be inlined into 'expr'.
		/// </summary>
		public bool CanInlineInto(ILExpression expr, ILVariable v, ILExpression expressionBeingMoved)
		{
			ILExpression parent;
			int pos;
			return FindLoadInNext(expr, v, expressionBeingMoved, out parent, out pos) == true;
		}
		
		/// <summary>
		/// Finds the position to inline to.
		/// </summary>
		/// <returns>true = found; false = cannot continue search; null = not found</returns>
		bool? FindLoadInNext(ILExpression expr, ILVariable v, ILExpression expressionBeingMoved, out ILExpression parent, out int pos)
		{
			parent = null;
			pos = 0;
			if (expr == null)
				return false;
			for (int i = 0; i < expr.Arguments.Count; i++) {
				// Stop when seeing an opcode that does not guarantee that its operands will be evaluated.
				// Inlining in that case might result in the inlined expresion not being evaluted.
				if (i == 1 && (expr.Code == ILCode.LogicAnd || expr.Code == ILCode.LogicOr || expr.Code == ILCode.TernaryOp || expr.Code == ILCode.NullCoalescing))
					return false;
				
				ILExpression arg = expr.Arguments[i];
				
				if ((arg.Code == ILCode.Ldloc || arg.Code == ILCode.Ldloca) && arg.Operand == v) {
					parent = expr;
					pos = i;
					return true;
				}
				bool? r = FindLoadInNext(arg, v, expressionBeingMoved, out parent, out pos);
				if (r != null)
					return r;
			}
			if (IsSafeForInlineOver(expr, expressionBeingMoved))
				return null; // continue searching
			else
				return false; // abort, inlining not possible
		}

		/// <summary>
		/// Determines whether it is safe to move 'expressionBeingMoved' past 'expr'
		/// </summary>
		bool IsSafeForInlineOver(ILExpression expr, ILExpression expressionBeingMoved)
		{
			switch (expr.Code) {
				case ILCode.Ldloc:
					ILVariable loadedVar = (ILVariable)expr.Operand;
					if (numLdloca.GetOrDefault(loadedVar) != 0) {
						// abort, inlining is not possible
						return false;
					}
					foreach (ILExpression potentialStore in expressionBeingMoved.GetSelfAndChildrenRecursive<ILExpression>(list_ILExpression)) {
						if (potentialStore.Code == ILCode.Stloc && potentialStore.Operand == loadedVar)
							return false;
					}
					// the expression is loading a non-forbidden variable
					return true;
				case ILCode.Ldloca:
				case ILCode.Ldflda:
				case ILCode.Ldsflda:
				case ILCode.Ldelema:
				case ILCode.AddressOf:
				case ILCode.ValueOf:
				case ILCode.NullableOf:
					// address-loading instructions are safe if their arguments are safe
					foreach (ILExpression arg in expr.Arguments) {
						if (!IsSafeForInlineOver(arg, expressionBeingMoved))
							return false;
					}
					return true;
				default:
					// instructions with no side-effects are safe (except for Ldloc and Ldloca which are handled separately)
					return expr.HasNoSideEffects();
			}
		}
		
		/// <summary>
		/// Runs a very simple form of copy propagation.
		/// Copy propagation is used in two cases:
		/// 1) assignments from arguments to local variables
		///    If the target variable is assigned to only once (so always is that argument) and the argument is never changed (no ldarga/starg),
		///    then we can replace the variable with the argument.
		/// 2) assignments of address-loading instructions to local variables
		/// </summary>
		public void CopyPropagation(List<ILNode> newList)
		{
			var newListTemp = newList;
			method.GetSelfAndChildrenRecursive<ILNode>(newList);
			bool recalc = false;
			foreach (var node1 in newList) {
				var block = node1 as ILBlock;
				if (block == null)
					continue;
				for (int i = 0; i < block.Body.Count; i++) {
					ILVariable v;
					ILExpression copiedExpr;
					if (block.Body[i].Match(ILCode.Stloc, out v, out copiedExpr)
					    && !v.IsParameter && numStloc.GetOrDefault(v) == 1 && numLdloca.GetOrDefault(v) == 0
					    && CanPerformCopyPropagation(copiedExpr, v))
					{
						// un-inline the arguments of the ldArg instruction
						ILVariable[] uninlinedArgs = new ILVariable[copiedExpr.Arguments.Count];
						for (int j = 0; j < uninlinedArgs.Length; j++) {
							uninlinedArgs[j] = new ILVariable { GeneratedByDecompiler = true, Name = v.Name + "_cp_" + j };
							block.Body.Insert(i++, new ILExpression(ILCode.Stloc, uninlinedArgs[j], copiedExpr.Arguments[j]));
							recalc = true;
						}
						
						// perform copy propagation:
						foreach (var node2 in newListTemp) {
							var expr = node2 as ILExpression;
							if (expr == null)
								continue;
							if (expr.Code == ILCode.Ldloc && expr.Operand == v) {
								expr.Code = copiedExpr.Code;
								expr.Operand = copiedExpr.Operand;
								for (int j = 0; j < uninlinedArgs.Length; j++) {
									expr.Arguments.Add(new ILExpression(ILCode.Ldloc, uninlinedArgs[j]));
								}
							}
						}
						
						Utils.AddILRanges(block, block.Body, i, block.Body[i].ILRanges);
						Utils.AddILRanges(block, block.Body, i, copiedExpr.ILRanges);
						block.Body.RemoveAt(i);
						if (uninlinedArgs.Length > 0) {
							// if we un-inlined stuff; we need to update the usage counters
							AnalyzeMethod();
						}
						InlineInto(block, block.Body, i, aggressive: false); // maybe inlining gets possible after the removal of block.Body[i]
						i -= uninlinedArgs.Length + 1;

						if (recalc) {
							recalc = false;
							newListTemp = method.GetSelfAndChildrenRecursive<ILNode>(newListTemp == newList ? (newListTemp = list_ILNode) : newListTemp);
						}
					}
				}
			}
		}
		
		bool CanPerformCopyPropagation(ILExpression expr, ILVariable copyVariable)
		{
			switch (expr.Code) {
				case ILCode.Ldloca:
				case ILCode.Ldelema:
				case ILCode.Ldflda:
				case ILCode.Ldsflda:
					// All address-loading instructions always return the same value for a given operand/argument combination,
					// so they can be safely copied.
					return true;
				case ILCode.Ldloc:
					ILVariable v = (ILVariable)expr.Operand;
					if (v.IsParameter) {
						// Parameters can be copied only if they aren't assigned to (directly or indirectly via ldarga)
						return numLdloca.GetOrDefault(v) == 0 && numStloc.GetOrDefault(v) == 0;
					} else {
						// Variables are be copied only if both they and the target copy variable are generated,
						// and if the variable has only a single assignment
						return v.GeneratedByDecompiler && copyVariable.GeneratedByDecompiler && numLdloca.GetOrDefault(v) == 0 && numStloc.GetOrDefault(v) == 1;
					}
				default:
					return false;
			}
		}
	}
}
