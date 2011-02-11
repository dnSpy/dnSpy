//
// conversion.cs: various routines for implementing conversions.
//
// Authors:
//   Miguel de Icaza (miguel@ximian.com)
//   Ravi Pratap (ravi@ximian.com)
//   Marek Safar (marek.safar@gmail.com)
//
// Copyright 2001, 2002, 2003 Ximian, Inc.
// Copyright 2003-2008 Novell, Inc.
//

using System;
using System.Collections.Generic;

#if STATIC
using IKVM.Reflection.Emit;
#else
using System.Reflection.Emit;
#endif

namespace Mono.CSharp {

	//
	// A container class for all the conversion operations
	//
	static class Convert {
		
		static EmptyExpression MyEmptyExpr;
		
		static Convert ()
		{
			Reset ();
		}
		
		public static void Reset ()
		{
			MyEmptyExpr = null;
		}
		
		//
		// From a one-dimensional array-type S[] to System.Collections.IList<T> and base
		// interfaces of this interface, provided there is an implicit reference conversion
		// from S to T.
		//
		static bool ArrayToIList (ArrayContainer array, TypeSpec list, bool isExplicit)
		{
			if (array.Rank != 1 || !list.IsGeneric)
				return false;

			var open_version = list.GetDefinition ();
			if ((open_version != TypeManager.generic_ilist_type) &&
				(open_version != TypeManager.generic_icollection_type) &&
				(open_version != TypeManager.generic_ienumerable_type))
				return false;

			var arg_type = list.TypeArguments[0];
			if (array.Element == arg_type)
				return true;

			if (isExplicit)
				return ExplicitReferenceConversionExists (array.Element, arg_type);

			if (MyEmptyExpr == null)
				MyEmptyExpr = new EmptyExpression (array.Element);
			else
				MyEmptyExpr.SetType (array.Element);

			return ImplicitReferenceConversionExists (MyEmptyExpr, arg_type);
		}
		
		static bool IList_To_Array(TypeSpec list, ArrayContainer array)
		{
			if (array.Rank != 1 || !list.IsGeneric)
				return false;

			var open_version = list.GetDefinition ();
			if ((open_version != TypeManager.generic_ilist_type) &&
				(open_version != TypeManager.generic_icollection_type) &&
				(open_version != TypeManager.generic_ienumerable_type))
				return false;

			var arg_type = list.TypeArguments[0];
			if (array.Element == arg_type)
				return true;
			
			if (MyEmptyExpr == null)
				MyEmptyExpr = new EmptyExpression (array.Element);
			else
				MyEmptyExpr.SetType (array.Element);

			return ImplicitReferenceConversionExists (MyEmptyExpr, arg_type) || ExplicitReferenceConversionExists (array.Element, arg_type);
		}

		public static Expression ImplicitTypeParameterConversion (Expression expr, TypeSpec target_type)
		{
			var expr_type = (TypeParameterSpec) expr.Type;

			//
			// From T to a type parameter U, provided T depends on U
			//
			var ttype = target_type as TypeParameterSpec;
			if (ttype != null) {
				if (expr_type.TypeArguments != null) {
					foreach (var targ in expr_type.TypeArguments) {
						if (!TypeSpecComparer.Override.IsEqual (ttype, targ))
							continue;

						if (expr_type.IsReferenceType && !ttype.IsReferenceType)
							return new BoxedCast (expr, target_type);

						return new ClassCast (expr, target_type);
					}
				}

				return null;
			}

			//
			// LAMESPEC: From T to dynamic type because it's like T to object
			//
			if (target_type == InternalType.Dynamic) {
				if (expr_type.IsReferenceType)
					return new ClassCast (expr, target_type);

				return new BoxedCast (expr, target_type);
			}

			//
			// From T to its effective base class C
			// From T to any base class of C (it cannot contain dynamic of be of dynamic type)
			// From T to any interface implemented by C
			//
			var base_type = expr_type.GetEffectiveBase ();
			if (base_type == target_type || TypeSpec.IsBaseClass (base_type, target_type, false) || base_type.ImplementsInterface (target_type, true)) {
				if (expr_type.IsReferenceType)
					return new ClassCast (expr, target_type);

				return new BoxedCast (expr, target_type);
			}

			if (target_type.IsInterface && expr_type.IsConvertibleToInterface (target_type)) {
				if (expr_type.IsReferenceType)
					return new ClassCast (expr, target_type);

				return new BoxedCast (expr, target_type);
			}

			return null;
		}

		static Expression ExplicitTypeParameterConversion (Expression source, TypeSpec source_type, TypeSpec target_type)
		{
			var target_tp = target_type as TypeParameterSpec;
			if (target_tp != null) {
				if (target_tp.TypeArguments != null) {
					foreach (var targ in target_tp.TypeArguments) {
						if (!TypeSpecComparer.Override.IsEqual (source_type, targ))
							continue;

						return source == null ? EmptyExpression.Null : new ClassCast (source, target_type);
					}
				}
/*
				if (target_tp.Interfaces != null) {
					foreach (TypeSpec iface in target_tp.Interfaces) {
						if (!TypeManager.IsGenericParameter (iface))
							continue;

						if (TypeManager.IsSubclassOf (source_type, iface))
							return source == null ? EmptyExpression.Null : new ClassCast (source, target_type, true);
					}
				}
*/
				return null;
			}

			if (target_type.IsInterface)
				return source == null ? EmptyExpression.Null : new ClassCast (source, target_type, true);

			return null;
		}

		public static Expression ImplicitReferenceConversion (Expression expr, TypeSpec target_type, bool explicit_cast)
		{
			TypeSpec expr_type = expr.Type;

			if (expr_type == null && expr.eclass == ExprClass.MethodGroup){
				// if we are a method group, emit a warning

				expr.Emit (null);
			}

			if (expr_type == TypeManager.void_type)
				return null;

			if (expr_type.Kind == MemberKind.TypeParameter)
				return ImplicitTypeParameterConversion (expr, target_type);

			//
			// from the null type to any reference-type.
			//
			NullLiteral nl = expr as NullLiteral;
			if (nl != null) {
				return nl.ConvertImplicitly (null, target_type);
			}

			if (ImplicitReferenceConversionExists (expr, target_type)) {
				// 
				// Avoid wrapping implicitly convertible reference type
				//
				if (!explicit_cast)
					return expr;

				return EmptyCast.Create (expr, target_type);
			}

			return ImplicitBoxingConversion (expr, expr_type, target_type);
		}

		//
		// 6.1.6 Implicit reference conversions
		//
		public static bool ImplicitReferenceConversionExists (Expression expr, TypeSpec target_type)
		{
			if (TypeManager.IsStruct (target_type))
				return false;

			TypeSpec expr_type = expr.Type;

			// from the null type to any reference-type.
			if (expr_type == InternalType.Null)
				return target_type != InternalType.AnonymousMethod;

			if (TypeManager.IsGenericParameter (expr_type))
				return ImplicitTypeParameterConversion (expr, target_type) != null;

			// This code is kind of mirrored inside ImplicitStandardConversionExists
			// with the small distinction that we only probe there
			//
			// Always ensure that the code here and there is in sync

			// from any class-type S to any interface-type T.
			if (target_type.IsInterface) {
				if (expr_type.ImplementsInterface (target_type, true)){
					return !TypeManager.IsValueType (expr_type);
				}
			}

			//
			// Implicit reference conversions (no-boxing) to object or dynamic
			//
			if (target_type == TypeManager.object_type || target_type == InternalType.Dynamic) {
				switch (expr_type.Kind) {
				case MemberKind.Class:
				case MemberKind.Interface:
				case MemberKind.Delegate:
				case MemberKind.ArrayType:
					return true;
				}

				return expr_type == InternalType.Dynamic;
			}

			if (target_type == TypeManager.value_type) {
				return expr_type == TypeManager.enum_type;
			} else if (expr_type == target_type || TypeSpec.IsBaseClass (expr_type, target_type, true)) {
				//
				// Special case: enumeration to System.Enum.
				// System.Enum is not a value type, it is a class, so we need
				// a boxing conversion
				//
				if (target_type == TypeManager.enum_type || TypeManager.IsGenericParameter (expr_type))
					return false;

				if (TypeManager.IsValueType (expr_type))
					return false;

				// Array type variance conversion
				//if (target_type.IsArray != expr_type.IsArray)
				//	return false;

				return true;
			}

			var expr_type_array = expr_type as ArrayContainer;
			if (expr_type_array != null) {
				var target_type_array = target_type as ArrayContainer;
				// from an array-type S to an array-type of type T
				if (target_type_array != null && expr_type_array.Rank == target_type_array.Rank) {

					//
					// Both SE and TE are reference-types
					//
					TypeSpec expr_element_type = expr_type_array.Element;
					if (!TypeManager.IsReferenceType (expr_element_type))
						return false;

					TypeSpec target_element_type = target_type_array.Element;
					if (!TypeManager.IsReferenceType (target_element_type))
						return false;

					if (MyEmptyExpr == null)
						MyEmptyExpr = new EmptyExpression (expr_element_type);
					else
						MyEmptyExpr.SetType (expr_element_type);

					return ImplicitStandardConversionExists (MyEmptyExpr, target_element_type);
				}

				// from an array-type to System.Array
				if (target_type == TypeManager.array_type)
					return true;

				// from an array-type of type T to IList<T>
				if (ArrayToIList (expr_type_array, target_type, false))
					return true;

				return false;
			}

			if (TypeSpecComparer.IsEqual (expr_type, target_type))
				return true;

			if (TypeSpecComparer.Variant.IsEqual (expr_type, target_type))
				return true;

			// from any interface type S to interface-type T.
			if (expr_type.IsInterface && target_type.IsInterface) {
				return expr_type.ImplementsInterface (target_type, true);
			}

			// from any delegate type to System.Delegate
			if (target_type == TypeManager.delegate_type &&
				(expr_type == TypeManager.delegate_type || expr_type.IsDelegate))
				return true;

			return false;
		}

