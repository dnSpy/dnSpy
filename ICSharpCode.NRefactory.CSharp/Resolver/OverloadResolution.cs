// Copyright (c) AlphaSierraPapa for the SharpDevelop Team
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
using System.Diagnostics;
using System.Linq;
using System.Text;

using ICSharpCode.NRefactory.Semantics;
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.TypeSystem.Implementation;

namespace ICSharpCode.NRefactory.CSharp.Resolver
{
	/// <summary>
	/// C# overload resolution (C# 4.0 spec: §7.5).
	/// </summary>
	public class OverloadResolution
	{
		sealed class Candidate
		{
			public readonly IParameterizedMember Member;
			
			/// <summary>
			/// Returns the normal form candidate, if this is an expanded candidate.
			/// </summary>
			public readonly bool IsExpandedForm;
			
			/// <summary>
			/// Gets the parameter types. In the first step, these are the types without any substition.
			/// After type inference, substitutions will be performed.
			/// </summary>
			public readonly IType[] ParameterTypes;
			
			/// <summary>
			/// argument index -> parameter index; -1 for arguments that could not be mapped
			/// </summary>
			public int[] ArgumentToParameterMap;
			
			public OverloadResolutionErrors Errors;
			public int ErrorCount;
			
			public bool HasUnmappedOptionalParameters;
			
			public IType[] InferredTypes;
			
			/// <summary>
			/// Gets the original member parameters (before any substitution!)
			/// </summary>
			public readonly IList<IParameter> Parameters;
			
			/// <summary>
			/// Gets the original method type parameters (before any substitution!)
			/// </summary>
			public readonly IList<ITypeParameter> TypeParameters;
			
			/// <summary>
			/// Conversions applied to the arguments.
			/// This field is set by the CheckApplicability step.
			/// </summary>
			public Conversion[] ArgumentConversions;
			
			public bool IsGenericMethod {
				get {
					IMethod method = Member as IMethod;
					return method != null && method.TypeParameters.Count > 0;
				}
			}
			
			public int ArgumentsPassedToParamsArray {
				get {
					int count = 0;
					if (IsExpandedForm) {
						int paramsParameterIndex = this.Parameters.Count - 1;
						foreach (int parameterIndex in ArgumentToParameterMap) {
							if (parameterIndex == paramsParameterIndex)
								count++;
						}
					}
					return count;
				}
			}
			
			public Candidate(IParameterizedMember member, bool isExpanded)
			{
				this.Member = member;
				this.IsExpandedForm = isExpanded;
				IMethod method = member as IMethod;
				if (method != null && method.TypeParameters.Count > 0) {
					// For generic methods, go back to the original parameters
					// (without any type parameter substitution, not even class type parameters)
					// We'll re-substitute them as part of RunTypeInference().
					method = (IMethod)method.MemberDefinition;
					this.Parameters = method.Parameters;
					this.TypeParameters = method.TypeParameters;
				} else {
					this.Parameters = member.Parameters;
				}
				this.ParameterTypes = new IType[this.Parameters.Count];
			}
			
			public void AddError(OverloadResolutionErrors newError)
			{
				this.Errors |= newError;
				this.ErrorCount++;
			}
		}
		
		readonly ICompilation compilation;
		readonly ResolveResult[] arguments;
		readonly string[] argumentNames;
		readonly Conversions conversions;
		//List<Candidate> candidates = new List<Candidate>();
		Candidate bestCandidate;
		Candidate bestCandidateAmbiguousWith;
		IType[] explicitlyGivenTypeArguments;
		
		#region Constructor
		public OverloadResolution(ICompilation compilation, ResolveResult[] arguments, string[] argumentNames = null, IType[] typeArguments = null, Conversions conversions = null)
		{
			if (compilation == null)
				throw new ArgumentNullException("compilation");
			if (arguments == null)
				throw new ArgumentNullException("arguments");
			if (argumentNames == null)
				argumentNames = new string[arguments.Length];
			else if (argumentNames.Length != arguments.Length)
				throw new ArgumentException("argumentsNames.Length must be equal to arguments.Length");
			this.compilation = compilation;
			this.arguments = arguments;
			this.argumentNames = argumentNames;
			
			// keep explicitlyGivenTypeArguments==null when no type arguments were specified
			if (typeArguments != null && typeArguments.Length > 0)
				this.explicitlyGivenTypeArguments = typeArguments;
			
			this.conversions = conversions ?? Conversions.Get(compilation);
			this.AllowExpandingParams = true;
		}
		#endregion
		
