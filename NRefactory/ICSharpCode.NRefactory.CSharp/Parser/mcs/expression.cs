//
// expression.cs: Expression representation for the IL tree.
//
// Author:
//   Miguel de Icaza (miguel@ximian.com)
//   Marek Safar (marek.safar@gmail.com)
//
// Copyright 2001, 2002, 2003 Ximian, Inc.
// Copyright 2003-2008 Novell, Inc.
// Copyright 2011 Xamarin Inc.
//

using System;
using System.Collections.Generic;
using System.Linq;
using SLE = System.Linq.Expressions;

#if STATIC
using MetaType = IKVM.Reflection.Type;
using IKVM.Reflection;
using IKVM.Reflection.Emit;
#else
using MetaType = System.Type;
using System.Reflection;
using System.Reflection.Emit;
#endif

namespace Mono.CSharp
{
	//
	// This is an user operator expression, automatically created during
	// resolve phase
	//
	public class UserOperatorCall : Expression {
		protected readonly Arguments arguments;
		protected readonly MethodSpec oper;
		readonly Func<ResolveContext, Expression, Expression> expr_tree;

		public UserOperatorCall (MethodSpec oper, Arguments args, Func<ResolveContext, Expression, Expression> expr_tree, Location loc)
		{
			this.oper = oper;
			this.arguments = args;
			this.expr_tree = expr_tree;

			type = oper.ReturnType;
			eclass = ExprClass.Value;
			this.loc = loc;
		}

		public override bool ContainsEmitWithAwait ()
		{
			return arguments.ContainsEmitWithAwait ();
		}

		public override Expression CreateExpressionTree (ResolveContext ec)
		{
			if (expr_tree != null)
				return expr_tree (ec, new TypeOfMethod (oper, loc));

			Arguments args = Arguments.CreateForExpressionTree (ec, arguments,
				new NullLiteral (loc),
				new TypeOfMethod (oper, loc));

			return CreateExpressionFactoryCall (ec, "Call", args);
		}

		protected override void CloneTo (CloneContext context, Expression target)
		{
			// Nothing to clone
		}
		
		protected override Expression DoResolve (ResolveContext ec)
		{
			//
			// We are born fully resolved
			//
			return this;
		}

		public override void Emit (EmitContext ec)
		{
			var call = new CallEmitter ();
			call.EmitPredefined (ec, oper, arguments);
		}

		public override SLE.Expression MakeExpression (BuilderContext ctx)
		{
#if STATIC
			return base.MakeExpression (ctx);
#else
			return SLE.Expression.Call ((MethodInfo) oper.GetMetaInfo (), Arguments.MakeExpression (arguments, ctx));
#endif
		}
	}

	public class ParenthesizedExpression : ShimExpression
	{
		public ParenthesizedExpression (Expression expr)
			: base (expr)
		{
			loc = expr.Location;
		}

		protected override Expression DoResolve (ResolveContext ec)
		{
			var res = expr.Resolve (ec);
			var constant = res as Constant;
			if (constant != null && constant.IsLiteral)
				return Constant.CreateConstantFromValue (res.Type, constant.GetValue (), expr.Location);

			return res;
		}

		public override Expression DoResolveLValue (ResolveContext ec, Expression right_side)
		{
			return expr.DoResolveLValue (ec, right_side);
		}
		
		public override object Accept (StructuralVisitor visitor)
		{
			return visitor.Visit (this);
		}
	}
	
	//
	//   Unary implements unary expressions.
	//
	public class Unary : Expression
	{
		public enum Operator : byte {
			UnaryPlus, UnaryNegation, LogicalNot, OnesComplement,
			AddressOf,  TOP
		}

		public readonly Operator Oper;
		public Expression Expr;
		Expression enum_conversion;

		public Unary (Operator op, Expression expr, Location loc)
		{
			Oper = op;
			Expr = expr;
			this.loc = loc;
		}

		// <summary>
		//   This routine will attempt to simplify the unary expression when the
		//   argument is a constant.
		// </summary>
		Constant TryReduceConstant (ResolveContext ec, Constant constant)
		{
			var e = constant;

			while (e is EmptyConstantCast)
				e = ((EmptyConstantCast) e).child;
			
			if (e is SideEffectConstant) {
				Constant r = TryReduceConstant (ec, ((SideEffectConstant) e).value);
				return r == null ? null : new SideEffectConstant (r, e, r.Location);
			}

			TypeSpec expr_type = e.Type;
			
			switch (Oper){
			case Operator.UnaryPlus:
				// Unary numeric promotions
				switch (expr_type.BuiltinType) {
				case BuiltinTypeSpec.Type.Byte:
					return new IntConstant (ec.BuiltinTypes, ((ByteConstant) e).Value, e.Location);
				case BuiltinTypeSpec.Type.SByte:
					return new IntConstant (ec.BuiltinTypes, ((SByteConstant) e).Value, e.Location);
				case BuiltinTypeSpec.Type.Short:
					return new IntConstant (ec.BuiltinTypes, ((ShortConstant) e).Value, e.Location);
				case BuiltinTypeSpec.Type.UShort:
					return new IntConstant (ec.BuiltinTypes, ((UShortConstant) e).Value, e.Location);
				case BuiltinTypeSpec.Type.Char:
					return new IntConstant (ec.BuiltinTypes, ((CharConstant) e).Value, e.Location);
				
				// Predefined operators
				case BuiltinTypeSpec.Type.Int:
				case BuiltinTypeSpec.Type.UInt:
				case BuiltinTypeSpec.Type.Long:
				case BuiltinTypeSpec.Type.ULong:
				case BuiltinTypeSpec.Type.Float:
				case BuiltinTypeSpec.Type.Double:
				case BuiltinTypeSpec.Type.Decimal:
					return e;
				}
				
				return null;
				
			case Operator.UnaryNegation:
				// Unary numeric promotions
				switch (expr_type.BuiltinType) {
				case BuiltinTypeSpec.Type.Byte:
					return new IntConstant (ec.BuiltinTypes, -((ByteConstant) e).Value, e.Location);
				case BuiltinTypeSpec.Type.SByte:
					return new IntConstant (ec.BuiltinTypes, -((SByteConstant) e).Value, e.Location);
				case BuiltinTypeSpec.Type.Short:
					return new IntConstant (ec.BuiltinTypes, -((ShortConstant) e).Value, e.Location);
				case BuiltinTypeSpec.Type.UShort:
					return new IntConstant (ec.BuiltinTypes, -((UShortConstant) e).Value, e.Location);
				case BuiltinTypeSpec.Type.Char:
					return new IntConstant (ec.BuiltinTypes, -((CharConstant) e).Value, e.Location);

				// Predefined operators
				case BuiltinTypeSpec.Type.Int:
					int ivalue = ((IntConstant) e).Value;
					if (ivalue == int.MinValue) {
						if (ec.ConstantCheckState) {
							ConstantFold.Error_CompileTimeOverflow (ec, loc);
							return null;
						}
						return e;
					}
					return new IntConstant (ec.BuiltinTypes, -ivalue, e.Location);

				case BuiltinTypeSpec.Type.Long:
					long lvalue = ((LongConstant) e).Value;
					if (lvalue == long.MinValue) {
						if (ec.ConstantCheckState) {
							ConstantFold.Error_CompileTimeOverflow (ec, loc);
							return null;
						}
						return e;
					}
					return new LongConstant (ec.BuiltinTypes, -lvalue, e.Location);

				case BuiltinTypeSpec.Type.UInt:
					UIntLiteral uil = constant as UIntLiteral;
					if (uil != null) {
						if (uil.Value == int.MaxValue + (uint) 1)
							return new IntLiteral (ec.BuiltinTypes, int.MinValue, e.Location);
						return new LongLiteral (ec.BuiltinTypes, -uil.Value, e.Location);
					}
					return new LongConstant (ec.BuiltinTypes, -((UIntConstant) e).Value, e.Location);


				case BuiltinTypeSpec.Type.ULong:
					ULongLiteral ull = constant as ULongLiteral;
					if (ull != null && ull.Value == 9223372036854775808)
						return new LongLiteral (ec.BuiltinTypes, long.MinValue, e.Location);
					return null;

				case BuiltinTypeSpec.Type.Float:
					FloatLiteral fl = constant as FloatLiteral;
					// For better error reporting
					if (fl != null)
						return new FloatLiteral (ec.BuiltinTypes, -fl.Value, e.Location);

					return new FloatConstant (ec.BuiltinTypes, -((FloatConstant) e).Value, e.Location);

				case BuiltinTypeSpec.Type.Double:
					DoubleLiteral dl = constant as DoubleLiteral;
					// For better error reporting
					if (dl != null)
						return new DoubleLiteral (ec.BuiltinTypes, -dl.Value, e.Location);

					return new DoubleConstant (ec.BuiltinTypes, -((DoubleConstant) e).Value, e.Location);

				case BuiltinTypeSpec.Type.Decimal:
					return new DecimalConstant (ec.BuiltinTypes, -((DecimalConstant) e).Value, e.Location);
				}

				return null;
				
			case Operator.LogicalNot:
				if (expr_type.BuiltinType != BuiltinTypeSpec.Type.Bool)
					return null;
				
				bool b = (bool)e.GetValue ();
				return new BoolConstant (ec.BuiltinTypes, !b, e.Location);
				
			case Operator.OnesComplement:
				// Unary numeric promotions
				switch (expr_type.BuiltinType) {
				case BuiltinTypeSpec.Type.Byte:
					return new IntConstant (ec.BuiltinTypes, ~((ByteConstant) e).Value, e.Location);
				case BuiltinTypeSpec.Type.SByte:
					return new IntConstant (ec.BuiltinTypes, ~((SByteConstant) e).Value, e.Location);
				case BuiltinTypeSpec.Type.Short:
					return new IntConstant (ec.BuiltinTypes, ~((ShortConstant) e).Value, e.Location);
				case BuiltinTypeSpec.Type.UShort:
					return new IntConstant (ec.BuiltinTypes, ~((UShortConstant) e).Value, e.Location);
				case BuiltinTypeSpec.Type.Char:
					return new IntConstant (ec.BuiltinTypes, ~((CharConstant) e).Value, e.Location);
				
				// Predefined operators
				case BuiltinTypeSpec.Type.Int:
					return new IntConstant (ec.BuiltinTypes, ~((IntConstant)e).Value, e.Location);
				case BuiltinTypeSpec.Type.UInt:
					return new UIntConstant (ec.BuiltinTypes, ~((UIntConstant) e).Value, e.Location);
				case BuiltinTypeSpec.Type.Long:
					return new LongConstant (ec.BuiltinTypes, ~((LongConstant) e).Value, e.Location);
				case BuiltinTypeSpec.Type.ULong:
					return new ULongConstant (ec.BuiltinTypes, ~((ULongConstant) e).Value, e.Location);
				}
				if (e is EnumConstant) {
					e = TryReduceConstant (ec, ((EnumConstant)e).Child);
					if (e != null)
						e = new EnumConstant (e, expr_type);
					return e;
				}
				return null;
			}
			throw new Exception ("Can not constant fold: " + Oper.ToString());
		}
		
		protected virtual Expression ResolveOperator (ResolveContext ec, Expression expr)
		{
			eclass = ExprClass.Value;

			TypeSpec expr_type = expr.Type;
			Expression best_expr;

			TypeSpec[] predefined = ec.BuiltinTypes.OperatorsUnary [(int) Oper];

			//
			// Primitive types first
			//
			if (BuiltinTypeSpec.IsPrimitiveType (expr_type)) {
				best_expr = ResolvePrimitivePredefinedType (ec, expr, predefined);
				if (best_expr == null)
					return null;

				type = best_expr.Type;
				Expr = best_expr;
				return this;
			}

			//
			// E operator ~(E x);
			//
			if (Oper == Operator.OnesComplement && expr_type.IsEnum)
				return ResolveEnumOperator (ec, expr, predefined);

			return ResolveUserType (ec, expr, predefined);
		}

		protected virtual Expression ResolveEnumOperator (ResolveContext ec, Expression expr, TypeSpec[] predefined)
		{
			TypeSpec underlying_type = EnumSpec.GetUnderlyingType (expr.Type);
			Expression best_expr = ResolvePrimitivePredefinedType (ec, EmptyCast.Create (expr, underlying_type), predefined);
			if (best_expr == null)
				return null;

			Expr = best_expr;
			enum_conversion = Convert.ExplicitNumericConversion (ec, new EmptyExpression (best_expr.Type), underlying_type);
			type = expr.Type;
			return EmptyCast.Create (this, type);
		}

		public override bool ContainsEmitWithAwait ()
		{
			return Expr.ContainsEmitWithAwait ();
		}

		public override Expression CreateExpressionTree (ResolveContext ec)
		{
			return CreateExpressionTree (ec, null);
		}

		Expression CreateExpressionTree (ResolveContext ec, Expression user_op)
		{
			string method_name;
			switch (Oper) {
			case Operator.AddressOf:
				Error_PointerInsideExpressionTree (ec);
				return null;
			case Operator.UnaryNegation:
				if (ec.HasSet (ResolveContext.Options.CheckedScope) && user_op == null && !IsFloat (type))
					method_name = "NegateChecked";
				else
					method_name = "Negate";
				break;
			case Operator.OnesComplement:
			case Operator.LogicalNot:
				method_name = "Not";
				break;
			case Operator.UnaryPlus:
				method_name = "UnaryPlus";
				break;
			default:
				throw new InternalErrorException ("Unknown unary operator " + Oper.ToString ());
			}

			Arguments args = new Arguments (2);
			args.Add (new Argument (Expr.CreateExpressionTree (ec)));
			if (user_op != null)
				args.Add (new Argument (user_op));

			return CreateExpressionFactoryCall (ec, method_name, args);
		}

		public static TypeSpec[][] CreatePredefinedOperatorsTable (BuiltinTypes types)
		{
			var predefined_operators = new TypeSpec[(int) Operator.TOP][];

			//
			// 7.6.1 Unary plus operator
			//
			predefined_operators [(int) Operator.UnaryPlus] = new TypeSpec [] {
				types.Int, types.UInt,
				types.Long, types.ULong,
				types.Float, types.Double,
				types.Decimal
			};

			//
			// 7.6.2 Unary minus operator
			//
			predefined_operators [(int) Operator.UnaryNegation] = new TypeSpec [] {
				types.Int,  types.Long,
				types.Float, types.Double,
				types.Decimal
			};

			//
			// 7.6.3 Logical negation operator
			//
			predefined_operators [(int) Operator.LogicalNot] = new TypeSpec [] {
				types.Bool
			};

			//
			// 7.6.4 Bitwise complement operator
			//
			predefined_operators [(int) Operator.OnesComplement] = new TypeSpec [] {
				types.Int, types.UInt,
				types.Long, types.ULong
			};

			return predefined_operators;
		}

		//
		// Unary numeric promotions
		//
		static Expression DoNumericPromotion (ResolveContext rc, Operator op, Expression expr)
		{
			TypeSpec expr_type = expr.Type;
			if (op == Operator.UnaryPlus || op == Operator.UnaryNegation || op == Operator.OnesComplement) {
				switch (expr_type.BuiltinType) {
				case BuiltinTypeSpec.Type.Byte:
				case BuiltinTypeSpec.Type.SByte:
				case BuiltinTypeSpec.Type.Short:
				case BuiltinTypeSpec.Type.UShort:
				case BuiltinTypeSpec.Type.Char:
					return Convert.ImplicitNumericConversion (expr, rc.BuiltinTypes.Int);
				}
			}

			if (op == Operator.UnaryNegation && expr_type.BuiltinType == BuiltinTypeSpec.Type.UInt)
				return Convert.ImplicitNumericConversion (expr, rc.BuiltinTypes.Long);

			return expr;
		}

		protected override Expression DoResolve (ResolveContext ec)
		{
			if (Oper == Operator.AddressOf) {
				return ResolveAddressOf (ec);
			}

			Expr = Expr.Resolve (ec);
			if (Expr == null)
				return null;

			if (Expr.Type.BuiltinType == BuiltinTypeSpec.Type.Dynamic) {
				Arguments args = new Arguments (1);
				args.Add (new Argument (Expr));
				return new DynamicUnaryConversion (GetOperatorExpressionTypeName (), args, loc).Resolve (ec);
			}

			if (Expr.Type.IsNullableType)
				return new Nullable.LiftedUnaryOperator (Oper, Expr, loc).Resolve (ec);

			//
			// Attempt to use a constant folding operation.
			//
			Constant cexpr = Expr as Constant;
			if (cexpr != null) {
				cexpr = TryReduceConstant (ec, cexpr);
				if (cexpr != null)
					return cexpr;
			}

			Expression expr = ResolveOperator (ec, Expr);
			if (expr == null)
				Error_OperatorCannotBeApplied (ec, loc, OperName (Oper), Expr.Type);
			
			//
			// Reduce unary operator on predefined types
			//
			if (expr == this && Oper == Operator.UnaryPlus)
				return Expr;

			return expr;
		}

		public override Expression DoResolveLValue (ResolveContext ec, Expression right)
		{
			return null;
		}

		public override void Emit (EmitContext ec)
		{
			EmitOperator (ec, type);
		}

		protected void EmitOperator (EmitContext ec, TypeSpec type)
		{
			switch (Oper) {
			case Operator.UnaryPlus:
				Expr.Emit (ec);
				break;
				
			case Operator.UnaryNegation:
				if (ec.HasSet (EmitContext.Options.CheckedScope) && !IsFloat (type)) {
					if (ec.HasSet (BuilderContext.Options.AsyncBody) && Expr.ContainsEmitWithAwait ())
						Expr = Expr.EmitToField (ec);

					ec.EmitInt (0);
					if (type.BuiltinType == BuiltinTypeSpec.Type.Long)
						ec.Emit (OpCodes.Conv_U8);
					Expr.Emit (ec);
					ec.Emit (OpCodes.Sub_Ovf);
				} else {
					Expr.Emit (ec);
					ec.Emit (OpCodes.Neg);
				}
				
				break;
				
			case Operator.LogicalNot:
				Expr.Emit (ec);
				ec.EmitInt (0);
				ec.Emit (OpCodes.Ceq);
				break;
				
			case Operator.OnesComplement:
				Expr.Emit (ec);
				ec.Emit (OpCodes.Not);
				break;
				
			case Operator.AddressOf:
				((IMemoryLocation)Expr).AddressOf (ec, AddressOp.LoadStore);
				break;
				
			default:
				throw new Exception ("This should not happen: Operator = "
						     + Oper.ToString ());
			}

			//
			// Same trick as in Binary expression
			//
			if (enum_conversion != null)
				enum_conversion.Emit (ec);
		}

		public override void EmitBranchable (EmitContext ec, Label target, bool on_true)
		{
			if (Oper == Operator.LogicalNot)
				Expr.EmitBranchable (ec, target, !on_true);
			else
				base.EmitBranchable (ec, target, on_true);
		}

		public override void EmitSideEffect (EmitContext ec)
		{
			Expr.EmitSideEffect (ec);
		}

		//
		// Converts operator to System.Linq.Expressions.ExpressionType enum name
		//
		string GetOperatorExpressionTypeName ()
		{
			switch (Oper) {
			case Operator.OnesComplement:
				return "OnesComplement";
			case Operator.LogicalNot:
				return "Not";
			case Operator.UnaryNegation:
				return "Negate";
			case Operator.UnaryPlus:
				return "UnaryPlus";
			default:
				throw new NotImplementedException ("Unknown express type operator " + Oper.ToString ());
			}
		}

		static bool IsFloat (TypeSpec t)
		{
			return t.BuiltinType == BuiltinTypeSpec.Type.Double || t.BuiltinType == BuiltinTypeSpec.Type.Float;
		}

		//
		// Returns a stringified representation of the Operator
		//
		public static string OperName (Operator oper)
		{
			switch (oper) {
			case Operator.UnaryPlus:
				return "+";
			case Operator.UnaryNegation:
				return "-";
			case Operator.LogicalNot:
				return "!";
			case Operator.OnesComplement:
				return "~";
			case Operator.AddressOf:
				return "&";
			}

			throw new NotImplementedException (oper.ToString ());
		}

		public override SLE.Expression MakeExpression (BuilderContext ctx)
		{
			var expr = Expr.MakeExpression (ctx);
			bool is_checked = ctx.HasSet (BuilderContext.Options.CheckedScope);

			switch (Oper) {
			case Operator.UnaryNegation:
				return is_checked ? SLE.Expression.NegateChecked (expr) : SLE.Expression.Negate (expr);
			case Operator.LogicalNot:
				return SLE.Expression.Not (expr);
#if NET_4_0 || MONODROID
			case Operator.OnesComplement:
				return SLE.Expression.OnesComplement (expr);
#endif
			default:
				throw new NotImplementedException (Oper.ToString ());
			}
		}

		Expression ResolveAddressOf (ResolveContext ec)
		{
			if (!ec.IsUnsafe)
				UnsafeError (ec, loc);

			Expr = Expr.DoResolveLValue (ec, EmptyExpression.UnaryAddress);
			if (Expr == null || Expr.eclass != ExprClass.Variable) {
				ec.Report.Error (211, loc, "Cannot take the address of the given expression");
				return null;
			}

			if (!TypeManager.VerifyUnmanaged (ec.Module, Expr.Type, loc)) {
				return null;
			}

			IVariableReference vr = Expr as IVariableReference;
			bool is_fixed;
			if (vr != null) {
				is_fixed = vr.IsFixed;
				vr.SetHasAddressTaken ();

				if (vr.IsHoisted) {
					AnonymousMethodExpression.Error_AddressOfCapturedVar (ec, vr, loc);
				}
			} else {
				IFixedExpression fe = Expr as IFixedExpression;
				is_fixed = fe != null && fe.IsFixed;
			}

			if (!is_fixed && !ec.HasSet (ResolveContext.Options.FixedInitializerScope)) {
				ec.Report.Error (212, loc, "You can only take the address of unfixed expression inside of a fixed statement initializer");
			}

			type = PointerContainer.MakeType (ec.Module, Expr.Type);
			eclass = ExprClass.Value;
			return this;
		}

		Expression ResolvePrimitivePredefinedType (ResolveContext rc, Expression expr, TypeSpec[] predefined)
		{
			expr = DoNumericPromotion (rc, Oper, expr);
			TypeSpec expr_type = expr.Type;
			foreach (TypeSpec t in predefined) {
				if (t == expr_type)
					return expr;
			}
			return null;
		}

		//
		// Perform user-operator overload resolution
		//
		protected virtual Expression ResolveUserOperator (ResolveContext ec, Expression expr)
		{
			CSharp.Operator.OpType op_type;
			switch (Oper) {
			case Operator.LogicalNot:
				op_type = CSharp.Operator.OpType.LogicalNot; break;
			case Operator.OnesComplement:
				op_type = CSharp.Operator.OpType.OnesComplement; break;
			case Operator.UnaryNegation:
				op_type = CSharp.Operator.OpType.UnaryNegation; break;
			case Operator.UnaryPlus:
				op_type = CSharp.Operator.OpType.UnaryPlus; break;
			default:
				throw new InternalErrorException (Oper.ToString ());
			}

			var methods = MemberCache.GetUserOperator (expr.Type, op_type, false);
			if (methods == null)
				return null;

			Arguments args = new Arguments (1);
			args.Add (new Argument (expr));

			var res = new OverloadResolver (methods, OverloadResolver.Restrictions.BaseMembersIncluded | OverloadResolver.Restrictions.NoBaseMembers, loc);
			var oper = res.ResolveOperator (ec, ref args);

			if (oper == null)
				return null;

			Expr = args [0].Expr;
			return new UserOperatorCall (oper, args, CreateExpressionTree, expr.Location);
		}

		//
		// Unary user type overload resolution
		//
		Expression ResolveUserType (ResolveContext ec, Expression expr, TypeSpec[] predefined)
		{
			Expression best_expr = ResolveUserOperator (ec, expr);
			if (best_expr != null)
				return best_expr;

			foreach (TypeSpec t in predefined) {
				Expression oper_expr = Convert.ImplicitUserConversion (ec, expr, t, expr.Location);
				if (oper_expr == null)
					continue;

				if (oper_expr == ErrorExpression.Instance)
					return oper_expr;

				//
				// decimal type is predefined but has user-operators
				//
				if (oper_expr.Type.BuiltinType == BuiltinTypeSpec.Type.Decimal)
					oper_expr = ResolveUserType (ec, oper_expr, predefined);
				else
					oper_expr = ResolvePrimitivePredefinedType (ec, oper_expr, predefined);

				if (oper_expr == null)
					continue;

				if (best_expr == null) {
					best_expr = oper_expr;
					continue;
				}

				int result = OverloadResolver.BetterTypeConversion (ec, best_expr.Type, t);
				if (result == 0) {
					if ((oper_expr is UserOperatorCall || oper_expr is UserCast) && (best_expr is UserOperatorCall || best_expr is UserCast)) {
						ec.Report.Error (35, loc, "Operator `{0}' is ambiguous on an operand of type `{1}'",
							OperName (Oper), expr.Type.GetSignatureForError ());
					} else {
						Error_OperatorCannotBeApplied (ec, loc, OperName (Oper), expr.Type);
					}

					break;
				}

				if (result == 2)
					best_expr = oper_expr;
			}
			
			if (best_expr == null)
				return null;
			
			//
			// HACK: Decimal user-operator is included in standard operators
			//
			if (best_expr.Type.BuiltinType == BuiltinTypeSpec.Type.Decimal)
				return best_expr;

			Expr = best_expr;
			type = best_expr.Type;
			return this;			
		}

		protected override void CloneTo (CloneContext clonectx, Expression t)
		{
			Unary target = (Unary) t;

			target.Expr = Expr.Clone (clonectx);
		}
		
		public override object Accept (StructuralVisitor visitor)
		{
			return visitor.Visit (this);
		}

	}

	//
	// Unary operators are turned into Indirection expressions
	// after semantic analysis (this is so we can take the address
	// of an indirection).
	//
	public class Indirection : Expression, IMemoryLocation, IAssignMethod, IFixedExpression {
		Expression expr;
		LocalTemporary temporary;
		bool prepared;
		
		public Indirection (Expression expr, Location l)
		{
			this.expr = expr;
			loc = l;
		}

		public Expression Expr {
			get {
				return expr;
			}
		}

		public bool IsFixed {
			get { return true; }
		}

		protected override void CloneTo (CloneContext clonectx, Expression t)
		{
			Indirection target = (Indirection) t;
			target.expr = expr.Clone (clonectx);
		}

		public override bool ContainsEmitWithAwait ()
		{
			throw new NotImplementedException ();
		}

		public override Expression CreateExpressionTree (ResolveContext ec)
		{
			Error_PointerInsideExpressionTree (ec);
			return null;
		}
		
		public override void Emit (EmitContext ec)
		{
			if (!prepared)
				expr.Emit (ec);
			
			ec.EmitLoadFromPtr (Type);
		}

		public void Emit (EmitContext ec, bool leave_copy)
		{
			Emit (ec);
			if (leave_copy) {
				ec.Emit (OpCodes.Dup);
				temporary = new LocalTemporary (expr.Type);
				temporary.Store (ec);
			}
		}
		
		public void EmitAssign (EmitContext ec, Expression source, bool leave_copy, bool isCompound)
		{
			prepared = isCompound;
			
			expr.Emit (ec);

			if (isCompound)
				ec.Emit (OpCodes.Dup);
			
			source.Emit (ec);
			if (leave_copy) {
				ec.Emit (OpCodes.Dup);
				temporary = new LocalTemporary (source.Type);
				temporary.Store (ec);
			}
			
			ec.EmitStoreFromPtr (type);
			
			if (temporary != null) {
				temporary.Emit (ec);
				temporary.Release (ec);
			}
		}
		
		public void AddressOf (EmitContext ec, AddressOp Mode)
		{
			expr.Emit (ec);
		}

		public override Expression DoResolveLValue (ResolveContext ec, Expression right_side)
		{
			return DoResolve (ec);
		}

		protected override Expression DoResolve (ResolveContext ec)
		{
			expr = expr.Resolve (ec);
			if (expr == null)
				return null;

			if (!ec.IsUnsafe)
				UnsafeError (ec, loc);

			var pc = expr.Type as PointerContainer;

			if (pc == null) {
				ec.Report.Error (193, loc, "The * or -> operator must be applied to a pointer");
				return null;
			}

			type = pc.Element;

			if (type.Kind == MemberKind.Void) {
				Error_VoidPointerOperation (ec);
				return null;
			}

			eclass = ExprClass.Variable;
			return this;
		}

		public override object Accept (StructuralVisitor visitor)
		{
			return visitor.Visit (this);
		}
	}
	
	/// <summary>
	///   Unary Mutator expressions (pre and post ++ and --)
	/// </summary>
	///
	/// <remarks>
	///   UnaryMutator implements ++ and -- expressions.   It derives from
	///   ExpressionStatement becuase the pre/post increment/decrement
	///   operators can be used in a statement context.
	///
	/// FIXME: Idea, we could split this up in two classes, one simpler
	/// for the common case, and one with the extra fields for more complex
	/// classes (indexers require temporary access;  overloaded require method)
	///
	/// </remarks>
	public class UnaryMutator : ExpressionStatement
	{
		class DynamicPostMutator : Expression, IAssignMethod
		{
			LocalTemporary temp;
			Expression expr;

			public DynamicPostMutator (Expression expr)
			{
				this.expr = expr;
				this.type = expr.Type;
				this.loc = expr.Location;
			}

			public override Expression CreateExpressionTree (ResolveContext ec)
			{
				throw new NotImplementedException ("ET");
			}

			protected override Expression DoResolve (ResolveContext rc)
			{
				eclass = expr.eclass;
				return this;
			}

			public override Expression DoResolveLValue (ResolveContext ec, Expression right_side)
			{
				expr.DoResolveLValue (ec, right_side);
				return DoResolve (ec);
			}

			public override void Emit (EmitContext ec)
			{
				temp.Emit (ec);
			}

			public void Emit (EmitContext ec, bool leave_copy)
			{
				throw new NotImplementedException ();
			}

			//
			// Emits target assignment using unmodified source value
			//
			public void EmitAssign (EmitContext ec, Expression source, bool leave_copy, bool isCompound)
			{
				//
				// Allocate temporary variable to keep original value before it's modified
				//
				temp = new LocalTemporary (type);
				expr.Emit (ec);
				temp.Store (ec);

				((IAssignMethod) expr).EmitAssign (ec, source, false, isCompound);

				if (leave_copy)
					Emit (ec);

				temp.Release (ec);
				temp = null;
			}
		}

		[Flags]
		public enum Mode : byte {
			IsIncrement    = 0,
			IsDecrement    = 1,
			IsPre          = 0,
			IsPost         = 2,
			
			PreIncrement   = 0,
			PreDecrement   = IsDecrement,
			PostIncrement  = IsPost,
			PostDecrement  = IsPost | IsDecrement
		}

		Mode mode;
		bool is_expr, recurse;

		protected Expression expr;

		// Holds the real operation
		Expression operation;
		
		public UnaryMutator (Mode m, Expression e, Location loc)
		{
			mode = m;
			this.loc = loc;
			expr = e;
		}

		public Mode UnaryMutatorMode {
			get {
				return mode;
			}
		}
		
		public Expression Expr {
			get {
				return expr;
			}
		}

		public override bool ContainsEmitWithAwait ()
		{
			return expr.ContainsEmitWithAwait ();
		}

		public override Expression CreateExpressionTree (ResolveContext ec)
		{
			return new SimpleAssign (this, this).CreateExpressionTree (ec);
		}

		public static TypeSpec[] CreatePredefinedOperatorsTable (BuiltinTypes types)
		{
			//
			// Predefined ++ and -- operators exist for the following types: 
			// sbyte, byte, short, ushort, int, uint, long, ulong, char, float, double, decimal
			//
			return new TypeSpec[] {
				types.Int,
				types.Long,

				types.SByte,
				types.Byte,
				types.Short,
				types.UInt,
				types.ULong,
				types.Char,
				types.Float,
				types.Double,
				types.Decimal
			};
		}

		protected override Expression DoResolve (ResolveContext ec)
		{
			expr = expr.Resolve (ec);
			
			if (expr == null)
				return null;

			if (expr.Type.BuiltinType == BuiltinTypeSpec.Type.Dynamic) {
				//
				// Handle postfix unary operators using local
				// temporary variable
				//
				if ((mode & Mode.IsPost) != 0)
					expr = new DynamicPostMutator (expr);

				Arguments args = new Arguments (1);
				args.Add (new Argument (expr));
				return new SimpleAssign (expr, new DynamicUnaryConversion (GetOperatorExpressionTypeName (), args, loc)).Resolve (ec);
			}

			if (expr.Type.IsNullableType)
				return new Nullable.LiftedUnaryMutator (mode, expr, loc).Resolve (ec);

			return DoResolveOperation (ec);
		}

		protected Expression DoResolveOperation (ResolveContext ec)
		{
			eclass = ExprClass.Value;
			type = expr.Type;

			if (expr is RuntimeValueExpression) {
				operation = expr;
			} else {
				// Use itself at the top of the stack
				operation = new EmptyExpression (type);
			}

			//
			// The operand of the prefix/postfix increment decrement operators
			// should be an expression that is classified as a variable,
			// a property access or an indexer access
			//
			// TODO: Move to parser, expr is ATypeNameExpression
			if (expr.eclass == ExprClass.Variable || expr.eclass == ExprClass.IndexerAccess || expr.eclass == ExprClass.PropertyAccess) {
				expr = expr.ResolveLValue (ec, expr);
			} else {
				ec.Report.Error (1059, loc, "The operand of an increment or decrement operator must be a variable, property or indexer");
			}

			//
			// Step 1: Try to find a user operator, it has priority over predefined ones
			//
			var user_op = IsDecrement ? Operator.OpType.Decrement : Operator.OpType.Increment;
			var methods = MemberCache.GetUserOperator (type, user_op, false);

			if (methods != null) {
				Arguments args = new Arguments (1);
				args.Add (new Argument (expr));

				var res = new OverloadResolver (methods, OverloadResolver.Restrictions.BaseMembersIncluded | OverloadResolver.Restrictions.NoBaseMembers, loc);
				var method = res.ResolveOperator (ec, ref args);
				if (method == null)
					return null;

				args[0].Expr = operation;
				operation = new UserOperatorCall (method, args, null, loc);
				operation = Convert.ImplicitConversionRequired (ec, operation, type, loc);
				return this;
			}

			//
			// Step 2: Try predefined types
			//

			Expression source = null;
			bool primitive_type;

			//
			// Predefined without user conversion first for speed-up
			//
			// Predefined ++ and -- operators exist for the following types: 
			// sbyte, byte, short, ushort, int, uint, long, ulong, char, float, double, decimal
			//
			switch (type.BuiltinType) {
			case BuiltinTypeSpec.Type.Byte:
			case BuiltinTypeSpec.Type.SByte:
			case BuiltinTypeSpec.Type.Short:
			case BuiltinTypeSpec.Type.UShort:
			case BuiltinTypeSpec.Type.Int:
			case BuiltinTypeSpec.Type.UInt:
			case BuiltinTypeSpec.Type.Long:
			case BuiltinTypeSpec.Type.ULong:
			case BuiltinTypeSpec.Type.Char:
			case BuiltinTypeSpec.Type.Float:
			case BuiltinTypeSpec.Type.Double:
			case BuiltinTypeSpec.Type.Decimal:
				source = operation;
				primitive_type = true;
				break;
			default:
				primitive_type = false;

				// ++/-- on pointer variables of all types except void*
				if (type.IsPointer) {
					if (((PointerContainer) type).Element.Kind == MemberKind.Void) {
						Error_VoidPointerOperation (ec);
						return null;
					}

					source = operation;
				} else {
					foreach (var t in ec.BuiltinTypes.OperatorsUnaryMutator) {
						source = Convert.ImplicitUserConversion (ec, operation, t, loc);

						// LAMESPEC: It should error on ambiguous operators but that would make us incompatible
						if (source != null) {
							break;
						}
					}
				}

				// ++/-- on enum types
				if (source == null && type.IsEnum)
					source = operation;

				if (source == null) {
					expr.Error_OperatorCannotBeApplied (ec, loc, Operator.GetName (user_op), type);
					return null;
				}

				break;
			}

			var one = new IntConstant (ec.BuiltinTypes, 1, loc);
			var op = IsDecrement ? Binary.Operator.Subtraction : Binary.Operator.Addition;
			operation = new Binary (op, source, one, loc);
			operation = operation.Resolve (ec);
			if (operation == null)
				throw new NotImplementedException ("should not be reached");

			if (operation.Type != type) {
				if (primitive_type)
					operation = Convert.ExplicitNumericConversion (ec, operation, type);
				else
					operation = Convert.ImplicitConversionRequired (ec, operation, type, loc);
			}

			return this;
		}

		void EmitCode (EmitContext ec, bool is_expr)
		{
			recurse = true;
			this.is_expr = is_expr;
			((IAssignMethod) expr).EmitAssign (ec, this, is_expr && (mode == Mode.PreIncrement || mode == Mode.PreDecrement), true);
		}

		public override void Emit (EmitContext ec)
		{
			//
			// We use recurse to allow ourselfs to be the source
			// of an assignment. This little hack prevents us from
			// having to allocate another expression
			//
			if (recurse) {
				((IAssignMethod) expr).Emit (ec, is_expr && (mode == Mode.PostIncrement || mode == Mode.PostDecrement));

				EmitOperation (ec);

				recurse = false;
				return;
			}

			EmitCode (ec, true);
		}

