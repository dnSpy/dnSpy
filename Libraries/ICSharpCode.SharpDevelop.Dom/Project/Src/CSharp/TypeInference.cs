// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Linq;

namespace ICSharpCode.SharpDevelop.Dom.CSharp
{
	/// <summary>
	/// Implements C# 3.0 type inference.
	/// </summary>
	sealed class TypeInference
	{
		private TypeInference() {}
		
		public static IReturnType[] InferTypeArguments(IMethod method, IList<IReturnType> arguments, out bool success)
		{
			TypeInference ti = new TypeInference();
			Log("Doing type inference for " + new CSharpAmbience().Convert(method));
			Log(" with arguments = ", arguments);
			ti.typeParameters = method.TypeParameters.Select(tp => new TP(tp)).ToList();
			ti.parameterTypes = method.Parameters.Select(p => p.ReturnType).Take(arguments.Count).ToList();
			ti.arguments = arguments.Take(ti.parameterTypes.Count).ToArray();
			ti.PhaseOne();
			success = ti.PhaseTwo();
			IReturnType[] result = ti.typeParameters.Select(tp => tp.FixedTo).ToArray();
			Log("Type inference for " + method.DotNetName + " " + (success ? "succeeded" : "failed") + ": ", result);
			return result;
		}
		
		List<TP> typeParameters;
		List<IReturnType> parameterTypes;
		IList<IReturnType> arguments;
		
		sealed class TP {
			public readonly ITypeParameter TypeParameter;
			public IReturnType FixedTo;
			public HashSet<IReturnType> Bounds = new HashSet<IReturnType>();
			
			public bool Fixed {
				get { return FixedTo != null; }
			}
			
			public TP(ITypeParameter typeParameter)
			{
				this.TypeParameter = typeParameter;
			}
			
			/// <summary>
			/// Gets whether this type parameter occurs in the specified return type.
			/// </summary>
			public bool OccursIn(IReturnType rt)
			{
				ArrayReturnType art = rt.CastToArrayReturnType();
				if (art != null) {
					return OccursIn(art.ArrayElementType);
				}
				ConstructedReturnType crt = rt.CastToConstructedReturnType();
				if (crt != null) {
					return crt.TypeArguments.Any(ta => OccursIn(ta));
				}
				GenericReturnType grt = rt.CastToGenericReturnType();
				if (grt != null) {
					return this.TypeParameter.Equals(grt.TypeParameter);
				}
				return false;
			}
			
			public override string ToString()
			{
				return TypeParameter.Method.Name + "." + TypeParameter.Name;
			}
		}
		
		void PhaseOne()
		{
			Log("Phase One");
			for (int i = 0; i < arguments.Count; i++) {
				IReturnType ei = arguments[i];
				IReturnType Ti = parameterTypes[i];
				if (ei is AnonymousMethodReturnType || ei is MethodGroupReturnType) {
					Log("MakeExplicitParameterTypeInference for #" + i);
					MakeExplicitParameterTypeInference(ei, Ti);
					
					if (OutputTypeContainsUnfixed(ei, Ti) && !InputTypesContainsUnfixed(ei, Ti)) {
						// an output type inference (§7.4.2.6) is made for ei with type Ti.
						Log("MakeOutputTypeInference for #" + i);
						MakeOutputTypeInference(ei, Ti);
					}
				} else {
					Log("MakeOutputTypeInference for #" + i);
					MakeOutputTypeInference(ei, Ti);
				}
			}
		}
		
		bool PhaseTwo()
		{
			Log("Phase Two");
			// All unfixed type variables Xi which do not depend on any Xj are fixed.
			List<TP> typeParametersToFix = new List<TP>();
			foreach (TP Xi in typeParameters) {
				if (Xi.Fixed == false) {
					if (!typeParameters.Any((TP Xj) => DependsOn(Xi, Xj))) {
						typeParametersToFix.Add(Xi);
					}
				}
			}
			// If no such type variables exist, all unfixed type variables Xi are fixed for which all of the following hold:
			if (typeParametersToFix.Count == 0) {
				foreach (TP Xi in typeParameters) {
					// Xi has a non­empty set of bounds
					if (Xi.Fixed == false && Xi.Bounds.Count > 0) {
						// There is at least one type variable Xj that depends on Xi
						if (typeParameters.Any((TP Xj) => DependsOn(Xj, Xi))) {
							typeParametersToFix.Add(Xi);
						}
					}
				}
			}
			// now fix 'em
			bool errorDuringFix = false;
			foreach (TP tp in typeParametersToFix) {
				if (!Fix(tp))
					errorDuringFix = true;
			}
			if (errorDuringFix)
				return false;
			bool unfixedTypeVariablesExist = typeParameters.Any((TP X) => X.Fixed == false);
			if (typeParametersToFix.Count == 0 && unfixedTypeVariablesExist) {
				// If no such type variables exist and there are still unfixed type variables, type inference fails.
				return false;
			} else if (!unfixedTypeVariablesExist) {
				// Otherwise, if no further unfixed type variables exist, type inference succeeds.
				return true;
			} else {
				// Otherwise, for all arguments ei with corresponding parameter type Ti
				for (int i = 0; i < arguments.Count; i++) {
					IReturnType ei = arguments[i];
					IReturnType Ti = parameterTypes[i];
					// where the output types (§7.4.2.4) contain unfixed type variables Xj
					// but the input types (§7.4.2.3) do not
					if (OutputTypeContainsUnfixed(ei, Ti) && !InputTypesContainsUnfixed(ei, Ti)) {
						// an output type inference (§7.4.2.6) is made for ei with type Ti.
						Log("MakeOutputTypeInference for #" + i);
						MakeOutputTypeInference(ei, Ti);
					}
				}
				// Then the second phase is repeated.
				return PhaseTwo();
			}
		}
		
