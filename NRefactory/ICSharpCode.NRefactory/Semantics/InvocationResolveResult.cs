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

using ICSharpCode.NRefactory.TypeSystem;

namespace ICSharpCode.NRefactory.Semantics
{
	/// <summary>
	/// Represents the result of a method, constructor or indexer invocation.
	/// </summary>
	public class InvocationResolveResult : MemberResolveResult
	{
		/// <summary>
		/// Gets the arguments that are being passed to the method, in the order the arguments are being evaluated.
		/// </summary>
		public readonly IList<ResolveResult> Arguments;
		
		/// <summary>
		/// Gets the list of initializer statements that are appplied to the result of this invocation.
		/// This is used to represent object and collection initializers.
		/// With the initializer statements, the <see cref="InitializedObjectResolveResult"/> is used
		/// to refer to the result of this invocation.
		/// </summary>
		public readonly IList<ResolveResult> InitializerStatements;
		
		public InvocationResolveResult(ResolveResult targetResult, IParameterizedMember member,
		                               IList<ResolveResult> arguments = null,
		                               IList<ResolveResult> initializerStatements = null,
		                               IType returnTypeOverride = null)
			: base(targetResult, member, returnTypeOverride)
		{
			this.Arguments = arguments ?? EmptyList<ResolveResult>.Instance;
			this.InitializerStatements = initializerStatements ?? EmptyList<ResolveResult>.Instance;
		}
		
		public new IParameterizedMember Member {
			get { return (IParameterizedMember)base.Member; }
		}
		
		/// <summary>
		/// Gets the arguments in the order they are being passed to the method.
		/// For parameter arrays (params), this will return an ArrayCreateResolveResult.
		/// </summary>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate",
		                                                 Justification = "Derived methods may be expensive and create new lists")]
		public virtual IList<ResolveResult> GetArgumentsForCall()
		{
			return Arguments;
		}
		
		public override IEnumerable<ResolveResult> GetChildResults()
		{
			return base.GetChildResults().Concat(this.Arguments).Concat(this.InitializerStatements);
		}
	}
}