		protected virtual void EmitOperation (EmitContext ec)
		{
			operation.Emit (ec);
		}

		public override void EmitStatement (EmitContext ec)
		{
			EmitCode (ec, false);
		}

		//
		// Converts operator to System.Linq.Expressions.ExpressionType enum name
		//
		string GetOperatorExpressionTypeName ()
		{
			return IsDecrement ? "Decrement" : "Increment";
		}

		bool IsDecrement {
			get { return (mode & Mode.IsDecrement) != 0; }
		}


#if NET_4_0 || MONODROID
		public override SLE.Expression MakeExpression (BuilderContext ctx)
		{
			var target = ((RuntimeValueExpression) expr).MetaObject.Expression;
			var source = SLE.Expression.Convert (operation.MakeExpression (ctx), target.Type);
			return SLE.Expression.Assign (target, source);
		}
#endif

		protected override void CloneTo (CloneContext clonectx, Expression t)
		{
			UnaryMutator target = (UnaryMutator) t;

			target.expr = expr.Clone (clonectx);
		}

		public override object Accept (StructuralVisitor visitor)
		{
			return visitor.Visit (this);
		}

	}

	//
	// Base class for the `is' and `as' operators
	//
	public abstract class Probe : Expression
	{
		public Expression ProbeType;
		protected Expression expr;
		protected TypeSpec probe_type_expr;
		
		public Probe (Expression expr, Expression probe_type, Location l)
		{
			ProbeType = probe_type;
			loc = l;
			this.expr = expr;
		}

		public Expression Expr {
			get {
				return expr;
			}
		}

		public override bool ContainsEmitWithAwait ()
		{
			return expr.ContainsEmitWithAwait ();
		}

		protected override Expression DoResolve (ResolveContext ec)
		{
			probe_type_expr = ProbeType.ResolveAsType (ec);
			if (probe_type_expr == null)
				return null;

			expr = expr.Resolve (ec);
			if (expr == null)
				return null;

			if (probe_type_expr.IsStatic) {
				ec.Report.Error (-244, loc, "The `{0}' operator cannot be applied to an operand of a static type",
					OperatorName);
			}
			
			if (expr.Type.IsPointer || probe_type_expr.IsPointer) {
				ec.Report.Error (244, loc, "The `{0}' operator cannot be applied to an operand of pointer type",
					OperatorName);
				return null;
			}

			if (expr.Type == InternalType.AnonymousMethod) {
				ec.Report.Error (837, loc, "The `{0}' operator cannot be applied to a lambda expression or anonymous method",
					OperatorName);
				return null;
			}

			return this;
		}

		protected abstract string OperatorName { get; }

		protected override void CloneTo (CloneContext clonectx, Expression t)
		{
			Probe target = (Probe) t;

			target.expr = expr.Clone (clonectx);
			target.ProbeType = ProbeType.Clone (clonectx);
		}

	}

	/// <summary>
	///   Implementation of the `is' operator.
	/// </summary>
	public class Is : Probe
	{
		Nullable.Unwrap expr_unwrap;

		public Is (Expression expr, Expression probe_type, Location l)
			: base (expr, probe_type, l)
		{
		}

		protected override string OperatorName {
			get { return "is"; }
		}

		public override Expression CreateExpressionTree (ResolveContext ec)
		{
			Arguments args = Arguments.CreateForExpressionTree (ec, null,
				expr.CreateExpressionTree (ec),
				new TypeOf (probe_type_expr, loc));

			return CreateExpressionFactoryCall (ec, "TypeIs", args);
		}
		
		public override void Emit (EmitContext ec)
		{
			if (expr_unwrap != null) {
				expr_unwrap.EmitCheck (ec);
				return;
			}

			expr.Emit (ec);

			// Only to make verifier happy
			if (probe_type_expr.IsGenericParameter && TypeSpec.IsValueType (expr.Type))
				ec.Emit (OpCodes.Box, expr.Type);

			ec.Emit (OpCodes.Isinst, probe_type_expr);
			ec.EmitNull ();
			ec.Emit (OpCodes.Cgt_Un);
		}

		public override void EmitBranchable (EmitContext ec, Label target, bool on_true)
		{
			if (expr_unwrap != null) {
				expr_unwrap.EmitCheck (ec);
			} else {
				expr.Emit (ec);
				ec.Emit (OpCodes.Isinst, probe_type_expr);
			}			
			ec.Emit (on_true ? OpCodes.Brtrue : OpCodes.Brfalse, target);
		}
		
		Expression CreateConstantResult (ResolveContext ec, bool result)
		{
			if (result)
				ec.Report.Warning (183, 1, loc, "The given expression is always of the provided (`{0}') type",
					TypeManager.CSharpName (probe_type_expr));
			else
				ec.Report.Warning (184, 1, loc, "The given expression is never of the provided (`{0}') type",
					TypeManager.CSharpName (probe_type_expr));

			return ReducedExpression.Create (new BoolConstant (ec.BuiltinTypes, result, loc), this);
		}

		protected override Expression DoResolve (ResolveContext ec)
		{
			if (base.DoResolve (ec) == null)
				return null;

			TypeSpec d = expr.Type;
			bool d_is_nullable = false;

			//
			// If E is a method group or the null literal, or if the type of E is a reference
			// type or a nullable type and the value of E is null, the result is false
			//
			if (expr.IsNull || expr.eclass == ExprClass.MethodGroup)
				return CreateConstantResult (ec, false);

			if (d.IsNullableType) {
				var ut = Nullable.NullableInfo.GetUnderlyingType (d);
				if (!ut.IsGenericParameter) {
					d = ut;
					d_is_nullable = true;
				}
			}

			type = ec.BuiltinTypes.Bool;
			eclass = ExprClass.Value;
			TypeSpec t = probe_type_expr;
			bool t_is_nullable = false;
			if (t.IsNullableType) {
				var ut = Nullable.NullableInfo.GetUnderlyingType (t);
				if (!ut.IsGenericParameter) {
					t = ut;
					t_is_nullable = true;
				}
			}

			if (t.IsStruct) {
				if (d == t) {
					//
					// D and T are the same value types but D can be null
					//
					if (d_is_nullable && !t_is_nullable) {
						expr_unwrap = Nullable.Unwrap.Create (expr, false);
						return this;
					}
					
					//
					// The result is true if D and T are the same value types
					//
					return CreateConstantResult (ec, true);
				}

				var tp = d as TypeParameterSpec;
				if (tp != null)
					return ResolveGenericParameter (ec, t, tp);

				//
				// An unboxing conversion exists
				//
				if (Convert.ExplicitReferenceConversionExists (d, t))
					return this;
			} else {
				if (TypeManager.IsGenericParameter (t))
					return ResolveGenericParameter (ec, d, (TypeParameterSpec) t);

				if (t.BuiltinType == BuiltinTypeSpec.Type.Dynamic) {
					ec.Report.Warning (1981, 3, loc,
						"Using `{0}' to test compatibility with `{1}' is identical to testing compatibility with `object'",
						OperatorName, t.GetSignatureForError ());
				}

				if (TypeManager.IsGenericParameter (d))
					return ResolveGenericParameter (ec, t, (TypeParameterSpec) d);

				if (TypeSpec.IsValueType (d)) {
					if (Convert.ImplicitBoxingConversion (null, d, t) != null) {
						if (d_is_nullable && !t_is_nullable) {
							expr_unwrap = Nullable.Unwrap.Create (expr, false);
							return this;
						}

						return CreateConstantResult (ec, true);
					}
				} else {
					if (Convert.ImplicitReferenceConversionExists (d, t)) {
						//
						// Do not optimize for imported type
						//
						if (d.MemberDefinition.IsImported && d.BuiltinType != BuiltinTypeSpec.Type.None)
							return this;
						
						//
						// Turn is check into simple null check for implicitly convertible reference types
						//
						return ReducedExpression.Create (
							new Binary (Binary.Operator.Inequality, expr, new NullLiteral (loc), loc).Resolve (ec),
							this).Resolve (ec);
					}

					if (Convert.ExplicitReferenceConversionExists (d, t)) {
						return this;
					}
				}
			}

			return CreateConstantResult (ec, false);
		}

		Expression ResolveGenericParameter (ResolveContext ec, TypeSpec d, TypeParameterSpec t)
		{
			if (t.IsReferenceType) {
				if (d.IsStruct)
					return CreateConstantResult (ec, false);
			}

			if (TypeManager.IsGenericParameter (expr.Type)) {
				if (expr.Type == d && TypeSpec.IsValueType (t))
					return CreateConstantResult (ec, true);

				expr = new BoxedCast (expr, d);
			}

			return this;
		}
		
		public override object Accept (StructuralVisitor visitor)
		{
			return visitor.Visit (this);
		}
	}

	/// <summary>
	///   Implementation of the `as' operator.
	/// </summary>
	public class As : Probe {
		Expression resolved_type;
		
		public As (Expression expr, Expression probe_type, Location l)
			: base (expr, probe_type, l)
		{
		}

		protected override string OperatorName {
			get { return "as"; }
		}

		public override Expression CreateExpressionTree (ResolveContext ec)
		{
			Arguments args = Arguments.CreateForExpressionTree (ec, null,
				expr.CreateExpressionTree (ec),
				new TypeOf (probe_type_expr, loc));

			return CreateExpressionFactoryCall (ec, "TypeAs", args);
		}

		public override void Emit (EmitContext ec)
		{
			expr.Emit (ec);

			ec.Emit (OpCodes.Isinst, type);

			if (TypeManager.IsGenericParameter (type) || type.IsNullableType)
				ec.Emit (OpCodes.Unbox_Any, type);
		}

		protected override Expression DoResolve (ResolveContext ec)
		{
			if (resolved_type == null) {
				resolved_type = base.DoResolve (ec);

				if (resolved_type == null)
					return null;
			}

			type = probe_type_expr;
			eclass = ExprClass.Value;
			TypeSpec etype = expr.Type;

			if (!TypeSpec.IsReferenceType (type) && !type.IsNullableType) {
				if (TypeManager.IsGenericParameter (type)) {
					ec.Report.Error (413, loc,
						"The `as' operator cannot be used with a non-reference type parameter `{0}'. Consider adding `class' or a reference type constraint",
						probe_type_expr.GetSignatureForError ());
				} else {
					ec.Report.Error (77, loc,
						"The `as' operator cannot be used with a non-nullable value type `{0}'",
						TypeManager.CSharpName (type));
				}
				return null;
			}

			if (expr.IsNull && type.IsNullableType) {
				return Nullable.LiftedNull.CreateFromExpression (ec, this);
			}

			// If the compile-time type of E is dynamic, unlike the cast operator the as operator is not dynamically bound
			if (etype.BuiltinType == BuiltinTypeSpec.Type.Dynamic) {
				return this;
			}
			
			Expression e = Convert.ImplicitConversionStandard (ec, expr, type, loc);
			if (e != null) {
				e = EmptyCast.Create (e, type);
				return ReducedExpression.Create (e, this).Resolve (ec);
			}

			if (Convert.ExplicitReferenceConversionExists (etype, type)){
				if (TypeManager.IsGenericParameter (etype))
					expr = new BoxedCast (expr, etype);

				return this;
			}

			if (InflatedTypeSpec.ContainsTypeParameter (etype) || InflatedTypeSpec.ContainsTypeParameter (type)) {
				expr = new BoxedCast (expr, etype);
				return this;
			}

			ec.Report.Error (39, loc, "Cannot convert type `{0}' to `{1}' via a built-in conversion",
				TypeManager.CSharpName (etype), TypeManager.CSharpName (type));

			return null;
		}

		public override object Accept (StructuralVisitor visitor)
		{
			return visitor.Visit (this);
		}
	}
	
	//
	// This represents a typecast in the source language.
	//
	public class Cast : ShimExpression {
		Expression target_type;

		public Cast (Expression cast_type, Expression expr, Location loc)
			: base (expr)
		{
			this.target_type = cast_type;
			this.loc = loc;
		}

		public Expression TargetType {
			get { return target_type; }
		}

		protected override Expression DoResolve (ResolveContext ec)
		{
			expr = expr.Resolve (ec);
			if (expr == null)
				return null;

			type = target_type.ResolveAsType (ec);
			if (type == null)
				return null;

			if (type.IsStatic) {
				ec.Report.Error (716, loc, "Cannot convert to static type `{0}'", TypeManager.CSharpName (type));
				return null;
			}

			if (type.IsPointer && !ec.IsUnsafe) {
				UnsafeError (ec, loc);
			}

			eclass = ExprClass.Value;
			
			Constant c = expr as Constant;
			if (c != null) {
				c = c.TryReduce (ec, type);
				if (c != null)
					return c;
			}

			var res = Convert.ExplicitConversion (ec, expr, type, loc);
			if (res == expr)
				return EmptyCast.Create (res, type);

			return res;
		}
		
		protected override void CloneTo (CloneContext clonectx, Expression t)
		{
			Cast target = (Cast) t;

			target.target_type = target_type.Clone (clonectx);
			target.expr = expr.Clone (clonectx);
		}

		public override object Accept (StructuralVisitor visitor)
		{
			return visitor.Visit (this);
		}
	}

	public class ImplicitCast : ShimExpression
	{
		bool arrayAccess;

		public ImplicitCast (Expression expr, TypeSpec target, bool arrayAccess)
			: base (expr)
		{
			this.loc = expr.Location;
			this.type = target;
			this.arrayAccess = arrayAccess;
		}

		protected override Expression DoResolve (ResolveContext ec)
		{
			expr = expr.Resolve (ec);
			if (expr == null)
				return null;

			if (arrayAccess)
				expr = ConvertExpressionToArrayIndex (ec, expr);
			else
				expr = Convert.ImplicitConversionRequired (ec, expr, type, loc);

			return expr;
		}
	}
	
	//
	// C# 2.0 Default value expression
	//
	public class DefaultValueExpression : Expression
	{
		Expression expr;

		public DefaultValueExpression (Expression expr, Location loc)
		{
			this.expr = expr;
			this.loc = loc;
		}

		public Expression Expr {
			get {
				return this.expr; 
			}
		}

		public override bool IsSideEffectFree {
			get {
				return true;
			}
		}

		public override bool ContainsEmitWithAwait ()
		{
			return false;
		}

		public override Expression CreateExpressionTree (ResolveContext ec)
		{
			Arguments args = new Arguments (2);
			args.Add (new Argument (this));
			args.Add (new Argument (new TypeOf (type, loc)));
			return CreateExpressionFactoryCall (ec, "Constant", args);
		}

		protected override Expression DoResolve (ResolveContext ec)
		{
			type = expr.ResolveAsType (ec);
			if (type == null)
				return null;

			if (type.IsStatic) {
				ec.Report.Error (-244, loc, "The `default value' operator cannot be applied to an operand of a static type");
			}

			if (type.IsPointer)
				return new NullLiteral (Location).ConvertImplicitly (type);

			if (TypeSpec.IsReferenceType (type))
				return new NullConstant (type, loc);

			Constant c = New.Constantify (type, expr.Location);
			if (c != null)
				return c;

			eclass = ExprClass.Variable;
			return this;
		}

		public override void Emit (EmitContext ec)
		{
			LocalTemporary temp_storage = new LocalTemporary(type);

			temp_storage.AddressOf(ec, AddressOp.LoadStore);
			ec.Emit(OpCodes.Initobj, type);
			temp_storage.Emit(ec);
			temp_storage.Release (ec);
		}

#if (NET_4_0 || MONODROID) && !STATIC
		public override SLE.Expression MakeExpression (BuilderContext ctx)
		{
			return SLE.Expression.Default (type.GetMetaInfo ());
		}
#endif

		protected override void CloneTo (CloneContext clonectx, Expression t)
		{
			DefaultValueExpression target = (DefaultValueExpression) t;
			
			target.expr = expr.Clone (clonectx);
		}
		
		public override object Accept (StructuralVisitor visitor)
		{
			return visitor.Visit (this);
		}
	}

	/// <summary>
	///   Binary operators
	/// </summary>
	public class Binary : Expression, IDynamicBinder
	{
		public class PredefinedOperator
		{
			protected readonly TypeSpec left;
			protected readonly TypeSpec right;
			public readonly Operator OperatorsMask;
			public TypeSpec ReturnType;

			public PredefinedOperator (TypeSpec ltype, TypeSpec rtype, Operator op_mask)
				: this (ltype, rtype, op_mask, ltype)
			{
			}

			public PredefinedOperator (TypeSpec type, Operator op_mask, TypeSpec return_type)
				: this (type, type, op_mask, return_type)
			{
			}

			public PredefinedOperator (TypeSpec type, Operator op_mask)
				: this (type, type, op_mask, type)
			{
			}

			public PredefinedOperator (TypeSpec ltype, TypeSpec rtype, Operator op_mask, TypeSpec return_type)
			{
				if ((op_mask & Operator.ValuesOnlyMask) != 0)
					throw new InternalErrorException ("Only masked values can be used");

				this.left = ltype;
				this.right = rtype;
				this.OperatorsMask = op_mask;
				this.ReturnType = return_type;
			}

			public virtual Expression ConvertResult (ResolveContext ec, Binary b)
			{
				b.type = ReturnType;

				b.left = Convert.ImplicitConversion (ec, b.left, left, b.left.Location);
				b.right = Convert.ImplicitConversion (ec, b.right, right, b.right.Location);

				//
				// A user operators does not support multiple user conversions, but decimal type
				// is considered to be predefined type therefore we apply predefined operators rules
				// and then look for decimal user-operator implementation
				//
				if (left.BuiltinType == BuiltinTypeSpec.Type.Decimal)
					return b.ResolveUserOperator (ec, b.left, b.right);

				var c = b.right as Constant;
				if (c != null) {
					if (c.IsDefaultValue && (b.oper == Operator.Addition || b.oper == Operator.Subtraction || (b.oper == Operator.BitwiseOr && !(b is Nullable.LiftedBinaryOperator))))
						return ReducedExpression.Create (b.left, b).Resolve (ec);
					if ((b.oper == Operator.Multiply || b.oper == Operator.Division) && c.IsOneInteger)
						return ReducedExpression.Create (b.left, b).Resolve (ec);
					return b;
				}

				c = b.left as Constant;
				if (c != null) {
					if (c.IsDefaultValue && (b.oper == Operator.Addition || (b.oper == Operator.BitwiseOr && !(b is Nullable.LiftedBinaryOperator))))
						return ReducedExpression.Create (b.right, b).Resolve (ec);
					if (b.oper == Operator.Multiply && c.IsOneInteger)
						return ReducedExpression.Create (b.right, b).Resolve (ec);
					return b;
				}

				return b;
			}

			public bool IsPrimitiveApplicable (TypeSpec ltype, TypeSpec rtype)
			{
				//
				// We are dealing with primitive types only
				//
				return left == ltype && ltype == rtype;
			}

			public virtual bool IsApplicable (ResolveContext ec, Expression lexpr, Expression rexpr)
			{
				// Quick path
				if (left == lexpr.Type && right == rexpr.Type)
					return true;

				return Convert.ImplicitConversionExists (ec, lexpr, left) &&
					Convert.ImplicitConversionExists (ec, rexpr, right);
			}

			public PredefinedOperator ResolveBetterOperator (ResolveContext ec, PredefinedOperator best_operator)
			{
				int result = 0;
				if (left != null && best_operator.left != null) {
					result = OverloadResolver.BetterTypeConversion (ec, best_operator.left, left);
				}

				//
				// When second argument is same as the first one, the result is same
				//
				if (right != null && (left != right || best_operator.left != best_operator.right)) {
					result |= OverloadResolver.BetterTypeConversion (ec, best_operator.right, right);
				}

				if (result == 0 || result > 2)
					return null;

				return result == 1 ? best_operator : this;
			}
		}

		sealed class PredefinedStringOperator : PredefinedOperator
		{
			public PredefinedStringOperator (TypeSpec type, Operator op_mask, TypeSpec retType)
				: base (type, type, op_mask, retType)
			{
			}

			public PredefinedStringOperator (TypeSpec ltype, TypeSpec rtype, Operator op_mask, TypeSpec retType)
				: base (ltype, rtype, op_mask, retType)
			{
			}

			public override Expression ConvertResult (ResolveContext ec, Binary b)
			{
				//
				// Use original expression for nullable arguments
				//
				Nullable.Unwrap unwrap = b.left as Nullable.Unwrap;
				if (unwrap != null)
					b.left = unwrap.Original;

				unwrap = b.right as Nullable.Unwrap;
				if (unwrap != null)
					b.right = unwrap.Original;

				b.left = Convert.ImplicitConversion (ec, b.left, left, b.left.Location);
				b.right = Convert.ImplicitConversion (ec, b.right, right, b.right.Location);

				//
				// Start a new concat expression using converted expression
				//
				return StringConcat.Create (ec, b.left, b.right, b.loc);
			}
		}

		sealed class PredefinedShiftOperator : PredefinedOperator
		{
			public PredefinedShiftOperator (TypeSpec ltype, TypeSpec rtype, Operator op_mask)
				: base (ltype, rtype, op_mask)
			{
			}

			public override Expression ConvertResult (ResolveContext ec, Binary b)
			{
				b.left = Convert.ImplicitConversion (ec, b.left, left, b.left.Location);

				Expression expr_tree_expr = Convert.ImplicitConversion (ec, b.right, right, b.right.Location);

				int right_mask = left.BuiltinType == BuiltinTypeSpec.Type.Int || left.BuiltinType == BuiltinTypeSpec.Type.UInt ? 0x1f : 0x3f;

				//
				// b = b.left >> b.right & (0x1f|0x3f)
				//
				b.right = new Binary (Operator.BitwiseAnd,
					b.right, new IntConstant (ec.BuiltinTypes, right_mask, b.right.Location), b.loc).Resolve (ec);

				//
				// Expression tree representation does not use & mask
				//
				b.right = ReducedExpression.Create (b.right, expr_tree_expr).Resolve (ec);
				b.type = ReturnType;

				//
				// Optimize shift by 0
				//
				var c = b.right as Constant;
				if (c != null && c.IsDefaultValue)
					return ReducedExpression.Create (b.left, b).Resolve (ec);

				return b;
			}
		}

		sealed class PredefinedEqualityOperator : PredefinedOperator
		{
			MethodSpec equal_method, inequal_method;

			public PredefinedEqualityOperator (TypeSpec arg, TypeSpec retType)
				: base (arg, arg, Operator.EqualityMask, retType)
			{
			}

			public override Expression ConvertResult (ResolveContext ec, Binary b)
			{
				b.type = ReturnType;

				b.left = Convert.ImplicitConversion (ec, b.left, left, b.left.Location);
				b.right = Convert.ImplicitConversion (ec, b.right, right, b.right.Location);

				Arguments args = new Arguments (2);
				args.Add (new Argument (b.left));
				args.Add (new Argument (b.right));

				MethodSpec method;
				if (b.oper == Operator.Equality) {
					if (equal_method == null) {
						if (left.BuiltinType == BuiltinTypeSpec.Type.String)
							equal_method = ec.Module.PredefinedMembers.StringEqual.Resolve (b.loc);
						else if (left.BuiltinType == BuiltinTypeSpec.Type.Delegate)
							equal_method = ec.Module.PredefinedMembers.DelegateEqual.Resolve (b.loc);
						else
							throw new NotImplementedException (left.GetSignatureForError ());
					}

					method = equal_method;
				} else {
					if (inequal_method == null) {
						if (left.BuiltinType == BuiltinTypeSpec.Type.String)
							inequal_method = ec.Module.PredefinedMembers.StringInequal.Resolve (b.loc);
						else if (left.BuiltinType == BuiltinTypeSpec.Type.Delegate)
							inequal_method = ec.Module.PredefinedMembers.DelegateInequal.Resolve (b.loc);
						else
							throw new NotImplementedException (left.GetSignatureForError ());
					}

					method = inequal_method;
				}

				return new UserOperatorCall (method, args, b.CreateExpressionTree, b.loc);
			}
		}

		class PredefinedPointerOperator : PredefinedOperator
		{
			public PredefinedPointerOperator (TypeSpec ltype, TypeSpec rtype, Operator op_mask)
				: base (ltype, rtype, op_mask)
			{
			}

			public PredefinedPointerOperator (TypeSpec ltype, TypeSpec rtype, Operator op_mask, TypeSpec retType)
				: base (ltype, rtype, op_mask, retType)
			{
			}

			public PredefinedPointerOperator (TypeSpec type, Operator op_mask, TypeSpec return_type)
				: base (type, op_mask, return_type)
			{
			}

			public override bool IsApplicable (ResolveContext ec, Expression lexpr, Expression rexpr)
			{
				if (left == null) {
					if (!lexpr.Type.IsPointer)
						return false;
				} else {
					if (!Convert.ImplicitConversionExists (ec, lexpr, left))
						return false;
				}

				if (right == null) {
					if (!rexpr.Type.IsPointer)
						return false;
				} else {
					if (!Convert.ImplicitConversionExists (ec, rexpr, right))
						return false;
				}

				return true;
			}

			public override Expression ConvertResult (ResolveContext ec, Binary b)
			{
				if (left != null) {
					b.left = EmptyCast.Create (b.left, left);
				} else if (right != null) {
					b.right = EmptyCast.Create (b.right, right);
				}

				TypeSpec r_type = ReturnType;
				Expression left_arg, right_arg;
				if (r_type == null) {
					if (left == null) {
						left_arg = b.left;
						right_arg = b.right;
						r_type = b.left.Type;
					} else {
						left_arg = b.right;
						right_arg = b.left;
						r_type = b.right.Type;
					}
				} else {
					left_arg = b.left;
					right_arg = b.right;
				}

				return new PointerArithmetic (b.oper, left_arg, right_arg, r_type, b.loc).Resolve (ec);
			}
		}

		[Flags]
		public enum Operator {
			Multiply	= 0 | ArithmeticMask,
			Division	= 1 | ArithmeticMask,
			Modulus		= 2 | ArithmeticMask,
			Addition	= 3 | ArithmeticMask | AdditionMask,
			Subtraction = 4 | ArithmeticMask | SubtractionMask,

			LeftShift	= 5 | ShiftMask,
			RightShift	= 6 | ShiftMask,

			LessThan	= 7 | ComparisonMask | RelationalMask,
			GreaterThan	= 8 | ComparisonMask | RelationalMask,
			LessThanOrEqual		= 9 | ComparisonMask | RelationalMask,
			GreaterThanOrEqual	= 10 | ComparisonMask | RelationalMask,
			Equality	= 11 | ComparisonMask | EqualityMask,
			Inequality	= 12 | ComparisonMask | EqualityMask,

			BitwiseAnd	= 13 | BitwiseMask,
			ExclusiveOr	= 14 | BitwiseMask,
			BitwiseOr	= 15 | BitwiseMask,

			LogicalAnd	= 16 | LogicalMask,
			LogicalOr	= 17 | LogicalMask,

			//
			// Operator masks
			//
			ValuesOnlyMask	= ArithmeticMask - 1,
			ArithmeticMask	= 1 << 5,
			ShiftMask		= 1 << 6,
			ComparisonMask	= 1 << 7,
			EqualityMask	= 1 << 8,
			BitwiseMask		= 1 << 9,
			LogicalMask		= 1 << 10,
			AdditionMask	= 1 << 11,
			SubtractionMask	= 1 << 12,
			RelationalMask	= 1 << 13
		}

		protected enum State
		{
			None = 0,
			Compound = 1 << 1,
			LeftNullLifted = 1 << 2,
			RightNullLifted = 1 << 3
		}

		readonly Operator oper;
		protected Expression left, right;
		protected State state;
		Expression enum_conversion;

		public Binary (Operator oper, Expression left, Expression right, bool isCompound, Location loc)
			: this (oper, left, right, loc)
		{
			if (isCompound)
				state |= State.Compound;
		}

		public Binary (Operator oper, Expression left, Expression right, Location loc)
		{
			this.oper = oper;
			this.left = left;
			this.right = right;
			this.loc = loc;
		}

		#region Properties

		public bool IsCompound {
			get {
				return (state & State.Compound) != 0;
			}
		}

		public Operator Oper {
			get {
				return oper;
			}
		}

		public Expression Left {
			get {
				return this.left;
			}
		}

		public Expression Right {
			get {
				return this.right;
			}
		}

		#endregion

		/// <summary>
		///   Returns a stringified representation of the Operator
		/// </summary>
		string OperName (Operator oper)
		{
			string s;
			switch (oper){
			case Operator.Multiply:
				s = "*";
				break;
			case Operator.Division:
				s = "/";
				break;
			case Operator.Modulus:
				s = "%";
				break;
			case Operator.Addition:
				s = "+";
				break;
			case Operator.Subtraction:
				s = "-";
				break;
			case Operator.LeftShift:
				s = "<<";
				break;
			case Operator.RightShift:
				s = ">>";
				break;
			case Operator.LessThan:
				s = "<";
				break;
			case Operator.GreaterThan:
				s = ">";
				break;
			case Operator.LessThanOrEqual:
				s = "<=";
				break;
			case Operator.GreaterThanOrEqual:
				s = ">=";
				break;
			case Operator.Equality:
				s = "==";
				break;
			case Operator.Inequality:
				s = "!=";
				break;
			case Operator.BitwiseAnd:
				s = "&";
				break;
			case Operator.BitwiseOr:
				s = "|";
				break;
			case Operator.ExclusiveOr:
				s = "^";
				break;
			case Operator.LogicalOr:
				s = "||";
				break;
			case Operator.LogicalAnd:
				s = "&&";
				break;
			default:
				s = oper.ToString ();
				break;
			}

			if (IsCompound)
				return s + "=";

			return s;
		}

		public static void Error_OperatorCannotBeApplied (ResolveContext ec, Expression left, Expression right, Operator oper, Location loc)
		{
			new Binary (oper, left, right, loc).Error_OperatorCannotBeApplied (ec, left, right);
		}

		public static void Error_OperatorCannotBeApplied (ResolveContext ec, Expression left, Expression right, string oper, Location loc)
		{
			if (left.Type == InternalType.ErrorType || right.Type == InternalType.ErrorType)
				return;

			string l, r;
			l = TypeManager.CSharpName (left.Type);
			r = TypeManager.CSharpName (right.Type);

			ec.Report.Error (19, loc, "Operator `{0}' cannot be applied to operands of type `{1}' and `{2}'",
				oper, l, r);
		}
		
		protected void Error_OperatorCannotBeApplied (ResolveContext ec, Expression left, Expression right)
		{
			Error_OperatorCannotBeApplied (ec, left, right, OperName (oper), loc);
		}

		//
		// Converts operator to System.Linq.Expressions.ExpressionType enum name
		//
		string GetOperatorExpressionTypeName ()
		{
			switch (oper) {
			case Operator.Addition:
				return IsCompound ? "AddAssign" : "Add";
			case Operator.BitwiseAnd:
				return IsCompound ? "AndAssign" : "And";
			case Operator.BitwiseOr:
				return IsCompound ? "OrAssign" : "Or";
			case Operator.Division:
				return IsCompound ? "DivideAssign" : "Divide";
			case Operator.ExclusiveOr:
				return IsCompound ? "ExclusiveOrAssign" : "ExclusiveOr";
			case Operator.Equality:
				return "Equal";
			case Operator.GreaterThan:
				return "GreaterThan";
			case Operator.GreaterThanOrEqual:
				return "GreaterThanOrEqual";
			case Operator.Inequality:
				return "NotEqual";
			case Operator.LeftShift:
				return IsCompound ? "LeftShiftAssign" : "LeftShift";
			case Operator.LessThan:
				return "LessThan";
			case Operator.LessThanOrEqual:
				return "LessThanOrEqual";
			case Operator.LogicalAnd:
				return "And";
			case Operator.LogicalOr:
				return "Or";
			case Operator.Modulus:
				return IsCompound ? "ModuloAssign" : "Modulo";
			case Operator.Multiply:
				return IsCompound ? "MultiplyAssign" : "Multiply";
			case Operator.RightShift:
				return IsCompound ? "RightShiftAssign" : "RightShift";
			case Operator.Subtraction:
				return IsCompound ? "SubtractAssign" : "Subtract";
			default:
				throw new NotImplementedException ("Unknown expression type operator " + oper.ToString ());
			}
		}

		static CSharp.Operator.OpType ConvertBinaryToUserOperator (Operator op)
		{
			switch (op) {
			case Operator.Addition:
				return CSharp.Operator.OpType.Addition;
			case Operator.BitwiseAnd:
			case Operator.LogicalAnd:
				return CSharp.Operator.OpType.BitwiseAnd;
			case Operator.BitwiseOr:
			case Operator.LogicalOr:
				return CSharp.Operator.OpType.BitwiseOr;
			case Operator.Division:
				return CSharp.Operator.OpType.Division;
			case Operator.Equality:
				return CSharp.Operator.OpType.Equality;
			case Operator.ExclusiveOr:
				return CSharp.Operator.OpType.ExclusiveOr;
			case Operator.GreaterThan:
				return CSharp.Operator.OpType.GreaterThan;
			case Operator.GreaterThanOrEqual:
				return CSharp.Operator.OpType.GreaterThanOrEqual;
			case Operator.Inequality:
				return CSharp.Operator.OpType.Inequality;
			case Operator.LeftShift:
				return CSharp.Operator.OpType.LeftShift;
			case Operator.LessThan:
				return CSharp.Operator.OpType.LessThan;
			case Operator.LessThanOrEqual:
				return CSharp.Operator.OpType.LessThanOrEqual;
			case Operator.Modulus:
				return CSharp.Operator.OpType.Modulus;
			case Operator.Multiply:
				return CSharp.Operator.OpType.Multiply;
			case Operator.RightShift:
				return CSharp.Operator.OpType.RightShift;
			case Operator.Subtraction:
				return CSharp.Operator.OpType.Subtraction;
			default:
				throw new InternalErrorException (op.ToString ());
			}
		}

		public override bool ContainsEmitWithAwait ()
		{
			return left.ContainsEmitWithAwait () || right.ContainsEmitWithAwait ();
		}

		public static void EmitOperatorOpcode (EmitContext ec, Operator oper, TypeSpec l)
		{
			OpCode opcode;

			switch (oper){
			case Operator.Multiply:
				if (ec.HasSet (EmitContext.Options.CheckedScope)) {
					if (l.BuiltinType == BuiltinTypeSpec.Type.Int || l.BuiltinType == BuiltinTypeSpec.Type.Long)
						opcode = OpCodes.Mul_Ovf;
					else if (!IsFloat (l))
						opcode = OpCodes.Mul_Ovf_Un;
					else
						opcode = OpCodes.Mul;
				} else
					opcode = OpCodes.Mul;
				
				break;
				
			case Operator.Division:
				if (IsUnsigned (l))
					opcode = OpCodes.Div_Un;
				else
					opcode = OpCodes.Div;
				break;
				
			case Operator.Modulus:
				if (IsUnsigned (l))
					opcode = OpCodes.Rem_Un;
				else
					opcode = OpCodes.Rem;
				break;

			case Operator.Addition:
				if (ec.HasSet (EmitContext.Options.CheckedScope)) {
					if (l.BuiltinType == BuiltinTypeSpec.Type.Int || l.BuiltinType == BuiltinTypeSpec.Type.Long)
						opcode = OpCodes.Add_Ovf;
					else if (!IsFloat (l))
						opcode = OpCodes.Add_Ovf_Un;
					else
						opcode = OpCodes.Add;
				} else
					opcode = OpCodes.Add;
				break;

			case Operator.Subtraction:
				if (ec.HasSet (EmitContext.Options.CheckedScope)) {
					if (l.BuiltinType == BuiltinTypeSpec.Type.Int || l.BuiltinType == BuiltinTypeSpec.Type.Long)
						opcode = OpCodes.Sub_Ovf;
					else if (!IsFloat (l))
						opcode = OpCodes.Sub_Ovf_Un;
					else
						opcode = OpCodes.Sub;
				} else
					opcode = OpCodes.Sub;
				break;

			case Operator.RightShift:
				if (IsUnsigned (l))
					opcode = OpCodes.Shr_Un;
				else
					opcode = OpCodes.Shr;
				break;
				
			case Operator.LeftShift:
				opcode = OpCodes.Shl;
				break;

			case Operator.Equality:
				opcode = OpCodes.Ceq;
				break;

			case Operator.Inequality:
				ec.Emit (OpCodes.Ceq);
				ec.EmitInt (0);
				
				opcode = OpCodes.Ceq;
				break;

			case Operator.LessThan:
				if (IsUnsigned (l))
					opcode = OpCodes.Clt_Un;
				else
					opcode = OpCodes.Clt;
				break;

			case Operator.GreaterThan:
				if (IsUnsigned (l))
					opcode = OpCodes.Cgt_Un;
				else
					opcode = OpCodes.Cgt;
				break;

			case Operator.LessThanOrEqual:
				if (IsUnsigned (l) || IsFloat (l))
					ec.Emit (OpCodes.Cgt_Un);
				else
					ec.Emit (OpCodes.Cgt);
				ec.EmitInt (0);
				
				opcode = OpCodes.Ceq;
				break;

			case Operator.GreaterThanOrEqual:
				if (IsUnsigned (l) || IsFloat (l))
					ec.Emit (OpCodes.Clt_Un);
				else
					ec.Emit (OpCodes.Clt);
				
				ec.EmitInt (0);
				
				opcode = OpCodes.Ceq;
				break;

			case Operator.BitwiseOr:
				opcode = OpCodes.Or;
				break;

			case Operator.BitwiseAnd:
				opcode = OpCodes.And;
				break;

			case Operator.ExclusiveOr:
				opcode = OpCodes.Xor;
				break;

			default:
				throw new InternalErrorException (oper.ToString ());
			}

			ec.Emit (opcode);
		}

