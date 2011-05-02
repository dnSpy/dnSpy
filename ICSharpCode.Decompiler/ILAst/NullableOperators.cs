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

using Mono.Cecil;

namespace ICSharpCode.Decompiler.ILAst
{
	partial class ILAstOptimizer
	{
		bool SimplifyNullableOperators(List<ILNode> body, ILExpression expr, int pos)
		{
			if (!SimplifyNullableOperators(expr)) return false;

			var inlining = new ILInlining(method);
			do pos--;
			while (inlining.InlineIfPossible(body, ref pos));

			return true;
		}

		static bool SimplifyNullableOperators(ILExpression expr)
		{
			if (IsNullableOperation(expr)) return true;

			bool modified = false;
			foreach (var a in expr.Arguments)
				modified |= SimplifyNullableOperators(a);
			return modified;
		}

		static bool IsNullableOperation(ILExpression expr)
		{
			var ps = PatternMatcher.PrimitiveComparisons;
			for (int i = 0; i < ps.Length; i += 2) {
				var pm = new PatternMatcher();
				if (!pm.Match(ps[i], expr)) continue;
				var n = pm.BuildNew(ps[i + 1], expr);
				expr.Code = n.Code;
				expr.Operand = n.Operand;
				expr.Arguments = n.Arguments;
				expr.ILRanges = n.ILRanges;
				return true;
			}
			return false;
		}

		struct PatternMatcher
		{
			public class Pattern
			{
				public readonly ILCode Code;
				public readonly Pattern[] Arguments;

				public Pattern(ILCode code, params Pattern[] arguments)
				{
					this.Code = code;
					this.Arguments = arguments;
				}

				public virtual bool Match(ref PatternMatcher pm, ILExpression e) { return true; }

				public virtual object GetOperand(ref PatternMatcher pm) { return null; }
			}

			sealed class MethodPattern : Pattern
			{
				public readonly Tuple<string, string, string> Method;

				public MethodPattern(ILCode code, Tuple<string, string, string> method, params Pattern[] arguments)
					: base(code, arguments)
				{
					this.Method = method;
				}

				public override bool Match(ref PatternMatcher pm, ILExpression e)
				{
					var m = e.Operand as MethodReference;
					if (m == null || m.Name != this.Method.Item1) return false;
					var t = m.DeclaringType;
					return t.Name == this.Method.Item2 && t.Namespace == this.Method.Item3;
				}
			}

			sealed class VariablePattern : Pattern
			{
				public readonly bool B;

				public VariablePattern(ILCode code, bool b)
					: base(code)
				{
					this.B = b;
				}

				public override bool Match(ref PatternMatcher pm, ILExpression e)
				{
					var v = e.Operand as ILVariable;
					return v != null && (this.B ? Capture(ref pm.B, v) : Capture(ref pm.A, v));
				}

				static bool Capture(ref ILVariable pmvar, ILVariable v)
				{
					if (pmvar != null) return pmvar == v;
					pmvar = v;
					return true;
				}

				public override object GetOperand(ref PatternMatcher pm)
				{
					return this.B ? pm.B : pm.A;
				}
			}

			sealed class PrimitivePattern : Pattern
			{
				public readonly object Operand;

				public PrimitivePattern(ILCode code, object operand, params Pattern[] arguments)
					: base(code, arguments)
				{
					this.Operand = operand;
				}

				public override bool Match(ref PatternMatcher pm, ILExpression e)
				{
					return object.Equals(e.Operand, this.Operand);
				}
			}

