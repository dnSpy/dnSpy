//
// cfold.cs: Constant Folding
//
// Author:
//   Miguel de Icaza (miguel@ximian.com)
//   Marek Safar (marek.safar@seznam.cz)
//
// Copyright 2002, 2003 Ximian, Inc.
// Copyright 2003-2008, Novell, Inc.
// 
using System;

namespace Mono.CSharp {

	public class ConstantFold {

		static TypeSpec[] binary_promotions;

		public static TypeSpec[] BinaryPromotionsTypes {
			get {
				if (binary_promotions == null) {
					 binary_promotions = new TypeSpec[] { 
						TypeManager.decimal_type, TypeManager.double_type, TypeManager.float_type,
						TypeManager.uint64_type, TypeManager.int64_type, TypeManager.uint32_type };
				}

				return binary_promotions;
			}
		}

		public static void Reset ()
		{
			binary_promotions = null;
		}

		//
		// Performs the numeric promotions on the left and right expresions
		// and deposits the results on `lc' and `rc'.
		//
		// On success, the types of `lc' and `rc' on output will always match,
		// and the pair will be one of:
		//
		// TODO: BinaryFold should be called as an optimization step only,
		// error checking here is weak
		//		
		static bool DoBinaryNumericPromotions (ResolveContext rc, ref Constant left, ref Constant right)
		{
			TypeSpec ltype = left.Type;
			TypeSpec rtype = right.Type;

			foreach (TypeSpec t in BinaryPromotionsTypes) {
				if (t == ltype)
					return t == rtype || ConvertPromotion (rc, ref right, ref left, t);

				if (t == rtype)
					return t == ltype || ConvertPromotion (rc, ref left, ref right, t);
			}

			left = left.ConvertImplicitly (rc, TypeManager.int32_type);
			right = right.ConvertImplicitly (rc, TypeManager.int32_type);
			return left != null && right != null;
		}

		static bool ConvertPromotion (ResolveContext rc, ref Constant prim, ref Constant second, TypeSpec type)
		{
			Constant c = prim.ConvertImplicitly (rc, type);
			if (c != null) {
				prim = c;
				return true;
			}

			if (type == TypeManager.uint32_type) {
				type = TypeManager.int64_type;
				prim = prim.ConvertImplicitly (rc, type);
				second = second.ConvertImplicitly (rc, type);
				return prim != null && second != null;
			}

			return false;
		}

		internal static void Error_CompileTimeOverflow (ResolveContext rc, Location loc)
		{
			rc.Report.Error (220, loc, "The operation overflows at compile time in checked mode");
		}
		