		static bool IsUnsigned (TypeSpec t)
		{
			switch (t.BuiltinType) {
			case BuiltinTypeSpec.Type.Char:
			case BuiltinTypeSpec.Type.UInt:
			case BuiltinTypeSpec.Type.ULong:
			case BuiltinTypeSpec.Type.UShort:
			case BuiltinTypeSpec.Type.Byte:
				return true;
			}

			return t.IsPointer;
		}

		static bool IsFloat (TypeSpec t)
		{
			return t.BuiltinType == BuiltinTypeSpec.Type.Float || t.BuiltinType == BuiltinTypeSpec.Type.Double;
		}

		Expression ResolveOperator (ResolveContext ec)
		{
			TypeSpec l = left.Type;
			TypeSpec r = right.Type;
			Expression expr;
			bool primitives_only = false;

			//
			// Handles predefined primitive types
			//
			if (BuiltinTypeSpec.IsPrimitiveType (l) && BuiltinTypeSpec.IsPrimitiveType (r)) {
				if ((oper & Operator.ShiftMask) == 0) {
					if (l.BuiltinType != BuiltinTypeSpec.Type.Bool && !DoBinaryOperatorPromotion (ec))
						return null;

					primitives_only = true;
				}
			} else {
				// Pointers
				if (l.IsPointer || r.IsPointer)
					return ResolveOperatorPointer (ec, l, r);

				// Enums
				bool lenum = l.IsEnum;
				bool renum = r.IsEnum;
				if (lenum || renum) {
					expr = ResolveOperatorEnum (ec, lenum, renum, l, r);

					if (expr != null)
						return expr;
				}

				// Delegates
				if ((oper == Operator.Addition || oper == Operator.Subtraction) && (l.IsDelegate || r.IsDelegate)) {
						
					expr = ResolveOperatorDelegate (ec, l, r);

					// TODO: Can this be ambiguous
					if (expr != null)
						return expr;
				}

				// User operators
				expr = ResolveUserOperator (ec, left, right);
				if (expr != null)
					return expr;

				// Predefined reference types equality
				if ((oper & Operator.EqualityMask) != 0) {
					expr = ResolveOperatorEquality (ec, l, r);
					if (expr != null)
						return expr;
				}
			}

			return ResolveOperatorPredefined (ec, ec.BuiltinTypes.OperatorsBinaryStandard, primitives_only, null);
		}

		// at least one of 'left' or 'right' is an enumeration constant (EnumConstant or SideEffectConstant or ...)
		// if 'left' is not an enumeration constant, create one from the type of 'right'
		Constant EnumLiftUp (ResolveContext ec, Constant left, Constant right, Location loc)
		{
			switch (oper) {
			case Operator.BitwiseOr:
			case Operator.BitwiseAnd:
			case Operator.ExclusiveOr:
			case Operator.Equality:
			case Operator.Inequality:
			case Operator.LessThan:
			case Operator.LessThanOrEqual:
			case Operator.GreaterThan:
			case Operator.GreaterThanOrEqual:
				if (left.Type.IsEnum)
					return left;
				
				if (left.IsZeroInteger)
					return left.TryReduce (ec, right.Type);
				
				break;
				
			case Operator.Addition:
			case Operator.Subtraction:
				return left;
				
			case Operator.Multiply:
			case Operator.Division:
			case Operator.Modulus:
			case Operator.LeftShift:
			case Operator.RightShift:
				if (right.Type.IsEnum || left.Type.IsEnum)
					break;
				return left;
			}

			return null;
		}

		//
		// The `|' operator used on types which were extended is dangerous
		//
		void CheckBitwiseOrOnSignExtended (ResolveContext ec)
		{
			OpcodeCast lcast = left as OpcodeCast;
			if (lcast != null) {
				if (IsUnsigned (lcast.UnderlyingType))
					lcast = null;
			}

			OpcodeCast rcast = right as OpcodeCast;
			if (rcast != null) {
				if (IsUnsigned (rcast.UnderlyingType))
					rcast = null;
			}

			if (lcast == null && rcast == null)
				return;

			// FIXME: consider constants

			ec.Report.Warning (675, 3, loc,
				"The operator `|' used on the sign-extended type `{0}'. Consider casting to a smaller unsigned type first",
				TypeManager.CSharpName (lcast != null ? lcast.UnderlyingType : rcast.UnderlyingType));
		}

		public static PredefinedOperator[] CreatePointerOperatorsTable (BuiltinTypes types)
		{
			return new PredefinedOperator[] {
				//
				// Pointer arithmetic:
				//
				// T* operator + (T* x, int y);		T* operator - (T* x, int y);
				// T* operator + (T* x, uint y);	T* operator - (T* x, uint y);
				// T* operator + (T* x, long y);	T* operator - (T* x, long y);
				// T* operator + (T* x, ulong y);	T* operator - (T* x, ulong y);
				//
				new PredefinedPointerOperator (null, types.Int, Operator.AdditionMask | Operator.SubtractionMask),
				new PredefinedPointerOperator (null, types.UInt, Operator.AdditionMask | Operator.SubtractionMask),
				new PredefinedPointerOperator (null, types.Long, Operator.AdditionMask | Operator.SubtractionMask),
				new PredefinedPointerOperator (null, types.ULong, Operator.AdditionMask | Operator.SubtractionMask),

				//
				// T* operator + (int y,   T* x);
				// T* operator + (uint y,  T *x);
				// T* operator + (long y,  T *x);
				// T* operator + (ulong y, T *x);
				//
				new PredefinedPointerOperator (types.Int, null, Operator.AdditionMask, null),
				new PredefinedPointerOperator (types.UInt, null, Operator.AdditionMask, null),
				new PredefinedPointerOperator (types.Long, null, Operator.AdditionMask, null),
				new PredefinedPointerOperator (types.ULong, null, Operator.AdditionMask, null),

				//
				// long operator - (T* x, T *y)
				//
				new PredefinedPointerOperator (null, Operator.SubtractionMask, types.Long)
			};
		}

		public static PredefinedOperator[] CreateStandardOperatorsTable (BuiltinTypes types)
		{
			TypeSpec bool_type = types.Bool;
			return new PredefinedOperator[] {
				new PredefinedOperator (types.Int, Operator.ArithmeticMask | Operator.BitwiseMask),
				new PredefinedOperator (types.UInt, Operator.ArithmeticMask | Operator.BitwiseMask),
				new PredefinedOperator (types.Long, Operator.ArithmeticMask | Operator.BitwiseMask),
				new PredefinedOperator (types.ULong, Operator.ArithmeticMask | Operator.BitwiseMask),
				new PredefinedOperator (types.Float, Operator.ArithmeticMask),
				new PredefinedOperator (types.Double, Operator.ArithmeticMask),
				new PredefinedOperator (types.Decimal, Operator.ArithmeticMask),

				new PredefinedOperator (types.Int, Operator.ComparisonMask, bool_type),
				new PredefinedOperator (types.UInt, Operator.ComparisonMask, bool_type),
				new PredefinedOperator (types.Long, Operator.ComparisonMask, bool_type),
				new PredefinedOperator (types.ULong, Operator.ComparisonMask, bool_type),
				new PredefinedOperator (types.Float, Operator.ComparisonMask, bool_type),
				new PredefinedOperator (types.Double, Operator.ComparisonMask, bool_type),
				new PredefinedOperator (types.Decimal, Operator.ComparisonMask, bool_type),

				new PredefinedStringOperator (types.String, Operator.AdditionMask, types.String),
				new PredefinedStringOperator (types.String, types.Object, Operator.AdditionMask, types.String),
				new PredefinedStringOperator (types.Object, types.String, Operator.AdditionMask, types.String),

				new PredefinedOperator (bool_type, Operator.BitwiseMask | Operator.LogicalMask | Operator.EqualityMask, bool_type),

				new PredefinedShiftOperator (types.Int, types.Int, Operator.ShiftMask),
				new PredefinedShiftOperator (types.UInt, types.Int, Operator.ShiftMask),
				new PredefinedShiftOperator (types.Long, types.Int, Operator.ShiftMask),
				new PredefinedShiftOperator (types.ULong, types.Int, Operator.ShiftMask)
			};
		}

		public static PredefinedOperator[] CreateEqualityOperatorsTable (BuiltinTypes types)
		{
			TypeSpec bool_type = types.Bool;

			return new PredefinedOperator[] {
				new PredefinedEqualityOperator (types.String, bool_type),
				new PredefinedEqualityOperator (types.Delegate, bool_type),
				new PredefinedOperator (bool_type, Operator.EqualityMask, bool_type)
			};
		}

		//
		// Rules used during binary numeric promotion
		//
		static bool DoNumericPromotion (ResolveContext rc, ref Expression prim_expr, ref Expression second_expr, TypeSpec type)
		{
			Expression temp;

			Constant c = prim_expr as Constant;
			if (c != null) {
				temp = c.ConvertImplicitly (type);
				if (temp != null) {
					prim_expr = temp;
					return true;
				}
			}

			if (type.BuiltinType == BuiltinTypeSpec.Type.UInt) {
				switch (prim_expr.Type.BuiltinType) {
				case BuiltinTypeSpec.Type.Int:
				case BuiltinTypeSpec.Type.Short:
				case BuiltinTypeSpec.Type.SByte:
				case BuiltinTypeSpec.Type.Long:
					type = rc.BuiltinTypes.Long;

					if (type != second_expr.Type) {
						c = second_expr as Constant;
						if (c != null)
							temp = c.ConvertImplicitly (type);
						else
							temp = Convert.ImplicitNumericConversion (second_expr, type);
						if (temp == null)
							return false;
						second_expr = temp;
					}
					break;
				}
			} else if (type.BuiltinType == BuiltinTypeSpec.Type.ULong) {
				//
				// A compile-time error occurs if the other operand is of type sbyte, short, int, or long
				//
				switch (type.BuiltinType) {
				case BuiltinTypeSpec.Type.Int:
				case BuiltinTypeSpec.Type.Long:
				case BuiltinTypeSpec.Type.Short:
				case BuiltinTypeSpec.Type.SByte:
					return false;
				}
			}

			temp = Convert.ImplicitNumericConversion (prim_expr, type);
			if (temp == null)
				return false;

			prim_expr = temp;
			return true;
		}

		//
		// 7.2.6.2 Binary numeric promotions
		//
		public bool DoBinaryOperatorPromotion (ResolveContext ec)
		{
			TypeSpec ltype = left.Type;
			TypeSpec rtype = right.Type;
			Expression temp;

			foreach (TypeSpec t in ec.BuiltinTypes.BinaryPromotionsTypes) {
				if (t == ltype)
					return t == rtype || DoNumericPromotion (ec, ref right, ref left, t);

				if (t == rtype)
					return t == ltype || DoNumericPromotion (ec, ref left, ref right, t);
			}

			TypeSpec int32 = ec.BuiltinTypes.Int;
			if (ltype != int32) {
				Constant c = left as Constant;
				if (c != null)
					temp = c.ConvertImplicitly (int32);
				else
					temp = Convert.ImplicitNumericConversion (left, int32);

				if (temp == null)
					return false;
				left = temp;
			}

			if (rtype != int32) {
				Constant c = right as Constant;
				if (c != null)
					temp = c.ConvertImplicitly (int32);
				else
					temp = Convert.ImplicitNumericConversion (right, int32);

				if (temp == null)
					return false;
				right = temp;
			}

			return true;
		}

		protected override Expression DoResolve (ResolveContext ec)
		{
			if (left == null)
				return null;

			if ((oper == Operator.Subtraction) && (left is ParenthesizedExpression)) {
				left = ((ParenthesizedExpression) left).Expr;
				left = left.Resolve (ec, ResolveFlags.VariableOrValue | ResolveFlags.Type);
				if (left == null)
					return null;

				if (left.eclass == ExprClass.Type) {
					ec.Report.Error (75, loc, "To cast a negative value, you must enclose the value in parentheses");
					return null;
				}
			} else
				left = left.Resolve (ec);

			if (left == null)
				return null;

			Constant lc = left as Constant;

			if (lc != null && lc.Type.BuiltinType == BuiltinTypeSpec.Type.Bool &&
				((oper == Operator.LogicalAnd && lc.IsDefaultValue) ||
				 (oper == Operator.LogicalOr && !lc.IsDefaultValue))) {

				// FIXME: resolve right expression as unreachable
				// right.Resolve (ec);

				ec.Report.Warning (429, 4, loc, "Unreachable expression code detected");
				return left;
			}

			right = right.Resolve (ec);
			if (right == null)
				return null;

			eclass = ExprClass.Value;
			Constant rc = right as Constant;

			// The conversion rules are ignored in enum context but why
			if (!ec.HasSet (ResolveContext.Options.EnumScope) && lc != null && rc != null && (left.Type.IsEnum || right.Type.IsEnum)) {
				lc = EnumLiftUp (ec, lc, rc, loc);
				if (lc != null)
					rc = EnumLiftUp (ec, rc, lc, loc);
			}

			if (rc != null && lc != null) {
				int prev_e = ec.Report.Errors;
				Expression e = ConstantFold.BinaryFold (ec, oper, lc, rc, loc);
				if (e != null || ec.Report.Errors != prev_e)
					return e;
			}

			// Comparison warnings
			if ((oper & Operator.ComparisonMask) != 0) {
				if (left.Equals (right)) {
					ec.Report.Warning (1718, 3, loc, "A comparison made to same variable. Did you mean to compare something else?");
				}
				CheckOutOfRangeComparison (ec, lc, right.Type);
				CheckOutOfRangeComparison (ec, rc, left.Type);
			}

			if (left.Type.BuiltinType == BuiltinTypeSpec.Type.Dynamic || right.Type.BuiltinType == BuiltinTypeSpec.Type.Dynamic) {
				var lt = left.Type;
				var rt = right.Type;
				if (lt.Kind == MemberKind.Void || lt == InternalType.MethodGroup || lt == InternalType.AnonymousMethod ||
					rt.Kind == MemberKind.Void || rt == InternalType.MethodGroup || rt == InternalType.AnonymousMethod) {
					Error_OperatorCannotBeApplied (ec, left, right);
					return null;
				}

				Arguments args;

				//
				// Special handling for logical boolean operators which require rhs not to be
				// evaluated based on lhs value
				//
				if ((oper & Operator.LogicalMask) != 0) {
					Expression cond_left, cond_right, expr;

					args = new Arguments (2);

					if (lt.BuiltinType == BuiltinTypeSpec.Type.Dynamic) {
						LocalVariable temp = LocalVariable.CreateCompilerGenerated (lt, ec.CurrentBlock, loc);

						var cond_args = new Arguments (1);
						cond_args.Add (new Argument (new SimpleAssign (temp.CreateReferenceExpression (ec, loc), left).Resolve (ec)));

						//
						// dynamic && bool => IsFalse (temp = left) ? temp : temp && right;
						// dynamic || bool => IsTrue (temp = left) ? temp : temp || right;
						//
						left = temp.CreateReferenceExpression (ec, loc);
						if (oper == Operator.LogicalAnd) {
							expr = DynamicUnaryConversion.CreateIsFalse (ec, cond_args, loc);
							cond_left = left;
						} else {
							expr = DynamicUnaryConversion.CreateIsTrue (ec, cond_args, loc);
							cond_left = left;
						}

						args.Add (new Argument (left));
						args.Add (new Argument (right));
						cond_right = new DynamicExpressionStatement (this, args, loc);
					} else {
						LocalVariable temp = LocalVariable.CreateCompilerGenerated (ec.BuiltinTypes.Bool, ec.CurrentBlock, loc);

						args.Add (new Argument (temp.CreateReferenceExpression (ec, loc).Resolve (ec)));
						args.Add (new Argument (right));
						right = new DynamicExpressionStatement (this, args, loc);

						//
						// bool && dynamic => (temp = left) ? temp && right : temp;
						// bool || dynamic => (temp = left) ? temp : temp || right;
						//
						if (oper == Operator.LogicalAnd) {
							cond_left = right;
							cond_right = temp.CreateReferenceExpression (ec, loc);
						} else {
							cond_left = temp.CreateReferenceExpression (ec, loc);
							cond_right = right;
						}

						expr = new BooleanExpression (new SimpleAssign (temp.CreateReferenceExpression (ec, loc), left));
					}

					return new Conditional (expr, cond_left, cond_right, loc).Resolve (ec);
				}

				args = new Arguments (2);
				args.Add (new Argument (left));
				args.Add (new Argument (right));
				return new DynamicExpressionStatement (this, args, loc).Resolve (ec);
			}

			if (ec.Module.Compiler.Settings.Version >= LanguageVersion.ISO_2 &&
				((left.Type.IsNullableType && (right is NullLiteral || right.Type.IsNullableType || TypeSpec.IsValueType (right.Type))) ||
				(TypeSpec.IsValueType (left.Type) && right is NullLiteral) ||
				(right.Type.IsNullableType && (left is NullLiteral || left.Type.IsNullableType || TypeSpec.IsValueType (left.Type))) ||
				(TypeSpec.IsValueType (right.Type) && left is NullLiteral))) {
				var lifted = new Nullable.LiftedBinaryOperator (oper, left, right, loc);
				lifted.state = state;
				return lifted.Resolve (ec);
			}

			return DoResolveCore (ec, left, right);
		}

		protected Expression DoResolveCore (ResolveContext ec, Expression left_orig, Expression right_orig)
		{
			Expression expr = ResolveOperator (ec);
			if (expr == null)
				Error_OperatorCannotBeApplied (ec, left_orig, right_orig);

			if (left == null || right == null)
				throw new InternalErrorException ("Invalid conversion");

			if (oper == Operator.BitwiseOr)
				CheckBitwiseOrOnSignExtended (ec);

			return expr;
		}

		public override SLE.Expression MakeExpression (BuilderContext ctx)
		{
			var le = left.MakeExpression (ctx);
			var re = right.MakeExpression (ctx);
			bool is_checked = ctx.HasSet (BuilderContext.Options.CheckedScope);

			switch (oper) {
			case Operator.Addition:
				return is_checked ? SLE.Expression.AddChecked (le, re) : SLE.Expression.Add (le, re);
			case Operator.BitwiseAnd:
				return SLE.Expression.And (le, re);
			case Operator.BitwiseOr:
				return SLE.Expression.Or (le, re);
			case Operator.Division:
				return SLE.Expression.Divide (le, re);
			case Operator.Equality:
				return SLE.Expression.Equal (le, re);
			case Operator.ExclusiveOr:
				return SLE.Expression.ExclusiveOr (le, re);
			case Operator.GreaterThan:
				return SLE.Expression.GreaterThan (le, re);
			case Operator.GreaterThanOrEqual:
				return SLE.Expression.GreaterThanOrEqual (le, re);
			case Operator.Inequality:
				return SLE.Expression.NotEqual (le, re);
			case Operator.LeftShift:
				return SLE.Expression.LeftShift (le, re);
			case Operator.LessThan:
				return SLE.Expression.LessThan (le, re);
			case Operator.LessThanOrEqual:
				return SLE.Expression.LessThanOrEqual (le, re);
			case Operator.LogicalAnd:
				return SLE.Expression.AndAlso (le, re);
			case Operator.LogicalOr:
				return SLE.Expression.OrElse (le, re);
			case Operator.Modulus:
				return SLE.Expression.Modulo (le, re);
			case Operator.Multiply:
				return is_checked ? SLE.Expression.MultiplyChecked (le, re) : SLE.Expression.Multiply (le, re);
			case Operator.RightShift:
				return SLE.Expression.RightShift (le, re);
			case Operator.Subtraction:
				return is_checked ? SLE.Expression.SubtractChecked (le, re) : SLE.Expression.Subtract (le, re);
			default:
				throw new NotImplementedException (oper.ToString ());
			}
		}

		//
		// D operator + (D x, D y)
		// D operator - (D x, D y)
		//
		Expression ResolveOperatorDelegate (ResolveContext ec, TypeSpec l, TypeSpec r)
		{
			if (l != r && !TypeSpecComparer.Variant.IsEqual (r, l)) {
				Expression tmp;
				if (right.eclass == ExprClass.MethodGroup || r == InternalType.AnonymousMethod || r == InternalType.NullLiteral) {
					tmp = Convert.ImplicitConversionRequired (ec, right, l, loc);
					if (tmp == null)
						return null;
					right = tmp;
					r = right.Type;
				} else if (left.eclass == ExprClass.MethodGroup || (l == InternalType.AnonymousMethod || l == InternalType.NullLiteral)) {
					tmp = Convert.ImplicitConversionRequired (ec, left, r, loc);
					if (tmp == null)
						return null;
					left = tmp;
					l = left.Type;
				} else {
					return null;
				}
			}

			MethodSpec method = null;
			Arguments args = new Arguments (2);
			args.Add (new Argument (left));
			args.Add (new Argument (right));

			if (oper == Operator.Addition) {
				method = ec.Module.PredefinedMembers.DelegateCombine.Resolve (loc);
			} else if (oper == Operator.Subtraction) {
				method = ec.Module.PredefinedMembers.DelegateRemove.Resolve (loc);
			}

			if (method == null)
				return new EmptyExpression (ec.BuiltinTypes.Decimal);

			MethodGroupExpr mg = MethodGroupExpr.CreatePredefined (method, ec.BuiltinTypes.Delegate, loc);
			Expression expr = new UserOperatorCall (mg.BestCandidate, args, CreateExpressionTree, loc);
			return new ClassCast (expr, l);
		}

		//
		// Enumeration operators
		//
		Expression ResolveOperatorEnum (ResolveContext ec, bool lenum, bool renum, TypeSpec ltype, TypeSpec rtype)
		{
			//
			// bool operator == (E x, E y);
			// bool operator != (E x, E y);
			// bool operator < (E x, E y);
			// bool operator > (E x, E y);
			// bool operator <= (E x, E y);
			// bool operator >= (E x, E y);
			//
			// E operator & (E x, E y);
			// E operator | (E x, E y);
			// E operator ^ (E x, E y);
			//
			// U operator - (E e, E f)
			// E operator - (E e, U x)
			// E operator - (U x, E e)	// LAMESPEC: Not covered by the specification
			//
			// E operator + (E e, U x)
			// E operator + (U x, E e)
			//
			Expression ltemp = left;
			Expression rtemp = right;
			TypeSpec underlying_type;
			TypeSpec underlying_type_result;
			TypeSpec res_type;
			Expression expr;
			
			//
			// LAMESPEC: There is never ambiguous conversion between enum operators
			// the one which contains more enum parameters always wins even if there
			// is an implicit conversion involved
			//
			if ((oper & (Operator.ComparisonMask | Operator.BitwiseMask)) != 0) {
				if (renum) {
					underlying_type = EnumSpec.GetUnderlyingType (rtype);
					expr = Convert.ImplicitConversion (ec, left, rtype, loc);
					if (expr == null)
						return null;

					left = expr;
					ltype = expr.Type;
				} else if (lenum) {
					underlying_type = EnumSpec.GetUnderlyingType (ltype);
					expr = Convert.ImplicitConversion (ec, right, ltype, loc);
					if (expr == null)
						return null;

					right = expr;
					rtype = expr.Type;
				} else {
					return null;
				}

				if ((oper & Operator.BitwiseMask) != 0) {
					res_type = ltype;
					underlying_type_result = underlying_type;
				} else {
					res_type = null;
					underlying_type_result = null;
				}
			} else if (oper == Operator.Subtraction) {
				if (renum) {
					underlying_type = EnumSpec.GetUnderlyingType (rtype);
					if (ltype != rtype) {
						expr = Convert.ImplicitConversion (ec, left, rtype, left.Location);
						if (expr == null) {
							expr = Convert.ImplicitConversion (ec, left, underlying_type, left.Location);
							if (expr == null)
								return null;

							res_type = rtype;
						} else {
							res_type = underlying_type;
						}

						left = expr;
					} else {
						res_type = underlying_type;
					}

					underlying_type_result = underlying_type;
				} else if (lenum) {
					underlying_type = EnumSpec.GetUnderlyingType (ltype);
					expr = Convert.ImplicitConversion (ec, right, ltype, right.Location);
					if (expr == null || expr is EnumConstant) {
						expr = Convert.ImplicitConversion (ec, right, underlying_type, right.Location);
						if (expr == null)
							return null;

						res_type = ltype;
					} else {
						res_type = underlying_type;
					}

					right = expr;
					underlying_type_result = underlying_type;
				} else {
					return null;
				}
			} else if (oper == Operator.Addition) {
				if (lenum) {
					underlying_type = EnumSpec.GetUnderlyingType (ltype);
					res_type = ltype;

					if (rtype != underlying_type && (state & (State.RightNullLifted | State.LeftNullLifted)) == 0) {
						expr = Convert.ImplicitConversion (ec, right, underlying_type, right.Location);
						if (expr == null)
							return null;

						right = expr;
					}
				} else {
					underlying_type = EnumSpec.GetUnderlyingType (rtype);
					res_type = rtype;
					if (ltype != underlying_type) {
						expr = Convert.ImplicitConversion (ec, left, underlying_type, left.Location);
						if (expr == null)
							return null;

						left = expr;
					}
				}

				underlying_type_result = underlying_type;
			} else {
				return null;
			}

			// Unwrap the constant correctly, so DoBinaryOperatorPromotion can do the magic
			// with constants and expressions
			if (left.Type != underlying_type) {
				if (left is Constant)
					left = ((Constant) left).ConvertExplicitly (false, underlying_type);
				else
					left = EmptyCast.Create (left, underlying_type);
			}

			if (right.Type != underlying_type) {
				if (right is Constant)
					right = ((Constant) right).ConvertExplicitly (false, underlying_type);
				else
					right = EmptyCast.Create (right, underlying_type);
			}

			//
			// C# specification uses explicit cast syntax which means binary promotion
			// should happen, however it seems that csc does not do that
			//
			if (!DoBinaryOperatorPromotion (ec)) {
				left = ltemp;
				right = rtemp;
				return null;
			}

			if (underlying_type_result != null && left.Type != underlying_type_result) {
				enum_conversion = Convert.ExplicitNumericConversion (ec, new EmptyExpression (left.Type), underlying_type_result);
			}

			expr = ResolveOperatorPredefined (ec, ec.BuiltinTypes.OperatorsBinaryStandard, true, res_type);
			if (expr == null)
				return null;

			if (!IsCompound)
				return expr;

			//
			// Section: 7.16.2
			//

			//
			// If the return type of the selected operator is implicitly convertible to the type of x
			//
			if (Convert.ImplicitConversionExists (ec, expr, ltype))
				return expr;

			//
			// Otherwise, if the selected operator is a predefined operator, if the return type of the
			// selected operator is explicitly convertible to the type of x, and if y is implicitly
			// convertible to the type of x or the operator is a shift operator, then the operation
			// is evaluated as x = (T)(x op y), where T is the type of x
			//
			expr = Convert.ExplicitConversion (ec, expr, ltype, loc);
			if (expr == null)
				return null;

			if (Convert.ImplicitConversionExists (ec, ltemp, ltype))
				return expr;

			return null;
		}

		//
		// 7.9.6 Reference type equality operators
		//
		Expression ResolveOperatorEquality (ResolveContext ec, TypeSpec l, TypeSpec r)
		{
			Expression result;
			type = ec.BuiltinTypes.Bool;

			//
			// a, Both operands are reference-type values or the value null
			// b, One operand is a value of type T where T is a type-parameter and
			// the other operand is the value null. Furthermore T does not have the
			// value type constraint
			//
			// LAMESPEC: Very confusing details in the specification, basically any
			// reference like type-parameter is allowed
			//
			var tparam_l = l as TypeParameterSpec;
			var tparam_r = r as TypeParameterSpec;
			if (tparam_l != null) {
				if (right is NullLiteral && !tparam_l.HasSpecialStruct) {
					left = new BoxedCast (left, ec.BuiltinTypes.Object);
					return this;
				}

				if (!tparam_l.IsReferenceType)
					return null;

				l = tparam_l.GetEffectiveBase ();
				left = new BoxedCast (left, l);
			} else if (left is NullLiteral && tparam_r == null) {
				if (!TypeSpec.IsReferenceType (r) || r.Kind == MemberKind.InternalCompilerType)
					return null;

				return this;
			}

			if (tparam_r != null) {
				if (left is NullLiteral && !tparam_r.HasSpecialStruct) {
					right = new BoxedCast (right, ec.BuiltinTypes.Object);
					return this;
				}

				if (!tparam_r.IsReferenceType)
					return null;

				r = tparam_r.GetEffectiveBase ();
				right = new BoxedCast (right, r);
			} else if (right is NullLiteral) {
				if (!TypeSpec.IsReferenceType (l) || l.Kind == MemberKind.InternalCompilerType)
					return null;

				return this;
			}

			bool no_arg_conv = false;

			//
			// LAMESPEC: method groups can be compared when they convert to other side delegate
			//
			if (l.IsDelegate) {
				if (right.eclass == ExprClass.MethodGroup) {
					result = Convert.ImplicitConversion (ec, right, l, loc);
					if (result == null)
						return null;

					right = result;
					r = l;
				} else if (r.IsDelegate && l != r) {
					return null;
				}
			} else if (left.eclass == ExprClass.MethodGroup && r.IsDelegate) {
				result = Convert.ImplicitConversionRequired (ec, left, r, loc);
				if (result == null)
					return null;

				left = result;
				l = r;
			} else {
				no_arg_conv = l == r && !l.IsStruct;
			}

			//
			// bool operator != (string a, string b)
			// bool operator == (string a, string b)
			//
			// bool operator != (Delegate a, Delegate b)
			// bool operator == (Delegate a, Delegate b)
			//
			// bool operator != (bool a, bool b)
			// bool operator == (bool a, bool b)
			//
			// LAMESPEC: Reference equality comparison can apply to value/reference types when
			// they implement an implicit conversion to any of types above. This does
			// not apply when both operands are of same reference type
			//
			if (r.BuiltinType != BuiltinTypeSpec.Type.Object && l.BuiltinType != BuiltinTypeSpec.Type.Object) {
				result = ResolveOperatorPredefined (ec, ec.BuiltinTypes.OperatorsBinaryEquality, no_arg_conv, null);
				if (result != null)
					return result;
			}

			//
			// bool operator != (object a, object b)
			// bool operator == (object a, object b)
			//
			// An explicit reference conversion exists from the
			// type of either operand to the type of the other operand.
			//

			// Optimize common path
			if (l == r) {
				return l.Kind == MemberKind.InternalCompilerType || l.Kind == MemberKind.Struct ? null : this;
			}

			if (!Convert.ExplicitReferenceConversionExists (l, r) &&
				!Convert.ExplicitReferenceConversionExists (r, l))
				return null;

			// Reject allowed explicit conversions like int->object
			if (!TypeSpec.IsReferenceType (l) || !TypeSpec.IsReferenceType (r))
				return null;

			if (l.BuiltinType == BuiltinTypeSpec.Type.String || l.BuiltinType == BuiltinTypeSpec.Type.Delegate || MemberCache.GetUserOperator (l, CSharp.Operator.OpType.Equality, false) != null)
				ec.Report.Warning (253, 2, loc,
					"Possible unintended reference comparison. Consider casting the right side expression to type `{0}' to get value comparison",
					l.GetSignatureForError ());

			if (r.BuiltinType == BuiltinTypeSpec.Type.String || r.BuiltinType == BuiltinTypeSpec.Type.Delegate || MemberCache.GetUserOperator (r, CSharp.Operator.OpType.Equality, false) != null)
				ec.Report.Warning (252, 2, loc,
					"Possible unintended reference comparison. Consider casting the left side expression to type `{0}' to get value comparison",
					r.GetSignatureForError ());

			return this;
		}


		Expression ResolveOperatorPointer (ResolveContext ec, TypeSpec l, TypeSpec r)
		{
			//
			// bool operator == (void* x, void* y);
			// bool operator != (void* x, void* y);
			// bool operator < (void* x, void* y);
			// bool operator > (void* x, void* y);
			// bool operator <= (void* x, void* y);
			// bool operator >= (void* x, void* y);
			//
			if ((oper & Operator.ComparisonMask) != 0) {
				Expression temp;
				if (!l.IsPointer) {
					temp = Convert.ImplicitConversion (ec, left, r, left.Location);
					if (temp == null)
						return null;
					left = temp;
				}

				if (!r.IsPointer) {
					temp = Convert.ImplicitConversion (ec, right, l, right.Location);
					if (temp == null)
						return null;
					right = temp;
				}

				type = ec.BuiltinTypes.Bool;
				return this;
			}

			return ResolveOperatorPredefined (ec, ec.BuiltinTypes.OperatorsBinaryUnsafe, false, null);
		}

		//
		// Build-in operators method overloading
		//
		protected virtual Expression ResolveOperatorPredefined (ResolveContext ec, PredefinedOperator [] operators, bool primitives_only, TypeSpec enum_type)
		{
			PredefinedOperator best_operator = null;
			TypeSpec l = left.Type;
			TypeSpec r = right.Type;
			Operator oper_mask = oper & ~Operator.ValuesOnlyMask;

			foreach (PredefinedOperator po in operators) {
				if ((po.OperatorsMask & oper_mask) == 0)
					continue;

				if (primitives_only) {
					if (!po.IsPrimitiveApplicable (l, r))
						continue;
				} else {
					if (!po.IsApplicable (ec, left, right))
						continue;
				}

				if (best_operator == null) {
					best_operator = po;
					if (primitives_only)
						break;

					continue;
				}

				best_operator = po.ResolveBetterOperator (ec, best_operator);

				if (best_operator == null) {
					ec.Report.Error (34, loc, "Operator `{0}' is ambiguous on operands of type `{1}' and `{2}'",
						OperName (oper), TypeManager.CSharpName (l), TypeManager.CSharpName (r));

					best_operator = po;
					break;
				}
			}

			if (best_operator == null)
				return null;

			Expression expr = best_operator.ConvertResult (ec, this);

			//
			// Optimize &/&& constant expressions with 0 value
			//
			if (oper == Operator.BitwiseAnd || oper == Operator.LogicalAnd) {
				Constant rc = right as Constant;
				Constant lc = left as Constant;
				if (((lc != null && lc.IsDefaultValue) || (rc != null && rc.IsDefaultValue)) && !(this is Nullable.LiftedBinaryOperator)) {
					//
					// The result is a constant with side-effect
					//
					Constant side_effect = rc == null ?
						new SideEffectConstant (lc, right, loc) :
						new SideEffectConstant (rc, left, loc);

					return ReducedExpression.Create (side_effect, expr);
				}
			}

			if (enum_type == null)
				return expr;

			//
			// HACK: required by enum_conversion
			//
			expr.Type = enum_type;
			return EmptyCast.Create (expr, enum_type);
		}

		//
		// Performs user-operator overloading
		//
		protected virtual Expression ResolveUserOperator (ResolveContext ec, Expression left, Expression right)
		{
			var op = ConvertBinaryToUserOperator (oper);
			var l = left.Type;
			if (l.IsNullableType)
				l = Nullable.NullableInfo.GetUnderlyingType (l);
			var r = right.Type;
			if (r.IsNullableType)
				r = Nullable.NullableInfo.GetUnderlyingType (r);

			IList<MemberSpec> left_operators = MemberCache.GetUserOperator (l, op, false);
			IList<MemberSpec> right_operators = null;

			if (l != r) {
				right_operators = MemberCache.GetUserOperator (r, op, false);
				if (right_operators == null && left_operators == null)
					return null;
			} else if (left_operators == null) {
				return null;
			}

			Arguments args = new Arguments (2);
			Argument larg = new Argument (left);
			args.Add (larg);
			Argument rarg = new Argument (right);
			args.Add (rarg);

			//
			// User-defined operator implementations always take precedence
			// over predefined operator implementations
			//
			if (left_operators != null && right_operators != null) {
				left_operators = CombineUserOperators (left_operators, right_operators);
			} else if (right_operators != null) {
				left_operators = right_operators;
			}

			var res = new OverloadResolver (left_operators, OverloadResolver.Restrictions.ProbingOnly | 
				OverloadResolver.Restrictions.NoBaseMembers | OverloadResolver.Restrictions.BaseMembersIncluded, loc);

			var oper_method = res.ResolveOperator (ec, ref args);
			if (oper_method == null)
				return null;

			var llifted = (state & State.LeftNullLifted) != 0;
			var rlifted = (state & State.RightNullLifted) != 0;
			if ((Oper & Operator.EqualityMask) != 0) {
				var parameters = oper_method.Parameters;
				// LAMESPEC: No idea why this is not allowed
				if ((left is Nullable.Unwrap || right is Nullable.Unwrap) && parameters.Types [0] != parameters.Types [1])
					return null;

				// Binary operation was lifted but we have found a user operator
				// which requires value-type argument, we downgrade ourself back to
				// binary operation
				// LAMESPEC: The user operator is not called (it cannot be we are passing null to struct)
				// but compilation succeeds
				if ((llifted && !parameters.Types[0].IsStruct) || (rlifted && !parameters.Types[1].IsStruct)) {
					state &= ~(State.LeftNullLifted | State.RightNullLifted);
				}
			}

			Expression oper_expr;

			// TODO: CreateExpressionTree is allocated every time
			if ((oper & Operator.LogicalMask) != 0) {
				oper_expr = new ConditionalLogicalOperator (oper_method, args, CreateExpressionTree,
					oper == Operator.LogicalAnd, loc).Resolve (ec);
			} else {
				oper_expr = new UserOperatorCall (oper_method, args, CreateExpressionTree, loc);
			}

			if (!llifted)
				this.left = larg.Expr;

			if (!rlifted)
				this.right = rarg.Expr;

			return oper_expr;
		}

