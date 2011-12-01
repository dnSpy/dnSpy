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

namespace ICSharpCode.NRefactory.TypeSystem
{
	/// <summary>
	/// Represents an accessor (property getter/setter; or event add/remove/invoke).
	/// </summary>
	public interface IUnresolvedAccessor : IHasAccessibility
	{
		/// <summary>
		/// Gets the accessor region.
		/// </summary>
		DomRegion Region { get; }
		
		/// <summary>
		/// Gets the attributes defined on this accessor.
		/// </summary>
		IList<IUnresolvedAttribute> Attributes { get; }
		
		/// <summary>
		/// Gets the attributes defined on the return type of the accessor. (e.g. [return: MarshalAs(...)])
		/// </summary>
		IList<IUnresolvedAttribute> ReturnTypeAttributes { get; }
		
		IAccessor CreateResolvedAccessor(ITypeResolveContext context);
	}
	
	/// <summary>
	/// Represents an accessor (property getter/setter; or event add/remove/invoke).
	/// </summary>
	public interface IAccessor : IHasAccessibility
	{
		/// <summary>
		/// Gets the accessor region.
		/// </summary>
		DomRegion Region { get; }
		
		/// <summary>
		/// Gets the attributes defined on this accessor.
		/// </summary>
		IList<IAttribute> Attributes { get; }
		
		/// <summary>
		/// Gets the attributes defined on the return type of the accessor. (e.g. [return: MarshalAs(...)])
		/// </summary>
		IList<IAttribute> ReturnTypeAttributes { get; }
	}
}