		/// <summary>
		/// Gets/Sets whether the methods are extension methods that are being called using extension method syntax.
		/// </summary>
		/// <remarks>
		/// Setting this property to true restricts the possible conversions on the first argument to
		/// implicit identity, reference, or boxing conversions.
		/// </remarks>
		public bool IsExtensionMethodInvocation { get; set; }
		
		/// <summary>
		/// Gets/Sets whether expanding 'params' into individual elements is allowed.
		/// The default value is true.
		/// </summary>
		public bool AllowExpandingParams { get; set; }
		
		/// <summary>
		/// Gets the arguments for which this OverloadResolution instance was created.
		/// </summary>
		public IList<ResolveResult> Arguments {
			get { return arguments; }
		}
		
		#region AddCandidate
		public OverloadResolutionErrors AddCandidate(IParameterizedMember member)
		{
			return AddCandidate(member, OverloadResolutionErrors.None);
		}
		
		public OverloadResolutionErrors AddCandidate(IParameterizedMember member, OverloadResolutionErrors additionalErrors)
		{
			if (member == null)
				throw new ArgumentNullException("member");
			
			Candidate c = new Candidate(member, false);
			if (additionalErrors != OverloadResolutionErrors.None)
				c.AddError(additionalErrors);
			if (CalculateCandidate(c)) {
				//candidates.Add(c);
			}
			
			if (this.AllowExpandingParams && member.Parameters.Count > 0
			    && member.Parameters[member.Parameters.Count - 1].IsParams)
			{
				Candidate expandedCandidate = new Candidate(member, true);
				if (additionalErrors != OverloadResolutionErrors.None)
					expandedCandidate.AddError(additionalErrors);
				// consider expanded form only if it isn't obviously wrong
				if (CalculateCandidate(expandedCandidate)) {
					//candidates.Add(expandedCandidate);
					
					if (expandedCandidate.ErrorCount < c.ErrorCount)
						return expandedCandidate.Errors;
				}
			}
			return c.Errors;
		}
		
		public static bool IsApplicable(OverloadResolutionErrors errors)
		{
			return (errors & ~OverloadResolutionErrors.AmbiguousMatch) == OverloadResolutionErrors.None;
		}
		
		/// <summary>
		/// Calculates applicability etc. for the candidate.
		/// </summary>
		/// <returns>True if the calculation was successful, false if the candidate should be removed without reporting an error</returns>
		bool CalculateCandidate(Candidate candidate)
		{
			if (!ResolveParameterTypes(candidate))
				return false;
			MapCorrespondingParameters(candidate);
			RunTypeInference(candidate);
			CheckApplicability(candidate);
			ConsiderIfNewCandidateIsBest(candidate);
			return true;
		}
		
		bool ResolveParameterTypes(Candidate candidate)
		{
			for (int i = 0; i < candidate.Parameters.Count; i++) {
				IType type = candidate.Parameters[i].Type;
				if (candidate.IsExpandedForm && i == candidate.Parameters.Count - 1) {
					ArrayType arrayType = type as ArrayType;
					if (arrayType != null && arrayType.Dimensions == 1)
						type = arrayType.ElementType;
					else
						return false; // error: cannot unpack params-array. abort considering the expanded form for this candidate
				}
				candidate.ParameterTypes[i] = type;
			}
			return true;
		}
		#endregion
		
