// Copyright (c) 2010-2013 AlphaSierraPapa for the SharpDevelop Team
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
				IParameterizedMember memberDefinition = (IParameterizedMember)member.MemberDefinition;
				// For specificialized methods, go back to the original parameters:
				// (without any type parameter substitution, not even class type parameters)
				// We'll re-substitute them as part of RunTypeInference().
				this.Parameters = memberDefinition.Parameters;
				IMethod methodDefinition = memberDefinition as IMethod;
				if (methodDefinition != null && methodDefinition.TypeParameters.Count > 0) {
					this.TypeParameters = methodDefinition.TypeParameters;
				}
				this.ParameterTypes = new IType[this.Parameters.Count];
			}
			
			public void AddError(OverloadResolutionErrors newError)
			{
				this.Errors |= newError;
				if (!IsApplicable(newError))
					this.ErrorCount++;
			}
		}
		
		readonly ICompilation compilation;
		readonly ResolveResult[] arguments;
		readonly string[] argumentNames;
		readonly CSharpConversions conversions;
		//List<Candidate> candidates = new List<Candidate>();
		Candidate bestCandidate;
		Candidate bestCandidateAmbiguousWith;
		IType[] explicitlyGivenTypeArguments;
		bool bestCandidateWasValidated;
		OverloadResolutionErrors bestCandidateValidationResult;
		
		#region Constructor
		public OverloadResolution(ICompilation compilation, ResolveResult[] arguments, string[] argumentNames = null, IType[] typeArguments = null, CSharpConversions conversions = null)
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
			
			this.conversions = conversions ?? CSharpConversions.Get(compilation);
			this.AllowExpandingParams = true;
			this.AllowOptionalParameters = true;
		}
		#endregion
		
		#region Input Properties
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
		/// Gets/Sets whether optional parameters may be left at their default value.
		/// The default value is true.
		/// If this property is set to false, optional parameters will be treated like regular parameters.
		/// </summary>
		public bool AllowOptionalParameters { get; set; }
		
		/// <summary>
		/// Gets/Sets whether ConversionResolveResults created by this OverloadResolution
		/// instance apply overflow checking.
		/// The default value is false.
		/// </summary>
		public bool CheckForOverflow { get; set; }
		
		/// <summary>
		/// Gets the arguments for which this OverloadResolution instance was created.
		/// </summary>
		public IList<ResolveResult> Arguments {
			get { return arguments; }
		}
		#endregion
		
		#region AddCandidate
		/// <summary>
		/// Adds a candidate to overload resolution.
		/// </summary>
		/// <param name="member">The candidate member to add.</param>
		/// <returns>The errors that prevent the member from being applicable, if any.
		/// Note: this method does not return errors that do not affect applicability.</returns>
		public OverloadResolutionErrors AddCandidate(IParameterizedMember member)
		{
			return AddCandidate(member, OverloadResolutionErrors.None);
		}
		
		/// <summary>
		/// Adds a candidate to overload resolution.
		/// </summary>
		/// <param name="member">The candidate member to add.</param>
		/// <param name="additionalErrors">Additional errors that apply to the candidate.
		/// This is used to represent errors during member lookup (e.g. OverloadResolutionErrors.Inaccessible)
		/// in overload resolution.</param>
		/// <returns>The errors that prevent the member from being applicable, if any.
		/// Note: this method does not return errors that do not affect applicability.</returns>
		public OverloadResolutionErrors AddCandidate(IParameterizedMember member, OverloadResolutionErrors additionalErrors)
		{
			if (member == null)
				throw new ArgumentNullException("member");
			
			Candidate c = new Candidate(member, false);
			c.AddError(additionalErrors);
			if (CalculateCandidate(c)) {
				//candidates.Add(c);
			}
			
			if (this.AllowExpandingParams && member.Parameters.Count > 0
			    && member.Parameters[member.Parameters.Count - 1].IsParams)
			{
				Candidate expandedCandidate = new Candidate(member, true);
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
		
		/// <summary>
		/// Calculates applicability etc. for the candidate.
		/// </summary>
		/// <returns>True if the calculation was successful, false if the candidate should be removed without reporting an error</returns>
		bool CalculateCandidate(Candidate candidate)
		{
			if (!ResolveParameterTypes(candidate, false))
				return false;
			MapCorrespondingParameters(candidate);
			RunTypeInference(candidate);
			CheckApplicability(candidate);
			ConsiderIfNewCandidateIsBest(candidate);
			return true;
		}
		
		bool ResolveParameterTypes(Candidate candidate, bool useSpecializedParameters)
		{
			for (int i = 0; i < candidate.Parameters.Count; i++) {
				IType type;
				if (useSpecializedParameters) {
					// Use the parameter type of the specialized non-generic method or indexer
					Debug.Assert(!candidate.IsGenericMethod);
					type = candidate.Member.Parameters[i].Type;
				} else {
					// Use the type of the original formal parameter
					type = candidate.Parameters[i].Type;
				}
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
			Log.WriteLine(string.Format("{0} {1} = {2}{3}",
			                            text, method,
			                            errors == OverloadResolutionErrors.None ? "Success" : errors.ToString(),
			                            this.BestCandidate == method ? " (best candidate so far)" :
			                            this.BestCandidateAmbiguousWith == method ? " (ambiguous)" : ""
			                           ));
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
			if (candidate.TypeParameters == null) {
				if (explicitlyGivenTypeArguments != null) {
					// method does not expect type arguments, but was given some
					candidate.AddError(OverloadResolutionErrors.WrongNumberOfTypeArguments);
				}
				// Grab new parameter types:
				ResolveParameterTypes(candidate, true);
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
				if (explicitlyGivenTypeArguments.Length == candidate.TypeParameters.Count) {
					candidate.InferredTypes = explicitlyGivenTypeArguments;
				} else {
					candidate.AddError(OverloadResolutionErrors.WrongNumberOfTypeArguments);
					// wrong number of type arguments given, so truncate the list or pad with UnknownType
					candidate.InferredTypes = new IType[candidate.TypeParameters.Count];
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
			readonly CSharpConversions conversions;
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
						var substitution = newParameterizedType.GetSubstitution();
						for (int i = 0; i < typeParameters.Count; i++) {
							if (!ValidateConstraints(typeParameters[i], newParameterizedType.GetTypeArgument(i), substitution, conversions)) {
								ConstraintsValid = false;
								break;
							}
						}
					}
				}
				return newType;
			}
		}
		#endregion
		
		#region Validate Constraints
		OverloadResolutionErrors ValidateMethodConstraints(Candidate candidate)
		{
			// If type inference already failed, we won't check the constraints:
			if ((candidate.Errors & OverloadResolutionErrors.TypeInferenceFailed) != 0)
				return OverloadResolutionErrors.None;
			
			if (candidate.TypeParameters == null || candidate.TypeParameters.Count == 0)
				return OverloadResolutionErrors.None; // the method isn't generic
			var substitution = GetSubstitution(candidate);
			for (int i = 0; i < candidate.TypeParameters.Count; i++) {
				if (!ValidateConstraints(candidate.TypeParameters[i], substitution.MethodTypeArguments[i], substitution))
					return OverloadResolutionErrors.MethodConstraintsNotSatisfied;
			}
			return OverloadResolutionErrors.None;
		}
		
		/// <summary>
		/// Validates whether the given type argument satisfies the constraints for the given type parameter.
		/// </summary>
		/// <param name="typeParameter">The type parameter.</param>
		/// <param name="typeArgument">The type argument.</param>
		/// <param name="substitution">The substitution that defines how type parameters are replaced with type arguments.
		/// The substitution is used to check constraints that depend on other type parameters (or recursively on the same type parameter).
		/// May be null if no substitution should be used.</param>
		/// <returns>True if the constraints are satisfied; false otherwise.</returns>
		public static bool ValidateConstraints(ITypeParameter typeParameter, IType typeArgument, TypeVisitor substitution = null)
		{
			if (typeParameter == null)
				throw new ArgumentNullException("typeParameter");
			if (typeArgument == null)
				throw new ArgumentNullException("typeArgument");
			return ValidateConstraints(typeParameter, typeArgument, substitution, CSharpConversions.Get(typeParameter.Owner.Compilation));
		}
		
		internal static bool ValidateConstraints(ITypeParameter typeParameter, IType typeArgument, TypeVisitor substitution, CSharpConversions conversions)
		{
			switch (typeArgument.Kind) { // void, null, and pointers cannot be used as type arguments
				case TypeKind.Void:
				case TypeKind.Null:
				case TypeKind.Pointer:
					return false;
			}
			if (typeParameter.HasReferenceTypeConstraint) {
				if (typeArgument.IsReferenceType != true)
					return false;
			}
			if (typeParameter.HasValueTypeConstraint) {
				if (!NullableType.IsNonNullableValueType(typeArgument))
					return false;
			}
			if (typeParameter.HasDefaultConstructorConstraint) {
				ITypeDefinition def = typeArgument.GetDefinition();
				if (def != null && def.IsAbstract)
					return false;
				var ctors = typeArgument.GetConstructors(
					m => m.Parameters.Count == 0 && m.Accessibility == Accessibility.Public,
					GetMemberOptions.IgnoreInheritedMembers | GetMemberOptions.ReturnMemberDefinitions
				);
				if (!ctors.Any())
					return false;
			}
			foreach (IType constraintType in typeParameter.DirectBaseTypes) {
				IType c = constraintType;
				if (substitution != null)
					c = c.AcceptVisitor(substitution);
				if (!conversions.IsConstraintConvertible(typeArgument, c))
					return false;
			}
			return true;
		}
		#endregion
		
		#region CheckApplicability
		/// <summary>
		/// Returns whether a candidate with the given errors is still considered to be applicable.
		/// </summary>
		public static bool IsApplicable(OverloadResolutionErrors errors)
		{
			const OverloadResolutionErrors errorsThatDoNotMatterForApplicability =
				OverloadResolutionErrors.AmbiguousMatch | OverloadResolutionErrors.MethodConstraintsNotSatisfied;
			return (errors & ~errorsThatDoNotMatterForApplicability) == OverloadResolutionErrors.None;
		}
		
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
					if (this.AllowOptionalParameters && candidate.Parameters[i].IsOptional)
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
				if (parameterIndex < 0) {
					candidate.ArgumentConversions[i] = Conversion.None;
					continue;
				}
				
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
					if ((!c.IsValid && !c.IsUserDefined && !c.IsMethodGroupConversion) && parameterType.Kind != TypeKind.Unknown)
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
				bestCandidateWasValidated = false;
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
						bestCandidateWasValidated = false;
						bestCandidateAmbiguousWith = null;
						break;
						// case 2: best candidate stays best
				}
			}
		}
		#endregion
		
		#region Output Properties
		public IParameterizedMember BestCandidate {
			get { return bestCandidate != null ? bestCandidate.Member : null; }
		}
		
		/// <summary>
		/// Returns the errors that apply to the best candidate.
		/// This includes additional errors that do not affect applicability (e.g. AmbiguousMatch, MethodConstraintsNotSatisfied)
		/// </summary>
		public OverloadResolutionErrors BestCandidateErrors {
			get {
				if (bestCandidate == null)
					return OverloadResolutionErrors.None;
				if (!bestCandidateWasValidated) {
					bestCandidateValidationResult = ValidateMethodConstraints(bestCandidate);
					bestCandidateWasValidated = true;
				}
				OverloadResolutionErrors err = bestCandidate.Errors | bestCandidateValidationResult;
				if (bestCandidateAmbiguousWith != null)
					err |= OverloadResolutionErrors.AmbiguousMatch;
				return err;
			}
		}
		
		public bool FoundApplicableCandidate {
			get { return bestCandidate != null && IsApplicable(bestCandidate.Errors); }
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
					return Enumerable.Repeat(Conversion.None, arguments.Length).ToList();
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
		
		/// <summary>
		/// Returns the arguments for the method call in the order they were provided (not in the order of the parameters).
		/// Arguments are wrapped in a <see cref="ConversionResolveResult"/> if an implicit conversion is being applied
		/// to them when calling the method.
		/// </summary>
		public IList<ResolveResult> GetArgumentsWithConversions()
		{
			if (bestCandidate == null)
				return arguments;
			else
				return GetArgumentsWithConversions(null, null);
		}
		
		/// <summary>
		/// Returns the arguments for the method call in the order they were provided (not in the order of the parameters).
		/// Arguments are wrapped in a <see cref="ConversionResolveResult"/> if an implicit conversion is being applied
		/// to them when calling the method.
		/// For arguments where an explicit argument name was provided, the argument will
		/// be wrapped in a <see cref="NamedArgumentResolveResult"/>.
		/// </summary>
		public IList<ResolveResult> GetArgumentsWithConversionsAndNames()
		{
			if (bestCandidate == null)
				return arguments;
			else
				return GetArgumentsWithConversions(null, GetBestCandidateWithSubstitutedTypeArguments());
		}
		
		IList<ResolveResult> GetArgumentsWithConversions(ResolveResult targetResolveResult, IParameterizedMember bestCandidateForNamedArguments)
		{
			var conversions = this.ArgumentConversions;
			ResolveResult[] args = new ResolveResult[arguments.Length];
			for (int i = 0; i < args.Length; i++) {
				var argument = arguments[i];
				if (this.IsExtensionMethodInvocation && i == 0 && targetResolveResult != null)
					argument = targetResolveResult;
				int parameterIndex = bestCandidate.ArgumentToParameterMap[i];
				if (parameterIndex >= 0 && conversions[i] != Conversion.IdentityConversion) {
					// Wrap argument in ConversionResolveResult
					IType parameterType = bestCandidate.ParameterTypes[parameterIndex];
					if (parameterType.Kind != TypeKind.Unknown) {
						if (arguments[i].IsCompileTimeConstant && conversions[i].IsValid && !conversions[i].IsUserDefined) {
							argument = new CSharpResolver(compilation).WithCheckForOverflow(CheckForOverflow).ResolveCast(parameterType, argument);
						} else {
							argument = new ConversionResolveResult(parameterType, argument, conversions[i], CheckForOverflow);
						}
					}
				}
				if (bestCandidateForNamedArguments != null && argumentNames[i] != null) {
					// Wrap argument in NamedArgumentResolveResult
					if (parameterIndex >= 0) {
						argument = new NamedArgumentResolveResult(bestCandidateForNamedArguments.Parameters[parameterIndex], argument, bestCandidateForNamedArguments);
					} else {
						argument = new NamedArgumentResolveResult(argumentNames[i], argument);
					}
				}
				args[i] = argument;
			}
			return args;
		}
		
		public IParameterizedMember GetBestCandidateWithSubstitutedTypeArguments()
		{
			if (bestCandidate == null)
				return null;
			IMethod method = bestCandidate.Member as IMethod;
			if (method != null && method.TypeParameters.Count > 0) {
				return ((IMethod)method.MemberDefinition).Specialize(GetSubstitution(bestCandidate));
			} else {
				return bestCandidate.Member;
			}
		}
		
		TypeParameterSubstitution GetSubstitution(Candidate candidate)
		{
			// Do not compose the substitutions, but merge them.
			// This is required for InvocationTests.SubstituteClassAndMethodTypeParametersAtOnce
			return new TypeParameterSubstitution(candidate.Member.Substitution.ClassTypeArguments, candidate.InferredTypes);
		}
		
		/// <summary>
		/// Creates a ResolveResult representing the result of overload resolution.
		/// </summary>
		/// <param name="targetResolveResult">
		/// The target expression of the call. May be <c>null</c> for static methods/constructors.
		/// </param>
		/// <param name="initializerStatements">
		/// Statements for Objects/Collections initializer.
		/// <see cref="InvocationResolveResult.InitializerStatements"/>
		/// <param name="returnTypeOverride">
		/// If not null, use this instead of the ReturnType of the member as the type of the created resolve result.
		/// </param>
		public CSharpInvocationResolveResult CreateResolveResult(ResolveResult targetResolveResult, IList<ResolveResult> initializerStatements = null, IType returnTypeOverride = null)
		{
			IParameterizedMember member = GetBestCandidateWithSubstitutedTypeArguments();
			if (member == null)
				throw new InvalidOperationException();

			return new CSharpInvocationResolveResult(
				this.IsExtensionMethodInvocation ? new TypeResolveResult(member.DeclaringType) : targetResolveResult,
				member,
				GetArgumentsWithConversions(targetResolveResult, member),
				this.BestCandidateErrors,
				this.IsExtensionMethodInvocation,
				this.BestCandidateIsExpandedForm,
				isDelegateInvocation: false,
				argumentToParameterMap: this.GetArgumentToParameterMap(),
				initializerStatements: initializerStatements,
				returnTypeOverride: returnTypeOverride);
		}
		#endregion
	}
}
