//
// nullable.cs: Nullable types support
//
// Authors: Martin Baulig (martin@ximian.com)
//          Miguel de Icaza (miguel@ximian.com)
//          Marek Safar (marek.safar@gmail.com)
//
// Dual licensed under the terms of the MIT X11 or GNU GPL
//
// Copyright 2001, 2002, 2003 Ximian, Inc (http://www.ximian.com)
// Copyright 2004-2008 Novell, Inc
// Copyright 2011 Xamarin Inc
//

using System;

#if STATIC
using IKVM.Reflection.Emit;
#else
using System.Reflection.Emit;
#endif
	
namespace Mono.CSharp.Nullable
{
	public class NullableType : TypeExpr
	{
		readonly TypeSpec underlying;

		public NullableType (TypeSpec type, Location loc)
		{
			this.underlying = type;
			this.loc = loc;
		}

		public override TypeSpec ResolveAsType (IMemberContext ec)
		{
			eclass = ExprClass.Type;

			var otype = ec.Module.PredefinedTypes.Nullable.Resolve ();
			if (otype == null)
				return null;

			TypeArguments args = new TypeArguments (new TypeExpression (underlying, loc));
			GenericTypeExpr ctype = new GenericTypeExpr (otype, args, loc);
			
			type = ctype.ResolveAsType (ec);
			return type;
		}
	}

	static class NullableInfo
	{
		public static MethodSpec GetConstructor (TypeSpec nullableType)
		{
			return (MethodSpec) MemberCache.FindMember (nullableType,
				MemberFilter.Constructor (ParametersCompiled.CreateFullyResolved (GetUnderlyingType (nullableType))), BindingRestriction.DeclaredOnly);
		}

		public static MethodSpec GetHasValue (TypeSpec nullableType)
		{
			return (MethodSpec) MemberCache.FindMember (nullableType,
				MemberFilter.Method ("get_HasValue", 0, ParametersCompiled.EmptyReadOnlyParameters, null), BindingRestriction.None);
		}

		public static MethodSpec GetGetValueOrDefault (TypeSpec nullableType)
		{
			return (MethodSpec) MemberCache.FindMember (nullableType,
				MemberFilter.Method ("GetValueOrDefault", 0, ParametersCompiled.EmptyReadOnlyParameters, null), BindingRestriction.None);
		}

		public static MethodSpec GetValue (TypeSpec nullableType)
		{
			return (MethodSpec) MemberCache.FindMember (nullableType,
				MemberFilter.Method ("get_Value", 0, ParametersCompiled.EmptyReadOnlyParameters, null), BindingRestriction.None);
		}

		public static TypeSpec GetUnderlyingType (TypeSpec nullableType)
		{
			return ((InflatedTypeSpec) nullableType).TypeArguments[0];
		}
	}

	public class Unwrap : Expression, IMemoryLocation
	{
		Expression expr;

		LocalTemporary temp;
		readonly bool useDefaultValue;

		Unwrap (Expression expr, bool useDefaultValue)
		{
			this.expr = expr;
			this.loc = expr.Location;
			this.useDefaultValue = useDefaultValue;

			type = NullableInfo.GetUnderlyingType (expr.Type);
			eclass = expr.eclass;
		}

		public override bool ContainsEmitWithAwait ()
		{
			return expr.ContainsEmitWithAwait ();
		}

		public static Expression Create (Expression expr)
		{
			//
			// Avoid unwraping and wraping of same type
			//
			Wrap wrap = expr as Wrap;
			if (wrap != null)
				return wrap.Child;

			return Create (expr, false);
		}

		public static Unwrap Create (Expression expr, bool useDefaultValue)
		{
			return new Unwrap (expr, useDefaultValue);
		}
		
		public override Expression CreateExpressionTree (ResolveContext ec)
		{
			return expr.CreateExpressionTree (ec);
		}

		protected override Expression DoResolve (ResolveContext ec)
		{
			return this;
		}

		public override Expression DoResolveLValue (ResolveContext ec, Expression right_side)
		{
			expr = expr.DoResolveLValue (ec, right_side);
			return this;
		}

		public override void Emit (EmitContext ec)
		{
			Store (ec);

			var call = new CallEmitter ();
			call.InstanceExpression = this;

			if (useDefaultValue)
				call.EmitPredefined (ec, NullableInfo.GetGetValueOrDefault (expr.Type), null);
			else
				call.EmitPredefined (ec, NullableInfo.GetValue (expr.Type), null);
		}

