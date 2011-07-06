// Copyright (c) 2010 AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Linq;

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
			
			public readonly IType[] ParameterTypes;
			
			/// <summary>
			/// argument index -> parameter index; -1 for arguments that could not be mapped
			/// </summary>
			public int[] ArgumentToParameterMap;
			
			public OverloadResolutionErrors Errors;
			public int ErrorCount;
			
			public bool HasUnmappedOptionalParameters;
			
			public IType[] InferredTypes;
			
			public IList<IParameter> Parameters { get { return Member.Parameters; } }
			
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
				this.ParameterTypes = new IType[member.Parameters.Count];
			}
			
			public void AddError(OverloadResolutionErrors newError)
			{
				this.Errors |= newError;
				this.ErrorCount++;
			}
		}
		
		readonly ITypeResolveContext context;
		readonly ResolveResult[] arguments;
		readonly string[] argumentNames;
		readonly Conversions conversions;
		//List<Candidate> candidates = new List<Candidate>();
		Candidate bestCandidate;
		Candidate bestCandidateAmbiguousWith;
		IType[] explicitlyGivenTypeArguments;
		
		#region Constructor
		public OverloadResolution(ITypeResolveContext context, ResolveResult[] arguments, string[] argumentNames = null, IType[] typeArguments = null)
		{
			if (context == null)
				throw new ArgumentNullException("context");
			if (arguments == null)
				throw new ArgumentNullException("arguments");
			if (argumentNames == null)
				argumentNames = new string[arguments.Length];
			else if (argumentNames.Length != arguments.Length)
				throw new ArgumentException("argumentsNames.Length must be equal to arguments.Length");
			this.context = context;
			this.arguments = arguments;
			this.argumentNames = argumentNames;
			
			// keep explicitlyGivenTypeArguments==null when no type arguments were specified
			if (typeArguments != null && typeArguments.Length > 0)
				this.explicitlyGivenTypeArguments = typeArguments;
			
			this.conversions = new Conversions(context);
		}
		#endregion
		
		#region AddCandidate
		public OverloadResolutionErrors AddCandidate(IParameterizedMember member)
		{
			if (member == null)
				throw new ArgumentNullException("member");
			
			Candidate c = new Candidate(member, false);
			if (CalculateCandidate(c)) {
				//candidates.Add(c);
			}
			
			if (member.Parameters.Count > 0 && member.Parameters[member.Parameters.Count - 1].IsParams) {
				Candidate expandedCandidate = new Candidate(member, true);
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
				IType type = candidate.Parameters[i].Type.Resolve(context);
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
					for (int j = 0; j < candidate.Member.Parameters.Count; j++) {
						if (argumentNames[i] == candidate.Member.Parameters[j].Name) {
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
							candidate.InferredTypes[i] = SharedTypes.UnknownType;
					}
				}
			} else {
				TypeInference ti = new TypeInference(context, conversions);
				bool success;
				candidate.InferredTypes = ti.InferTypeArguments(method.TypeParameters, arguments, candidate.ParameterTypes, out success);
				if (!success)
					candidate.AddError(OverloadResolutionErrors.TypeInferenceFailed);
			}
			// Now substitute in the formal parameters:
			var substitution = new ConstraintValidatingSubstitution(candidate.InferredTypes, this);
			for (int i = 0; i < candidate.ParameterTypes.Length; i++) {
				candidate.ParameterTypes[i] = candidate.ParameterTypes[i].AcceptVisitor(substitution);
			}
			if (!substitution.ConstraintsValid)
				candidate.AddError(OverloadResolutionErrors.ConstructedTypeDoesNotSatisfyConstraint);
		}
		
		sealed class ConstraintValidatingSubstitution : MethodTypeParameterSubstitution
		{
			readonly OverloadResolution overloadResolution;
			public bool ConstraintsValid = true;
			
			public ConstraintValidatingSubstitution(IType[] typeArguments, OverloadResolution overloadResolution)
				: base(typeArguments)
			{
				this.overloadResolution = overloadResolution;
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
							IType typeArg = newParameterizedType.TypeArguments[i];
							if (tp.HasReferenceTypeConstraint) {
								if (typeArg.IsReferenceType(overloadResolution.context) != true)
									ConstraintsValid = false;
							}
							if (tp.HasValueTypeConstraint) {
								if (typeArg.IsReferenceType(overloadResolution.context) != false)
									ConstraintsValid = false;
								if (NullableType.IsNullable(typeArg))
									ConstraintsValid = false;
							}
							if (tp.HasDefaultConstructorConstraint) {
								ITypeDefinition def = typeArg.GetDefinition();
								if (def != null && def.IsAbstract)
									ConstraintsValid = false;
								ConstraintsValid &= typeArg.GetConstructors(
									overloadResolution.context,
									m => m.Parameters.Count == 0 && m.Accessibility == Accessibility.Public
								).Any();
							}
							foreach (IType constraintType in tp.Constraints) {
								IType c = newParameterizedType.SubstituteInType(constraintType);
								ConstraintsValid &= overloadResolution.IsConstraintConvertible(typeArg, c);
							}
						}
					}
				}
				return newType;
			}
		}
		
		bool IsConstraintConvertible(IType typeArg, IType constraintType)
		{
			// TODO: this isn't exactly correct; not all kinds of implicit conversions are allowed here
			return conversions.ImplicitConversion(typeArg, constraintType);
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
				if (!conversions.ImplicitConversion(arguments[i], candidate.ParameterTypes[parameterIndex]))
					candidate.AddError(OverloadResolutionErrors.ArgumentTypeMismatch);
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
			
			return MoreSpecificFormalParameters(c1.Parameters.Select(p => p.Type.Resolve(context)),
			                                    c2.Parameters.Select(p => p.Type.Resolve(context)));
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
						if (bestCandidateAmbiguousWith == null)
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
					return Array.AsReadOnly(bestCandidate.InferredTypes);
				else
					return EmptyList<IType>.Instance;
			}
		}
	}
}
