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
using ICSharpCode.NRefactory.Utils;

namespace ICSharpCode.NRefactory.Semantics
{
	/// <summary>
	/// Resolve result representing an array creation.
	/// </summary>
	public class ArrayCreateResolveResult : ResolveResult
	{
		/// <summary>
		/// Gets the size arguments.
		/// </summary>
		public readonly IList<ResolveResult> SizeArguments;
		
		/// <summary>
		/// Gets the initializer elements.
		/// This field may be null if no initializer was specified.
		/// </summary>
		public readonly IList<ResolveResult> InitializerElements;
		
		public ArrayCreateResolveResult(IType arrayType, IList<ResolveResult> sizeArguments, IList<ResolveResult> initializerElements)
			: base(arrayType)
		{
			if (sizeArguments == null)
				throw new ArgumentNullException("sizeArguments");
			this.SizeArguments = sizeArguments;
			this.InitializerElements = initializerElements;
		}
		
		public override IEnumerable<ResolveResult> GetChildResults()
		{
			if (InitializerElements != null)
				return SizeArguments.Concat(InitializerElements);
			else
				return SizeArguments;
		}
	}
}
