// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Diagnostics;
using System.Linq;

namespace ICSharpCode.Decompiler.ILAst
{
	/// <summary>
	/// Performs inlining transformations.
	/// </summary>
	public class ILInlining
	{
		/// <summary>
		/// Inlines instructions before pos into block.Body[pos].
		/// </summary>
		/// <returns>The number of instructions that were inlined.</returns>
		public static int InlineInto(ILBlock block, int pos, ILBlock method)
		{
			int count = 0;
			while (--pos >= 0) {
				ILExpression expr = block.Body[pos] as ILExpression;
				if (expr == null || expr.Code != ILCode.Stloc)
					break;
				if (InlineIfPossible(block, pos, method))
					count++;
				else
					break;
			}
			return count;
		}
		
		/// <summary>
		/// Inlines the stloc instruction at block.Body[pos] into the next instruction, if possible.
		/// </summary>
		public static bool InlineIfPossible(ILBlock block, int pos, ILBlock method)
		{
			if (InlineIfPossible((ILExpression)block.Body[pos], block.Body.ElementAtOrDefault(pos+1), method)) {
				block.Body.RemoveAt(pos);
				return true;
			}
			return false;
		}
		
		/// <summary>
		/// Inlines 'expr' into 'next', if possible.
		/// </summary>
		public static bool InlineIfPossible(ILExpression expr, ILNode next, ILBlock method)
		{
			if (expr.Code != ILCode.Stloc)
				throw new ArgumentException("expr must be stloc");
			// ensure the variable is accessed only a single time
			if (method.GetSelfAndChildrenRecursive<ILExpression>().Count(e => e != expr && e.Operand == expr.Operand) != 1)
				return false;
			ILExpression parent;
			int pos;
			if (FindLoadInNext(next as ILExpression, (ILVariable)expr.Operand, out parent, out pos) == true) {
				parent.Arguments[pos] = expr.Arguments[0];
				return true;
			}
			return false;
		}
		
		/// <summary>
		/// Finds the position to inline to.
		/// </summary>
		/// <returns>true = found; false = cannot continue search; null = not found</returns>
		static bool? FindLoadInNext(ILExpression expr, ILVariable v, out ILExpression parent, out int pos)
		{
			parent = null;
			pos = 0;
			if (expr == null)
				return false;
			for (int i = 0; i < expr.Arguments.Count; i++) {
				ILExpression arg = expr.Arguments[i];
				if (arg.Code == ILCode.Ldloc && arg.Operand == v) {
					parent = expr;
					pos = i;
					return true;
				}
				bool? r = FindLoadInNext(arg, v, out parent, out pos);
				if (r != null)
					return r;
			}
			return IsWithoutSideEffects(expr.Code) ? (bool?)null : false;
		}
		
		static bool IsWithoutSideEffects(ILCode code)
		{
			return code == ILCode.Ldloc;
		}
	}
}
