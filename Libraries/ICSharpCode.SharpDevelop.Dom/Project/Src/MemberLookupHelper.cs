// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace ICSharpCode.SharpDevelop.Dom
{
	/// <summary>
	/// Class with methods to help finding the correct overload for a member.
	/// </summary>
	/// <remarks>
	/// This class does member lookup as specified by the C# spec (ECMA-334, § 14.3).
	/// Other languages might need custom lookup methods.
	/// </remarks>
	public static class MemberLookupHelper
	{
		#region LookupMember / GetAccessibleMembers
		public static List<IMember> GetAllMembers(IReturnType rt)
		{
			List<IMember> members = new List<IMember>();
			if (rt != null) {
				rt.GetMethods().ForEach(members.Add);
				rt.GetProperties().ForEach(members.Add);
				rt.GetFields().ForEach(members.Add);
				rt.GetEvents().ForEach(members.Add);
			}
			return members;
		}
		
		public static List<IList<IMember>> LookupMember(
			IReturnType type, string name, IClass callingClass,
			LanguageProperties language, bool isInvocation, bool? isAccessThoughReferenceOfCurrentClass)
		{
			if (language == null)
				throw new ArgumentNullException("language");
			
			if (isAccessThoughReferenceOfCurrentClass == null) {
				isAccessThoughReferenceOfCurrentClass = false;
				IClass underlyingClass = type.GetUnderlyingClass();
				if (underlyingClass != null)
					isAccessThoughReferenceOfCurrentClass = underlyingClass.IsTypeInInheritanceTree(callingClass);
			}
			
			IEnumerable<IMember> members;
			if (language == LanguageProperties.VBNet && language.NameComparer.Equals(name, "New")) {
				members = GetAllMembers(type).OfType<IMethod>().Where(m => m.IsConstructor).Select(m=>(IMember)m);
			} else {
				members = GetAllMembers(type).Where(m => language.NameComparer.Equals(m.Name, name));
			}
			
			return LookupMember(members, callingClass, (bool)isAccessThoughReferenceOfCurrentClass, isInvocation);
		}
		
		sealed class InheritanceLevelComparer : IComparer<IClass>
		{
			public readonly static InheritanceLevelComparer Instance = new InheritanceLevelComparer();
			
			public int Compare(IClass x, IClass y)
			{
				if (x == y)
					return 0;
				if (x.IsTypeInInheritanceTree(y))
					return 1;
				else
					return -1;
			}
		}
		
		public static List<IList<IMember>> LookupMember(
			IEnumerable<IMember> possibleMembers, IClass callingClass,
			bool isAccessThoughReferenceOfCurrentClass, bool isInvocation)
		{
//			Console.WriteLine("Possible members:");
//			foreach (IMember m in possibleMembers) {
//				Console.WriteLine("  " + m.DotNetName);
//			}
			
			IEnumerable<IMember> accessibleMembers = possibleMembers.Where(member => member.IsAccessible(callingClass, isAccessThoughReferenceOfCurrentClass));
			if (isInvocation) {
				accessibleMembers = accessibleMembers.Where(IsInvocable);
			}
			
			// base most member => most derived member
			//Dictionary<IMember, IMember> overrideDict = new Dictionary<IMember, IMember>();
			
			ParameterListComparer parameterListComparer = new ParameterListComparer();
			HashSet<IMethod> handledMethods = new HashSet<IMethod>(parameterListComparer);
			Dictionary<IMethod, IMethod> overrideMethodDict = new Dictionary<IMethod, IMethod>(parameterListComparer);
			IMember nonMethodOverride = null;
			
			List<IList<IMember>> allResults = new List<IList<IMember>>();
			List<IMember> results = new List<IMember>();
			
			foreach (var group in accessibleMembers
			         .GroupBy(m => m.DeclaringType.GetCompoundClass())
			         .OrderByDescending(g => g.Key, InheritanceLevelComparer.Instance))
			{
				//Console.WriteLine("Member group " + group.Key);
				foreach (IMember m in group) {
					//Console.WriteLine("  " + m.DotNetName);
					if (m.IsOverride) {
						IMethod method = m as IMethod;
						if (method != null) {
							if (!overrideMethodDict.ContainsKey(method))
								overrideMethodDict[method] = method;
						} else {
							if (nonMethodOverride == null)
								nonMethodOverride = m;
						}
					} else {
						IMethod method = m as IMethod;
						if (method != null) {
							if (handledMethods.Add(method)) {
								IMethod mostOverriddenMethod;
								if (overrideMethodDict.TryGetValue(method, out mostOverriddenMethod))
									results.Add(mostOverriddenMethod);
								else {
									results.Add(method);
								}
							}
						} else {
							// non-methods are only available if they aren't hidden by something else
							if (allResults.Count == 0) {
								results.Add(nonMethodOverride ?? m);
							}
						}
					}
				}
				if (results.Count > 0) {
					allResults.Add(results);
					results = new List<IMember>();
				}
			}
			// Sometimes there might be 'override's without corresponding 'virtual's.
			// Ensure those get found, too.
			if (nonMethodOverride != null && allResults.Count == 0) {
				results.Add(nonMethodOverride);
			}
			foreach (IMethod method in overrideMethodDict.Values) {
				if (handledMethods.Add(method)) {
					results.Add(method);
				}
			}
			if (results.Count > 0) {
				allResults.Add(results);
			}
			return allResults;
		}
		
		static bool IsInvocable(IMember member)
		{
			if (member == null)
				throw new ArgumentNullException("member");
			if (member is IMethod || member is IEvent)
				return true;
			IProperty p = member as IProperty;
			if (p != null && p.Parameters.Count > 0)
				return true;
			IReturnType returnType = member.ReturnType;
			if (returnType == null)
				return false;
			IClass c = returnType.GetUnderlyingClass();
			return c != null && c.ClassType == ClassType.Delegate;
		}
		
		/// <summary>
		/// Gets all accessible members, including indexers and constructors.
		/// </summary>
		public static List<IMember> GetAccessibleMembers(IReturnType rt, IClass callingClass, LanguageProperties language)
		{
			bool isAccessThoughReferenceOfCurrentClass = false;
			IClass underlyingClass = rt.GetUnderlyingClass();
			if (underlyingClass != null)
				isAccessThoughReferenceOfCurrentClass = underlyingClass.IsTypeInInheritanceTree(callingClass);
			return GetAccessibleMembers(rt, callingClass, language, isAccessThoughReferenceOfCurrentClass);
		}
		
		/// <summary>
		/// Gets all accessible members, including indexers and constructors.
		/// </summary>
		public static List<IMember> GetAccessibleMembers(IReturnType rt, IClass callingClass, LanguageProperties language, bool isAccessThoughReferenceOfCurrentClass)
		{
			if (language == null)
				throw new ArgumentNullException("language");
			
			List<IMember> result = new List<IMember>();
			foreach (var g in GetAllMembers(rt).GroupBy(m => m.Name, language.NameComparer).OrderBy(g2=>g2.Key)) {
				foreach (var group in LookupMember(g, callingClass, isAccessThoughReferenceOfCurrentClass, false)) {
					result.AddRange(group);
				}
			}
			return result;
		}
		#endregion
		
		#region FindOverload
		/// <summary>
		/// Finds the correct overload according to the C# specification.
		/// </summary>
		/// <param name="methods">List with the methods to check.</param>
		/// <param name="arguments">The types of the arguments passed to the method.</param>
		/// <param name="resultIsAcceptable">Out parameter. Will be true if the resulting method
		/// is an acceptable match, false if the resulting method is just a guess and will lead
		/// to a compile error.</param>
		/// <returns>The method that will be called.</returns>
		public static IMethod FindOverload(IList<IMethod> methods, IReturnType[] arguments, out bool resultIsAcceptable)
		{
			if (methods == null)
				throw new ArgumentNullException("methods");
			resultIsAcceptable = false;
			if (methods.Count == 0)
				return null;
			return (IMethod)CSharp.OverloadResolution.FindOverload(
				methods,
				arguments,
				false,
				true,
				out resultIsAcceptable);
		}
		
		public static IProperty FindOverload(IList<IProperty> properties, IReturnType[] arguments)
		{
			if (properties.Count == 0)
				return null;
			bool acceptableMatch;
			return (IProperty)CSharp.OverloadResolution.FindOverload(
				properties,
				arguments,
				false,
				false,
				out acceptableMatch);
		}
		#endregion
		
		#region Type Argument Inference
		/// <summary>
		/// Infers type arguments specified by passing expectedArgument as parameter where passedArgument
		/// was expected. The resulting type arguments are written to outputArray.
		/// Returns false when expectedArgument and passedArgument are incompatible, otherwise true
		/// is returned (true is used both for successful inferring and other kind of errors).
		/// 
		/// Warning: This method for single-argument type inference doesn't support lambdas!
		/// </summary>
		/// <remarks>
		/// The C# spec (§ 25.6.4) has a bug: it says that type inference works if the passedArgument is IEnumerable{T}
		/// and the expectedArgument is an array; passedArgument and expectedArgument must be swapped here.
		/// </remarks>
		public static bool InferTypeArgument(IReturnType expectedArgument, IReturnType passedArgument, IReturnType[] outputArray)
		{
			if (expectedArgument == null) return true;
			if (passedArgument == null || passedArgument == NullReturnType.Instance) return true;
			
			if (passedArgument.IsArrayReturnType) {
				IReturnType passedArrayElementType = passedArgument.CastToArrayReturnType().ArrayElementType;
				if (expectedArgument.IsArrayReturnType && expectedArgument.CastToArrayReturnType().ArrayDimensions == passedArgument.CastToArrayReturnType().ArrayDimensions) {
					return InferTypeArgument(expectedArgument.CastToArrayReturnType().ArrayElementType, passedArrayElementType, outputArray);
				} else if (expectedArgument.IsConstructedReturnType) {
					switch (expectedArgument.FullyQualifiedName) {
						case "System.Collections.Generic.IList":
						case "System.Collections.Generic.ICollection":
						case "System.Collections.Generic.IEnumerable":
							return InferTypeArgument(expectedArgument.CastToConstructedReturnType().TypeArguments[0], passedArrayElementType, outputArray);
					}
				}
				// If P is an array type, and A is not an array type of the same rank,
				// or an instantiation of IList<>, ICollection<>, or IEnumerable<>, then
				// type inference fails for the generic method.
				return false;
			}
			if (expectedArgument.IsGenericReturnType) {
				GenericReturnType methodTP = expectedArgument.CastToGenericReturnType();
				if (methodTP.TypeParameter.Method != null) {
					if (methodTP.TypeParameter.Index < outputArray.Length) {
						outputArray[methodTP.TypeParameter.Index] = passedArgument;
					}
					return true;
				}
			}
			if (expectedArgument.IsConstructedReturnType) {
				// The spec for this case is quite complex.
				// For our purposes, we can simplify enourmously:
				if (!passedArgument.IsConstructedReturnType) return false;
				
				IList<IReturnType> expectedTA = expectedArgument.CastToConstructedReturnType().TypeArguments;
				IList<IReturnType> passedTA   = passedArgument.CastToConstructedReturnType().TypeArguments;
				
				int count = Math.Min(expectedTA.Count, passedTA.Count);
				for (int i = 0; i < count; i++) {
					InferTypeArgument(expectedTA[i], passedTA[i], outputArray);
				}
			}
			return true;
		}
		#endregion
		
		#region IsApplicable
		public static bool IsApplicable(IReturnType argument, IParameter expected, IMethod targetMethod)
		{
			bool parameterIsRefOrOut = expected.IsRef || expected.IsOut;
			bool argumentIsRefOrOut = argument != null && argument.IsDecoratingReturnType<ReferenceReturnType>();
			if (parameterIsRefOrOut != argumentIsRefOrOut)
				return false;
			if (parameterIsRefOrOut) {
				return object.Equals(argument, expected.ReturnType);
			} else {
				return IsApplicable(argument, expected.ReturnType, targetMethod);
			}
		}
		
		/// <summary>
		/// Tests whether an argument of type "argument" is valid for a parameter of type "expected" for a call
		/// to "targetMethod".
		/// targetMethod may be null, it is only used when it is a generic method and expected is (or contains) one of
		/// its type parameters.
		/// </summary>
		public static bool IsApplicable(IReturnType argument, IReturnType expected, IMethod targetMethod)
		{
			return ConversionExistsInternal(argument, expected, targetMethod);
		}
		#endregion
		
		#region Conversion exists
		/// <summary>
		/// Checks if an implicit conversion exists from <paramref name="from"/> to <paramref name="to"/>.
		/// </summary>
		public static bool ConversionExists(IReturnType from, IReturnType to)
		{
			return ConversionExistsInternal(from, to, null);
		}
		
		/// <summary>
		/// Tests if an implicit conversion exists from "from" to "to".
		/// Conversions from concrete types to generic types are only allowed when the generic type belongs to the
		/// method "allowGenericTargetsOnThisMethod".
		/// </summary>
		static bool ConversionExistsInternal(IReturnType from, IReturnType to, IMethod allowGenericTargetsOnThisMethod)
		{
			// ECMA-334, § 13.1 Implicit conversions
			
			// Identity conversion:
			if (from == to) return true;
			if (from == null || to == null) return false;
			if (from.Equals(to)) {
				return true;
			}
			
			bool fromIsDefault = from.IsDefaultReturnType;
			bool toIsDefault = to.IsDefaultReturnType;
			
			if (fromIsDefault && toIsDefault) {
				// Implicit numeric conversions:
				int f = GetPrimitiveType(from);
				int t = GetPrimitiveType(to);
				if (f == SByte && (t == Short || t == Int || t == Long || t == Float || t == Double || t == Decimal))
					return true;
				if (f == Byte && (t == Short || t == UShort || t == Int || t == UInt || t == Long || t == ULong || t == Float || t == Double || t == Decimal))
					return true;
				if (f == Short && (t == Int || t == Long || t == Float || t == Double || t == Decimal))
					return true;
				if (f == UShort && (t == Int || t == UInt || t == Long || t == ULong || t == Float || t == Double || t == Decimal))
					return true;
				if (f == Int && (t == Long || t == Float || t == Double || t == Decimal))
					return true;
				if (f == UInt && (t == Long || t == ULong || t == Float || t == Double || t == Decimal))
					return true;
				if ((f == Long || f == ULong) && (t == Float || t == Double || t == Decimal))
					return true;
				if (f == Char && (t == UShort || t == Int || t == UInt || t == Long || t == ULong || t == Float || t == Double || t == Decimal))
					return true;
				if (f == Float && t == Double)
					return true;
			}
			// Implicit reference conversions:
			
			if (toIsDefault && to.FullyQualifiedName == "System.Object") {
				return true; // from any type to object
			}
			if (from == NullReturnType.Instance) {
				IClass toClass = to.GetUnderlyingClass();
				if (toClass != null) {
					switch (toClass.ClassType) {
						case ClassType.Class:
						case ClassType.Delegate:
						case ClassType.Interface:
							return true;
						case ClassType.Struct:
							return toClass.FullyQualifiedName == "System.Nullable";
					}
				}
				return false;
			}
			
			if ((toIsDefault || to.IsConstructedReturnType || to.IsGenericReturnType)
			    && (fromIsDefault || from.IsArrayReturnType || from.IsConstructedReturnType))
			{
				foreach (IReturnType baseTypeOfFrom in GetTypeInheritanceTree(from)) {
					if (IsConstructedConversionToGenericReturnType(baseTypeOfFrom, to, allowGenericTargetsOnThisMethod))
						return true;
				}
			}
			
			if (from.IsArrayReturnType && to.IsArrayReturnType) {
				ArrayReturnType fromArt = from.CastToArrayReturnType();
				ArrayReturnType toArt   = to.CastToArrayReturnType();
				// from array to other array type
				if (fromArt.ArrayDimensions == toArt.ArrayDimensions) {
					return ConversionExistsInternal(fromArt.ArrayElementType, toArt.ArrayElementType, allowGenericTargetsOnThisMethod);
				}
			}
			
			if (from.IsDecoratingReturnType<AnonymousMethodReturnType>() && (toIsDefault || to.IsConstructedReturnType)) {
				AnonymousMethodReturnType amrt = from.CastToDecoratingReturnType<AnonymousMethodReturnType>();
				IMethod method = CSharp.TypeInference.GetDelegateOrExpressionTreeSignature(to, amrt.CanBeConvertedToExpressionTree);
				if (method != null) {
					if (amrt.HasParameterList) {
						if (amrt.MethodParameters.Count != method.Parameters.Count)
							return false;
						for (int i = 0; i < amrt.MethodParameters.Count; i++) {
							if (amrt.MethodParameters[i].ReturnType != null) {
								if (!object.Equals(amrt.MethodParameters[i].ReturnType,
								                   method.Parameters[i].ReturnType))
								{
									return false;
								}
							}
						}
					}
					IReturnType rt = amrt.ResolveReturnType(method.Parameters.Select(p => p.ReturnType).ToArray());
					return ConversionExistsInternal(rt, method.ReturnType, allowGenericTargetsOnThisMethod);
				}
			}
			
			return false;
		}
		
		static bool IsConstructedConversionToGenericReturnType(IReturnType from, IReturnType to, IMethod allowGenericTargetsOnThisMethod)
		{
			// null could be passed when type arguments could not be resolved/inferred
			if (from == to) // both are null or
				return true;
			if (from == null || to == null)
				return false;
			
			if (from.Equals(to))
				return true;
			
			if (allowGenericTargetsOnThisMethod == null)
				return false;
			
			if (to.IsGenericReturnType) {
				ITypeParameter typeParameter = to.CastToGenericReturnType().TypeParameter;
				if (typeParameter.Method == allowGenericTargetsOnThisMethod)
					return true;
				// applicability ignores constraints
//				foreach (IReturnType constraintType in typeParameter.Constraints) {
//					if (!ConversionExistsInternal(from, constraintType, allowGenericTargetsOnThisMethod)) {
//						return false;
//					}
//				}
				return false;
			}
			
			// for conversions like from IEnumerable<string> to IEnumerable<T>, where T is a GenericReturnType
			ConstructedReturnType cFrom = from.CastToConstructedReturnType();
			ConstructedReturnType cTo   = to.CastToConstructedReturnType();
			if (cFrom != null && cTo != null) {
				if (cFrom.FullyQualifiedName == cTo.FullyQualifiedName && cFrom.TypeArguments.Count == cTo.TypeArguments.Count) {
					for (int i = 0; i < cFrom.TypeArguments.Count; i++) {
						if (!IsConstructedConversionToGenericReturnType(cFrom.TypeArguments[i], cTo.TypeArguments[i], allowGenericTargetsOnThisMethod))
							return false;
					}
					return true;
				}
			}
			return false;
		}
		#endregion
		
		#region Better conversion
		/// <summary>
		/// Gets if the conversion from <paramref name="from"/> to <paramref name="to1"/> is better than
		/// the conversion from <paramref name="from"/> to <paramref name="to2"/>.
		/// </summary>
		/// <returns>
		/// 0 = neither conversion is better<br/>
		/// 1 = from -> to1 is the better conversion<br/>
		/// 2 = from -> to2 is the better conversion.
		/// </returns>
		public static int GetBetterConversion(IReturnType from, IReturnType to1, IReturnType to2)
		{
			if (from == null) return 0;
			if (to1 == null) return 2;
			if (to2 == null) return 1;
			
			// See ECMA-334, § 14.4.2.3
			
			// If T1 and T2 are the same type, neither conversion is better.
			if (to1.Equals(to2)) {
				return 0;
			}
			// If S is T1, C1 is the better conversion.
			if (from.Equals(to1)) {
				return 1;
			}
			// If S is T2, C2 is the better conversion.
			if (from.Equals(to2)) {
				return 2;
			}
			bool canConvertFrom1To2 = ConversionExists(to1, to2);
			bool canConvertFrom2To1 = ConversionExists(to2, to1);
			// If an implicit conversion from T1 to T2 exists, and no implicit conversion
			// from T2 to T1 exists, C1 is the better conversion.
			if (canConvertFrom1To2 && !canConvertFrom2To1) {
				return 1;
			}
			// If an implicit conversion from T2 to T1 exists, and no implicit conversion
			// from T1 to T2 exists, C2 is the better conversion.
			if (canConvertFrom2To1 && !canConvertFrom1To2) {
				return 2;
			}
			if (to1.IsDefaultReturnType && to2.IsDefaultReturnType) {
				return GetBetterPrimitiveConversion(to1, to2);
			}
			// Otherwise, neither conversion is better.
			return 0;
		}
		
		const int Byte   = 1;
		const int Short  = 2;
		const int Int    = 3;
		const int Long   = 4;
		const int SByte  = 5;
		const int UShort = 6;
		const int UInt   = 7;
		const int ULong  = 8;
		const int Float  = 9;
		const int Double = 10;
		const int Char   = 11;
		const int Decimal= 12;
		
		static int GetBetterPrimitiveConversion(IReturnType to1, IReturnType to2)
		{
			int t1 = GetPrimitiveType(to1);
			int t2 = GetPrimitiveType(to2);
			if (t1 == 0 || t2 == 0) return 0; // not primitive
			if (t1 == SByte && (t2 == Byte || t2 == UShort || t2 == UInt || t2 == ULong))
				return 1;
			if (t2 == SByte && (t1 == Byte || t1 == UShort || t1 == UInt || t1 == ULong))
				return 2;
			if (t1 == Short && (t2 == UShort || t2 == UInt || t2 == ULong))
				return 1;
			if (t2 == Short && (t1 == UShort || t1 == UInt || t1 == ULong))
				return 2;
			if (t1 == Int && (t2 == UInt || t2 == ULong))
				return 1;
			if (t2 == Int && (t1 == UInt || t1 == ULong))
				return 2;
			if (t1 == Long && t2 == ULong)
				return 1;
			if (t2 == Long && t1 == ULong)
				return 2;
			return 0;
		}
		
		static int GetPrimitiveType(IReturnType t)
		{
			switch (t.FullyQualifiedName) {
					case "System.SByte": return SByte;
					case "System.Byte": return Byte;
					case "System.Int16": return Short;
					case "System.UInt16": return UShort;
					case "System.Int32": return Int;
					case "System.UInt32": return UInt;
					case "System.Int64": return Long;
					case "System.UInt64": return ULong;
					case "System.Single": return Float;
					case "System.Double": return Double;
					case "System.Char": return Char;
					case "System.Decimal": return Decimal;
					default: return 0;
			}
		}
		#endregion
		
		#region GetCommonType
		/// <summary>
		/// Gets the common base type of a and b.
		/// </summary>
		public static IReturnType GetCommonType(IProjectContent projectContent, IReturnType a, IReturnType b)
		{
			if (projectContent == null)
				throw new ArgumentNullException("projectContent");
			if (a == null) return b;
			if (b == null) return a;
			if (ConversionExists(a, b))
				return b;
			//if (ConversionExists(b, a)) - not required because the first baseTypeOfA is a
			//	return a;
			foreach (IReturnType baseTypeOfA in GetTypeInheritanceTree(a)) {
				if (ConversionExists(b, baseTypeOfA))
					return baseTypeOfA;
			}
			return projectContent.SystemTypes.Object;
		}
		#endregion
		
		#region GetTypeParameterPassedToBaseClass / GetTypeInheritanceTree
		/// <summary>
		/// Gets the type parameter that was passed to a certain base class.
		/// For example, when <paramref name="returnType"/> is Dictionary(of string, int)
		/// this method will return KeyValuePair(of string, int)
		/// </summary>
		public static IReturnType GetTypeParameterPassedToBaseClass(IReturnType parentType, IClass baseClass, int baseClassTypeParameterIndex)
		{
			foreach (IReturnType rt in GetTypeInheritanceTree(parentType)) {
				ConstructedReturnType crt = rt.CastToConstructedReturnType();
				if (crt != null && baseClass.CompareTo(rt.GetUnderlyingClass()) == 0) {
					if (baseClassTypeParameterIndex < crt.TypeArguments.Count) {
						return crt.TypeArguments[baseClassTypeParameterIndex];
					}
				}
			}
			return null;
		}
		
		/// <summary>
		/// Translates typeToTranslate using the type arguments from parentType;
		/// </summary>
		static IReturnType TranslateIfRequired(IReturnType parentType, IReturnType typeToTranslate)
		{
			if (typeToTranslate == null)
				return null;
			ConstructedReturnType parentConstructedType = parentType.CastToConstructedReturnType();
			if (parentConstructedType != null) {
				return ConstructedReturnType.TranslateType(typeToTranslate, parentConstructedType.TypeArguments, false);
			} else {
				return typeToTranslate;
			}
		}
		
		readonly static Dictionary<IReturnType, IEnumerable<IReturnType>> getTypeInheritanceTreeCache = new Dictionary<IReturnType, IEnumerable<IReturnType>>();
		
		static void ClearGetTypeInheritanceTreeCache()
		{
			lock (getTypeInheritanceTreeCache) {
				getTypeInheritanceTreeCache.Clear();
			}
		}
		
		/// <summary>
		/// Gets all types the specified type inherits from (all classes and interfaces).
		/// Unlike the class inheritance tree, this method takes care of type arguments and calculates the type
		/// arguments that are passed to base classes.
		/// </summary>
		public static IEnumerable<IReturnType> GetTypeInheritanceTree(IReturnType typeToListInheritanceTreeFor)
		{
			if (typeToListInheritanceTreeFor == null)
				throw new ArgumentNullException("typeToListInheritanceTreeFor");
			
			lock (getTypeInheritanceTreeCache) {
				IEnumerable<IReturnType> result;
				if (getTypeInheritanceTreeCache.TryGetValue(typeToListInheritanceTreeFor, out result))
					return result;
			}
			
			IClass classToListInheritanceTreeFor = typeToListInheritanceTreeFor.GetUnderlyingClass();
			if (classToListInheritanceTreeFor == null)
				return new IReturnType[] { typeToListInheritanceTreeFor };
			
			if (typeToListInheritanceTreeFor.IsArrayReturnType) {
				IReturnType elementType = typeToListInheritanceTreeFor.CastToArrayReturnType().ArrayElementType;
				List<IReturnType> resultList = new List<IReturnType>();
				resultList.Add(typeToListInheritanceTreeFor);
				resultList.AddRange(GetTypeInheritanceTree(
					new ConstructedReturnType(
						classToListInheritanceTreeFor.ProjectContent.GetClass("System.Collections.Generic.IList", 1).DefaultReturnType,
						new IReturnType[] { elementType }
					)
				));
				resultList.Add(classToListInheritanceTreeFor.ProjectContent.GetClass("System.Collections.IList", 0).DefaultReturnType);
				resultList.Add(classToListInheritanceTreeFor.ProjectContent.GetClass("System.Collections.ICollection", 0).DefaultReturnType);
				// non-generic IEnumerable is already added by generic IEnumerable
				return resultList;
			}
			
			HashSet<IReturnType> visitedSet = new HashSet<IReturnType>();
			List<IReturnType> visitedList = new List<IReturnType>();
			Queue<IReturnType> typesToVisit = new Queue<IReturnType>();
			bool enqueuedLastBaseType = false;
			
			IReturnType currentType = typeToListInheritanceTreeFor;
			IClass currentClass = classToListInheritanceTreeFor;
			IReturnType nextType;
			do {
				if (currentClass != null) {
					if (visitedSet.Add(currentType)) {
						visitedList.Add(currentType);
						foreach (IReturnType type in currentClass.BaseTypes) {
							typesToVisit.Enqueue(TranslateIfRequired(currentType, type));
						}
					}
				}
				if (typesToVisit.Count > 0) {
					nextType = typesToVisit.Dequeue();
				} else {
					nextType = enqueuedLastBaseType ? null : DefaultClass.GetBaseTypeByClassType(classToListInheritanceTreeFor);
					enqueuedLastBaseType = true;
				}
				if (nextType != null) {
					currentType = nextType;
					currentClass = nextType.GetUnderlyingClass();
				}
			} while (nextType != null);
			lock (getTypeInheritanceTreeCache) {
				if (getTypeInheritanceTreeCache.Count == 0) {
					DomCache.RegisterForClear(ClearGetTypeInheritanceTreeCache);
				}
				getTypeInheritanceTreeCache[typeToListInheritanceTreeFor] = visitedList;
			}
			return visitedList;
		}
		#endregion
		
		#region IsSimilarMember / FindBaseMember
		/// <summary>
		/// Gets if member1 is the same as member2 or if member1 overrides member2.
		/// </summary>
		public static bool IsSimilarMember(IMember member1, IMember member2)
		{
			member1 = GetGenericMember(member1);
			member2 = GetGenericMember(member2);
			do {
				if (IsSimilarMemberInternal(member1, member2))
					return true;
			} while ((member1 = FindBaseMember(member1)) != null);
			return false;
		}
		
		/// <summary>
		/// Gets the generic member from a specialized member.
		/// Specialized members are the result of overload resolution with type substitution.
		/// </summary>
		static IMember GetGenericMember(IMember member)
		{
			// e.g. member = string[] ToArray<string>(IEnumerable<string> input)
			// result = T[] ToArray<T>(IEnumerable<T> input)
			if (member != null) {
				while (member.GenericMember != null)
					member = member.GenericMember;
			}
			return member;
		}
		
		static bool IsSimilarMemberInternal(IMember member1, IMember member2)
		{
			if (member1 == member2)
				return true;
			if (member1 == null || member2 == null)
				return false;
			if (member1.FullyQualifiedName != member2.FullyQualifiedName)
				return false;
			if (member1.IsStatic != member2.IsStatic)
				return false;
			IMethodOrProperty m1 = member1 as IMethodOrProperty;
			IMethodOrProperty m2 = member2 as IMethodOrProperty;
			if (m1 != null || m2 != null) {
				if (m1 != null && m2 != null) {
					if (DiffUtility.Compare(m1.Parameters, m2.Parameters) != 0)
						return false;
					if (m1 is IMethod && m2 is IMethod) {
						if ((m1 as IMethod).TypeParameters.Count != (m2 as IMethod).TypeParameters.Count)
							return false;
					}
				} else {
					return false;
				}
			}
			IField f1 = member1 as IField;
			IField f2 = member2 as IField;
			if (f1 != null || f2 != null) {
				if (f1 != null && f2 != null) {
					if (f1.IsLocalVariable != f2.IsLocalVariable || f1.IsParameter != f2.IsParameter)
						return false;
				} else {
					return false;
				}
			}
			return true;
		}
		
		public static IMember FindSimilarMember(IClass type, IMember member)
		{
			if (type == null)
				throw new ArgumentNullException("type");
			StringComparer nameComparer = member.DeclaringType.ProjectContent.Language.NameComparer;
			member = GetGenericMember(member);
			if (member is IMethod) {
				IMethod parentMethod = (IMethod)member;
				foreach (IMethod m in type.Methods) {
					if (nameComparer.Equals(parentMethod.Name, m.Name)) {
						if (m.IsStatic == parentMethod.IsStatic) {
							if (DiffUtility.Compare(parentMethod.Parameters, m.Parameters) == 0) {
								return m;
							}
						}
					}
				}
			} else if (member is IProperty) {
				IProperty parentMethod = (IProperty)member;
				foreach (IProperty m in type.Properties) {
					if (nameComparer.Equals(parentMethod.Name, m.Name)) {
						if (m.IsStatic == parentMethod.IsStatic) {
							if (DiffUtility.Compare(parentMethod.Parameters, m.Parameters) == 0) {
								return m;
							}
						}
					}
				}
			}
			return null;
		}
		
		public static IMember FindBaseMember(IMember member)
		{
			if (member == null) return null;
			if (member is IMethod && (member as IMethod).IsConstructor) return null;
			IClass parentClass = member.DeclaringType;
			IClass baseClass = parentClass.BaseClass;
			if (baseClass == null) return null;
			
			foreach (IClass childClass in baseClass.ClassInheritanceTree) {
				IMember m = FindSimilarMember(childClass, member);
				if (m != null)
					return m;
			}
			return null;
		}
		#endregion
		
		[System.Diagnostics.ConditionalAttribute("DEBUG")]
		internal static void Log(string text)
		{
			Debug.WriteLine(text);
		}
		
		[System.Diagnostics.ConditionalAttribute("DEBUG")]
		internal static void Log(string text, IEnumerable<IReturnType> types)
		{
			Log(text, types.Select(t => t != null ? t.DotNetName : "<null>"));
		}
		
		[System.Diagnostics.ConditionalAttribute("DEBUG")]
		internal static void Log<T>(string text, IEnumerable<T> lines)
		{
			#if DEBUG
			T[] arr = lines.ToArray();
			if (arr.Length == 0) {
				Log(text + "<empty collection>");
			} else {
				Log(text + arr[0]);
				for (int i = 1; i < arr.Length; i++) {
					Log(new string(' ', text.Length) + arr[i]);
				}
			}
			#endif
		}
	}
}