		public void EmitCheck (EmitContext ec)
		{
			Store (ec);

			var call = new CallEmitter ();
			call.InstanceExpression = this;

			call.EmitPredefined (ec, NullableInfo.GetHasValue (expr.Type), null);
		}

		public override bool Equals (object obj)
		{
			Unwrap uw = obj as Unwrap;
			return uw != null && expr.Equals (uw.expr);
		}

		public Expression Original {
			get {
				return expr;
			}
		}
		
		public override int GetHashCode ()
		{
			return expr.GetHashCode ();
		}

		public override bool IsNull {
			get {
				return expr.IsNull;
			}
		}

		void Store (EmitContext ec)
		{
			if (temp != null)
				return;

			if (expr is VariableReference)
				return;

			expr.Emit (ec);
			LocalVariable.Store (ec);
		}

		public void Load (EmitContext ec)
		{
			if (expr is VariableReference)
				expr.Emit (ec);
			else
				LocalVariable.Emit (ec);
		}

		public override System.Linq.Expressions.Expression MakeExpression (BuilderContext ctx)
		{
			return expr.MakeExpression (ctx);
		}

		public void AddressOf (EmitContext ec, AddressOp mode)
		{
			IMemoryLocation ml = expr as VariableReference;
			if (ml != null)
				ml.AddressOf (ec, mode);
			else
				LocalVariable.AddressOf (ec, mode);
		}

		//
		// Keeps result of non-variable expression
		//
		LocalTemporary LocalVariable {
			get {
				if (temp == null)
					temp = new LocalTemporary (expr.Type);
				return temp;
			}
		}
	}

	//
	// Calls get_Value method on nullable expression
	//
	public class UnwrapCall : CompositeExpression
	{
		public UnwrapCall (Expression expr)
			: base (expr)
		{
		}

		protected override Expression DoResolve (ResolveContext rc)
		{
			base.DoResolve (rc);

			if (type != null)
				type = NullableInfo.GetUnderlyingType (type);

			return this;
		}

		public override void Emit (EmitContext ec)
		{
			var call = new CallEmitter ();
			call.InstanceExpression = Child;
			call.EmitPredefined (ec, NullableInfo.GetValue (Child.Type), null);
		}
	}

	public class Wrap : TypeCast
	{
		private Wrap (Expression expr, TypeSpec type)
			: base (expr, type)
		{
			eclass = ExprClass.Value;
		}

		public override Expression CreateExpressionTree (ResolveContext ec)
		{
			TypeCast child_cast = child as TypeCast;
			if (child_cast != null) {
				child.Type = type;
				return child_cast.CreateExpressionTree (ec);
			}

			return base.CreateExpressionTree (ec);
		}

		public static Expression Create (Expression expr, TypeSpec type)
		{
			//
			// Avoid unwraping and wraping of the same type
			//
			Unwrap unwrap = expr as Unwrap;
			if (unwrap != null && expr.Type == NullableInfo.GetUnderlyingType (type))
				return unwrap.Original;
		
			return new Wrap (expr, type);
		}
		
		public override void Emit (EmitContext ec)
		{
			child.Emit (ec);
			ec.Emit (OpCodes.Newobj, NullableInfo.GetConstructor (type));
		}
	}

	//
	// Represents null literal lifted to nullable type
	//
	public class LiftedNull : NullConstant, IMemoryLocation
	{
		private LiftedNull (TypeSpec nullable_type, Location loc)
			: base (nullable_type, loc)
		{
			eclass = ExprClass.Value;
		}

		public static Constant Create (TypeSpec nullable, Location loc)
		{
			return new LiftedNull (nullable, loc);
		}

		public static Constant CreateFromExpression (ResolveContext ec, Expression e)
		{
			ec.Report.Warning (458, 2, e.Location, "The result of the expression is always `null' of type `{0}'",
				TypeManager.CSharpName (e.Type));

			return ReducedExpression.Create (Create (e.Type, e.Location), e);
		}

		public override void Emit (EmitContext ec)
		{
			// TODO: generate less temporary variables
			LocalTemporary value_target = new LocalTemporary (type);

			value_target.AddressOf (ec, AddressOp.Store);
			ec.Emit (OpCodes.Initobj, type);
			value_target.Emit (ec);
			value_target.Release (ec);
		}

		public void AddressOf (EmitContext ec, AddressOp Mode)
		{
			LocalTemporary value_target = new LocalTemporary (type);
				
			value_target.AddressOf (ec, AddressOp.Store);
			ec.Emit (OpCodes.Initobj, type);
			((IMemoryLocation) value_target).AddressOf (ec, Mode);
		}
	}