		/// <summary>
		///   Constant expression folder for binary operations.
		///
		///   Returns null if the expression can not be folded.
		/// </summary>
		static public Constant BinaryFold (ResolveContext ec, Binary.Operator oper,
						     Constant left, Constant right, Location loc)
		{
			Constant result = null;

			if (left is EmptyConstantCast)
				return BinaryFold (ec, oper, ((EmptyConstantCast)left).child, right, loc);

			if (left is SideEffectConstant) {
				result = BinaryFold (ec, oper, ((SideEffectConstant) left).value, right, loc);
				if (result == null)
					return null;
				return new SideEffectConstant (result, left, loc);
			}

			if (right is EmptyConstantCast)
				return BinaryFold (ec, oper, left, ((EmptyConstantCast)right).child, loc);

			if (right is SideEffectConstant) {
				result = BinaryFold (ec, oper, left, ((SideEffectConstant) right).value, loc);
				if (result == null)
					return null;
				return new SideEffectConstant (result, right, loc);
			}

			TypeSpec lt = left.Type;
			TypeSpec rt = right.Type;
			bool bool_res;

			if (lt == TypeManager.bool_type && lt == rt) {
				bool lv = (bool) left.GetValue ();
				bool rv = (bool) right.GetValue ();			
				switch (oper) {
				case Binary.Operator.BitwiseAnd:
				case Binary.Operator.LogicalAnd:
					return new BoolConstant (lv && rv, left.Location);
				case Binary.Operator.BitwiseOr:
				case Binary.Operator.LogicalOr:
					return new BoolConstant (lv || rv, left.Location);
				case Binary.Operator.ExclusiveOr:
					return new BoolConstant (lv ^ rv, left.Location);
				case Binary.Operator.Equality:
					return new BoolConstant (lv == rv, left.Location);
				case Binary.Operator.Inequality:
					return new BoolConstant (lv != rv, left.Location);
				}
				return null;
			}

			//
			// During an enum evaluation, none of the rules are valid
			// Not sure whether it is bug in csc or in documentation
			//
			if (ec.HasSet (ResolveContext.Options.EnumScope)){
				if (left is EnumConstant)
					left = ((EnumConstant) left).Child;
				
				if (right is EnumConstant)
					right = ((EnumConstant) right).Child;
			} else if (left is EnumConstant && rt == lt) {
				switch (oper){
					///
					/// E operator |(E x, E y);
					/// E operator &(E x, E y);
					/// E operator ^(E x, E y);
					/// 
					case Binary.Operator.BitwiseOr:
					case Binary.Operator.BitwiseAnd:
					case Binary.Operator.ExclusiveOr:
						result = BinaryFold (ec, oper, ((EnumConstant)left).Child, ((EnumConstant)right).Child, loc);
						if (result != null)
							result = result.Resolve (ec).TryReduce (ec, lt, loc);
						return result;

					///
					/// U operator -(E x, E y);
					/// 
					case Binary.Operator.Subtraction:
						result = BinaryFold (ec, oper, ((EnumConstant)left).Child, ((EnumConstant)right).Child, loc);
						if (result != null)
							result = result.Resolve (ec).TryReduce (ec, EnumSpec.GetUnderlyingType (lt), loc);
						return result;

					///
					/// bool operator ==(E x, E y);
					/// bool operator !=(E x, E y);
					/// bool operator <(E x, E y);
					/// bool operator >(E x, E y);
					/// bool operator <=(E x, E y);
					/// bool operator >=(E x, E y);
					/// 
					case Binary.Operator.Equality:				
					case Binary.Operator.Inequality:
					case Binary.Operator.LessThan:				
					case Binary.Operator.GreaterThan:
					case Binary.Operator.LessThanOrEqual:				
					case Binary.Operator.GreaterThanOrEqual:
						return BinaryFold(ec, oper, ((EnumConstant)left).Child, ((EnumConstant)right).Child, loc);
				}
				return null;
			}

			switch (oper){
			case Binary.Operator.BitwiseOr:
				//
				// bool? operator &(bool? x, bool? y);
				//
				if ((lt == TypeManager.bool_type && right is NullLiteral) ||
					(rt == TypeManager.bool_type && left is NullLiteral)) {
					var b = new Nullable.LiftedBinaryOperator (oper, left, right, loc).Resolve (ec);

					// false | null => null
					// null | false => null
					if ((right is NullLiteral && left.IsDefaultValue) || (left is NullLiteral && right.IsDefaultValue))
						return Nullable.LiftedNull.CreateFromExpression (ec, b);

					// true | null => true
					// null | true => true
					return ReducedExpression.Create (new BoolConstant (true, loc).Resolve (ec), b);					
				}

				if (!DoBinaryNumericPromotions (ec, ref left, ref right))
					return null;

				if (left is IntConstant){
					int res = ((IntConstant) left).Value | ((IntConstant) right).Value;
					
					return new IntConstant (res, left.Location);
				}
				if (left is UIntConstant){
					uint res = ((UIntConstant)left).Value | ((UIntConstant)right).Value;
					
					return new UIntConstant (res, left.Location);
				}
				if (left is LongConstant){
					long res = ((LongConstant)left).Value | ((LongConstant)right).Value;
					
					return new LongConstant (res, left.Location);
				}
				if (left is ULongConstant){
					ulong res = ((ULongConstant)left).Value |
						((ULongConstant)right).Value;
					
					return new ULongConstant (res, left.Location);
				}
				break;
				
			case Binary.Operator.BitwiseAnd:
				//
				// bool? operator &(bool? x, bool? y);
				//
				if ((lt == TypeManager.bool_type && right is NullLiteral) ||
					(rt == TypeManager.bool_type && left is NullLiteral)) {
					var b = new Nullable.LiftedBinaryOperator (oper, left, right, loc).Resolve (ec);

					// false & null => false
					// null & false => false
					if ((right is NullLiteral && left.IsDefaultValue) || (left is NullLiteral && right.IsDefaultValue))
						return ReducedExpression.Create (new BoolConstant (false, loc).Resolve (ec), b);

					// true & null => null
					// null & true => null
					return Nullable.LiftedNull.CreateFromExpression (ec, b);
				}

				if (!DoBinaryNumericPromotions (ec, ref left, ref right))
					return null;
				
				///
				/// int operator &(int x, int y);
				/// uint operator &(uint x, uint y);
				/// long operator &(long x, long y);
				/// ulong operator &(ulong x, ulong y);
				///
				if (left is IntConstant){
					int res = ((IntConstant) left).Value & ((IntConstant) right).Value;
					return new IntConstant (res, left.Location);
				}
				if (left is UIntConstant){
					uint res = ((UIntConstant)left).Value & ((UIntConstant)right).Value;
					return new UIntConstant (res, left.Location);
				}
				if (left is LongConstant){
					long res = ((LongConstant)left).Value & ((LongConstant)right).Value;
					return new LongConstant (res, left.Location);
				}
				if (left is ULongConstant){
					ulong res = ((ULongConstant)left).Value &
						((ULongConstant)right).Value;
					
					return new ULongConstant (res, left.Location);
				}
				break;

			case Binary.Operator.ExclusiveOr:
				if (!DoBinaryNumericPromotions (ec, ref left, ref right))
					return null;
				
				if (left is IntConstant){
					int res = ((IntConstant) left).Value ^ ((IntConstant) right).Value;
					return new IntConstant (res, left.Location);
				}
				if (left is UIntConstant){
					uint res = ((UIntConstant)left).Value ^ ((UIntConstant)right).Value;
					
					return  new UIntConstant (res, left.Location);
				}
				if (left is LongConstant){
					long res = ((LongConstant)left).Value ^ ((LongConstant)right).Value;
					
					return new LongConstant (res, left.Location);
				}
				if (left is ULongConstant){
					ulong res = ((ULongConstant)left).Value ^
						((ULongConstant)right).Value;
					
					return new ULongConstant (res, left.Location);
				}
				break;

			case Binary.Operator.Addition:
				if (lt == InternalType.Null)
					return right;

				if (rt == InternalType.Null)
					return left;

				//
				// If both sides are strings, then concatenate, if
				// one is a string, and the other is not, then defer
				// to runtime concatenation
				//
				if (lt == TypeManager.string_type || rt == TypeManager.string_type){
					if (lt == rt)
						return new StringConstant ((string)left.GetValue () + (string)right.GetValue (),
							left.Location);
					
					return null;
				}

				//
				// handle "E operator + (E x, U y)"
				// handle "E operator + (Y y, E x)"
				//
				EnumConstant lc = left as EnumConstant;
				EnumConstant rc = right as EnumConstant;
				if (lc != null || rc != null){
					if (lc == null) {
						lc = rc;
						lt = lc.Type;
						right = left;
					}

					// U has to be implicitly convetible to E.base
					right = right.ConvertImplicitly (ec, lc.Child.Type);
					if (right == null)
						return null;

					result = BinaryFold (ec, oper, lc.Child, right, loc);
					if (result == null)
						return null;

					result = result.Resolve (ec).TryReduce (ec, lt, loc);
					if (result == null)
						return null;

					return new EnumConstant (result, lt);
				}

				if (!DoBinaryNumericPromotions (ec, ref left, ref right))
					return null;

				try {
					if (left is DoubleConstant){
						double res;
						
						if (ec.ConstantCheckState)
							res = checked (((DoubleConstant) left).Value +
								       ((DoubleConstant) right).Value);
						else
							res = unchecked (((DoubleConstant) left).Value +
									 ((DoubleConstant) right).Value);
						
						return new DoubleConstant (res, left.Location);
					}
					if (left is FloatConstant){
						float res;
						
						if (ec.ConstantCheckState)
							res = checked (((FloatConstant) left).Value +
								       ((FloatConstant) right).Value);
						else
							res = unchecked (((FloatConstant) left).Value +
									 ((FloatConstant) right).Value);
						
						result = new FloatConstant (res, left.Location);
					} else if (left is ULongConstant){
						ulong res;
						
						if (ec.ConstantCheckState)
							res = checked (((ULongConstant) left).Value +
								       ((ULongConstant) right).Value);
						else
							res = unchecked (((ULongConstant) left).Value +
									 ((ULongConstant) right).Value);

						result = new ULongConstant (res, left.Location);
					} else if (left is LongConstant){
						long res;
						
						if (ec.ConstantCheckState)
							res = checked (((LongConstant) left).Value +
								       ((LongConstant) right).Value);
						else
							res = unchecked (((LongConstant) left).Value +
									 ((LongConstant) right).Value);
						
						result = new LongConstant (res, left.Location);
					} else if (left is UIntConstant){
						uint res;
						
						if (ec.ConstantCheckState)
							res = checked (((UIntConstant) left).Value +
								       ((UIntConstant) right).Value);
						else
							res = unchecked (((UIntConstant) left).Value +
									 ((UIntConstant) right).Value);
						
						result = new UIntConstant (res, left.Location);
					} else if (left is IntConstant){
						int res;

						if (ec.ConstantCheckState)
							res = checked (((IntConstant) left).Value +
								       ((IntConstant) right).Value);
						else
							res = unchecked (((IntConstant) left).Value +
									 ((IntConstant) right).Value);

						result = new IntConstant (res, left.Location);
					} else if (left is DecimalConstant) {
						decimal res;

						if (ec.ConstantCheckState)
							res = checked (((DecimalConstant) left).Value +
								((DecimalConstant) right).Value);
						else
							res = unchecked (((DecimalConstant) left).Value +
								((DecimalConstant) right).Value);

						result = new DecimalConstant (res, left.Location);
					}
				} catch (OverflowException){
					Error_CompileTimeOverflow (ec, loc);
				}

				return result;

			case Binary.Operator.Subtraction:
				//
				// handle "E operator - (E x, U y)"
				// handle "E operator - (Y y, E x)"
				//
				lc = left as EnumConstant;
				rc = right as EnumConstant;
				if (lc != null || rc != null){
					if (lc == null) {
						lc = rc;
						lt = lc.Type;
						right = left;
					}

					// U has to be implicitly convetible to E.base
					right = right.ConvertImplicitly (ec, lc.Child.Type);
					if (right == null)
						return null;

					result = BinaryFold (ec, oper, lc.Child, right, loc);
					if (result == null)
						return null;

					result = result.Resolve (ec).TryReduce (ec, lt, loc);
					if (result == null)
						return null;

					return new EnumConstant (result, lt);
				}

				if (left is NullLiteral && right is NullLiteral) {
					var lifted_int = new Nullable.NullableType (TypeManager.int32_type, loc).ResolveAsTypeTerminal (ec, false);
					return (Constant) new Nullable.LiftedBinaryOperator (oper, lifted_int, right, loc).Resolve (ec);
				}

				if (!DoBinaryNumericPromotions (ec, ref left, ref right))
					return null;

				try {
					if (left is DoubleConstant){
						double res;
						
						if (ec.ConstantCheckState)
							res = checked (((DoubleConstant) left).Value -
								       ((DoubleConstant) right).Value);
						else
							res = unchecked (((DoubleConstant) left).Value -
									 ((DoubleConstant) right).Value);
						
						result = new DoubleConstant (res, left.Location);
					} else if (left is FloatConstant){
						float res;
						
						if (ec.ConstantCheckState)
							res = checked (((FloatConstant) left).Value -
								       ((FloatConstant) right).Value);
						else
							res = unchecked (((FloatConstant) left).Value -
									 ((FloatConstant) right).Value);
						
						result = new FloatConstant (res, left.Location);
					} else if (left is ULongConstant){
						ulong res;
						
						if (ec.ConstantCheckState)
							res = checked (((ULongConstant) left).Value -
								       ((ULongConstant) right).Value);
						else
							res = unchecked (((ULongConstant) left).Value -
									 ((ULongConstant) right).Value);
						
						result = new ULongConstant (res, left.Location);
					} else if (left is LongConstant){
						long res;
						
						if (ec.ConstantCheckState)
							res = checked (((LongConstant) left).Value -
								       ((LongConstant) right).Value);
						else
							res = unchecked (((LongConstant) left).Value -
									 ((LongConstant) right).Value);
						
						result = new LongConstant (res, left.Location);
					} else if (left is UIntConstant){
						uint res;
						
						if (ec.ConstantCheckState)
							res = checked (((UIntConstant) left).Value -
								       ((UIntConstant) right).Value);
						else
							res = unchecked (((UIntConstant) left).Value -
									 ((UIntConstant) right).Value);
						
						result = new UIntConstant (res, left.Location);
					} else if (left is IntConstant){
						int res;

						if (ec.ConstantCheckState)
							res = checked (((IntConstant) left).Value -
								       ((IntConstant) right).Value);
						else
							res = unchecked (((IntConstant) left).Value -
									 ((IntConstant) right).Value);

						result = new IntConstant (res, left.Location);
					} else if (left is DecimalConstant) {
						decimal res;

						if (ec.ConstantCheckState)
							res = checked (((DecimalConstant) left).Value -
								((DecimalConstant) right).Value);
						else
							res = unchecked (((DecimalConstant) left).Value -
								((DecimalConstant) right).Value);

						return new DecimalConstant (res, left.Location);
					} else {
						throw new Exception ( "Unexepected subtraction input: " + left);
					}
				} catch (OverflowException){
					Error_CompileTimeOverflow (ec, loc);
				}

				return result;
				
			case Binary.Operator.Multiply:
				if (left is NullLiteral && right is NullLiteral) {
					var lifted_int = new Nullable.NullableType (TypeManager.int32_type, loc).ResolveAsTypeTerminal (ec, false);
					return (Constant) new Nullable.LiftedBinaryOperator (oper, lifted_int, right, loc).Resolve (ec);
				}

				if (!DoBinaryNumericPromotions (ec, ref left, ref right))
					return null;

				try {
					if (left is DoubleConstant){
						double res;
						
						if (ec.ConstantCheckState)
							res = checked (((DoubleConstant) left).Value *
								((DoubleConstant) right).Value);
						else
							res = unchecked (((DoubleConstant) left).Value *
								((DoubleConstant) right).Value);
						
						return new DoubleConstant (res, left.Location);
					} else if (left is FloatConstant){
						float res;
						
						if (ec.ConstantCheckState)
							res = checked (((FloatConstant) left).Value *
								((FloatConstant) right).Value);
						else
							res = unchecked (((FloatConstant) left).Value *
								((FloatConstant) right).Value);
						
						return new FloatConstant (res, left.Location);
					} else if (left is ULongConstant){
						ulong res;
						
						if (ec.ConstantCheckState)
							res = checked (((ULongConstant) left).Value *
								((ULongConstant) right).Value);
						else
							res = unchecked (((ULongConstant) left).Value *
								((ULongConstant) right).Value);
						
						return new ULongConstant (res, left.Location);
					} else if (left is LongConstant){
						long res;
						
						if (ec.ConstantCheckState)
							res = checked (((LongConstant) left).Value *
								((LongConstant) right).Value);
						else
							res = unchecked (((LongConstant) left).Value *
								((LongConstant) right).Value);
						
						return new LongConstant (res, left.Location);
					} else if (left is UIntConstant){
						uint res;
						
						if (ec.ConstantCheckState)
							res = checked (((UIntConstant) left).Value *
								((UIntConstant) right).Value);
						else
							res = unchecked (((UIntConstant) left).Value *
								((UIntConstant) right).Value);
						
						return new UIntConstant (res, left.Location);
					} else if (left is IntConstant){
						int res;

						if (ec.ConstantCheckState)
							res = checked (((IntConstant) left).Value *
								((IntConstant) right).Value);
						else
							res = unchecked (((IntConstant) left).Value *
								((IntConstant) right).Value);

						return new IntConstant (res, left.Location);
					} else if (left is DecimalConstant) {
						decimal res;

						if (ec.ConstantCheckState)
							res = checked (((DecimalConstant) left).Value *
								((DecimalConstant) right).Value);
						else
							res = unchecked (((DecimalConstant) left).Value *
								((DecimalConstant) right).Value);

						return new DecimalConstant (res, left.Location);
					} else {
						throw new Exception ( "Unexepected multiply input: " + left);
					}
				} catch (OverflowException){
					Error_CompileTimeOverflow (ec, loc);
				}
				break;

			case Binary.Operator.Division:
				if (left is NullLiteral && right is NullLiteral) {
					var lifted_int = new Nullable.NullableType (TypeManager.int32_type, loc).ResolveAsTypeTerminal (ec, false);
					return (Constant) new Nullable.LiftedBinaryOperator (oper, lifted_int, right, loc).Resolve (ec);
				}

				if (!DoBinaryNumericPromotions (ec, ref left, ref right))
					return null;

				try {
					if (left is DoubleConstant){
						double res;
						
						if (ec.ConstantCheckState)
							res = checked (((DoubleConstant) left).Value /
								((DoubleConstant) right).Value);
						else
							res = unchecked (((DoubleConstant) left).Value /
								((DoubleConstant) right).Value);
						
						return new DoubleConstant (res, left.Location);
					} else if (left is FloatConstant){
						float res;
						
						if (ec.ConstantCheckState)
							res = checked (((FloatConstant) left).Value /
								((FloatConstant) right).Value);
						else
							res = unchecked (((FloatConstant) left).Value /
								((FloatConstant) right).Value);
						
						return new FloatConstant (res, left.Location);
					} else if (left is ULongConstant){
						ulong res;
						
						if (ec.ConstantCheckState)
							res = checked (((ULongConstant) left).Value /
								((ULongConstant) right).Value);
						else
							res = unchecked (((ULongConstant) left).Value /
								((ULongConstant) right).Value);
						
						return new ULongConstant (res, left.Location);
					} else if (left is LongConstant){
						long res;
						
						if (ec.ConstantCheckState)
							res = checked (((LongConstant) left).Value /
								((LongConstant) right).Value);
						else
							res = unchecked (((LongConstant) left).Value /
								((LongConstant) right).Value);
						
						return new LongConstant (res, left.Location);
					} else if (left is UIntConstant){
						uint res;
						
						if (ec.ConstantCheckState)
							res = checked (((UIntConstant) left).Value /
								((UIntConstant) right).Value);
						else
							res = unchecked (((UIntConstant) left).Value /
								((UIntConstant) right).Value);
						
						return new UIntConstant (res, left.Location);
					} else if (left is IntConstant){
						int res;

						if (ec.ConstantCheckState)
							res = checked (((IntConstant) left).Value /
								((IntConstant) right).Value);
						else
							res = unchecked (((IntConstant) left).Value /
								((IntConstant) right).Value);

						return new IntConstant (res, left.Location);
					} else if (left is DecimalConstant) {
						decimal res;

						if (ec.ConstantCheckState)
							res = checked (((DecimalConstant) left).Value /
								((DecimalConstant) right).Value);
						else
							res = unchecked (((DecimalConstant) left).Value /
								((DecimalConstant) right).Value);

						return new DecimalConstant (res, left.Location);
					} else {
						throw new Exception ( "Unexepected division input: " + left);
					}
				} catch (OverflowException){
					Error_CompileTimeOverflow (ec, loc);

				} catch (DivideByZeroException) {
					ec.Report.Error (20, loc, "Division by constant zero");
				}
				
				break;
				
			case Binary.Operator.Modulus:
				if (left is NullLiteral && right is NullLiteral) {
					var lifted_int = new Nullable.NullableType (TypeManager.int32_type, loc).ResolveAsTypeTerminal (ec, false);
					return (Constant) new Nullable.LiftedBinaryOperator (oper, lifted_int, right, loc).Resolve (ec);
				}

				if (!DoBinaryNumericPromotions (ec, ref left, ref right))
					return null;

				try {
					if (left is DoubleConstant){
						double res;
						
						if (ec.ConstantCheckState)
							res = checked (((DoubleConstant) left).Value %
								       ((DoubleConstant) right).Value);
						else
							res = unchecked (((DoubleConstant) left).Value %
									 ((DoubleConstant) right).Value);
						
						return new DoubleConstant (res, left.Location);
					} else if (left is FloatConstant){
						float res;
						
						if (ec.ConstantCheckState)
							res = checked (((FloatConstant) left).Value %
								       ((FloatConstant) right).Value);
						else
							res = unchecked (((FloatConstant) left).Value %
									 ((FloatConstant) right).Value);
						
						return new FloatConstant (res, left.Location);
					} else if (left is ULongConstant){
						ulong res;
						
						if (ec.ConstantCheckState)
							res = checked (((ULongConstant) left).Value %
								       ((ULongConstant) right).Value);
						else
							res = unchecked (((ULongConstant) left).Value %
									 ((ULongConstant) right).Value);
						
						return new ULongConstant (res, left.Location);
					} else if (left is LongConstant){
						long res;
						
						if (ec.ConstantCheckState)
							res = checked (((LongConstant) left).Value %
								       ((LongConstant) right).Value);
						else
							res = unchecked (((LongConstant) left).Value %
									 ((LongConstant) right).Value);
						
						return new LongConstant (res, left.Location);
					} else if (left is UIntConstant){
						uint res;
						
						if (ec.ConstantCheckState)
							res = checked (((UIntConstant) left).Value %
								       ((UIntConstant) right).Value);
						else
							res = unchecked (((UIntConstant) left).Value %
									 ((UIntConstant) right).Value);
						
						return new UIntConstant (res, left.Location);
					} else if (left is IntConstant){
						int res;

						if (ec.ConstantCheckState)
							res = checked (((IntConstant) left).Value %
								       ((IntConstant) right).Value);
						else
							res = unchecked (((IntConstant) left).Value %
									 ((IntConstant) right).Value);

						return new IntConstant (res, left.Location);
					} else {
						throw new Exception ( "Unexepected modulus input: " + left);
					}
				} catch (DivideByZeroException){
					ec.Report.Error (20, loc, "Division by constant zero");
				} catch (OverflowException){
					Error_CompileTimeOverflow (ec, loc);
				}
				break;

				//
				// There is no overflow checking on left shift
				//
			case Binary.Operator.LeftShift:
				if (left is NullLiteral && right is NullLiteral) {
					var lifted_int = new Nullable.NullableType (TypeManager.int32_type, loc).ResolveAsTypeTerminal (ec, false);
					return (Constant) new Nullable.LiftedBinaryOperator (oper, lifted_int, right, loc).Resolve (ec);
				}

				IntConstant ic = right.ConvertImplicitly (ec, TypeManager.int32_type) as IntConstant;
				if (ic == null){
					Binary.Error_OperatorCannotBeApplied (ec, left, right, oper, loc);
					return null;
				}

				int lshift_val = ic.Value;
				if (left.Type == TypeManager.uint64_type)
					return new ULongConstant (((ULongConstant)left).Value << lshift_val, left.Location);
				if (left.Type == TypeManager.int64_type)
					return new LongConstant (((LongConstant)left).Value << lshift_val, left.Location);
				if (left.Type == TypeManager.uint32_type)
					return new UIntConstant (((UIntConstant)left).Value << lshift_val, left.Location);

				// null << value => null
				if (left is NullLiteral)
					return (Constant) new Nullable.LiftedBinaryOperator (oper, left, right, loc).Resolve (ec);

				left = left.ConvertImplicitly (ec, TypeManager.int32_type);
				if (left.Type == TypeManager.int32_type)
					return new IntConstant (((IntConstant)left).Value << lshift_val, left.Location);

				Binary.Error_OperatorCannotBeApplied (ec, left, right, oper, loc);
				break;

				//
				// There is no overflow checking on right shift
				//
			case Binary.Operator.RightShift:
				if (left is NullLiteral && right is NullLiteral) {
					var lifted_int = new Nullable.NullableType (TypeManager.int32_type, loc).ResolveAsTypeTerminal (ec, false);
					return (Constant) new Nullable.LiftedBinaryOperator (oper, lifted_int, right, loc).Resolve (ec);
				}

				IntConstant sic = right.ConvertImplicitly (ec, TypeManager.int32_type) as IntConstant;
				if (sic == null){
					Binary.Error_OperatorCannotBeApplied (ec, left, right, oper, loc); ;
					return null;
				}
				int rshift_val = sic.Value;
				if (left.Type == TypeManager.uint64_type)
					return new ULongConstant (((ULongConstant)left).Value >> rshift_val, left.Location);
				if (left.Type == TypeManager.int64_type)
					return new LongConstant (((LongConstant)left).Value >> rshift_val, left.Location);
				if (left.Type == TypeManager.uint32_type)
					return new UIntConstant (((UIntConstant)left).Value >> rshift_val, left.Location);

				// null >> value => null
				if (left is NullLiteral)
					return (Constant) new Nullable.LiftedBinaryOperator (oper, left, right, loc).Resolve (ec);

				left = left.ConvertImplicitly (ec, TypeManager.int32_type);
				if (left.Type == TypeManager.int32_type)
					return new IntConstant (((IntConstant)left).Value >> rshift_val, left.Location);

				Binary.Error_OperatorCannotBeApplied (ec, left, right, oper, loc);
				break;

			case Binary.Operator.Equality:
				if (TypeManager.IsReferenceType (lt) && TypeManager.IsReferenceType (rt) ||
					(left is Nullable.LiftedNull && right.IsNull) ||
					(right is Nullable.LiftedNull && left.IsNull)) {
					if (left.IsNull || right.IsNull) {
						return ReducedExpression.Create (
							new BoolConstant (left.IsNull == right.IsNull, left.Location).Resolve (ec),
							new Binary (oper, left, right, loc));
					}

					if (left is StringConstant && right is StringConstant)
						return new BoolConstant (
							((StringConstant) left).Value == ((StringConstant) right).Value, left.Location);

					return null;
				}

				if (!DoBinaryNumericPromotions (ec, ref left, ref right))
					return null;

				bool_res = false;
				if (left is DoubleConstant)
					bool_res = ((DoubleConstant) left).Value ==
						((DoubleConstant) right).Value;
				else if (left is FloatConstant)
					bool_res = ((FloatConstant) left).Value ==
						((FloatConstant) right).Value;
				else if (left is ULongConstant)
					bool_res = ((ULongConstant) left).Value ==
						((ULongConstant) right).Value;
				else if (left is LongConstant)
					bool_res = ((LongConstant) left).Value ==
						((LongConstant) right).Value;
				else if (left is UIntConstant)
					bool_res = ((UIntConstant) left).Value ==
						((UIntConstant) right).Value;
				else if (left is IntConstant)
					bool_res = ((IntConstant) left).Value ==
						((IntConstant) right).Value;
				else
					return null;

				return new BoolConstant (bool_res, left.Location);

			case Binary.Operator.Inequality:
				if (TypeManager.IsReferenceType (lt) && TypeManager.IsReferenceType (rt) ||
					(left is Nullable.LiftedNull && right.IsNull) ||
					(right is Nullable.LiftedNull && left.IsNull)) {
					if (left.IsNull || right.IsNull) {
						return ReducedExpression.Create (
							new BoolConstant (left.IsNull != right.IsNull, left.Location).Resolve (ec),
							new Binary (oper, left, right, loc));
					}

					if (left is StringConstant && right is StringConstant)
						return new BoolConstant (
							((StringConstant) left).Value != ((StringConstant) right).Value, left.Location);

					return null;
				}

				if (!DoBinaryNumericPromotions (ec, ref left, ref right))
					return null;

				bool_res = false;
				if (left is DoubleConstant)
					bool_res = ((DoubleConstant) left).Value !=
						((DoubleConstant) right).Value;
				else if (left is FloatConstant)
					bool_res = ((FloatConstant) left).Value !=
						((FloatConstant) right).Value;
				else if (left is ULongConstant)
					bool_res = ((ULongConstant) left).Value !=
						((ULongConstant) right).Value;
				else if (left is LongConstant)
					bool_res = ((LongConstant) left).Value !=
						((LongConstant) right).Value;
				else if (left is UIntConstant)
					bool_res = ((UIntConstant) left).Value !=
						((UIntConstant) right).Value;
				else if (left is IntConstant)
					bool_res = ((IntConstant) left).Value !=
						((IntConstant) right).Value;
				else
					return null;

				return new BoolConstant (bool_res, left.Location);

			case Binary.Operator.LessThan:
				if (right is NullLiteral) {
					if (left is NullLiteral) {
						var lifted_int = new Nullable.NullableType (TypeManager.int32_type, loc).ResolveAsTypeTerminal (ec, false);
						return (Constant) new Nullable.LiftedBinaryOperator (oper, lifted_int, right, loc).Resolve (ec);
					}

					if (left is Nullable.LiftedNull) {
						return (Constant) new Nullable.LiftedBinaryOperator (oper, left, right, loc).Resolve (ec);
					}
				}

				if (!DoBinaryNumericPromotions (ec, ref left, ref right))
					return null;

				bool_res = false;
				if (left is DoubleConstant)
					bool_res = ((DoubleConstant) left).Value <
						((DoubleConstant) right).Value;
				else if (left is FloatConstant)
					bool_res = ((FloatConstant) left).Value <
						((FloatConstant) right).Value;
				else if (left is ULongConstant)
					bool_res = ((ULongConstant) left).Value <
						((ULongConstant) right).Value;
				else if (left is LongConstant)
					bool_res = ((LongConstant) left).Value <
						((LongConstant) right).Value;
				else if (left is UIntConstant)
					bool_res = ((UIntConstant) left).Value <
						((UIntConstant) right).Value;
				else if (left is IntConstant)
					bool_res = ((IntConstant) left).Value <
						((IntConstant) right).Value;
				else
					return null;

				return new BoolConstant (bool_res, left.Location);
				
			case Binary.Operator.GreaterThan:
				if (right is NullLiteral) {
					if (left is NullLiteral) {
						var lifted_int = new Nullable.NullableType (TypeManager.int32_type, loc).ResolveAsTypeTerminal (ec, false);
						return (Constant) new Nullable.LiftedBinaryOperator (oper, lifted_int, right, loc).Resolve (ec);
					}

					if (left is Nullable.LiftedNull) {
						return (Constant) new Nullable.LiftedBinaryOperator (oper, left, right, loc).Resolve (ec);
					}
				}

				if (!DoBinaryNumericPromotions (ec, ref left, ref right))
					return null;

				bool_res = false;
				if (left is DoubleConstant)
					bool_res = ((DoubleConstant) left).Value >
						((DoubleConstant) right).Value;
				else if (left is FloatConstant)
					bool_res = ((FloatConstant) left).Value >
						((FloatConstant) right).Value;
				else if (left is ULongConstant)
					bool_res = ((ULongConstant) left).Value >
						((ULongConstant) right).Value;
				else if (left is LongConstant)
					bool_res = ((LongConstant) left).Value >
						((LongConstant) right).Value;
				else if (left is UIntConstant)
					bool_res = ((UIntConstant) left).Value >
						((UIntConstant) right).Value;
				else if (left is IntConstant)
					bool_res = ((IntConstant) left).Value >
						((IntConstant) right).Value;
				else
					return null;

				return new BoolConstant (bool_res, left.Location);

			case Binary.Operator.GreaterThanOrEqual:
				if (right is NullLiteral) {
					if (left is NullLiteral) {
						var lifted_int = new Nullable.NullableType (TypeManager.int32_type, loc).ResolveAsTypeTerminal (ec, false);
						return (Constant) new Nullable.LiftedBinaryOperator (oper, lifted_int, right, loc).Resolve (ec);
					}

					if (left is Nullable.LiftedNull) {
						return (Constant) new Nullable.LiftedBinaryOperator (oper, left, right, loc).Resolve (ec);
					}
				}

				if (!DoBinaryNumericPromotions (ec, ref left, ref right))
					return null;

				bool_res = false;
				if (left is DoubleConstant)
					bool_res = ((DoubleConstant) left).Value >=
						((DoubleConstant) right).Value;
				else if (left is FloatConstant)
					bool_res = ((FloatConstant) left).Value >=
						((FloatConstant) right).Value;
				else if (left is ULongConstant)
					bool_res = ((ULongConstant) left).Value >=
						((ULongConstant) right).Value;
				else if (left is LongConstant)
					bool_res = ((LongConstant) left).Value >=
						((LongConstant) right).Value;
				else if (left is UIntConstant)
					bool_res = ((UIntConstant) left).Value >=
						((UIntConstant) right).Value;
				else if (left is IntConstant)
					bool_res = ((IntConstant) left).Value >=
						((IntConstant) right).Value;
				else
					return null;

				return new BoolConstant (bool_res, left.Location);

			case Binary.Operator.LessThanOrEqual:
				if (right is NullLiteral) {
					if (left is NullLiteral) {
						var lifted_int = new Nullable.NullableType (TypeManager.int32_type, loc).ResolveAsTypeTerminal (ec, false);
						return (Constant) new Nullable.LiftedBinaryOperator (oper, lifted_int, right, loc).Resolve (ec);
					}

					if (left is Nullable.LiftedNull) {
						return (Constant) new Nullable.LiftedBinaryOperator (oper, left, right, loc).Resolve (ec);
					}
				}

				if (!DoBinaryNumericPromotions (ec, ref left, ref right))
					return null;

				bool_res = false;
				if (left is DoubleConstant)
					bool_res = ((DoubleConstant) left).Value <=
						((DoubleConstant) right).Value;
				else if (left is FloatConstant)
					bool_res = ((FloatConstant) left).Value <=
						((FloatConstant) right).Value;
				else if (left is ULongConstant)
					bool_res = ((ULongConstant) left).Value <=
						((ULongConstant) right).Value;
				else if (left is LongConstant)
					bool_res = ((LongConstant) left).Value <=
						((LongConstant) right).Value;
				else if (left is UIntConstant)
					bool_res = ((UIntConstant) left).Value <=
						((UIntConstant) right).Value;
				else if (left is IntConstant)
					bool_res = ((IntConstant) left).Value <=
						((IntConstant) right).Value;
				else
					return null;

				return new BoolConstant (bool_res, left.Location);
			}
					
			return null;
		}
	}
}