		bool OutputTypeContainsUnfixed(IReturnType argumentType, IReturnType parameterType)
		{
			return OutputTypes(argumentType, parameterType).Any(t => TypeContainsUnfixedParameter(t));
		}
		
		bool InputTypesContainsUnfixed(IReturnType argumentType, IReturnType parameterType)
		{
			return InputTypes(argumentType, parameterType).Any(t => TypeContainsUnfixedParameter(t));
		}
		
		bool TypeContainsUnfixedParameter(IReturnType type)
		{
			return typeParameters.Where(tp => !tp.Fixed).Any(tp => tp.OccursIn(type));
		}
		
		IEnumerable<IReturnType> OutputTypes(IReturnType e, IReturnType T)
		{
			AnonymousMethodReturnType amrt = e as AnonymousMethodReturnType;
			if (amrt != null || e is MethodGroupReturnType) {
				IMethod m = GetDelegateOrExpressionTreeSignature(T, amrt != null && amrt.CanBeConvertedToExpressionTree);
				if (m != null) {
					return new[] { m.ReturnType };
				}
			}
			return EmptyList<IReturnType>.Instance;
		}
		
		IEnumerable<IReturnType> InputTypes(IReturnType e, IReturnType T)
		{
			AnonymousMethodReturnType amrt = e as AnonymousMethodReturnType;
			if (amrt != null && amrt.HasImplicitlyTypedParameters || e is MethodGroupReturnType) {
				IMethod m = GetDelegateOrExpressionTreeSignature(T, amrt != null && amrt.CanBeConvertedToExpressionTree);
				if (m != null) {
					return m.Parameters.Select(p => p.ReturnType);
				}
			}
			return EmptyList<IReturnType>.Instance;
		}
		
		internal static IMethod GetDelegateOrExpressionTreeSignature(IReturnType rt, bool allowExpressionTree)
		{
			if (rt == null)
				return null;
			IClass c = rt.GetUnderlyingClass();
			if (allowExpressionTree && c != null && c.FullyQualifiedName == "System.Linq.Expressions.Expression") {
				ConstructedReturnType crt = rt.CastToConstructedReturnType();
				if (crt != null && crt.TypeArguments.Count == 1) {
					// get delegate type from expression type
					rt = crt.TypeArguments[0];
					c = rt != null ? rt.GetUnderlyingClass() : null;
				}
			}
			if (c != null && c.ClassType == ClassType.Delegate) {
				return rt.GetMethods().FirstOrDefault((IMethod m) => m.Name == "Invoke");
			}
			return null;
		}
		
		bool DependsDirectlyOn(TP Xi, TP Xj)
		{
			if (Xj.Fixed)
				return false;
			for (int k = 0; k < arguments.Count; k++) {
				if (InputTypes(arguments[k], parameterTypes[k]).Any(t => Xj.OccursIn(t))
				    && OutputTypes(arguments[k], parameterTypes[k]).Any(t => Xi.OccursIn(t)))
				{
					return true;
				}
			}
			return false;
		}
		
		void AddDependencies(HashSet<TP> hash, TP Xi)
		{
			foreach (TP Xj in typeParameters) {
				if (DependsDirectlyOn(Xi, Xj)) {
					if (hash.Add(Xj))
						AddDependencies(hash, Xj);
				}
			}
		}
		
		HashSet<TP> GetDependencies(TP X)
		{
			HashSet<TP> hash = new HashSet<TP>();
			AddDependencies(hash, X);
			return hash;
		}
		
		bool DependsOn(TP Xi, TP Xj)
		{
			return GetDependencies(Xi).Contains(Xj);
		}
		