	//
	// Generic lifting expression, supports all S/S? -> T/T? cases
	//
	public class Lifted : Expression, IMemoryLocation
	{
		Expression expr, null_value;
		Unwrap unwrap;

		public Lifted (Expression expr, Unwrap unwrap, TypeSpec type)
		{
			this.expr = expr;
			this.unwrap = unwrap;
			this.loc = expr.Location;
			this.type = type;
		}

		public Lifted (Expression expr, Expression unwrap, TypeSpec type)
			: this (expr, unwrap as Unwrap, type)
		{
		}

		public override bool ContainsEmitWithAwait ()
		{
			return unwrap.ContainsEmitWithAwait ();
		}
		
		public override Expression CreateExpressionTree (ResolveContext ec)
		{
			return expr.CreateExpressionTree (ec);
		}			

		protected override Expression DoResolve (ResolveContext ec)
		{
			//
			// It's null when lifting non-nullable type
			//
			if (unwrap == null) {
				// S -> T? is wrap only
				if (type.IsNullableType)
					return Wrap.Create (expr, type);

				// S -> T can be simplified
				return expr;
			}

			// Wrap target for T?
			if (type.IsNullableType) {
				expr = Wrap.Create (expr, type);
				if (expr == null)
					return null;

				null_value = LiftedNull.Create (type, loc);
			} else if (TypeSpec.IsValueType (type)) {
				null_value = LiftedNull.Create (type, loc);
			} else {
				null_value = new NullConstant (type, loc);
			}

			eclass = ExprClass.Value;
			return this;
		}

		public override void Emit (EmitContext ec)
		{
			Label is_null_label = ec.DefineLabel ();
			Label end_label = ec.DefineLabel ();

			unwrap.EmitCheck (ec);
			ec.Emit (OpCodes.Brfalse, is_null_label);

			expr.Emit (ec);

			ec.Emit (OpCodes.Br, end_label);
			ec.MarkLabel (is_null_label);

			null_value.Emit (ec);
			ec.MarkLabel (end_label);
		}

		public void AddressOf (EmitContext ec, AddressOp mode)
		{
			unwrap.AddressOf (ec, mode);
		}
	}

	public class LiftedUnaryOperator : Unary, IMemoryLocation
	{
		Unwrap unwrap;
		Expression user_operator;

		public LiftedUnaryOperator (Unary.Operator op, Expression expr, Location loc)
			: base (op, expr, loc)
		{
		}

		public void AddressOf (EmitContext ec, AddressOp mode)
		{
			unwrap.AddressOf (ec, mode);
		}

		public override Expression CreateExpressionTree (ResolveContext ec)
		{
			if (user_operator != null)
				return user_operator.CreateExpressionTree (ec);

			if (Oper == Operator.UnaryPlus)
				return Expr.CreateExpressionTree (ec);

			return base.CreateExpressionTree (ec);
		}

		protected override Expression DoResolve (ResolveContext ec)
		{
			unwrap = Unwrap.Create (Expr, false);
			if (unwrap == null)
				return null;

			Expression res = base.ResolveOperator (ec, unwrap);
			if (res != this) {
				if (user_operator == null)
					return res;
			} else {
				res = Expr = LiftExpression (ec, Expr);
			}

			if (res == null)
				return null;

			eclass = ExprClass.Value;
			type = res.Type;
			return this;
		}

		public override void Emit (EmitContext ec)
		{
			Label is_null_label = ec.DefineLabel ();
			Label end_label = ec.DefineLabel ();

			unwrap.EmitCheck (ec);
			ec.Emit (OpCodes.Brfalse, is_null_label);

			if (user_operator != null) {
				user_operator.Emit (ec);
			} else {
				EmitOperator (ec, NullableInfo.GetUnderlyingType (type));
			}

			ec.Emit (OpCodes.Newobj, NullableInfo.GetConstructor (type));
			ec.Emit (OpCodes.Br_S, end_label);

			ec.MarkLabel (is_null_label);
			LiftedNull.Create (type, loc).Emit (ec);

			ec.MarkLabel (end_label);
		}

		static Expression LiftExpression (ResolveContext ec, Expression expr)
		{
			var lifted_type = new NullableType (expr.Type, expr.Location);
			if (lifted_type.ResolveAsType (ec) == null)
				return null;

			expr.Type = lifted_type.Type;
			return expr;
		}