		//
		// Merge two sets of user operators into one, they are mostly distinguish
		// except when they share base type and it contains an operator
		//
		static IList<MemberSpec> CombineUserOperators (IList<MemberSpec> left, IList<MemberSpec> right)
		{
			var combined = new List<MemberSpec> (left.Count + right.Count);
			combined.AddRange (left);
			foreach (var r in right) {
				bool same = false;
				foreach (var l in left) {
					if (l.DeclaringType == r.DeclaringType) {
						same = true;
						break;
					}
				}

				if (!same)
					combined.Add (r);
			}

			return combined;
		}

		void CheckOutOfRangeComparison (ResolveContext ec, Constant c, TypeSpec type)
		{
			if (c is IntegralConstant || c is CharConstant) {
				try {
					c.ConvertExplicitly (true, type);
				} catch (OverflowException) {
					ec.Report.Warning (652, 2, loc,
						"A comparison between a constant and a variable is useless. The constant is out of the range of the variable type `{0}'",
						TypeManager.CSharpName (type));
				}
			}
		}

		/// <remarks>
		///   EmitBranchable is called from Statement.EmitBoolExpression in the
		///   context of a conditional bool expression.  This function will return
		///   false if it is was possible to use EmitBranchable, or true if it was.
		///
		///   The expression's code is generated, and we will generate a branch to `target'
		///   if the resulting expression value is equal to isTrue
		/// </remarks>
		public override void EmitBranchable (EmitContext ec, Label target, bool on_true)
		{
			//
			// This is more complicated than it looks, but its just to avoid
			// duplicated tests: basically, we allow ==, !=, >, <, >= and <=
			// but on top of that we want for == and != to use a special path
			// if we are comparing against null
			//
			if ((oper & Operator.EqualityMask) != 0 && (left is Constant || right is Constant)) {
				bool my_on_true = oper == Operator.Inequality ? on_true : !on_true;
				
				//
				// put the constant on the rhs, for simplicity
				//
				if (left is Constant) {
					Expression swap = right;
					right = left;
					left = swap;
				}
				
				//
				// brtrue/brfalse works with native int only
				//
				if (((Constant) right).IsZeroInteger && right.Type.BuiltinType != BuiltinTypeSpec.Type.Long && right.Type.BuiltinType != BuiltinTypeSpec.Type.ULong) {
					left.EmitBranchable (ec, target, my_on_true);
					return;
				}
				if (right.Type.BuiltinType == BuiltinTypeSpec.Type.Bool) {
					// right is a boolean, and it's not 'false' => it is 'true'
					left.EmitBranchable (ec, target, !my_on_true);
					return;
				}

			} else if (oper == Operator.LogicalAnd) {

				if (on_true) {
					Label tests_end = ec.DefineLabel ();
					
					left.EmitBranchable (ec, tests_end, false);
					right.EmitBranchable (ec, target, true);
					ec.MarkLabel (tests_end);					
				} else {
					//
					// This optimizes code like this 
					// if (true && i > 4)
					//
					if (!(left is Constant))
						left.EmitBranchable (ec, target, false);

					if (!(right is Constant)) 
						right.EmitBranchable (ec, target, false);
				}
				
				return;
				
			} else if (oper == Operator.LogicalOr){
				if (on_true) {
					left.EmitBranchable (ec, target, true);
					right.EmitBranchable (ec, target, true);
					
				} else {
					Label tests_end = ec.DefineLabel ();
					left.EmitBranchable (ec, tests_end, true);
					right.EmitBranchable (ec, target, false);
					ec.MarkLabel (tests_end);
				}
				
				return;

			} else if ((oper & Operator.ComparisonMask) == 0) {
				base.EmitBranchable (ec, target, on_true);
				return;
			}
			
			left.Emit (ec);
			right.Emit (ec);

			TypeSpec t = left.Type;
			bool is_float = IsFloat (t);
			bool is_unsigned = is_float || IsUnsigned (t);
			
			switch (oper){
			case Operator.Equality:
				if (on_true)
					ec.Emit (OpCodes.Beq, target);
				else
					ec.Emit (OpCodes.Bne_Un, target);
				break;

			case Operator.Inequality:
				if (on_true)
					ec.Emit (OpCodes.Bne_Un, target);
				else
					ec.Emit (OpCodes.Beq, target);
				break;

			case Operator.LessThan:
				if (on_true)
					if (is_unsigned && !is_float)
						ec.Emit (OpCodes.Blt_Un, target);
					else
						ec.Emit (OpCodes.Blt, target);
				else
					if (is_unsigned)
						ec.Emit (OpCodes.Bge_Un, target);
					else
						ec.Emit (OpCodes.Bge, target);
				break;

			case Operator.GreaterThan:
				if (on_true)
					if (is_unsigned && !is_float)
						ec.Emit (OpCodes.Bgt_Un, target);
					else
						ec.Emit (OpCodes.Bgt, target);
				else
					if (is_unsigned)
						ec.Emit (OpCodes.Ble_Un, target);
					else
						ec.Emit (OpCodes.Ble, target);
				break;

			case Operator.LessThanOrEqual:
				if (on_true)
					if (is_unsigned && !is_float)
						ec.Emit (OpCodes.Ble_Un, target);
					else
						ec.Emit (OpCodes.Ble, target);
				else
					if (is_unsigned)
						ec.Emit (OpCodes.Bgt_Un, target);
					else
						ec.Emit (OpCodes.Bgt, target);
				break;


			case Operator.GreaterThanOrEqual:
				if (on_true)
					if (is_unsigned && !is_float)
						ec.Emit (OpCodes.Bge_Un, target);
					else
						ec.Emit (OpCodes.Bge, target);
				else
					if (is_unsigned)
						ec.Emit (OpCodes.Blt_Un, target);
					else
						ec.Emit (OpCodes.Blt, target);
				break;
			default:
				throw new InternalErrorException (oper.ToString ());
			}
		}
		
		public override void Emit (EmitContext ec)
		{
			EmitOperator (ec, left.Type);
		}

		protected virtual void EmitOperator (EmitContext ec, TypeSpec l)
		{
			if (ec.HasSet (BuilderContext.Options.AsyncBody) && right.ContainsEmitWithAwait ()) {
				left = left.EmitToField (ec);

				if ((oper & Operator.LogicalMask) == 0) {
					right = right.EmitToField (ec);
				}
			}

			//
			// Handle short-circuit operators differently
			// than the rest
			//
			if ((oper & Operator.LogicalMask) != 0) {
				Label load_result = ec.DefineLabel ();
				Label end = ec.DefineLabel ();

				bool is_or = oper == Operator.LogicalOr;
				left.EmitBranchable (ec, load_result, is_or);
				right.Emit (ec);
				ec.Emit (OpCodes.Br_S, end);
				
				ec.MarkLabel (load_result);
				ec.EmitInt (is_or ? 1 : 0);
				ec.MarkLabel (end);
				return;
			}

			//
			// Optimize zero-based operations which cannot be optimized at expression level
			//
			if (oper == Operator.Subtraction) {
				var lc = left as IntegralConstant;
				if (lc != null && lc.IsDefaultValue) {
					right.Emit (ec);
					ec.Emit (OpCodes.Neg);
					return;
				}
			}

			left.Emit (ec);
			right.Emit (ec);
			EmitOperatorOpcode (ec, oper, l);

			//
			// Nullable enum could require underlying type cast and we cannot simply wrap binary
			// expression because that would wrap lifted binary operation
			//
			if (enum_conversion != null)
				enum_conversion.Emit (ec);
		}

		public override void EmitSideEffect (EmitContext ec)
		{
			if ((oper & Operator.LogicalMask) != 0 ||
				(ec.HasSet (EmitContext.Options.CheckedScope) && (oper == Operator.Multiply || oper == Operator.Addition || oper == Operator.Subtraction))) {
				base.EmitSideEffect (ec);
			} else {
				left.EmitSideEffect (ec);
				right.EmitSideEffect (ec);
			}
		}

		public override Expression EmitToField (EmitContext ec)
		{
			if ((oper & Operator.LogicalMask) == 0) {
				var await_expr = left as Await;
				if (await_expr != null && right.IsSideEffectFree) {
					await_expr.Statement.EmitPrologue (ec);
					left = await_expr.Statement.GetResultExpression (ec);
					return this;
				}

				await_expr = right as Await;
				if (await_expr != null && left.IsSideEffectFree) {
					await_expr.Statement.EmitPrologue (ec);
					right = await_expr.Statement.GetResultExpression (ec);
					return this;
				}
			}

			return base.EmitToField (ec);
		}

		protected override void CloneTo (CloneContext clonectx, Expression t)
		{
			Binary target = (Binary) t;

			target.left = left.Clone (clonectx);
			target.right = right.Clone (clonectx);
		}

		public Expression CreateCallSiteBinder (ResolveContext ec, Arguments args)
		{
			Arguments binder_args = new Arguments (4);

			MemberAccess sle = new MemberAccess (new MemberAccess (
				new QualifiedAliasMember (QualifiedAliasMember.GlobalAlias, "System", loc), "Linq", loc), "Expressions", loc);

			CSharpBinderFlags flags = 0;
			if (ec.HasSet (ResolveContext.Options.CheckedScope))
				flags = CSharpBinderFlags.CheckedContext;

			if ((oper & Operator.LogicalMask) != 0)
				flags |= CSharpBinderFlags.BinaryOperationLogical;

			binder_args.Add (new Argument (new EnumConstant (new IntLiteral (ec.BuiltinTypes, (int) flags, loc), ec.Module.PredefinedTypes.BinderFlags.Resolve ())));
			binder_args.Add (new Argument (new MemberAccess (new MemberAccess (sle, "ExpressionType", loc), GetOperatorExpressionTypeName (), loc)));
			binder_args.Add (new Argument (new TypeOf (ec.CurrentType, loc)));									
			binder_args.Add (new Argument (new ImplicitlyTypedArrayCreation (args.CreateDynamicBinderArguments (ec), loc)));

			return new Invocation (new MemberAccess (new TypeExpression (ec.Module.PredefinedTypes.Binder.TypeSpec, loc), "BinaryOperation", loc), binder_args);
		}
		
		public override Expression CreateExpressionTree (ResolveContext ec)
		{
			return CreateExpressionTree (ec, null);
		}

		Expression CreateExpressionTree (ResolveContext ec, Expression method)		
		{
			string method_name;
			bool lift_arg = false;
			
			switch (oper) {
			case Operator.Addition:
				if (method == null && ec.HasSet (ResolveContext.Options.CheckedScope) && !IsFloat (type))
					method_name = "AddChecked";
				else
					method_name = "Add";
				break;
			case Operator.BitwiseAnd:
				method_name = "And";
				break;
			case Operator.BitwiseOr:
				method_name = "Or";
				break;
			case Operator.Division:
				method_name = "Divide";
				break;
			case Operator.Equality:
				method_name = "Equal";
				lift_arg = true;
				break;
			case Operator.ExclusiveOr:
				method_name = "ExclusiveOr";
				break;				
			case Operator.GreaterThan:
				method_name = "GreaterThan";
				lift_arg = true;
				break;
			case Operator.GreaterThanOrEqual:
				method_name = "GreaterThanOrEqual";
				lift_arg = true;
				break;
			case Operator.Inequality:
				method_name = "NotEqual";
				lift_arg = true;
				break;
			case Operator.LeftShift:
				method_name = "LeftShift";
				break;
			case Operator.LessThan:
				method_name = "LessThan";
				lift_arg = true;
				break;
			case Operator.LessThanOrEqual:
				method_name = "LessThanOrEqual";
				lift_arg = true;
				break;
			case Operator.LogicalAnd:
				method_name = "AndAlso";
				break;
			case Operator.LogicalOr:
				method_name = "OrElse";
				break;
			case Operator.Modulus:
				method_name = "Modulo";
				break;
			case Operator.Multiply:
				if (method == null && ec.HasSet (ResolveContext.Options.CheckedScope) && !IsFloat (type))
					method_name = "MultiplyChecked";
				else
					method_name = "Multiply";
				break;
			case Operator.RightShift:
				method_name = "RightShift";
				break;
			case Operator.Subtraction:
				if (method == null && ec.HasSet (ResolveContext.Options.CheckedScope) && !IsFloat (type))
					method_name = "SubtractChecked";
				else
					method_name = "Subtract";
				break;

			default:
				throw new InternalErrorException ("Unknown expression tree binary operator " + oper);
			}

			Arguments args = new Arguments (2);
			args.Add (new Argument (left.CreateExpressionTree (ec)));
			args.Add (new Argument (right.CreateExpressionTree (ec)));
			if (method != null) {
				if (lift_arg)
					args.Add (new Argument (new BoolLiteral (ec.BuiltinTypes, false, loc)));

				args.Add (new Argument (method));
			}
			
			return CreateExpressionFactoryCall (ec, method_name, args);
		}
		
		public override object Accept (StructuralVisitor visitor)
		{
			return visitor.Visit (this);
		}

	}
	
	//
	// Represents the operation a + b [+ c [+ d [+ ...]]], where a is a string
	// b, c, d... may be strings or objects.
	//
	public class StringConcat : Expression
	{
		Arguments arguments;
		
		StringConcat (Location loc)
		{
			this.loc = loc;
			arguments = new Arguments (2);
		}

		public override bool ContainsEmitWithAwait ()
		{
			return arguments.ContainsEmitWithAwait ();
		}

		public static StringConcat Create (ResolveContext rc, Expression left, Expression right, Location loc)
		{
			if (left.eclass == ExprClass.Unresolved || right.eclass == ExprClass.Unresolved)
				throw new ArgumentException ();

			var s = new StringConcat (loc);
			s.type = rc.BuiltinTypes.String;
			s.eclass = ExprClass.Value;

			s.Append (rc, left);
			s.Append (rc, right);
			return s;
		}

		public override Expression CreateExpressionTree (ResolveContext ec)
		{
			Argument arg = arguments [0];
			return CreateExpressionAddCall (ec, arg, arg.CreateExpressionTree (ec), 1);
		}

		//
		// Creates nested calls tree from an array of arguments used for IL emit
		//
		Expression CreateExpressionAddCall (ResolveContext ec, Argument left, Expression left_etree, int pos)
		{
			Arguments concat_args = new Arguments (2);
			Arguments add_args = new Arguments (3);

			concat_args.Add (left);
			add_args.Add (new Argument (left_etree));

			concat_args.Add (arguments [pos]);
			add_args.Add (new Argument (arguments [pos].CreateExpressionTree (ec)));

			var methods = GetConcatMethodCandidates ();
			if (methods == null)
				return null;

			var res = new OverloadResolver (methods, OverloadResolver.Restrictions.NoBaseMembers, loc);
			var method = res.ResolveMember<MethodSpec> (ec, ref concat_args);
			if (method == null)
				return null;

			add_args.Add (new Argument (new TypeOfMethod (method, loc)));

			Expression expr = CreateExpressionFactoryCall (ec, "Add", add_args);
			if (++pos == arguments.Count)
				return expr;

			left = new Argument (new EmptyExpression (method.ReturnType));
			return CreateExpressionAddCall (ec, left, expr, pos);
		}

		protected override Expression DoResolve (ResolveContext ec)
		{
			return this;
		}
		
		void Append (ResolveContext rc, Expression operand)
		{
			//
			// Constant folding
			//
			StringConstant sc = operand as StringConstant;
			if (sc != null) {
				if (arguments.Count != 0) {
					Argument last_argument = arguments [arguments.Count - 1];
					StringConstant last_expr_constant = last_argument.Expr as StringConstant;
					if (last_expr_constant != null) {
						last_argument.Expr = new StringConstant (rc.BuiltinTypes, last_expr_constant.Value + sc.Value, sc.Location);
						return;
					}
				}
			} else {
				//
				// Multiple (3+) concatenation are resolved as multiple StringConcat instances
				//
				StringConcat concat_oper = operand as StringConcat;
				if (concat_oper != null) {
					arguments.AddRange (concat_oper.arguments);
					return;
				}
			}

			arguments.Add (new Argument (operand));
		}

		IList<MemberSpec> GetConcatMethodCandidates ()
		{
			return MemberCache.FindMembers (type, "Concat", true);
		}

		public override void Emit (EmitContext ec)
		{
			var members = GetConcatMethodCandidates ();
			var res = new OverloadResolver (members, OverloadResolver.Restrictions.NoBaseMembers, loc);
			var method = res.ResolveMember<MethodSpec> (new ResolveContext (ec.MemberContext), ref arguments);
			if (method != null) {
				var call = new CallEmitter ();
				call.EmitPredefined (ec, method, arguments);
			}
		}

		public override SLE.Expression MakeExpression (BuilderContext ctx)
		{
			if (arguments.Count != 2)
				throw new NotImplementedException ("arguments.Count != 2");

			var concat = typeof (string).GetMethod ("Concat", new[] { typeof (object), typeof (object) });
			return SLE.Expression.Add (arguments[0].Expr.MakeExpression (ctx), arguments[1].Expr.MakeExpression (ctx), concat);
		}
	}

	//
	// User-defined conditional logical operator
	//
	public class ConditionalLogicalOperator : UserOperatorCall
	{
		readonly bool is_and;
		Expression oper_expr;

		public ConditionalLogicalOperator (MethodSpec oper, Arguments arguments, Func<ResolveContext, Expression, Expression> expr_tree, bool is_and, Location loc)
			: base (oper, arguments, expr_tree, loc)
		{
			this.is_and = is_and;
			eclass = ExprClass.Unresolved;
		}
		
		protected override Expression DoResolve (ResolveContext ec)
		{
			AParametersCollection pd = oper.Parameters;
			if (!TypeSpecComparer.IsEqual (type, pd.Types[0]) || !TypeSpecComparer.IsEqual (type, pd.Types[1])) {
				ec.Report.Error (217, loc,
					"A user-defined operator `{0}' must have parameters and return values of the same type in order to be applicable as a short circuit operator",
					oper.GetSignatureForError ());
				return null;
			}

			Expression left_dup = new EmptyExpression (type);
			Expression op_true = GetOperatorTrue (ec, left_dup, loc);
			Expression op_false = GetOperatorFalse (ec, left_dup, loc);
			if (op_true == null || op_false == null) {
				ec.Report.Error (218, loc,
					"The type `{0}' must have operator `true' and operator `false' defined when `{1}' is used as a short circuit operator",
					TypeManager.CSharpName (type), oper.GetSignatureForError ());
				return null;
			}

			oper_expr = is_and ? op_false : op_true;
			eclass = ExprClass.Value;
			return this;
		}

		public override void Emit (EmitContext ec)
		{
			Label end_target = ec.DefineLabel ();

			//
			// Emit and duplicate left argument
			//
			bool right_contains_await = ec.HasSet (BuilderContext.Options.AsyncBody) && arguments[1].Expr.ContainsEmitWithAwait ();
			if (right_contains_await) {
				arguments[0] = arguments[0].EmitToField (ec, false);
				arguments[0].Expr.Emit (ec);
			} else {
				arguments[0].Expr.Emit (ec);
				ec.Emit (OpCodes.Dup);
				arguments.RemoveAt (0);
			}

			oper_expr.EmitBranchable (ec, end_target, true);

			base.Emit (ec);

			if (right_contains_await) {
				//
				// Special handling when right expression contains await and left argument
				// could not be left on stack before logical branch
				//
				Label skip_left_load = ec.DefineLabel ();
				ec.Emit (OpCodes.Br_S, skip_left_load);
				ec.MarkLabel (end_target);
				arguments[0].Expr.Emit (ec);
				ec.MarkLabel (skip_left_load);
			} else {
				ec.MarkLabel (end_target);
			}
		}
	}

	public class PointerArithmetic : Expression {
		Expression left, right;
		Binary.Operator op;

		//
		// We assume that `l' is always a pointer
		//
		public PointerArithmetic (Binary.Operator op, Expression l, Expression r, TypeSpec t, Location loc)
		{
			type = t;
			this.loc = loc;
			left = l;
			right = r;
			this.op = op;
		}

		public override bool ContainsEmitWithAwait ()
		{
			throw new NotImplementedException ();
		}

		public override Expression CreateExpressionTree (ResolveContext ec)
		{
			Error_PointerInsideExpressionTree (ec);
			return null;
		}

		protected override Expression DoResolve (ResolveContext ec)
		{
			eclass = ExprClass.Variable;

			var pc = left.Type as PointerContainer;
			if (pc != null && pc.Element.Kind == MemberKind.Void) {
				Error_VoidPointerOperation (ec);
				return null;
			}
			
			return this;
		}

		public override void Emit (EmitContext ec)
		{
			TypeSpec op_type = left.Type;
			
			// It must be either array or fixed buffer
			TypeSpec element;
			if (TypeManager.HasElementType (op_type)) {
				element = TypeManager.GetElementType (op_type);
			} else {
				FieldExpr fe = left as FieldExpr;
				if (fe != null)
					element = ((FixedFieldSpec) (fe.Spec)).ElementType;
				else
					element = op_type;
			}

			int size = BuiltinTypeSpec.GetSize(element);
			TypeSpec rtype = right.Type;
			
			if ((op & Binary.Operator.SubtractionMask) != 0 && rtype.IsPointer){
				//
				// handle (pointer - pointer)
				//
				left.Emit (ec);
				right.Emit (ec);
				ec.Emit (OpCodes.Sub);

				if (size != 1){
					if (size == 0)
						ec.Emit (OpCodes.Sizeof, element);
					else 
						ec.EmitInt (size);
					ec.Emit (OpCodes.Div);
				}
				ec.Emit (OpCodes.Conv_I8);
			} else {
				//
				// handle + and - on (pointer op int)
				//
				Constant left_const = left as Constant;
				if (left_const != null) {
					//
					// Optimize ((T*)null) pointer operations
					//
					if (left_const.IsDefaultValue) {
						left = EmptyExpression.Null;
					} else {
						left_const = null;
					}
				}

				left.Emit (ec);

				var right_const = right as Constant;
				if (right_const != null) {
					//
					// Optimize 0-based arithmetic
					//
					if (right_const.IsDefaultValue)
						return;

					if (size != 0)
						right = new IntConstant (ec.BuiltinTypes, size, right.Location);
					else
						right = new SizeOf (new TypeExpression (element, right.Location), right.Location);
					
					// TODO: Should be the checks resolve context sensitive?
					ResolveContext rc = new ResolveContext (ec.MemberContext, ResolveContext.Options.UnsafeScope);
					right = new Binary (Binary.Operator.Multiply, right, right_const, loc).Resolve (rc);
					if (right == null)
						return;
				}

				right.Emit (ec);
				switch (rtype.BuiltinType) {
				case BuiltinTypeSpec.Type.SByte:
				case BuiltinTypeSpec.Type.Byte:
				case BuiltinTypeSpec.Type.Short:
				case BuiltinTypeSpec.Type.UShort:
					ec.Emit (OpCodes.Conv_I);
					break;
				case BuiltinTypeSpec.Type.UInt:
					ec.Emit (OpCodes.Conv_U);
					break;
				}

				if (right_const == null && size != 1){
					if (size == 0)
						ec.Emit (OpCodes.Sizeof, element);
					else 
						ec.EmitInt (size);
					if (rtype.BuiltinType == BuiltinTypeSpec.Type.Long || rtype.BuiltinType == BuiltinTypeSpec.Type.ULong)
						ec.Emit (OpCodes.Conv_I8);

					Binary.EmitOperatorOpcode (ec, Binary.Operator.Multiply, rtype);
				}

				if (left_const == null) {
					if (rtype.BuiltinType == BuiltinTypeSpec.Type.Long)
						ec.Emit (OpCodes.Conv_I);
					else if (rtype.BuiltinType == BuiltinTypeSpec.Type.ULong)
						ec.Emit (OpCodes.Conv_U);

					Binary.EmitOperatorOpcode (ec, op, op_type);
				}
			}
		}
	}

	//
	// A boolean-expression is an expression that yields a result
	// of type bool
	//
	public class BooleanExpression : ShimExpression
	{
		public BooleanExpression (Expression expr)
			: base (expr)
		{
			this.loc = expr.Location;
		}

		public override Expression CreateExpressionTree (ResolveContext ec)
		{
			// TODO: We should emit IsTrue (v4) instead of direct user operator
			// call but that would break csc compatibility
			return base.CreateExpressionTree (ec);
		}

		protected override Expression DoResolve (ResolveContext ec)
		{
			// A boolean-expression is required to be of a type
			// that can be implicitly converted to bool or of
			// a type that implements operator true

			expr = expr.Resolve (ec);
			if (expr == null)
				return null;

			Assign ass = expr as Assign;
			if (ass != null && ass.Source is Constant) {
				ec.Report.Warning (665, 3, loc,
					"Assignment in conditional expression is always constant. Did you mean to use `==' instead ?");
			}

			if (expr.Type.BuiltinType == BuiltinTypeSpec.Type.Bool)
				return expr;

			if (expr.Type.BuiltinType == BuiltinTypeSpec.Type.Dynamic) {
				Arguments args = new Arguments (1);
				args.Add (new Argument (expr));
				return DynamicUnaryConversion.CreateIsTrue (ec, args, loc).Resolve (ec);
			}

			type = ec.BuiltinTypes.Bool;
			Expression converted = Convert.ImplicitConversion (ec, expr, type, loc);
			if (converted != null)
				return converted;

			//
			// If no implicit conversion to bool exists, try using `operator true'
			//
			converted = GetOperatorTrue (ec, expr, loc);
			if (converted == null) {
				expr.Error_ValueCannotBeConverted (ec, type, false);
				return null;
			}

			return converted;
		}
		
		public override object Accept (StructuralVisitor visitor)
		{
			return visitor.Visit (this);
		}
	}

	public class BooleanExpressionFalse : Unary
	{
		public BooleanExpressionFalse (Expression expr)
			: base (Operator.LogicalNot, expr, expr.Location)
		{
		}

		protected override Expression ResolveOperator (ResolveContext ec, Expression expr)
		{
			return GetOperatorFalse (ec, expr, loc) ?? base.ResolveOperator (ec, expr);
		}
	}
	
	/// <summary>
	///   Implements the ternary conditional operator (?:)
	/// </summary>
	public class Conditional : Expression {
		Expression expr, true_expr, false_expr;

		public Conditional (Expression expr, Expression true_expr, Expression false_expr, Location loc)
		{
			this.expr = expr;
			this.true_expr = true_expr;
			this.false_expr = false_expr;
			this.loc = loc;
		}

		#region Properties

		public Expression Expr {
			get {
				return expr;
			}
		}

		public Expression TrueExpr {
			get {
				return true_expr;
			}
		}

		public Expression FalseExpr {
			get {
				return false_expr;
			}
		}

		#endregion

		public override bool ContainsEmitWithAwait ()
		{
			return Expr.ContainsEmitWithAwait () || true_expr.ContainsEmitWithAwait () || false_expr.ContainsEmitWithAwait ();
		}

		public override Expression CreateExpressionTree (ResolveContext ec)
		{
			Arguments args = new Arguments (3);
			args.Add (new Argument (expr.CreateExpressionTree (ec)));
			args.Add (new Argument (true_expr.CreateExpressionTree (ec)));
			args.Add (new Argument (false_expr.CreateExpressionTree (ec)));
			return CreateExpressionFactoryCall (ec, "Condition", args);
		}

		protected override Expression DoResolve (ResolveContext ec)
		{
			expr = expr.Resolve (ec);

			//
			// Unreachable code needs different resolve path. For instance for await
			// expression to not generate unreachable resumable statement
			//
			Constant c = expr as Constant;
			if (c != null && ec.CurrentBranching != null) {
				bool unreachable = ec.CurrentBranching.CurrentUsageVector.IsUnreachable;

				if (c.IsDefaultValue) {
					ec.CurrentBranching.CurrentUsageVector.IsUnreachable = true;
					true_expr = true_expr.Resolve (ec);
					ec.CurrentBranching.CurrentUsageVector.IsUnreachable = unreachable;

					false_expr = false_expr.Resolve (ec);
				} else {
					true_expr = true_expr.Resolve (ec);

					ec.CurrentBranching.CurrentUsageVector.IsUnreachable = true;
					false_expr = false_expr.Resolve (ec);
					ec.CurrentBranching.CurrentUsageVector.IsUnreachable = unreachable;
				}
			} else {
				true_expr = true_expr.Resolve (ec);
				false_expr = false_expr.Resolve (ec);
			}

			if (true_expr == null || false_expr == null || expr == null)
				return null;

			eclass = ExprClass.Value;
			TypeSpec true_type = true_expr.Type;
			TypeSpec false_type = false_expr.Type;
			type = true_type;

			//
			// First, if an implicit conversion exists from true_expr
			// to false_expr, then the result type is of type false_expr.Type
			//
			if (!TypeSpecComparer.IsEqual (true_type, false_type)) {
				Expression conv = Convert.ImplicitConversion (ec, true_expr, false_type, loc);
				if (conv != null && true_type.BuiltinType != BuiltinTypeSpec.Type.Dynamic) {
					//
					// Check if both can convert implicitly to each other's type
					//
					type = false_type;

					if (false_type.BuiltinType != BuiltinTypeSpec.Type.Dynamic && Convert.ImplicitConversion (ec, false_expr, true_type, loc) != null) {
						ec.Report.Error (172, true_expr.Location,
							"Type of conditional expression cannot be determined as `{0}' and `{1}' convert implicitly to each other",
								true_type.GetSignatureForError (), false_type.GetSignatureForError ());
						return null;
					}

					true_expr = conv;
				} else if ((conv = Convert.ImplicitConversion (ec, false_expr, true_type, loc)) != null) {
					false_expr = conv;
				} else {
					ec.Report.Error (173, true_expr.Location,
						"Type of conditional expression cannot be determined because there is no implicit conversion between `{0}' and `{1}'",
						TypeManager.CSharpName (true_type), TypeManager.CSharpName (false_type));
					return null;
				}
			}			

			if (c != null) {
				bool is_false = c.IsDefaultValue;

				//
				// Don't issue the warning for constant expressions
				//
				if (!(is_false ? true_expr is Constant : false_expr is Constant)) {
					ec.Report.Warning (429, 4, is_false ? true_expr.Location : false_expr.Location,
						"Unreachable expression code detected");
				}

				return ReducedExpression.Create (
					is_false ? false_expr : true_expr, this,
					false_expr is Constant && true_expr is Constant).Resolve (ec);
			}

			return this;
		}

		public override void Emit (EmitContext ec)
		{
			Label false_target = ec.DefineLabel ();
			Label end_target = ec.DefineLabel ();

			expr.EmitBranchable (ec, false_target, false);
			true_expr.Emit (ec);

			ec.Emit (OpCodes.Br, end_target);
			ec.MarkLabel (false_target);
			false_expr.Emit (ec);
			ec.MarkLabel (end_target);
		}

		protected override void CloneTo (CloneContext clonectx, Expression t)
		{
			Conditional target = (Conditional) t;

			target.expr = expr.Clone (clonectx);
			target.true_expr = true_expr.Clone (clonectx);
			target.false_expr = false_expr.Clone (clonectx);
		}
		
		public override object Accept (StructuralVisitor visitor)
		{
			return visitor.Visit (this);
		}
	}

	public abstract class VariableReference : Expression, IAssignMethod, IMemoryLocation, IVariableReference
	{
		LocalTemporary temp;

		#region Abstract
		public abstract HoistedVariable GetHoistedVariable (AnonymousExpression ae);
		public abstract void SetHasAddressTaken ();
		public abstract void VerifyAssigned (ResolveContext rc);

		public abstract bool IsLockedByStatement { get; set; }

		public abstract bool IsFixed { get; }
		public abstract bool IsRef { get; }
		public abstract string Name { get; }

		//
		// Variable IL data, it has to be protected to encapsulate hoisted variables
		//
		protected abstract ILocalVariable Variable { get; }
		
		//
		// Variable flow-analysis data
		//
		public abstract VariableInfo VariableInfo { get; }
		#endregion

		public virtual void AddressOf (EmitContext ec, AddressOp mode)
		{
			HoistedVariable hv = GetHoistedVariable (ec);
			if (hv != null) {
				hv.AddressOf (ec, mode);
				return;
			}

			Variable.EmitAddressOf (ec);
		}

		public override bool ContainsEmitWithAwait ()
		{
			return false;
		}

		public override Expression CreateExpressionTree (ResolveContext ec)
		{
			HoistedVariable hv = GetHoistedVariable (ec);
			if (hv != null)
				return hv.CreateExpressionTree ();

			Arguments arg = new Arguments (1);
			arg.Add (new Argument (this));
			return CreateExpressionFactoryCall (ec, "Constant", arg);
		}

		public override Expression DoResolveLValue (ResolveContext rc, Expression right_side)
		{
			if (IsLockedByStatement) {
				rc.Report.Warning (728, 2, loc,
					"Possibly incorrect assignment to `{0}' which is the argument to a using or lock statement",
					Name);
			}

			return this;
		}

		public override void Emit (EmitContext ec)
		{
			Emit (ec, false);
		}

		public override void EmitSideEffect (EmitContext ec)
		{
			// do nothing
		}

		//
		// This method is used by parameters that are references, that are
		// being passed as references:  we only want to pass the pointer (that
		// is already stored in the parameter, not the address of the pointer,
		// and not the value of the variable).
		//
		public void EmitLoad (EmitContext ec)
		{
			Variable.Emit (ec);
		}

		public void Emit (EmitContext ec, bool leave_copy)
		{
			HoistedVariable hv = GetHoistedVariable (ec);
			if (hv != null) {
				hv.Emit (ec, leave_copy);
				return;
			}

			EmitLoad (ec);

			if (IsRef) {
				//
				// If we are a reference, we loaded on the stack a pointer
				// Now lets load the real value
				//
				ec.EmitLoadFromPtr (type);
			}

			if (leave_copy) {
				ec.Emit (OpCodes.Dup);

				if (IsRef) {
					temp = new LocalTemporary (Type);
					temp.Store (ec);
				}
			}
		}

		public void EmitAssign (EmitContext ec, Expression source, bool leave_copy,
					bool prepare_for_load)
		{
			HoistedVariable hv = GetHoistedVariable (ec);
			if (hv != null) {
				hv.EmitAssign (ec, source, leave_copy, prepare_for_load);
				return;
			}

			New n_source = source as New;
			if (n_source != null) {
				if (!n_source.Emit (ec, this)) {
					if (leave_copy) {
						EmitLoad (ec);
						if (IsRef)
							ec.EmitLoadFromPtr (type);
					}
					return;
				}
			} else {
				if (IsRef)
					EmitLoad (ec);

				source.Emit (ec);
			}

			if (leave_copy) {
				ec.Emit (OpCodes.Dup);
				if (IsRef) {
					temp = new LocalTemporary (Type);
					temp.Store (ec);
				}
			}

			if (IsRef)
				ec.EmitStoreFromPtr (type);
			else
				Variable.EmitAssign (ec);

			if (temp != null) {
				temp.Emit (ec);
				temp.Release (ec);
			}
		}

		public override Expression EmitToField (EmitContext ec)
		{
			HoistedVariable hv = GetHoistedVariable (ec);
			if (hv != null) {
				return hv.EmitToField (ec);
			}

			return base.EmitToField (ec);
		}

		public HoistedVariable GetHoistedVariable (ResolveContext rc)
		{
			return GetHoistedVariable (rc.CurrentAnonymousMethod);
		}

		public HoistedVariable GetHoistedVariable (EmitContext ec)
		{
			return GetHoistedVariable (ec.CurrentAnonymousMethod);
		}

		public override string GetSignatureForError ()
		{
			return Name;
		}

		public bool IsHoisted {
			get { return GetHoistedVariable ((AnonymousExpression) null) != null; }
		}
	}

	//
	// Resolved reference to a local variable
	//
	public class LocalVariableReference : VariableReference
	{
		public LocalVariable local_info;

		public LocalVariableReference (LocalVariable li, Location l)
		{
			this.local_info = li;
			loc = l;
		}

		public override VariableInfo VariableInfo {
			get { return local_info.VariableInfo; }
		}

		public override HoistedVariable GetHoistedVariable (AnonymousExpression ae)
		{
			return local_info.HoistedVariant;
		}

		#region Properties

		//		
		// A local variable is always fixed
		//
		public override bool IsFixed {
			get {
				return true;
			}
		}

		public override bool IsLockedByStatement {
			get {
				return local_info.IsLocked;
			}
			set {
				local_info.IsLocked = value;
			}
		}

		public override bool IsRef {
			get { return false; }
		}

		public override string Name {
			get { return local_info.Name; }
		}

		#endregion

		public override void VerifyAssigned (ResolveContext rc)
		{
			VariableInfo variable_info = local_info.VariableInfo;
			if (variable_info == null)
				return;

			if (variable_info.IsAssigned (rc))
				return;

			rc.Report.Error (165, loc, "Use of unassigned local variable `{0}'", Name);
			variable_info.SetAssigned (rc);
		}

