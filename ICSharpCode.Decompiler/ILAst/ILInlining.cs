// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Mono.Cecil;

namespace ICSharpCode.Decompiler.ILAst
{
	/// <summary>
	/// Performs inlining transformations.
	/// </summary>
	public class ILInlining
	{
		readonly ILBlock method;
		internal Dictionary<ILVariable, int> numStloc  = new Dictionary<ILVariable, int>();
		internal Dictionary<ILVariable, int> numLdloc  = new Dictionary<ILVariable, int>();
		Dictionary<ILVariable, int> numLdloca = new Dictionary<ILVariable, int>();
		
		Dictionary<ParameterDefinition, int> numStarg  = new Dictionary<ParameterDefinition, int>();
		Dictionary<ParameterDefinition, int> numLdarga = new Dictionary<ParameterDefinition, int>();
		
		public ILInlining(ILBlock method)
		{
			this.method = method;
			// Analyse the whole method
			foreach(ILExpression expr in method.GetSelfAndChildrenRecursive<ILExpression>()) {
				ILVariable locVar = expr.Operand as ILVariable;
				if (locVar != null) {
					if (expr.Code == ILCode.Stloc) {
						numStloc[locVar] = numStloc.GetOrDefault(locVar) + 1;
					} else if (expr.Code == ILCode.Ldloc) {
						numLdloc[locVar] = numLdloc.GetOrDefault(locVar) + 1;
					} else if (expr.Code == ILCode.Ldloca) {
						numLdloca[locVar] = numLdloca.GetOrDefault(locVar) + 1;
					} else {
						throw new NotSupportedException(expr.Code.ToString());
					}
				} else {
					ParameterDefinition pd = expr.Operand as ParameterDefinition;
					if (pd != null) {
						if (expr.Code == ILCode.Starg)
							numStarg[pd] = numStarg.GetOrDefault(pd) + 1;
						else if (expr.Code == ILCode.Ldarga)
							numLdarga[pd] = numLdarga.GetOrDefault(pd) + 1;
					}
				}
			}
		}
		
		public void InlineAllVariables()
		{
			ILInlining i = new ILInlining(method);
			foreach (ILBlock block in method.GetSelfAndChildrenRecursive<ILBlock>())
				i.InlineAllInBlock(block);
		}
		
		public void InlineAllInBlock(ILBlock block)
		{
			List<ILNode> body = block.Body;
			for(int i = 0; i < body.Count - 1;) {
				ILExpression nextExpr = body[i + 1] as ILExpression;
				ILVariable locVar;
				ILExpression expr;
				if (body[i].Match(ILCode.Stloc, out locVar, out expr) && InlineOneIfPossible(block, i, aggressive: false)) {
					i = Math.Max(0, i - 1); // Go back one step
				} else {
					i++;
				}
			}
		}
		
