// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Linq;
using System.Collections.Generic;

namespace ICSharpCode.SharpDevelop.Dom.CSharp
{
	/// <summary>
	/// Implements C# 3.0 overload resolution.
	/// </summary>
	public sealed class OverloadResolution
	{
		private OverloadResolution() {}
		
		public static IMethodOrProperty FindOverload(IEnumerable<IMethodOrProperty> list,
		                                             IReturnType[] arguments,
		                                             bool allowAdditionalArguments,
		                                             bool substituteInferredTypes,
		                                             out bool acceptableMatch)
		{
			OverloadResolution or = new OverloadResolution();
			or.candidates = list.Select(m => new Candidate(m)).ToList();
			or.arguments = arguments;
			or.allowAdditionalArguments = allowAdditionalArguments;
			
			if (or.candidates.Count == 0)
				throw new ArgumentException("at least one candidate is required");
			
			MemberLookupHelper.Log("OverloadResolution");
			MemberLookupHelper.Log("  arguments = ", arguments);
			
			or.ConstructExpandedForms();
			or.InferTypeArguments();
			or.CheckApplicability();
			
			Candidate result = or.FindBestCandidate();
			MemberLookupHelper.Log("Overload resolution finished. Winning candidate = " + result);
			acceptableMatch = result.Status == CandidateStatus.Success;
			if (substituteInferredTypes)
				return result.Method;
			else
				return result.OriginalMethod;
		}
		
		enum CandidateStatus
		{
			Success,
			WrongParameterCount,
			TypeInferenceFailed,
			NotApplicable
		}
		
		sealed class Candidate
		{
			public bool IsExpanded;
			public IMethodOrProperty Method;
			public IMethodOrProperty OriginalMethod;
			public CandidateStatus Status = CandidateStatus.Success;
			public int ApplicableArgumentCount;
			
			public IList<IParameter> Parameters {
				get { return Method.Parameters; }
			}
			
			public int TypeParameterCount {
				get {
					IMethod m = Method as IMethod;
					if (m != null)
						return m.TypeParameters.Count;
					else
						return 0;
				}
			}
			
			public Candidate(IMethodOrProperty method)
			{
				if (method == null)
					throw new ArgumentNullException("method");
				this.Method = method;
				this.OriginalMethod = method;
			}
			
			public override string ToString()
			{
				return "[Candidate: " + Method + ", Status=" + Status + "]";
			}
		}
		
		List<Candidate> candidates;
		IList<IReturnType> arguments;
		bool allowAdditionalArguments;
		
		/// <summary>
		/// For methods having a params-array as last parameter, expand the params array to
		/// n parameters of the element type and add those as new candidates.
		/// Mark candidates with the wrong parameter count as Status.WrongParameterCount.
		/// </summary>
		void ConstructExpandedForms()
		{
			LogStep("Step 1 (Construct expanded forms)");
			foreach (Candidate candidate in candidates.ToArray()) {
				if (candidate.Status == CandidateStatus.Success) {
					if (candidate.Parameters.Count > 0 && arguments.Count >= candidate.Parameters.Count - 1) {
						IParameter lastParameter = candidate.Parameters[candidate.Parameters.Count - 1];
						if (lastParameter.IsParams && lastParameter.ReturnType.IsArrayReturnType) {
							// try to construct an expanded form with the correct parameter count
							IReturnType elementType = lastParameter.ReturnType.CastToArrayReturnType().ArrayElementType;
							IMethodOrProperty expanded = (IMethodOrProperty)candidate.Method.CreateSpecializedMember();
							expanded.Parameters.RemoveAt(candidate.Parameters.Count - 1);
							int index = 0;
							while (expanded.Parameters.Count < arguments.Count) {
								expanded.Parameters.Add(new DefaultParameter(lastParameter.Name + (index++), elementType, lastParameter.Region));
							}
							candidates.Add(new Candidate(expanded) {
							               	IsExpanded = true,
							               	OriginalMethod = candidate.Method
							               });
						}
					}
					
					if (allowAdditionalArguments) {
						if (candidate.Parameters.Count < arguments.Count) {
							candidate.Status = CandidateStatus.WrongParameterCount;
						}
					} else {
						if (candidate.Parameters.Count != arguments.Count) {
							candidate.Status = CandidateStatus.WrongParameterCount;
						}
					}
				}
			}
		}
		