		#region AddMethodLists
		/// <summary>
		/// Adds all candidates from the method lists.
		/// 
		/// This method implements the logic that causes applicable methods in derived types to hide
		/// all methods in base types.
		/// </summary>
		/// <param name="methodLists">The methods, grouped by declaring type. Base types must come first in the list.</param>
		public void AddMethodLists(IList<MethodListWithDeclaringType> methodLists)
		{
			if (methodLists == null)
				throw new ArgumentNullException("methodLists");
			// Base types come first, so go through the list backwards (derived types first)
			bool[] isHiddenByDerivedType;
			if (methodLists.Count > 1)
				isHiddenByDerivedType = new bool[methodLists.Count];
			else
				isHiddenByDerivedType = null;
			for (int i = methodLists.Count - 1; i >= 0; i--) {
				if (isHiddenByDerivedType != null && isHiddenByDerivedType[i]) {
					Log.WriteLine("  Skipping methods in {0} because they are hidden by an applicable method in a derived type", methodLists[i].DeclaringType);
					continue;
				}
				
				MethodListWithDeclaringType methodList = methodLists[i];
				bool foundApplicableCandidateInCurrentList = false;
				
				for (int j = 0; j < methodList.Count; j++) {
					IParameterizedMember method = methodList[j];
					Log.Indent();
					OverloadResolutionErrors errors = AddCandidate(method);
					Log.Unindent();
					LogCandidateAddingResult("  Candidate", method, errors);
					
					foundApplicableCandidateInCurrentList |= IsApplicable(errors);
				}
				
				if (foundApplicableCandidateInCurrentList && i > 0) {
					foreach (IType baseType in methodList.DeclaringType.GetAllBaseTypes()) {
						for (int j = 0; j < i; j++) {
							if (!isHiddenByDerivedType[j] && baseType.Equals(methodLists[j].DeclaringType))
								isHiddenByDerivedType[j] = true;
						}
					}
				}
			}
		}
		
		[Conditional("DEBUG")]
		internal void LogCandidateAddingResult(string text, IParameterizedMember method, OverloadResolutionErrors errors)
		{
			#if DEBUG
			StringBuilder b = new StringBuilder(text);
			b.Append(' ');
			b.Append(method);
			b.Append(" = ");
			if (errors == OverloadResolutionErrors.None)
				b.Append("Success");
			else
				b.Append(errors);
			if (this.BestCandidate == method) {
				b.Append(" (best candidate so far)");
			} else if (this.BestCandidateAmbiguousWith == method) {
				b.Append(" (ambiguous)");
			}
			Log.WriteLine(b.ToString());
			#endif
		}
		#endregion
		
		#region MapCorrespondingParameters
		void MapCorrespondingParameters(Candidate candidate)
		{
			// C# 4.0 spec: §7.5.1.1 Corresponding parameters
			candidate.ArgumentToParameterMap = new int[arguments.Length];
			for (int i = 0; i < arguments.Length; i++) {
				candidate.ArgumentToParameterMap[i] = -1;
				if (argumentNames[i] == null) {
					// positional argument
					if (i < candidate.ParameterTypes.Length) {
						candidate.ArgumentToParameterMap[i] = i;
					} else if (candidate.IsExpandedForm) {
						candidate.ArgumentToParameterMap[i] = candidate.ParameterTypes.Length - 1;
					} else {
						candidate.AddError(OverloadResolutionErrors.TooManyPositionalArguments);
					}
				} else {
					// named argument
					for (int j = 0; j < candidate.Parameters.Count; j++) {
						if (argumentNames[i] == candidate.Parameters[j].Name) {
							candidate.ArgumentToParameterMap[i] = j;
						}
					}
					if (candidate.ArgumentToParameterMap[i] < 0)
						candidate.AddError(OverloadResolutionErrors.NoParameterFoundForNamedArgument);
				}
			}
		}
		#endregion
		