		public static Expression ImplicitBoxingConversion (Expression expr, TypeSpec expr_type, TypeSpec target_type)
		{
			//
			// From any value-type to the type object.
			//
			if (target_type == TypeManager.object_type || target_type == InternalType.Dynamic) {
				//
				// A pointer type cannot be converted to object
				//
				if (expr_type.IsPointer)
					return null;

				if (!TypeManager.IsValueType (expr_type))
					return null;

				return expr == null ? EmptyExpression.Null : new BoxedCast (expr, target_type);
			}
			
			//
			// From any value-type to the type System.ValueType.
			//
			if (target_type == TypeManager.value_type) {
				if (!TypeManager.IsValueType (expr_type))
					return null;

				return expr == null ? EmptyExpression.Null : new BoxedCast (expr, target_type);
			}

			if (target_type == TypeManager.enum_type) {
				//
				// From any enum-type to the type System.Enum.
				//
				if (TypeManager.IsEnumType (expr_type))
					return expr == null ? EmptyExpression.Null : new BoxedCast (expr, target_type);
			}

			//
			// From a nullable-type to a reference type, if a boxing conversion exists from
			// the underlying type to the reference type
			//
			if (TypeManager.IsNullableType (expr_type)) {
				if (!TypeManager.IsReferenceType (target_type))
					return null;

				var res = ImplicitBoxingConversion (expr, Nullable.NullableInfo.GetUnderlyingType (expr_type), target_type);

				// "cast" underlying type to target type to emit correct InvalidCastException when
				// underlying hierarchy changes without recompilation
				if (res != null && expr != null)
					res = new UnboxCast (res, target_type);

				return res;
			}

			if (TypeSpec.IsBaseClass (expr_type, target_type, false)) {
				//
				// Don't box same type arguments
				//
				if (TypeManager.IsGenericParameter (expr_type) && expr_type != target_type)
					return expr == null ? EmptyExpression.Null : new BoxedCast (expr, target_type);

				return null;
			}

			// This code is kind of mirrored inside ImplicitStandardConversionExists
			// with the small distinction that we only probe there
			//
			// Always ensure that the code here and there is in sync

			// from any class-type S to any interface-type T.
			if (target_type.IsInterface) {
				if (expr_type.ImplementsInterface (target_type, true) &&
					(TypeManager.IsGenericParameter (expr_type) || TypeManager.IsValueType (expr_type))) {
					return expr == null ? EmptyExpression.Null : new BoxedCast (expr, target_type);
				}
			}

			return null;
		}

		public static Expression ImplicitNulableConversion (ResolveContext ec, Expression expr, TypeSpec target_type)
		{
			TypeSpec expr_type = expr.Type;

			//
			// From null to any nullable type
			//
			if (expr_type == InternalType.Null)
				return ec == null ? EmptyExpression.Null : Nullable.LiftedNull.Create (target_type, expr.Location);

			// S -> T?
			TypeSpec t_el = Nullable.NullableInfo.GetUnderlyingType (target_type);

			// S? -> T?
			if (TypeManager.IsNullableType (expr_type))
				expr_type = Nullable.NullableInfo.GetUnderlyingType (expr_type);

			//
			// Predefined implicit identity or implicit numeric conversion
			// has to exist between underlying type S and underlying type T
			//

			// conversion exists only mode
			if (ec == null) {
				if (TypeSpecComparer.IsEqual (expr_type, t_el))
					return EmptyExpression.Null;

				if (expr is Constant)
					return ((Constant) expr).ConvertImplicitly (ec, t_el);

				return ImplicitNumericConversion (null, expr_type, t_el);
			}

			Expression unwrap;
			if (expr_type != expr.Type)
				unwrap = Nullable.Unwrap.Create (expr);
			else
				unwrap = expr;

			Expression conv = unwrap;
			if (!TypeSpecComparer.IsEqual (expr_type, t_el)) {
				if (conv is Constant)
					conv = ((Constant)conv).ConvertImplicitly (ec, t_el);
				else
					conv = ImplicitNumericConversion (conv, expr_type, t_el);

				if (conv == null)
					return null;
			}
			
			if (expr_type != expr.Type)
				return new Nullable.Lifted (conv, unwrap, target_type).Resolve (ec);

			return Nullable.Wrap.Create (conv, target_type);
		}

		/// <summary>
		///   Implicit Numeric Conversions.
		///
		///   expr is the expression to convert, returns a new expression of type
		///   target_type or null if an implicit conversion is not possible.
		/// </summary>
		public static Expression ImplicitNumericConversion (Expression expr, TypeSpec target_type)
		{
			return ImplicitNumericConversion (expr, expr.Type, target_type);
		}

