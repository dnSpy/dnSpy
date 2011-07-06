// Copyright (c) 2010 AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Linq;
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.Utils;

namespace ICSharpCode.NRefactory.CSharp.Resolver
{
	/// <summary>
	/// Contains logic that determines whether an implicit conversion exists between two types.
	/// </summary>
	public class Conversions : IConversions
	{
		readonly ITypeResolveContext context;
		readonly IType objectType;
		
		public Conversions(ITypeResolveContext context)
		{
			if (context == null)
				throw new ArgumentNullException("context");
			this.context = context;
			this.objectType = KnownTypeReference.Object.Resolve(context);
			this.dynamicErasure = new DynamicErasure(this);
		}
		
		#region ImplicitConversion
		public bool ImplicitConversion(ResolveResult resolveResult, IType toType)
		{
			if (resolveResult == null)
				throw new ArgumentNullException("resolveResult");
			if (resolveResult.IsCompileTimeConstant) {
				if (ImplicitEnumerationConversion(resolveResult, toType))
					return true;
				if (ImplicitConstantExpressionConversion(resolveResult, toType))
					return true;
			}
			if (ImplicitConversion(resolveResult.Type, toType))
				return true;
			// TODO: Anonymous function conversions
			// TODO: Method group conversions
			return false;
		}
		
		public bool ImplicitConversion(IType fromType, IType toType)
		{
			if (fromType == null)
				throw new ArgumentNullException("fromType");
			if (toType == null)
				throw new ArgumentNullException("toType");
			// C# 4.0 spec: §6.1
			if (IdentityConversion(fromType, toType))
				return true;
			if (ImplicitNumericConversion(fromType, toType))
				return true;
			if (ImplicitNullableConversion(fromType, toType))
				return true;
			if (NullLiteralConversion(fromType, toType))
				return true;
			if (ImplicitReferenceConversion(fromType, toType))
				return true;
			if (BoxingConversion(fromType, toType))
				return true;
			if (ImplicitDynamicConversion(fromType, toType))
				return true;
			if (ImplicitTypeParameterConversion(fromType, toType))
				return true;
			if (ImplicitPointerConversion(fromType, toType))
				return true;
			if (ImplicitUserDefinedConversion(fromType, toType))
				return true;
			return false;
		}
		
		public bool StandardImplicitConversion(IType fromType, IType toType)
		{
			if (fromType == null)
				throw new ArgumentNullException("fromType");
			if (toType == null)
				throw new ArgumentNullException("toType");
			// C# 4.0 spec: §6.3.1
			if (IdentityConversion(fromType, toType))
				return true;
			if (ImplicitNumericConversion(fromType, toType))
				return true;
			if (ImplicitNullableConversion(fromType, toType))
				return true;
			if (ImplicitReferenceConversion(fromType, toType))
				return true;
			if (ImplicitTypeParameterConversion(fromType, toType))
				return true;
			if (BoxingConversion(fromType, toType))
				return true;
			return false;
		}
		#endregion
		
		#region IdentityConversion
		/// <summary>
		/// Gets whether there is an identity conversion from <paramref name="fromType"/> to <paramref name="toType"/>
		/// </summary>
		public bool IdentityConversion(IType fromType, IType toType)
		{
			// C# 4.0 spec: §6.1.1
			return fromType.AcceptVisitor(dynamicErasure).Equals(toType.AcceptVisitor(dynamicErasure));
		}
		
		readonly DynamicErasure dynamicErasure;
		
		sealed class DynamicErasure : TypeVisitor
		{
			readonly IType objectType;
			
			public DynamicErasure(Conversions conversions)
			{
				this.objectType = conversions.objectType;
			}
			
			public override IType VisitOtherType(IType type)
			{
				if (type == SharedTypes.Dynamic)
					return objectType;
				else
					return base.VisitOtherType(type);
			}
		}
		#endregion
		
		#region ImplicitNumericConversion
		static readonly bool[,] implicitNumericConversionLookup = {
			//       to:   short  ushort  int   uint   long   ulong
			// from:
			/* char   */ { false, true , true , true , true , true  },
			/* sbyte  */ { true , false, true , false, true , false },
			/* byte   */ { true , true , true , true , true , true  },
			/* short  */ { false, false, true , false, true , false },
			/* ushort */ { false, false, true , true , true , true  },
			/* int    */ { false, false, false, false, true , false },
			/* uint   */ { false, false, false, false, true , true  },
		};
		
