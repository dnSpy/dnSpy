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
			if (!new PatternMatcher(typeSystem).SimplifyNullableOperators(expr)) return false;

			var inlining = new ILInlining(method);
			while (--pos >= 0 && inlining.InlineIfPossible(body, ref pos)) ;

			return true;
		}

		sealed class PatternMatcher
		{
			readonly TypeSystem typeSystem;
			public PatternMatcher(TypeSystem typeSystem)
			{
				this.typeSystem = typeSystem;
			}

			public bool SimplifyNullableOperators(ILExpression expr)
			{
				if (Simplify(expr)) return true;

				bool modified = false;
				foreach (var a in expr.Arguments)
					modified |= SimplifyNullableOperators(a);
				return modified;
			}

			abstract class Pattern
			{
				public readonly Pattern[] Arguments;

				protected Pattern(Pattern[] arguments)
				{
					this.Arguments = arguments;
				}

				public virtual bool Match(PatternMatcher pm, ILExpression e)
				{
					if (e.Arguments.Count != this.Arguments.Length || e.Prefixes != null) return false;
					for (int i = 0; i < this.Arguments.Length; i++)
						if (!this.Arguments[i].Match(pm, e.Arguments[i])) return false;
					return true;
				}

				public virtual ILExpression BuildNew(PatternMatcher pm)
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

				public override bool Match(PatternMatcher pm, ILExpression e)
				{
					return e.Code == this.code && base.Match(pm, e);
				}

				public override ILExpression BuildNew(PatternMatcher pm)
				{
					var args = new ILExpression[this.Arguments.Length];
					for (int i = 0; i < args.Length; i++) args[i] = this.Arguments[i].BuildNew(pm);
					TypeReference t = null;
					switch (code) {
						case ILCode.Ceq:
						case ILCode.Cne:
							t = pm.typeSystem.Boolean;
							break;
						case ILCode.NullCoalescing:
							t = args[1].InferredType;
							break;
					}
					return new ILExpression(code, null, args) { InferredType = t };
				}
			}

			sealed class MethodPattern : Pattern
			{
				readonly ILCode code;
				readonly string method;

				public MethodPattern(ILCode code, string method, params Pattern[] arguments)
					: base(arguments)
				{
					this.code = code;
					this.method = method;
				}

				public override bool Match(PatternMatcher pm, ILExpression e)
				{
					if (e.Code != this.code) return false;
					var m = e.Operand as MethodReference;
					if (m == null || m.Name != this.method) return false;
					var t = m.DeclaringType;
					return t.Name == "Nullable`1" && t.Namespace == "System" && base.Match(pm, e);
				}
			}

			enum OperatorType
			{
				Equality, InEquality, Comparison, Other
			}

			sealed class OperatorPattern : Pattern
			{
				OperatorType type;
				bool simple;

				public OperatorPattern() : base(null) { }

				public OperatorPattern(OperatorType type, bool simple)
					: this()
				{
					this.type = type;
					this.simple = simple;
				}

				public override bool Match(PatternMatcher pm, ILExpression e)
				{
					switch (e.Code) {
						case ILCode.Ceq:
							if (type != OperatorType.Equality) return false;
							break;
						case ILCode.Cne:
							if (type != OperatorType.InEquality) return false;
							break;
						case ILCode.Cgt:
						case ILCode.Cgt_Un:
						case ILCode.Cge:
						case ILCode.Cge_Un:
						case ILCode.Clt:
						case ILCode.Clt_Un:
						case ILCode.Cle:
						case ILCode.Cle_Un:
							if (type != OperatorType.Comparison) return false;
							break;
						case ILCode.Add:
						case ILCode.Add_Ovf:
						case ILCode.Add_Ovf_Un:
						case ILCode.Sub:
						case ILCode.Sub_Ovf:
						case ILCode.Sub_Ovf_Un:
						case ILCode.Mul:
						case ILCode.Mul_Ovf:
						case ILCode.Mul_Ovf_Un:
						case ILCode.Div:
						case ILCode.Div_Un:
						case ILCode.Rem:
						case ILCode.Rem_Un:
						case ILCode.And:
						case ILCode.Or:
						case ILCode.Xor:
						case ILCode.Shl:
						case ILCode.Shr:
						case ILCode.Shr_Un:
						case ILCode.Not:
						case ILCode.Neg:
						case ILCode.LogicNot:
							if (type != OperatorType.Other) return false;
							break;
						case ILCode.Call:
							var m = e.Operand as MethodReference;
							if (m == null || m.HasThis || !m.HasParameters || e.Arguments.Count > 2 || !IsCustomOperator(m.Name)) return false;
							break;
						default: return false;
					}
					if (pm.Operator != null) throw new InvalidOperationException();
					pm.Operator = e;

					var a0 = e.Arguments[0];
					if (!simple) return VariableAGetValueOrDefault.Match(pm, a0) && VariableBGetValueOrDefault.Match(pm, e.Arguments[1]);
					if (e.Arguments.Count == 1) return VariableAGetValueOrDefault.Match(pm, a0);
					if (VariableAGetValueOrDefault.Match(pm, a0)) {
						pm.SimpleOperand = e.Arguments[1];
						pm.SimpleLeftOperand = false;
						return true;
					}
					if (VariableAGetValueOrDefault.Match(pm, e.Arguments[1])) {
						pm.SimpleOperand = a0;
						pm.SimpleLeftOperand = true;
						return true;
					}
					return false;
				}

				bool IsCustomOperator(string s)
				{
					if (s.Length < 11 || !s.StartsWith("op_", StringComparison.Ordinal)) return false;
					switch (s) {
						case "op_Equality":
							return type == OperatorType.Equality;
						case "op_Inequality":
							return type == OperatorType.InEquality;
						case "op_GreaterThan":
						case "op_GreaterThanOrEqual":
						case "op_LessThan":
						case "op_LessThanOrEqual":
							return type == OperatorType.Comparison;
						case "op_Addition":
						case "op_Subtraction":
						case "op_Multiply":
						case "op_Division":
						case "op_Modulus":
						case "op_BitwiseAnd":
						case "op_BitwiseOr":
						case "op_ExclusiveOr":
						case "op_LeftShift":
						case "op_RightShift":
						case "op_Negation":
						case "op_UnaryNegation":
						case "op_UnaryPlus":
							return type == OperatorType.Other;
						default: return false;
					}
				}

				public override ILExpression BuildNew(PatternMatcher pm)
				{
					var res = pm.Operator;
					res.Arguments.Clear();
					if (pm.SimpleLeftOperand) res.Arguments.Add(pm.SimpleOperand);
					res.Arguments.Add(VariableA.BuildNew(pm));
					if (pm.B != null) res.Arguments.Add(VariableB.BuildNew(pm));
					else if (pm.SimpleOperand != null && !pm.SimpleLeftOperand) res.Arguments.Add(pm.SimpleOperand);
					return res;
				}
			}

			sealed class AnyPattern : Pattern
			{
				public AnyPattern() : base(null) { }

				public override bool Match(PatternMatcher pm, ILExpression e)
				{
					if (pm.SimpleOperand != null) throw new InvalidOperationException();
					pm.SimpleOperand = e;
					return true;
				}

				public override ILExpression BuildNew(PatternMatcher pm)
				{
					return pm.SimpleOperand;
				}
			}

			sealed class VariablePattern : Pattern
			{
				readonly ILCode code;
				readonly bool b;

				public VariablePattern(ILCode code, bool b)
					: base(null)
				{
					this.code = code;
					this.b = b;
				}

				public override bool Match(PatternMatcher pm, ILExpression e)
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

				static readonly ILExpression[] EmptyArguments = new ILExpression[0];
				public override ILExpression BuildNew(PatternMatcher pm)
				{
					var v = this.b ? pm.B : pm.A;
					var e = new ILExpression(ILCode.Ldloc, v, EmptyArguments);
					if (v.Type.Name == "Nullable`1" && v.Type.Namespace == "System") e = new ILExpression(ILCode.ValueOf, null, e);
					return e;
				}
			}

			sealed class BooleanPattern : Pattern
			{
				readonly object value;
				public BooleanPattern(bool value)
					: base(null)
				{
					this.value = Convert.ToInt32(value);
				}

				public override bool Match(PatternMatcher pm, ILExpression e)
				{
					return e.Code == ILCode.Ldc_I4 && TypeAnalysis.IsBoolean(e.InferredType) && object.Equals(e.Operand, value);
				}

				public override ILExpression BuildNew(PatternMatcher pm)
				{
					// boolean constants are wrapped inside a container to disable simplyfication of equality comparisons
					return new ILExpression(ILCode.Wrap, null, new ILExpression(ILCode.Ldc_I4, value));
				}
			}

			static readonly Pattern VariableRefA = new VariablePattern(ILCode.Ldloca, false), VariableRefB = new VariablePattern(ILCode.Ldloca, true);
			static readonly Pattern VariableA = new VariablePattern(ILCode.Ldloc, false), VariableB = new VariablePattern(ILCode.Ldloc, true);
			static readonly Pattern VariableAHasValue = new MethodPattern(ILCode.CallGetter, "get_HasValue", VariableRefA);
			static readonly Pattern VariableAGetValueOrDefault = new MethodPattern(ILCode.Call, "GetValueOrDefault", VariableRefA);
			static readonly Pattern VariableBHasValue = new MethodPattern(ILCode.CallGetter, "get_HasValue", VariableRefB);
			static readonly Pattern VariableBGetValueOrDefault = new MethodPattern(ILCode.Call, "GetValueOrDefault", VariableRefB);
			static readonly Pattern CeqHasValue = new ILPattern(ILCode.Ceq, VariableAHasValue, VariableBHasValue);
			static readonly Pattern CneHasValue = new ILPattern(ILCode.Cne, VariableAHasValue, VariableBHasValue);
			static readonly Pattern AndHasValue = new ILPattern(ILCode.And, VariableAHasValue, VariableBHasValue);
			static readonly Pattern Any = new AnyPattern();
			static readonly Pattern OperatorVariableAB = new OperatorPattern();

			static OperatorPattern OperatorNN(OperatorType type)
			{
				return new OperatorPattern(type, false);
			}

			static OperatorPattern OperatorNV(OperatorType type)
			{
				return new OperatorPattern(type, true);
			}

			static readonly Pattern[] Comparisons = new Pattern[] {
				/* both operands nullable */
				// == (primitive, decimal)
				OperatorNN(OperatorType.Equality) & CeqHasValue,
				// == (struct)
				CeqHasValue & (!VariableAHasValue | OperatorNN(OperatorType.Equality)),
				// != (primitive, decimal)
				OperatorNN(OperatorType.InEquality) | CneHasValue,
				// != (struct)
				CneHasValue | (VariableAHasValue & OperatorNN(OperatorType.InEquality)),
				// > , < , >= , <= (primitive, decimal)
				OperatorNN(OperatorType.Comparison) & AndHasValue,
				// > , < , >= , <= (struct)
				AndHasValue & OperatorNN(OperatorType.Comparison),

				/* only one operand nullable */
				// == (primitive, decimal)
				OperatorNV(OperatorType.Equality) & VariableAHasValue,
				// == (struct)
				VariableAHasValue & OperatorNV(OperatorType.Equality),
				// != (primitive, decimal)
				OperatorNV(OperatorType.InEquality) | !VariableAHasValue,
				// != (struct)
				!VariableAHasValue | OperatorNV(OperatorType.InEquality),
				// > , <, >= , <= (primitive, decimal)
				OperatorNV(OperatorType.Comparison) & VariableAHasValue,
				// > , < , >= , <= (struct)
				VariableAHasValue & OperatorNV(OperatorType.Comparison),
			};

			static readonly Pattern[] Other = new Pattern[] {
				/* both operands nullable */
				// & (bool)
				new ILPattern(ILCode.TernaryOp, VariableAGetValueOrDefault | (!VariableBGetValueOrDefault & !VariableAHasValue), VariableB, VariableA),
				new ILPattern(ILCode.And, VariableA, VariableB),
				// | (bool)
				new ILPattern(ILCode.TernaryOp, VariableAGetValueOrDefault | (!VariableBGetValueOrDefault & !VariableAHasValue), VariableA, VariableB),
				new ILPattern(ILCode.Or, VariableA, VariableB),
				// null coalescing
				new ILPattern(ILCode.TernaryOp, VariableAHasValue, new MethodPattern(ILCode.Newobj, ".ctor", VariableAGetValueOrDefault), VariableB),
				new ILPattern(ILCode.NullCoalescing, VariableA, VariableB),
				// all other
				new ILPattern(ILCode.TernaryOp, AndHasValue, new MethodPattern(ILCode.Newobj, ".ctor", OperatorNN(OperatorType.Other)), new ILPattern(ILCode.DefaultValue)),
				null,

				/* only one operand nullable */
				// & (bool)
				new ILPattern(ILCode.TernaryOp, Any, VariableA, new MethodPattern(ILCode.Newobj, ".ctor", new BooleanPattern(false))),
				new ILPattern(ILCode.And, VariableA, Any),
				// | (bool)
				new ILPattern(ILCode.TernaryOp, Any, new MethodPattern(ILCode.Newobj, ".ctor", new BooleanPattern(true)), VariableA),
				new ILPattern(ILCode.Or, VariableA, Any),
				// == true
				VariableAGetValueOrDefault & VariableAHasValue,
				new ILPattern(ILCode.Ceq, VariableA, new BooleanPattern(true)),
				// != true
				!VariableAGetValueOrDefault | !VariableAHasValue,
				new ILPattern(ILCode.Cne, VariableA, new BooleanPattern(true)),
				// == false
				!VariableAGetValueOrDefault & VariableAHasValue,
				new ILPattern(ILCode.Ceq, VariableA, new BooleanPattern(false)),
				// != false
				VariableAGetValueOrDefault | !VariableAHasValue,
				new ILPattern(ILCode.Cne, VariableA, new BooleanPattern(false)),
				// null coalescing
				new ILPattern(ILCode.TernaryOp, VariableAHasValue, VariableAGetValueOrDefault, Any),
				new ILPattern(ILCode.NullCoalescing, VariableA, Any),
				// all other
				new ILPattern(ILCode.TernaryOp, VariableAHasValue, new MethodPattern(ILCode.Newobj, ".ctor", OperatorNV(OperatorType.Other)), new ILPattern(ILCode.DefaultValue)),
				null,
			};

			ILVariable A, B;
			ILExpression Operator, SimpleOperand;
			bool SimpleLeftOperand;

			void Reset()
			{
				this.A = null;
				this.B = null;
				this.Operator = null;
				this.SimpleOperand = null;
				this.SimpleLeftOperand = false;
			}

			bool Simplify(ILExpression expr)
			{
				if (expr.Code == ILCode.TernaryOp || expr.Code == ILCode.LogicAnd || expr.Code == ILCode.LogicOr) {
					Pattern[] ps;
					if (expr.Code != ILCode.TernaryOp) {
						ps = Comparisons;
						for (int i = 0; i < ps.Length; i++) {
							this.Reset();
							if (!ps[i].Match(this, expr)) continue;
							var n = OperatorVariableAB.BuildNew(this);
							n.ILRanges = ILRange.OrderAndJoint(expr.GetSelfAndChildrenRecursive<ILExpression>().SelectMany(el => el.ILRanges));
							// the new expression is wrapped in a container so that negations aren't pushed through the comparison operation
							expr.Code = ILCode.Wrap;
							expr.Operand = null;
							expr.Arguments.Clear();
							expr.Arguments.Add(n);
							expr.ILRanges.Clear();
							expr.InferredType = n.InferredType;
							return true;
						}
					}
					ps = Other;
					for (int i = 0; i < ps.Length; i += 2) {
						this.Reset();
						if (!ps[i].Match(this, expr)) continue;
						var n = (ps[i + 1] ?? OperatorVariableAB).BuildNew(this);
						n.ILRanges = ILRange.OrderAndJoint(expr.GetSelfAndChildrenRecursive<ILExpression>().SelectMany(el => el.ILRanges));
						// the new expression is wrapped in a container so that negations aren't pushed through the comparison operation
						expr.Code = ILCode.Wrap;
						expr.Operand = null;
						expr.Arguments.Clear();
						expr.Arguments.Add(n);
						expr.ILRanges.Clear();
						expr.InferredType = n.InferredType;
						return true;
					}
				}
				return false;
			}
		}
	}
}