		#region RunTypeInference
		void RunTypeInference(Candidate candidate)
		{
			IMethod method = candidate.Member as IMethod;
			if (method == null || method.TypeParameters.Count == 0) {
				if (explicitlyGivenTypeArguments != null) {
					// method does not expect type arguments, but was given some
					candidate.AddError(OverloadResolutionErrors.WrongNumberOfTypeArguments);
				}
				return;
			}
			ParameterizedType parameterizedDeclaringType = candidate.Member.DeclaringType as ParameterizedType;
			IList<IType> classTypeArguments;
			if (parameterizedDeclaringType != null) {
				classTypeArguments = parameterizedDeclaringType.TypeArguments;
			} else {
				classTypeArguments = null;
			}
			// The method is generic:
			if (explicitlyGivenTypeArguments != null) {
				if (explicitlyGivenTypeArguments.Length == method.TypeParameters.Count) {
					candidate.InferredTypes = explicitlyGivenTypeArguments;
				} else {
					candidate.AddError(OverloadResolutionErrors.WrongNumberOfTypeArguments);
					// wrong number of type arguments given, so truncate the list or pad with UnknownType
					candidate.InferredTypes = new IType[method.TypeParameters.Count];
					for (int i = 0; i < candidate.InferredTypes.Length; i++) {
						if (i < explicitlyGivenTypeArguments.Length)
							candidate.InferredTypes[i] = explicitlyGivenTypeArguments[i];
						else
							candidate.InferredTypes[i] = SpecialType.UnknownType;
					}
				}
			} else {
				TypeInference ti = new TypeInference(compilation, conversions);
				bool success;
				candidate.InferredTypes = ti.InferTypeArguments(candidate.TypeParameters, arguments, candidate.ParameterTypes, out success, classTypeArguments);
				if (!success)
					candidate.AddError(OverloadResolutionErrors.TypeInferenceFailed);
			}
			// Now substitute in the formal parameters:
			var substitution = new ConstraintValidatingSubstitution(classTypeArguments, candidate.InferredTypes, this);
			for (int i = 0; i < candidate.ParameterTypes.Length; i++) {
				candidate.ParameterTypes[i] = candidate.ParameterTypes[i].AcceptVisitor(substitution);
			}
			if (!substitution.ConstraintsValid)
				candidate.AddError(OverloadResolutionErrors.ConstructedTypeDoesNotSatisfyConstraint);
		}
		
		sealed class ConstraintValidatingSubstitution : TypeParameterSubstitution
		{
			readonly Conversions conversions;
			public bool ConstraintsValid = true;
			
			public ConstraintValidatingSubstitution(IList<IType> classTypeArguments, IList<IType> methodTypeArguments, OverloadResolution overloadResolution)
				: base(classTypeArguments, methodTypeArguments)
			{
				this.conversions = overloadResolution.conversions;
			}
			
			public override IType VisitParameterizedType(ParameterizedType type)
			{
				IType newType = base.VisitParameterizedType(type);
				if (newType != type && ConstraintsValid) {
					// something was changed, so we need to validate the constraints
					ParameterizedType newParameterizedType = newType as ParameterizedType;
					if (newParameterizedType != null) {
						// C# 4.0 spec: §4.4.4 Satisfying constraints
						var typeParameters = newParameterizedType.GetDefinition().TypeParameters;
						for (int i = 0; i < typeParameters.Count; i++) {
							ITypeParameter tp = typeParameters[i];
							IType typeArg = newParameterizedType.GetTypeArgument(i);
							switch (typeArg.Kind) { // void, null, and pointers cannot be used as type arguments
								case TypeKind.Void:
								case TypeKind.Null:
								case TypeKind.Pointer:
									ConstraintsValid = false;
									break;
							}
							if (tp.HasReferenceTypeConstraint) {
								if (typeArg.IsReferenceType != true)
									ConstraintsValid = false;
							}
							if (tp.HasValueTypeConstraint) {
								if (!NullableType.IsNonNullableValueType(typeArg))
									ConstraintsValid = false;
							}
							if (tp.HasDefaultConstructorConstraint) {
								ITypeDefinition def = typeArg.GetDefinition();
								if (def != null && def.IsAbstract)
									ConstraintsValid = false;
								ConstraintsValid &= typeArg.GetConstructors(
									m => m.Parameters.Count == 0 && m.Accessibility == Accessibility.Public,
									GetMemberOptions.IgnoreInheritedMembers | GetMemberOptions.ReturnMemberDefinitions
								).Any();
							}
							foreach (IType constraintType in tp.DirectBaseTypes) {
								IType c = constraintType.AcceptVisitor(newParameterizedType.GetSubstitution());
								ConstraintsValid &= conversions.IsConstraintConvertible(typeArg, c);
							}
						}
					}
				}
				return newType;
			}
		}
		#endregion
		