		bool ImplicitNumericConversion(IType fromType, IType toType)
		{
			// C# 4.0 spec: §6.1.2
			
			TypeCode from = ReflectionHelper.GetTypeCode(fromType);
			TypeCode to = ReflectionHelper.GetTypeCode(toType);
			if (to >= TypeCode.Single && to <= TypeCode.Decimal) {
				// Conversions to float/double/decimal exist from all integral types,
				// and there's a conversion from float to double.
				return from >= TypeCode.Char && from <= TypeCode.UInt64
					|| from == TypeCode.Single && to == TypeCode.Double;
			} else {
				// Conversions to integral types: look at the table
				return from >= TypeCode.Char && from <= TypeCode.UInt32
					&& to >= TypeCode.Int16 && to <= TypeCode.UInt64
					&& implicitNumericConversionLookup[from - TypeCode.Char, to - TypeCode.Int16];
			}
		}
		#endregion
		
		#region ImplicitEnumerationConversion
		bool ImplicitEnumerationConversion(ResolveResult rr, IType toType)
		{
			// C# 4.0 spec: §6.1.3
			TypeCode constantType = ReflectionHelper.GetTypeCode(rr.Type);
			if (constantType >= TypeCode.SByte && constantType <= TypeCode.Decimal && Convert.ToDouble(rr.ConstantValue) == 0) {
				return NullableType.GetUnderlyingType(toType).IsEnum();
			}
			return false;
		}
		#endregion
		
		#region ImplicitNullableConversion
		bool ImplicitNullableConversion(IType fromType, IType toType)
		{
			// C# 4.0 spec: §6.1.4
			if (NullableType.IsNullable(toType)) {
				IType t = NullableType.GetUnderlyingType(toType);
				IType s = NullableType.GetUnderlyingType(fromType); // might or might not be nullable
				return IdentityConversion(s, t) || ImplicitNumericConversion(s, t);
			} else {
				return false;
			}
		}
		#endregion
		
		#region NullLiteralConversion
		bool NullLiteralConversion(IType fromType, IType toType)
		{
			// C# 4.0 spec: §6.1.5
			return fromType == SharedTypes.Null && NullableType.IsNullable(toType);
			// This function only handles the conversion from the null literal to nullable value types,
			// reference types are handled by ImplicitReferenceConversion instead.
		}
		#endregion
		
		#region ImplicitReferenceConversion
		public bool ImplicitReferenceConversion(IType fromType, IType toType)
		{
			// C# 4.0 spec: §6.1.6
			
			// reference conversions are possible only if both types are known to be reference types
			if (!(fromType.IsReferenceType(context) == true && toType.IsReferenceType(context) == true))
				return false;
			
			// conversion from null literal is always possible
			if (fromType == SharedTypes.Null)
				return true;
			
			ArrayType fromArray = fromType as ArrayType;
			if (fromArray != null) {
				ArrayType toArray = toType as ArrayType;
				if (toArray != null) {
					// array covariance (the broken kind)
					return fromArray.Dimensions == toArray.Dimensions
						&& ImplicitReferenceConversion(fromArray.ElementType, toArray.ElementType);
				}
				// conversion from single-dimensional array S[] to IList<T>:
				ParameterizedType toPT = toType as ParameterizedType;
				if (fromArray.Dimensions == 1 && toPT != null && toPT.TypeArguments.Count == 1
				    && toPT.Namespace == "System.Collections.Generic"
				    && (toPT.Name == "IList" || toPT.Name == "ICollection" || toPT.Name == "IEnumerable"))
				{
					// array covariance plays a part here as well (string[] is IList<object>)
					return IdentityConversion(fromArray.ElementType, toPT.TypeArguments[0])
						|| ImplicitReferenceConversion(fromArray.ElementType, toPT.TypeArguments[0]);
				}
				// conversion from any array to System.Array and the interfaces it implements:
				ITypeDefinition systemArray = context.GetTypeDefinition("System", "Array", 0, StringComparer.Ordinal);
				return systemArray != null && (systemArray.Equals(toType) || ImplicitReferenceConversion(systemArray, toType));
			}
			
			// now comes the hard part: traverse the inheritance chain and figure out generics+variance
			return IsSubtypeOf(fromType, toType);
		}
		