		static Expression ImplicitNumericConversion (Expression expr, TypeSpec expr_type, TypeSpec target_type)
		{
			if (expr_type == TypeManager.sbyte_type){
				//
				// From sbyte to short, int, long, float, double, decimal
				//
				if (target_type == TypeManager.int32_type)
					return expr == null ? EmptyExpression.Null : new OpcodeCast (expr, target_type, OpCodes.Conv_I4);
				if (target_type == TypeManager.int64_type)
					return expr == null ? EmptyExpression.Null : new OpcodeCast (expr, target_type, OpCodes.Conv_I8);
				if (target_type == TypeManager.double_type)
					return expr == null ? EmptyExpression.Null : new OpcodeCast (expr, target_type, OpCodes.Conv_R8);
				if (target_type == TypeManager.float_type)
					return expr == null ? EmptyExpression.Null : new OpcodeCast (expr, target_type, OpCodes.Conv_R4);
				if (target_type == TypeManager.short_type)
					return expr == null ? EmptyExpression.Null : new OpcodeCast (expr, target_type, OpCodes.Conv_I2);
				if (target_type == TypeManager.decimal_type)
					return expr == null ? EmptyExpression.Null : new CastToDecimal (expr);
			} else if (expr_type == TypeManager.byte_type){
				//
				// From byte to short, ushort, int, uint, long, ulong, float, double, decimal
				//
				if (target_type == TypeManager.int32_type || target_type == TypeManager.uint32_type ||
				    target_type == TypeManager.short_type || target_type == TypeManager.ushort_type)
					return expr == null ? EmptyExpression.Null : EmptyCast.Create (expr, target_type);

				if (target_type == TypeManager.uint64_type)
					return expr == null ? EmptyExpression.Null : new OpcodeCast (expr, target_type, OpCodes.Conv_U8);
				if (target_type == TypeManager.int64_type)
					return expr == null ? EmptyExpression.Null : new OpcodeCast (expr, target_type, OpCodes.Conv_I8);
				if (target_type == TypeManager.float_type)
					return expr == null ? EmptyExpression.Null : new OpcodeCast (expr, target_type, OpCodes.Conv_R4);
				if (target_type == TypeManager.double_type)
					return expr == null ? EmptyExpression.Null : new OpcodeCast (expr, target_type, OpCodes.Conv_R8);
				if (target_type == TypeManager.decimal_type)
					return expr == null ? EmptyExpression.Null : new CastToDecimal (expr);

			} else if (expr_type == TypeManager.short_type){
				//
				// From short to int, long, float, double, decimal
				//
				if (target_type == TypeManager.int32_type)
					return expr == null ? EmptyExpression.Null : EmptyCast.Create (expr, target_type);
				if (target_type == TypeManager.int64_type)
					return expr == null ? EmptyExpression.Null : new OpcodeCast (expr, target_type, OpCodes.Conv_I8);
				if (target_type == TypeManager.double_type)
					return expr == null ? EmptyExpression.Null : new OpcodeCast (expr, target_type, OpCodes.Conv_R8);
				if (target_type == TypeManager.float_type)
					return expr == null ? EmptyExpression.Null : new OpcodeCast (expr, target_type, OpCodes.Conv_R4);
				if (target_type == TypeManager.decimal_type)
					return expr == null ? EmptyExpression.Null : new CastToDecimal (expr);

			} else if (expr_type == TypeManager.ushort_type){
				//
				// From ushort to int, uint, long, ulong, float, double, decimal
				//
				if (target_type == TypeManager.int32_type || target_type == TypeManager.uint32_type)
					return expr == null ? EmptyExpression.Null : EmptyCast.Create (expr, target_type);
				
				if (target_type == TypeManager.uint64_type)
					return expr == null ? EmptyExpression.Null : new OpcodeCast (expr, target_type, OpCodes.Conv_U8);
				if (target_type == TypeManager.int64_type)
					return expr == null ? EmptyExpression.Null : new OpcodeCast (expr, target_type, OpCodes.Conv_I8);
				if (target_type == TypeManager.double_type)
					return expr == null ? EmptyExpression.Null : new OpcodeCast (expr, target_type, OpCodes.Conv_R8);
				if (target_type == TypeManager.float_type)
					return expr == null ? EmptyExpression.Null : new OpcodeCast (expr, target_type, OpCodes.Conv_R4);
				if (target_type == TypeManager.decimal_type)
					return expr == null ? EmptyExpression.Null : new CastToDecimal (expr);
			} else if (expr_type == TypeManager.int32_type){
				//
				// From int to long, float, double, decimal
				//
				if (target_type == TypeManager.int64_type)
					return expr == null ? EmptyExpression.Null : new OpcodeCast (expr, target_type, OpCodes.Conv_I8);
				if (target_type == TypeManager.double_type)
					return expr == null ? EmptyExpression.Null : new OpcodeCast (expr, target_type, OpCodes.Conv_R8);
				if (target_type == TypeManager.float_type)
					return expr == null ? EmptyExpression.Null : new OpcodeCast (expr, target_type, OpCodes.Conv_R4);
				if (target_type == TypeManager.decimal_type)
					return expr == null ? EmptyExpression.Null : new CastToDecimal (expr);
			} else if (expr_type == TypeManager.uint32_type){
				//
				// From uint to long, ulong, float, double, decimal
				//
				if (target_type == TypeManager.int64_type)
					return expr == null ? EmptyExpression.Null : new OpcodeCast (expr, target_type, OpCodes.Conv_U8);
				if (target_type == TypeManager.uint64_type)
					return expr == null ? EmptyExpression.Null : new OpcodeCast (expr, target_type, OpCodes.Conv_U8);
				if (target_type == TypeManager.double_type)
					return expr == null ? EmptyExpression.Null : new OpcodeCastDuplex (expr, target_type, OpCodes.Conv_R_Un, OpCodes.Conv_R8);
				if (target_type == TypeManager.float_type)
					return expr == null ? EmptyExpression.Null : new OpcodeCastDuplex (expr, target_type, OpCodes.Conv_R_Un, OpCodes.Conv_R4);
				if (target_type == TypeManager.decimal_type)
					return expr == null ? EmptyExpression.Null : new CastToDecimal (expr);
			} else if (expr_type == TypeManager.int64_type){
				//
				// From long/ulong to float, double
				//
				if (target_type == TypeManager.double_type)
					return expr == null ? EmptyExpression.Null : new OpcodeCast (expr, target_type, OpCodes.Conv_R8);
				if (target_type == TypeManager.float_type)
					return expr == null ? EmptyExpression.Null : new OpcodeCast (expr, target_type, OpCodes.Conv_R4);
				if (target_type == TypeManager.decimal_type)
					return expr == null ? EmptyExpression.Null : new CastToDecimal (expr);
			} else if (expr_type == TypeManager.uint64_type){
				//
				// From ulong to float, double
				//
				if (target_type == TypeManager.double_type)
					return expr == null ? EmptyExpression.Null : new OpcodeCastDuplex (expr, target_type, OpCodes.Conv_R_Un, OpCodes.Conv_R8);
				if (target_type == TypeManager.float_type)
					return expr == null ? EmptyExpression.Null : new OpcodeCastDuplex (expr, target_type, OpCodes.Conv_R_Un, OpCodes.Conv_R4);
				if (target_type == TypeManager.decimal_type)
					return expr == null ? EmptyExpression.Null : new CastToDecimal (expr);
			} else if (expr_type == TypeManager.char_type){
				//
				// From char to ushort, int, uint, long, ulong, float, double, decimal
				//
				if ((target_type == TypeManager.ushort_type) ||
				    (target_type == TypeManager.int32_type) ||
				    (target_type == TypeManager.uint32_type))
					return expr == null ? EmptyExpression.Null : EmptyCast.Create (expr, target_type);
				if (target_type == TypeManager.uint64_type)
					return expr == null ? EmptyExpression.Null : new OpcodeCast (expr, target_type, OpCodes.Conv_U8);
				if (target_type == TypeManager.int64_type)
					return expr == null ? EmptyExpression.Null : new OpcodeCast (expr, target_type, OpCodes.Conv_I8);
				if (target_type == TypeManager.float_type)
					return expr == null ? EmptyExpression.Null : new OpcodeCast (expr, target_type, OpCodes.Conv_R4);
				if (target_type == TypeManager.double_type)
					return expr == null ? EmptyExpression.Null : new OpcodeCast (expr, target_type, OpCodes.Conv_R8);
				if (target_type == TypeManager.decimal_type)
					return expr == null ? EmptyExpression.Null : new CastToDecimal (expr);
			} else if (expr_type == TypeManager.float_type){
				//
				// float to double
				//
				if (target_type == TypeManager.double_type)
					return expr == null ? EmptyExpression.Null : new OpcodeCast (expr, target_type, OpCodes.Conv_R8);
			}

			return null;
		}

		/// <summary>
		///  Same as ImplicitStandardConversionExists except that it also looks at
		///  implicit user defined conversions - needed for overload resolution
		/// </summary>
		public static bool ImplicitConversionExists (ResolveContext ec, Expression expr, TypeSpec target_type)
		{
			if (ImplicitStandardConversionExists (expr, target_type))
				return true;

			if (expr.Type == InternalType.AnonymousMethod) {
				if (!TypeManager.IsDelegateType (target_type) && target_type.GetDefinition () != TypeManager.expression_type)
					return false;

				AnonymousMethodExpression ame = (AnonymousMethodExpression) expr;
				return ame.ImplicitStandardConversionExists (ec, target_type);
			}
			
			if (expr.eclass == ExprClass.MethodGroup) {
				if (target_type.IsDelegate && RootContext.Version != LanguageVersion.ISO_1) {
					MethodGroupExpr mg = expr as MethodGroupExpr;
					if (mg != null)
						return DelegateCreation.ImplicitStandardConversionExists (ec, mg, target_type);
				}

				return false;
			}

			return ImplicitUserConversion (ec, expr, target_type, Location.Null) != null;
		}

		/// <summary>
		///  Determines if a standard implicit conversion exists from
		///  expr_type to target_type
		///
		/// </summary>
		public static bool ImplicitStandardConversionExists (Expression expr, TypeSpec target_type)
		{
			TypeSpec expr_type = expr.Type;

			NullLiteral nl = expr as NullLiteral;
			if (nl != null)
				return nl.ConvertImplicitly (null, target_type) != null;

			if (expr_type == TypeManager.void_type)
				return false;

			if (expr_type == target_type)
				return true;

			// Implicit dynamic conversion
			if (expr_type == InternalType.Dynamic) {
				switch (target_type.Kind) {
				case MemberKind.ArrayType:
				case MemberKind.Class:
				case MemberKind.Struct:
				case MemberKind.Delegate:
				case MemberKind.Enum:
				case MemberKind.Interface:
				case MemberKind.TypeParameter:
					return true;
				}

				// dynamic to __arglist
				if (target_type == InternalType.Arglist)
					return true;

				return false;
			}

			if (TypeManager.IsNullableType (target_type)) {
				return ImplicitNulableConversion (null, expr, target_type) != null;
			}

			// First numeric conversions
			if (ImplicitNumericConversion (null, expr_type, target_type) != null)
				return true;

			if (ImplicitReferenceConversionExists (expr, target_type))
				return true;

			if (ImplicitBoxingConversion (null, expr_type, target_type) != null)
				return true;
			
			//
			// Implicit Constant Expression Conversions
			//
			if (expr is IntConstant){
				int value = ((IntConstant) expr).Value;

				if (target_type == TypeManager.sbyte_type){
					if (value >= SByte.MinValue && value <= SByte.MaxValue)
						return true;
				} else if (target_type == TypeManager.byte_type){
					if (value >= 0 && value <= Byte.MaxValue)
						return true;
				} else if (target_type == TypeManager.short_type){
					if (value >= Int16.MinValue && value <= Int16.MaxValue)
						return true;
				} else if (target_type == TypeManager.ushort_type){
					if (value >= UInt16.MinValue && value <= UInt16.MaxValue)
						return true;
				} else if (target_type == TypeManager.uint32_type){
					if (value >= 0)
						return true;
				} else if (target_type == TypeManager.uint64_type){
					 //
					 // we can optimize this case: a positive int32
					 // always fits on a uint64.  But we need an opcode
					 // to do it.
					 //
					if (value >= 0)
						return true;
				}
			}

			if (expr is LongConstant && target_type == TypeManager.uint64_type){
				//
				// Try the implicit constant expression conversion
				// from long to ulong, instead of a nice routine,
				// we just inline it
				//
				long v = ((LongConstant) expr).Value;
				if (v >= 0)
					return true;
			}

			if (expr is IntegralConstant && TypeManager.IsEnumType (target_type)) {
				var i = (IntegralConstant) expr;
				//
				// LAMESPEC: csc allows any constant like 0 values to be converted, including const float f = 0.0
				//
				// An implicit enumeration conversion permits the decimal-integer-literal 0
				// to be converted to any enum-type and to any nullable-type whose underlying
				// type is an enum-type
				//
				return i.IsZeroInteger;
			}

			//
			// If `expr_type' implements `target_type' (which is an iface)
			// see TryImplicitIntConversion
			//
			if (target_type.IsInterface && expr_type.ImplementsInterface (target_type, true))
				return true;

			if (target_type.IsPointer && expr_type.IsPointer && ((PointerContainer) target_type).Element.BuildinType == BuildinTypeSpec.Type.Void)
				return true;

			// Conversion from __arglist to System.ArgIterator
			if (expr_type == InternalType.Arglist)
				return target_type == TypeManager.arg_iterator_type;

			if (TypeSpecComparer.IsEqual (expr_type, target_type))
				return true;

			return false;
		}

