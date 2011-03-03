// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Linq;
using ICSharpCode.NRefactory.Utils;
using Mono.Cecil;

namespace ICSharpCode.Decompiler.ILAst
{
	public delegate void PeepholeTransform(ILBlock block, ref int i);
	
	/// <summary>
	/// Handles peephole transformations on the ILAst.
	/// </summary>
	public static class PeepholeTransforms
	{
		public static void Run(DecompilerContext context, ILBlock method)
		{
			PeepholeTransform[] blockTransforms = {
				ArrayInitializers.Transform(method)
			};
			Func<ILExpression, ILExpression>[] exprTransforms = {
				EliminateDups,
				HandleDecimalConstants
			};
			// Traverse in post order so that nested blocks are transformed first. This is required so that
			// patterns on the parent block can assume that all nested blocks are already transformed.
			foreach (var node in TreeTraversal.PostOrder<ILNode>(method, c => c != null ? c.GetChildren() : null)) {
				ILBlock block = node as ILBlock;
				ILExpression expr;
				if (block != null) {
					// go through the instructions in reverse so that transforms can build up nested structures inside-out
					for (int i = block.Body.Count - 1; i >= 0; i--) {
						context.CancellationToken.ThrowIfCancellationRequested();
						expr = block.Body[i] as ILExpression;
						if (expr != null) {
							// apply expr transforms to top-level expr in block
							foreach (var t in exprTransforms)
								expr = t(expr);
							block.Body[i] = expr;
						}
						// apply block transforms
						foreach (var t in blockTransforms) {
							t(block, ref i);
						}
					}
				}
				expr = node as ILExpression;
				if (expr != null) {
					// apply expr transforms to all arguments
					for (int i = 0; i < expr.Arguments.Count; i++) {
						ILExpression arg = expr.Arguments[i];
						foreach (var t in exprTransforms)
							arg = t(arg);
						expr.Arguments[i] = arg;
					}
				}
			}
		}
		
		static ILExpression EliminateDups(ILExpression expr)
		{
			if (expr.Code == ILCode.Dup)
				return expr.Arguments.Single();
			else
				return expr;
		}
		
		static ILExpression HandleDecimalConstants(ILExpression expr)
		{
			if (expr.Code == ILCode.Newobj) {
				MethodReference r = (MethodReference)expr.Operand;
				if (r.DeclaringType.Name == "Decimal" && r.DeclaringType.Namespace == "System") {
					if (expr.Arguments.Count == 1) {
						int? val = GetI4Constant(expr.Arguments[0]);
						if (val != null) {
							expr.Arguments.Clear();
							expr.Code = ILCode.Ldc_Decimal;
							expr.Operand = new decimal(val.Value);
							expr.InferredType = r.DeclaringType;
						}
					} else if (expr.Arguments.Count == 5) {
						int? lo = GetI4Constant(expr.Arguments[0]);
						int? mid = GetI4Constant(expr.Arguments[1]);
						int? hi = GetI4Constant(expr.Arguments[2]);
						int? isNegative = GetI4Constant(expr.Arguments[3]);
						int? scale = GetI4Constant(expr.Arguments[4]);
						if (lo != null && mid != null && hi != null && isNegative != null && scale != null) {
							expr.Arguments.Clear();
							expr.Code = ILCode.Ldc_Decimal;
							expr.Operand = new decimal(lo.Value, mid.Value, hi.Value, isNegative.Value != 0, (byte)scale);
							expr.InferredType = r.DeclaringType;
						}
					}
				}
			}
			return expr;
		}
		
		static int? GetI4Constant(ILExpression expr)
		{
			if (expr != null && expr.Code == ILCode.Ldc_I4)
				return (int)expr.Operand;
			else
				return null;
		}
	}
}