		#region CheckApplicability
		void CheckApplicability(Candidate candidate)
		{
			// C# 4.0 spec: §7.5.3.1 Applicable function member
			
			// Test whether parameters were mapped the correct number of arguments:
			int[] argumentCountPerParameter = new int[candidate.ParameterTypes.Length];
			foreach (int parameterIndex in candidate.ArgumentToParameterMap) {
				if (parameterIndex >= 0)
					argumentCountPerParameter[parameterIndex]++;
			}
			for (int i = 0; i < argumentCountPerParameter.Length; i++) {
				if (candidate.IsExpandedForm && i == argumentCountPerParameter.Length - 1)
					continue; // any number of arguments is fine for the params-array
				if (argumentCountPerParameter[i] == 0) {
					if (candidate.Parameters[i].IsOptional)
						candidate.HasUnmappedOptionalParameters = true;
					else
						candidate.AddError(OverloadResolutionErrors.MissingArgumentForRequiredParameter);
				} else if (argumentCountPerParameter[i] > 1) {
					candidate.AddError(OverloadResolutionErrors.MultipleArgumentsForSingleParameter);
				}
			}
			
			candidate.ArgumentConversions = new Conversion[arguments.Length];
			// Test whether argument passing mode matches the parameter passing mode
			for (int i = 0; i < arguments.Length; i++) {
				int parameterIndex = candidate.ArgumentToParameterMap[i];
				if (parameterIndex < 0) continue;
				
				ByReferenceResolveResult brrr = arguments[i] as ByReferenceResolveResult;
				if (brrr != null) {
					if ((brrr.IsOut && !candidate.Parameters[parameterIndex].IsOut) || (brrr.IsRef && !candidate.Parameters[parameterIndex].IsRef))
						candidate.AddError(OverloadResolutionErrors.ParameterPassingModeMismatch);
				} else {
					if (candidate.Parameters[parameterIndex].IsOut || candidate.Parameters[parameterIndex].IsRef)
						candidate.AddError(OverloadResolutionErrors.ParameterPassingModeMismatch);
				}
				IType parameterType = candidate.ParameterTypes[parameterIndex];
				Conversion c = conversions.ImplicitConversion(arguments[i], parameterType);
				candidate.ArgumentConversions[i] = c;
				if (IsExtensionMethodInvocation && parameterIndex == 0) {
					// First parameter to extension method must be an identity, reference or boxing conversion
					if (!(c == Conversion.IdentityConversion || c == Conversion.ImplicitReferenceConversion || c == Conversion.BoxingConversion))
						candidate.AddError(OverloadResolutionErrors.ArgumentTypeMismatch);
				} else {
					if (!c)
						candidate.AddError(OverloadResolutionErrors.ArgumentTypeMismatch);
				}
			}
		}
		#endregion
		
		#region BetterFunctionMember
		/// <summary>
		/// Returns 1 if c1 is better than c2; 2 if c2 is better than c1; or 0 if neither is better.
		/// </summary>
		int BetterFunctionMember(Candidate c1, Candidate c2)
		{
			// prefer applicable members (part of heuristic that produces a best candidate even if none is applicable)
			if (c1.ErrorCount == 0 && c2.ErrorCount > 0)
				return 1;
			if (c1.ErrorCount > 0 && c2.ErrorCount == 0)
				return 2;
			
			// C# 4.0 spec: §7.5.3.2 Better function member
			bool c1IsBetter = false;
			bool c2IsBetter = false;
			for (int i = 0; i < arguments.Length; i++) {
				int p1 = c1.ArgumentToParameterMap[i];
				int p2 = c2.ArgumentToParameterMap[i];
				if (p1 >= 0 && p2 < 0) {
					c1IsBetter = true;
				} else if (p1 < 0 && p2 >= 0) {
					c2IsBetter = true;
				} else if (p1 >= 0 && p2 >= 0) {
					switch (conversions.BetterConversion(arguments[i], c1.ParameterTypes[p1], c2.ParameterTypes[p2])) {
						case 1:
							c1IsBetter = true;
							break;
						case 2:
							c2IsBetter = true;
							break;
					}
				}
			}
			if (c1IsBetter && !c2IsBetter)
				return 1;
			if (!c1IsBetter && c2IsBetter)
				return 2;
			
			// prefer members with less errors (part of heuristic that produces a best candidate even if none is applicable)
			if (c1.ErrorCount < c2.ErrorCount) return 1;
			if (c1.ErrorCount > c2.ErrorCount) return 2;
			
			if (!c1IsBetter && !c2IsBetter) {
				// we need the tie-breaking rules
				
				// non-generic methods are better
				if (!c1.IsGenericMethod && c2.IsGenericMethod)
					return 1;
				else if (c1.IsGenericMethod && !c2.IsGenericMethod)
					return 2;
				
				// non-expanded members are better
				if (!c1.IsExpandedForm && c2.IsExpandedForm)
					return 1;
				else if (c1.IsExpandedForm && !c2.IsExpandedForm)
					return 2;
				
				// prefer the member with less arguments mapped to the params-array
				int r = c1.ArgumentsPassedToParamsArray.CompareTo(c2.ArgumentsPassedToParamsArray);
				if (r < 0) return 1;
				else if (r > 0) return 2;
				
				// prefer the member where no default values need to be substituted
				if (!c1.HasUnmappedOptionalParameters && c2.HasUnmappedOptionalParameters)
					return 1;
				else if (c1.HasUnmappedOptionalParameters && !c2.HasUnmappedOptionalParameters)
					return 2;
				
				// compare the formal parameters
				r = MoreSpecificFormalParameters(c1, c2);
				if (r != 0)
					return r;
				
				// prefer non-lifted operators
				ILiftedOperator lift1 = c1.Member as ILiftedOperator;
				ILiftedOperator lift2 = c2.Member as ILiftedOperator;
				if (lift1 == null && lift2 != null)
					return 1;
				if (lift1 != null && lift2 == null)
					return 2;
			}
			return 0;
		}
		
