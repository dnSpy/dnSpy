// Copyright (c) 2010 AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.TypeSystem.Implementation;

namespace ICSharpCode.NRefactory.CSharp.Resolver
{
	public enum TypeInferenceAlgorithm
	{
		/// <summary>
		/// C# 4.0 type inference.
		/// </summary>
		CSharp4,
		/// <summary>
		/// Improved algorithm (not part of any specification) using FindTypeInBounds for fixing.
		/// </summary>
		Improved,
		/// <summary>
		/// Improved algorithm (not part of any specification) using FindTypeInBounds for fixing;
		/// uses <see cref="IntersectionType"/> to report all results (in case of ambiguities).
		/// </summary>
		ImprovedReturnAllResults
	}
	
	/// <summary>
	/// Implements C# 4.0 Type Inference (§7.5.2).
	/// </summary>
	public sealed class TypeInference
	{
		readonly ITypeResolveContext context;
		readonly Conversions conversions;
		TypeInferenceAlgorithm algorithm = TypeInferenceAlgorithm.CSharp4;
		
		// determines the maximum generic nesting level; necessary to avoid infinite recursion in 'Improved' mode.
		const int maxNestingLevel = 5;
		int nestingLevel;
		
		#region Constructor
		public TypeInference(ITypeResolveContext context, Conversions conversions = null)
		{
			if (context == null)
				throw new ArgumentNullException("context");
			this.context = context;
			this.conversions = conversions ?? new Conversions(context);
		}
		#endregion
		
		#region Properties
		/// <summary>
		/// Gets/Sets the type inference algorithm used.
		/// </summary>
		public TypeInferenceAlgorithm Algorithm {
			get { return algorithm; }
			set { algorithm = value; }
		}
		
		TypeInference CreateNestedInstance()
		{
			TypeInference c = new TypeInference(context, conversions);
			c.algorithm = algorithm;
			c.nestingLevel = nestingLevel + 1;
			return c;
		}
		#endregion
		
		TP[] typeParameters;
		IType[] parameterTypes;
		ResolveResult[] arguments;
		bool[,] dependencyMatrix;
		
		#region InferTypeArguments (main function)
		public IType[] InferTypeArguments(IList<ITypeParameter> typeParameters, IList<ResolveResult> arguments, IList<IType> parameterTypes, out bool success)
		{
			if (typeParameters == null)
				throw new ArgumentNullException("typeParameters");
			if (arguments == null)
				throw new ArgumentNullException("arguments");
			if (parameterTypes == null)
				throw new ArgumentNullException("parameterTypes");
			try {
				this.typeParameters = new TP[typeParameters.Count];
				for (int i = 0; i < this.typeParameters.Length; i++) {
					if (i != typeParameters[i].Index)
						throw new ArgumentException("Type parameter has wrong index");
					this.typeParameters[i] = new TP(typeParameters[i]);
				}
				this.parameterTypes = new IType[Math.Min(arguments.Count, parameterTypes.Count)];
				this.arguments = new ResolveResult[this.parameterTypes.Length];
				for (int i = 0; i < this.parameterTypes.Length; i++) {
					if (arguments[i] == null || parameterTypes[i] == null)
						throw new ArgumentNullException();
					this.arguments[i] = arguments[i];
					this.parameterTypes[i] = parameterTypes[i];
				}
				PhaseOne();
				success = PhaseTwo();
				return this.typeParameters.Select(tp => tp.FixedTo ?? SharedTypes.UnknownType).ToArray();
			} finally {
				Reset();
			}
		}
		
		void Reset()
		{
			// clean up so that memory used by the operation can be garbage collected as soon as possible
			this.typeParameters = null;
			this.parameterTypes = null;
			this.arguments = null;
			this.dependencyMatrix = null;
		}
		