		// Determines whether s is a subtype of t.
		// Helper method used for ImplicitReferenceConversion, BoxingConversion and ImplicitTypeParameterConversion
		bool IsSubtypeOf(IType s, IType t)
		{
			// conversion to dynamic + object are always possible
			if (t == SharedTypes.Dynamic || t.Equals(objectType))
				return true;
			
			// let GetAllBaseTypes do the work for us
			foreach (IType baseType in s.GetAllBaseTypes(context)) {
				if (IdentityOrVarianceConversion(baseType, t))
					return true;
			}
			return false;
		}
		
		bool IdentityOrVarianceConversion(IType s, IType t)
		{
			ITypeDefinition def = s.GetDefinition();
			if (def != null && def.Equals(t.GetDefinition())) {
				ParameterizedType ps = s as ParameterizedType;
				ParameterizedType pt = t as ParameterizedType;
				if (ps != null && pt != null
				    && ps.TypeArguments.Count == pt.TypeArguments.Count
				    && ps.TypeArguments.Count == def.TypeParameters.Count)
				{
					// C# 4.0 spec: §13.1.3.2 Variance Conversion
					for (int i = 0; i < def.TypeParameters.Count; i++) {
						IType si = ps.TypeArguments[i];
						IType ti = pt.TypeArguments[i];
						if (IdentityConversion(si, ti))
							continue;
						ITypeParameter xi = def.TypeParameters[i];
						switch (xi.Variance) {
							case VarianceModifier.Covariant:
								if (!ImplicitReferenceConversion(si, ti))
									return false;
								break;
							case VarianceModifier.Contravariant:
								if (!ImplicitReferenceConversion(ti, si))
									return false;
								break;
							default:
								return false;
						}
					}
				} else if (ps != null || pt != null) {
					return false; // only of of them is parameterized, or counts don't match? -> not valid conversion
				}
				return true;
			}
			return false;
		}
		#endregion
		
		#region BoxingConversion
		bool BoxingConversion(IType fromType, IType toType)
		{
			// C# 4.0 spec: §6.1.7
			fromType = NullableType.GetUnderlyingType(fromType);
			return fromType.IsReferenceType(context) == false && toType.IsReferenceType(context) == true && IsSubtypeOf(fromType, toType);
		}
		#endregion
		
		#region ImplicitDynamicConversion
		bool ImplicitDynamicConversion(IType fromType, IType toType)
		{
			// C# 4.0 spec: §6.1.8
			return fromType == SharedTypes.Dynamic;
		}
		#endregion
		
		#region ImplicitConstantExpressionConversion
		bool ImplicitConstantExpressionConversion(ResolveResult rr, IType toType)
		{
			// C# 4.0 spec: §6.1.9
			TypeCode fromTypeCode = ReflectionHelper.GetTypeCode(rr.Type);
			TypeCode toTypeCode = ReflectionHelper.GetTypeCode(NullableType.GetUnderlyingType(toType));
			if (fromTypeCode == TypeCode.Int64) {
				long val = (long)rr.ConstantValue;
				return val >= 0 && toTypeCode == TypeCode.UInt64;
			} else if (fromTypeCode == TypeCode.Int32) {
				int val = (int)rr.ConstantValue;
				switch (toTypeCode) {
					case TypeCode.SByte:
						return val >= SByte.MinValue && val <= SByte.MaxValue;
					case TypeCode.Byte:
						return val >= Byte.MinValue && val <= Byte.MaxValue;
					case TypeCode.Int16:
						return val >= Int16.MinValue && val <= Int16.MaxValue;
					case TypeCode.UInt16:
						return val >= UInt16.MinValue && val <= UInt16.MaxValue;
					case TypeCode.UInt32:
						return val >= 0;
					case TypeCode.UInt64:
						return val >= 0;
				}
			}
			return false;
		}
		#endregion
		
		#region ImplicitTypeParameterConversion
		/// <summary>
		/// Implicit conversions involving type parameters.
		/// </summary>
		bool ImplicitTypeParameterConversion(IType fromType, IType toType)
		{
			ITypeParameter t = fromType as ITypeParameter;
			if (t == null)
				return false; // not a type parameter
			if (t.IsReferenceType(context) == true)
				return false; // already handled by ImplicitReferenceConversion
			return IsSubtypeOf(t, toType);
		}
		#endregion
		
		#region ImplicitPointerConversion
		bool ImplicitPointerConversion(IType fromType, IType toType)
		{
			// C# 4.0 spec: §18.4 Pointer conversions
			if (fromType is PointerType && toType is PointerType && toType.ReflectionName == "System.Void*")
				return true;
			if (fromType == SharedTypes.Null && toType is PointerType)
				return true;
			return false;
		}
		#endregion
		
