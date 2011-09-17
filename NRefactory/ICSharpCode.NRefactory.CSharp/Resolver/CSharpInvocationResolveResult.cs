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
using ICSharpCode.NRefactory.Semantics;
using ICSharpCode.NRefactory.TypeSystem;

namespace ICSharpCode.NRefactory.CSharp.Resolver
{
	/// <summary>
	/// Represents the result of a method invocation.
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
		
		/// <summary>
		/// Gets whether this is a lifted operator invocation.
		/// </summary>
		public readonly bool IsLiftedOperatorInvocation;
		
		readonly IList<int> argumentToParameterMap;
		
		public CSharpInvocationResolveResult(
			ResolveResult targetResult, IParameterizedMember member, IType returnType,
			IList<ResolveResult> arguments,
			OverloadResolutionErrors overloadResolutionErrors = OverloadResolutionErrors.None,
			bool isExtensionMethodInvocation = false,
			bool isExpandedForm = false,
			bool isLiftedOperatorInvocation = false,
			bool isDelegateInvocation = false,
			IList<int> argumentToParameterMap = null)
			: base(targetResult, member, returnType, arguments)
		{
			this.OverloadResolutionErrors = overloadResolutionErrors;
			this.IsExtensionMethodInvocation = isExtensionMethodInvocation;
			this.IsExpandedForm = isExpandedForm;
			this.IsLiftedOperatorInvocation = isLiftedOperatorInvocation;
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
	}
}
