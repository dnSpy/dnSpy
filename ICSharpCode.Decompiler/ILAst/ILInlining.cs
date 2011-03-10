// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace ICSharpCode.Decompiler.ILAst
{
	/// <summary>
	/// Performs inlining transformations.
	/// </summary>
	public class ILInlining
	{
		public static void InlineAllVariables(ILBlock method)
		{
			ILInlining i = new ILInlining(method);
			foreach (ILBlock block in method.GetSelfAndChildrenRecursive<ILBlock>())
				i.InlineAllInBlock(block);
		}
		
		Dictionary<ILVariable, int> numStloc  = new Dictionary<ILVariable, int>();
		Dictionary<ILVariable, int> numLdloc  = new Dictionary<ILVariable, int>();
		Dictionary<ILVariable, int> numLdloca = new Dictionary<ILVariable, int>();
		
		public ILInlining(ILBlock method)
		{
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
				}
			}
		}
		
		public void InlineAllInBlock(ILBlock block)
		{
			List<ILNode> body = block.Body;
			for(int i = 0; i < body.Count - 1;) {
				ILExpression nextExpr = body[i + 1] as ILExpression;
				ILVariable locVar;
				ILExpression expr;
				ILExpression ldParent;
				int ldPos;
				if (body[i].Match(ILCode.Stloc, out locVar, out expr) && InlineIfPossible(block, i)) {
					
					
					// We are moving the expression evaluation past the other aguments.
					// It is ok to pass ldloc because the expression can not contain stloc and thus the ldloc will still return the same value
					
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
		public int InlineInto(ILBlock block, int pos)
		{
			int count = 0;
			while (--pos >= 0) {
				ILExpression expr = block.Body[pos] as ILExpression;
				if (expr == null || expr.Code != ILCode.Stloc)
					break;
				if (InlineIfPossible(block, pos))
					count++;
				else
					break;
			}
			return count;
		}
		
		/// <summary>
		/// Inlines the stloc instruction at block.Body[pos] into the next instruction, if possible.
		/// </summary>
		public bool InlineIfPossible(ILBlock block, int pos)
		{
			ILVariable v;
			ILExpression inlinedExpression;
			if (block.Body[pos].Match(ILCode.Stloc, out v, out inlinedExpression)
			    && InlineIfPossible(v, inlinedExpression, block.Body.ElementAtOrDefault(pos+1)))
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
		bool InlineIfPossible(ILVariable v, ILExpression inlinedExpression, ILNode next)
		{
			// ensure the variable is accessed only a single time
			if (!(numStloc.GetOrDefault(v) == 1 && numLdloc.GetOrDefault(v) == 1 && numLdloca.GetOrDefault(v) == 0))
				return false;
			HashSet<ILVariable> forbiddenVariables = new HashSet<ILVariable>();
			foreach (ILExpression potentialStore in inlinedExpression.GetSelfAndChildrenRecursive<ILExpression>()) {
				if (potentialStore.Code == ILCode.Stloc)
					forbiddenVariables.Add((ILVariable)potentialStore.Operand);
			}
			ILExpression parent;
			int pos;
			if (FindLoadInNext(next as ILExpression, v, forbiddenVariables, out parent, out pos) == true) {
				// Assign the ranges of the ldloc instruction:
				inlinedExpression.ILRanges.AddRange(parent.Arguments[pos].ILRanges);
				
				parent.Arguments[pos] = inlinedExpression;
				
				return true;
			}
			return false;
		}
		
		/// <summary>
		/// Finds the position to inline to.
		/// </summary>
		/// <returns>true = found; false = cannot continue search; null = not found</returns>
		bool? FindLoadInNext(ILExpression expr, ILVariable v, HashSet<ILVariable> forbiddenVariables, out ILExpression parent, out int pos)
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
				bool? r = FindLoadInNext(arg, v, forbiddenVariables, out parent, out pos);
				if (r != null)
					return r;
			}
			if (expr.Code == ILCode.Ldloc) {
				ILVariable loadedVar = (ILVariable)expr.Operand;
				if (!forbiddenVariables.Contains(loadedVar) && numLdloca.GetOrDefault(loadedVar) == 0) {
					// the expression is loading a non-forbidden variable:
					// we're allowed to continue searching
					return null;
				}
			}
			// otherwise: abort, inlining is not possible
			return false;
		}
	}
}
