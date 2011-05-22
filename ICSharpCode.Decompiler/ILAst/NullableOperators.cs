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
			if (PatternMatcher.Simplify(expr)) return true;

			bool modified = false;
			foreach (var a in expr.Arguments)
				modified |= SimplifyNullableOperators(a);
			return modified;
		}

		struct PatternMatcher
		{
			abstract class Pattern
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

				public static Pattern operator &(Pattern a, Pattern b)
				{
					return new ILPattern(ILCode.LogicAnd, a, b);
				}

				public static Pattern operator |(Pattern a, Pattern b)
				{
					return new ILPattern(ILCode.LogicOr, a, b);
				}

				public static Pattern operator !(Pattern a)
				{
					return new ILPattern(ILCode.LogicNot, a);
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

				public OperatorPattern(bool? equals, bool? custom, Pattern[] arguments)
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
						case ILCode.Cne:
							if (equals.GetValueOrDefault(true) || custom.GetValueOrDefault()) return false;
							break;
						case ILCode.Cgt:
						case ILCode.Cgt_Un:
						case ILCode.Cge:
						case ILCode.Cge_Un:
						case ILCode.Clt:
						case ILCode.Clt_Un:
						case ILCode.Cle:
						case ILCode.Cle_Un:
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

			static readonly Tuple<string, string, string> GetValueOrDefault = new Tuple<string, string, string>("GetValueOrDefault", "Nullable`1", "System");
			static readonly Tuple<string, string, string> get_HasValue = new Tuple<string, string, string>("get_HasValue", "Nullable`1", "System");
			static readonly Pattern VariableRefA = new VariablePattern(ILCode.Ldloca, false), VariableRefB = new VariablePattern(ILCode.Ldloca, true);
			static readonly Pattern VariableA = new VariablePattern(ILCode.Ldloc, false), VariableB = new VariablePattern(ILCode.Ldloc, true);
			static readonly Pattern VariableAHasValue = new MethodPattern(ILCode.CallGetter, get_HasValue, VariableRefA);
			static readonly Pattern VariableAGetValueOrDefault = new MethodPattern(ILCode.Call, GetValueOrDefault, VariableRefA);
			static readonly Pattern VariableBHasValue = new MethodPattern(ILCode.CallGetter, get_HasValue, VariableRefB);
			static readonly Pattern VariableBGetValueOrDefault = new MethodPattern(ILCode.Call, GetValueOrDefault, VariableRefB);
			static readonly Pattern CeqHasValue = new ILPattern(ILCode.Ceq, VariableAHasValue, VariableBHasValue);
			static readonly Pattern CneHasValue = new ILPattern(ILCode.Cne, VariableAHasValue, VariableBHasValue);
			static readonly Pattern AndHasValue = new ILPattern(ILCode.And, VariableAHasValue, VariableBHasValue);

			static readonly Pattern[] LoadValuesNN = new[] { VariableAGetValueOrDefault, VariableBGetValueOrDefault };
			static OperatorPattern OperatorNN(bool? equals = null, bool? custom = null)
			{
				return new OperatorPattern(equals, custom, LoadValuesNN);
			}

			static readonly Pattern[] LoadValuesNV = new[] { VariableAGetValueOrDefault, VariableB };
			static OperatorPattern OperatorNV(bool? equals = null, bool? custom = null)
			{
				return new OperatorPattern(equals, custom, LoadValuesNV);
			}

			static readonly Pattern[] LoadValuesVN = new[] { VariableA, VariableBGetValueOrDefault };
			static OperatorPattern OperatorVN(bool? equals = null, bool? custom = null)
			{
				return new OperatorPattern(equals, custom, LoadValuesVN);
			}

			static readonly Pattern[] Comparisons = new Pattern[] {
				/* both operands nullable */
				// == (Primitive, Decimal)
				OperatorNN(equals: true) & CeqHasValue,
				// == (Struct)
				CeqHasValue & (!VariableAHasValue | OperatorNN(equals: true, custom: true)),
				// != (Primitive, Decimal)
				OperatorNN(equals: false) | CneHasValue,
				// != (Struct)
				CneHasValue | (VariableAHasValue & OperatorNN(equals: false, custom: true)),
				// > , < , >= , <= (Primitive, Decimal)
				OperatorNN() & AndHasValue,
				// > , < , >= , <= (Struct)
				AndHasValue & OperatorNN(custom: true),

				/* only first operand nullable */
				// == (Primitive, Decimal)
				OperatorNV(equals: true) & VariableAHasValue,
				// == (Struct)
				VariableAHasValue & OperatorNV(equals: true, custom: true),
				// != (Primitive, Decimal)
				OperatorNV(equals: false) | !VariableAHasValue,
				// != (Struct)
				!VariableAHasValue | OperatorNV(equals: false, custom: true),
				// > , <, >= , <= (Primitive, Decimal)
				OperatorNV() & VariableAHasValue,
				// > , < , >= , <= (Struct)
				VariableAHasValue & OperatorNV(custom: true),

				/* only second operand nullable */
				// == (Primitive, Decimal)
				OperatorVN(equals: true) & VariableBHasValue,
				// == (Struct)
				VariableBHasValue & OperatorVN(equals: true, custom: true),
				// != (Primitive, Decimal)
				OperatorVN(equals: false) | !VariableBHasValue,
				// != (Struct)
				!VariableBHasValue | OperatorVN(equals: false, custom: true),
				// > , <, >= , <= (Primitive, Decimal)
				OperatorVN() & VariableBHasValue,
				// > , < , >= , <= (Struct)
				VariableBHasValue & OperatorVN(custom: true),
			};

			ILVariable A, B;
			ILExpression Operator;
			bool Match(Pattern p, ILExpression e)
			{
				if (!p.Match(ref this, e) || e.Arguments.Count != p.Arguments.Length || e.Prefixes != null) return false;
				for (int i = 0; i < p.Arguments.Length; i++)
					if (!Match(p.Arguments[i], e.Arguments[i])) return false;
				return true;
			}

			ILExpression BuildNew(Pattern p, ILExpression old)
			{
				var args = new ILExpression[p.Arguments.Length];
				for (int i = 0; i < args.Length; i++) args[i] = BuildNew(p.Arguments[i], old);
				var res = p.BuildNew(ref this, args);
				if (p is OperatorPattern) res.ILRanges = ILRange.OrderAndJoint(old.GetSelfAndChildrenRecursive<ILExpression>().SelectMany(el => el.ILRanges));
				return res;
			}

			static readonly Pattern OperatorVariableAB = new OperatorPattern(VariableA, VariableB);

			public static bool Simplify(ILExpression expr)
			{
				if (expr.Code != ILCode.LogicAnd && expr.Code != ILCode.LogicOr) return false;

				var ps = Comparisons;
				for (int i = 0; i < ps.Length; i++) {
					var pm = new PatternMatcher();
					if (!pm.Match(ps[i], expr)) continue;
					var n = pm.BuildNew(OperatorVariableAB, expr);
					expr.Code = n.Code;
					expr.Operand = n.Operand;
					expr.Arguments = n.Arguments;
					expr.ILRanges = n.ILRanges;
					expr.InferredType = n.InferredType;
					return true;
				}
				return false;
			}
		}
	}
}
