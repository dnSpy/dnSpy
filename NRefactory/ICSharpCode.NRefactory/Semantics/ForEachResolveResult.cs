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
using ICSharpCode.NRefactory.TypeSystem;

namespace ICSharpCode.NRefactory.Semantics
{
	/// <summary>
	/// Resolve result representing a 'foreach' loop.
	/// </summary>
	public class ForEachResolveResult : ResolveResult
	{
		/// <summary>
		/// Gets the semantic tree for the call to GetEnumerator.
		/// </summary>
		public readonly ResolveResult GetEnumeratorCall;
		
		/// <summary>
		/// Gets the collection type.
		/// </summary>
		public readonly IType CollectionType;
		
		/// <summary>
		/// Gets the enumerator type.
		/// </summary>
		public readonly IType EnumeratorType;
		
		/// <summary>
		/// Gets the element type.
		/// This is the type that would be inferred for an implicitly-typed element variable.
		/// For explicitly-typed element variables, this type may differ from <c>ElementVariable.Type</c>.
		/// </summary>
		public readonly IType ElementType;
		
		/// <summary>
		/// Gets the element variable.
		/// </summary>
		public readonly IVariable ElementVariable;
		
		/// <summary>
		/// Gets the Current property on the IEnumerator.
		/// Returns null if the property is not found.
		/// </summary>
		public readonly IProperty CurrentProperty;
		
		/// <summary>
		/// Gets the MoveNext() method on the IEnumerator.
		/// Returns null if the method is not found.
		/// </summary>
		public readonly IMethod MoveNextMethod;
		
		public ForEachResolveResult(ResolveResult getEnumeratorCall, IType collectionType, IType enumeratorType, IType elementType, IVariable elementVariable, IProperty currentProperty, IMethod moveNextMethod, IType voidType)
			: base(voidType)
		{
			if (getEnumeratorCall == null)
				throw new ArgumentNullException("getEnumeratorCall");
			if (collectionType == null)
				throw new ArgumentNullException("collectionType");
			if (enumeratorType == null)
				throw new ArgumentNullException("enumeratorType");
			if (elementType == null)
				throw new ArgumentNullException("elementType");
			if (elementVariable == null)
				throw new ArgumentNullException("elementVariable");
			this.GetEnumeratorCall = getEnumeratorCall;
			this.CollectionType = collectionType;
			this.EnumeratorType = enumeratorType;
			this.ElementType = elementType;
			this.ElementVariable = elementVariable;
			this.CurrentProperty = currentProperty;
			this.MoveNextMethod = moveNextMethod;
		}
	}
}
