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
	public static class PatternMatching
	{
		public static bool Match(this ILNode node, ILCode code)
		{
			ILExpression expr = node as ILExpression;
			return expr != null && expr.Prefixes == null && expr.Code == code;
		}
		
		public static bool Match<T>(this ILNode node, ILCode code, out T operand)
		{
			ILExpression expr = node as ILExpression;
			if (expr != null && expr.Prefixes == null && expr.Code == code && expr.Arguments.Count == 0) {
				operand = (T)expr.Operand;
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
		
		public static bool MatchSingle<T>(this ILBasicBlock bb, ILCode code, out T operand, out ILExpression arg)
		{
			if (bb.Body.Count == 2 &&
			    bb.Body[0] is ILLabel &&
			    bb.Body[1].Match(code, out operand, out arg))
			{
				return true;
			}
			operand = default(T);
			arg = null;
			return false;
		}
		
		public static bool MatchSingleAndBr<T>(this ILBasicBlock bb, ILCode code, out T operand, out ILExpression arg, out ILLabel brLabel)
		{
			if (bb.Body.Count == 3 &&
			    bb.Body[0] is ILLabel &&
			    bb.Body[1].Match(code, out operand, out arg) &&
			    bb.Body[2].Match(ILCode.Br, out brLabel))
			{
				return true;
			}
			operand = default(T);
			arg = null;
			brLabel = null;
			return false;
		}
		
		public static bool MatchLastAndBr<T>(this ILBasicBlock bb, ILCode code, out T operand, out ILExpression arg, out ILLabel brLabel)
		{
			if (bb.Body.ElementAtOrDefault(bb.Body.Count - 2).Match(code, out operand, out arg) &&
			    bb.Body.LastOrDefault().Match(ILCode.Br, out brLabel))
			{
				return true;
			}
			operand = default(T);
			arg = null;
			brLabel = null;
			return false;
		}
		
		public static bool MatchThis(this ILNode node)
		{
			ILVariable v;
			return node.Match(ILCode.Ldloc, out v) && v.IsParameter && v.OriginalParameter.Index == -1;
		}
		
		public static bool MatchLdloc(this ILNode node, ILVariable expectedVar)
		{
			ILVariable v;
			return node.Match(ILCode.Ldloc, out v) && v == expectedVar;
		}
		
		public static bool MatchLdloca(this ILNode node, ILVariable expectedVar)
		{
			ILVariable v;
			return node.Match(ILCode.Ldloca, out v) && v == expectedVar;
		}
		
		public static bool MatchStloc(this ILNode node, ILVariable expectedVar, out ILExpression expr)
		{
			ILVariable v;
			return node.Match(ILCode.Stloc, out v, out expr) && v == expectedVar;
		}
		
		public static bool MatchLdcI4(this ILNode node, int expectedValue)
		{
			int v;
			return node.Match(ILCode.Ldc_I4, out v) && v == expectedValue;
		}
	}
}