		protected override Expression ResolveEnumOperator (ResolveContext ec, Expression expr, TypeSpec[] predefined)
		{
			expr = base.ResolveEnumOperator (ec, expr, predefined);
			if (expr == null)
				return null;

			Expr = LiftExpression (ec, Expr);
			return LiftExpression (ec, expr);
		}

		protected override Expression ResolveUserOperator (ResolveContext ec, Expression expr)
		{
			expr = base.ResolveUserOperator (ec, expr);
			if (expr == null)
				return null;

			//
			// When a user operator is of non-nullable type
			//
			if (Expr is Unwrap) {
				user_operator = LiftExpression (ec, expr);
				return user_operator;
			}

			return expr;
		}
	}

	public class LiftedBinaryOperator : Binary
	{
		Unwrap left_unwrap, right_unwrap;
		Expression left_orig, right_orig;
		Expression user_operator;
		MethodSpec wrap_ctor;

		public LiftedBinaryOperator (Binary.Operator op, Expression left, Expression right, Location loc)
			: base (op, left, right, loc)
		{
		}

		bool IsBitwiseBoolean {
			get {
				return (Oper == Operator.BitwiseAnd || Oper == Operator.BitwiseOr) &&
				((left_unwrap != null && left_unwrap.Type.BuiltinType == BuiltinTypeSpec.Type.Bool) ||
				 (right_unwrap != null && right_unwrap.Type.BuiltinType == BuiltinTypeSpec.Type.Bool));
			}
		}

		bool IsLeftNullLifted {
			get {
				return (state & State.LeftNullLifted) != 0;
			}
		}

		bool IsRightNullLifted {
			get {
				return (state & State.RightNullLifted) != 0;
			}
		}

		public override Expression CreateExpressionTree (ResolveContext ec)
		{
			if (user_operator != null)
				return user_operator.CreateExpressionTree (ec);

			return base.CreateExpressionTree (ec);
		}

		//
		// CSC 2 has this behavior, it allows structs to be compared
		// with the null literal *outside* of a generics context and
		// inlines that as true or false.
		//
		Constant CreateNullConstant (ResolveContext ec, Expression expr)
		{
			// FIXME: Handle side effect constants
			Constant c = new BoolConstant (ec.BuiltinTypes, Oper == Operator.Inequality, loc);

			if ((Oper & Operator.EqualityMask) != 0) {
				ec.Report.Warning (472, 2, loc, "The result of comparing value type `{0}' with null is `{1}'",
					TypeManager.CSharpName (expr.Type), c.GetValueAsLiteral ());
			} else {
				ec.Report.Warning (464, 2, loc, "The result of comparing type `{0}' with null is always `{1}'",
					TypeManager.CSharpName (expr.Type), c.GetValueAsLiteral ());
			}

			return ReducedExpression.Create (c, this);
		}

		protected override Expression DoResolve (ResolveContext ec)
		{
			if ((Oper & Operator.LogicalMask) != 0) {
				Error_OperatorCannotBeApplied (ec, left, right);
				return null;
			}

			bool use_default_call = (Oper & (Operator.BitwiseMask | Operator.EqualityMask)) != 0;
			left_orig = left;
			if (left.Type.IsNullableType) {
				left = left_unwrap = Unwrap.Create (left, use_default_call);
				if (left == null)
					return null;
			}

			right_orig = right;
			if (right.Type.IsNullableType) {
				right = right_unwrap = Unwrap.Create (right, use_default_call);
				if (right == null)
					return null;
			}

			//
			// Some details are in 6.4.2, 7.2.7
			// Arguments can be lifted for equal operators when the return type is bool and both
			// arguments are of same type
			//	
			if (left_orig is NullLiteral) {
				left = right;
				state |= State.LeftNullLifted;
				type = ec.BuiltinTypes.Bool;
			}

			if (right_orig.IsNull) {
				if ((Oper & Operator.ShiftMask) != 0)
					right = new EmptyExpression (ec.BuiltinTypes.Int);
				else
					right = left;

				state |= State.RightNullLifted;
				type = ec.BuiltinTypes.Bool;
			}

			eclass = ExprClass.Value;
			return DoResolveCore (ec, left_orig, right_orig);
		}

