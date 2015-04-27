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
using System.Diagnostics.Contracts;

namespace ICSharpCode.NRefactory.TypeSystem
{
	public interface INamedElement
	{
		/// <summary>
		/// Gets the fully qualified name of the class the return type is pointing to.
		/// </summary>
		/// <returns>
		/// "System.Int32[]" for int[]<br/>
		/// "System.Collections.Generic.List" for List&lt;string&gt;
		/// "System.Environment.SpecialFolder" for Environment.SpecialFolder
		/// </returns>
		string FullName { get; }
		
		/// <summary>
		/// Gets the short name of the class the return type is pointing to.
		/// </summary>
		/// <returns>
		/// "Int32[]" for int[]<br/>
		/// "List" for List&lt;string&gt;
		/// "SpecialFolder" for Environment.SpecialFolder
		/// </returns>
		string Name { get; }
		
		/// <summary>
		/// Gets the full reflection name of the element.
		/// </summary>
		/// <remarks>
		/// For types, the reflection name can be parsed back into a ITypeReference by using
		/// <see cref="ReflectionHelper.ParseReflectionName(string)"/>.
		/// </remarks>
		/// <returns>
		/// "System.Int32[]" for int[]<br/>
		/// "System.Int32[][,]" for C# int[,][]<br/>
		/// "System.Collections.Generic.List`1[[System.String]]" for List&lt;string&gt;
		/// "System.Environment+SpecialFolder" for Environment.SpecialFolder
		/// </returns>
		string ReflectionName { get; }
		
		/// <summary>
		/// Gets the full name of the namespace containing this entity.
		/// </summary>
		string Namespace { get; }
	}
}