		void MakeOutputTypeInference(IReturnType e, IReturnType T)
		{
			//If e is an anonymous function with inferred return type  U (§7.4.2.11) and T is
			// a delegate type or expression tree type with return type Tb, then a lower­bound
			// inference (§7.4.2.9) is made from U for Tb.
			AnonymousMethodReturnType amrt = e as AnonymousMethodReturnType;
			if (amrt != null) {
				IMethod m = GetDelegateOrExpressionTreeSignature(T, amrt.CanBeConvertedToExpressionTree);
				if (m != null) {
					IReturnType inferredReturnType;
					if (amrt.HasParameterList && amrt.MethodParameters.Count == m.Parameters.Count) {
						var inferredParameterTypes = m.Parameters.Select(p => SubstituteFixedTypes(p.ReturnType)).ToArray();
						inferredReturnType = amrt.ResolveReturnType(inferredParameterTypes);
					} else {
						inferredReturnType = amrt.ResolveReturnType();
					}
					
					MakeLowerBoundInference(inferredReturnType, m.ReturnType);
					return;
				}
			}
			// Otherwise, if e is a method group and T is a delegate type or expression tree type
			// return type Tb with parameter types T1…Tk and return type Tb, and overload resolution
			// of e with the types T1…Tk yields a single method with return type U, then a lower­bound
			// inference is made from U for Tb.
			if (e is MethodGroupReturnType) {
				// the MS C# doesn't seem to implement this rule, so we can safely skip this
				return;
			}
			// Otherwise, if e is an expression with type U, then a lower­bound inference is made from
			// U for T.
			MakeLowerBoundInference(e, T);
		}
		
		IReturnType SubstituteFixedTypes(IReturnType rt)
		{
			return ConstructedReturnType.TranslateType(
				rt, typeParameters.Select(tp => tp.FixedTo).ToList(), true);
		}
		
		void MakeExplicitParameterTypeInference(IReturnType e, IReturnType T)
		{
			// If e is an explicitly typed anonymous function with parameter types U1…Uk and T is a
			// delegate type with parameter types V1…Vk then for each Ui an exact inference (§7.4.2.8)
			// is made from Ui for the corresponding Vi.
			AnonymousMethodReturnType amrt = e as AnonymousMethodReturnType;
			if (amrt != null && amrt.HasParameterList) {
				IMethod m = GetDelegateOrExpressionTreeSignature(T, amrt.CanBeConvertedToExpressionTree);
				if (m != null && amrt.MethodParameters.Count == m.Parameters.Count) {
					for (int i = 0; i < amrt.MethodParameters.Count; i++) {
						MakeExactInference(amrt.MethodParameters[i].ReturnType, m.Parameters[i].ReturnType);
					}
				}
			}
		}
		
		/// <summary>
		/// Make exact inference from U for V.
		/// </summary>
		void MakeExactInference(IReturnType U, IReturnType V)
		{
			Log(" MakeExactInference from " + U + " for " + V);
			if (U == null || V == null)
				return;
			
			// If V is one of the unfixed Xi then U is added to the set of bounds for Xi.
			TP tp = GetTPForType(V);
			if (tp != null && tp.Fixed == false) {
				Log(" Add bound '" + U.DotNetName + "' to " + tp);
				tp.Bounds.Add(U);
				return;
			}
			// Otherwise if U is an array type Ue[…] and V is an array type Ve[…] of the same rank
			// then an exact inference from Ue to Ve is made
			ArrayReturnType arrU = U.CastToArrayReturnType();
			ArrayReturnType arrV = V.CastToArrayReturnType();
			if (arrU != null && arrV != null && arrU.ArrayDimensions == arrV.ArrayDimensions) {
				MakeExactInference(arrU.ArrayElementType, arrV.ArrayElementType);
				return;
			}
			// Otherwise if V is a constructed type C<V1…Vk> and U is a constructed
			// type C<U1…Uk> then an exact inference is made from each Ui to the corresponding Vi.
			ConstructedReturnType CU = U.CastToConstructedReturnType();
			ConstructedReturnType CV = V.CastToConstructedReturnType();
			if (CU != null && CV != null
			    && object.Equals(CU.UnboundType, CV.UnboundType)
			    && CU.TypeArgumentCount == CV.TypeArgumentCount)
			{
				for (int i = 0; i < CU.TypeArgumentCount; i++) {
					MakeExactInference(CU.TypeArguments[i], CV.TypeArguments[i]);
				}
				return;
			}
		}
		
		TP GetTPForType(IReturnType t)
		{
			if (t == null)
				return null;
			GenericReturnType grt = t.CastToGenericReturnType();
			if (grt != null) {
				return typeParameters.FirstOrDefault(tp => tp.TypeParameter.Equals(grt.TypeParameter));
			}
			return null;
		}
		