		#region ImplicitUserDefinedConversion
		bool ImplicitUserDefinedConversion(IType fromType, IType toType)
		{
			// C# 4.0 spec §6.4.4 User-defined implicit conversions
			// Currently we only test whether an applicable implicit conversion exists,
			// we do not resolve which conversion is the most specific and gets used.
			
			// Find the candidate operators:
			Predicate<IMethod> opImplicitFilter = m => m.IsStatic && m.IsOperator && m.Name == "op_Implicit" && m.Parameters.Count == 1;
			var operators = NullableType.GetUnderlyingType(fromType).GetMethods(context, opImplicitFilter)
				.Concat(NullableType.GetUnderlyingType(toType).GetMethods(context, opImplicitFilter));
			// Determine whether one of them is applicable:
			foreach (IMethod op in operators) {
				IType sourceType = op.Parameters[0].Type.Resolve(context);
				IType targetType = op.ReturnType.Resolve(context);
				// Try if the operator is applicable:
				if (StandardImplicitConversion(fromType, sourceType) && StandardImplicitConversion(targetType, toType)) {
					return true;
				}
				// Try if the operator is applicable in lifted form:
				if (sourceType.IsReferenceType(context) == false && targetType.IsReferenceType(context) == false) {
					IType liftedSourceType = NullableType.Create(sourceType, context);
					IType liftedTargetType = NullableType.Create(targetType, context);
					if (StandardImplicitConversion(fromType, liftedSourceType) && StandardImplicitConversion(liftedTargetType, toType)) {
						return true;
					}
				}
			}
			return false;
		}
		#endregion
		
		#region BetterConversion
		/// <summary>
		/// Gets the better conversion (C# 4.0 spec, §7.5.3.3)
		/// </summary>
		/// <returns>0 = neither is better; 1 = t1 is better; 2 = t2 is better</returns>
		public int BetterConversion(ResolveResult resolveResult, IType t1, IType t2)
		{
			// TODO: implement the special logic for anonymous functions
			return BetterConversion(resolveResult.Type, t1, t2);
		}
		
		/// <summary>
		/// Gets the better conversion (C# 4.0 spec, §7.5.3.4)
		/// </summary>
		/// <returns>0 = neither is better; 1 = t1 is better; 2 = t2 is better</returns>
		public int BetterConversion(IType s, IType t1, IType t2)
		{
			bool ident1 = IdentityConversion(s, t1);
			bool ident2 = IdentityConversion(s, t2);
			if (ident1 && !ident2)
				return 1;
			if (ident2 && !ident1)
				return 2;
			return BetterConversionTarget(t1, t2);
		}
		
		/// <summary>
		/// Gets the better conversion target (C# 4.0 spec, §7.5.3.5)
		/// </summary>
		/// <returns>0 = neither is better; 1 = t1 is better; 2 = t2 is better</returns>
		int BetterConversionTarget(IType t1, IType t2)
		{
			bool t1To2 = ImplicitConversion(t1, t2);
			bool t2To1 = ImplicitConversion(t2, t1);
			if (t1To2 && !t2To1)
				return 1;
			if (t2To1 && !t1To2)
				return 2;
			TypeCode t1Code = ReflectionHelper.GetTypeCode(t1);
			TypeCode t2Code = ReflectionHelper.GetTypeCode(t2);
			if (IsBetterIntegralType(t1Code, t2Code))
				return 1;
			if (IsBetterIntegralType(t2Code, t1Code))
				return 2;
			return 0;
		}
		
		bool IsBetterIntegralType(TypeCode t1, TypeCode t2)
		{
			// signed types are better than unsigned types
			switch (t1) {
				case TypeCode.SByte:
					return t2 == TypeCode.Byte || t2 == TypeCode.UInt16 || t2 == TypeCode.UInt32 || t2 == TypeCode.UInt64;
				case TypeCode.Int16:
					return t2 == TypeCode.UInt16 || t2 == TypeCode.UInt32 || t2 == TypeCode.UInt64;
				case TypeCode.Int32:
					return t2 == TypeCode.UInt32 || t2 == TypeCode.UInt64;
				case TypeCode.Int64:
					return t2 == TypeCode.UInt64;
				default:
					return false;
			}
		}
		#endregion
	}
}