		/// <summary>
		/// Inlines instructions before pos into block.Body[pos].
		/// </summary>
		/// <returns>The number of instructions that were inlined.</returns>
		public int InlineInto(ILBlock block, int pos, bool aggressive)
		{
			if (pos >= block.Body.Count)
				return 0;
			int count = 0;
			while (--pos >= 0) {
				ILExpression expr = block.Body[pos] as ILExpression;
				if (expr == null || expr.Code != ILCode.Stloc)
					break;
				if (InlineOneIfPossible(block, pos, aggressive))
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
		public bool InlineIfPossible(ILBlock block, ref int pos)
		{
			if (InlineOneIfPossible(block, pos, true)) {
				pos -= InlineInto(block, pos, false);
				return true;
			}
			return false;
		}
		
		/// <summary>
		/// Inlines the stloc instruction at block.Body[pos] into the next instruction, if possible.
		/// </summary>
		public bool InlineOneIfPossible(ILBlock block, int pos, bool aggressive)
		{
			ILVariable v;
			ILExpression inlinedExpression;
			if (block.Body[pos].Match(ILCode.Stloc, out v, out inlinedExpression)
			    && InlineIfPossible(v, inlinedExpression, block.Body.ElementAtOrDefault(pos+1), aggressive))
			{
				// Assign the ranges of the stloc instruction:
				inlinedExpression.ILRanges.AddRange(((ILExpression)block.Body[pos]).ILRanges);
				// Remove the stloc instruction:
				block.Body.RemoveAt(pos);
				return true;
			}
			return false;
		}
		
		/// <summary>
		/// Inlines 'expr' into 'next', if possible.
		/// </summary>
		bool InlineIfPossible(ILVariable v, ILExpression inlinedExpression, ILNode next, bool aggressive)
		{
			// ensure the variable is accessed only a single time
			if (!(numStloc.GetOrDefault(v) == 1 && numLdloc.GetOrDefault(v) == 1 && numLdloca.GetOrDefault(v) == 0))
				return false;
			ILExpression parent;
			int pos;
			if (FindLoadInNext(next as ILExpression, v, inlinedExpression, out parent, out pos) == true) {
				if (!aggressive && !v.IsGenerated && !NonAggressiveInlineInto((ILExpression)next, parent))
					return false;
				
				// Assign the ranges of the ldloc instruction:
				inlinedExpression.ILRanges.AddRange(parent.Arguments[pos].ILRanges);
				
				parent.Arguments[pos] = inlinedExpression;
				
				return true;
			}
			return false;
		}
		
		bool NonAggressiveInlineInto(ILExpression next, ILExpression parent)
		{
			switch (next.Code) {
				case ILCode.Ret:
					return parent.Code == ILCode.Ret;
				case ILCode.Brtrue:
					return parent.Code == ILCode.Brtrue;
				case ILCode.Switch:
					return parent.Code == ILCode.Switch || parent.Code == ILCode.Sub;
				default:
					return false;
			}
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
				if (i == 1 && (expr.Code == ILCode.LogicAnd || expr.Code == ILCode.LogicOr || expr.Code == ILCode.TernaryOp))
					return false;
				
				ILExpression arg = expr.Arguments[i];
				
				if (arg.Code == ILCode.Ldloc && arg.Operand == v) {
					parent = expr;
					pos = i;
					return true;
				}
				bool? r = FindLoadInNext(arg, v, expressionBeingMoved, out parent, out pos);
				if (r != null)
					return r;
			}
			switch (expr.Code) {
				case ILCode.Ldloc:
					ILVariable loadedVar = (ILVariable)expr.Operand;
					if (numLdloca.GetOrDefault(loadedVar) != 0) {
						// abort, inlining is not possible
						return false;
					}
					foreach (ILExpression potentialStore in expressionBeingMoved.GetSelfAndChildrenRecursive<ILExpression>()) {
						if (potentialStore.Code == ILCode.Stloc && potentialStore.Operand == loadedVar)
							return false;
					}
					// the expression is loading a non-forbidden variable:
					// we're allowed to continue searching
					return null;
				case ILCode.Ldarg:
					// Also try moving over ldarg instructions - this is necessary because an earlier copy propagation
					// step might have introduced ldarg in place of an ldloc that would be skipped.
					ParameterDefinition loadedParam = (ParameterDefinition)expr.Operand;
					if (numLdarga.GetOrDefault(loadedParam) != 0)
						return false;
					foreach (ILExpression potentialStore in expressionBeingMoved.GetSelfAndChildrenRecursive<ILExpression>()) {
						if (potentialStore.Code == ILCode.Starg && potentialStore.Operand == loadedParam)
							return false;
					}
					return null;
				case ILCode.Ldloca:
				case ILCode.Ldarga:
					// Continue searching:
					// It is always safe to move code past an instruction that loads a constant.
					return null;
				default:
					// abort, inlining is not possible
					return false;
			}
		}
		
		/// <summary>
		/// Runs a very simple form of copy propagation.
		/// Copy propagation is used in two cases:
		/// 1) assignments from arguments to local variables
		///    If the target variable is assigned to only once (so always is that argument) and the argument is never changed (no ldarga/starg),
		///    then we can replace the variable with the argument.
		/// 2) assignments of 'ldloca/ldarga' to local variables
		/// </summary>
		public void CopyPropagation()
		{
			// Perform 'dup' removal prior to copy propagation:
			foreach (ILExpression expr in method.GetSelfAndChildrenRecursive<ILExpression>()) {
				for (int i = 0; i < expr.Arguments.Count; i++) {
					if (expr.Arguments[i].Code == ILCode.Dup) {
						ILExpression child = expr.Arguments[i].Arguments[0];
						child.ILRanges.AddRange(expr.Arguments[i].ILRanges);
						expr.Arguments[i] = child;
					}
				}
			}
			
			foreach (ILBlock block in method.GetSelfAndChildrenRecursive<ILBlock>()) {
				for (int i = 0; i < block.Body.Count; i++) {
					ILVariable v;
					ILExpression ldArg;
					if (block.Body[i].Match(ILCode.Stloc, out v, out ldArg)
					    && numStloc.GetOrDefault(v) == 1 && numLdloca.GetOrDefault(v) == 0
					    && CanPerformCopyPropagation(ldArg))
					{
						// perform copy propagation:
						foreach (var expr in method.GetSelfAndChildrenRecursive<ILExpression>()) {
							if (expr.Code == ILCode.Ldloc && expr.Operand == v) {
								expr.Code = ldArg.Code;
								expr.Operand = ldArg.Operand;
							}
						}
						
						block.Body.RemoveAt(i);
						InlineInto(block, i, aggressive: false); // maybe inlining gets possible after the removal of block.Body[i]
						i--;
					}
				}
			}
		}
		
		bool CanPerformCopyPropagation(ILExpression ldArg)
		{
			switch (ldArg.Code) {
				case ILCode.Ldloca:
				case ILCode.Ldarga:
					return true; // ldloca/ldarga always return the same value for a given operand, so they can be safely copied
				case ILCode.Ldarg:
					// arguments can be copied only if they aren't assigned to (directly or indirectly via ldarga)
					ParameterDefinition pd = (ParameterDefinition)ldArg.Operand;
					return numLdarga.GetOrDefault(pd) == 0 && numStarg.GetOrDefault(pd) == 0;
				default:
					return false;
			}
		}
	}
}