		/// <summary>
		///  Finds "most encompassed type" according to the spec (13.4.2)
		///  amongst the methods in the MethodGroupExpr
		/// </summary>
		public static TypeSpec FindMostEncompassedType (IEnumerable<TypeSpec> types)
		{
			TypeSpec best = null;
			EmptyExpression expr = EmptyExpression.Grab ();

			foreach (TypeSpec t in types) {
				if (best == null) {
					best = t;
					continue;
				}

				expr.SetType (t);
				if (ImplicitStandardConversionExists (expr, best))
					best = t;
			}

			expr.SetType (best);
			foreach (TypeSpec t in types) {
				if (best == t)
					continue;
				if (!ImplicitStandardConversionExists (expr, t)) {
					best = null;
					break;
				}
			}

			EmptyExpression.Release (expr);

			return best;
		}

		/// <summary>
		///  Finds "most encompassing type" according to the spec (13.4.2)
		///  amongst the types in the given set
		/// </summary>
		static TypeSpec FindMostEncompassingType (IList<TypeSpec> types)
		{
			TypeSpec best = null;

			if (types.Count == 0)
				return null;

			if (types.Count == 1)
				return types [0];

			EmptyExpression expr = EmptyExpression.Grab ();

			foreach (TypeSpec t in types) {
				if (best == null) {
					best = t;
					continue;
				}

				expr.SetType (best);
				if (ImplicitStandardConversionExists (expr, t))
					best = t;
			}

			foreach (TypeSpec t in types) {
				if (best == t)
					continue;
				expr.SetType (t);
				if (!ImplicitStandardConversionExists (expr, best)) {
					best = null;
					break;
				}
			}

			EmptyExpression.Release (expr);

			return best;
		}

		//
		// Finds the most specific source Sx according to the rules of the spec (13.4.4)
		// by making use of FindMostEncomp* methods. Applies the correct rules separately
		// for explicit and implicit conversion operators.
		//
		static TypeSpec FindMostSpecificSource (List<MethodSpec> list, TypeSpec sourceType, Expression source, bool apply_explicit_conv_rules)
		{
			var src_types_set = new TypeSpec [list.Count];

			//
			// Try exact match first, if any operator converts from S then Sx = S
			//
			for (int i = 0; i < src_types_set.Length; ++i) {
				TypeSpec param_type = list [i].Parameters.Types [0];

				if (param_type == sourceType)
					return param_type;

				src_types_set [i] = param_type;
			}

			//
			// Explicit Conv rules
			//
			if (apply_explicit_conv_rules) {
				var candidate_set = new List<TypeSpec> ();

				foreach (TypeSpec param_type in src_types_set){
					if (ImplicitStandardConversionExists (source, param_type))
						candidate_set.Add (param_type);
				}

				if (candidate_set.Count != 0)
					return FindMostEncompassedType (candidate_set);
			}

			//
			// Final case
			//
			if (apply_explicit_conv_rules)
				return FindMostEncompassingType (src_types_set);
			else
				return FindMostEncompassedType (src_types_set);
		}

		/// <summary>
		///  Finds the most specific target Tx according to section 13.4.4
		/// </summary>
		static public TypeSpec FindMostSpecificTarget (IList<MethodSpec> list,
							   TypeSpec target, bool apply_explicit_conv_rules)
		{
			var tgt_types_set = new List<TypeSpec> ();

			//
			// If any operator converts to T then Tx = T
			//
			foreach (var mi in list){
				TypeSpec ret_type = mi.ReturnType;
				if (ret_type == target)
					return ret_type;

				tgt_types_set.Add (ret_type);
			}

			//
			// Explicit conv rules
			//
			if (apply_explicit_conv_rules) {
				var candidate_set = new List<TypeSpec> ();

				EmptyExpression expr = EmptyExpression.Grab ();

				foreach (TypeSpec ret_type in tgt_types_set){
					expr.SetType (ret_type);

					if (ImplicitStandardConversionExists (expr, target))
						candidate_set.Add (ret_type);
				}

				EmptyExpression.Release (expr);

				if (candidate_set.Count != 0)
					return FindMostEncompassingType (candidate_set);
			}

			//
			// Okay, final case !
			//
			if (apply_explicit_conv_rules)
				return FindMostEncompassedType (tgt_types_set);
			else
				return FindMostEncompassingType (tgt_types_set);
		}

		/// <summary>
		///  User-defined Implicit conversions
		/// </summary>
		static public Expression ImplicitUserConversion (ResolveContext ec, Expression source, TypeSpec target, Location loc)
		{
			return UserDefinedConversion (ec, source, target, true, loc);
		}

		/// <summary>
		///  User-defined Explicit conversions
		/// </summary>
		static Expression ExplicitUserConversion (ResolveContext ec, Expression source, TypeSpec target, Location loc)
		{
			return UserDefinedConversion (ec, source, target, false, loc);
		}

		static void FindApplicableUserDefinedConversionOperators (IList<MemberSpec> operators, Expression source, TypeSpec target, bool implicitOnly, ref List<MethodSpec> candidates)
		{
			//
			// LAMESPEC: Undocumented IntPtr/UIntPtr conversions
			// IntPtr -> uint uses int
			// UIntPtr -> long uses ulong
			//
			if (source.Type == TypeManager.intptr_type) {
				if (target == TypeManager.uint32_type)
					target = TypeManager.int32_type;
			} else if (source.Type == TypeManager.uintptr_type) {
				if (target == TypeManager.int64_type)
					target = TypeManager.uint64_type;
			}

			// Neither A nor B are interface-types
			if (source.Type.IsInterface)
				return;

			// For a conversion operator to be applicable, it must be possible
			// to perform a standard conversion from the source type to
			// the operand type of the operator, and it must be possible
			// to perform a standard conversion from the result type of
			// the operator to the target type.

			Expression texpr = null;

			foreach (MethodSpec op in operators) {
				
				// Can be null because MemberCache.GetUserOperator does not resize the array
				if (op == null)
					continue;

				var t = op.Parameters.Types[0];
				if (source.Type != t && !ImplicitStandardConversionExists (source, t)) {
					if (implicitOnly)
						continue;

					if (!ImplicitStandardConversionExists (new EmptyExpression (t), source.Type))
						continue;
				}

				t = op.ReturnType;

				// LAMESPEC: Exclude UIntPtr -> int conversion
				if (t == TypeManager.uint32_type && source.Type == TypeManager.uintptr_type)
					continue;

				if (t.IsInterface)
					continue;

				if (target != t) {
					if (TypeManager.IsNullableType (t))
						t = Nullable.NullableInfo.GetUnderlyingType (t);

					if (!ImplicitStandardConversionExists (new EmptyExpression (t), target)) {
						if (implicitOnly)
							continue;

						if (texpr == null)
							texpr = new EmptyExpression (target);

						if (!ImplicitStandardConversionExists (texpr, t))
							continue;
					}
				}

				if (candidates == null)
					candidates = new List<MethodSpec> ();

				candidates.Add (op);
			}
		}

