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
using System.Linq;

using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.TypeSystem.Implementation;

namespace ICSharpCode.NRefactory.Semantics
{
	/// <summary>
	/// Represents the result of a method invocation.
	/// </summary>
	public abstract class InvocationResolveResult : MemberResolveResult
	{
		/// <summary>
		/// Gets the arguments that are being passed to the method, in the order the arguments are being evaluated.
		/// </summary>
		public readonly IList<ResolveResult> Arguments;
		
		public InvocationResolveResult(ResolveResult targetResult, IParameterizedMember member, IList<ResolveResult> arguments)
			: base(targetResult, member)
		{
			this.Arguments = arguments ?? EmptyList<ResolveResult>.Instance;
		}
		
		public new IParameterizedMember Member {
			get { return (IParameterizedMember)base.Member; }
		}
		
		/// <summary>
		/// Gets the arguments in the order they are being passed to the method.
		/// For parameter arrays (params), this will return an ArrayCreateResolveResult.
		/// </summary>
		public virtual IList<ResolveResult> GetArgumentsForCall()
		{
			return Arguments;
		}
		
		public override IEnumerable<ResolveResult> GetChildResults()
		{
			return base.GetChildResults().Concat(this.Arguments);
		}
	}
}
