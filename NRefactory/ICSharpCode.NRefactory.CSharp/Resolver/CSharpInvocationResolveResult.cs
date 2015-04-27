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
using System.Linq;
using ICSharpCode.NRefactory.Semantics;
using ICSharpCode.NRefactory.TypeSystem;

namespace ICSharpCode.NRefactory.CSharp.Resolver
{
	/// <summary>
	/// Represents the result of a method, constructor or indexer invocation.
	/// Provides additional C#-specific information for InvocationResolveResult.
	/// </summary>
	public class CSharpInvocationResolveResult : InvocationResolveResult
	{
		public readonly OverloadResolutionErrors OverloadResolutionErrors;
		
		/// <summary>
		/// Gets whether this invocation is calling an extension method using extension method syntax.
		/// </summary>
		public readonly bool IsExtensionMethodInvocation;
		
		/// <summary>
		/// Gets whether this invocation is calling a delegate (without explicitly calling ".Invoke()").
		/// </summary>
		public readonly bool IsDelegateInvocation;
		
		/// <summary>
		/// Gets whether a params-Array is being used in its expanded form.
		/// </summary>
		public readonly bool IsExpandedForm;
		
		readonly IList<int> argumentToParameterMap;

		/// <summary>
		/// If IsExtensionMethodInvocation is true this property holds the reduced method.
		/// </summary>
		IMethod reducedMethod;
		public IMethod ReducedMethod {
			get {
				if (!IsExtensionMethodInvocation)
					return null;
				if (reducedMethod == null && Member is IMethod)
					reducedMethod = new ReducedExtensionMethod ((IMethod)Member);
				return reducedMethod;
			}
		}
		
		public CSharpInvocationResolveResult(
			ResolveResult targetResult, IParameterizedMember member,
			IList<ResolveResult> arguments,
			OverloadResolutionErrors overloadResolutionErrors = OverloadResolutionErrors.None,
			bool isExtensionMethodInvocation = false,
			bool isExpandedForm = false,
			bool isDelegateInvocation = false,
			IList<int> argumentToParameterMap = null,
			IList<ResolveResult> initializerStatements = null,
			IType returnTypeOverride = null
		)
			: base(targetResult, member, arguments, initializerStatements, returnTypeOverride)
		{
			this.OverloadResolutionErrors = overloadResolutionErrors;
			this.IsExtensionMethodInvocation = isExtensionMethodInvocation;
			this.IsExpandedForm = isExpandedForm;
			this.IsDelegateInvocation = isDelegateInvocation;
			this.argumentToParameterMap = argumentToParameterMap;
		}
		
		public override bool IsError {
			get { return this.OverloadResolutionErrors != OverloadResolutionErrors.None; }
		}
		
		/// <summary>
		/// Gets an array that maps argument indices to parameter indices.
		/// For arguments that could not be mapped to any parameter, the value will be -1.
		/// 
		/// parameterIndex = ArgumentToParameterMap[argumentIndex]
		/// </summary>
		public IList<int> GetArgumentToParameterMap()
		{
			return argumentToParameterMap;
		}
		
		public override IList<ResolveResult> GetArgumentsForCall()
		{
			ResolveResult[] results = new ResolveResult[Member.Parameters.Count];
			List<ResolveResult> paramsArguments = IsExpandedForm ? new List<ResolveResult>() : null;
			// map arguments to parameters:
			for (int i = 0; i < Arguments.Count; i++) {
				int mappedTo;
				if (argumentToParameterMap != null)
					mappedTo = argumentToParameterMap[i];
				else
					mappedTo = IsExpandedForm ? Math.Min(i, results.Length - 1) : i;
				
				if (mappedTo >= 0 && mappedTo < results.Length) {
					if (IsExpandedForm && mappedTo == results.Length - 1) {
						paramsArguments.Add(Arguments[i]);
					} else {
						var narr = Arguments[i] as NamedArgumentResolveResult;
						if (narr != null)
							results[mappedTo] = narr.Argument;
						else
							results[mappedTo] = Arguments[i];
					}
				}
			}
			if (IsExpandedForm){
				IType arrayType = Member.Parameters.Last().Type;
				IType int32 = Member.Compilation.FindType(KnownTypeCode.Int32);
				ResolveResult[] sizeArguments = { new ConstantResolveResult(int32, paramsArguments.Count) };
				results[results.Length - 1] = new ArrayCreateResolveResult(arrayType, sizeArguments, paramsArguments);
			}
			
			for (int i = 0; i < results.Length; i++) {
				if (results[i] == null) {
					if (Member.Parameters[i].IsOptional) {
						results[i] = new ConstantResolveResult(Member.Parameters[i].Type, Member.Parameters[i].ConstantValue);
					} else {
						results[i] = ErrorResolveResult.UnknownError;
					}
				}
			}
			
			return results;
		}
	}
}