		/// <summary>
		/// Infers type arguments for the <paramref name="typeParameters"/> occurring in the <paramref name="targetType"/>
		/// so that the resulting type (after substition) satisfies the given bounds.
		/// </summary>
		public IType[] InferTypeArgumentsFromBounds(IList<ITypeParameter> typeParameters, IType targetType, IList<IType> lowerBounds, IList<IType> upperBounds, out bool success)
		{
			if (typeParameters == null)
				throw new ArgumentNullException("typeParameters");
			if (targetType == null)
				throw new ArgumentNullException("targetType");
			if (lowerBounds == null)
				throw new ArgumentNullException("lowerBounds");
			if (upperBounds == null)
				throw new ArgumentNullException("upperBounds");
			this.typeParameters = new TP[typeParameters.Count];
			for (int i = 0; i < this.typeParameters.Length; i++) {
				if (i != typeParameters[i].Index)
					throw new ArgumentException("Type parameter has wrong index");
				this.typeParameters[i] = new TP(typeParameters[i]);
			}
			foreach (IType b in lowerBounds) {
				MakeLowerBoundInference(b, targetType);
			}
			foreach (IType b in upperBounds) {
				MakeUpperBoundInference(b, targetType);
			}
			IType[] result = new IType[this.typeParameters.Length];
			success = true;
			for (int i = 0; i < result.Length; i++) {
				success &= Fix(this.typeParameters[i]);
				result[i] = this.typeParameters[i].FixedTo ?? SharedTypes.UnknownType;
			}
			Reset();
			return result;
		}
		#endregion
		
		sealed class TP
		{
			public readonly HashSet<IType> LowerBounds = new HashSet<IType>();
			public readonly HashSet<IType> UpperBounds = new HashSet<IType>();
			public readonly ITypeParameter TypeParameter;
			public IType FixedTo;
			
			public bool IsFixed {
				get { return FixedTo != null; }
			}
			
			public bool HasBounds {
				get { return LowerBounds.Count > 0 || UpperBounds.Count > 0; }
			}
			
			public TP(ITypeParameter typeParameter)
			{
				if (typeParameter == null)
					throw new ArgumentNullException("typeParameter");
				this.TypeParameter = typeParameter;
			}
			
			public override string ToString()
			{
				return TypeParameter.Name;
			}
		}
		
		sealed class OccursInVisitor : TypeVisitor
		{
			readonly TP[] tp;
			public readonly bool[] Occurs;
			
			public OccursInVisitor(TypeInference typeInference)
			{
				this.tp = typeInference.typeParameters;
				this.Occurs = new bool[tp.Length];
			}
			
			public override IType VisitTypeParameter(ITypeParameter type)
			{
				int index = type.Index;
				if (index < tp.Length && tp[index].TypeParameter == type)
					Occurs[index] = true;
				return base.VisitTypeParameter(type);
			}
		}
		
		#region Inference Phases
		void PhaseOne()
		{
			// C# 4.0 spec: §7.5.2.1 The first phase
			Log("Phase One");
			for (int i = 0; i < arguments.Length; i++) {
				ResolveResult Ei = arguments[i];
				IType Ti = parameterTypes[i];
				// TODO: what if Ei is an anonymous function?
				IType U = Ei.Type;
				if (U != SharedTypes.UnknownType) {
					if (Ti is ByReferenceType) {
						MakeExactInference(Ei.Type, Ti);
					} else {
						MakeLowerBoundInference(Ei.Type, Ti);
					}
				}
			}
		}
		
