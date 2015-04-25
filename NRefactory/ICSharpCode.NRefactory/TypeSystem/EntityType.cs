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

namespace ICSharpCode.NRefactory.TypeSystem
{
	[Obsolete("Use SymbolKind instead")]
	public enum EntityType : byte
	{
		None = SymbolKind.None,
		/// <seealso cref="ITypeDefinition"/>
		TypeDefinition = SymbolKind.TypeDefinition,
		/// <seealso cref="IField"/>
		Field = SymbolKind.Field,
		/// <summary>
		/// The symbol is a property, but not an indexer.
		/// </summary>
		/// <seealso cref="IProperty"/>
		Property = SymbolKind.Property,
		/// <summary>
		/// The symbol is an indexer, not a regular property.
		/// </summary>
		/// <seealso cref="IProperty"/>
		Indexer = SymbolKind.Indexer,
		/// <seealso cref="IEvent"/>
		Event = SymbolKind.Event,
		/// <summary>
		/// The symbol is a method which is not an operator/constructor/destructor or accessor.
		/// </summary>
		/// <seealso cref="IMethod"/>
		Method = SymbolKind.Method,
		/// <summary>
		/// The symbol is a user-defined operator.
		/// </summary>
		/// <seealso cref="IMethod"/>
		Operator = SymbolKind.Operator,
		/// <seealso cref="IMethod"/>
		Constructor = SymbolKind.Constructor,
		/// <seealso cref="IMethod"/>
		Destructor = SymbolKind.Destructor,
		/// <summary>
		/// The accessor method for a property getter/setter or event add/remove.
		/// </summary>
		/// <seealso cref="IMethod"/>
		Accessor = SymbolKind.Accessor,
	}
}