		//
		// User-defined conversions
		//
		static Expression UserDefinedConversion (ResolveContext ec, Expression source, TypeSpec target, bool implicitOnly, Location loc)
		{
			List<MethodSpec> candidates = null;

			//
			// If S or T are nullable types, source_type and target_type are their underlying types
			// otherwise source_type and target_type are equal to S and T respectively.
			//
			TypeSpec source_type = source.Type;
			TypeSpec target_type = target;
			Expression source_type_expr;

			if (TypeManager.IsNullableType (source_type)) {
				// No implicit conversion S? -> T for non-reference types
				if (implicitOnly && !TypeManager.IsReferenceType (target_type) && !TypeManager.IsNullableType (target_type))
					return null;

				source_type_expr = Nullable.Unwrap.Create (source);
				source_type = source_type_expr.Type;
			} else {
				source_type_expr = source;
			}

			if (TypeManager.IsNullableType (target_type))
				target_type = Nullable.NullableInfo.GetUnderlyingType (target_type);

			// Only these containers can contain a user defined implicit or explicit operators
			const MemberKind user_conversion_kinds = MemberKind.Class | MemberKind.Struct | MemberKind.TypeParameter;

			if ((source_type.Kind & user_conversion_kinds) != 0 && source_type != TypeManager.decimal_type) {
				bool declared_only = source_type.IsStruct;

				var operators = MemberCache.GetUserOperator (source_type, Operator.OpType.Implicit, declared_only);
				if (operators != null) {
					FindApplicableUserDefinedConversionOperators (operators, source_type_expr, target_type, implicitOnly, ref candidates);
				}

				if (!implicitOnly) {
					operators = MemberCache.GetUserOperator (source_type, Operator.OpType.Explicit, declared_only);
					if (operators != null) {
						FindApplicableUserDefinedConversionOperators (operators, source_type_expr, target_type, false, ref candidates);
					}
				}
			}

			if ((target.Kind & user_conversion_kinds) != 0 && target_type != TypeManager.decimal_type) {
				bool declared_only = target.IsStruct || implicitOnly;

				var operators = MemberCache.GetUserOperator (target_type, Operator.OpType.Implicit, declared_only);
				if (operators != null) {
					FindApplicableUserDefinedConversionOperators (operators, source_type_expr, target_type, implicitOnly, ref candidates);
				}

				if (!implicitOnly) {
					operators = MemberCache.GetUserOperator (target_type, Operator.OpType.Explicit, declared_only);
					if (operators != null) {
						FindApplicableUserDefinedConversionOperators (operators, source_type_expr, target_type, false, ref candidates);
					}
				}
			}

			if (candidates == null)
				return null;

			//
			// Find the most specific conversion operator
			//
			MethodSpec most_specific_operator;
			TypeSpec s_x, t_x;
			if (candidates.Count == 1) {
				most_specific_operator = candidates[0];
				s_x = most_specific_operator.Parameters.Types[0];
				t_x = most_specific_operator.ReturnType;
			} else {
				//
				// Pass original source type to find the best match against input type and
				// not the unwrapped expression
				//
				s_x = FindMostSpecificSource (candidates, source.Type, source_type_expr, !implicitOnly);
				if (s_x == null)
					return null;

				t_x = FindMostSpecificTarget (candidates, target, !implicitOnly);
				if (t_x == null)
					return null;

				most_specific_operator = null;
				for (int i = 0; i < candidates.Count; ++i) {
					if (candidates[i].ReturnType == t_x && candidates[i].Parameters.Types[0] == s_x) {
						most_specific_operator = candidates[i];
						break;
					}
				}

				if (most_specific_operator == null) {
					MethodSpec ambig_arg = null;
					foreach (var candidate in candidates) {
						if (candidate.ReturnType == t_x)
							most_specific_operator = candidate;
						else if (candidate.Parameters.Types[0] == s_x)
							ambig_arg = candidate;
					}

					ec.Report.Error (457, loc,
						"Ambiguous user defined operators `{0}' and `{1}' when converting from `{2}' to `{3}'",
						ambig_arg.GetSignatureForError (), most_specific_operator.GetSignatureForError (),
						source.Type.GetSignatureForError (), target.GetSignatureForError ());
				}
			}

			//
			// Convert input type when it's different to best operator argument
			//
			if (s_x != source_type) {
				var c = source as Constant;
				if (c != null) {
					source = c.TryReduce (ec, s_x, loc);
				} else {
					source = implicitOnly ?
						ImplicitConversionStandard (ec, source_type_expr, s_x, loc) :
						ExplicitConversionStandard (ec, source_type_expr, s_x, loc);
				}
			} else {
				source = source_type_expr;
			}

			source = new UserCast (most_specific_operator, source, loc).Resolve (ec);

			//
			// Convert result type when it's different to best operator return type
			//
			if (t_x != target_type) {
				//
				// User operator is of T?, no need to lift it
				//
				if (TypeManager.IsNullableType (t_x) && t_x == target)
					return source;

				source = implicitOnly ?
					ImplicitConversionStandard (ec, source, target_type, loc) :
					ExplicitConversionStandard (ec, source, target_type, loc);

				if (source == null)
					return null;
			}

			//
			// Source expression is of nullable type, lift the result in the case it's null and
			// not nullable/lifted user operator is used
			//
			if (source_type_expr is Nullable.Unwrap && !TypeManager.IsNullableType (s_x) && (TypeManager.IsReferenceType (target) || target_type != target))
				source = new Nullable.Lifted (source, source_type_expr, target).Resolve (ec);
			else if (target_type != target)
				source = Nullable.Wrap.Create (source, target);

			return source;
		}

		/// <summary>
		///   Converts implicitly the resolved expression `expr' into the
		///   `target_type'.  It returns a new expression that can be used
		///   in a context that expects a `target_type'.
		/// </summary>
		static public Expression ImplicitConversion (ResolveContext ec, Expression expr,
							     TypeSpec target_type, Location loc)
		{
			Expression e;

			if (target_type == null)
				throw new Exception ("Target type is null");

			e = ImplicitConversionStandard (ec, expr, target_type, loc);
			if (e != null)
				return e;

			e = ImplicitUserConversion (ec, expr, target_type, loc);
			if (e != null)
				return e;

			return null;
		}


		/// <summary>
		///   Attempts to apply the `Standard Implicit
		///   Conversion' rules to the expression `expr' into
		///   the `target_type'.  It returns a new expression
		///   that can be used in a context that expects a
		///   `target_type'.
		///
		///   This is different from `ImplicitConversion' in that the
		///   user defined implicit conversions are excluded.
		/// </summary>
		static public Expression ImplicitConversionStandard (ResolveContext ec, Expression expr,
								     TypeSpec target_type, Location loc)
		{
			return ImplicitConversionStandard (ec, expr, target_type, loc, false);
		}

		static Expression ImplicitConversionStandard (ResolveContext ec, Expression expr, TypeSpec target_type, Location loc, bool explicit_cast)
		{
			if (expr.eclass == ExprClass.MethodGroup){
				if (!TypeManager.IsDelegateType (target_type)){
					return null;
				}

				//
				// Only allow anonymous method conversions on post ISO_1
				//
				if (RootContext.Version != LanguageVersion.ISO_1){
					MethodGroupExpr mg = expr as MethodGroupExpr;
					if (mg != null)
						return ImplicitDelegateCreation.Create (
							ec, mg, target_type, loc);
				}
			}

			TypeSpec expr_type = expr.Type;
			Expression e;

			if (expr_type == target_type) {
				if (expr_type != InternalType.Null && expr_type != InternalType.AnonymousMethod)
					return expr;
				return null;
			}

			if (expr_type == InternalType.Dynamic) {
				switch (target_type.Kind) {
				case MemberKind.ArrayType:
				case MemberKind.Class:
					if (target_type == TypeManager.object_type)
						return EmptyCast.Create (expr, target_type);

					goto case MemberKind.Struct;
				case MemberKind.Struct:
					// TODO: Should really introduce MemberKind.Void
					if (target_type == TypeManager.void_type)
						return null;

					goto case MemberKind.Enum;
				case MemberKind.Delegate:
				case MemberKind.Enum:
				case MemberKind.Interface:
				case MemberKind.TypeParameter:
					Arguments args = new Arguments (1);
					args.Add (new Argument (expr));
					return new DynamicConversion (target_type, explicit_cast ? CSharpBinderFlags.ConvertExplicit : 0, args, loc).Resolve (ec);
				}

				return null;
			}

			if (TypeManager.IsNullableType (target_type))
				return ImplicitNulableConversion (ec, expr, target_type);

			//
			// Attempt to do the implicit constant expression conversions
			//
			Constant c = expr as Constant;
			if (c != null) {
				try {
					c = c.ConvertImplicitly (ec, target_type);
				} catch {
					Console.WriteLine ("Conversion error happened in line {0}", loc);
					throw;
				}
				if (c != null)
					return c;
			}

			e = ImplicitNumericConversion (expr, expr_type, target_type);
			if (e != null)
				return e;

			e = ImplicitReferenceConversion (expr, target_type, explicit_cast);
			if (e != null)
				return e;

			if (expr is IntegralConstant && TypeManager.IsEnumType (target_type)){
				var i = (IntegralConstant) expr;
				//
				// LAMESPEC: csc allows any constant like 0 values to be converted, including const float f = 0.0
				//
				// An implicit enumeration conversion permits the decimal-integer-literal 0
				// to be converted to any enum-type and to any nullable-type whose underlying
				// type is an enum-type
				//
				if (i.IsZeroInteger) {
					// Recreate 0 literal to remove any collected conversions
					return new EnumConstant (new IntLiteral (0, i.Location), target_type).Resolve (ec);
				}
			}

			if (ec.IsUnsafe) {
				var target_pc = target_type as PointerContainer;
				if (target_pc != null) {
					if (expr_type.IsPointer) {
						//
						// Pointer types are same when they have same element types
						//
						if (expr_type == target_pc)
							return expr;

						if (target_pc.Element.BuildinType == BuildinTypeSpec.Type.Void)
							return EmptyCast.Create (expr, target_type);

						//return null;
					}

					if (expr_type == InternalType.Null)
						return EmptyCast.Create (new NullPointer (loc), target_type);
				}
			}

			if (expr_type == InternalType.AnonymousMethod){
				AnonymousMethodExpression ame = (AnonymousMethodExpression) expr;
				Expression am = ame.Compatible (ec, target_type);
				if (am != null)
					return am.Resolve (ec);
			}

			if (expr_type == InternalType.Arglist && target_type == TypeManager.arg_iterator_type)
				return expr;

			if (TypeSpecComparer.IsEqual (expr_type, target_type)) {
				if (expr_type == target_type)
					return expr;

				return EmptyCast.Create (expr, target_type);
			}

			return null;
		}

		/// <summary>
		///   Attempts to implicitly convert `source' into `target_type', using
		///   ImplicitConversion.  If there is no implicit conversion, then
		///   an error is signaled
		/// </summary>
		static public Expression ImplicitConversionRequired (ResolveContext ec, Expression source,
								     TypeSpec target_type, Location loc)
		{
			Expression e = ImplicitConversion (ec, source, target_type, loc);
			if (e != null)
				return e;

			source.Error_ValueCannotBeConverted (ec, loc, target_type, false);
			return null;
		}