		void EmitBitwiseBoolean (EmitContext ec)
		{
			Label load_left = ec.DefineLabel ();
			Label load_right = ec.DefineLabel ();
			Label end_label = ec.DefineLabel ();

			// null & value, null | value
			if (left_unwrap == null) {
				left_unwrap = right_unwrap;
				right_unwrap = null;
				right = left;
			}

			left_unwrap.Emit (ec);
			ec.Emit (OpCodes.Brtrue, load_right);

			// value & null, value | null
			if (right_unwrap != null) {
				right_unwrap.Emit (ec);
				ec.Emit (OpCodes.Brtrue_S, load_left);
			}

			left_unwrap.EmitCheck (ec);
			ec.Emit (OpCodes.Brfalse_S, load_right);

			// load left
			ec.MarkLabel (load_left);

			if (Oper == Operator.BitwiseAnd) {
				left_unwrap.Load (ec);
			} else {
				if (right_unwrap == null) {
					right.Emit (ec);
					if (right is EmptyConstantCast || right is EmptyCast)
						ec.Emit (OpCodes.Newobj, NullableInfo.GetConstructor (type));
				} else {
					right_unwrap.Load (ec);
					right_unwrap = left_unwrap;
				}
			}
			ec.Emit (OpCodes.Br_S, end_label);

			// load right
			ec.MarkLabel (load_right);
			if (right_unwrap == null) {
				if (Oper == Operator.BitwiseAnd) {
					right.Emit (ec);
					if (right is EmptyConstantCast || right is EmptyCast)
						ec.Emit (OpCodes.Newobj, NullableInfo.GetConstructor (type));
				} else {
					left_unwrap.Load (ec);
				}
			} else {
				right_unwrap.Load (ec);
			}

			ec.MarkLabel (end_label);
		}

		//
		// Emits optimized equality or inequality operator when possible
		//
		void EmitEquality (EmitContext ec)
		{
			//
			// Either left or right is null
			//
			if (left_unwrap != null && (IsRightNullLifted || right.IsNull)) {
				left_unwrap.EmitCheck (ec);
				if (Oper == Binary.Operator.Equality) {
					ec.EmitInt (0);
					ec.Emit (OpCodes.Ceq);
				}
				return;
			}

			if (right_unwrap != null && (IsLeftNullLifted || left.IsNull)) {
				right_unwrap.EmitCheck (ec);
				if (Oper == Binary.Operator.Equality) {
					ec.EmitInt (0);
					ec.Emit (OpCodes.Ceq);
				}
				return;
			}

			Label dissimilar_label = ec.DefineLabel ();
			Label end_label = ec.DefineLabel ();

			if (user_operator != null) {
				user_operator.Emit (ec);
				ec.Emit (Oper == Operator.Equality ? OpCodes.Brfalse_S : OpCodes.Brtrue_S, dissimilar_label);
			} else {
				if (ec.HasSet (BuilderContext.Options.AsyncBody) && right.ContainsEmitWithAwait ()) {
					left = left.EmitToField (ec);
					right = right.EmitToField (ec);
				}

				left.Emit (ec);
				right.Emit (ec);

				ec.Emit (OpCodes.Bne_Un_S, dissimilar_label);
			}

			if (left_unwrap != null)
				left_unwrap.EmitCheck (ec);

			if (right_unwrap != null)
				right_unwrap.EmitCheck (ec);

			if (left_unwrap != null && right_unwrap != null) {
				if (Oper == Operator.Inequality)
					ec.Emit (OpCodes.Xor);
				else
					ec.Emit (OpCodes.Ceq);
			} else {
				if (Oper == Operator.Inequality) {
					ec.EmitInt (0);
					ec.Emit (OpCodes.Ceq);
				}
			}

			ec.Emit (OpCodes.Br_S, end_label);

			ec.MarkLabel (dissimilar_label);
			if (Oper == Operator.Inequality)
				ec.EmitInt (1);
			else
				ec.EmitInt (0);

			ec.MarkLabel (end_label);
		}
		
		public override void EmitBranchable (EmitContext ec, Label target, bool onTrue)
		{
			Emit (ec);
			ec.Emit (onTrue ? OpCodes.Brtrue : OpCodes.Brfalse, target);
		}			

		public override void Emit (EmitContext ec)
		{
			//
			// Optimize same expression operation
			//
			if (right_unwrap != null && right.Equals (left))
				right_unwrap = left_unwrap;

			if (user_operator == null && IsBitwiseBoolean) {
				EmitBitwiseBoolean (ec);
				return;
			}

			if ((Oper & Operator.EqualityMask) != 0) {
				EmitEquality (ec);
				return;
			}

			Label is_null_label = ec.DefineLabel ();
			Label end_label = ec.DefineLabel ();

			if (left_unwrap != null) {
				left_unwrap.EmitCheck (ec);
				ec.Emit (OpCodes.Brfalse, is_null_label);
			}

			//
			// Don't emit HasValue check when left and right expressions are same
			//
			if (right_unwrap != null && !left.Equals (right)) {
				right_unwrap.EmitCheck (ec);
				ec.Emit (OpCodes.Brfalse, is_null_label);
			}

			EmitOperator (ec, left.Type);

			if (wrap_ctor != null)
				ec.Emit (OpCodes.Newobj, wrap_ctor);

			ec.Emit (OpCodes.Br_S, end_label);
			ec.MarkLabel (is_null_label);

			if ((Oper & Operator.ComparisonMask) != 0) {
				ec.EmitInt (0);
			} else {
				LiftedNull.Create (type, loc).Emit (ec);
			}

			ec.MarkLabel (end_label);
		}