		public override void SetHasAddressTaken ()
		{
			local_info.SetHasAddressTaken ();
		}

		void DoResolveBase (ResolveContext ec)
		{
			//
			// If we are referencing a variable from the external block
			// flag it for capturing
			//
			if (ec.MustCaptureVariable (local_info)) {
				if (local_info.AddressTaken) {
					AnonymousMethodExpression.Error_AddressOfCapturedVar (ec, this, loc);
				} else if (local_info.IsFixed) {
					ec.Report.Error (1764, loc,
						"Cannot use fixed local `{0}' inside an anonymous method, lambda expression or query expression",
						GetSignatureForError ());
				}

				if (ec.IsVariableCapturingRequired) {
					AnonymousMethodStorey storey = local_info.Block.Explicit.CreateAnonymousMethodStorey (ec);
					storey.CaptureLocalVariable (ec, local_info);
				}
			}

			eclass = ExprClass.Variable;
			type = local_info.Type;
		}

		protected override Expression DoResolve (ResolveContext ec)
		{
			local_info.SetIsUsed ();

			VerifyAssigned (ec);

			DoResolveBase (ec);
			return this;
		}

		public override Expression DoResolveLValue (ResolveContext ec, Expression rhs)
		{
			//
			// Don't be too pedantic when variable is used as out param or for some broken code
			// which uses property/indexer access to run some initialization
			//
			if (rhs == EmptyExpression.OutAccess || rhs.eclass == ExprClass.PropertyAccess || rhs.eclass == ExprClass.IndexerAccess)
				local_info.SetIsUsed ();

			if (local_info.IsReadonly && !ec.HasAny (ResolveContext.Options.FieldInitializerScope | ResolveContext.Options.UsingInitializerScope)) {
				int code;
				string msg;
				if (rhs == EmptyExpression.OutAccess) {
					code = 1657; msg = "Cannot pass `{0}' as a ref or out argument because it is a `{1}'";
				} else if (rhs == EmptyExpression.LValueMemberAccess) {
					code = 1654; msg = "Cannot assign to members of `{0}' because it is a `{1}'";
				} else if (rhs == EmptyExpression.LValueMemberOutAccess) {
					code = 1655; msg = "Cannot pass members of `{0}' as ref or out arguments because it is a `{1}'";
				} else if (rhs == EmptyExpression.UnaryAddress) {
					code = 459; msg = "Cannot take the address of {1} `{0}'";
				} else {
					code = 1656; msg = "Cannot assign to `{0}' because it is a `{1}'";
				}
				ec.Report.Error (code, loc, msg, Name, local_info.GetReadOnlyContext ());
			} else if (VariableInfo != null) {
				VariableInfo.SetAssigned (ec);
			}

			if (eclass == ExprClass.Unresolved)
				DoResolveBase (ec);

			return base.DoResolveLValue (ec, rhs);
		}

		public override int GetHashCode ()
		{
			return local_info.GetHashCode ();
		}

		public override bool Equals (object obj)
		{
			LocalVariableReference lvr = obj as LocalVariableReference;
			if (lvr == null)
				return false;

			return local_info == lvr.local_info;
		}

		protected override ILocalVariable Variable {
			get { return local_info; }
		}

		public override string ToString ()
		{
			return String.Format ("{0} ({1}:{2})", GetType (), Name, loc);
		}

		protected override void CloneTo (CloneContext clonectx, Expression t)
		{
			// Nothing
		}
		
		public override object Accept (StructuralVisitor visitor)
		{
			return visitor.Visit (this);
		}
	}

	/// <summary>
	///   This represents a reference to a parameter in the intermediate
	///   representation.
	/// </summary>
	public class ParameterReference : VariableReference
	{
		protected ParametersBlock.ParameterInfo pi;

		public ParameterReference (ParametersBlock.ParameterInfo pi, Location loc)
		{
			this.pi = pi;
			this.loc = loc;
		}

		#region Properties

		public override bool IsLockedByStatement {
			get {
				return pi.IsLocked;
			}
			set	{
				pi.IsLocked = value;
			}
		}

		public override bool IsRef {
			get { return (pi.Parameter.ModFlags & Parameter.Modifier.RefOutMask) != 0; }
		}

		bool HasOutModifier {
			get { return (pi.Parameter.ModFlags & Parameter.Modifier.OUT) != 0; }
		}

		public override HoistedVariable GetHoistedVariable (AnonymousExpression ae)
		{
			return pi.Parameter.HoistedVariant;
		}

		//
		// A ref or out parameter is classified as a moveable variable, even 
		// if the argument given for the parameter is a fixed variable
		//		
		public override bool IsFixed {
			get { return !IsRef; }
		}

		public override string Name {
			get { return Parameter.Name; }
		}

		public Parameter Parameter {
			get { return pi.Parameter; }
		}

		public override VariableInfo VariableInfo {
			get { return pi.VariableInfo; }
		}

		protected override ILocalVariable Variable {
			get { return Parameter; }
		}

		#endregion

		public override void AddressOf (EmitContext ec, AddressOp mode)
		{
			//
			// ParameterReferences might already be a reference
			//
			if (IsRef) {
				EmitLoad (ec);
				return;
			}

			base.AddressOf (ec, mode);
		}

		public override void SetHasAddressTaken ()
		{
			Parameter.HasAddressTaken = true;
		}

		void SetAssigned (ResolveContext ec)
		{
			if (HasOutModifier && ec.DoFlowAnalysis)
				ec.CurrentBranching.SetAssigned (VariableInfo);
		}

		bool DoResolveBase (ResolveContext ec)
		{
			if (eclass != ExprClass.Unresolved)
				return true;

			type = pi.ParameterType;
			eclass = ExprClass.Variable;

			//
			// If we are referencing a parameter from the external block
			// flag it for capturing
			//
			if (ec.MustCaptureVariable (pi)) {
				if (Parameter.HasAddressTaken)
					AnonymousMethodExpression.Error_AddressOfCapturedVar (ec, this, loc);

				if (IsRef) {
					ec.Report.Error (1628, loc,
						"Parameter `{0}' cannot be used inside `{1}' when using `ref' or `out' modifier",
						Name, ec.CurrentAnonymousMethod.ContainerType);
				}

				if (ec.IsVariableCapturingRequired && !pi.Block.ParametersBlock.IsExpressionTree) {
					AnonymousMethodStorey storey = pi.Block.Explicit.CreateAnonymousMethodStorey (ec);
					storey.CaptureParameter (ec, pi, this);
				}
			}

			return true;
		}

		public override int GetHashCode ()
		{
			return Name.GetHashCode ();
		}

		public override bool Equals (object obj)
		{
			ParameterReference pr = obj as ParameterReference;
			if (pr == null)
				return false;

			return Name == pr.Name;
		}
	
		protected override void CloneTo (CloneContext clonectx, Expression target)
		{
			// Nothing to clone
			return;
		}

		public override Expression CreateExpressionTree (ResolveContext ec)
		{
			HoistedVariable hv = GetHoistedVariable (ec);
			if (hv != null)
				return hv.CreateExpressionTree ();

			return Parameter.ExpressionTreeVariableReference ();
		}

		//
		// Notice that for ref/out parameters, the type exposed is not the
		// same type exposed externally.
		//
		// for "ref int a":
		//   externally we expose "int&"
		//   here we expose       "int".
		//
		// We record this in "is_ref".  This means that the type system can treat
		// the type as it is expected, but when we generate the code, we generate
		// the alternate kind of code.
		//
		protected override Expression DoResolve (ResolveContext ec)
		{
			if (!DoResolveBase (ec))
				return null;

			VerifyAssigned (ec);
			return this;
		}

		public override Expression DoResolveLValue (ResolveContext ec, Expression right_side)
		{
			if (!DoResolveBase (ec))
				return null;

			SetAssigned (ec);
			return base.DoResolveLValue (ec, right_side);
		}

		public override void VerifyAssigned (ResolveContext rc)
		{
			// HACK: Variables are not captured in probing mode
			if (rc.IsInProbingMode)
				return;

			if (HasOutModifier && !VariableInfo.IsAssigned (rc)) {
				rc.Report.Error (269, loc, "Use of unassigned out parameter `{0}'", Name);
			}
		}
	}
	
	/// <summary>
	///   Invocation of methods or delegates.
	/// </summary>
	public class Invocation : ExpressionStatement
	{
		protected Arguments arguments;
		protected Expression expr;
		protected MethodGroupExpr mg;

		//
		// arguments is an ArrayList, but we do not want to typecast,
		// as it might be null.
		//
		public Invocation (Expression expr, Arguments arguments)
		{
			this.expr = expr;		
			this.arguments = arguments;
			if (expr != null)
				loc = expr.Location;
		}

		#region Properties
		public Arguments Arguments {
			get {
				return arguments;
			}
		}
		
		public Expression Exp {
			get {
				return expr;
			}
		}

		public MethodGroupExpr MethodGroup {
			get {
				return mg;
			}
		}
		#endregion

		protected override void CloneTo (CloneContext clonectx, Expression t)
		{
			Invocation target = (Invocation) t;

			if (arguments != null)
				target.arguments = arguments.Clone (clonectx);

			target.expr = expr.Clone (clonectx);
		}

		public override bool ContainsEmitWithAwait ()
		{
			if (arguments != null && arguments.ContainsEmitWithAwait ())
				return true;

			return mg.ContainsEmitWithAwait ();
		}

		public override Expression CreateExpressionTree (ResolveContext ec)
		{
			Expression instance = mg.IsInstance ?
				mg.InstanceExpression.CreateExpressionTree (ec) :
				new NullLiteral (loc);

			var args = Arguments.CreateForExpressionTree (ec, arguments,
				instance,
				mg.CreateExpressionTree (ec));

			return CreateExpressionFactoryCall (ec, "Call", args);
		}

		protected override Expression DoResolve (ResolveContext ec)
		{
			Expression member_expr;
			var atn = expr as ATypeNameExpression;
			if (atn != null) {
				member_expr = atn.LookupNameExpression (ec, MemberLookupRestrictions.InvocableOnly | MemberLookupRestrictions.ReadAccess);
				if (member_expr != null)
					member_expr = member_expr.Resolve (ec);
			} else {
				member_expr = expr.Resolve (ec, ResolveFlags.VariableOrValue | ResolveFlags.MethodGroup);
			}

			if (member_expr == null)
				return null;

			//
			// Next, evaluate all the expressions in the argument list
			//
			bool dynamic_arg = false;
			if (arguments != null)
				arguments.Resolve (ec, out dynamic_arg);

			TypeSpec expr_type = member_expr.Type;
			if (expr_type.BuiltinType == BuiltinTypeSpec.Type.Dynamic)
				return DoResolveDynamic (ec, member_expr);

			mg = member_expr as MethodGroupExpr;
			Expression invoke = null;

			if (mg == null) {
				if (expr_type != null && expr_type.IsDelegate) {
					invoke = new DelegateInvocation (member_expr, arguments, loc);
					invoke = invoke.Resolve (ec);
					if (invoke == null || !dynamic_arg)
						return invoke;
				} else {
					if (member_expr is RuntimeValueExpression) {
						ec.Report.Error (Report.RuntimeErrorId, loc, "Cannot invoke a non-delegate type `{0}'",
							member_expr.Type.GetSignatureForError ()); ;
						return null;
					}

					MemberExpr me = member_expr as MemberExpr;
					if (me == null) {
						member_expr.Error_UnexpectedKind (ec, ResolveFlags.MethodGroup, loc);
						return null;
					}

					ec.Report.Error (1955, loc, "The member `{0}' cannot be used as method or delegate",
							member_expr.GetSignatureForError ());
					return null;
				}
			}

			if (invoke == null) {
				mg = DoResolveOverload (ec);
				if (mg == null)
					return null;
			}

			if (dynamic_arg)
				return DoResolveDynamic (ec, member_expr);

			var method = mg.BestCandidate;
			type = mg.BestCandidateReturnType;
		
			if (arguments == null && method.DeclaringType.BuiltinType == BuiltinTypeSpec.Type.Object && method.Name == Destructor.MetadataName) {
				if (mg.IsBase)
					ec.Report.Error (250, loc, "Do not directly call your base class Finalize method. It is called automatically from your destructor");
				else
					ec.Report.Error (245, loc, "Destructors and object.Finalize cannot be called directly. Consider calling IDisposable.Dispose if available");
				return null;
			}

			IsSpecialMethodInvocation (ec, method, loc);
			
			eclass = ExprClass.Value;
			return this;
		}

		protected virtual Expression DoResolveDynamic (ResolveContext ec, Expression memberExpr)
		{
			Arguments args;
			DynamicMemberBinder dmb = memberExpr as DynamicMemberBinder;
			if (dmb != null) {
				args = dmb.Arguments;
				if (arguments != null)
					args.AddRange (arguments);
			} else if (mg == null) {
				if (arguments == null)
					args = new Arguments (1);
				else
					args = arguments;

				args.Insert (0, new Argument (memberExpr));
				this.expr = null;
			} else {
				if (mg.IsBase) {
					ec.Report.Error (1971, loc,
						"The base call to method `{0}' cannot be dynamically dispatched. Consider casting the dynamic arguments or eliminating the base access",
						mg.Name);
					return null;
				}

				if (arguments == null)
					args = new Arguments (1);
				else
					args = arguments;

				MemberAccess ma = expr as MemberAccess;
				if (ma != null) {
					var left_type = ma.LeftExpression as TypeExpr;
					if (left_type != null) {
						args.Insert (0, new Argument (new TypeOf (left_type.Type, loc).Resolve (ec), Argument.AType.DynamicTypeName));
					} else {
						//
						// Any value type has to be pass as by-ref to get back the same
						// instance on which the member was called
						//
						var mod = ma.LeftExpression is IMemoryLocation && TypeSpec.IsValueType (ma.LeftExpression.Type) ?
							Argument.AType.Ref : Argument.AType.None;
						args.Insert (0, new Argument (ma.LeftExpression.Resolve (ec), mod));
					}
				} else {	// is SimpleName
					if (ec.IsStatic) {
						args.Insert (0, new Argument (new TypeOf (ec.CurrentType, loc).Resolve (ec), Argument.AType.DynamicTypeName));
					} else {
						args.Insert (0, new Argument (new This (loc).Resolve (ec)));
					}
				}
			}

			return new DynamicInvocation (expr as ATypeNameExpression, args, loc).Resolve (ec);
		}

		protected virtual MethodGroupExpr DoResolveOverload (ResolveContext ec)
		{
			return mg.OverloadResolve (ec, ref arguments, null, OverloadResolver.Restrictions.None);
		}

		public override string GetSignatureForError ()
		{
			return mg.GetSignatureForError ();
		}

		//
		// If a member is a method or event, or if it is a constant, field or property of either a delegate type
		// or the type dynamic, then the member is invocable
		//
		public static bool IsMemberInvocable (MemberSpec member)
		{
			switch (member.Kind) {
			case MemberKind.Event:
				return true;
			case MemberKind.Field:
			case MemberKind.Property:
				var m = member as IInterfaceMemberSpec;
				return m.MemberType.IsDelegate || m.MemberType.BuiltinType == BuiltinTypeSpec.Type.Dynamic;
			default:
				return false;
			}
		}

		public static bool IsSpecialMethodInvocation (ResolveContext ec, MethodSpec method, Location loc)
		{
			if (!method.IsReservedMethod)
				return false;

			if (ec.HasSet (ResolveContext.Options.InvokeSpecialName) || ec.CurrentMemberDefinition.IsCompilerGenerated)
				return false;

			ec.Report.SymbolRelatedToPreviousError (method);
			ec.Report.Error (571, loc, "`{0}': cannot explicitly call operator or accessor",
				method.GetSignatureForError ());
	
			return true;
		}

		public override void Emit (EmitContext ec)
		{
			mg.EmitCall (ec, arguments);
		}
		
		public override void EmitStatement (EmitContext ec)
		{
			Emit (ec);

			// 
			// Pop the return value if there is one
			//
			if (type.Kind != MemberKind.Void)
				ec.Emit (OpCodes.Pop);
		}

		public override SLE.Expression MakeExpression (BuilderContext ctx)
		{
			return MakeExpression (ctx, mg.InstanceExpression, mg.BestCandidate, arguments);
		}

		public static SLE.Expression MakeExpression (BuilderContext ctx, Expression instance, MethodSpec mi, Arguments args)
		{
#if STATIC
			throw new NotSupportedException ();
#else
			var instance_expr = instance == null ? null : instance.MakeExpression (ctx);
			return SLE.Expression.Call (instance_expr, (MethodInfo) mi.GetMetaInfo (), Arguments.MakeExpression (args, ctx));
#endif
		}

		public override object Accept (StructuralVisitor visitor)
		{
			return visitor.Visit (this);
		}
	}

	//
	// Implements simple new expression 
	//
	public class New : ExpressionStatement, IMemoryLocation
	{
		protected Arguments arguments;

		//
		// During bootstrap, it contains the RequestedType,
		// but if `type' is not null, it *might* contain a NewDelegate
		// (because of field multi-initialization)
		//
		protected Expression RequestedType;

		protected MethodSpec method;

		public New (Expression requested_type, Arguments arguments, Location l)
		{
			RequestedType = requested_type;
			this.arguments = arguments;
			loc = l;
		}

		#region Properties
		public Arguments Arguments {
			get {
				return arguments;
			}
		}

		public Expression TypeRequested {
			get {
				return RequestedType;
			}
		}

		//
		// Returns true for resolved `new S()'
		//
		public bool IsDefaultStruct {
			get {
				return arguments == null && type.IsStruct && GetType () == typeof (New);
			}
		}

		public Expression TypeExpression {
			get {
				return RequestedType;
			}
		}

		#endregion

		/// <summary>
		/// Converts complex core type syntax like 'new int ()' to simple constant
		/// </summary>
		public static Constant Constantify (TypeSpec t, Location loc)
		{
			switch (t.BuiltinType) {
			case BuiltinTypeSpec.Type.Int:
				return new IntConstant (t, 0, loc);
			case BuiltinTypeSpec.Type.UInt:
				return new UIntConstant (t, 0, loc);
			case BuiltinTypeSpec.Type.Long:
				return new LongConstant (t, 0, loc);
			case BuiltinTypeSpec.Type.ULong:
				return new ULongConstant (t, 0, loc);
			case BuiltinTypeSpec.Type.Float:
				return new FloatConstant (t, 0, loc);
			case BuiltinTypeSpec.Type.Double:
				return new DoubleConstant (t, 0, loc);
			case BuiltinTypeSpec.Type.Short:
				return new ShortConstant (t, 0, loc);
			case BuiltinTypeSpec.Type.UShort:
				return new UShortConstant (t, 0, loc);
			case BuiltinTypeSpec.Type.SByte:
				return new SByteConstant (t, 0, loc);
			case BuiltinTypeSpec.Type.Byte:
				return new ByteConstant (t, 0, loc);
			case BuiltinTypeSpec.Type.Char:
				return new CharConstant (t, '\0', loc);
			case BuiltinTypeSpec.Type.Bool:
				return new BoolConstant (t, false, loc);
			case BuiltinTypeSpec.Type.Decimal:
				return new DecimalConstant (t, 0, loc);
			}

			if (t.IsEnum)
				return new EnumConstant (Constantify (EnumSpec.GetUnderlyingType (t), loc), t);

			if (t.IsNullableType)
				return Nullable.LiftedNull.Create (t, loc);

			return null;
		}

		public override bool ContainsEmitWithAwait ()
		{
			return arguments != null && arguments.ContainsEmitWithAwait ();
		}

		//
		// Checks whether the type is an interface that has the
		// [ComImport, CoClass] attributes and must be treated
		// specially
		//
		public Expression CheckComImport (ResolveContext ec)
		{
			if (!type.IsInterface)
				return null;

			//
			// Turn the call into:
			// (the-interface-stated) (new class-referenced-in-coclassattribute ())
			//
			var real_class = type.MemberDefinition.GetAttributeCoClass ();
			if (real_class == null)
				return null;

			New proxy = new New (new TypeExpression (real_class, loc), arguments, loc);
			Cast cast = new Cast (new TypeExpression (type, loc), proxy, loc);
			return cast.Resolve (ec);
		}

		public override Expression CreateExpressionTree (ResolveContext ec)
		{
			Arguments args;
			if (method == null) {
				args = new Arguments (1);
				args.Add (new Argument (new TypeOf (type, loc)));
			} else {
				args = Arguments.CreateForExpressionTree (ec,
					arguments, new TypeOfMethod (method, loc));
			}

			return CreateExpressionFactoryCall (ec, "New", args);
		}
		
		protected override Expression DoResolve (ResolveContext ec)
		{
			type = RequestedType.ResolveAsType (ec);
			if (type == null)
				return null;

			eclass = ExprClass.Value;

			if (type.IsPointer) {
				ec.Report.Error (1919, loc, "Unsafe type `{0}' cannot be used in an object creation expression",
					TypeManager.CSharpName (type));
				return null;
			}

			if (arguments == null) {
				Constant c = Constantify (type, RequestedType.Location);
				if (c != null)
					return ReducedExpression.Create (c, this);
			}

			if (type.IsDelegate) {
				return (new NewDelegate (type, arguments, loc)).Resolve (ec);
			}

			var tparam = type as TypeParameterSpec;
			if (tparam != null) {
				//
				// Check whether the type of type parameter can be constructed. BaseType can be a struct for method overrides
				// where type parameter constraint is inflated to struct
				//
				if ((tparam.SpecialConstraint & (SpecialConstraint.Struct | SpecialConstraint.Constructor)) == 0 && !TypeSpec.IsValueType (tparam)) {
					ec.Report.Error (304, loc,
						"Cannot create an instance of the variable type `{0}' because it does not have the new() constraint",
						TypeManager.CSharpName (type));
				}

				if ((arguments != null) && (arguments.Count != 0)) {
					ec.Report.Error (417, loc,
						"`{0}': cannot provide arguments when creating an instance of a variable type",
						TypeManager.CSharpName (type));
				}

				return this;
			}

			if (type.IsStatic) {
				ec.Report.SymbolRelatedToPreviousError (type);
				ec.Report.Error (712, loc, "Cannot create an instance of the static class `{0}'", TypeManager.CSharpName (type));
				return null;
			}

			if (type.IsInterface || type.IsAbstract){
				if (!TypeManager.IsGenericType (type)) {
					RequestedType = CheckComImport (ec);
					if (RequestedType != null)
						return RequestedType;
				}
				
				ec.Report.SymbolRelatedToPreviousError (type);
				ec.Report.Error (144, loc, "Cannot create an instance of the abstract class or interface `{0}'", TypeManager.CSharpName (type));
				return null;
			}

			//
			// Any struct always defines parameterless constructor
			//
			if (type.IsStruct && arguments == null)
				return this;

			bool dynamic;
			if (arguments != null) {
				arguments.Resolve (ec, out dynamic);
			} else {
				dynamic = false;
			}

			method = ConstructorLookup (ec, type, ref arguments, loc);

			if (dynamic) {
				arguments.Insert (0, new Argument (new TypeOf (type, loc).Resolve (ec), Argument.AType.DynamicTypeName));
				return new DynamicConstructorBinder (type, arguments, loc).Resolve (ec);
			}

			return this;
		}

		bool DoEmitTypeParameter (EmitContext ec)
		{
			var m = ec.Module.PredefinedMembers.ActivatorCreateInstance.Resolve (loc);
			if (m == null)
				return true;

			var ctor_factory = m.MakeGenericMethod (ec.MemberContext, type);
			var tparam = (TypeParameterSpec) type;

			if (tparam.IsReferenceType) {
				ec.Emit (OpCodes.Call, ctor_factory);
				return true;
			}

			// Allow DoEmit() to be called multiple times.
			// We need to create a new LocalTemporary each time since
			// you can't share LocalBuilders among ILGeneators.
			LocalTemporary temp = new LocalTemporary (type);

			Label label_activator = ec.DefineLabel ();
			Label label_end = ec.DefineLabel ();

			temp.AddressOf (ec, AddressOp.Store);
			ec.Emit (OpCodes.Initobj, type);

			temp.Emit (ec);
			ec.Emit (OpCodes.Box, type);
			ec.Emit (OpCodes.Brfalse, label_activator);

			temp.AddressOf (ec, AddressOp.Store);
			ec.Emit (OpCodes.Initobj, type);
			temp.Emit (ec);
			temp.Release (ec);
			ec.Emit (OpCodes.Br_S, label_end);

			ec.MarkLabel (label_activator);

			ec.Emit (OpCodes.Call, ctor_factory);
			ec.MarkLabel (label_end);
			return true;
		}

		//
		// This Emit can be invoked in two contexts:
		//    * As a mechanism that will leave a value on the stack (new object)
		//    * As one that wont (init struct)
		//
		// If we are dealing with a ValueType, we have a few
		// situations to deal with:
		//
		//    * The target is a ValueType, and we have been provided
		//      the instance (this is easy, we are being assigned).
		//
		//    * The target of New is being passed as an argument,
		//      to a boxing operation or a function that takes a
		//      ValueType.
		//
		//      In this case, we need to create a temporary variable
		//      that is the argument of New.
		//
		// Returns whether a value is left on the stack
		//
		// *** Implementation note ***
		//
		// To benefit from this optimization, each assignable expression
		// has to manually cast to New and call this Emit.
		//
		// TODO: It's worth to implement it for arrays and fields
		//
		public virtual bool Emit (EmitContext ec, IMemoryLocation target)
		{
			bool is_value_type = TypeSpec.IsValueType (type);
			VariableReference vr = target as VariableReference;

			if (target != null && is_value_type && (vr != null || method == null)) {
				target.AddressOf (ec, AddressOp.Store);
			} else if (vr != null && vr.IsRef) {
				vr.EmitLoad (ec);
			}

			if (arguments != null) {
				if (ec.HasSet (BuilderContext.Options.AsyncBody) && (arguments.Count > (this is NewInitialize ? 0 : 1)) && arguments.ContainsEmitWithAwait ())
					arguments = arguments.Emit (ec, false, true);

				arguments.Emit (ec);
			}

			if (is_value_type) {
				if (method == null) {
					ec.Emit (OpCodes.Initobj, type);
					return false;
				}

				if (vr != null) {
					ec.Emit (OpCodes.Call, method);
					return false;
				}
			}
			
			if (type is TypeParameterSpec)
				return DoEmitTypeParameter (ec);			

			ec.Emit (OpCodes.Newobj, method);
			return true;
		}

		public override void Emit (EmitContext ec)
		{
			LocalTemporary v = null;
			if (method == null && TypeSpec.IsValueType (type)) {
				// TODO: Use temporary variable from pool
				v = new LocalTemporary (type);
			}

			if (!Emit (ec, v))
				v.Emit (ec);
		}
		
		public override void EmitStatement (EmitContext ec)
		{
			LocalTemporary v = null;
			if (method == null && TypeSpec.IsValueType (type)) {
				// TODO: Use temporary variable from pool
				v = new LocalTemporary (type);
			}

			if (Emit (ec, v))
				ec.Emit (OpCodes.Pop);
		}

		public void AddressOf (EmitContext ec, AddressOp mode)
		{
			EmitAddressOf (ec, mode);
		}

		protected virtual IMemoryLocation EmitAddressOf (EmitContext ec, AddressOp mode)
		{
			LocalTemporary value_target = new LocalTemporary (type);

			if (type is TypeParameterSpec) {
				DoEmitTypeParameter (ec);
				value_target.Store (ec);
				value_target.AddressOf (ec, mode);
				return value_target;
			}

			value_target.AddressOf (ec, AddressOp.Store);

			if (method == null) {
				ec.Emit (OpCodes.Initobj, type);
			} else {
				if (arguments != null)
					arguments.Emit (ec);

				ec.Emit (OpCodes.Call, method);
			}
			
			value_target.AddressOf (ec, mode);
			return value_target;
		}

		protected override void CloneTo (CloneContext clonectx, Expression t)
		{
			New target = (New) t;

			target.RequestedType = RequestedType.Clone (clonectx);
			if (arguments != null){
				target.arguments = arguments.Clone (clonectx);
			}
		}

		public override SLE.Expression MakeExpression (BuilderContext ctx)
		{
#if STATIC
			return base.MakeExpression (ctx);
#else
			return SLE.Expression.New ((ConstructorInfo) method.GetMetaInfo (), Arguments.MakeExpression (arguments, ctx));
#endif
		}
		
		public override object Accept (StructuralVisitor visitor)
		{
			return visitor.Visit (this);
		}
	}

	//
	// Array initializer expression, the expression is allowed in
	// variable or field initialization only which makes it tricky as
	// the type has to be infered based on the context either from field
	// type or variable type (think of multiple declarators)
	//
	public class ArrayInitializer : Expression
	{
		List<Expression> elements;
		BlockVariableDeclaration variable;

		public ArrayInitializer (List<Expression> init, Location loc)
		{
			elements = init;
			this.loc = loc;
		}

		public ArrayInitializer (int count, Location loc)
			: this (new List<Expression> (count), loc)
		{
		}

		public ArrayInitializer (Location loc)
			: this (4, loc)
		{
		}

		#region Properties

		public int Count {
			get { return elements.Count; }
		}

		public List<Expression> Elements {
			get {
				return elements;
			}
		}

		public Expression this [int index] {
			get {
				return elements [index];
			}
		}

		public BlockVariableDeclaration VariableDeclaration {
			get {
				return variable;
			}
			set {
				variable = value;
			}
		}
		#endregion

		public void Add (Expression expr)
		{
			elements.Add (expr);
		}

		public override bool ContainsEmitWithAwait ()
		{
			throw new NotSupportedException ();
		}

		public override Expression CreateExpressionTree (ResolveContext ec)
		{
			throw new NotSupportedException ("ET");
		}

		protected override void CloneTo (CloneContext clonectx, Expression t)
		{
			var target = (ArrayInitializer) t;

			target.elements = new List<Expression> (elements.Count);
			foreach (var element in elements)
				target.elements.Add (element.Clone (clonectx));
		}

		protected override Expression DoResolve (ResolveContext rc)
		{
			var current_field = rc.CurrentMemberDefinition as FieldBase;
			TypeExpression type;
			if (current_field != null && rc.CurrentAnonymousMethod == null) {
				type = new TypeExpression (current_field.MemberType, current_field.Location);
			} else if (variable != null) {
				if (variable.TypeExpression is VarExpr) {
					rc.Report.Error (820, loc, "An implicitly typed local variable declarator cannot use an array initializer");
					return EmptyExpression.Null;
				}

				type = new TypeExpression (variable.Variable.Type, variable.Variable.Location);
			} else {
				throw new NotImplementedException ("Unexpected array initializer context");
			}

			return new ArrayCreation (type, this).Resolve (rc);
		}

		public override void Emit (EmitContext ec)
		{
			throw new InternalErrorException ("Missing Resolve call");
		}
		
		public override object Accept (StructuralVisitor visitor)
		{
			return visitor.Visit (this);
		}
	}

	/// <summary>
	///   14.5.10.2: Represents an array creation expression.
	/// </summary>
	///
	/// <remarks>
	///   There are two possible scenarios here: one is an array creation
	///   expression that specifies the dimensions and optionally the
	///   initialization data and the other which does not need dimensions
	///   specified but where initialization data is mandatory.
	/// </remarks>
	public class ArrayCreation : Expression
	{
		FullNamedExpression requested_base_type;
		ArrayInitializer initializers;

		//
		// The list of Argument types.
		// This is used to construct the `newarray' or constructor signature
		//
		protected List<Expression> arguments;
		
		protected TypeSpec array_element_type;
		int num_arguments = 0;
		protected int dimensions;
		protected readonly ComposedTypeSpecifier rank;
		Expression first_emit;
		LocalTemporary first_emit_temp;

		protected List<Expression> array_data;

		Dictionary<int, int> bounds;

#if STATIC
		// The number of constants in array initializers
		int const_initializers_count;
		bool only_constant_initializers;
#endif
		public ArrayCreation (FullNamedExpression requested_base_type, List<Expression> exprs, ComposedTypeSpecifier rank, ArrayInitializer initializers, Location l)
			: this (requested_base_type, rank, initializers, l)
		{
			arguments = exprs;
			num_arguments = arguments.Count;
		}

		//
		// For expressions like int[] foo = new int[] { 1, 2, 3 };
		//
		public ArrayCreation (FullNamedExpression requested_base_type, ComposedTypeSpecifier rank, ArrayInitializer initializers, Location loc)
		{
			this.requested_base_type = requested_base_type;
			this.rank = rank;
			this.initializers = initializers;
			this.loc = loc;

			if (rank != null)
				num_arguments = rank.Dimension;
		}

		//
		// For compiler generated single dimensional arrays only
		//
		public ArrayCreation (FullNamedExpression requested_base_type, ArrayInitializer initializers, Location loc)
			: this (requested_base_type, ComposedTypeSpecifier.SingleDimension, initializers, loc)
		{
		}

		//
		// For expressions like int[] foo = { 1, 2, 3 };
		//
		public ArrayCreation (FullNamedExpression requested_base_type, ArrayInitializer initializers)
			: this (requested_base_type, null, initializers, initializers.Location)
		{
		}

		public ComposedTypeSpecifier Rank {
			get {
				return this.rank;
			}
		}
		
		public FullNamedExpression TypeExpression {
			get {
				return this.requested_base_type;
			}
		}
		
		public ArrayInitializer Initializers {
			get {
				return this.initializers;
			}
		}
		
		public List<Expression> Arguments {
			get { return this.arguments; }
		}
		
		bool CheckIndices (ResolveContext ec, ArrayInitializer probe, int idx, bool specified_dims, int child_bounds)
		{
			if (initializers != null && bounds == null) {
				//
				// We use this to store all the date values in the order in which we
				// will need to store them in the byte blob later
				//
				array_data = new List<Expression> ();
				bounds = new Dictionary<int, int> ();
			}

			if (specified_dims) { 
				Expression a = arguments [idx];
				a = a.Resolve (ec);
				if (a == null)
					return false;

				a = ConvertExpressionToArrayIndex (ec, a);
				if (a == null)
					return false;

				arguments[idx] = a;

				if (initializers != null) {
					Constant c = a as Constant;
					if (c == null && a is ArrayIndexCast)
						c = ((ArrayIndexCast) a).Child as Constant;

					if (c == null) {
						ec.Report.Error (150, a.Location, "A constant value is expected");
						return false;
					}

					int value;
					try {
						value = System.Convert.ToInt32 (c.GetValue ());
					} catch {
						ec.Report.Error (150, a.Location, "A constant value is expected");
						return false;
					}

					// TODO: probe.Count does not fit ulong in
					if (value != probe.Count) {
						ec.Report.Error (847, loc, "An array initializer of length `{0}' was expected", value.ToString ());
						return false;
					}

					bounds[idx] = value;
				}
			}

			if (initializers == null)
				return true;

			for (int i = 0; i < probe.Count; ++i) {
				var o = probe [i];
				if (o is ArrayInitializer) {
					var sub_probe = o as ArrayInitializer;
					if (idx + 1 >= dimensions){
						ec.Report.Error (623, loc, "Array initializers can only be used in a variable or field initializer. Try using a new expression instead");
						return false;
					}
					
					bool ret = CheckIndices (ec, sub_probe, idx + 1, specified_dims, child_bounds - 1);
					if (!ret)
						return false;
				} else if (child_bounds > 1) {
					ec.Report.Error (846, o.Location, "A nested array initializer was expected");
				} else {
					Expression element = ResolveArrayElement (ec, o);
					if (element == null)
						continue;
#if STATIC
					// Initializers with the default values can be ignored
					Constant c = element as Constant;
					if (c != null) {
						if (!c.IsDefaultInitializer (array_element_type)) {
							++const_initializers_count;
						}
					} else {
						only_constant_initializers = false;
					}
#endif					
					array_data.Add (element);
				}
			}

			return true;
		}

		public override bool ContainsEmitWithAwait ()
		{
			foreach (var arg in arguments) {
				if (arg.ContainsEmitWithAwait ())
					return true;
			}

			return InitializersContainAwait ();
		}

		public override Expression CreateExpressionTree (ResolveContext ec)
		{
			Arguments args;

			if (array_data == null) {
				args = new Arguments (arguments.Count + 1);
				args.Add (new Argument (new TypeOf (array_element_type, loc)));
				foreach (Expression a in arguments)
					args.Add (new Argument (a.CreateExpressionTree (ec)));

				return CreateExpressionFactoryCall (ec, "NewArrayBounds", args);
			}

			if (dimensions > 1) {
				ec.Report.Error (838, loc, "An expression tree cannot contain a multidimensional array initializer");
				return null;
			}

			args = new Arguments (array_data == null ? 1 : array_data.Count + 1);
			args.Add (new Argument (new TypeOf (array_element_type, loc)));
			if (array_data != null) {
				for (int i = 0; i < array_data.Count; ++i) {
					Expression e = array_data [i];
					args.Add (new Argument (e.CreateExpressionTree (ec)));
				}
			}

			return CreateExpressionFactoryCall (ec, "NewArrayInit", args);
		}		
		