		/// <summary>
		/// Implement this interface to give overload resolution a hint that the member represents a lifted operator,
		/// which is used in the tie-breaking rules.
		/// </summary>
		public interface ILiftedOperator : IParameterizedMember
		{
			IList<IParameter> NonLiftedParameters { get; }
		}
		
		int MoreSpecificFormalParameters(Candidate c1, Candidate c2)
		{
			// prefer the member with more formal parmeters (in case both have different number of optional parameters)
			int r = c1.Parameters.Count.CompareTo(c2.Parameters.Count);
			if (r > 0) return 1;
			else if (r < 0) return 2;
			
			return MoreSpecificFormalParameters(c1.Parameters.Select(p => p.Type), c2.Parameters.Select(p => p.Type));
		}
		
		static int MoreSpecificFormalParameters(IEnumerable<IType> t1, IEnumerable<IType> t2)
		{
			bool c1IsBetter = false;
			bool c2IsBetter = false;
			foreach (var pair in t1.Zip(t2, (a,b) => new { Item1 = a, Item2 = b })) {
				switch (MoreSpecificFormalParameter(pair.Item1, pair.Item2)) {
					case 1:
						c1IsBetter = true;
						break;
					case 2:
						c2IsBetter = true;
						break;
				}
			}
			if (c1IsBetter && !c2IsBetter)
				return 1;
			if (!c1IsBetter && c2IsBetter)
				return 2;
			return 0;
		}
		
		static int MoreSpecificFormalParameter(IType t1, IType t2)
		{
			if ((t1 is ITypeParameter) && !(t2 is ITypeParameter))
				return 2;
			if ((t2 is ITypeParameter) && !(t1 is ITypeParameter))
				return 1;
			
			ParameterizedType p1 = t1 as ParameterizedType;
			ParameterizedType p2 = t2 as ParameterizedType;
			if (p1 != null && p2 != null && p1.TypeParameterCount == p2.TypeParameterCount) {
				int r = MoreSpecificFormalParameters(p1.TypeArguments, p2.TypeArguments);
				if (r > 0)
					return r;
			}
			TypeWithElementType tew1 = t1 as TypeWithElementType;
			TypeWithElementType tew2 = t2 as TypeWithElementType;
			if (tew1 != null && tew2 != null) {
				return MoreSpecificFormalParameter(tew1.ElementType, tew2.ElementType);
			}
			return 0;
		}
		#endregion
		
		#region ConsiderIfNewCandidateIsBest
		void ConsiderIfNewCandidateIsBest(Candidate candidate)
		{
			if (bestCandidate == null) {
				bestCandidate = candidate;
			} else {
				switch (BetterFunctionMember(candidate, bestCandidate)) {
					case 0:
						// Overwrite 'bestCandidateAmbiguousWith' so that API users can
						// detect the set of all ambiguous methods if they look at
						// bestCandidateAmbiguousWith after each step.
						bestCandidateAmbiguousWith = candidate;
						break;
					case 1:
						bestCandidate = candidate;
						bestCandidateAmbiguousWith = null;
						break;
						// case 2: best candidate stays best
				}
			}
		}
		#endregion
		