		protected override void EmitOperator (EmitContext ec, TypeSpec l)
		{
			if (user_operator != null) {
				user_operator.Emit (ec);
				return;
			}

			if (left.Type.IsNullableType) {
				l = NullableInfo.GetUnderlyingType (left.Type);
				left = EmptyCast.Create (left, l);
			}

			if (right.Type.IsNullableType) {
				right = EmptyCast.Create (right, NullableInfo.GetUnderlyingType (right.Type));
			}

			base.EmitOperator (ec, l);
		}

		Expression LiftResult (ResolveContext ec, Expression res_expr)
		{
			TypeSpec lifted_type;

			//
			// Avoid double conversion
			//
			if (left_unwrap == null || IsLeftNullLifted || left_unwrap.Type != left.Type || (left_unwrap != null && IsRightNullLifted)) {
				lifted_type = new NullableType (left.Type, loc).ResolveAsType (ec);
				if (lifted_type == null)
					return null;

				if (left is UserCast || left is EmptyCast || left is OpcodeCast)
					left.Type = lifted_type;
				else
					left = EmptyCast.Create (left, lifted_type);
			}

			if (left != right && (right_unwrap == null || IsRightNullLifted || right_unwrap.Type != right.Type || (right_unwrap != null && IsLeftNullLifted))) {
				lifted_type = new NullableType (right.Type, loc).ResolveAsType (ec);
				if (lifted_type == null)
					return null;

				var r = right;
				if (r is ReducedExpression)
					r = ((ReducedExpression) r).OriginalExpression;

				if (r is UserCast || r is EmptyCast || r is OpcodeCast)
					r.Type = lifted_type;
				else
					right = EmptyCast.Create (right, lifted_type);
			}

			if ((Oper & Operator.ComparisonMask) == 0) {
				lifted_type = new NullableType (res_expr.Type, loc).ResolveAsType (ec);
				if (lifted_type == null)
					return null;

				wrap_ctor = NullableInfo.GetConstructor (lifted_type);
				type = res_expr.Type = lifted_type;
			}

			if (IsLeftNullLifted) {
				left = LiftedNull.Create (right.Type, left.Location);

				//
				// Special case for bool?, the result depends on both null right side and left side value
				//
				if ((Oper == Operator.BitwiseAnd || Oper == Operator.BitwiseOr) && NullableInfo.GetUnderlyingType (type).BuiltinType == BuiltinTypeSpec.Type.Bool) {
					return res_expr;
				}

				if ((Oper & (Operator.ArithmeticMask | Operator.ShiftMask | Operator.BitwiseMask)) != 0)
					return LiftedNull.CreateFromExpression (ec, res_expr);

				//
				// Value types and null comparison
				//
				if (right_unwrap == null || (Oper & Operator.RelationalMask) != 0)
					return CreateNullConstant (ec, right_orig);
			}

			if (IsRightNullLifted) {
				right = LiftedNull.Create (left.Type, right.Location);

				//
				// Special case for bool?, the result depends on both null right side and left side value
				//
				if ((Oper == Operator.BitwiseAnd || Oper == Operator.BitwiseOr) && NullableInfo.GetUnderlyingType (type).BuiltinType == BuiltinTypeSpec.Type.Bool) {
					return res_expr;
				}

				if ((Oper & (Operator.ArithmeticMask | Operator.ShiftMask | Operator.BitwiseMask)) != 0)
					return LiftedNull.CreateFromExpression (ec, res_expr);

				//
				// Value types and null comparison
				//
				if (left_unwrap == null || (Oper & Operator.RelationalMask) != 0)
					return CreateNullConstant (ec, left_orig);
			}

			return res_expr;
		}