		/// <summary>
		///   Performs the explicit numeric conversions
		///
		/// There are a few conversions that are not part of the C# standard,
		/// they were interim hacks in the C# compiler that were supposed to
		/// become explicit operators in the UIntPtr class and IntPtr class,
		/// but for historical reasons it did not happen, so the C# compiler
		/// ended up with these special hacks.
		///
		/// See bug 59800 for details.
		///
		/// The conversion are:
		///   UIntPtr->SByte
		///   UIntPtr->Int16
		///   UIntPtr->Int32
		///   IntPtr->UInt64
		///   UInt64->IntPtr
		///   SByte->UIntPtr
		///   Int16->UIntPtr
		///
		/// </summary>
		public static Expression ExplicitNumericConversion (Expression expr, TypeSpec target_type)
		{
			TypeSpec expr_type = expr.Type;
			TypeSpec real_target_type = target_type;

			if (expr_type == TypeManager.sbyte_type){
				//
				// From sbyte to byte, ushort, uint, ulong, char, uintptr
				//
				if (real_target_type == TypeManager.byte_type)
					return new ConvCast (expr, target_type, ConvCast.Mode.I1_U1);
				if (real_target_type == TypeManager.ushort_type)
					return new ConvCast (expr, target_type, ConvCast.Mode.I1_U2);
				if (real_target_type == TypeManager.uint32_type)
					return new ConvCast (expr, target_type, ConvCast.Mode.I1_U4);
				if (real_target_type == TypeManager.uint64_type)
					return new ConvCast (expr, target_type, ConvCast.Mode.I1_U8);
				if (real_target_type == TypeManager.char_type)
					return new ConvCast (expr, target_type, ConvCast.Mode.I1_CH);

				// One of the built-in conversions that belonged in the class library
				if (real_target_type == TypeManager.uintptr_type){
					Expression u8e = new ConvCast (expr, TypeManager.uint64_type, ConvCast.Mode.I1_U8);

					return new OperatorCast (u8e, TypeManager.uintptr_type, true);
				}
			} else if (expr_type == TypeManager.byte_type){
				//
				// From byte to sbyte and char
				//
				if (real_target_type == TypeManager.sbyte_type)
					return new ConvCast (expr, target_type, ConvCast.Mode.U1_I1);
				if (real_target_type == TypeManager.char_type)
					return new ConvCast (expr, target_type, ConvCast.Mode.U1_CH);
			} else if (expr_type == TypeManager.short_type){
				//
				// From short to sbyte, byte, ushort, uint, ulong, char, uintptr
				//
				if (real_target_type == TypeManager.sbyte_type)
					return new ConvCast (expr, target_type, ConvCast.Mode.I2_I1);
				if (real_target_type == TypeManager.byte_type)
					return new ConvCast (expr, target_type, ConvCast.Mode.I2_U1);
				if (real_target_type == TypeManager.ushort_type)
					return new ConvCast (expr, target_type, ConvCast.Mode.I2_U2);
				if (real_target_type == TypeManager.uint32_type)
					return new ConvCast (expr, target_type, ConvCast.Mode.I2_U4);
				if (real_target_type == TypeManager.uint64_type)
					return new ConvCast (expr, target_type, ConvCast.Mode.I2_U8);
				if (real_target_type == TypeManager.char_type)
					return new ConvCast (expr, target_type, ConvCast.Mode.I2_CH);

				// One of the built-in conversions that belonged in the class library
				if (real_target_type == TypeManager.uintptr_type){
					Expression u8e = new ConvCast (expr, TypeManager.uint64_type, ConvCast.Mode.I2_U8);

					return new OperatorCast (u8e, TypeManager.uintptr_type, true);
				}
			} else if (expr_type == TypeManager.ushort_type){
				//
				// From ushort to sbyte, byte, short, char
				//
				if (real_target_type == TypeManager.sbyte_type)
					return new ConvCast (expr, target_type, ConvCast.Mode.U2_I1);
				if (real_target_type == TypeManager.byte_type)
					return new ConvCast (expr, target_type, ConvCast.Mode.U2_U1);
				if (real_target_type == TypeManager.short_type)
					return new ConvCast (expr, target_type, ConvCast.Mode.U2_I2);
				if (real_target_type == TypeManager.char_type)
					return new ConvCast (expr, target_type, ConvCast.Mode.U2_CH);
			} else if (expr_type == TypeManager.int32_type){
				//
				// From int to sbyte, byte, short, ushort, uint, ulong, char, uintptr
				//
				if (real_target_type == TypeManager.sbyte_type)
					return new ConvCast (expr, target_type, ConvCast.Mode.I4_I1);
				if (real_target_type == TypeManager.byte_type)
					return new ConvCast (expr, target_type, ConvCast.Mode.I4_U1);
				if (real_target_type == TypeManager.short_type)
					return new ConvCast (expr, target_type, ConvCast.Mode.I4_I2);
				if (real_target_type == TypeManager.ushort_type)
					return new ConvCast (expr, target_type, ConvCast.Mode.I4_U2);
				if (real_target_type == TypeManager.uint32_type)
					return new ConvCast (expr, target_type, ConvCast.Mode.I4_U4);
				if (real_target_type == TypeManager.uint64_type)
					return new ConvCast (expr, target_type, ConvCast.Mode.I4_U8);
				if (real_target_type == TypeManager.char_type)
					return new ConvCast (expr, target_type, ConvCast.Mode.I4_CH);

				// One of the built-in conversions that belonged in the class library
				if (real_target_type == TypeManager.uintptr_type){
					Expression u8e = new ConvCast (expr, TypeManager.uint64_type, ConvCast.Mode.I2_U8);

					return new OperatorCast (u8e, TypeManager.uintptr_type, true);
				}
			} else if (expr_type == TypeManager.uint32_type){
				//
				// From uint to sbyte, byte, short, ushort, int, char
				//
				if (real_target_type == TypeManager.sbyte_type)
					return new ConvCast (expr, target_type, ConvCast.Mode.U4_I1);
				if (real_target_type == TypeManager.byte_type)
					return new ConvCast (expr, target_type, ConvCast.Mode.U4_U1);
				if (real_target_type == TypeManager.short_type)
					return new ConvCast (expr, target_type, ConvCast.Mode.U4_I2);
				if (real_target_type == TypeManager.ushort_type)
					return new ConvCast (expr, target_type, ConvCast.Mode.U4_U2);
				if (real_target_type == TypeManager.int32_type)
					return new ConvCast (expr, target_type, ConvCast.Mode.U4_I4);
				if (real_target_type == TypeManager.char_type)
					return new ConvCast (expr, target_type, ConvCast.Mode.U4_CH);
			} else if (expr_type == TypeManager.int64_type){
				//
				// From long to sbyte, byte, short, ushort, int, uint, ulong, char
				//
				if (real_target_type == TypeManager.sbyte_type)
					return new ConvCast (expr, target_type, ConvCast.Mode.I8_I1);
				if (real_target_type == TypeManager.byte_type)
					return new ConvCast (expr, target_type, ConvCast.Mode.I8_U1);
				if (real_target_type == TypeManager.short_type)
					return new ConvCast (expr, target_type, ConvCast.Mode.I8_I2);
				if (real_target_type == TypeManager.ushort_type)
					return new ConvCast (expr, target_type, ConvCast.Mode.I8_U2);
				if (real_target_type == TypeManager.int32_type)
					return new ConvCast (expr, target_type, ConvCast.Mode.I8_I4);
				if (real_target_type == TypeManager.uint32_type)
					return new ConvCast (expr, target_type, ConvCast.Mode.I8_U4);
				if (real_target_type == TypeManager.uint64_type)
					return new ConvCast (expr, target_type, ConvCast.Mode.I8_U8);
				if (real_target_type == TypeManager.char_type)
					return new ConvCast (expr, target_type, ConvCast.Mode.I8_CH);
			} else if (expr_type == TypeManager.uint64_type){
				//
				// From ulong to sbyte, byte, short, ushort, int, uint, long, char
				//
				if (real_target_type == TypeManager.sbyte_type)
					return new ConvCast (expr, target_type, ConvCast.Mode.U8_I1);
				if (real_target_type == TypeManager.byte_type)
					return new ConvCast (expr, target_type, ConvCast.Mode.U8_U1);
				if (real_target_type == TypeManager.short_type)
					return new ConvCast (expr, target_type, ConvCast.Mode.U8_I2);
				if (real_target_type == TypeManager.ushort_type)
					return new ConvCast (expr, target_type, ConvCast.Mode.U8_U2);
				if (real_target_type == TypeManager.int32_type)
					return new ConvCast (expr, target_type, ConvCast.Mode.U8_I4);
				if (real_target_type == TypeManager.uint32_type)
					return new ConvCast (expr, target_type, ConvCast.Mode.U8_U4);
				if (real_target_type == TypeManager.int64_type)
					return new ConvCast (expr, target_type, ConvCast.Mode.U8_I8);
				if (real_target_type == TypeManager.char_type)
					return new ConvCast (expr, target_type, ConvCast.Mode.U8_CH);

				// One of the built-in conversions that belonged in the class library
				if (real_target_type == TypeManager.intptr_type){
					return new OperatorCast (EmptyCast.Create (expr, TypeManager.int64_type),
								 TypeManager.intptr_type, true);
				}
			} else if (expr_type == TypeManager.char_type){
				//
				// From char to sbyte, byte, short
				//
				if (real_target_type == TypeManager.sbyte_type)
					return new ConvCast (expr, target_type, ConvCast.Mode.CH_I1);
				if (real_target_type == TypeManager.byte_type)
					return new ConvCast (expr, target_type, ConvCast.Mode.CH_U1);
				if (real_target_type == TypeManager.short_type)
					return new ConvCast (expr, target_type, ConvCast.Mode.CH_I2);
			} else if (expr_type == TypeManager.float_type){
				//
				// From float to sbyte, byte, short,
				// ushort, int, uint, long, ulong, char
				// or decimal
				//
				if (real_target_type == TypeManager.sbyte_type)
					return new ConvCast (expr, target_type, ConvCast.Mode.R4_I1);
				if (real_target_type == TypeManager.byte_type)
					return new ConvCast (expr, target_type, ConvCast.Mode.R4_U1);
				if (real_target_type == TypeManager.short_type)
					return new ConvCast (expr, target_type, ConvCast.Mode.R4_I2);
				if (real_target_type == TypeManager.ushort_type)
					return new ConvCast (expr, target_type, ConvCast.Mode.R4_U2);
				if (real_target_type == TypeManager.int32_type)
					return new ConvCast (expr, target_type, ConvCast.Mode.R4_I4);
				if (real_target_type == TypeManager.uint32_type)
					return new ConvCast (expr, target_type, ConvCast.Mode.R4_U4);
				if (real_target_type == TypeManager.int64_type)
					return new ConvCast (expr, target_type, ConvCast.Mode.R4_I8);
				if (real_target_type == TypeManager.uint64_type)
					return new ConvCast (expr, target_type, ConvCast.Mode.R4_U8);
				if (real_target_type == TypeManager.char_type)
					return new ConvCast (expr, target_type, ConvCast.Mode.R4_CH);
				if (real_target_type == TypeManager.decimal_type)
					return new CastToDecimal (expr, true);
			} else if (expr_type == TypeManager.double_type){
				//
				// From double to sbyte, byte, short,
				// ushort, int, uint, long, ulong,
				// char, float or decimal
				//
				if (real_target_type == TypeManager.sbyte_type)
					return new ConvCast (expr, target_type, ConvCast.Mode.R8_I1);
				if (real_target_type == TypeManager.byte_type)
					return new ConvCast (expr, target_type, ConvCast.Mode.R8_U1);
				if (real_target_type == TypeManager.short_type)
					return new ConvCast (expr, target_type, ConvCast.Mode.R8_I2);
				if (real_target_type == TypeManager.ushort_type)
					return new ConvCast (expr, target_type, ConvCast.Mode.R8_U2);
				if (real_target_type == TypeManager.int32_type)
					return new ConvCast (expr, target_type, ConvCast.Mode.R8_I4);
				if (real_target_type == TypeManager.uint32_type)
					return new ConvCast (expr, target_type, ConvCast.Mode.R8_U4);
				if (real_target_type == TypeManager.int64_type)
					return new ConvCast (expr, target_type, ConvCast.Mode.R8_I8);
				if (real_target_type == TypeManager.uint64_type)
					return new ConvCast (expr, target_type, ConvCast.Mode.R8_U8);
				if (real_target_type == TypeManager.char_type)
					return new ConvCast (expr, target_type, ConvCast.Mode.R8_CH);
				if (real_target_type == TypeManager.float_type)
					return new ConvCast (expr, target_type, ConvCast.Mode.R8_R4);
				if (real_target_type == TypeManager.decimal_type)
					return new CastToDecimal (expr, true);
			} else if (expr_type == TypeManager.uintptr_type){
				//
				// Various built-in conversions that belonged in the class library
				//
				// from uintptr to sbyte, short, int32
				//
				if (real_target_type == TypeManager.sbyte_type){
					Expression uint32e = new OperatorCast (expr, TypeManager.uint32_type, true);
					return new ConvCast (uint32e, TypeManager.sbyte_type, ConvCast.Mode.U4_I1);
				}
				if (real_target_type == TypeManager.short_type){
					Expression uint32e = new OperatorCast (expr, TypeManager.uint32_type, true);
					return new ConvCast (uint32e, TypeManager.sbyte_type, ConvCast.Mode.U4_I2);
				}
				if (real_target_type == TypeManager.int32_type){
					return EmptyCast.Create (new OperatorCast (expr, TypeManager.uint32_type, true),
							      TypeManager.int32_type);
				}
			} else if (expr_type == TypeManager.intptr_type){
				if (real_target_type == TypeManager.uint64_type){
					return EmptyCast.Create (new OperatorCast (expr, TypeManager.int64_type, true),
							      TypeManager.uint64_type);
				}
			} else if (expr_type == TypeManager.decimal_type) {
				return new CastFromDecimal (expr, target_type).Resolve ();
			}
			return null;
		}