		void UpdateIndices (ResolveContext rc)
		{
			int i = 0;
			for (var probe = initializers; probe != null;) {
				Expression e = new IntConstant (rc.BuiltinTypes, probe.Count, Location.Null);
				arguments.Add (e);
				bounds[i++] = probe.Count;

				if (probe.Count > 0 && probe [0] is ArrayInitializer) {
					probe = (ArrayInitializer) probe[0];
				} else if (dimensions > i) {
					continue;
				} else {
					return;
				}
			}
		}

		protected override void Error_NegativeArrayIndex (ResolveContext ec, Location loc)
		{
			ec.Report.Error (248, loc, "Cannot create an array with a negative size");
		}

		bool InitializersContainAwait ()
		{
			if (array_data == null)
				return false;

			foreach (var expr in array_data) {
				if (expr.ContainsEmitWithAwait ())
					return true;
			}

			return false;
		}

		protected virtual Expression ResolveArrayElement (ResolveContext ec, Expression element)
		{
			element = element.Resolve (ec);
			if (element == null)
				return null;

			if (element is CompoundAssign.TargetExpression) {
				if (first_emit != null)
					throw new InternalErrorException ("Can only handle one mutator at a time");
				first_emit = element;
				element = first_emit_temp = new LocalTemporary (element.Type);
			}

			return Convert.ImplicitConversionRequired (
				ec, element, array_element_type, loc);
		}

		protected bool ResolveInitializers (ResolveContext ec)
		{
#if STATIC
			only_constant_initializers = true;
#endif

			if (arguments != null) {
				bool res = true;
				for (int i = 0; i < arguments.Count; ++i) {
					res &= CheckIndices (ec, initializers, i, true, dimensions);
					if (initializers != null)
						break;
				}

				return res;
			}

			arguments = new List<Expression> ();

			if (!CheckIndices (ec, initializers, 0, false, dimensions))
				return false;
				
			UpdateIndices (ec);
				
			return true;
		}

		//
		// Resolved the type of the array
		//
		bool ResolveArrayType (ResolveContext ec)
		{
			//
			// Lookup the type
			//
			FullNamedExpression array_type_expr;
			if (num_arguments > 0) {
				array_type_expr = new ComposedCast (requested_base_type, rank);
			} else {
				array_type_expr = requested_base_type;
			}

			type = array_type_expr.ResolveAsType (ec);
			if (array_type_expr == null)
				return false;

			var ac = type as ArrayContainer;
			if (ac == null) {
				ec.Report.Error (622, loc, "Can only use array initializer expressions to assign to array types. Try using a new expression instead");
				return false;
			}

			array_element_type = ac.Element;
			dimensions = ac.Rank;

			return true;
		}

		protected override Expression DoResolve (ResolveContext ec)
		{
			if (type != null)
				return this;

			if (!ResolveArrayType (ec))
				return null;

			//
			// validate the initializers and fill in any missing bits
			//
			if (!ResolveInitializers (ec))
				return null;

			eclass = ExprClass.Value;
			return this;
		}

		byte [] MakeByteBlob ()
		{
			int factor;
			byte [] data;
			byte [] element;
			int count = array_data.Count;

			TypeSpec element_type = array_element_type;
			if (element_type.IsEnum)
				element_type = EnumSpec.GetUnderlyingType (element_type);

			factor = BuiltinTypeSpec.GetSize (element_type);
			if (factor == 0)
				throw new Exception ("unrecognized type in MakeByteBlob: " + element_type);

			data = new byte [(count * factor + 3) & ~3];
			int idx = 0;

			for (int i = 0; i < count; ++i) {
				var c = array_data[i] as Constant;
				if (c == null) {
					idx += factor;
					continue;
				}

				object v = c.GetValue ();

				switch (element_type.BuiltinType) {
				case BuiltinTypeSpec.Type.Long:
					long lval = (long) v;

					for (int j = 0; j < factor; ++j) {
						data[idx + j] = (byte) (lval & 0xFF);
						lval = (lval >> 8);
					}
					break;
				case BuiltinTypeSpec.Type.ULong:
					ulong ulval = (ulong) v;

					for (int j = 0; j < factor; ++j) {
						data[idx + j] = (byte) (ulval & 0xFF);
						ulval = (ulval >> 8);
					}
					break;
				case BuiltinTypeSpec.Type.Float:
					element = BitConverter.GetBytes ((float) v);

					for (int j = 0; j < factor; ++j)
						data[idx + j] = element[j];
					if (!BitConverter.IsLittleEndian)
						System.Array.Reverse (data, idx, 4);
					break;
				case BuiltinTypeSpec.Type.Double:
					element = BitConverter.GetBytes ((double) v);

					for (int j = 0; j < factor; ++j)
						data[idx + j] = element[j];

					// FIXME: Handle the ARM float format.
					if (!BitConverter.IsLittleEndian)
						System.Array.Reverse (data, idx, 8);
					break;
				case BuiltinTypeSpec.Type.Char:
					int chval = (int) ((char) v);

					data[idx] = (byte) (chval & 0xff);
					data[idx + 1] = (byte) (chval >> 8);
					break;
				case BuiltinTypeSpec.Type.Short:
					int sval = (int) ((short) v);

					data[idx] = (byte) (sval & 0xff);
					data[idx + 1] = (byte) (sval >> 8);
					break;
				case BuiltinTypeSpec.Type.UShort:
					int usval = (int) ((ushort) v);

					data[idx] = (byte) (usval & 0xff);
					data[idx + 1] = (byte) (usval >> 8);
					break;
				case BuiltinTypeSpec.Type.Int:
					int val = (int) v;

					data[idx] = (byte) (val & 0xff);
					data[idx + 1] = (byte) ((val >> 8) & 0xff);
					data[idx + 2] = (byte) ((val >> 16) & 0xff);
					data[idx + 3] = (byte) (val >> 24);
					break;
				case BuiltinTypeSpec.Type.UInt:
					uint uval = (uint) v;

					data[idx] = (byte) (uval & 0xff);
					data[idx + 1] = (byte) ((uval >> 8) & 0xff);
					data[idx + 2] = (byte) ((uval >> 16) & 0xff);
					data[idx + 3] = (byte) (uval >> 24);
					break;
				case BuiltinTypeSpec.Type.SByte:
					data[idx] = (byte) (sbyte) v;
					break;
				case BuiltinTypeSpec.Type.Byte:
					data[idx] = (byte) v;
					break;
				case BuiltinTypeSpec.Type.Bool:
					data[idx] = (byte) ((bool) v ? 1 : 0);
					break;
				case BuiltinTypeSpec.Type.Decimal:
					int[] bits = Decimal.GetBits ((decimal) v);
					int p = idx;

					// FIXME: For some reason, this doesn't work on the MS runtime.
					int[] nbits = new int[4];
					nbits[0] = bits[3];
					nbits[1] = bits[2];
					nbits[2] = bits[0];
					nbits[3] = bits[1];

					for (int j = 0; j < 4; j++) {
						data[p++] = (byte) (nbits[j] & 0xff);
						data[p++] = (byte) ((nbits[j] >> 8) & 0xff);
						data[p++] = (byte) ((nbits[j] >> 16) & 0xff);
						data[p++] = (byte) (nbits[j] >> 24);
					}
					break;
				default:
					throw new Exception ("Unrecognized type in MakeByteBlob: " + element_type);
				}

				idx += factor;
			}

			return data;
		}

#if NET_4_0 || MONODROID
		public override SLE.Expression MakeExpression (BuilderContext ctx)
		{
#if STATIC
			return base.MakeExpression (ctx);
#else
			var initializers = new SLE.Expression [array_data.Count];
			for (var i = 0; i < initializers.Length; i++) {
				if (array_data [i] == null)
					initializers [i] = SLE.Expression.Default (array_element_type.GetMetaInfo ());
				else
					initializers [i] = array_data [i].MakeExpression (ctx);
			}

			return SLE.Expression.NewArrayInit (array_element_type.GetMetaInfo (), initializers);
#endif
		}
#endif
#if STATIC
		//
		// Emits the initializers for the array
		//
		void EmitStaticInitializers (EmitContext ec, FieldExpr stackArray)
		{
			var m = ec.Module.PredefinedMembers.RuntimeHelpersInitializeArray.Resolve (loc);
			if (m == null)
				return;

			//
			// First, the static data
			//
			byte [] data = MakeByteBlob ();
			var fb = ec.CurrentTypeDefinition.Module.MakeStaticData (data, loc);

			if (stackArray == null) {
				ec.Emit (OpCodes.Dup);
			} else {
				stackArray.Emit (ec);
			}

			ec.Emit (OpCodes.Ldtoken, fb);
			ec.Emit (OpCodes.Call, m);
		}
#endif

		//
		// Emits pieces of the array that can not be computed at compile
		// time (variables and string locations).
		//
		// This always expect the top value on the stack to be the array
		//
		void EmitDynamicInitializers (EmitContext ec, bool emitConstants, FieldExpr stackArray)
		{
			int dims = bounds.Count;
			var current_pos = new int [dims];

			for (int i = 0; i < array_data.Count; i++){

				Expression e = array_data [i];
				var c = e as Constant;

				// Constant can be initialized via StaticInitializer
				if (c == null || (c != null && emitConstants && !c.IsDefaultInitializer (array_element_type))) {

					var etype = e.Type;

					if (stackArray != null) {
						if (e.ContainsEmitWithAwait ()) {
							e = e.EmitToField (ec);
						}

						stackArray.Emit (ec);
					} else {
						ec.Emit (OpCodes.Dup);
					}

					for (int idx = 0; idx < dims; idx++) 
						ec.EmitInt (current_pos [idx]);

					//
					// If we are dealing with a struct, get the
					// address of it, so we can store it.
					//
					if (dims == 1 && etype.IsStruct) {
						switch (etype.BuiltinType) {
						case BuiltinTypeSpec.Type.Byte:
						case BuiltinTypeSpec.Type.SByte:
						case BuiltinTypeSpec.Type.Bool:
						case BuiltinTypeSpec.Type.Short:
						case BuiltinTypeSpec.Type.UShort:
						case BuiltinTypeSpec.Type.Char:
						case BuiltinTypeSpec.Type.Int:
						case BuiltinTypeSpec.Type.UInt:
						case BuiltinTypeSpec.Type.Long:
						case BuiltinTypeSpec.Type.ULong:
						case BuiltinTypeSpec.Type.Float:
						case BuiltinTypeSpec.Type.Double:
							break;
						default:
							ec.Emit (OpCodes.Ldelema, etype);
							break;
						}
					}

					e.Emit (ec);

					ec.EmitArrayStore ((ArrayContainer) type);
				}
				
				//
				// Advance counter
				//
				for (int j = dims - 1; j >= 0; j--){
					current_pos [j]++;
					if (current_pos [j] < bounds [j])
						break;
					current_pos [j] = 0;
				}
			}
		}

		public override void Emit (EmitContext ec)
		{
			EmitToFieldSource (ec);
		}

		protected sealed override FieldExpr EmitToFieldSource (EmitContext ec)
		{
			if (first_emit != null) {
				first_emit.Emit (ec);
				first_emit_temp.Store (ec);
			}

			FieldExpr await_stack_field;
			if (ec.HasSet (BuilderContext.Options.AsyncBody) && InitializersContainAwait ()) {
				await_stack_field = ec.GetTemporaryField (type);
				ec.EmitThis ();
			} else {
				await_stack_field = null;
			}

			EmitExpressionsList (ec, arguments);

			ec.EmitArrayNew ((ArrayContainer) type);
			
			if (initializers == null)
				return await_stack_field;

			if (await_stack_field != null)
				await_stack_field.EmitAssignFromStack (ec);

#if STATIC
			//
			// Emit static initializer for arrays which contain more than 2 items and
			// the static initializer will initialize at least 25% of array values or there
			// is more than 10 items to be initialized
			//
			// NOTE: const_initializers_count does not contain default constant values.
			//
			if (const_initializers_count > 2 && (array_data.Count > 10 || const_initializers_count * 4 > (array_data.Count)) &&
				(BuiltinTypeSpec.IsPrimitiveType (array_element_type) || array_element_type.IsEnum)) {
				EmitStaticInitializers (ec, await_stack_field);

				if (!only_constant_initializers)
					EmitDynamicInitializers (ec, false, await_stack_field);
			} else
#endif
			{
				EmitDynamicInitializers (ec, true, await_stack_field);
			}

			if (first_emit_temp != null)
				first_emit_temp.Release (ec);

			return await_stack_field;
		}

		public override void EncodeAttributeValue (IMemberContext rc, AttributeEncoder enc, TypeSpec targetType)
		{
			// no multi dimensional or jagged arrays
			if (arguments.Count != 1 || array_element_type.IsArray) {
				base.EncodeAttributeValue (rc, enc, targetType);
				return;
			}

			// No array covariance, except for array -> object
			if (type != targetType) {
				if (targetType.BuiltinType != BuiltinTypeSpec.Type.Object) {
					base.EncodeAttributeValue (rc, enc, targetType);
					return;
				}

				if (enc.Encode (type) == AttributeEncoder.EncodedTypeProperties.DynamicType) {
					Attribute.Error_AttributeArgumentIsDynamic (rc, loc);
					return;
				}
			}

			// Single dimensional array of 0 size
			if (array_data == null) {
				IntConstant ic = arguments[0] as IntConstant;
				if (ic == null || !ic.IsDefaultValue) {
					base.EncodeAttributeValue (rc, enc, targetType);
				} else {
					enc.Encode (0);
				}

				return;
			}

			enc.Encode (array_data.Count);
			foreach (var element in array_data) {
				element.EncodeAttributeValue (rc, enc, array_element_type);
			}
		}
		
		protected override void CloneTo (CloneContext clonectx, Expression t)
		{
			ArrayCreation target = (ArrayCreation) t;

			if (requested_base_type != null)
				target.requested_base_type = (FullNamedExpression)requested_base_type.Clone (clonectx);

			if (arguments != null){
				target.arguments = new List<Expression> (arguments.Count);
				foreach (Expression e in arguments)
					target.arguments.Add (e.Clone (clonectx));
			}

			if (initializers != null)
				target.initializers = (ArrayInitializer) initializers.Clone (clonectx);
		}
		
		public override object Accept (StructuralVisitor visitor)
		{
			return visitor.Visit (this);
		}
	}
	
	//
	// Represents an implicitly typed array epxression
	//
	class ImplicitlyTypedArrayCreation : ArrayCreation
	{
		sealed class InferenceContext : TypeInferenceContext
		{
			class ExpressionBoundInfo : BoundInfo
			{
				readonly Expression expr;

				public ExpressionBoundInfo (Expression expr)
					: base (expr.Type, BoundKind.Lower)
				{
					this.expr = expr;
				}

				public override bool Equals (BoundInfo other)
				{
					// We are using expression not type for conversion check
					// no optimization based on types is possible
					return false;
				}

				public override Expression GetTypeExpression ()
				{
					return expr;
				}
			}

			public void AddExpression (Expression expr)
			{
				AddToBounds (new ExpressionBoundInfo (expr), 0);
			}
		}

		InferenceContext best_type_inference;

		public ImplicitlyTypedArrayCreation (ComposedTypeSpecifier rank, ArrayInitializer initializers, Location loc)
			: base (null, rank, initializers, loc)
		{			
		}

		public ImplicitlyTypedArrayCreation (ArrayInitializer initializers, Location loc)
			: base (null, initializers, loc)
		{
		}

		protected override Expression DoResolve (ResolveContext ec)
		{
			if (type != null)
				return this;

			dimensions = rank.Dimension;

			best_type_inference = new InferenceContext ();

			if (!ResolveInitializers (ec))
				return null;

			best_type_inference.FixAllTypes (ec);
			array_element_type = best_type_inference.InferredTypeArguments[0];
			best_type_inference = null;

			if (array_element_type == null ||
				array_element_type == InternalType.NullLiteral || array_element_type == InternalType.MethodGroup || array_element_type == InternalType.AnonymousMethod ||
				arguments.Count != rank.Dimension) {
				ec.Report.Error (826, loc,
					"The type of an implicitly typed array cannot be inferred from the initializer. Try specifying array type explicitly");
				return null;
			}

			//
			// At this point we found common base type for all initializer elements
			// but we have to be sure that all static initializer elements are of
			// same type
			//
			UnifyInitializerElement (ec);

			type = ArrayContainer.MakeType (ec.Module, array_element_type, dimensions);
			eclass = ExprClass.Value;
			return this;
		}

		//
		// Converts static initializer only
		//
		void UnifyInitializerElement (ResolveContext ec)
		{
			for (int i = 0; i < array_data.Count; ++i) {
				Expression e = array_data[i];
				if (e != null)
					array_data [i] = Convert.ImplicitConversion (ec, e, array_element_type, Location.Null);
			}
		}

		protected override Expression ResolveArrayElement (ResolveContext ec, Expression element)
		{
			element = element.Resolve (ec);
			if (element != null)
				best_type_inference.AddExpression (element);

			return element;
		}
	}	
	
	sealed class CompilerGeneratedThis : This
	{
		public CompilerGeneratedThis (TypeSpec type, Location loc)
			: base (loc)
		{
			this.type = type;
			eclass = ExprClass.Variable;
		}

		protected override Expression DoResolve (ResolveContext ec)
		{
			return this;
		}

		public override HoistedVariable GetHoistedVariable (AnonymousExpression ae)
		{
			return null;
		}
	}
	
	/// <summary>
	///   Represents the `this' construct
	/// </summary>

	public class This : VariableReference
	{
		sealed class ThisVariable : ILocalVariable
		{
			public static readonly ILocalVariable Instance = new ThisVariable ();

			public void Emit (EmitContext ec)
			{
				ec.EmitThis ();
			}

			public void EmitAssign (EmitContext ec)
			{
				throw new InvalidOperationException ();
			}

			public void EmitAddressOf (EmitContext ec)
			{
				ec.EmitThis ();
			}
		}

		VariableInfo variable_info;

		public This (Location loc)
		{
			this.loc = loc;
		}

		#region Properties

		public override string Name {
			get { return "this"; }
		}

		public override bool IsLockedByStatement {
			get {
				return false;
			}
			set {
			}
		}

		public override bool IsRef {
			get { return type.IsStruct; }
		}

		public override bool IsSideEffectFree {
			get {
				return true;
			}
		}

		protected override ILocalVariable Variable {
			get { return ThisVariable.Instance; }
		}

		public override VariableInfo VariableInfo {
			get { return variable_info; }
		}

		public override bool IsFixed {
			get { return false; }
		}

		#endregion

		public void CheckStructThisDefiniteAssignment (ResolveContext rc)
		{
			//
			// It's null for all cases when we don't need to check `this'
			// definitive assignment
			//
			if (variable_info == null)
				return;

			if (rc.HasSet (ResolveContext.Options.OmitStructFlowAnalysis))
				return;

			if (!variable_info.IsAssigned (rc)) {
				rc.Report.Error (188, loc,
					"The `this' object cannot be used before all of its fields are assigned to");
			}
		}

		protected virtual void Error_ThisNotAvailable (ResolveContext ec)
		{
			if (ec.IsStatic && !ec.HasSet (ResolveContext.Options.ConstantScope)) {
				ec.Report.Error (26, loc, "Keyword `this' is not valid in a static property, static method, or static field initializer");
			} else if (ec.CurrentAnonymousMethod != null) {
				ec.Report.Error (1673, loc,
					"Anonymous methods inside structs cannot access instance members of `this'. " +
					"Consider copying `this' to a local variable outside the anonymous method and using the local instead");
			} else {
				ec.Report.Error (27, loc, "Keyword `this' is not available in the current context");
			}
		}

		public override HoistedVariable GetHoistedVariable (AnonymousExpression ae)
		{
			if (ae == null)
				return null;

			AnonymousMethodStorey storey = ae.Storey;
			return storey != null ? storey.HoistedThis : null;
		}

		public static bool IsThisAvailable (ResolveContext ec, bool ignoreAnonymous)
		{
			if (ec.IsStatic || ec.HasAny (ResolveContext.Options.FieldInitializerScope | ResolveContext.Options.BaseInitializer | ResolveContext.Options.ConstantScope))
				return false;

			if (ignoreAnonymous || ec.CurrentAnonymousMethod == null)
				return true;

			if (ec.CurrentType.IsStruct && !(ec.CurrentAnonymousMethod is StateMachineInitializer))
				return false;

			return true;
		}

		public virtual void ResolveBase (ResolveContext ec)
		{
			eclass = ExprClass.Variable;
			type = ec.CurrentType;

			if (!IsThisAvailable (ec, false)) {
				Error_ThisNotAvailable (ec);
				return;
			}

			var block = ec.CurrentBlock;
			if (block != null) {
				var top = block.ParametersBlock.TopBlock;
				if (top.ThisVariable != null)
					variable_info = top.ThisVariable.VariableInfo;

				AnonymousExpression am = ec.CurrentAnonymousMethod;
				if (am != null && ec.IsVariableCapturingRequired && !block.Explicit.HasCapturedThis) {
					//
					// Hoisted this is almost like hoisted variable but not exactly. When
					// there is no variable hoisted we can simply emit an instance method
					// without lifting this into a storey. Unfotunatelly this complicates
					// this in other cases because we don't know where this will be hoisted
					// until top-level block is fully resolved
					//
					top.AddThisReferenceFromChildrenBlock (block.Explicit);
					am.SetHasThisAccess ();
				}
			}
		}

		protected override Expression DoResolve (ResolveContext ec)
		{
			ResolveBase (ec);

			CheckStructThisDefiniteAssignment (ec);

			return this;
		}

		public override Expression DoResolveLValue (ResolveContext ec, Expression right_side)
		{
			if (eclass == ExprClass.Unresolved)
				ResolveBase (ec);

			if (variable_info != null)
				variable_info.SetAssigned (ec);

			if (type.IsClass){
				if (right_side == EmptyExpression.UnaryAddress)
					ec.Report.Error (459, loc, "Cannot take the address of `this' because it is read-only");
				else if (right_side == EmptyExpression.OutAccess)
					ec.Report.Error (1605, loc, "Cannot pass `this' as a ref or out argument because it is read-only");
				else
					ec.Report.Error (1604, loc, "Cannot assign to `this' because it is read-only");
			}

			return this;
		}

		public override int GetHashCode()
		{
			throw new NotImplementedException ();
		}

		public override bool Equals (object obj)
		{
			This t = obj as This;
			if (t == null)
				return false;

			return true;
		}

		protected override void CloneTo (CloneContext clonectx, Expression t)
		{
			// Nothing
		}

		public override void SetHasAddressTaken ()
		{
			// Nothing
		}

		public override void VerifyAssigned (ResolveContext rc)
		{
		
		}

		
		public override object Accept (StructuralVisitor visitor)
		{
			return visitor.Visit (this);
		}
	}

	/// <summary>
	///   Represents the `__arglist' construct
	/// </summary>
	public class ArglistAccess : Expression
	{
		public ArglistAccess (Location loc)
		{
			this.loc = loc;
		}

		protected override void CloneTo (CloneContext clonectx, Expression target)
		{
			// nothing.
		}

		public override bool ContainsEmitWithAwait ()
		{
			return false;
		}

		public override Expression CreateExpressionTree (ResolveContext ec)
		{
			throw new NotSupportedException ("ET");
		}

		protected override Expression DoResolve (ResolveContext ec)
		{
			eclass = ExprClass.Variable;
			type = ec.Module.PredefinedTypes.RuntimeArgumentHandle.Resolve ();

			if (ec.HasSet (ResolveContext.Options.FieldInitializerScope) || !ec.CurrentBlock.ParametersBlock.Parameters.HasArglist) {
				ec.Report.Error (190, loc,
					"The __arglist construct is valid only within a variable argument method");
			}

			return this;
		}

		public override void Emit (EmitContext ec)
		{
			ec.Emit (OpCodes.Arglist);
		}

		public override object Accept (StructuralVisitor visitor)
		{
			return visitor.Visit (this);
		}
	}

	/// <summary>
	///   Represents the `__arglist (....)' construct
	/// </summary>
	public class Arglist : Expression
	{
		Arguments arguments;

		public Arglist (Location loc)
			: this (null, loc)
		{
		}

		public Arglist (Arguments args, Location l)
		{
			arguments = args;
			loc = l;
		}

		public Arguments Arguments {
			get {
				return arguments;
			}
		}

		public MetaType[] ArgumentTypes {
		    get {
				if (arguments == null)
					return MetaType.EmptyTypes;

				var retval = new MetaType[arguments.Count];
				for (int i = 0; i < retval.Length; i++)
					retval[i] = arguments[i].Expr.Type.GetMetaInfo ();

		        return retval;
		    }
		}

		public override bool ContainsEmitWithAwait ()
		{
			throw new NotImplementedException ();
		}
		
		public override Expression CreateExpressionTree (ResolveContext ec)
		{
			ec.Report.Error (1952, loc, "An expression tree cannot contain a method with variable arguments");
			return null;
		}

		protected override Expression DoResolve (ResolveContext ec)
		{
			eclass = ExprClass.Variable;
			type = InternalType.Arglist;
			if (arguments != null) {
				bool dynamic;	// Can be ignored as there is always only 1 overload
				arguments.Resolve (ec, out dynamic);
			}

			return this;
		}

		public override void Emit (EmitContext ec)
		{
			if (arguments != null)
				arguments.Emit (ec);
		}

		protected override void CloneTo (CloneContext clonectx, Expression t)
		{
			Arglist target = (Arglist) t;

			if (arguments != null)
				target.arguments = arguments.Clone (clonectx);
		}

		public override object Accept (StructuralVisitor visitor)
		{
			return visitor.Visit (this);
		}
	}

	public class RefValueExpr : ShimExpression
	{
		FullNamedExpression texpr;
		
		public FullNamedExpression FullNamedExpression {
			get { return texpr;}
		}
		
		public RefValueExpr (Expression expr, FullNamedExpression texpr, Location loc)
			: base (expr)
		{
			this.texpr = texpr;
			this.loc = loc;
		}

		public FullNamedExpression TypeExpression {
			get {
				return texpr;
			}
		}

		public override bool ContainsEmitWithAwait ()
		{
			return false;
		}

		protected override Expression DoResolve (ResolveContext rc)
		{
			expr = expr.Resolve (rc);
			type = texpr.ResolveAsType (rc);
			if (expr == null || type == null)
				return null;

			expr = Convert.ImplicitConversionRequired (rc, expr, rc.Module.PredefinedTypes.TypedReference.Resolve (), loc);
			eclass = ExprClass.Value;
			return this;
		}

		public override void Emit (EmitContext ec)
		{
			expr.Emit (ec);
			ec.Emit (OpCodes.Refanyval, type);
			ec.EmitLoadFromPtr (type);
		}
		
		public override object Accept (StructuralVisitor visitor)
		{
			return visitor.Visit (this);
		}
	}

	public class RefTypeExpr : ShimExpression
	{
		public RefTypeExpr (Expression expr, Location loc)
			: base (expr)
		{
			this.loc = loc;
		}

		protected override Expression DoResolve (ResolveContext rc)
		{
			expr = expr.Resolve (rc);
			if (expr == null)
				return null;

			expr = Convert.ImplicitConversionRequired (rc, expr, rc.Module.PredefinedTypes.TypedReference.Resolve (), loc);
			if (expr == null)
				return null;

			type = rc.BuiltinTypes.Type;
			eclass = ExprClass.Value;
			return this;
		}

		public override void Emit (EmitContext ec)
		{
			expr.Emit (ec);
			ec.Emit (OpCodes.Refanytype);
			var m = ec.Module.PredefinedMembers.TypeGetTypeFromHandle.Resolve (loc);
			if (m != null)
				ec.Emit (OpCodes.Call, m);
		}
		
		public override object Accept (StructuralVisitor visitor)
		{
			return visitor.Visit (this);
		}
	}

	public class MakeRefExpr : ShimExpression
	{
		public MakeRefExpr (Expression expr, Location loc)
			: base (expr)
		{
			this.loc = loc;
		}

		public override bool ContainsEmitWithAwait ()
		{
			throw new NotImplementedException ();
		}

		protected override Expression DoResolve (ResolveContext rc)
		{
			expr = expr.ResolveLValue (rc, EmptyExpression.LValueMemberAccess);
			type = rc.Module.PredefinedTypes.TypedReference.Resolve ();
			eclass = ExprClass.Value;
			return this;
		}

		public override void Emit (EmitContext ec)
		{
			((IMemoryLocation) expr).AddressOf (ec, AddressOp.Load);
			ec.Emit (OpCodes.Mkrefany, expr.Type);
		}
		
		public override object Accept (StructuralVisitor visitor)
		{
			return visitor.Visit (this);
		}
	}

	/// <summary>
	///   Implements the typeof operator
	/// </summary>
	public class TypeOf : Expression {
		FullNamedExpression QueriedType;
		TypeSpec typearg;

		public TypeOf (FullNamedExpression queried_type, Location l)
		{
			QueriedType = queried_type;
			loc = l;
		}

		//
		// Use this constructor for any compiler generated typeof expression
		//
		public TypeOf (TypeSpec type, Location loc)
		{
			this.typearg = type;
			this.loc = loc;
		}

		#region Properties

		public override bool IsSideEffectFree {
			get {
				return true;
			}
		}

		public TypeSpec TypeArgument {
			get {
				return typearg;
			}
		}

		public FullNamedExpression TypeExpression {
			get {
				return QueriedType;
			}
		}

		#endregion


		protected override void CloneTo (CloneContext clonectx, Expression t)
		{
			TypeOf target = (TypeOf) t;
			if (QueriedType != null)
				target.QueriedType = (FullNamedExpression) QueriedType.Clone (clonectx);
		}

		public override bool ContainsEmitWithAwait ()
		{
			return false;
		}

		public override Expression CreateExpressionTree (ResolveContext ec)
		{
			Arguments args = new Arguments (2);
			args.Add (new Argument (this));
			args.Add (new Argument (new TypeOf (new TypeExpression (type, loc), loc)));
			return CreateExpressionFactoryCall (ec, "Constant", args);
		}

		protected override Expression DoResolve (ResolveContext ec)
		{
			if (eclass != ExprClass.Unresolved)
				return this;

			if (typearg == null) {
				//
				// Pointer types are allowed without explicit unsafe, they are just tokens
				//
				using (ec.Set (ResolveContext.Options.UnsafeScope)) {
					typearg = QueriedType.ResolveAsType (ec);
				}

				if (typearg == null)
					return null;

				if (typearg.BuiltinType == BuiltinTypeSpec.Type.Dynamic) {
					ec.Report.Error (1962, QueriedType.Location,
						"The typeof operator cannot be used on the dynamic type");
				}
			}

			type = ec.BuiltinTypes.Type;

			// Even though what is returned is a type object, it's treated as a value by the compiler.
			// In particular, 'typeof (Foo).X' is something totally different from 'Foo.X'.
			eclass = ExprClass.Value;
			return this;
		}

		static bool ContainsDynamicType (TypeSpec type)
		{
			if (type.BuiltinType == BuiltinTypeSpec.Type.Dynamic)
				return true;

			var element_container = type as ElementTypeSpec;
			if (element_container != null)
				return ContainsDynamicType (element_container.Element);

			foreach (var t in type.TypeArguments) {
				if (ContainsDynamicType (t)) {
					return true;
				}
			}

			return false;
		}

		public override void EncodeAttributeValue (IMemberContext rc, AttributeEncoder enc, TypeSpec targetType)
		{
			// Target type is not System.Type therefore must be object
			// and we need to use different encoding sequence
			if (targetType != type)
				enc.Encode (type);

			if (typearg is InflatedTypeSpec) {
				var gt = typearg;
				do {
					if (InflatedTypeSpec.ContainsTypeParameter (gt)) {
						rc.Module.Compiler.Report.Error (416, loc, "`{0}': an attribute argument cannot use type parameters",
							typearg.GetSignatureForError ());
						return;
					}

					gt = gt.DeclaringType;
				} while (gt != null);
			}

			if (ContainsDynamicType (typearg)) {
				Attribute.Error_AttributeArgumentIsDynamic (rc, loc);
				return;
			}

			enc.EncodeTypeName (typearg);
		}

		public override void Emit (EmitContext ec)
		{
			ec.Emit (OpCodes.Ldtoken, typearg);
			var m = ec.Module.PredefinedMembers.TypeGetTypeFromHandle.Resolve (loc);
			if (m != null)
				ec.Emit (OpCodes.Call, m);
		}
		
		public override object Accept (StructuralVisitor visitor)
		{
			return visitor.Visit (this);
		}
	}

	sealed class TypeOfMethod : TypeOfMember<MethodSpec>
	{
		public TypeOfMethod (MethodSpec method, Location loc)
			: base (method, loc)
		{
		}

		protected override Expression DoResolve (ResolveContext ec)
		{
			if (member.IsConstructor) {
				type = ec.Module.PredefinedTypes.ConstructorInfo.Resolve ();
			} else {
				type = ec.Module.PredefinedTypes.MethodInfo.Resolve ();
			}

			if (type == null)
				return null;

			return base.DoResolve (ec);
		}

		public override void Emit (EmitContext ec)
		{
			ec.Emit (OpCodes.Ldtoken, member);

			base.Emit (ec);
			ec.Emit (OpCodes.Castclass, type);
		}

		protected override PredefinedMember<MethodSpec> GetTypeFromHandle (EmitContext ec)
		{
			return ec.Module.PredefinedMembers.MethodInfoGetMethodFromHandle;
		}

		protected override PredefinedMember<MethodSpec> GetTypeFromHandleGeneric (EmitContext ec)
		{
			return ec.Module.PredefinedMembers.MethodInfoGetMethodFromHandle2;
		}
	}

	abstract class TypeOfMember<T> : Expression where T : MemberSpec
	{
		protected readonly T member;

		protected TypeOfMember (T member, Location loc)
		{
			this.member = member;
			this.loc = loc;
		}

		public override bool IsSideEffectFree {
			get {
				return true;
			}
		}

		public override bool ContainsEmitWithAwait ()
		{
			return false;
		}

		public override Expression CreateExpressionTree (ResolveContext ec)
		{
			Arguments args = new Arguments (2);
			args.Add (new Argument (this));
			args.Add (new Argument (new TypeOf (type, loc)));
			return CreateExpressionFactoryCall (ec, "Constant", args);
		}

		protected override Expression DoResolve (ResolveContext ec)
		{
			eclass = ExprClass.Value;
			return this;
		}

		public override void Emit (EmitContext ec)
		{
			bool is_generic = member.DeclaringType.IsGenericOrParentIsGeneric;
			PredefinedMember<MethodSpec> p;
			if (is_generic) {
				p = GetTypeFromHandleGeneric (ec);
				ec.Emit (OpCodes.Ldtoken, member.DeclaringType);
			} else {
				p = GetTypeFromHandle (ec);
			}

			var mi = p.Resolve (loc);
			if (mi != null)
				ec.Emit (OpCodes.Call, mi);
		}

		protected abstract PredefinedMember<MethodSpec> GetTypeFromHandle (EmitContext ec);
		protected abstract PredefinedMember<MethodSpec> GetTypeFromHandleGeneric (EmitContext ec);
	}

	sealed class TypeOfField : TypeOfMember<FieldSpec>
	{
		public TypeOfField (FieldSpec field, Location loc)
			: base (field, loc)
		{
		}

		protected override Expression DoResolve (ResolveContext ec)
		{
			type = ec.Module.PredefinedTypes.FieldInfo.Resolve ();
			if (type == null)
				return null;

			return base.DoResolve (ec);
		}

		public override void Emit (EmitContext ec)
		{
			ec.Emit (OpCodes.Ldtoken, member);
			base.Emit (ec);
		}

		protected override PredefinedMember<MethodSpec> GetTypeFromHandle (EmitContext ec)
		{
			return ec.Module.PredefinedMembers.FieldInfoGetFieldFromHandle;
		}

		protected override PredefinedMember<MethodSpec> GetTypeFromHandleGeneric (EmitContext ec)
		{
			return ec.Module.PredefinedMembers.FieldInfoGetFieldFromHandle2;
		}
	}

	/// <summary>
	///   Implements the sizeof expression
	/// </summary>
	public class SizeOf : Expression {
		readonly Expression texpr;
		TypeSpec type_queried;
		
		public SizeOf (Expression queried_type, Location l)
		{
			this.texpr = queried_type;
			loc = l;
		}

		public override bool IsSideEffectFree {
			get {
				return true;
			}
		}

		public Expression TypeExpression {
			get {
				return texpr;
			}
		}

		public override bool ContainsEmitWithAwait ()
		{
			return false;
		}

		public override Expression CreateExpressionTree (ResolveContext ec)
		{
			Error_PointerInsideExpressionTree (ec);
			return null;
		}

		protected override Expression DoResolve (ResolveContext ec)
		{
			type_queried = texpr.ResolveAsType (ec);
			if (type_queried == null)
				return null;

			if (type_queried.IsEnum)
				type_queried = EnumSpec.GetUnderlyingType (type_queried);

			int size_of = BuiltinTypeSpec.GetSize (type_queried);
			if (size_of > 0) {
				return new IntConstant (ec.BuiltinTypes, size_of, loc);
			}

			if (!TypeManager.VerifyUnmanaged (ec.Module, type_queried, loc)){
				return null;
			}

			if (!ec.IsUnsafe) {
				ec.Report.Error (233, loc,
					"`{0}' does not have a predefined size, therefore sizeof can only be used in an unsafe context (consider using System.Runtime.InteropServices.Marshal.SizeOf)",
					TypeManager.CSharpName (type_queried));
			}
			
			type = ec.BuiltinTypes.Int;
			eclass = ExprClass.Value;
			return this;
		}