		/// <summary>
		/// Infer the type arguments for generic methods.
		/// </summary>
		void InferTypeArguments()
		{
			LogStep("Step 2 (Infer type arguments)");
			foreach (Candidate candidate in candidates) {
				IMethod method = candidate.Method as IMethod;
				if (method != null && method.TypeParameters.Count > 0
				    && candidate.Status == CandidateStatus.Success)
				{
					bool success;
					IReturnType[] typeArguments = TypeInference.InferTypeArguments(method, arguments, out success);
					if (!success) {
						candidate.Status = CandidateStatus.TypeInferenceFailed;
					}
					candidate.Method = ApplyTypeArgumentsToMethod(method, typeArguments);
				}
			}
		}
		
		static IMethod ApplyTypeArgumentsToMethod(IMethod genericMethod, IList<IReturnType> typeArguments)
		{
			if (typeArguments != null && typeArguments.Count > 0) {
				// apply inferred type arguments
				IMethod method = (IMethod)genericMethod.CreateSpecializedMember();
				method.ReturnType = ConstructedReturnType.TranslateType(method.ReturnType, typeArguments, true);
				for (int i = 0; i < method.Parameters.Count; ++i) {
					method.Parameters[i].ReturnType = ConstructedReturnType.TranslateType(method.Parameters[i].ReturnType, typeArguments, true);
				}
				for (int i = 0; i < Math.Min(typeArguments.Count, method.TypeParameters.Count); i++) {
					var tp = new BoundTypeParameter(method.TypeParameters[i], method.DeclaringType, method);
					tp.BoundTo = typeArguments[i];
					method.TypeParameters[i] = tp;
				}
				return method;
			} else {
				return genericMethod;
			}
		}
		
		void CheckApplicability()
		{
			LogStep("Step 3 (CheckApplicability)");
			foreach (Candidate candidate in candidates) {
				int c = Math.Min(arguments.Count, candidate.Parameters.Count);
				for (int i = 0; i < c; i++) {
					if (MemberLookupHelper.IsApplicable(arguments[i], candidate.Parameters[i], candidate.Method as IMethod))
						candidate.ApplicableArgumentCount++;
				}
				if (candidate.Status == CandidateStatus.Success && candidate.ApplicableArgumentCount < arguments.Count) {
					candidate.Status = CandidateStatus.NotApplicable;
				}
			}
		}
		
		Candidate FindBestCandidate()
		{
			LogStep("Step 4 (FindBestCandidate)");
			
			// Find a candidate that is better than all other candidates
			Candidate best = null;
			foreach (Candidate candidate in candidates) {
				if (candidate.Status == CandidateStatus.Success) {
					if (best == null || GetBetterFunctionMember(best, candidate) == 2) {
						best = candidate;
					}
				}
			}
			
			if (best != null)
				return best;
			
			// no successful candidate found:
			// find the candidate that is nearest to being applicable
			// first try only candidates with the correct parameter count
			foreach (Candidate candidate in candidates) {
				if (candidate.Status != CandidateStatus.WrongParameterCount) {
					if (best == null || candidate.ApplicableArgumentCount > best.ApplicableArgumentCount)
						best = candidate;
				}
			}
			if (best != null)
				return best;
			// if all candidates have the wrong parameter count, return the candidate
			// with the most applicable parameters.
			best = candidates[0];
			foreach (Candidate candidate in candidates) {
				if (candidate.ApplicableArgumentCount > best.ApplicableArgumentCount)
					best = candidate;
			}
			return best;
		}
		