		/// <summary>
		///  Returns whether an explicit reference conversion can be performed
		///  from source_type to target_type
		/// </summary>
		public static bool ExplicitReferenceConversionExists (TypeSpec source_type, TypeSpec target_type)
		{
			Expression e = ExplicitReferenceConversion (null, source_type, target_type);
			if (e == null)
				return false;

			if (e == EmptyExpression.Null)
				return true;

			throw new InternalErrorException ("Invalid probing conversion result");
		}

		/// <summary>
		///   Implements Explicit Reference conversions
		/// </summary>
		static Expression ExplicitReferenceConversion (Expression source, TypeSpec source_type, TypeSpec target_type)
		{
			//
			// From object to a generic parameter
			//
			if (source_type == TypeManager.object_type && TypeManager.IsGenericParameter (target_type))
				return source == null ? EmptyExpression.Null : new UnboxCast (source, target_type);

			//
			// Explicit type parameter conversion.
			//
			if (source_type.Kind == MemberKind.TypeParameter)
				return ExplicitTypeParameterConversion (source, source_type, target_type);

			bool target_is_value_type = TypeManager.IsStruct (target_type) || TypeManager.IsEnumType (target_type);

			//
			// Unboxing conversion from System.ValueType to any non-nullable-value-type
			//
			if (source_type == TypeManager.value_type && target_is_value_type)
				return source == null ? EmptyExpression.Null : new UnboxCast (source, target_type);

			//
			// From object or dynamic to any reference type or value type (unboxing)
			//
			if (source_type == TypeManager.object_type || source_type == InternalType.Dynamic) {
				if (target_type.IsPointer)
					return null;

				return
					source == null ? EmptyExpression.Null :
					target_is_value_type ? new UnboxCast (source, target_type) :
					source is Constant ? (Expression) new EmptyConstantCast ((Constant) source, target_type) :
					new ClassCast (source, target_type);
			}

			//
			// From any class S to any class-type T, provided S is a base class of T
			//
			if (source_type.Kind == MemberKind.Class && TypeSpec.IsBaseClass (target_type, source_type, true))
				return source == null ? EmptyExpression.Null : new ClassCast (source, target_type);

			//
			// From any interface-type S to to any class type T, provided T is not
			// sealed, or provided T implements S.
			//
			if (source_type.IsInterface) {
				if (!target_type.IsSealed || target_type.ImplementsInterface (source_type, true)) {
					if (source == null)
						return EmptyExpression.Null;

					//
					// Unboxing conversion from any interface-type to any non-nullable-value-type that
					// implements the interface-type
					//
					return target_is_value_type ? new UnboxCast (source, target_type) : (Expression) new ClassCast (source, target_type);
				}

				//
				// From System.Collections.Generic.IList<T> and its base interfaces to a one-dimensional
				// array type S[], provided there is an implicit or explicit reference conversion from S to T.
				//
				var target_array = target_type as ArrayContainer;
				if (target_array != null && IList_To_Array (source_type, target_array))
					return source == null ? EmptyExpression.Null : new ClassCast (source, target_type);

				return null;
			}

			var source_array = source_type as ArrayContainer;
			if (source_array != null) {
				var target_array = target_type as ArrayContainer;
				if (target_array != null) {
					//
					// From System.Array to any array-type
					//
					if (source_type == TypeManager.array_type)
						return source == null ? EmptyExpression.Null : new ClassCast (source, target_type);

					//
					// From an array type S with an element type Se to an array type T with an
					// element type Te provided all the following are true:
					//     * S and T differe only in element type, in other words, S and T
					//       have the same number of dimensions.
					//     * Both Se and Te are reference types
					//     * An explicit reference conversions exist from Se to Te
					//
					if (source_array.Rank == target_array.Rank) {

						source_type = source_array.Element;
						if (!TypeManager.IsReferenceType (source_type))
							return null;

						var target_element = target_array.Element;
						if (!TypeManager.IsReferenceType (target_element))
							return null;

						if (ExplicitReferenceConversionExists (source_type, target_element))
							return source == null ? EmptyExpression.Null : new ClassCast (source, target_type);
							
						return null;
					}
				}

				//
				// From a single-dimensional array type S[] to System.Collections.Generic.IList<T> and its base interfaces, 
				// provided that there is an explicit reference conversion from S to T
				//
				if (ArrayToIList (source_array, target_type, true))
					return source == null ? EmptyExpression.Null : new ClassCast (source, target_type);

				return null;
			}

			//
			// From any class type S to any interface T, provides S is not sealed
			// and provided S does not implement T.
			//
			if (target_type.IsInterface && !source_type.IsSealed && !source_type.ImplementsInterface (target_type, true)) {
				return source == null ? EmptyExpression.Null : new ClassCast (source, target_type);
			}

			//
			// From System delegate to any delegate-type
			//
			if (source_type == TypeManager.delegate_type && TypeManager.IsDelegateType (target_type))
				return source == null ? EmptyExpression.Null : new ClassCast (source, target_type);

			//
			// From variant generic delegate to same variant generic delegate type
			//
			if (source_type.IsDelegate && target_type.IsDelegate && source_type.MemberDefinition == target_type.MemberDefinition) {
				var tparams = source_type.MemberDefinition.TypeParameters;
				var targs_src = source_type.TypeArguments;
				var targs_dst = target_type.TypeArguments;
				int i;
				for (i = 0; i < tparams.Length; ++i) {
					//
					// If TP is invariant, types have to be identical
					//
					if (TypeSpecComparer.IsEqual (targs_src[i], targs_dst[i]))
						continue;

					if (tparams[i].Variance == Variance.Covariant) {
						//
						//If TP is covariant, an implicit or explicit identity or reference conversion is required
						//
						if (ImplicitReferenceConversionExists (new EmptyExpression (targs_src[i]), targs_dst[i]))
							continue;

						if (ExplicitReferenceConversionExists (targs_src[i], targs_dst[i]))
							continue;

					} else if (tparams[i].Variance == Variance.Contravariant) {
						//
						//If TP is contravariant, both are either identical or reference types
						//
						if (TypeManager.IsReferenceType (targs_src[i]) && TypeManager.IsReferenceType (targs_dst[i]))
							continue;
					}

					break;
				}

				if (i == tparams.Length)
					return source == null ? EmptyExpression.Null : new ClassCast (source, target_type);
			}

			return null;
		}