		public override void Emit (EmitContext ec)
		{
			ec.Emit (OpCodes.Sizeof, type_queried);
		}

		protected override void CloneTo (CloneContext clonectx, Expression t)
		{
		}
		
		public override object Accept (StructuralVisitor visitor)
		{
			return visitor.Visit (this);
		}
	}

	/// <summary>
	///   Implements the qualified-alias-member (::) expression.
	/// </summary>
	public class QualifiedAliasMember : MemberAccess
	{
		public readonly string alias;
		public static readonly string GlobalAlias = "global";

		public QualifiedAliasMember (string alias, string identifier, Location l)
			: base (null, identifier, l)
		{
			this.alias = alias;
		}

		public QualifiedAliasMember (string alias, string identifier, TypeArguments targs, Location l)
			: base (null, identifier, targs, l)
		{
			this.alias = alias;
		}

		public QualifiedAliasMember (string alias, string identifier, int arity, Location l)
			: base (null, identifier, arity, l)
		{
			this.alias = alias;
		}

		public string Alias {
			get {
				return alias;
			}
		}

		public override FullNamedExpression ResolveAsTypeOrNamespace (IMemberContext ec)
		{
			if (alias == GlobalAlias) {
				expr = ec.Module.GlobalRootNamespace;
				return base.ResolveAsTypeOrNamespace (ec);
			}

			int errors = ec.Module.Compiler.Report.Errors;
			expr = ec.LookupNamespaceAlias (alias);
			if (expr == null) {
				if (errors == ec.Module.Compiler.Report.Errors)
					ec.Module.Compiler.Report.Error (432, loc, "Alias `{0}' not found", alias);
				return null;
			}
			
			return base.ResolveAsTypeOrNamespace (ec);
		}

		protected override Expression DoResolve (ResolveContext ec)
		{
			return ResolveAsTypeOrNamespace (ec);
		}

		public override string GetSignatureForError ()
		{
			string name = Name;
			if (targs != null) {
				name = Name + "<" + targs.GetSignatureForError () + ">";
			}

			return alias + "::" + name;
		}

		public override Expression LookupNameExpression (ResolveContext rc, MemberLookupRestrictions restrictions)
		{
			if ((restrictions & MemberLookupRestrictions.InvocableOnly) != 0) {
				rc.Module.Compiler.Report.Error (687, loc,
					"The namespace alias qualifier `::' cannot be used to invoke a method. Consider using `.' instead",
					GetSignatureForError ());

				return null;
			}

			return DoResolve (rc);
		}

		protected override void CloneTo (CloneContext clonectx, Expression t)
		{
			// Nothing 
		}
		
		public override object Accept (StructuralVisitor visitor)
		{
			return visitor.Visit (this);
		}
	}

	/// <summary>
	///   Implements the member access expression
	/// </summary>
	public class MemberAccess : ATypeNameExpression
	{
		protected Expression expr;

#if FULL_AST
		public Location DotLocation {
			get;
			set;
		}
#endif
		
		public MemberAccess (Expression expr, string id)
			: base (id, expr.Location)
		{
			this.expr = expr;
		}

		public MemberAccess (Expression expr, string identifier, Location loc)
			: base (identifier, loc)
		{
			this.expr = expr;
		}

		public MemberAccess (Expression expr, string identifier, TypeArguments args, Location loc)
			: base (identifier, args, loc)
		{
			this.expr = expr;
		}

		public MemberAccess (Expression expr, string identifier, int arity, Location loc)
			: base (identifier, arity, loc)
		{
			this.expr = expr;
		}

		public Expression LeftExpression {
			get {
				return expr;
			}
		}

		protected override Expression DoResolve (ResolveContext rc)
		{
			var e = DoResolveName (rc, null);

			if (!rc.OmitStructFlowAnalysis) {
				var fe = e as FieldExpr;
				if (fe != null) {
					fe.VerifyAssignedStructField (rc, null);
				}
			}

			return e;
		}

		public override Expression DoResolveLValue (ResolveContext rc, Expression rhs)
		{
			var e = DoResolveName (rc, rhs);

			if (!rc.OmitStructFlowAnalysis) {
				var fe = e as FieldExpr;
				if (fe != null && fe.InstanceExpression is FieldExpr) {
					fe = (FieldExpr) fe.InstanceExpression;
					fe.VerifyAssignedStructField (rc, rhs);
				}
			}

			return e;
		}

		Expression DoResolveName (ResolveContext rc, Expression right_side)
		{
			Expression e = LookupNameExpression (rc, right_side == null ? MemberLookupRestrictions.ReadAccess : MemberLookupRestrictions.None);
			if (e == null)
				return null;

			if (right_side != null) {
				if (e is TypeExpr) {
					e.Error_UnexpectedKind (rc, ResolveFlags.VariableOrValue, loc);
					return null;
				}

				e = e.ResolveLValue (rc, right_side);
			} else {
				e = e.Resolve (rc, ResolveFlags.VariableOrValue | ResolveFlags.Type);
			}

			return e;
		}

		protected virtual void Error_OperatorCannotBeApplied (ResolveContext rc, TypeSpec type)
		{
			if (type == InternalType.NullLiteral && rc.IsRuntimeBinder)
				rc.Report.Error (Report.RuntimeErrorId, loc, "Cannot perform member binding on `null' value");
			else
				expr.Error_OperatorCannotBeApplied (rc, loc, ".", type);
		}

		public static bool IsValidDotExpression (TypeSpec type)
		{
			const MemberKind dot_kinds = MemberKind.Class | MemberKind.Struct | MemberKind.Delegate | MemberKind.Enum |
				MemberKind.Interface | MemberKind.TypeParameter | MemberKind.ArrayType;

			return (type.Kind & dot_kinds) != 0 || type.BuiltinType == BuiltinTypeSpec.Type.Dynamic;
		}

		public override Expression LookupNameExpression (ResolveContext rc, MemberLookupRestrictions restrictions)
		{
			var sn = expr as SimpleName;
			const ResolveFlags flags = ResolveFlags.VariableOrValue | ResolveFlags.Type;

			//
			// Resolve the expression with flow analysis turned off, we'll do the definite
			// assignment checks later.  This is because we don't know yet what the expression
			// will resolve to - it may resolve to a FieldExpr and in this case we must do the
			// definite assignment check on the actual field and not on the whole struct.
			//
			using (rc.Set (ResolveContext.Options.OmitStructFlowAnalysis)) {
				if (sn != null) {
					expr = sn.LookupNameExpression (rc, MemberLookupRestrictions.ReadAccess | MemberLookupRestrictions.ExactArity);

					//
					// Resolve expression which does have type set as we need expression type
					// with disable flow analysis as we don't know whether left side expression
					// is used as variable or type
					//
					if (expr is VariableReference || expr is ConstantExpr || expr is Linq.TransparentMemberAccess) {
						using (rc.With (ResolveContext.Options.DoFlowAnalysis, false)) {
							expr = expr.Resolve (rc);
						}
					} else if (expr is TypeParameterExpr) {
						expr.Error_UnexpectedKind (rc, flags, sn.Location);
						expr = null;
					}
				} else {
					expr = expr.Resolve (rc, flags);
				}
			}

			if (expr == null)
				return null;

			Namespace ns = expr as Namespace;
			if (ns != null) {
				var retval = ns.LookupTypeOrNamespace (rc, Name, Arity, LookupMode.Normal, loc);

				if (retval == null) {
					ns.Error_NamespaceDoesNotExist (rc, Name, Arity, loc);
					return null;
				}

				if (HasTypeArguments)
					return new GenericTypeExpr (retval.Type, targs, loc);

				return retval;
			}

			MemberExpr me;
			TypeSpec expr_type = expr.Type;
			if (expr_type.BuiltinType == BuiltinTypeSpec.Type.Dynamic) {
				me = expr as MemberExpr;
				if (me != null)
					me.ResolveInstanceExpression (rc, null);

				//
				// Run defined assigned checks on expressions resolved with
				// disabled flow-analysis
				//
				if (sn != null) {
					var vr = expr as VariableReference;
					if (vr != null)
						vr.VerifyAssigned (rc);
				}

				Arguments args = new Arguments (1);
				args.Add (new Argument (expr));
				return new DynamicMemberBinder (Name, args, loc);
			}

			if (!IsValidDotExpression (expr_type)) {
				Error_OperatorCannotBeApplied (rc, expr_type);
				return null;
			}

			var lookup_arity = Arity;
			bool errorMode = false;
			Expression member_lookup;
			while (true) {
				member_lookup = MemberLookup (rc, errorMode, expr_type, Name, lookup_arity, restrictions, loc);
				if (member_lookup == null) {
					//
					// Try to look for extension method when member lookup failed
					//
					if (MethodGroupExpr.IsExtensionMethodArgument (expr)) {
						var methods = rc.LookupExtensionMethod (expr_type, Name, lookup_arity);
						if (methods != null) {
							var emg = new ExtensionMethodGroupExpr (methods, expr, loc);
							if (HasTypeArguments) {
								if (!targs.Resolve (rc))
									return null;

								emg.SetTypeArguments (rc, targs);
							}

							//
							// Run defined assigned checks on expressions resolved with
							// disabled flow-analysis
							//
							if (sn != null && !errorMode) {
								var vr = expr as VariableReference;
								if (vr != null)
									vr.VerifyAssigned (rc);
							}

							// TODO: it should really skip the checks bellow
							return emg.Resolve (rc);
						}
					}
				}

				if (errorMode) {
					if (member_lookup == null) {
						var dep = expr_type.GetMissingDependencies ();
						if (dep != null) {
							ImportedTypeDefinition.Error_MissingDependency (rc, dep, loc);
						} else if (expr is TypeExpr) {
							base.Error_TypeDoesNotContainDefinition (rc, expr_type, Name);
						} else {
							Error_TypeDoesNotContainDefinition (rc, expr_type, Name);
						}

						return null;
					}

					if (member_lookup is MethodGroupExpr) {
						// Leave it to overload resolution to report correct error
					} else if (!(member_lookup is TypeExpr)) {
						// TODO: rc.SymbolRelatedToPreviousError
						ErrorIsInaccesible (rc, member_lookup.GetSignatureForError (), loc);
					}
					break;
				}

				if (member_lookup != null)
					break;

				lookup_arity = 0;
				restrictions &= ~MemberLookupRestrictions.InvocableOnly;
				errorMode = true;
			}

			TypeExpr texpr = member_lookup as TypeExpr;
			if (texpr != null) {
				if (!(expr is TypeExpr)) {
					me = expr as MemberExpr;
					if (me == null || me.ProbeIdenticalTypeName (rc, expr, sn) == expr) {
						rc.Report.Error (572, loc, "`{0}': cannot reference a type through an expression; try `{1}' instead",
							Name, member_lookup.GetSignatureForError ());
						return null;
					}
				}

				if (!texpr.Type.IsAccessible (rc)) {
					rc.Report.SymbolRelatedToPreviousError (member_lookup.Type);
					ErrorIsInaccesible (rc, member_lookup.Type.GetSignatureForError (), loc);
					return null;
				}

				if (HasTypeArguments) {
					return new GenericTypeExpr (member_lookup.Type, targs, loc);
				}

				return member_lookup;
			}

			me = member_lookup as MemberExpr;

			if (sn != null && me.IsStatic && (expr = me.ProbeIdenticalTypeName (rc, expr, sn)) != expr) {
				sn = null;
			}

			me = me.ResolveMemberAccess (rc, expr, sn);

			if (Arity > 0) {
				if (!targs.Resolve (rc))
					return null;

				me.SetTypeArguments (rc, targs);
			}

			//
			// Run defined assigned checks on expressions resolved with
			// disabled flow-analysis
			//
			if (sn != null && !(me is FieldExpr && TypeSpec.IsValueType (expr_type))) {
				var vr = expr as VariableReference;
				if (vr != null)
					vr.VerifyAssigned (rc);
			}

			return me;
		}

		public override FullNamedExpression ResolveAsTypeOrNamespace (IMemberContext rc)
		{
			FullNamedExpression fexpr = expr as FullNamedExpression;
			if (fexpr == null) {
				expr.ResolveAsType (rc);
				return null;
			}

			FullNamedExpression expr_resolved = fexpr.ResolveAsTypeOrNamespace (rc);

			if (expr_resolved == null)
				return null;

			Namespace ns = expr_resolved as Namespace;
			if (ns != null) {
				FullNamedExpression retval = ns.LookupTypeOrNamespace (rc, Name, Arity, LookupMode.Normal, loc);

				if (retval == null) {
					ns.Error_NamespaceDoesNotExist (rc, Name, Arity, loc);
				} else if (HasTypeArguments) {
					retval = new GenericTypeExpr (retval.Type, targs, loc);
					if (retval.ResolveAsType (rc) == null)
						return null;
				}

				return retval;
			}

			var tnew_expr = expr_resolved.ResolveAsType (rc);
			if (tnew_expr == null)
				return null;

			TypeSpec expr_type = tnew_expr;
			if (TypeManager.IsGenericParameter (expr_type)) {
				rc.Module.Compiler.Report.Error (704, loc, "A nested type cannot be specified through a type parameter `{0}'",
					tnew_expr.GetSignatureForError ());
				return null;
			}

			var qam = this as QualifiedAliasMember;
			if (qam != null) {
				rc.Module.Compiler.Report.Error (431, loc,
					"Alias `{0}' cannot be used with `::' since it denotes a type. Consider replacing `::' with `.'",
					qam.Alias);

			}

			TypeSpec nested = null;
			while (expr_type != null) {
				nested = MemberCache.FindNestedType (expr_type, Name, Arity);
				if (nested == null) {
					if (expr_type == tnew_expr) {
						Error_IdentifierNotFound (rc, expr_type, Name);
						return null;
					}

					expr_type = tnew_expr;
					nested = MemberCache.FindNestedType (expr_type, Name, Arity);
					ErrorIsInaccesible (rc, nested.GetSignatureForError (), loc);
					break;
				}

				if (nested.IsAccessible (rc))
					break;

				//
				// Keep looking after inaccessible candidate but only if
				// we are not in same context as the definition itself
 				//
				if (expr_type.MemberDefinition == rc.CurrentMemberDefinition)
					break;

				expr_type = expr_type.BaseType;
			}
			
			TypeExpr texpr;
			if (Arity > 0) {
				if (HasTypeArguments) {
					texpr = new GenericTypeExpr (nested, targs, loc);
				} else {
					texpr = new GenericOpenTypeExpr (nested, loc);
				}
			} else {
				texpr = new TypeExpression (nested, loc);
			}

			if (texpr.ResolveAsType (rc) == null)
				return null;

			return texpr;
		}

		protected virtual void Error_IdentifierNotFound (IMemberContext rc, TypeSpec expr_type, string identifier)
		{
			var nested = MemberCache.FindNestedType (expr_type, Name, -System.Math.Max (1, Arity));

			if (nested != null) {
				Error_TypeArgumentsCannotBeUsed (rc, nested, Arity, expr.Location);
				return;
			}

			var any_other_member = MemberLookup (rc, false, expr_type, Name, 0, MemberLookupRestrictions.None, loc);
			if (any_other_member != null) {
				any_other_member.Error_UnexpectedKind (rc, any_other_member, "type", any_other_member.ExprClassName, loc);
				return;
			}

			rc.Module.Compiler.Report.Error (426, loc, "The nested type `{0}' does not exist in the type `{1}'",
				Name, expr_type.GetSignatureForError ());
		}

		protected override void Error_TypeDoesNotContainDefinition (ResolveContext ec, TypeSpec type, string name)
		{
			if (ec.Module.Compiler.Settings.Version > LanguageVersion.ISO_2 && !ec.IsRuntimeBinder && MethodGroupExpr.IsExtensionMethodArgument (expr)) {
				ec.Report.SymbolRelatedToPreviousError (type);
				ec.Report.Error (1061, loc,
					"Type `{0}' does not contain a definition for `{1}' and no extension method `{1}' of type `{0}' could be found (are you missing a using directive or an assembly reference?)",
					type.GetSignatureForError (), name);
				return;
			}

			base.Error_TypeDoesNotContainDefinition (ec, type, name);
		}

		public override string GetSignatureForError ()
		{
			return expr.GetSignatureForError () + "." + base.GetSignatureForError ();
		}

		protected override void CloneTo (CloneContext clonectx, Expression t)
		{
			MemberAccess target = (MemberAccess) t;

			target.expr = expr.Clone (clonectx);
		}
		
		public override object Accept (StructuralVisitor visitor)
		{
			return visitor.Visit (this);
		}
	}

	/// <summary>
	///   Implements checked expressions
	/// </summary>
	public class CheckedExpr : Expression {

		public Expression Expr;
		
		public CheckedExpr (Expression e, Location l)
		{
			Expr = e;
			loc = l;
		}

		public override bool ContainsEmitWithAwait ()
		{
			return Expr.ContainsEmitWithAwait ();
		}
		
		public override Expression CreateExpressionTree (ResolveContext ec)
		{
			using (ec.With (ResolveContext.Options.AllCheckStateFlags, true))
				return Expr.CreateExpressionTree (ec);
		}

		protected override Expression DoResolve (ResolveContext ec)
		{
			using (ec.With (ResolveContext.Options.AllCheckStateFlags, true))
				Expr = Expr.Resolve (ec);
			
			if (Expr == null)
				return null;

			if (Expr is Constant || Expr is MethodGroupExpr || Expr is AnonymousMethodExpression || Expr is DefaultValueExpression)
				return Expr;
			
			eclass = Expr.eclass;
			type = Expr.Type;
			return this;
		}

		public override void Emit (EmitContext ec)
		{
			using (ec.With (EmitContext.Options.CheckedScope, true))
				Expr.Emit (ec);
		}

		public override void EmitBranchable (EmitContext ec, Label target, bool on_true)
		{
			using (ec.With (EmitContext.Options.CheckedScope, true))
				Expr.EmitBranchable (ec, target, on_true);
		}

		public override SLE.Expression MakeExpression (BuilderContext ctx)
		{
			using (ctx.With (BuilderContext.Options.CheckedScope, true)) {
				return Expr.MakeExpression (ctx);
			}
		}

		protected override void CloneTo (CloneContext clonectx, Expression t)
		{
			CheckedExpr target = (CheckedExpr) t;

			target.Expr = Expr.Clone (clonectx);
		}

		public override object Accept (StructuralVisitor visitor)
		{
			return visitor.Visit (this);
		}
	}

	/// <summary>
	///   Implements the unchecked expression
	/// </summary>
	public class UnCheckedExpr : Expression {

		public Expression Expr;

		public UnCheckedExpr (Expression e, Location l)
		{
			Expr = e;
			loc = l;
		}

		public override bool ContainsEmitWithAwait ()
		{
			return Expr.ContainsEmitWithAwait ();
		}
		
		public override Expression CreateExpressionTree (ResolveContext ec)
		{
			using (ec.With (ResolveContext.Options.AllCheckStateFlags, false))
				return Expr.CreateExpressionTree (ec);
		}

		protected override Expression DoResolve (ResolveContext ec)
		{
			using (ec.With (ResolveContext.Options.AllCheckStateFlags, false))
				Expr = Expr.Resolve (ec);

			if (Expr == null)
				return null;

			if (Expr is Constant || Expr is MethodGroupExpr || Expr is AnonymousMethodExpression || Expr is DefaultValueExpression)
				return Expr;
			
			eclass = Expr.eclass;
			type = Expr.Type;
			return this;
		}

		public override void Emit (EmitContext ec)
		{
			using (ec.With (EmitContext.Options.CheckedScope, false))
				Expr.Emit (ec);
		}

		public override void EmitBranchable (EmitContext ec, Label target, bool on_true)
		{
			using (ec.With (EmitContext.Options.CheckedScope, false))
				Expr.EmitBranchable (ec, target, on_true);
		}

		protected override void CloneTo (CloneContext clonectx, Expression t)
		{
			UnCheckedExpr target = (UnCheckedExpr) t;

			target.Expr = Expr.Clone (clonectx);
		}

		public override object Accept (StructuralVisitor visitor)
		{
			return visitor.Visit (this);
		}
	}

	/// <summary>
	///   An Element Access expression.
	///
	///   During semantic analysis these are transformed into 
	///   IndexerAccess, ArrayAccess or a PointerArithmetic.
	/// </summary>
	public class ElementAccess : Expression
	{
		public Arguments Arguments;
		public Expression Expr;

		public ElementAccess (Expression e, Arguments args, Location loc)
		{
			Expr = e;
			this.loc = loc;
			this.Arguments = args;
		}

		public override bool ContainsEmitWithAwait ()
		{
			return Expr.ContainsEmitWithAwait () || Arguments.ContainsEmitWithAwait ();
		}

		//
		// We perform some simple tests, and then to "split" the emit and store
		// code we create an instance of a different class, and return that.
		//
		Expression CreateAccessExpression (ResolveContext ec)
		{
			if (type.IsArray)
				return (new ArrayAccess (this, loc));

			if (type.IsPointer)
				return MakePointerAccess (ec, type);

			FieldExpr fe = Expr as FieldExpr;
			if (fe != null) {
				var ff = fe.Spec as FixedFieldSpec;
				if (ff != null) {
					return MakePointerAccess (ec, ff.ElementType);
				}
			}

			var indexers = MemberCache.FindMembers (type, MemberCache.IndexerNameAlias, false);
			if (indexers != null || type.BuiltinType == BuiltinTypeSpec.Type.Dynamic) {
				return new IndexerExpr (indexers, type, this);
			}

			if (type != InternalType.ErrorType) {
				ec.Report.Error (21, loc, "Cannot apply indexing with [] to an expression of type `{0}'",
					type.GetSignatureForError ());
			}

			return null;
		}

		public override Expression CreateExpressionTree (ResolveContext ec)
		{
			Arguments args = Arguments.CreateForExpressionTree (ec, Arguments,
				Expr.CreateExpressionTree (ec));

			return CreateExpressionFactoryCall (ec, "ArrayIndex", args);
		}

		Expression MakePointerAccess (ResolveContext ec, TypeSpec type)
		{
			if (Arguments.Count != 1){
				ec.Report.Error (196, loc, "A pointer must be indexed by only one value");
				return null;
			}

			if (Arguments [0] is NamedArgument)
				Error_NamedArgument ((NamedArgument) Arguments[0], ec.Report);

			Expression p = new PointerArithmetic (Binary.Operator.Addition, Expr, Arguments [0].Expr.Resolve (ec), type, loc);
			return new Indirection (p, loc);
		}
		
		protected override Expression DoResolve (ResolveContext ec)
		{
			Expr = Expr.Resolve (ec);
			if (Expr == null)
				return null;

			type = Expr.Type;

			// TODO: Create 1 result for Resolve and ResolveLValue ?
			var res = CreateAccessExpression (ec);
			if (res == null)
				return null;

			return res.Resolve (ec);
		}

		public override Expression DoResolveLValue (ResolveContext ec, Expression rhs)
		{
			Expr = Expr.Resolve (ec);
			if (Expr == null)
				return null;

			type = Expr.Type;

			var res = CreateAccessExpression (ec);
			if (res == null)
				return null;

			bool lvalue_instance = rhs != null && type.IsStruct && (Expr is Invocation || Expr is PropertyExpr);
			if (lvalue_instance) {
				Expr.Error_ValueAssignment (ec, EmptyExpression.LValueMemberAccess);
			}

			return res.ResolveLValue (ec, rhs);
		}
		
		public override void Emit (EmitContext ec)
		{
			throw new Exception ("Should never be reached");
		}

		public static void Error_NamedArgument (NamedArgument na, Report Report)
		{
			Report.Error (1742, na.Location, "An element access expression cannot use named argument");
		}

		public override string GetSignatureForError ()
		{
			return Expr.GetSignatureForError ();
		}

		protected override void CloneTo (CloneContext clonectx, Expression t)
		{
			ElementAccess target = (ElementAccess) t;

			target.Expr = Expr.Clone (clonectx);
			if (Arguments != null)
				target.Arguments = Arguments.Clone (clonectx);
		}
		
		public override object Accept (StructuralVisitor visitor)
		{
			return visitor.Visit (this);
		}
	}

	/// <summary>
	///   Implements array access 
	/// </summary>
	public class ArrayAccess : Expression, IDynamicAssign, IMemoryLocation {
		//
		// Points to our "data" repository
		//
		ElementAccess ea;

		LocalTemporary temp;
		bool prepared;
		bool? has_await_args;
		
		public ArrayAccess (ElementAccess ea_data, Location l)
		{
			ea = ea_data;
			loc = l;
		}

		public void AddressOf (EmitContext ec, AddressOp mode)
		{
			var ac = (ArrayContainer) ea.Expr.Type;

			LoadInstanceAndArguments (ec, false, false);

			if (ac.Element.IsGenericParameter && mode == AddressOp.Load)
				ec.Emit (OpCodes.Readonly);

			ec.EmitArrayAddress (ac);
		}

		public override Expression CreateExpressionTree (ResolveContext ec)
		{
			return ea.CreateExpressionTree (ec);
		}

		public override bool ContainsEmitWithAwait ()
		{
			return ea.ContainsEmitWithAwait ();
		}

		public override Expression DoResolveLValue (ResolveContext ec, Expression right_side)
		{
			return DoResolve (ec);
		}

		protected override Expression DoResolve (ResolveContext ec)
		{
			// dynamic is used per argument in ConvertExpressionToArrayIndex case
			bool dynamic;
			ea.Arguments.Resolve (ec, out dynamic);

			var ac = ea.Expr.Type as ArrayContainer;
			int rank = ea.Arguments.Count;
			if (ac.Rank != rank) {
				ec.Report.Error (22, ea.Location, "Wrong number of indexes `{0}' inside [], expected `{1}'",
					  rank.ToString (), ac.Rank.ToString ());
				return null;
			}

			type = ac.Element;
			if (type.IsPointer && !ec.IsUnsafe) {
				UnsafeError (ec, ea.Location);
			}

			foreach (Argument a in ea.Arguments) {
				if (a is NamedArgument)
					ElementAccess.Error_NamedArgument ((NamedArgument) a, ec.Report);

				a.Expr = ConvertExpressionToArrayIndex (ec, a.Expr);
			}
			
			eclass = ExprClass.Variable;

			return this;
		}

		protected override void Error_NegativeArrayIndex (ResolveContext ec, Location loc)
		{
			ec.Report.Warning (251, 2, loc, "Indexing an array with a negative index (array indices always start at zero)");
		}

		//
		// Load the array arguments into the stack.
		//
		void LoadInstanceAndArguments (EmitContext ec, bool duplicateArguments, bool prepareAwait)
		{
			if (prepareAwait) {
				ea.Expr = ea.Expr.EmitToField (ec);
			} else if (duplicateArguments) {
				ea.Expr.Emit (ec);
				ec.Emit (OpCodes.Dup);

				var copy = new LocalTemporary (ea.Expr.Type);
				copy.Store (ec);
				ea.Expr = copy;
			} else {
				ea.Expr.Emit (ec);
			}

			var dup_args = ea.Arguments.Emit (ec, duplicateArguments, prepareAwait);
			if (dup_args != null)
				ea.Arguments = dup_args;
		}

		public void Emit (EmitContext ec, bool leave_copy)
		{
			var ac = ea.Expr.Type as ArrayContainer;

			if (prepared) {
				ec.EmitLoadFromPtr (type);
			} else {
				if (!has_await_args.HasValue && ec.HasSet (BuilderContext.Options.AsyncBody) && ea.Arguments.ContainsEmitWithAwait ()) {
					LoadInstanceAndArguments (ec, false, true);
				}

				LoadInstanceAndArguments (ec, false, false);
				ec.EmitArrayLoad (ac);
			}	

			if (leave_copy) {
				ec.Emit (OpCodes.Dup);
				temp = new LocalTemporary (this.type);
				temp.Store (ec);
			}
		}
		
		public override void Emit (EmitContext ec)
		{
			Emit (ec, false);
		}

		public void EmitAssign (EmitContext ec, Expression source, bool leave_copy, bool isCompound)
		{
			var ac = (ArrayContainer) ea.Expr.Type;
			TypeSpec t = source.Type;

			has_await_args = ec.HasSet (BuilderContext.Options.AsyncBody) && (ea.Arguments.ContainsEmitWithAwait () || source.ContainsEmitWithAwait ());

			//
			// When we are dealing with a struct, get the address of it to avoid value copy
			// Same cannot be done for reference type because array covariance and the
			// check in ldelema requires to specify the type of array element stored at the index
			//
			if (t.IsStruct && ((isCompound && !(source is DynamicExpressionStatement)) || !BuiltinTypeSpec.IsPrimitiveType (t))) {
				LoadInstanceAndArguments (ec, false, has_await_args.Value);

				if (has_await_args.Value) {
					if (source.ContainsEmitWithAwait ()) {
						source = source.EmitToField (ec);
						isCompound = false;
						prepared = true;
					}

					LoadInstanceAndArguments (ec, isCompound, false);
				} else {
					prepared = true;
				}

				ec.EmitArrayAddress (ac);

				if (isCompound) {
					ec.Emit (OpCodes.Dup);
					prepared = true;
				}
			} else {
				LoadInstanceAndArguments (ec, isCompound, has_await_args.Value);

				if (has_await_args.Value) {
					if (source.ContainsEmitWithAwait ())
						source = source.EmitToField (ec);

					LoadInstanceAndArguments (ec, false, false);
				}
			}

			source.Emit (ec);

			if (isCompound) {
				var lt = ea.Expr as LocalTemporary;
				if (lt != null)
					lt.Release (ec);
			}

			if (leave_copy) {
				ec.Emit (OpCodes.Dup);
				temp = new LocalTemporary (this.type);
				temp.Store (ec);
			}

			if (prepared) {
				ec.EmitStoreFromPtr (t);
			} else {
				ec.EmitArrayStore (ac);
			}
			
			if (temp != null) {
				temp.Emit (ec);
				temp.Release (ec);
			}
		}

		public override Expression EmitToField (EmitContext ec)
		{
			//
			// Have to be specialized for arrays to get access to
			// underlying element. Instead of another result copy we
			// need direct access to element 
			//
			// Consider:
			//
			// CallRef (ref a[await Task.Factory.StartNew (() => 1)]);
			//
			ea.Expr = ea.Expr.EmitToField (ec);
			return this;
		}

		public SLE.Expression MakeAssignExpression (BuilderContext ctx, Expression source)
		{
#if NET_4_0 || MONODROID
			return SLE.Expression.ArrayAccess (ea.Expr.MakeExpression (ctx), MakeExpressionArguments (ctx));
#else
			throw new NotImplementedException ();
#endif
		}

		public override SLE.Expression MakeExpression (BuilderContext ctx)
		{
			return SLE.Expression.ArrayIndex (ea.Expr.MakeExpression (ctx), MakeExpressionArguments (ctx));
		}

		SLE.Expression[] MakeExpressionArguments (BuilderContext ctx)
		{
			using (ctx.With (BuilderContext.Options.CheckedScope, true)) {
				return Arguments.MakeExpression (ea.Arguments, ctx);
			}
		}
	}

	//
	// Indexer access expression
	//
	sealed class IndexerExpr : PropertyOrIndexerExpr<IndexerSpec>, OverloadResolver.IBaseMembersProvider
	{
		IList<MemberSpec> indexers;
		Arguments arguments;
		TypeSpec queried_type;
		
		public IndexerExpr (IList<MemberSpec> indexers, TypeSpec queriedType, ElementAccess ea)
			: base (ea.Location)
		{
			this.indexers = indexers;
			this.queried_type = queriedType;
			this.InstanceExpression = ea.Expr;
			this.arguments = ea.Arguments;
		}

		#region Properties

		protected override Arguments Arguments {
			get {
				return arguments;
			}
			set {
				arguments = value;
			}
		}

		protected override TypeSpec DeclaringType {
			get {
				return best_candidate.DeclaringType;
			}
		}

		public override bool IsInstance {
			get {
				return true;
			}
		}

		public override bool IsStatic {
			get {
				return false;
			}
		}

		public override string KindName {
			get { return "indexer"; }
		}

		public override string Name {
			get {
				return "this";
			}
		}

		#endregion

		public override bool ContainsEmitWithAwait ()
		{
			return base.ContainsEmitWithAwait () || arguments.ContainsEmitWithAwait ();
		}

		public override Expression CreateExpressionTree (ResolveContext ec)
		{
			Arguments args = Arguments.CreateForExpressionTree (ec, arguments,
				InstanceExpression.CreateExpressionTree (ec),
				new TypeOfMethod (Getter, loc));

			return CreateExpressionFactoryCall (ec, "Call", args);
		}
	
		public override void EmitAssign (EmitContext ec, Expression source, bool leave_copy, bool isCompound)
		{
			LocalTemporary await_source_arg = null;

			if (isCompound) {
				emitting_compound_assignment = true;
				if (source is DynamicExpressionStatement) {
					Emit (ec, false);
				} else {
					source.Emit (ec);
				}
				emitting_compound_assignment = false;

				if (has_await_arguments) {
					await_source_arg = new LocalTemporary (Type);
					await_source_arg.Store (ec);

					arguments.Add (new Argument (await_source_arg));

					if (leave_copy) {
						temp = await_source_arg;
					}

					has_await_arguments = false;
				} else {
					arguments = null;

					if (leave_copy) {
						ec.Emit (OpCodes.Dup);
						temp = new LocalTemporary (Type);
						temp.Store (ec);
					}
				}
			} else {
				if (leave_copy) {
					if (ec.HasSet (BuilderContext.Options.AsyncBody) && (arguments.ContainsEmitWithAwait () || source.ContainsEmitWithAwait ())) {
						source = source.EmitToField (ec);
					} else {
						temp = new LocalTemporary (Type);
						source.Emit (ec);
						temp.Store (ec);
						source = temp;
					}
				}

				arguments.Add (new Argument (source));
			}

			var call = new CallEmitter ();
			call.InstanceExpression = InstanceExpression;
			if (arguments == null)
				call.InstanceExpressionOnStack = true;

			call.Emit (ec, Setter, arguments, loc);

			if (temp != null) {
				temp.Emit (ec);
				temp.Release (ec);
			} else if (leave_copy) {
				source.Emit (ec);
			}

			if (await_source_arg != null) {
				await_source_arg.Release (ec);
			}
		}

		public override string GetSignatureForError ()
		{
			return best_candidate.GetSignatureForError ();
		}
		
		public override SLE.Expression MakeAssignExpression (BuilderContext ctx, Expression source)
		{
#if STATIC
			throw new NotSupportedException ();
#else
			var value = new[] { source.MakeExpression (ctx) };
			var args = Arguments.MakeExpression (arguments, ctx).Concat (value);
#if NET_4_0 || MONODROID
			return SLE.Expression.Block (
					SLE.Expression.Call (InstanceExpression.MakeExpression (ctx), (MethodInfo) Setter.GetMetaInfo (), args),
					value [0]);
#else
			return args.First ();
#endif
#endif
		}

		public override SLE.Expression MakeExpression (BuilderContext ctx)
		{
#if STATIC
			return base.MakeExpression (ctx);
#else
			var args = Arguments.MakeExpression (arguments, ctx);
			return SLE.Expression.Call (InstanceExpression.MakeExpression (ctx), (MethodInfo) Getter.GetMetaInfo (), args);
#endif
		}

		protected override Expression OverloadResolve (ResolveContext rc, Expression right_side)
		{
			if (best_candidate != null)
				return this;

			eclass = ExprClass.IndexerAccess;

			bool dynamic;
			arguments.Resolve (rc, out dynamic);

			if (indexers == null && InstanceExpression.Type.BuiltinType == BuiltinTypeSpec.Type.Dynamic) {
				dynamic = true;
			} else {
				var res = new OverloadResolver (indexers, OverloadResolver.Restrictions.None, loc);
				res.BaseMembersProvider = this;
				res.InstanceQualifier = this;

				// TODO: Do I need 2 argument sets?
				best_candidate = res.ResolveMember<IndexerSpec> (rc, ref arguments);
				if (best_candidate != null)
					type = res.BestCandidateReturnType;
				else if (!res.BestCandidateIsDynamic)
					return null;
			}

			//
			// It has dynamic arguments
			//
			if (dynamic) {
				Arguments args = new Arguments (arguments.Count + 1);
				if (IsBase) {
					rc.Report.Error (1972, loc,
						"The indexer base access cannot be dynamically dispatched. Consider casting the dynamic arguments or eliminating the base access");
				} else {
					args.Add (new Argument (InstanceExpression));
				}
				args.AddRange (arguments);

				best_candidate = null;
				return new DynamicIndexBinder (args, loc);
			}

			//
			// Try to avoid resolving left expression again
			//
			if (right_side != null)
				ResolveInstanceExpression (rc, right_side);

			return this;
		}

		protected override void CloneTo (CloneContext clonectx, Expression t)
		{
			IndexerExpr target = (IndexerExpr) t;

			if (arguments != null)
				target.arguments = arguments.Clone (clonectx);
		}

		public override void SetTypeArguments (ResolveContext ec, TypeArguments ta)
		{
			Error_TypeArgumentsCannotBeUsed (ec, "indexer", GetSignatureForError (), loc);
		}

		#region IBaseMembersProvider Members

		IList<MemberSpec> OverloadResolver.IBaseMembersProvider.GetBaseMembers (TypeSpec baseType)
		{
			return baseType == null ? null : MemberCache.FindMembers (baseType, MemberCache.IndexerNameAlias, false);
		}