		public IParameterizedMember BestCandidate {
			get { return bestCandidate != null ? bestCandidate.Member : null; }
		}
		
		public OverloadResolutionErrors BestCandidateErrors {
			get {
				if (bestCandidate == null)
					return OverloadResolutionErrors.None;
				OverloadResolutionErrors err = bestCandidate.Errors;
				if (bestCandidateAmbiguousWith != null)
					err |= OverloadResolutionErrors.AmbiguousMatch;
				return err;
			}
		}
		
		public bool FoundApplicableCandidate {
			get { return bestCandidate != null && bestCandidate.Errors == OverloadResolutionErrors.None; }
		}
		
		public IParameterizedMember BestCandidateAmbiguousWith {
			get { return bestCandidateAmbiguousWith != null ? bestCandidateAmbiguousWith.Member : null; }
		}
		
		public bool BestCandidateIsExpandedForm {
			get { return bestCandidate != null ? bestCandidate.IsExpandedForm : false; }
		}
		
		public bool IsAmbiguous {
			get { return bestCandidateAmbiguousWith != null; }
		}
		
		public IList<IType> InferredTypeArguments {
			get {
				if (bestCandidate != null && bestCandidate.InferredTypes != null)
					return bestCandidate.InferredTypes;
				else
					return EmptyList<IType>.Instance;
			}
		}
		
		/// <summary>
		/// Gets the implicit conversions that are being applied to the arguments.
		/// </summary>
		public IList<Conversion> ArgumentConversions {
			get {
				if (bestCandidate != null && bestCandidate.ArgumentConversions != null)
					return bestCandidate.ArgumentConversions;
				else
					return new Conversion[arguments.Length];
			}
		}
		
		/// <summary>
		/// Gets an array that maps argument indices to parameter indices.
		/// For arguments that could not be mapped to any parameter, the value will be -1.
		/// 
		/// parameterIndex = GetArgumentToParameterMap()[argumentIndex]
		/// </summary>
		public IList<int> GetArgumentToParameterMap()
		{
			if (bestCandidate != null)
				return bestCandidate.ArgumentToParameterMap;
			else
				return null;
		}
		
		public IList<ResolveResult> GetArgumentsWithConversions()
		{
			if (bestCandidate == null)
				return arguments;
			var conversions = this.ArgumentConversions;
			ResolveResult[] args = new ResolveResult[arguments.Length];
			for (int i = 0; i < args.Length; i++) {
				if (conversions[i] == Conversion.IdentityConversion || conversions[i] == Conversion.None) {
					args[i] = arguments[i];
				} else {
					int parameterIndex = bestCandidate.ArgumentToParameterMap[i];
					IType parameterType;
					if (parameterIndex >= 0)
						parameterType = bestCandidate.ParameterTypes[parameterIndex];
					else
						parameterType = SpecialType.UnknownType;
					args[i] = new ConversionResolveResult(parameterType, arguments[i], conversions[i]);
				}
			}
			return args;
		}
		
		public IParameterizedMember GetBestCandidateWithSubstitutedTypeArguments()
		{
			if (bestCandidate == null)
				return null;
			IMethod method = bestCandidate.Member as IMethod;
			if (method != null && method.TypeParameters.Count > 0) {
				return new SpecializedMethod(method.DeclaringType, (IMethod)method.MemberDefinition, bestCandidate.InferredTypes);
			} else {
				return bestCandidate.Member;
			}
		}
		
		public CSharpInvocationResolveResult CreateResolveResult(ResolveResult targetResolveResult)
		{
			IParameterizedMember member = GetBestCandidateWithSubstitutedTypeArguments();
			if (member == null)
				throw new InvalidOperationException();
			
			return new CSharpInvocationResolveResult(
				targetResolveResult,
				member,
				GetArgumentsWithConversions(),
				this.BestCandidateErrors,
				this.IsExtensionMethodInvocation,
				this.BestCandidateIsExpandedForm,
				member is ILiftedOperator,
				isDelegateInvocation: false,
				argumentToParameterMap: this.GetArgumentToParameterMap());
		}
	}
}
