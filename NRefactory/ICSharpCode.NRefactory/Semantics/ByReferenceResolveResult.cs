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
using ICSharpCode.NRefactory.TypeSystem;

namespace ICSharpCode.NRefactory.Semantics
{
	/// <summary>
	/// Represents the resolve result of an 'ref x' or 'out x' expression.
	/// </summary>
	public class ByReferenceResolveResult : ResolveResult
	{
		public bool IsOut { get; private set; }
		public bool IsRef { get { return !IsOut;} }
		
		public readonly ResolveResult ElementResult;
		
		public ByReferenceResolveResult(ResolveResult elementResult, bool isOut)
			: this(elementResult.Type, isOut)
		{
			this.ElementResult = elementResult;
		}
		
		public ByReferenceResolveResult(IType elementType, bool isOut)
			: base(new ByReferenceType(elementType))
		{
			this.IsOut = isOut;
		}
		
		public IType ElementType {
			get { return ((ByReferenceType)this.Type).ElementType; }
		}
		
		public override IEnumerable<ResolveResult> GetChildResults()
		{
			if (ElementResult != null)
				return new[] { ElementResult };
			else
				return Enumerable.Empty<ResolveResult>();
		}
		
		public override string ToString()
		{
			return string.Format(CultureInfo.InvariantCulture, "[{0} {1} {2}]", GetType().Name, IsOut ? "out" : "ref", ElementType);
		}
	}
}