		/// <summary>
		/// Make lower bound inference from U for V.
		/// </summary>
		void MakeLowerBoundInference(IReturnType U, IReturnType V)
		{
			Log(" MakeLowerBoundInference from " + U + " for " + V);
			if (U == null || V == null)
				return;
			
			// If V is one of the unfixed Xi then U is added to the set of bounds for Xi.
			TP tp = GetTPForType(V);
			if (tp != null && tp.Fixed == false) {
				Log("  Add bound '" + U.DotNetName + "' to " + tp);
				tp.Bounds.Add(U);
				return;
			}
			// Otherwise if U is an array type Ue[…] and V is either an array type Ve[…]of the
			// same rank, or if U is a one­dimensional array type Ue[]and V is one of
			// IEnumerable<Ve>, ICollection<Ve> or IList<Ve> then
			ArrayReturnType arrU = U.CastToArrayReturnType();
			ArrayReturnType arrV = V.CastToArrayReturnType();
			ConstructedReturnType CV = V.CastToConstructedReturnType();
			if (arrU != null &&
			    (arrV != null && arrU.ArrayDimensions == arrV.ArrayDimensions
			     || (arrU.ArrayDimensions == 1 && IsIEnumerableCollectionOrList(CV))))
			{
				IReturnType Ue = arrU.ArrayElementType;
				IReturnType Ve = arrV != null ? arrV.ArrayElementType : CV.TypeArguments[0];
				// If Ue is known to be a reference type then a lower­bound inference from Ue to Ve is made
				if (IsReferenceType(Ue) ?? false) {
					MakeLowerBoundInference(Ue, Ve);
				} else {
					// Otherwise an exact inference from Ue to Ve is made
					MakeExactInference(Ue, Ve);
				}
				return;
			}
			// Otherwise if V is a constructed type C<V1…Vk> and there is a unique set of
			// types U1…Uk such that a standard implicit conversion exists from U to C<U1…Uk>
			// then an exact inference is made from each Ui for the corresponding Vi.
			if (CV != null) {
				foreach (IReturnType U2 in MemberLookupHelper.GetTypeInheritanceTree(U)) {
					ConstructedReturnType CU2 = U2.CastToConstructedReturnType();
					if (CU2 != null &&
					    object.Equals(CU2.UnboundType, CV.UnboundType) &&
					    CU2.TypeArgumentCount == CV.TypeArgumentCount)
					{
						for (int i = 0; i < CU2.TypeArgumentCount; i++) {
							MakeExactInference(CU2.TypeArguments[i], CV.TypeArguments[i]);
						}
						return;
					}
				}
			}
		}
		
		bool IsIEnumerableCollectionOrList(ConstructedReturnType rt)
		{
			if (rt == null || rt.TypeArgumentCount != 1)
				return false;
			switch (rt.UnboundType.FullyQualifiedName) {
				case "System.Collections.Generic.IList":
				case "System.Collections.Generic.ICollection":
				case "System.Collections.Generic.IEnumerable":
					return true;
				default:
					return false;
			}
		}
		
		bool? IsReferenceType(IReturnType rt)
		{
			if (rt == null)
				return null;
			IClass c = rt.GetUnderlyingClass();
			if (c == null)
				return null;
			switch (c.ClassType) {
				case ClassType.Enum:
				case ClassType.Struct:
					return false;
				default:
					return true;
			}
		}
		
		bool Fix(TP X)
		{
			Log("Trying to fix " + X);
			Log("  bounds = ", X.Bounds);
			List<IReturnType> candidates = new List<IReturnType>(X.Bounds);
			foreach (IReturnType U in X.Bounds) {
				candidates.RemoveAll((IReturnType candidate) => !MemberLookupHelper.ConversionExists(U, candidate));
			}
			Log("  candidates after removal round = ", candidates);
			if (candidates.Count == 0)
				return false;
			var results = candidates.Where(
				c1 => candidates.All(c2 => MemberLookupHelper.ConversionExists(c1, c2))
			).ToList();
			Log("  possible solutions (should be exactly one) = ", candidates);
			if (results.Count == 1) {
				X.FixedTo = results[0];
				return true;
			} else {
				return false;
			}
		}
		
		[System.Diagnostics.ConditionalAttribute("DEBUG")]
		static void Log(string text)
		{
			MemberLookupHelper.Log(text);
		}
		
		[System.Diagnostics.ConditionalAttribute("DEBUG")]
		static void Log(string text, IEnumerable<IReturnType> types)
		{
			MemberLookupHelper.Log(text, types);
		}
	}
}
