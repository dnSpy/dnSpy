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
using SLE = System.Linq.Expressions;

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

		public override TypeSpec ResolveAsType (IMemberContext ec, bool allowUnboundTypeArguments = false)
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

		//
		// Don't use unless really required for correctness, see Unwrap::Emit
		//
		public static MethodSpec GetValue (TypeSpec nullableType)
		{
			return (MethodSpec) MemberCache.FindMember (nullableType,
				MemberFilter.Method ("get_Value", 0, ParametersCompiled.EmptyReadOnlyParameters, null), BindingRestriction.None);
		}

		public static TypeSpec GetUnderlyingType (TypeSpec nullableType)
		{
			return ((InflatedTypeSpec) nullableType).TypeArguments[0];
		}

		public static TypeSpec GetEnumUnderlyingType (ModuleContainer module, TypeSpec nullableEnum)
		{
			return MakeType (module, EnumSpec.GetUnderlyingType (GetUnderlyingType (nullableEnum)));
		}

		public static TypeSpec MakeType (ModuleContainer module, TypeSpec underlyingType)
		{
			return module.PredefinedTypes.Nullable.TypeSpec.MakeGenericType (module,
				new[] { underlyingType });

		}
	}

	public class Unwrap : Expression, IMemoryLocation
	{
		Expression expr;

		LocalTemporary temp;
		Expression temp_field;
		readonly bool useDefaultValue;

		public Unwrap (Expression expr, bool useDefaultValue = true)
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

		// TODO: REMOVE
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

		public static Expression CreateUnwrapped (Expression expr)
		{
			//
			// Avoid unwraping and wraping of same type
			//
			Wrap wrap = expr as Wrap;
			if (wrap != null)
				return wrap.Child;

			return Create (expr, true);
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

			//
			// Using GetGetValueOrDefault is prefered because JIT can possibly
			// inline it whereas Value property contains a throw which is very
			// unlikely to be inlined
			//
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

		public override void EmitSideEffect (EmitContext ec)
		{
			expr.EmitSideEffect (ec);
		}

		public override Expression EmitToField (EmitContext ec)
		{
			if (temp_field == null)
				temp_field = this.expr.EmitToField (ec);
			
			return this;
		}

		public override bool Equals (object obj)
		{
			Unwrap uw = obj as Unwrap;
			return uw != null && expr.Equals (uw.expr);
		}

		public override void FlowAnalysis (FlowAnalysisContext fc)
		{
			expr.FlowAnalysis (fc);
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

		public void Store (EmitContext ec)
		{
			if (temp != null || temp_field != null)
				return;

			if (expr is VariableReference)
				return;

			expr.Emit (ec);
			LocalVariable.Store (ec);
		}

		public void Load (EmitContext ec)
		{
			if (temp_field != null)
				temp_field.Emit (ec);
			else if (expr is VariableReference)
				expr.Emit (ec);
			else
				LocalVariable.Emit (ec);
		}

		public override SLE.Expression MakeExpression (BuilderContext ctx)
		{
			return expr.MakeExpression (ctx);
		}

		public void AddressOf (EmitContext ec, AddressOp mode)
		{
			IMemoryLocation ml;

			if (temp_field != null) {
				ml = temp_field as IMemoryLocation;
				if (ml == null) {
					var lt = new LocalTemporary (temp_field.Type);
					temp_field.Emit (ec);
					lt.Store (ec);
					ml = lt;
				}
			} else {
				ml = expr as VariableReference;
			}

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
				if (temp == null && temp_field == null)
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

			var user_cast = child as UserCast;
			if (user_cast != null) {
				child.Type = type;
				return user_cast.CreateExpressionTree (ec);
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

		public static Constant CreateFromExpression (ResolveContext rc, Expression e)
		{
			if (!rc.HasSet (ResolveContext.Options.ExpressionTreeConversion)) {
				rc.Report.Warning (458, 2, e.Location, "The result of the expression is always `null' of type `{0}'",
					e.Type.GetSignatureForError ());
			}

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
			value_target.AddressOf (ec, Mode);
		}
	}

	//
	// Generic lifting expression, supports all S/S? -> T/T? cases
	//
	public class LiftedConversion : Expression, IMemoryLocation
	{
		Expression expr, null_value;
		Unwrap unwrap;

		public LiftedConversion (Expression expr, Unwrap unwrap, TypeSpec type)
		{
			this.expr = expr;
			this.unwrap = unwrap;
			this.loc = expr.Location;
			this.type = type;
		}

		public LiftedConversion (Expression expr, Expression unwrap, TypeSpec type)
			: this (expr, unwrap as Unwrap, type)
		{
		}

		public override bool IsNull {
			get {
				return expr.IsNull;
			}
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
				if (!expr.Type.IsNullableType) {
					expr = Wrap.Create (expr, type);
					if (expr == null)
						return null;
				}

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

		public override void FlowAnalysis (FlowAnalysisContext fc)
		{
			expr.FlowAnalysis (fc);
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
			if (res == null) {
				Error_OperatorCannotBeApplied (ec, loc, OperName (Oper), Expr.Type);
				return null;
			}

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

	//
	// Lifted version of binary operators
	//
	class LiftedBinaryOperator : Expression
	{
		public LiftedBinaryOperator (Binary b)
		{
			this.Binary = b;
			this.loc = b.Location;
		}

		public Binary Binary { get; private set; }

		public Expression Left { get; set; }

		public Expression Right { get; set; }

		public Unwrap UnwrapLeft { get; set; }

		public Unwrap UnwrapRight { get; set; }

		public MethodSpec UserOperator { get; set; }

		bool IsBitwiseBoolean {
			get {
				return (Binary.Oper == Binary.Operator.BitwiseAnd || Binary.Oper == Binary.Operator.BitwiseOr) &&
				((UnwrapLeft != null && UnwrapLeft.Type.BuiltinType == BuiltinTypeSpec.Type.Bool) ||
				 (UnwrapRight != null && UnwrapRight.Type.BuiltinType == BuiltinTypeSpec.Type.Bool));
			}
		}

		public override bool ContainsEmitWithAwait ()
		{
			return Left.ContainsEmitWithAwait () || Right.ContainsEmitWithAwait ();
		}

		public override Expression CreateExpressionTree (ResolveContext rc)
		{
			if (UserOperator != null) {
				Arguments args = new Arguments (2);
				args.Add (new Argument (Binary.Left));
				args.Add (new Argument (Binary.Right));

				var method = new UserOperatorCall (UserOperator, args, Binary.CreateExpressionTree, loc);
				return method.CreateExpressionTree (rc);
			}

			return Binary.CreateExpressionTree (rc);
		}

		protected override Expression DoResolve (ResolveContext rc)
		{
			if (rc.IsRuntimeBinder) {
				if (UnwrapLeft == null && !Left.Type.IsNullableType)
					Left = LiftOperand (rc, Left);

				if (UnwrapRight == null && !Right.Type.IsNullableType)
					Right = LiftOperand (rc, Right);
			} else {
				if (UnwrapLeft == null && Left != null && Left.Type.IsNullableType) {
					Left = Unwrap.CreateUnwrapped (Left);
					UnwrapLeft = Left as Unwrap;
				}

				if (UnwrapRight == null && Right != null && Right.Type.IsNullableType) {
					Right = Unwrap.CreateUnwrapped (Right);
					UnwrapRight = Right as Unwrap;
				}
			}

			type = Binary.Type;
			eclass = Binary.eclass;	

			return this;
		}

		Expression LiftOperand (ResolveContext rc, Expression expr)
		{
			TypeSpec type;
			if (expr.IsNull) {
				type = Left.IsNull ? Right.Type : Left.Type;
			} else {
				type = expr.Type;
			}

			if (!type.IsNullableType)
				type = NullableInfo.MakeType (rc.Module, type);

			return Wrap.Create (expr, type);
		}

		public override void Emit (EmitContext ec)
		{
			if (IsBitwiseBoolean && UserOperator == null) {
				EmitBitwiseBoolean (ec);
				return;
			}

			if ((Binary.Oper & Binary.Operator.EqualityMask) != 0) {
				EmitEquality (ec);
				return;
			}

			Label is_null_label = ec.DefineLabel ();
			Label end_label = ec.DefineLabel ();

			if (ec.HasSet (BuilderContext.Options.AsyncBody) && Right.ContainsEmitWithAwait ()) {
				Left = Left.EmitToField (ec);
				Right = Right.EmitToField (ec);
			}

			if (UnwrapLeft != null) {
				UnwrapLeft.EmitCheck (ec);
			}

			//
			// Don't emit HasValue check when left and right expressions are same
			//
			if (UnwrapRight != null && !Binary.Left.Equals (Binary.Right)) {
				UnwrapRight.EmitCheck (ec);
				if (UnwrapLeft != null) {
					ec.Emit (OpCodes.And);
				}
			}

			ec.Emit (OpCodes.Brfalse, is_null_label);

			if (UserOperator != null) {
				var args = new Arguments (2);
				args.Add (new Argument (Left));
				args.Add (new Argument (Right));

				var call = new CallEmitter ();
				call.EmitPredefined (ec, UserOperator, args);
			} else {
				Binary.EmitOperator (ec, Left, Right);
			}

			//
			// Wrap the result when the operator return type is nullable type
			//
			if (type.IsNullableType)
				ec.Emit (OpCodes.Newobj, NullableInfo.GetConstructor (type));

			ec.Emit (OpCodes.Br_S, end_label);
			ec.MarkLabel (is_null_label);

			if ((Binary.Oper & Binary.Operator.ComparisonMask) != 0) {
				ec.EmitInt (0);
			} else {
				LiftedNull.Create (type, loc).Emit (ec);
			}

			ec.MarkLabel (end_label);
		}

		void EmitBitwiseBoolean (EmitContext ec)
		{
			Label load_left = ec.DefineLabel ();
			Label load_right = ec.DefineLabel ();
			Label end_label = ec.DefineLabel ();
			Label is_null_label = ec.DefineLabel ();

			bool or = Binary.Oper == Binary.Operator.BitwiseOr;

			//
			// Both operands are bool? types
			//
			if (UnwrapLeft != null && UnwrapRight != null) {
				if (ec.HasSet (BuilderContext.Options.AsyncBody) && Binary.Right.ContainsEmitWithAwait ()) {
					Left = Left.EmitToField (ec);
					Right = Right.EmitToField (ec);
				} else {
					UnwrapLeft.Store (ec);
					UnwrapRight.Store (ec);
				}

				Left.Emit (ec);
				ec.Emit (OpCodes.Brtrue_S, load_right);

				Right.Emit (ec);
				ec.Emit (OpCodes.Brtrue_S, load_left);

				UnwrapLeft.EmitCheck (ec);
				ec.Emit (OpCodes.Brfalse_S, load_right);

				// load left
				ec.MarkLabel (load_left);
				if (or)
					UnwrapRight.Load (ec);
				else
					UnwrapLeft.Load (ec);

				ec.Emit (OpCodes.Br_S, end_label);

				// load right
				ec.MarkLabel (load_right);
				if (or)
					UnwrapLeft.Load (ec);
				else
					UnwrapRight.Load (ec);

				ec.MarkLabel (end_label);
				return;
			}

			//
			// Faster version when one operand is bool
			//
			if (UnwrapLeft == null) {
				//
				// (bool, bool?)
				//
				// Optimizes remaining (false & bool?), (true | bool?) which are not easy to handle
				// in binary expression reduction
				//
				var c = Left as BoolConstant;
				if (c != null) {
					// Keep evaluation order
					UnwrapRight.Store (ec);

					ec.EmitInt (or ? 1 : 0);
					ec.Emit (OpCodes.Newobj, NullableInfo.GetConstructor (type));
				} else if (Left.IsNull) {
					UnwrapRight.Emit (ec);
					ec.Emit (or ? OpCodes.Brfalse_S : OpCodes.Brtrue_S, is_null_label);

					UnwrapRight.Load (ec);
					ec.Emit (OpCodes.Br_S, end_label);

					ec.MarkLabel (is_null_label);
					LiftedNull.Create (type, loc).Emit (ec);
				} else {
					Left.Emit (ec);
					ec.Emit (or ? OpCodes.Brfalse_S : OpCodes.Brtrue_S, load_right);

					ec.EmitInt (or ? 1 : 0);
					ec.Emit (OpCodes.Newobj, NullableInfo.GetConstructor (type));

					ec.Emit (OpCodes.Br_S, end_label);

					ec.MarkLabel (load_right);
					UnwrapRight.Original.Emit (ec);
				}
			} else {
				//
				// (bool?, bool)
				//
				// Keep left-right evaluation order
				UnwrapLeft.Store (ec);

				//
				// Optimizes remaining (bool? & false), (bool? | true) which are not easy to handle
				// in binary expression reduction
				//
				var c = Right as BoolConstant;
				if (c != null) {
					ec.EmitInt (or ? 1 : 0);
					ec.Emit (OpCodes.Newobj, NullableInfo.GetConstructor (type));
				} else if (Right.IsNull) {
					UnwrapLeft.Emit (ec);
					ec.Emit (or ? OpCodes.Brfalse_S : OpCodes.Brtrue_S, is_null_label);

					UnwrapLeft.Load (ec);
					ec.Emit (OpCodes.Br_S, end_label);

					ec.MarkLabel (is_null_label);
					LiftedNull.Create (type, loc).Emit (ec);
				} else {
					Right.Emit (ec);
					ec.Emit (or ? OpCodes.Brfalse_S : OpCodes.Brtrue_S, load_right);

					ec.EmitInt (or ? 1 : 0);
					ec.Emit (OpCodes.Newobj, NullableInfo.GetConstructor (type));

					ec.Emit (OpCodes.Br_S, end_label);

					ec.MarkLabel (load_right);

					UnwrapLeft.Load (ec);
				}
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
			if (UnwrapLeft != null && Binary.Right.IsNull) { // TODO: Optimize for EmitBranchable
				//
				// left.HasValue == false 
				//
				UnwrapLeft.EmitCheck (ec);
				if (Binary.Oper == Binary.Operator.Equality) {
					ec.EmitInt (0);
					ec.Emit (OpCodes.Ceq);
				}
				return;
			}

			if (UnwrapRight != null && Binary.Left.IsNull) {
				//
				// right.HasValue == false 
				//
				UnwrapRight.EmitCheck (ec);
				if (Binary.Oper == Binary.Operator.Equality) {
					ec.EmitInt (0);
					ec.Emit (OpCodes.Ceq);
				}
				return;
			}

			Label dissimilar_label = ec.DefineLabel ();
			Label end_label = ec.DefineLabel ();

			if (UserOperator != null) {
				var left = Left;

				if (UnwrapLeft != null) {
					UnwrapLeft.EmitCheck (ec);
				} else {
					// Keep evaluation order same
					if (!(Left is VariableReference)) {
						Left.Emit (ec);
						var lt = new LocalTemporary (Left.Type);
						lt.Store (ec);
						left = lt;
					}
				}

				if (UnwrapRight != null) {
					UnwrapRight.EmitCheck (ec);

					if (UnwrapLeft != null) {
						ec.Emit (OpCodes.Bne_Un, dissimilar_label);

						Label compare_label = ec.DefineLabel ();
						UnwrapLeft.EmitCheck (ec);
						ec.Emit (OpCodes.Brtrue, compare_label);

						if (Binary.Oper == Binary.Operator.Equality)
							ec.EmitInt (1);
						else
							ec.EmitInt (0);

						ec.Emit (OpCodes.Br, end_label);

						ec.MarkLabel (compare_label);
					} else {
						ec.Emit (OpCodes.Brfalse, dissimilar_label);
					}
				} else {
					ec.Emit (OpCodes.Brfalse, dissimilar_label);
				}

				var args = new Arguments (2);
				args.Add (new Argument (left));
				args.Add (new Argument (Right));

				var call = new CallEmitter ();
				call.EmitPredefined (ec, UserOperator, args);
			} else {
				if (ec.HasSet (BuilderContext.Options.AsyncBody) && Binary.Right.ContainsEmitWithAwait ()) {
					Left = Left.EmitToField (ec);
					Right = Right.EmitToField (ec);
				}

				//
				// Emit underlying value comparison first.
				//
				// For this code: int? a = 1; bool b = a == 1;
				//
				// We emit something similar to this. Expressions with side effects have local
				// variable created by Unwrap expression
				//
				//	left.GetValueOrDefault ()
				//	right
				//	bne.un.s   dissimilar_label
				//  left.HasValue
				//	br.s       end_label
				// dissimilar_label:
				//	ldc.i4.0
				// end_label:
				//

				Left.Emit (ec);
				Right.Emit (ec);

				ec.Emit (OpCodes.Bne_Un_S, dissimilar_label);

				//
				// Check both left and right expressions for Unwrap call in which
				// case we need to run get_HasValue() check because the type is
				// nullable and could have null value
				//
				if (UnwrapLeft != null)
					UnwrapLeft.EmitCheck (ec);

				if (UnwrapRight != null)
					UnwrapRight.EmitCheck (ec);

				if (UnwrapLeft != null && UnwrapRight != null) {
					if (Binary.Oper == Binary.Operator.Inequality)
						ec.Emit (OpCodes.Xor);
					else
						ec.Emit (OpCodes.Ceq);
				} else {
					if (Binary.Oper == Binary.Operator.Inequality) {
						ec.EmitInt (0);
						ec.Emit (OpCodes.Ceq);
					}
				}
			}

			ec.Emit (OpCodes.Br_S, end_label);

			ec.MarkLabel (dissimilar_label);
			if (Binary.Oper == Binary.Operator.Inequality)
				ec.EmitInt (1);
			else
				ec.EmitInt (0);

			ec.MarkLabel (end_label);
		}

		public override void FlowAnalysis (FlowAnalysisContext fc)
		{
			Binary.FlowAnalysis (fc);
		}

		public override SLE.Expression MakeExpression (BuilderContext ctx)
		{
			return Binary.MakeExpression (ctx, Left, Right);
		}
	}

	public class NullCoalescingOperator : Expression
	{
		Expression left, right;
		Unwrap unwrap;

		public NullCoalescingOperator (Expression left, Expression right)
		{
			this.left = left;
			this.right = right;
			this.loc = left.Location;
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

				Expression conv;
				if (right.Type.IsNullableType) {
					conv = right.Type == ltype ? right : Convert.ImplicitNulableConversion (ec, right, ltype);
					if (conv != null) {
						right = conv;
						type = ltype;
						return this;
					}
				} else {
					conv = Convert.ImplicitConversion (ec, right, unwrap.Type, loc);
					if (conv != null) {
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

						right = conv;
						type = ltype;
						return this;
					}
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
						return ReducedExpression.Create (lc, this, false);

					//
					// Reduce (left ?? null) to left OR (null-constant ?? right) to right
					//
					if (right.IsNull || lc != null) {
						//
						// Special case null ?? null
						//
						if (right.IsNull && ltype == right.Type)
							return null;

						return ReducedExpression.Create (lc != null ? right : left, this, false);
					}

					right = Convert.ImplicitConversion (ec, right, ltype, loc);
					type = ltype;
					return this;
				}
			} else {
				return null;
			}

			TypeSpec rtype = right.Type;
			if (!Convert.ImplicitConversionExists (ec, unwrap ?? left, rtype) || right.eclass == ExprClass.MethodGroup)
				return null;

			//
			// Reduce (null ?? right) to right
			//
			if (left.IsNull)
				return ReducedExpression.Create (right, this, false).Resolve (ec);

			left = Convert.ImplicitConversion (ec, unwrap ?? left, rtype, loc);

			if (TypeSpec.IsValueType (left.Type) && !left.Type.IsNullableType) {
				Warning_UnreachableExpression (ec, right.Location);
				return ReducedExpression.Create (left, this, false).Resolve (ec);
			}

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

				//
				// When both expressions are nullable the unwrap
				// is needed only for null check not for value uwrap
				//
				if (type.IsNullableType && TypeSpecComparer.IsEqual (NullableInfo.GetUnderlyingType (type), unwrap.Type))
					unwrap.Load (ec);
				else
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

		public override void FlowAnalysis (FlowAnalysisContext fc)
		{
			left.FlowAnalysis (fc);
			var left_da = fc.BranchDefiniteAssignment ();
			right.FlowAnalysis (fc);
			fc.DefiniteAssignment = left_da;
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
			call.EmitPredefined (ec, NullableInfo.GetGetValueOrDefault (expr.Type), null);

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

