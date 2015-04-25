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
	/// Represents the result of resolving an expression.
	/// </summary>
	public class ResolveResult
	{
		readonly IType type;
		
		public ResolveResult(IType type)
		{
			if (type == null)
				throw new ArgumentNullException("type");
			this.type = type;
		}
		
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1721:PropertyNamesShouldNotMatchGetMethods",
		                                                 Justification = "Unrelated to object.GetType()")]
		public IType Type {
			get { return type; }
		}
		
		public virtual bool IsCompileTimeConstant {
			get { return false; }
		}
		
		public virtual object ConstantValue {
			get { return null; }
		}
		
		public virtual bool IsError {
			get { return false; }
		}
		
		public override string ToString()
		{
			return "[" + GetType().Name + " " + type + "]";
		}
		
		public virtual IEnumerable<ResolveResult> GetChildResults()
		{
			return Enumerable.Empty<ResolveResult>();
		}
		
		public virtual DomRegion GetDefinitionRegion()
		{
			return DomRegion.Empty;
		}
		
		public virtual ResolveResult ShallowClone()
		{
			return (ResolveResult)MemberwiseClone();
		}
	}
}