		IParametersMember OverloadResolver.IBaseMembersProvider.GetOverrideMemberParameters (MemberSpec member)
		{
			if (queried_type == member.DeclaringType)
				return null;

			var filter = new MemberFilter (MemberCache.IndexerNameAlias, 0, MemberKind.Indexer, ((IndexerSpec) member).Parameters, null);
			return MemberCache.FindMember (queried_type, filter, BindingRestriction.InstanceOnly | BindingRestriction.OverrideOnly) as IParametersMember;
		}

		MethodGroupExpr OverloadResolver.IBaseMembersProvider.LookupExtensionMethod (ResolveContext rc)
		{
			return null;
		}

		#endregion
	}

	//
	// A base access expression
	//
	public class BaseThis : This
	{
		public BaseThis (Location loc)
			: base (loc)
		{
		}

		public BaseThis (TypeSpec type, Location loc)
			: base (loc)
		{
			this.type = type;
			eclass = ExprClass.Variable;
		}

		#region Properties

		public override string Name {
			get {
				return "base";
			}
		}

		#endregion

		public override Expression CreateExpressionTree (ResolveContext ec)
		{
			ec.Report.Error (831, loc, "An expression tree may not contain a base access");
			return base.CreateExpressionTree (ec);
		}

		public override void Emit (EmitContext ec)
		{
			base.Emit (ec);

			var context_type = ec.CurrentType;
			if (context_type.IsStruct) {
				ec.Emit (OpCodes.Ldobj, context_type);
				ec.Emit (OpCodes.Box, context_type);
			}
		}

		protected override void Error_ThisNotAvailable (ResolveContext ec)
		{
			if (ec.IsStatic) {
				ec.Report.Error (1511, loc, "Keyword `base' is not available in a static method");
			} else {
				ec.Report.Error (1512, loc, "Keyword `base' is not available in the current context");
			}
		}

		public override void ResolveBase (ResolveContext ec)
		{
			base.ResolveBase (ec);
			type = ec.CurrentType.BaseType;
		}

		public override object Accept (StructuralVisitor visitor)
		{
			return visitor.Visit (this);
		}
	}

	/// <summary>
	///   This class exists solely to pass the Type around and to be a dummy
	///   that can be passed to the conversion functions (this is used by
	///   foreach implementation to typecast the object return value from
	///   get_Current into the proper type.  All code has been generated and
	///   we only care about the side effect conversions to be performed
	///
	///   This is also now used as a placeholder where a no-action expression
	///   is needed (the `New' class).
	/// </summary>
	public class EmptyExpression : Expression
	{
		sealed class OutAccessExpression : EmptyExpression
		{
			public OutAccessExpression (TypeSpec t)
				: base (t)
			{
			}

			public override Expression DoResolveLValue (ResolveContext rc, Expression right_side)
			{
				rc.Report.Error (206, right_side.Location,
					"A property, indexer or dynamic member access may not be passed as `ref' or `out' parameter");

				return null;
			}
		}

		public static readonly EmptyExpression LValueMemberAccess = new EmptyExpression (InternalType.FakeInternalType);
		public static readonly EmptyExpression LValueMemberOutAccess = new EmptyExpression (InternalType.FakeInternalType);
		public static readonly EmptyExpression UnaryAddress = new EmptyExpression (InternalType.FakeInternalType);
		public static readonly EmptyExpression EventAddition = new EmptyExpression (InternalType.FakeInternalType);
		public static readonly EmptyExpression EventSubtraction = new EmptyExpression (InternalType.FakeInternalType);
		public static readonly EmptyExpression MissingValue = new EmptyExpression (InternalType.FakeInternalType);
		public static readonly Expression Null = new EmptyExpression (InternalType.FakeInternalType);
		public static readonly EmptyExpression OutAccess = new OutAccessExpression (InternalType.FakeInternalType);

		public EmptyExpression (TypeSpec t)
		{
			type = t;
			eclass = ExprClass.Value;
			loc = Location.Null;
		}

		public override bool ContainsEmitWithAwait ()
		{
			return false;
		}

		public override Expression CreateExpressionTree (ResolveContext ec)
		{
			throw new NotSupportedException ("ET");
		}
		
		protected override Expression DoResolve (ResolveContext ec)
		{
			return this;
		}

		public override void Emit (EmitContext ec)
		{
			// nothing, as we only exist to not do anything.
		}

		public override void EmitSideEffect (EmitContext ec)
		{
		}

		public override object Accept (StructuralVisitor visitor)
		{
			return visitor.Visit (this);
		}
	}
	
	sealed class EmptyAwaitExpression : EmptyExpression
	{
		public EmptyAwaitExpression (TypeSpec type)
			: base (type)
		{
		}
		
		public override bool ContainsEmitWithAwait ()
		{
			return true;
		}
	}
	
	//
	// Empty statement expression
	//
	public sealed class EmptyExpressionStatement : ExpressionStatement
	{
		public static readonly EmptyExpressionStatement Instance = new EmptyExpressionStatement ();

		private EmptyExpressionStatement ()
		{
			loc = Location.Null;
		}

		public override bool ContainsEmitWithAwait ()
		{
			return false;
		}

		public override Expression CreateExpressionTree (ResolveContext ec)
		{
			return null;
		}

		public override void EmitStatement (EmitContext ec)
		{
			// Do nothing
		}

		protected override Expression DoResolve (ResolveContext ec)
		{
			eclass = ExprClass.Value;
			type = ec.BuiltinTypes.Object;
			return this;
		}

		public override void Emit (EmitContext ec)
		{
			// Do nothing
		}
		
		public override object Accept (StructuralVisitor visitor)
		{
			return visitor.Visit (this);
		}
	}

	public class ErrorExpression : EmptyExpression
	{
		public static readonly ErrorExpression Instance = new ErrorExpression ();
		public readonly int ErrorCode;
		public readonly string Error;
		
		private ErrorExpression ()
			: base (InternalType.ErrorType)
		{
		}
		
		ErrorExpression (int errorCode, Location location, string error)
			: base (InternalType.ErrorType)
		{
			this.ErrorCode = errorCode;
			base.loc = location;
			this.Error = error;
		}
		
		public static ErrorExpression Create (int errorCode, Location location, string error)
		{
			return new ErrorExpression (errorCode, location, error);
		}

		public override Expression CreateExpressionTree (ResolveContext ec)
		{
			return this;
		}

		public override Expression DoResolveLValue (ResolveContext rc, Expression right_side)
		{
			return this;
		}

		public override void Error_ValueAssignment (ResolveContext rc, Expression rhs)
		{
		}

		public override void Error_UnexpectedKind (ResolveContext ec, ResolveFlags flags, Location loc)
		{
		}

		public override void Error_ValueCannotBeConverted (ResolveContext ec, TypeSpec target, bool expl)
		{
		}

		public override void Error_OperatorCannotBeApplied (ResolveContext rc, Location loc, string oper, TypeSpec t)
		{
		}
		
		public override object Accept (StructuralVisitor visitor)
		{
			return visitor.Visit (this);
		}
	}

	public class UserCast : Expression {
		MethodSpec method;
		Expression source;
		
		public UserCast (MethodSpec method, Expression source, Location l)
		{
			this.method = method;
			this.source = source;
			type = method.ReturnType;
			loc = l;
		}

		public Expression Source {
			get {
				return source;
			}
		}

		public override bool ContainsEmitWithAwait ()
		{
			return source.ContainsEmitWithAwait ();
		}

		public override Expression CreateExpressionTree (ResolveContext ec)
		{
			Arguments args = new Arguments (3);
			args.Add (new Argument (source.CreateExpressionTree (ec)));
			args.Add (new Argument (new TypeOf (type, loc)));
			args.Add (new Argument (new TypeOfMethod (method, loc)));
			return CreateExpressionFactoryCall (ec, "Convert", args);
		}
			
		protected override Expression DoResolve (ResolveContext ec)
		{
			ObsoleteAttribute oa = method.GetAttributeObsolete ();
			if (oa != null)
				AttributeTester.Report_ObsoleteMessage (oa, GetSignatureForError (), loc, ec.Report);

			eclass = ExprClass.Value;
			return this;
		}

		public override void Emit (EmitContext ec)
		{
			source.Emit (ec);
			ec.Emit (OpCodes.Call, method);
		}

		public override string GetSignatureForError ()
		{
			return TypeManager.CSharpSignature (method);
		}

		public override SLE.Expression MakeExpression (BuilderContext ctx)
		{
#if STATIC
			return base.MakeExpression (ctx);
#else
			return SLE.Expression.Convert (source.MakeExpression (ctx), type.GetMetaInfo (), (MethodInfo) method.GetMetaInfo ());
#endif
		}
	}

	//
	// Holds additional type specifiers like ?, *, []
	//
	public class ComposedTypeSpecifier
	{
		public static readonly ComposedTypeSpecifier SingleDimension = new ComposedTypeSpecifier (1, Location.Null);

		public readonly int Dimension;
		public readonly Location Location;

		public ComposedTypeSpecifier (int specifier, Location loc)
		{
			this.Dimension = specifier;
			this.Location = loc;
		}

		#region Properties
		public bool IsNullable {
			get {
				return Dimension == -1;
			}
		}

		public bool IsPointer {
			get {
				return Dimension == -2;
			}
		}

		public ComposedTypeSpecifier Next { get; set; }

		#endregion

		public static ComposedTypeSpecifier CreateArrayDimension (int dimension, Location loc)
		{
			return new ComposedTypeSpecifier (dimension, loc);
		}

		public static ComposedTypeSpecifier CreateNullable (Location loc)
		{
			return new ComposedTypeSpecifier (-1, loc);
		}

		public static ComposedTypeSpecifier CreatePointer (Location loc)
		{
			return new ComposedTypeSpecifier (-2, loc);
		}

		public string GetSignatureForError ()
		{
			string s =
				IsPointer ? "*" :
				IsNullable ? "?" :
				ArrayContainer.GetPostfixSignature (Dimension);

			return Next != null ? s + Next.GetSignatureForError () : s;
		}
	}

	// <summary>
	//   This class is used to "construct" the type during a typecast
	//   operation.  Since the Type.GetType class in .NET can parse
	//   the type specification, we just use this to construct the type
	//   one bit at a time.
	// </summary>
	public class ComposedCast : TypeExpr {
		FullNamedExpression left;
		ComposedTypeSpecifier spec;

		public FullNamedExpression Left {
			get { return this.left; }
		}

		public ComposedTypeSpecifier Spec {
			get {
				return this.spec;
			}
		}

		public ComposedCast (FullNamedExpression left, ComposedTypeSpecifier spec)
		{
			if (spec == null)
				throw new ArgumentNullException ("spec");

			this.left = left;
			this.spec = spec;
			this.loc = spec.Location;
		}

		public override TypeSpec ResolveAsType (IMemberContext ec)
		{
			type = left.ResolveAsType (ec);
			if (type == null)
				return null;

			eclass = ExprClass.Type;

			var single_spec = spec;

			if (single_spec.IsNullable) {
				type = new Nullable.NullableType (type, loc).ResolveAsType (ec);
				if (type == null)
					return null;

				single_spec = single_spec.Next;
			} else if (single_spec.IsPointer) {
				if (!TypeManager.VerifyUnmanaged (ec.Module, type, loc))
					return null;

				if (!ec.IsUnsafe) {
					UnsafeError (ec.Module.Compiler.Report, loc);
				}

				do {
					type = PointerContainer.MakeType (ec.Module, type);
					single_spec = single_spec.Next;
				} while (single_spec != null && single_spec.IsPointer);
			}

			if (single_spec != null && single_spec.Dimension > 0) {
				if (type.IsSpecialRuntimeType) {
					ec.Module.Compiler.Report.Error (611, loc, "Array elements cannot be of type `{0}'", type.GetSignatureForError ());
				} else if (type.IsStatic) {
					ec.Module.Compiler.Report.SymbolRelatedToPreviousError (type);
					ec.Module.Compiler.Report.Error (719, loc, "Array elements cannot be of static type `{0}'",
						type.GetSignatureForError ());
				} else {
					MakeArray (ec.Module, single_spec);
				}
			}

			return type;
		}

		void MakeArray (ModuleContainer module, ComposedTypeSpecifier spec)
		{
			if (spec.Next != null)
				MakeArray (module, spec.Next);

			type = ArrayContainer.MakeType (module, type, spec.Dimension);
		}

		public override string GetSignatureForError ()
		{
			return left.GetSignatureForError () + spec.GetSignatureForError ();
		}

		public override object Accept (StructuralVisitor visitor)
		{
			return visitor.Visit (this);
		}
	}

	class FixedBufferPtr : Expression
	{
		readonly Expression array;

		public FixedBufferPtr (Expression array, TypeSpec array_type, Location l)
		{
			this.type = array_type;
			this.array = array;
			this.loc = l;
		}

		public override bool ContainsEmitWithAwait ()
		{
			throw new NotImplementedException ();
		}

		public override Expression CreateExpressionTree (ResolveContext ec)
		{
			Error_PointerInsideExpressionTree (ec);
			return null;
		}

		public override void Emit(EmitContext ec)
		{
			array.Emit (ec);
		}

		protected override Expression DoResolve (ResolveContext ec)
		{
			type = PointerContainer.MakeType (ec.Module, type);
			eclass = ExprClass.Value;
			return this;
		}
	}


	//
	// This class is used to represent the address of an array, used
	// only by the Fixed statement, this generates "&a [0]" construct
	// for fixed (char *pa = a)
	//
	class ArrayPtr : FixedBufferPtr
	{
		public ArrayPtr (Expression array, TypeSpec array_type, Location l):
			base (array, array_type, l)
		{
		}

		public override void Emit (EmitContext ec)
		{
			base.Emit (ec);
			
			ec.EmitInt (0);
			ec.Emit (OpCodes.Ldelema, ((PointerContainer) type).Element);
		}
	}

	//
	// Encapsulates a conversion rules required for array indexes
	//
	public class ArrayIndexCast : TypeCast
	{
		public ArrayIndexCast (Expression expr, TypeSpec returnType)
			: base (expr, returnType)
		{
			if (expr.Type == returnType) // int -> int
				throw new ArgumentException ("unnecessary array index conversion");
		}

		public override Expression CreateExpressionTree (ResolveContext ec)
		{
			using (ec.Set (ResolveContext.Options.CheckedScope)) {
				return base.CreateExpressionTree (ec);
			}
		}

		public override void Emit (EmitContext ec)
		{
			child.Emit (ec);

			switch (child.Type.BuiltinType) {
			case BuiltinTypeSpec.Type.UInt:
				ec.Emit (OpCodes.Conv_U);
				break;
			case BuiltinTypeSpec.Type.Long:
				ec.Emit (OpCodes.Conv_Ovf_I);
				break;
			case BuiltinTypeSpec.Type.ULong:
				ec.Emit (OpCodes.Conv_Ovf_I_Un);
				break;
			default:
				throw new InternalErrorException ("Cannot emit cast to unknown array element type", type);
			}
		}
	}

	//
	// Implements the `stackalloc' keyword
	//
	public class StackAlloc : Expression {
		TypeSpec otype;
		Expression t;
		Expression count;

		public StackAlloc (Expression type, Expression count, Location l)
		{
			t = type;
			this.count = count;
			loc = l;
		}

		public Expression TypeExpression {
			get {
				return this.t;
			}
		}

		public Expression CountExpression {
			get {
				return this.count;
			}
		}

		public override bool ContainsEmitWithAwait ()
		{
			return false;
		}

		public override Expression CreateExpressionTree (ResolveContext ec)
		{
			throw new NotSupportedException ("ET");
		}

		protected override Expression DoResolve (ResolveContext ec)
		{
			count = count.Resolve (ec);
			if (count == null)
				return null;
			
			if (count.Type.BuiltinType != BuiltinTypeSpec.Type.UInt){
				count = Convert.ImplicitConversionRequired (ec, count, ec.BuiltinTypes.Int, loc);
				if (count == null)
					return null;
			}

			Constant c = count as Constant;
			if (c != null && c.IsNegative) {
				ec.Report.Error (247, loc, "Cannot use a negative size with stackalloc");
			}

			if (ec.HasAny (ResolveContext.Options.CatchScope | ResolveContext.Options.FinallyScope)) {
				ec.Report.Error (255, loc, "Cannot use stackalloc in finally or catch");
			}

			otype = t.ResolveAsType (ec);
			if (otype == null)
				return null;

			if (!TypeManager.VerifyUnmanaged (ec.Module, otype, loc))
				return null;

			type = PointerContainer.MakeType (ec.Module, otype);
			eclass = ExprClass.Value;

			return this;
		}

		public override void Emit (EmitContext ec)
		{
			int size = BuiltinTypeSpec.GetSize (otype);

			count.Emit (ec);

			if (size == 0)
				ec.Emit (OpCodes.Sizeof, otype);
			else
				ec.EmitInt (size);

			ec.Emit (OpCodes.Mul_Ovf_Un);
			ec.Emit (OpCodes.Localloc);
		}

		protected override void CloneTo (CloneContext clonectx, Expression t)
		{
			StackAlloc target = (StackAlloc) t;
			target.count = count.Clone (clonectx);
			target.t = t.Clone (clonectx);
		}
		
		public override object Accept (StructuralVisitor visitor)
		{
			return visitor.Visit (this);
		}
	}

	//
	// An object initializer expression
	//
	public class ElementInitializer : Assign
	{
		public readonly string Name;

		public ElementInitializer (string name, Expression initializer, Location loc)
			: base (null, initializer, loc)
		{
			this.Name = name;
		}
		
		protected override void CloneTo (CloneContext clonectx, Expression t)
		{
			ElementInitializer target = (ElementInitializer) t;
			target.source = source.Clone (clonectx);
		}

		public override Expression CreateExpressionTree (ResolveContext ec)
		{
			Arguments args = new Arguments (2);
			FieldExpr fe = target as FieldExpr;
			if (fe != null)
				args.Add (new Argument (fe.CreateTypeOfExpression ()));
			else
				args.Add (new Argument (((PropertyExpr) target).CreateSetterTypeOfExpression (ec)));

			string mname;
			Expression arg_expr;
			var cinit = source as CollectionOrObjectInitializers;
			if (cinit == null) {
				mname = "Bind";
				arg_expr = source.CreateExpressionTree (ec);
			} else {
				mname = cinit.IsEmpty || cinit.Initializers[0] is ElementInitializer ? "MemberBind" : "ListBind";
				arg_expr = cinit.CreateExpressionTree (ec, !cinit.IsEmpty);
			}

			args.Add (new Argument (arg_expr));
			return CreateExpressionFactoryCall (ec, mname, args);
		}

		protected override Expression DoResolve (ResolveContext ec)
		{
			if (source == null)
				return EmptyExpressionStatement.Instance;

			var t = ec.CurrentInitializerVariable.Type;
			if (t.BuiltinType == BuiltinTypeSpec.Type.Dynamic) {
				Arguments args = new Arguments (1);
				args.Add (new Argument (ec.CurrentInitializerVariable));
				target = new DynamicMemberBinder (Name, args, loc);
			} else {

				var member = MemberLookup (ec, false, t, Name, 0, MemberLookupRestrictions.ExactArity, loc);
				if (member == null) {
					member = Expression.MemberLookup (ec, true, t, Name, 0, MemberLookupRestrictions.ExactArity, loc);

					if (member != null) {
						// TODO: ec.Report.SymbolRelatedToPreviousError (member);
						ErrorIsInaccesible (ec, member.GetSignatureForError (), loc);
						return null;
					}
				}

				if (member == null) {
					Error_TypeDoesNotContainDefinition (ec, loc, t, Name);
					return null;
				}

				if (!(member is PropertyExpr || member is FieldExpr)) {
					ec.Report.Error (1913, loc,
						"Member `{0}' cannot be initialized. An object initializer may only be used for fields, or properties",
						member.GetSignatureForError ());

					return null;
				}

				var me = member as MemberExpr;
				if (me.IsStatic) {
					ec.Report.Error (1914, loc,
						"Static field or property `{0}' cannot be assigned in an object initializer",
						me.GetSignatureForError ());
				}

				target = me;
				me.InstanceExpression = ec.CurrentInitializerVariable;
			}

			if (source is CollectionOrObjectInitializers) {
				Expression previous = ec.CurrentInitializerVariable;
				ec.CurrentInitializerVariable = target;
				source = source.Resolve (ec);
				ec.CurrentInitializerVariable = previous;
				if (source == null)
					return null;
					
				eclass = source.eclass;
				type = source.Type;
				return this;
			}

			return base.DoResolve (ec);
		}
	
		public override void EmitStatement (EmitContext ec)
		{
			if (source is CollectionOrObjectInitializers)
				source.Emit (ec);
			else
				base.EmitStatement (ec);
		}
	}
	
	//
	// A collection initializer expression
	//
	class CollectionElementInitializer : Invocation
	{
		public class ElementInitializerArgument : Argument
		{
			public ElementInitializerArgument (Expression e)
				: base (e)
			{
			}
		}

		sealed class AddMemberAccess : MemberAccess
		{
			public AddMemberAccess (Expression expr, Location loc)
				: base (expr, "Add", loc)
			{
			}

			protected override void Error_TypeDoesNotContainDefinition (ResolveContext ec, TypeSpec type, string name)
			{
				if (TypeManager.HasElementType (type))
					return;

				base.Error_TypeDoesNotContainDefinition (ec, type, name);
			}
		}

		public CollectionElementInitializer (Expression argument)
			: base (null, new Arguments (1))
		{
			base.arguments.Add (new ElementInitializerArgument (argument));
			this.loc = argument.Location;
		}

		public CollectionElementInitializer (List<Expression> arguments, Location loc)
			: base (null, new Arguments (arguments.Count))
		{
			foreach (Expression e in arguments)
				base.arguments.Add (new ElementInitializerArgument (e));

			this.loc = loc;
		}

		public override Expression CreateExpressionTree (ResolveContext ec)
		{
			Arguments args = new Arguments (2);
			args.Add (new Argument (mg.CreateExpressionTree (ec)));

			var expr_initializers = new ArrayInitializer (arguments.Count, loc);
			foreach (Argument a in arguments)
				expr_initializers.Add (a.CreateExpressionTree (ec));

			args.Add (new Argument (new ArrayCreation (
				CreateExpressionTypeExpression (ec, loc), expr_initializers, loc)));
			return CreateExpressionFactoryCall (ec, "ElementInit", args);
		}

		protected override void CloneTo (CloneContext clonectx, Expression t)
		{
			CollectionElementInitializer target = (CollectionElementInitializer) t;
			if (arguments != null)
				target.arguments = arguments.Clone (clonectx);
		}

		protected override Expression DoResolve (ResolveContext ec)
		{
			base.expr = new AddMemberAccess (ec.CurrentInitializerVariable, loc);

			return base.DoResolve (ec);
		}
	}
	
	//
	// A block of object or collection initializers
	//
	public class CollectionOrObjectInitializers : ExpressionStatement
	{
		IList<Expression> initializers;
		bool is_collection_initialization;
		
		public static readonly CollectionOrObjectInitializers Empty = 
			new CollectionOrObjectInitializers (Array.AsReadOnly (new Expression [0]), Location.Null);

		public CollectionOrObjectInitializers (IList<Expression> initializers, Location loc)
		{
			this.initializers = initializers;
			this.loc = loc;
		}
		
		public bool IsEmpty {
			get {
				return initializers.Count == 0;
			}
		}

		public bool IsCollectionInitializer {
			get {
				return is_collection_initialization;
			}
		}

		public IList<Expression> Initializers {
			get {
				return initializers;
			}
		}

		protected override void CloneTo (CloneContext clonectx, Expression target)
		{
			CollectionOrObjectInitializers t = (CollectionOrObjectInitializers) target;

			t.initializers = new List<Expression> (initializers.Count);
			foreach (var e in initializers)
				t.initializers.Add (e.Clone (clonectx));
		}

		public override bool ContainsEmitWithAwait ()
		{
			foreach (var e in initializers) {
				if (e.ContainsEmitWithAwait ())
					return true;
			}

			return false;
		}

		public override Expression CreateExpressionTree (ResolveContext ec)
		{
			return CreateExpressionTree (ec, false);
		}

		public Expression CreateExpressionTree (ResolveContext ec, bool inferType)
		{
			var expr_initializers = new ArrayInitializer (initializers.Count, loc);
			foreach (Expression e in initializers) {
				Expression expr = e.CreateExpressionTree (ec);
				if (expr != null)
					expr_initializers.Add (expr);
			}

			if (inferType)
				return new ImplicitlyTypedArrayCreation (expr_initializers, loc);

			return new ArrayCreation (new TypeExpression (ec.Module.PredefinedTypes.MemberBinding.Resolve (), loc), expr_initializers, loc); 
		}
		
		protected override Expression DoResolve (ResolveContext ec)
		{
			List<string> element_names = null;
			for (int i = 0; i < initializers.Count; ++i) {
				Expression initializer = initializers [i];
				ElementInitializer element_initializer = initializer as ElementInitializer;

				if (i == 0) {
					if (element_initializer != null) {
						element_names = new List<string> (initializers.Count);
						element_names.Add (element_initializer.Name);
					} else if (initializer is CompletingExpression){
						initializer.Resolve (ec);
						throw new InternalErrorException ("This line should never be reached");
					} else {
						var t = ec.CurrentInitializerVariable.Type;
						// LAMESPEC: The collection must implement IEnumerable only, no dynamic support
						if (!t.ImplementsInterface (ec.BuiltinTypes.IEnumerable, false) && t.BuiltinType != BuiltinTypeSpec.Type.Dynamic) {
							ec.Report.Error (1922, loc, "A field or property `{0}' cannot be initialized with a collection " +
								"object initializer because type `{1}' does not implement `{2}' interface",
								ec.CurrentInitializerVariable.GetSignatureForError (),
								TypeManager.CSharpName (ec.CurrentInitializerVariable.Type),
								TypeManager.CSharpName (ec.BuiltinTypes.IEnumerable));
							return null;
						}
						is_collection_initialization = true;
					}
				} else {
					if (is_collection_initialization != (element_initializer == null)) {
						ec.Report.Error (747, initializer.Location, "Inconsistent `{0}' member declaration",
							is_collection_initialization ? "collection initializer" : "object initializer");
						continue;
					}

					if (!is_collection_initialization) {
						if (element_names.Contains (element_initializer.Name)) {
							ec.Report.Error (1912, element_initializer.Location,
								"An object initializer includes more than one member `{0}' initialization",
								element_initializer.Name);
						} else {
							element_names.Add (element_initializer.Name);
						}
					}
				}

				Expression e = initializer.Resolve (ec);
				if (e == EmptyExpressionStatement.Instance)
					initializers.RemoveAt (i--);
				else
					initializers [i] = e;
			}

			type = ec.CurrentInitializerVariable.Type;
			if (is_collection_initialization) {
				if (TypeManager.HasElementType (type)) {
					ec.Report.Error (1925, loc, "Cannot initialize object of type `{0}' with a collection initializer",
						TypeManager.CSharpName (type));
				}
			}

			eclass = ExprClass.Variable;
			return this;
		}

		public override void Emit (EmitContext ec)
		{
			EmitStatement (ec);
		}

		public override void EmitStatement (EmitContext ec)
		{
			foreach (ExpressionStatement e in initializers) {
				// TODO: need location region
				ec.Mark (e.Location);
				e.EmitStatement (ec);
			}
		}
	}
	
	//
	// New expression with element/object initializers
	//
	public class NewInitialize : New
	{
		//
		// This class serves as a proxy for variable initializer target instances.
		// A real variable is assigned later when we resolve left side of an
		// assignment
		//
		sealed class InitializerTargetExpression : Expression, IMemoryLocation
		{
			NewInitialize new_instance;

			public InitializerTargetExpression (NewInitialize newInstance)
			{
				this.type = newInstance.type;
				this.loc = newInstance.loc;
				this.eclass = newInstance.eclass;
				this.new_instance = newInstance;
			}

			public override bool ContainsEmitWithAwait ()
			{
				return false;
			}

			public override Expression CreateExpressionTree (ResolveContext ec)
			{
				// Should not be reached
				throw new NotSupportedException ("ET");
			}

			protected override Expression DoResolve (ResolveContext ec)
			{
				return this;
			}

			public override Expression DoResolveLValue (ResolveContext ec, Expression right_side)
			{
				return this;
			}

			public override void Emit (EmitContext ec)
			{
				Expression e = (Expression) new_instance.instance;
				e.Emit (ec);
			}

			public override Expression EmitToField (EmitContext ec)
			{
				return (Expression) new_instance.instance;
			}

			#region IMemoryLocation Members

			public void AddressOf (EmitContext ec, AddressOp mode)
			{
				new_instance.instance.AddressOf (ec, mode);
			}

			#endregion
		}

		CollectionOrObjectInitializers initializers;
		IMemoryLocation instance;

		public NewInitialize (FullNamedExpression requested_type, Arguments arguments, CollectionOrObjectInitializers initializers, Location l)
			: base (requested_type, arguments, l)
		{
			this.initializers = initializers;
		}

		public CollectionOrObjectInitializers Initializers {
			get {
				return initializers;
			}
		}

		protected override void CloneTo (CloneContext clonectx, Expression t)
		{
			base.CloneTo (clonectx, t);

			NewInitialize target = (NewInitialize) t;
			target.initializers = (CollectionOrObjectInitializers) initializers.Clone (clonectx);
		}

		public override bool ContainsEmitWithAwait ()
		{
			return base.ContainsEmitWithAwait () || initializers.ContainsEmitWithAwait ();
		}

		public override Expression CreateExpressionTree (ResolveContext ec)
		{
			Arguments args = new Arguments (2);
			args.Add (new Argument (base.CreateExpressionTree (ec)));
			if (!initializers.IsEmpty)
				args.Add (new Argument (initializers.CreateExpressionTree (ec, initializers.IsCollectionInitializer)));

			return CreateExpressionFactoryCall (ec,
				initializers.IsCollectionInitializer ? "ListInit" : "MemberInit",
				args);
		}

		protected override Expression DoResolve (ResolveContext ec)
		{
			Expression e = base.DoResolve (ec);
			if (type == null)
				return null;

			Expression previous = ec.CurrentInitializerVariable;
			ec.CurrentInitializerVariable = new InitializerTargetExpression (this);
			initializers.Resolve (ec);
			ec.CurrentInitializerVariable = previous;
			return e;
		}

		public override bool Emit (EmitContext ec, IMemoryLocation target)
		{
			bool left_on_stack = base.Emit (ec, target);

			if (initializers.IsEmpty)
				return left_on_stack;

			LocalTemporary temp = null;

			instance = target as LocalTemporary;

			if (instance == null) {
				if (!left_on_stack) {
					VariableReference vr = target as VariableReference;

					// FIXME: This still does not work correctly for pre-set variables
					if (vr != null && vr.IsRef)
						target.AddressOf (ec, AddressOp.Load);

					((Expression) target).Emit (ec);
					left_on_stack = true;
				}

				if (ec.HasSet (BuilderContext.Options.AsyncBody) && initializers.ContainsEmitWithAwait ()) {
					instance = new EmptyAwaitExpression (Type).EmitToField (ec) as IMemoryLocation;
				} else {
					temp = new LocalTemporary (type);
					instance = temp;
				}
			}

			if (left_on_stack && temp != null)
				temp.Store (ec);

			initializers.Emit (ec);

			if (left_on_stack) {
				if (temp != null) {
					temp.Emit (ec);
					temp.Release (ec);
				} else {
					((Expression) instance).Emit (ec);
				}
			}

			return left_on_stack;
		}

		protected override IMemoryLocation EmitAddressOf (EmitContext ec, AddressOp Mode)
		{
			instance = base.EmitAddressOf (ec, Mode);

			if (!initializers.IsEmpty)
				initializers.Emit (ec);

			return instance;
		}

		public override object Accept (StructuralVisitor visitor)
		{
			return visitor.Visit (this);
		}
	}

	public class NewAnonymousType : New
	{
		static readonly AnonymousTypeParameter[] EmptyParameters = new AnonymousTypeParameter[0];

		List<AnonymousTypeParameter> parameters;
		readonly TypeContainer parent;
		AnonymousTypeClass anonymous_type;

		public NewAnonymousType (List<AnonymousTypeParameter> parameters, TypeContainer parent, Location loc)
			 : base (null, null, loc)
		{
			this.parameters = parameters;
			this.parent = parent;
		}

		public List<AnonymousTypeParameter> Parameters {
			get {
				return this.parameters;
			}
		}

		protected override void CloneTo (CloneContext clonectx, Expression target)
		{
			if (parameters == null)
				return;

			NewAnonymousType t = (NewAnonymousType) target;
			t.parameters = new List<AnonymousTypeParameter> (parameters.Count);
			foreach (AnonymousTypeParameter atp in parameters)
				t.parameters.Add ((AnonymousTypeParameter) atp.Clone (clonectx));
		}

		AnonymousTypeClass CreateAnonymousType (ResolveContext ec, IList<AnonymousTypeParameter> parameters)
		{
			AnonymousTypeClass type = parent.Module.GetAnonymousType (parameters);
			if (type != null)
				return type;

			type = AnonymousTypeClass.Create (parent, parameters, loc);
			if (type == null)
				return null;

			int errors = ec.Report.Errors;
			type.CreateContainer ();
			type.DefineContainer ();
			type.Define ();
			if ((ec.Report.Errors - errors) == 0) {
				parent.Module.AddAnonymousType (type);
			}

			return type;
		}

		public override Expression CreateExpressionTree (ResolveContext ec)
		{
			if (parameters == null)
				return base.CreateExpressionTree (ec);

			var init = new ArrayInitializer (parameters.Count, loc);
			foreach (var m in anonymous_type.Members) {
				var p = m as Property;
				if (p != null)
					init.Add (new TypeOfMethod (MemberCache.GetMember (type, p.Get.Spec), loc));
			}

			var ctor_args = new ArrayInitializer (arguments.Count, loc);
			foreach (Argument a in arguments)
				ctor_args.Add (a.CreateExpressionTree (ec));

			Arguments args = new Arguments (3);
			args.Add (new Argument (new TypeOfMethod (method, loc)));
			args.Add (new Argument (new ArrayCreation (CreateExpressionTypeExpression (ec, loc), ctor_args, loc)));
			args.Add (new Argument (new ImplicitlyTypedArrayCreation (init, loc)));

			return CreateExpressionFactoryCall (ec, "New", args);
		}

		protected override Expression DoResolve (ResolveContext ec)
		{
			if (ec.HasSet (ResolveContext.Options.ConstantScope)) {
				ec.Report.Error (836, loc, "Anonymous types cannot be used in this expression");
				return null;
			}

			if (parameters == null) {
				anonymous_type = CreateAnonymousType (ec, EmptyParameters);
				RequestedType = new TypeExpression (anonymous_type.Definition, loc);
				return base.DoResolve (ec);
			}

			bool error = false;
			arguments = new Arguments (parameters.Count);
			var t_args = new TypeSpec [parameters.Count];
			for (int i = 0; i < parameters.Count; ++i) {
				Expression e = parameters [i].Resolve (ec);
				if (e == null) {
					error = true;
					continue;
				}

				arguments.Add (new Argument (e));
				t_args [i] = e.Type;
			}

			if (error)
				return null;

			anonymous_type = CreateAnonymousType (ec, parameters);
			if (anonymous_type == null)
				return null;

			type = anonymous_type.Definition.MakeGenericType (ec.Module, t_args);
			method = (MethodSpec) MemberCache.FindMember (type, MemberFilter.Constructor (null), BindingRestriction.DeclaredOnly);
			eclass = ExprClass.Value;
			return this;
		}

		public override void EmitStatement (EmitContext ec)
		{
			base.EmitStatement (ec);
		}
		
		public override object Accept (StructuralVisitor visitor)
		{
			return visitor.Visit (this);
		}
	}

	public class AnonymousTypeParameter : ShimExpression
	{
		public readonly string Name;

		public AnonymousTypeParameter (Expression initializer, string name, Location loc)
			: base (initializer)
		{
			this.Name = name;
			this.loc = loc;
		}
		
		public AnonymousTypeParameter (Parameter parameter)
			: base (new SimpleName (parameter.Name, parameter.Location))
		{
			this.Name = parameter.Name;
			this.loc = parameter.Location;
		}		

		public override bool Equals (object o)
		{
			AnonymousTypeParameter other = o as AnonymousTypeParameter;
			return other != null && Name == other.Name;
		}

		public override int GetHashCode ()
		{
			return Name.GetHashCode ();
		}

		protected override Expression DoResolve (ResolveContext ec)
		{
			Expression e = expr.Resolve (ec);
			if (e == null)
				return null;

			if (e.eclass == ExprClass.MethodGroup) {
				Error_InvalidInitializer (ec, e.ExprClassName);
				return null;
			}

			type = e.Type;
			if (type.Kind == MemberKind.Void || type == InternalType.NullLiteral || type == InternalType.AnonymousMethod || type.IsPointer) {
				Error_InvalidInitializer (ec, type.GetSignatureForError ());
				return null;
			}

			return e;
		}

		protected virtual void Error_InvalidInitializer (ResolveContext ec, string initializer)
		{
			ec.Report.Error (828, loc, "An anonymous type property `{0}' cannot be initialized with `{1}'",
				Name, initializer);
		}
	}
}
