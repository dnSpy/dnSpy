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
			while (--pos >= 0 && inlining.InlineIfPossible(body, ref pos)) ;

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
			if (expr.Code != ILCode.LogicAnd && expr.Code != ILCode.LogicOr) return false;

			var ps = PatternMatcher.Comparisons;
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
			public abstract class Pattern
			{
				public readonly Pattern[] Arguments;

				protected static readonly Pattern[] EmptyArguments = new Pattern[0];

				protected Pattern(Pattern[] arguments)
				{
					this.Arguments = arguments;
				}

				public abstract bool Match(ref PatternMatcher pm, ILExpression e);

				public virtual ILExpression BuildNew(ref PatternMatcher pm, ILExpression[] args)
				{
					throw new NotSupportedException();
				}
			}

			sealed class ILPattern : Pattern
			{
				readonly ILCode code;

				public ILPattern(ILCode code, params Pattern[] arguments)
					: base(arguments)
				{
					this.code = code;
				}

				public override bool Match(ref PatternMatcher pm, ILExpression e)
				{
					return e.Code == this.code;
				}

				public override ILExpression BuildNew(ref PatternMatcher pm, ILExpression[] args)
				{
					return new ILExpression(this.code, null, args);
				}
			}

			sealed class MethodPattern : Pattern
			{
				readonly ILCode code;
				readonly Tuple<string, string, string> method;

				public MethodPattern(ILCode code, Tuple<string, string, string> method, params Pattern[] arguments)
					: base(arguments)
				{
					this.code = code;
					this.method = method;
				}

				public override bool Match(ref PatternMatcher pm, ILExpression e)
				{
					if (e.Code != this.code) return false;
					var m = e.Operand as MethodReference;
					if (m == null || m.Name != this.method.Item1) return false;
					var t = m.DeclaringType;
					return t.Name == this.method.Item2 && t.Namespace == this.method.Item3;
				}
			}

			sealed class OperatorPattern : Pattern
			{
				bool? equals, custom;

				public OperatorPattern(params Pattern[] arguments) : base(arguments) { }

				public OperatorPattern(bool? equals, bool? custom, params Pattern[] arguments)
					: base(arguments)
				{
					this.equals = equals;
					this.custom = custom;
				}

				public override bool Match(ref PatternMatcher pm, ILExpression e)
				{
					switch (e.Code) {
						case ILCode.Ceq:
							if (!equals.GetValueOrDefault() || custom.GetValueOrDefault()) return false;
							break;
						case ILCode.Cgt:
						case ILCode.Cgt_Un:
						case ILCode.Clt:
						case ILCode.Clt_Un:
							if (equals != null || custom.GetValueOrDefault()) return false;
							break;
						case ILCode.Call:
							if (custom != null && !custom.GetValueOrDefault()) return false;
							var m = e.Operand as MethodReference;
							if (m == null || m.HasThis || !m.HasParameters || m.Parameters.Count != base.Arguments.Length || !IsCustomOperator(m)) return false;
							break;
						default: return false;
					}
					if (pm.Operator != null) throw new InvalidOperationException();
					pm.Operator = e;
					return true;
				}

				bool IsCustomOperator(MethodReference m)
				{
					switch (m.Name) {
						case "op_Equality":
							return equals.GetValueOrDefault();
						case "op_Inequality":
							return equals != null && !equals.GetValueOrDefault();
						case "op_GreaterThan":
						case "op_GreaterThanOrEqual":
						case "op_LessThan":
						case "op_LessThanOrEqual":
							return equals == null;
						default: return false;
					}
				}

				public override ILExpression BuildNew(ref PatternMatcher pm, ILExpression[] args)
				{
					var res = pm.Operator;
					res.Arguments.Clear();
					res.Arguments.AddRange(args);
					return res;
				}
			}

			sealed class VariablePattern : Pattern
			{
				readonly ILCode code;
				readonly bool b;

				public VariablePattern(ILCode code, bool b)
					: base(EmptyArguments)
				{
					this.code = code;
					this.b = b;
				}

				public override bool Match(ref PatternMatcher pm, ILExpression e)
				{
					if (e.Code != this.code) return false;
					var v = e.Operand as ILVariable;
					return v != null && (this.b ? Capture(ref pm.B, v) : Capture(ref pm.A, v));
				}

				static bool Capture(ref ILVariable pmvar, ILVariable v)
				{
					if (pmvar != null) return pmvar == v;
					pmvar = v;
					return true;
				}

				public override ILExpression BuildNew(ref PatternMatcher pm, ILExpression[] args)
				{
					return new ILExpression(this.code, this.b ? pm.B : pm.A, args);
				}
			}

			sealed class PrimitivePattern : Pattern
			{
				readonly ILCode code;
				readonly object operand;

				public PrimitivePattern(ILCode code, object operand, params Pattern[] arguments)
					: base(arguments)
				{
					this.code = code;
					this.operand = operand;
				}

				public override bool Match(ref PatternMatcher pm, ILExpression e)
				{
					return e.Code == code && object.Equals(e.Operand, this.operand);
				}
			}

			static readonly Tuple<string, string, string> GetValueOrDefault = new Tuple<string, string, string>("GetValueOrDefault", "Nullable`1", "System");
			static readonly Tuple<string, string, string> get_HasValue = new Tuple<string, string, string>("get_HasValue", "Nullable`1", "System");
			static readonly VariablePattern VariableRefA = new VariablePattern(ILCode.Ldloca, false), VariableRefB = new VariablePattern(ILCode.Ldloca, true);
			static readonly OperatorPattern OperatorVariableAB = new OperatorPattern(new VariablePattern(ILCode.Ldloc, false), new VariablePattern(ILCode.Ldloc, true));
			static readonly MethodPattern[] Call2GetValueOrDefault = new[] {
				new MethodPattern(ILCode.Call, GetValueOrDefault, VariableRefA),
				new MethodPattern(ILCode.Call, GetValueOrDefault, VariableRefB)};
			static readonly MethodPattern CallVariableAget_HasValue = new MethodPattern(ILCode.CallGetter, get_HasValue, VariableRefA);
			static readonly MethodPattern[] Call2get_HasValue = new[] { CallVariableAget_HasValue, new MethodPattern(ILCode.CallGetter, get_HasValue, VariableRefB) };
			static readonly Pattern CeqHasValue = new ILPattern(ILCode.Ceq, Call2get_HasValue);
			static readonly Pattern AndHasValue = new ILPattern(ILCode.And, Call2get_HasValue);

			public static readonly Pattern[] Comparisons = new Pattern[] {
				// == (Primitive, Decimal)
				new ILPattern(ILCode.LogicAnd, new ILPattern(ILCode.LogicNot, new ILPattern(ILCode.LogicNot, new OperatorPattern(true, null, Call2GetValueOrDefault))), CeqHasValue),
				OperatorVariableAB,
				// == (Struct)
				new ILPattern(ILCode.LogicAnd,
					new ILPattern(ILCode.LogicNot, new ILPattern(ILCode.LogicNot, CeqHasValue)),
					new ILPattern(ILCode.LogicOr, new ILPattern(ILCode.LogicNot, CallVariableAget_HasValue), new OperatorPattern(true, true, Call2GetValueOrDefault))),
				OperatorVariableAB,
				// != (P)
				new ILPattern(ILCode.LogicOr,
					new ILPattern(ILCode.LogicNot, new OperatorPattern(true, false, Call2GetValueOrDefault)),
					new ILPattern(ILCode.Ceq, CeqHasValue, new PrimitivePattern(ILCode.Ldc_I4, 0))),
				new ILPattern(ILCode.LogicNot, OperatorVariableAB),
				// != (D)
				new ILPattern(ILCode.LogicOr,
					new OperatorPattern(false, true, Call2GetValueOrDefault),
					new ILPattern(ILCode.Ceq, CeqHasValue, new PrimitivePattern(ILCode.Ldc_I4, 0))),
				OperatorVariableAB,
				// != (S)
				new ILPattern(ILCode.LogicOr,
					new ILPattern(ILCode.LogicNot, CeqHasValue),
					new ILPattern(ILCode.LogicAnd,
						new ILPattern(ILCode.LogicNot, new ILPattern(ILCode.LogicNot, CallVariableAget_HasValue)),
						new OperatorPattern(false, true, Call2GetValueOrDefault))),
				OperatorVariableAB,
				// > (P, D), < (P, D), >= (D), <= (D)
				new ILPattern(ILCode.LogicAnd, new ILPattern(ILCode.LogicNot, new ILPattern(ILCode.LogicNot, new OperatorPattern(null, null, Call2GetValueOrDefault))), AndHasValue),
				OperatorVariableAB,
				// >= (P), <= (P)
				new ILPattern(ILCode.LogicAnd, new ILPattern(ILCode.LogicNot, new OperatorPattern(null, false, Call2GetValueOrDefault)), AndHasValue),
				new ILPattern(ILCode.LogicNot, OperatorVariableAB),
				// > (S), < (S), >= (S), <= (S)
				new ILPattern(ILCode.LogicAnd, AndHasValue, new OperatorPattern(null, true, Call2GetValueOrDefault)),
				OperatorVariableAB,
			};

			public ILVariable A, B;
			public ILExpression Operator;
			public bool Match(Pattern p, ILExpression e)
			{
				if (!p.Match(ref this, e) || e.Arguments.Count != p.Arguments.Length || e.Prefixes != null) return false;
				for (int i = 0; i < p.Arguments.Length; i++)
					if (!Match(p.Arguments[i], e.Arguments[i])) return false;
				return true;
			}

			public ILExpression BuildNew(Pattern p, ILExpression old)
			{
				var args = new ILExpression[p.Arguments.Length];
				for (int i = 0; i < args.Length; i++) args[i] = BuildNew(p.Arguments[i], old);
				var res = p.BuildNew(ref this, args);
				if (args.Length == 2) res.ILRanges = ILRange.OrderAndJoint(old.GetSelfAndChildrenRecursive<ILExpression>().SelectMany(el => el.ILRanges));
				return res;
			}
		}
	}
}