		/// <summary>
		///   Performs an explicit conversion of the expression `expr' whose
		///   type is expr.Type to `target_type'.
		/// </summary>
		static public Expression ExplicitConversionCore (ResolveContext ec, Expression expr,
								 TypeSpec target_type, Location loc)
		{
			TypeSpec expr_type = expr.Type;

			// Explicit conversion includes implicit conversion and it used for enum underlying types too
			Expression ne = ImplicitConversionStandard (ec, expr, target_type, loc, true);
			if (ne != null)
				return ne;

			if (TypeManager.IsEnumType (expr_type)) {
				TypeSpec real_target = TypeManager.IsEnumType (target_type) ? EnumSpec.GetUnderlyingType (target_type) : target_type;
				Expression underlying = EmptyCast.Create (expr, EnumSpec.GetUnderlyingType (expr_type));
				if (underlying.Type == real_target)
					ne = underlying;

				if (ne == null)
					ne = ImplicitNumericConversion (underlying, real_target);

				if (ne == null)
					ne = ExplicitNumericConversion (underlying, real_target);

				//
				// LAMESPEC: IntPtr and UIntPtr conversion to any Enum is allowed
				//
				if (ne == null && (real_target == TypeManager.intptr_type || real_target == TypeManager.uintptr_type))
					ne = ExplicitUserConversion (ec, underlying, real_target, loc);

				return ne != null ? EmptyCast.Create (ne, target_type) : null;
			}

			if (TypeManager.IsEnumType (target_type)) {
				//
				// System.Enum can be unboxed to any enum-type
				//
				if (expr_type == TypeManager.enum_type)
					return new UnboxCast (expr, target_type);

				TypeSpec real_target = TypeManager.IsEnumType (target_type) ? EnumSpec.GetUnderlyingType (target_type) : target_type;

				if (expr_type == real_target)
					return EmptyCast.Create (expr, target_type);

				ne = ImplicitNumericConversion (expr, real_target);
				if (ne != null)
					return EmptyCast.Create (ne, target_type);

				ne = ExplicitNumericConversion (expr, real_target);
				if (ne != null)
					return EmptyCast.Create (ne, target_type);

				//
				// LAMESPEC: IntPtr and UIntPtr conversion to any Enum is allowed
				//
				if (expr_type == TypeManager.intptr_type || expr_type == TypeManager.uintptr_type) {
					ne = ExplicitUserConversion (ec, expr, real_target, loc);
					if (ne != null)
						return ExplicitConversionCore (ec, ne, target_type, loc);
				}
			} else {
				ne = ExplicitNumericConversion (expr, target_type);
				if (ne != null)
					return ne;
			}

			//
			// Skip the ExplicitReferenceConversion because we can not convert
			// from Null to a ValueType, and ExplicitReference wont check against
			// null literal explicitly
			//
			if (expr_type != InternalType.Null) {
				ne = ExplicitReferenceConversion (expr, expr_type, target_type);
				if (ne != null)
					return ne;
			}

			if (ec.IsUnsafe){
				ne = ExplicitUnsafe (expr, target_type);
				if (ne != null)
					return ne;
			}
			
			return null;
		}

		public static Expression ExplicitUnsafe (Expression expr, TypeSpec target_type)
		{
			TypeSpec expr_type = expr.Type;

			if (target_type.IsPointer){
				if (expr_type.IsPointer)
					return EmptyCast.Create (expr, target_type);

				if (expr_type == TypeManager.sbyte_type ||
					expr_type == TypeManager.short_type ||
					expr_type == TypeManager.int32_type)
					return new OpcodeCast (expr, target_type, OpCodes.Conv_I);

				if (expr_type == TypeManager.ushort_type ||
					expr_type == TypeManager.uint32_type ||
					expr_type == TypeManager.byte_type)
					return new OpcodeCast (expr, target_type, OpCodes.Conv_U);

				if (expr_type == TypeManager.int64_type)
					return new ConvCast (expr, target_type, ConvCast.Mode.I8_I);

				if (expr_type == TypeManager.uint64_type)
					return new ConvCast (expr, target_type, ConvCast.Mode.U8_I);
			}

			if (expr_type.IsPointer){
				if (target_type == TypeManager.sbyte_type)
					return new OpcodeCast (expr, target_type, OpCodes.Conv_I1);
				if (target_type == TypeManager.byte_type)
					return new OpcodeCast (expr, target_type, OpCodes.Conv_U1);
				if (target_type == TypeManager.short_type)
					return new OpcodeCast (expr, target_type, OpCodes.Conv_I2);
				if (target_type == TypeManager.ushort_type)
					return new OpcodeCast (expr, target_type, OpCodes.Conv_U2);
				if (target_type == TypeManager.int32_type)
					return new OpcodeCast (expr, target_type, OpCodes.Conv_I4);
				if (target_type == TypeManager.uint32_type)
					return new OpcodeCast (expr, target_type, OpCodes.Conv_U4);
				if (target_type == TypeManager.int64_type)
					return new ConvCast (expr, target_type, ConvCast.Mode.I_I8);
				if (target_type == TypeManager.uint64_type)
					return new OpcodeCast (expr, target_type, OpCodes.Conv_U8);
			}
			return null;
		}

		/// <summary>
		///   Same as ExplicitConversion, only it doesn't include user defined conversions
		/// </summary>
		static public Expression ExplicitConversionStandard (ResolveContext ec, Expression expr,
								     TypeSpec target_type, Location l)
		{
			int errors = ec.Report.Errors;
			Expression ne = ImplicitConversionStandard (ec, expr, target_type, l);
			if (ec.Report.Errors > errors)
				return null;

			if (ne != null)
				return ne;

			ne = ExplicitNumericConversion (expr, target_type);
			if (ne != null)
				return ne;

			ne = ExplicitReferenceConversion (expr, expr.Type, target_type);
			if (ne != null)
				return ne;

			if (ec.IsUnsafe && expr.Type.IsPointer && target_type.IsPointer && ((PointerContainer)expr.Type).Element.BuildinType == BuildinTypeSpec.Type.Void)
				return EmptyCast.Create (expr, target_type);

			expr.Error_ValueCannotBeConverted (ec, l, target_type, true);
			return null;
		}

		/// <summary>
		///   Performs an explicit conversion of the expression `expr' whose
		///   type is expr.Type to `target_type'.
		/// </summary>
		static public Expression ExplicitConversion (ResolveContext ec, Expression expr,
			TypeSpec target_type, Location loc)
		{
			Expression e = ExplicitConversionCore (ec, expr, target_type, loc);
			if (e != null) {
				//
				// Don't eliminate explicit precission casts
				//
				if (e == expr) {
					if (target_type == TypeManager.float_type)
						return new OpcodeCast (expr, target_type, OpCodes.Conv_R4);
					
					if (target_type == TypeManager.double_type)
						return new OpcodeCast (expr, target_type, OpCodes.Conv_R8);
				}
					
				return e;
			}

			TypeSpec expr_type = expr.Type;
			if (TypeManager.IsNullableType (target_type)) {
				if (TypeManager.IsNullableType (expr_type)) {
					TypeSpec target = Nullable.NullableInfo.GetUnderlyingType (target_type);
					Expression unwrap = Nullable.Unwrap.Create (expr);
					e = ExplicitConversion (ec, unwrap, target, expr.Location);
					if (e == null)
						return null;

					return new Nullable.Lifted (e, unwrap, target_type).Resolve (ec);
				} else if (expr_type == TypeManager.object_type) {
					return new UnboxCast (expr, target_type);
				} else {
					TypeSpec target = TypeManager.GetTypeArguments (target_type) [0];

					e = ExplicitConversionCore (ec, expr, target, loc);
					if (e != null)
						return Nullable.Wrap.Create (e, target_type);
				}
			} else if (TypeManager.IsNullableType (expr_type)) {
				e = ImplicitBoxingConversion (expr, Nullable.NullableInfo.GetUnderlyingType (expr_type), target_type);
				if (e != null)
					return e;

				e = Nullable.Unwrap.Create (expr, false);			
				e = ExplicitConversionCore (ec, e, target_type, loc);
				if (e != null)
					return EmptyCast.Create (e, target_type);
			}
			
			e = ExplicitUserConversion (ec, expr, target_type, loc);
			if (e != null)
				return e;			

			expr.Error_ValueCannotBeConverted (ec, loc, target_type, true);
			return null;
		}
	}
}