		/// <summary>
		/// Gets which function member is better. (§ 14.4.2.2)
		/// </summary>
		/// <returns>0 if neither method is better. 1 if c1 is better. 2 if c2 is better.</returns>
		int GetBetterFunctionMember(Candidate c1, Candidate c2)
		{
			int length = Math.Min(Math.Min(c1.Parameters.Count, c2.Parameters.Count), arguments.Count);
			bool foundBetterParamIn1 = false;
			bool foundBetterParamIn2 = false;
			for (int i = 0; i < length; i++) {
				if (arguments[i] == null)
					continue;
				int res = MemberLookupHelper.GetBetterConversion(arguments[i], c1.Parameters[i].ReturnType, c2.Parameters[i].ReturnType);
				if (res == 1) foundBetterParamIn1 = true;
				if (res == 2) foundBetterParamIn2 = true;
			}
			if (foundBetterParamIn1 && !foundBetterParamIn2)
				return 1;
			if (foundBetterParamIn2 && !foundBetterParamIn1)
				return 2;
			if (foundBetterParamIn1 && foundBetterParamIn2)
				return 0; // ambigous
			// If none conversion is better than any other, it is possible that the
			// expanded parameter lists are the same:
			for (int i = 0; i < length; i++) {
				if (!object.Equals(c1.Parameters[i].ReturnType, c2.Parameters[i].ReturnType)) {
					// if expanded parameters are not the same, neither function member is better
					return 0;
				}
			}
			
			// the expanded parameters are the same, apply the tie-breaking rules from the spec:
			
			// if one method is generic and the other non-generic, the non-generic is better
			bool m1IsGeneric = c1.TypeParameterCount > 0;
			bool m2IsGeneric = c2.TypeParameterCount > 0;
			if (m1IsGeneric && !m2IsGeneric) return 2;
			if (m2IsGeneric && !m1IsGeneric) return 1;
			
			// for params parameters: non-expanded calls are better
			if (c1.IsExpanded && !c2.IsExpanded) return 2;
			if (c2.IsExpanded && !c1.IsExpanded) return 1;
			
			// if the number of parameters is different, the one with more parameters is better
			// this occurs when only when both methods are expanded
			if (c1.OriginalMethod.Parameters.Count > c2.OriginalMethod.Parameters.Count) return 1;
			if (c2.OriginalMethod.Parameters.Count > c1.OriginalMethod.Parameters.Count) return 2;
			
			IReturnType[] m1ParamTypes = new IReturnType[c1.Parameters.Count];
			IReturnType[] m2ParamTypes = new IReturnType[c2.Parameters.Count];
			for (int i = 0; i < m1ParamTypes.Length; i++) {
				m1ParamTypes[i] = c1.Parameters[i].ReturnType;
				m2ParamTypes[i] = c2.Parameters[i].ReturnType;
			}
			return GetMoreSpecific(m1ParamTypes, m2ParamTypes);
		}
		
		
		/// <summary>
		/// Gets which return type list is more specific.
		/// § 14.4.2.2: types with generic arguments are less specific than types with fixed arguments
		/// </summary>
		/// <returns>0 if both are equally specific, 1 if <paramref name="r"/> is more specific,
		/// 2 if <paramref name="s"/> is more specific.</returns>
		static int GetMoreSpecific(IList<IReturnType> r, IList<IReturnType> s)
		{
			bool foundMoreSpecificParamIn1 = false;
			bool foundMoreSpecificParamIn2 = false;
			int length = Math.Min(r.Count, s.Count);
			for (int i = 0; i < length; i++) {
				int res = GetMoreSpecific(r[i], s[i]);
				if (res == 1) foundMoreSpecificParamIn1 = true;
				if (res == 2) foundMoreSpecificParamIn2 = true;
			}
			if (foundMoreSpecificParamIn1 && !foundMoreSpecificParamIn2)
				return 1;
			if (foundMoreSpecificParamIn2 && !foundMoreSpecificParamIn1)
				return 2;
			return 0;
		}
		
		static int GetMoreSpecific(IReturnType r, IReturnType s)
		{
			if (r == null && s == null) return 0;
			if (r == null) return 2;
			if (s == null) return 1;
			if (r.IsGenericReturnType && !(s.IsGenericReturnType))
				return 2;
			if (s.IsGenericReturnType && !(r.IsGenericReturnType))
				return 1;
			if (r.IsArrayReturnType && s.IsArrayReturnType)
				return GetMoreSpecific(r.CastToArrayReturnType().ArrayElementType, s.CastToArrayReturnType().ArrayElementType);
			if (r.IsConstructedReturnType && s.IsConstructedReturnType)
				return GetMoreSpecific(r.CastToConstructedReturnType().TypeArguments, s.CastToConstructedReturnType().TypeArguments);
			return 0;
		}
		
		[System.Diagnostics.ConditionalAttribute("DEBUG")]
		void LogStep(string title)
		{
			MemberLookupHelper.Log("  candidates = ", candidates);
			MemberLookupHelper.Log("Overload resolution (" + title + ")");
		}
		
		[System.Diagnostics.ConditionalAttribute("DEBUG")]
		void Log(string text)
		{
			MemberLookupHelper.Log(text);
		}
	}
}