		protected override Expression ResolveOperatorPredefined (ResolveContext ec, Binary.PredefinedOperator [] operators, bool primitives_only, TypeSpec enum_type)
		{
			Expression e = base.ResolveOperatorPredefined (ec, operators, primitives_only, enum_type);

			if (e == this || enum_type != null)
				return LiftResult (ec, e);

			//
			// 7.9.9 Equality operators and null
			//
			// The == and != operators permit one operand to be a value of a nullable type and
			// the other to be the null literal, even if no predefined or user-defined operator
			// (in unlifted or lifted form) exists for the operation.
			//
			if (e == null && (Oper & Operator.EqualityMask) != 0) {
				if ((IsLeftNullLifted && right_unwrap != null) || (IsRightNullLifted && left_unwrap != null))
					return LiftResult (ec, this);
			}

			return e;
		}

		protected override Expression ResolveUserOperator (ResolveContext ec, Expression left, Expression right)
		{
			//
			// Try original types first for exact match without unwrapping
			//
			Expression expr = base.ResolveUserOperator (ec, left_orig, right_orig);
			if (expr != null)
				return expr;

			State orig_state = state;

			//
			// One side is a nullable type, try to match underlying types
			//
			if (left_unwrap != null || right_unwrap != null || (state & (State.RightNullLifted | State.LeftNullLifted)) != 0) {
				expr = base.ResolveUserOperator (ec, left, right);
			}

			if (expr == null)
				return null;

			//
			// Lift the result in the case it can be null and predefined or user operator
			// result type is of a value type
			//
			if (!TypeSpec.IsValueType (expr.Type))
				return null;

			if (state != orig_state)
				return expr;

			expr = LiftResult (ec, expr);
			if (expr is Constant)
				return expr;

			type = expr.Type;
			user_operator = expr;
			return this;
		}
	}

	public class NullCoalescingOperator : Expression
	{
		Expression left, right;
		Unwrap unwrap;
		
		public NullCoalescingOperator (Expression left, Expression right, Location loc)
		{
			this.left = left;
			this.right = right;
			this.loc = loc;
		}

		public Expression LeftExpression {
			get {
 				return left;
 			}
		}

		public Expression RightExpression {
			get {
 				return right;
 			}
		}
		
		public override Expression CreateExpressionTree (ResolveContext ec)
		{
			if (left is NullLiteral)
				ec.Report.Error (845, loc, "An expression tree cannot contain a coalescing operator with null left side");

			UserCast uc = left as UserCast;
			Expression conversion = null;
			if (uc != null) {
				left = uc.Source;

				Arguments c_args = new Arguments (2);
				c_args.Add (new Argument (uc.CreateExpressionTree (ec)));
				c_args.Add (new Argument (left.CreateExpressionTree (ec)));
				conversion = CreateExpressionFactoryCall (ec, "Lambda", c_args);
			}

			Arguments args = new Arguments (3);
			args.Add (new Argument (left.CreateExpressionTree (ec)));
			args.Add (new Argument (right.CreateExpressionTree (ec)));
			if (conversion != null)
				args.Add (new Argument (conversion));
			
			return CreateExpressionFactoryCall (ec, "Coalesce", args);
		}

		Expression ConvertExpression (ResolveContext ec)
		{
			// TODO: ImplicitConversionExists should take care of this
			if (left.eclass == ExprClass.MethodGroup)
				return null;

			TypeSpec ltype = left.Type;

			//
			// If left is a nullable type and an implicit conversion exists from right to underlying type of left,
			// the result is underlying type of left
			//
			if (ltype.IsNullableType) {
				unwrap = Unwrap.Create (left, false);
				if (unwrap == null)
					return null;

				//
				// Reduce (left ?? null) to left
				//
				if (right.IsNull)
					return ReducedExpression.Create (left, this);

				if (Convert.ImplicitConversionExists (ec, right, unwrap.Type)) {
					left = unwrap;
					ltype = left.Type;

					//
					// If right is a dynamic expression, the result type is dynamic
					//
					if (right.Type.BuiltinType == BuiltinTypeSpec.Type.Dynamic) {
						type = right.Type;

						// Need to box underlying value type
						left = Convert.ImplicitBoxingConversion (left, ltype, type);
						return this;
					}

					right = Convert.ImplicitConversion (ec, right, ltype, loc);
					type = ltype;
					return this;
				}
			} else if (TypeSpec.IsReferenceType (ltype)) {
				if (Convert.ImplicitConversionExists (ec, right, ltype)) {
					//
					// If right is a dynamic expression, the result type is dynamic
					//
					if (right.Type.BuiltinType == BuiltinTypeSpec.Type.Dynamic) {
						type = right.Type;
						return this;
					}

					//
					// Reduce ("foo" ?? expr) to expression
					//
					Constant lc = left as Constant;
					if (lc != null && !lc.IsDefaultValue)
						return ReducedExpression.Create (lc, this);

					//
					// Reduce (left ?? null) to left OR (null-constant ?? right) to right
					//
					if (right.IsNull || lc != null)
						return ReducedExpression.Create (lc != null ? right : left, this);

					right = Convert.ImplicitConversion (ec, right, ltype, loc);
					type = ltype;
					return this;
				}

				//
				// Special case null ?? null
				//
				if (ltype == right.Type) {
					type = ltype;
					return this;
				}
			} else {
				return null;
			}

			TypeSpec rtype = right.Type;
			if (!Convert.ImplicitConversionExists (ec, unwrap != null ? unwrap : left, rtype) || right.eclass == ExprClass.MethodGroup)
				return null;

			//
			// Reduce (null ?? right) to right
			//
			if (left.IsNull)
				return ReducedExpression.Create (right, this).Resolve (ec);

			left = Convert.ImplicitConversion (ec, unwrap != null ? unwrap : left, rtype, loc);
			type = rtype;
			return this;
		}