		bool PhaseTwo()
		{
			// C# 4.0 spec: §7.5.2.2 The second phase
			Log("Phase Two");
			// All unfixed type variables Xi which do not depend on any Xj are fixed.
			List<TP> typeParametersToFix = new List<TP>();
			foreach (TP Xi in typeParameters) {
				if (Xi.IsFixed == false) {
					if (!typeParameters.Any((TP Xj) => DependsOn(Xi, Xj))) {
						typeParametersToFix.Add(Xi);
					}
				}
			}
			// If no such type variables exist, all unfixed type variables Xi are fixed for which all of the following hold:
			if (typeParametersToFix.Count == 0) {
				foreach (TP Xi in typeParameters) {
					// Xi has a non­empty set of bounds
					if (!Xi.IsFixed && Xi.HasBounds) {
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
			bool unfixedTypeVariablesExist = typeParameters.Any((TP X) => X.IsFixed == false);
			if (typeParametersToFix.Count == 0 && unfixedTypeVariablesExist) {
				// If no such type variables exist and there are still unfixed type variables, type inference fails.
				return false;
			} else if (!unfixedTypeVariablesExist) {
				// Otherwise, if no further unfixed type variables exist, type inference succeeds.
				return true;
			} else {
				// Otherwise, for all arguments ei with corresponding parameter type Ti
				for (int i = 0; i < arguments.Length; i++) {
					ResolveResult Ei = arguments[i];
					IType Ti = parameterTypes[i];
					// where the output types (§7.4.2.4) contain unfixed type variables Xj
					// but the input types (§7.4.2.3) do not
					if (OutputTypeContainsUnfixed(Ei, Ti) && !InputTypesContainsUnfixed(Ei, Ti)) {
						// an output type inference (§7.4.2.6) is made for ei with type Ti.
						Log("MakeOutputTypeInference for #" + i);
						MakeOutputTypeInference(Ei, Ti);
					}
				}
				// Then the second phase is repeated.
				return PhaseTwo();
			}
		}
		#endregion
		
		#region Input Types / Output Types (§7.5.2.3 + §7.5.2.4)
		static readonly IType[] emptyTypeArray = new IType[0];
		
		IType[] InputTypes(ResolveResult e, IType t)
		{
			// C# 4.0 spec: §7.5.2.3 Input types
			/* TODO
			AnonymousMethodReturnType amrt = e as AnonymousMethodReturnType;
			if (amrt != null && amrt.HasImplicitlyTypedParameters || e is MethodGroupReturnType) {
				IMethod m = GetDelegateOrExpressionTreeSignature(t, amrt != null && amrt.CanBeConvertedToExpressionTree);
				if (m != null) {
					return m.Parameters.Select(p => p.ReturnType);
				}
			}*/
			return emptyTypeArray;
		}
		
		
		IType[] OutputTypes(ResolveResult e, IType t)
		{
			// C# 4.0 spec: §7.5.2.4 Input types
			/*
			AnonymousMethodReturnType amrt = e as AnonymousMethodReturnType;
			if (amrt != null || e is MethodGroupReturnType) {
				IMethod m = GetDelegateOrExpressionTreeSignature(T, amrt != null && amrt.CanBeConvertedToExpressionTree);
				if (m != null) {
					return new[] { m.ReturnType };
				}
			}
			 */
			return emptyTypeArray;
		}
		
		bool InputTypesContainsUnfixed(ResolveResult argument, IType parameterType)
		{
			return AnyTypeContainsUnfixedParameter(InputTypes(argument, parameterType));
		}
		
		bool OutputTypeContainsUnfixed(ResolveResult argument, IType parameterType)
		{
			return AnyTypeContainsUnfixedParameter(OutputTypes(argument, parameterType));
		}
		
		bool AnyTypeContainsUnfixedParameter(IEnumerable<IType> types)
		{
			OccursInVisitor o = new OccursInVisitor(this);
			foreach (var type in types) {
				type.AcceptVisitor(o);
			}
			for (int i = 0; i < typeParameters.Length; i++) {
				if (!typeParameters[i].IsFixed && o.Occurs[i])
					return true;
			}
			return false;
		}
		#endregion
		
		#region DependsOn (§7.5.2.5)
		// C# 4.0 spec: §7.5.2.5 Dependance
		
		void CalculateDependencyMatrix()
		{
			int n = typeParameters.Length;
			dependencyMatrix = new bool[n, n];
			for (int k = 0; k < arguments.Length; k++) {
				OccursInVisitor input = new OccursInVisitor(this);
				OccursInVisitor output = new OccursInVisitor(this);
				foreach (var type in InputTypes(arguments[k], parameterTypes[k])) {
					type.AcceptVisitor(input);
				}
				foreach (var type in OutputTypes(arguments[k], parameterTypes[k])) {
					type.AcceptVisitor(output);
				}
				for (int i = 0; i < n; i++) {
					for (int j = 0; j < n; j++) {
						dependencyMatrix[i, j] |= input.Occurs[j] && output.Occurs[i];
					}
				}
			}
			// calculate transitive closure using Warshall's algorithm:
			for (int i = 0; i < n; i++) {
				for (int j = 0; j < n; j++) {
					if (dependencyMatrix[i, j]) {
						for (int k = 0; k < n; k++) {
							if (dependencyMatrix[j, k])
								dependencyMatrix[i, k] = true;
						}
					}
				}
			}
		}
		
		bool DependsOn(TP x, TP y)
		{
			if (dependencyMatrix == null)
				CalculateDependencyMatrix();
			// x depends on y
			return dependencyMatrix[x.TypeParameter.Index, y.TypeParameter.Index];
		}
		#endregion
		
		#region MakeOutputTypeInference (§7.5.2.6)
		void MakeOutputTypeInference(ResolveResult e, IType t)
		{
			// If E is an anonymous function with inferred return type  U (§7.5.2.12) and T is a delegate type or expression
			// tree type with return type Tb, then a lower-bound inference (§7.5.2.9) is made from U to Tb.
			/* TODO AnonymousMethodReturnType amrt = e as AnonymousMethodReturnType;
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
			}*/
			// Otherwise, if E is a method group and T is a delegate type or expression tree type
			// with parameter types T1…Tk and return type Tb, and overload resolution
			// of E with the types T1…Tk yields a single method with return type U, then a lower­-bound
			// inference is made from U to Tb.
			MethodGroupResolveResult mgrr = e as MethodGroupResolveResult;
			if (mgrr != null) {
				throw new NotImplementedException();
			}
			// Otherwise, if E is an expression with type U, then a lower-bound inference is made from U to T.
			if (e.Type != SharedTypes.UnknownType) {
				MakeLowerBoundInference(e.Type, t);
			}
		}
		#endregion
		
		#region MakeExplicitParameterTypeInference (§7.5.2.7)
		void MakeExplicitParameterTypeInference(ResolveResult e, IType t)
		{
			// C# 4.0 spec: §7.5.2.7 Explicit parameter type inferences
			throw new NotImplementedException();
			/*AnonymousMethodReturnType amrt = e as AnonymousMethodReturnType;
			if (amrt != null && amrt.HasParameterList) {
				IMethod m = GetDelegateOrExpressionTreeSignature(T, amrt.CanBeConvertedToExpressionTree);
				if (m != null && amrt.MethodParameters.Count == m.Parameters.Count) {
					for (int i = 0; i < amrt.MethodParameters.Count; i++) {
						MakeExactInference(amrt.MethodParameters[i].ReturnType, m.Parameters[i].ReturnType);
					}
				}
			}*/
		}
		#endregion
		
		#region MakeExactInference (§7.5.2.8)
		/// <summary>
		/// Make exact inference from U to V.
		/// C# 4.0 spec: §7.5.2.8 Exact inferences
		/// </summary>
		void MakeExactInference(IType U, IType V)
		{
			Log("MakeExactInference from " + U + " to " + V);
			
			// If V is one of the unfixed Xi then U is added to the set of bounds for Xi.
			TP tp = GetTPForType(V);
			if (tp != null && tp.IsFixed == false) {
				Log(" Add exact bound '" + U + "' to " + tp);
				tp.LowerBounds.Add(U);
				tp.UpperBounds.Add(U);
				return;
			}
			// Handle by reference types:
			ByReferenceType brU = U as ByReferenceType;
			ByReferenceType brV = V as ByReferenceType;
			if (brU != null && brV != null) {
				MakeExactInference(brU.ElementType, brV.ElementType);
				return;
			}
			// Handle array types:
			ArrayType arrU = U as ArrayType;
			ArrayType arrV = V as ArrayType;
			if (arrU != null && arrV != null && arrU.Dimensions == arrV.Dimensions) {
				MakeExactInference(arrU.ElementType, arrV.ElementType);
				return;
			}
			// Handle parameterized type:
			ParameterizedType pU = U as ParameterizedType;
			ParameterizedType pV = V as ParameterizedType;
			if (pU != null && pV != null
			    && object.Equals(pU.GetDefinition(), pV.GetDefinition())
			    && pU.TypeParameterCount == pV.TypeParameterCount)
			{
				Debug.Indent();
				for (int i = 0; i < pU.TypeParameterCount; i++) {
					MakeExactInference(pU.TypeArguments[i], pV.TypeArguments[i]);
				}
				Debug.Unindent();
			}
		}
		
		TP GetTPForType(IType v)
		{
			ITypeParameter p = v as ITypeParameter;
			if (p != null) {
				int index = p.Index;
				if (index < typeParameters.Length && typeParameters[index].TypeParameter == p)
					return typeParameters[index];
			}
			return null;
		}
		#endregion
		
		#region MakeLowerBoundInference (§7.5.2.9)
		/// <summary>
		/// Make lower bound inference from U to V.
		/// C# 4.0 spec: §7.5.2.9 Lower-bound inferences
		/// </summary>
		void MakeLowerBoundInference(IType U, IType V)
		{
			Log(" MakeLowerBoundInference from " + U + " to " + V);
			
			// If V is one of the unfixed Xi then U is added to the set of bounds for Xi.
			TP tp = GetTPForType(V);
			if (tp != null && tp.IsFixed == false) {
				Log("  Add lower bound '" + U + "' to " + tp);
				tp.LowerBounds.Add(U);
				return;
			}
			
			// Handle array types:
			ArrayType arrU = U as ArrayType;
			ArrayType arrV = V as ArrayType;
			ParameterizedType pV = V as ParameterizedType;
			if (arrU != null && arrV != null && arrU.Dimensions == arrV.Dimensions) {
				MakeLowerBoundInference(arrU.ElementType, arrV.ElementType);
				return;
			} else if (arrU != null && IsIEnumerableCollectionOrList(pV) && arrU.Dimensions == 1) {
				MakeLowerBoundInference(arrU.ElementType, pV.TypeArguments[0]);
				return;
			}
			// Handle parameterized types:
			if (pV != null) {
				ParameterizedType uniqueBaseType = null;
				foreach (IType baseU in U.GetAllBaseTypes(context)) {
					ParameterizedType pU = baseU as ParameterizedType;
					if (pU != null && object.Equals(pU.GetDefinition(), pV.GetDefinition()) && pU.TypeParameterCount == pV.TypeParameterCount) {
						if (uniqueBaseType == null)
							uniqueBaseType = pU;
						else
							return; // cannot make an inference because it's not unique
					}
				}
				Debug.Indent();
				if (uniqueBaseType != null) {
					for (int i = 0; i < uniqueBaseType.TypeParameterCount; i++) {
						IType Ui = uniqueBaseType.TypeArguments[i];
						IType Vi = pV.TypeArguments[i];
						if (Ui.IsReferenceType(context) == true) {
							// look for variance
							ITypeParameter Xi = pV.GetDefinition().TypeParameters[i];
							switch (Xi.Variance) {
								case VarianceModifier.Covariant:
									MakeLowerBoundInference(Ui, Vi);
									break;
								case VarianceModifier.Contravariant:
									MakeUpperBoundInference(Ui, Vi);
									break;
								default: // invariant
									MakeExactInference(Ui, Vi);
									break;
							}
						} else {
							// not known to be a reference type
							MakeExactInference(Ui, Vi);
						}
					}
				}
				Debug.Unindent();
			}
		}
		
		static bool IsIEnumerableCollectionOrList(ParameterizedType rt)
		{
			if (rt == null || rt.TypeParameterCount != 1)
				return false;
			switch (rt.GetDefinition().FullName) {
				case "System.Collections.Generic.IList":
				case "System.Collections.Generic.ICollection":
				case "System.Collections.Generic.IEnumerable":
					return true;
				default:
					return false;
			}
		}
		#endregion
		
		#region MakeUpperBoundInference (§7.5.2.10)
		/// <summary>
		/// Make upper bound inference from U to V.
		/// C# 4.0 spec: §7.5.2.10 Upper-bound inferences
		/// </summary>
		void MakeUpperBoundInference(IType U, IType V)
		{
			Log(" MakeUpperBoundInference from " + U + " to " + V);
			
			// If V is one of the unfixed Xi then U is added to the set of bounds for Xi.
			TP tp = GetTPForType(V);
			if (tp != null && tp.IsFixed == false) {
				Log("  Add upper bound '" + U + "' to " + tp);
				tp.UpperBounds.Add(U);
				return;
			}
			
			// Handle array types:
			ArrayType arrU = U as ArrayType;
			ArrayType arrV = V as ArrayType;
			ParameterizedType pU = U as ParameterizedType;
			if (arrV != null && arrU != null && arrU.Dimensions == arrV.Dimensions) {
				MakeUpperBoundInference(arrU.ElementType, arrV.ElementType);
				return;
			} else if (arrV != null && IsIEnumerableCollectionOrList(pU) && arrV.Dimensions == 1) {
				MakeUpperBoundInference(pU.TypeArguments[0], arrV.ElementType);
				return;
			}
			// Handle parameterized types:
			if (pU != null) {
				ParameterizedType uniqueBaseType = null;
				foreach (IType baseV in V.GetAllBaseTypes(context)) {
					ParameterizedType pV = baseV as ParameterizedType;
					if (pV != null && object.Equals(pU.GetDefinition(), pV.GetDefinition()) && pU.TypeParameterCount == pV.TypeParameterCount) {
						if (uniqueBaseType == null)
							uniqueBaseType = pV;
						else
							return; // cannot make an inference because it's not unique
					}
				}
				Debug.Indent();
				if (uniqueBaseType != null) {
					for (int i = 0; i < uniqueBaseType.TypeParameterCount; i++) {
						IType Ui = pU.TypeArguments[i];
						IType Vi = uniqueBaseType.TypeArguments[i];
						if (Ui.IsReferenceType(context) == true) {
							// look for variance
							ITypeParameter Xi = pU.GetDefinition().TypeParameters[i];
							switch (Xi.Variance) {
								case VarianceModifier.Covariant:
									MakeUpperBoundInference(Ui, Vi);
									break;
								case VarianceModifier.Contravariant:
									MakeLowerBoundInference(Ui, Vi);
									break;
								default: // invariant
									MakeExactInference(Ui, Vi);
									break;
							}
						} else {
							// not known to be a reference type
							MakeExactInference(Ui, Vi);
						}
					}
				}
				Debug.Unindent();
			}
		}
		#endregion
		
		#region Fixing (§7.5.2.11)
		bool Fix(TP tp)
		{
			Log(" Trying to fix " + tp);
			Debug.Assert(!tp.IsFixed);
			Debug.Indent();
			var types = CreateNestedInstance().FindTypesInBounds(tp.LowerBounds.ToArray(), tp.UpperBounds.ToArray());
			Debug.Unindent();
			if (algorithm == TypeInferenceAlgorithm.ImprovedReturnAllResults) {
				tp.FixedTo = IntersectionType.Create(types);
				Log("  T was fixed " + (types.Count >= 1 ? "successfully" : "(with errors)") + " to " + tp.FixedTo);
				return types.Count >= 1;
			} else {
				tp.FixedTo = GetFirstTypePreferNonInterfaces(types);
				Log("  T was fixed " + (types.Count == 1 ? "successfully" : "(with errors)") + " to " + tp.FixedTo);
				return types.Count == 1;
			}
		}
		#endregion
		
		#region Finding the best common type of a set of expresssions
		/// <summary>
		/// Gets the best common type (C# 4.0 spec: §7.5.2.14) of a set of expressions.
		/// </summary>
		public IType GetBestCommonType(IEnumerable<ResolveResult> expressions, out bool success)
		{
			if (expressions == null)
				throw new ArgumentNullException("expressions");
			try {
				this.typeParameters = new TP[1] { new TP(DummyTypeParameter.Instance) };
				foreach (ResolveResult r in expressions) {
					MakeOutputTypeInference(r, DummyTypeParameter.Instance);
				}
				success = Fix(typeParameters[0]);
				return typeParameters[0].FixedTo ?? SharedTypes.UnknownType;
			} finally {
				Reset();
			}
		}
		
		sealed class DummyTypeParameter : AbstractType, ITypeParameter
		{
			public static readonly DummyTypeParameter Instance = new DummyTypeParameter();
			
			public override string Name {
				get { return "X"; }
			}
			
			public override bool? IsReferenceType(ITypeResolveContext context)
			{
				return null;
			}
			
			public override int GetHashCode()
			{
				return 0;
			}
			
			public override bool Equals(IType other)
			{
				return this == other;
			}
			
			int ITypeParameter.Index {
				get { return 0; }
			}
			
			IList<IAttribute> ITypeParameter.Attributes {
				get { return EmptyList<IAttribute>.Instance; }
			}
			
			EntityType ITypeParameter.OwnerType {
				get { return EntityType.Method; }
			}
			
			IList<ITypeReference> ITypeParameter.Constraints {
				get { return EmptyList<ITypeReference>.Instance; }
			}
			
			bool ITypeParameter.HasDefaultConstructorConstraint {
				get { return false; }
			}
			
			bool ITypeParameter.HasReferenceTypeConstraint {
				get { return false; }
			}
			
			bool ITypeParameter.HasValueTypeConstraint {
				get { return false; }
			}
			
			VarianceModifier ITypeParameter.Variance {
				get { return VarianceModifier.Invariant; }
			}
			
			IType ITypeParameter.BoundTo {
				get { return null; }
			}
			
			ITypeParameter ITypeParameter.UnboundTypeParameter {
				get { return null; }
			}
			
			bool IFreezable.IsFrozen {
				get { return true; }
			}
			
			void IFreezable.Freeze()
			{
			}
			
			DomRegion ITypeParameter.Region {
				get { return DomRegion.Empty; }
			}
		}
		#endregion
		
		#region FindTypeInBounds
		/// <summary>
		/// Finds a type that satisfies the given lower and upper bounds.
		/// </summary>
		public IType FindTypeInBounds(IList<IType> lowerBounds, IList<IType> upperBounds)
		{
			if (lowerBounds == null)
				throw new ArgumentNullException("lowerBounds");
			if (upperBounds == null)
				throw new ArgumentNullException("upperBounds");
			
			IList<IType> result = FindTypesInBounds(lowerBounds, upperBounds);
			
			if (algorithm == TypeInferenceAlgorithm.ImprovedReturnAllResults) {
				return IntersectionType.Create(result);
			} else {
				// return any of the candidates (prefer non-interfaces)
				return GetFirstTypePreferNonInterfaces(result);
			}
		}
		
		static IType GetFirstTypePreferNonInterfaces(IList<IType> result)
		{
			return result.FirstOrDefault(c => c.GetDefinition().ClassType != ClassType.Interface)
				?? result.FirstOrDefault() ?? SharedTypes.UnknownType;
		}
		
		IList<IType> FindTypesInBounds(IList<IType> lowerBounds, IList<IType> upperBounds)
		{
			// If there's only a single type; return that single type.
			// If both inputs are empty, return the empty list.
			if (lowerBounds.Count == 0 && upperBounds.Count <= 1)
				return upperBounds;
			if (upperBounds.Count == 0 && lowerBounds.Count <= 1)
				return lowerBounds;
			if (nestingLevel > maxNestingLevel)
				return EmptyList<IType>.Instance;
			
			// Finds a type X so that "LB <: X <: UB"
			Log("FindTypesInBound, LowerBounds=", lowerBounds);
			Log("FindTypesInBound, UpperBounds=", upperBounds);
			Debug.Indent();
			
			// First try the Fixing algorithm from the C# spec (§7.5.2.11)
			List<IType> candidateTypes = lowerBounds.Union(upperBounds)
				.Where(c => lowerBounds.All(b => conversions.ImplicitConversion(b, c)))
				.Where(c => upperBounds.All(b => conversions.ImplicitConversion(c, b)))
				.ToList(); // evaluate the query only once
			
			candidateTypes = candidateTypes.Where(
				c => candidateTypes.All(o => conversions.ImplicitConversion(c, o))
			).ToList();
			// If the specified algorithm produces a single candidate, we return
			// that candidate.
			// We also return the whole candidate list if we're not using the improved
			// algorithm.
			if (candidateTypes.Count == 1 || !(algorithm == TypeInferenceAlgorithm.Improved || algorithm == TypeInferenceAlgorithm.ImprovedReturnAllResults))
			{
				return candidateTypes;
			}
			candidateTypes.Clear();
			
			// Now try the improved algorithm
			List<ITypeDefinition> candidateTypeDefinitions;
			if (lowerBounds.Count > 0) {
				// Find candidates by using the lower bounds:
				var hashSet = new HashSet<ITypeDefinition>(lowerBounds[0].GetAllBaseTypeDefinitions(context));
				for (int i = 1; i < lowerBounds.Count; i++) {
					hashSet.IntersectWith(lowerBounds[i].GetAllBaseTypeDefinitions(context));
				}
				candidateTypeDefinitions = hashSet.ToList();
			} else {
				// Find candidates by looking at all classes in the project:
				candidateTypeDefinitions = context.GetAllTypes().ToList();
			}
			
			// Now filter out candidates that violate the upper bounds:
			foreach (IType ub in upperBounds) {
				ITypeDefinition ubDef = ub.GetDefinition();
				if (ubDef != null) {
					candidateTypeDefinitions.RemoveAll(c => !c.IsDerivedFrom(ubDef, context));
				}
			}
			
			foreach (ITypeDefinition candidateDef in candidateTypeDefinitions) {
				// determine the type parameters for the candidate:
				IType candidate;
				if (candidateDef.TypeParameterCount == 0) {
					candidate = candidateDef;
				} else {
					Log("Inferring arguments for candidate type definition: " + candidateDef);
					bool success;
					IType[] result = InferTypeArgumentsFromBounds(
						candidateDef.TypeParameters,
						new ParameterizedType(candidateDef, candidateDef.TypeParameters.SafeCast<ITypeParameter, IType>()),
						lowerBounds, upperBounds,
						out success);
					if (success) {
						candidate = new ParameterizedType(candidateDef, result);
					} else {
						Log("Inference failed; ignoring candidate");
						continue;
					}
				}
				Log("Candidate type: " + candidate);
				
				if (lowerBounds.Count > 0) {
					// if there were lower bounds, we aim for the most specific candidate:
					
					// if this candidate isn't made redundant by an existing, more specific candidate:
					if (!candidateTypes.Any(c => c.GetDefinition().IsDerivedFrom(candidateDef, context))) {
						// remove all existing candidates made redundant by this candidate:
						candidateTypes.RemoveAll(c => candidateDef.IsDerivedFrom(c.GetDefinition(), context));
						// add new candidate
						candidateTypes.Add(candidate);
					}
				} else {
					// if there only were upper bounds, we aim for the least specific candidate:
					
					// if this candidate isn't made redundant by an existing, less specific candidate:
					if (!candidateTypes.Any(c => candidateDef.IsDerivedFrom(c.GetDefinition(), context))) {
						// remove all existing candidates made redundant by this candidate:
						candidateTypes.RemoveAll(c => c.GetDefinition().IsDerivedFrom(candidateDef, context));
						// add new candidate
						candidateTypes.Add(candidate);
					}
				}
			}
			Debug.Unindent();
			return candidateTypes;
		}
		#endregion
		
		[Conditional("DEBUG")]
		static void Log(string text)
		{
			Debug.WriteLine(text);
		}
		
		[Conditional("DEBUG")]
		static void Log<T>(string text, IEnumerable<T> lines)
		{
			#if DEBUG
			T[] arr = lines.ToArray();
			if (arr.Length == 0) {
				Log(text + "<empty collection>");
			} else {
				Log(text + (arr[0] != null ? arr[0].ToString() : "<null>"));
				for (int i = 1; i < arr.Length; i++) {
					Log(new string(' ', text.Length) + (arr[i] != null ? arr[i].ToString() : "<null>"));
				}
			}
			#endif
		}
	}
}
