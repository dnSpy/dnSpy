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
using System.Globalization;
using System.Linq;
using ICSharpCode.NRefactory.Semantics;
using ICSharpCode.NRefactory.TypeSystem;

namespace ICSharpCode.NRefactory.CSharp.Resolver
{
	public enum DynamicInvocationType {
		/// <summary>
		/// The invocation is a normal invocation ( 'a(b)' ).
		/// </summary>
		Invocation,

		/// <summary>
		/// The invocation is an indexing ( 'a[b]' ).
		/// </summary>
		Indexing,

		/// <summary>
		/// The invocation is an object creation ( 'new a(b)' ). Also used when invoking a base constructor ( ' : base(a) ' ) and chaining constructors ( ' : this(a) ').
		/// </summary>
		ObjectCreation,
	}

	/// <summary>
	/// Represents the result of an invocation of a member of a dynamic object.
	/// </summary>
	public class DynamicInvocationResolveResult : ResolveResult
	{
		/// <summary>
		/// Target of the invocation. Can be a dynamic expression or a <see cref="MethodGroupResolveResult"/>.
		/// </summary>
		public readonly ResolveResult Target;

		/// <summary>
		/// Type of the invocation.
		/// </summary>
		public readonly DynamicInvocationType InvocationType;

		/// <summary>
		/// Arguments for the call. Named arguments will be instances of <see cref="NamedArgumentResolveResult"/>.
		/// </summary>
		public readonly IList<ResolveResult> Arguments; 

		/// <summary>
		/// Gets the list of initializer statements that are appplied to the result of this invocation.
		/// This is used to represent object and collection initializers.
		/// With the initializer statements, the <see cref="InitializedObjectResolveResult"/> is used
		/// to refer to the result of this invocation.
		/// Initializer statements can only exist if the <see cref="InvocationType"/> is <see cref="DynamicInvocationType.ObjectCreation"/>.
		/// </summary>
		public readonly IList<ResolveResult> InitializerStatements;

		public DynamicInvocationResolveResult(ResolveResult target, DynamicInvocationType invocationType, IList<ResolveResult> arguments, IList<ResolveResult> initializerStatements = null) : base(SpecialType.Dynamic) {
			this.Target                = target;
			this.InvocationType        = invocationType;
			this.Arguments             = arguments ?? EmptyList<ResolveResult>.Instance;
			this.InitializerStatements = initializerStatements ?? EmptyList<ResolveResult>.Instance;
		}

		public override string ToString()
		{
			return string.Format(CultureInfo.InvariantCulture, "[Dynamic invocation ]");
		}
	}
}