			static readonly Tuple<string, string, string> GetValueOrDefault = new Tuple<string, string, string>("GetValueOrDefault", "Nullable`1", "System");
			static readonly Tuple<string, string, string> get_HasValue = new Tuple<string, string, string>("get_HasValue", "Nullable`1", "System");
			static readonly VariablePattern VariableRefA = new VariablePattern(ILCode.Ldloca, false), VariableRefB = new VariablePattern(ILCode.Ldloca, true);
			static readonly VariablePattern[] VariableAB = new[] { new VariablePattern(ILCode.Ldloc, false), new VariablePattern(ILCode.Ldloc, true) };
			static readonly MethodPattern[] Call2GetValueOrDefault = new[] {
				new MethodPattern(ILCode.Call, GetValueOrDefault, VariableRefA),
				new MethodPattern(ILCode.Call, GetValueOrDefault, VariableRefB)};
			static readonly MethodPattern[] Call2get_HasValue = new[] {
				new MethodPattern(ILCode.CallGetter, get_HasValue, VariableRefA),
				new MethodPattern(ILCode.CallGetter, get_HasValue, VariableRefB) };
			static readonly Pattern CeqHasValue = new Pattern(ILCode.Ceq, Call2get_HasValue);
			static readonly Pattern AndHasValue = new Pattern(ILCode.And, Call2get_HasValue);

			public static readonly Pattern[] PrimitiveComparisons = new[] {
				// ==
				new Pattern(ILCode.LogicAnd, new Pattern(ILCode.LogicNot, new Pattern(ILCode.LogicNot, new Pattern(ILCode.Ceq, Call2GetValueOrDefault))), CeqHasValue),
				new Pattern(ILCode.Ceq, VariableAB),
				// !=
				new Pattern(ILCode.LogicOr,
					new Pattern(ILCode.LogicNot, new Pattern(ILCode.Ceq, Call2GetValueOrDefault)),
					new Pattern(ILCode.Ceq, CeqHasValue, new PrimitivePattern(ILCode.Ldc_I4, 0))),
				new Pattern(ILCode.LogicNot, new Pattern(ILCode.Ceq, VariableAB)),
				// >
				new Pattern(ILCode.LogicAnd, new Pattern(ILCode.LogicNot, new Pattern(ILCode.LogicNot, new Pattern(ILCode.Cgt, Call2GetValueOrDefault))), AndHasValue),
				new Pattern(ILCode.Cgt, VariableAB),
				// <
				new Pattern(ILCode.LogicAnd, new Pattern(ILCode.LogicNot, new Pattern(ILCode.LogicNot, new Pattern(ILCode.Clt, Call2GetValueOrDefault))), AndHasValue),
				new Pattern(ILCode.Clt, VariableAB),
				// >=
				new Pattern(ILCode.LogicAnd, new Pattern(ILCode.LogicNot, new Pattern(ILCode.Clt, Call2GetValueOrDefault)), AndHasValue),
				new Pattern(ILCode.LogicNot, new Pattern(ILCode.Clt, VariableAB)),
				// <=
				new Pattern(ILCode.LogicAnd, new Pattern(ILCode.LogicNot, new Pattern(ILCode.Cgt, Call2GetValueOrDefault)), AndHasValue),
				new Pattern(ILCode.LogicNot, new Pattern(ILCode.Cgt, VariableAB)),
			};

			public ILVariable A, B;
			public bool Match(Pattern p, ILExpression e)
			{
				if (e.Code != p.Code || e.Arguments.Count != p.Arguments.Length || e.Prefixes != null || !p.Match(ref this, e)) return false;
				for (int i = 0; i < p.Arguments.Length; i++)
					if (!Match(p.Arguments[i], e.Arguments[i])) return false;
				return true;
			}

			public ILExpression BuildNew(Pattern p, ILExpression old)
			{
				var args = new ILExpression[p.Arguments.Length];
				for (int i = 0; i < args.Length; i++) args[i] = BuildNew(p.Arguments[i], old);
				var res = new ILExpression(p.Code, p.GetOperand(ref this), args);
				if (args.Length == 2) res.ILRanges = ILRange.OrderAndJoint(old.GetSelfAndChildrenRecursive<ILExpression>().SelectMany(el => el.ILRanges));
				return res;
			}
		}
	}
}