		public override bool ContainsEmitWithAwait ()
		{
			if (unwrap != null)
				return unwrap.ContainsEmitWithAwait () || right.ContainsEmitWithAwait ();

			return left.ContainsEmitWithAwait () || right.ContainsEmitWithAwait ();
		}

		protected override Expression DoResolve (ResolveContext ec)
		{
			left = left.Resolve (ec);
			right = right.Resolve (ec);

			if (left == null || right == null)
				return null;

			eclass = ExprClass.Value;

			Expression e = ConvertExpression (ec);
			if (e == null) {
				Binary.Error_OperatorCannotBeApplied (ec, left, right, "??", loc);
				return null;
			}

			return e;
		}

		public override void Emit (EmitContext ec)
		{
			Label end_label = ec.DefineLabel ();

			if (unwrap != null) {
				Label is_null_label = ec.DefineLabel ();

				unwrap.EmitCheck (ec);
				ec.Emit (OpCodes.Brfalse, is_null_label);

				left.Emit (ec);
				ec.Emit (OpCodes.Br, end_label);

				ec.MarkLabel (is_null_label);
				right.Emit (ec);

				ec.MarkLabel (end_label);
				return;
			}

			left.Emit (ec);
			ec.Emit (OpCodes.Dup);

			// Only to make verifier happy
			if (left.Type.IsGenericParameter)
				ec.Emit (OpCodes.Box, left.Type);

			ec.Emit (OpCodes.Brtrue, end_label);

			ec.Emit (OpCodes.Pop);
			right.Emit (ec);

			ec.MarkLabel (end_label);
		}

		protected override void CloneTo (CloneContext clonectx, Expression t)
		{
			NullCoalescingOperator target = (NullCoalescingOperator) t;

			target.left = left.Clone (clonectx);
			target.right = right.Clone (clonectx);
		}
		
		public override object Accept (StructuralVisitor visitor)
		{
			return visitor.Visit (this);
		}
	}

	class LiftedUnaryMutator : UnaryMutator
	{
		public LiftedUnaryMutator (Mode mode, Expression expr, Location loc)
			: base (mode, expr, loc)
		{
		}

		protected override Expression DoResolve (ResolveContext ec)
		{
			var orig_expr = expr;

			expr = Unwrap.Create (expr);

			var res = base.DoResolveOperation (ec);

			expr = orig_expr;
			type = expr.Type;

			return res;
		}

		protected override void EmitOperation (EmitContext ec)
		{
			Label is_null_label = ec.DefineLabel ();
			Label end_label = ec.DefineLabel ();

			LocalTemporary lt = new LocalTemporary (type);

			// Value is on the stack
			lt.Store (ec);

			var call = new CallEmitter ();
			call.InstanceExpression = lt;
			call.EmitPredefined (ec, NullableInfo.GetHasValue (expr.Type), null);

			ec.Emit (OpCodes.Brfalse, is_null_label);

			call = new CallEmitter ();
			call.InstanceExpression = lt;
			call.EmitPredefined (ec, NullableInfo.GetValue (expr.Type), null);

			lt.Release (ec);

			base.EmitOperation (ec);

			ec.Emit (OpCodes.Newobj, NullableInfo.GetConstructor (type));
			ec.Emit (OpCodes.Br_S, end_label);

			ec.MarkLabel (is_null_label);
			LiftedNull.Create (type, loc).Emit (ec);

			ec.MarkLabel (end_label);
		}
	}
}

