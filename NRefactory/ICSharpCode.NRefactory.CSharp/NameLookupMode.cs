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

namespace ICSharpCode.NRefactory.CSharp
{
	public enum NameLookupMode
	{
		/// <summary>
		/// Normal name lookup in expressions
		/// </summary>
		Expression,
		/// <summary>
		/// Name lookup in expression, where the expression is the target of an invocation.
		/// Such a lookup will only return methods and delegate-typed fields.
		/// </summary>
		InvocationTarget,
		/// <summary>
		/// Normal name lookup in type references.
		/// </summary>
		Type,
		/// <summary>
		/// Name lookup in the type reference inside a using declaration.
		/// </summary>
		TypeInUsingDeclaration,
		/// <summary>
		/// Name lookup for base type references.
		/// </summary>
		BaseTypeReference
	}
}
