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
	public enum SymbolKind : byte
	{
		None,
		/// <seealso cref="ITypeDefinition"/>
		TypeDefinition,
		/// <seealso cref="IField"/>
		Field,
		/// <summary>
		/// The symbol is a property, but not an indexer.
		/// </summary>
		/// <seealso cref="IProperty"/>
		Property,
		/// <summary>
		/// The symbol is an indexer, not a regular property.
		/// </summary>
		/// <seealso cref="IProperty"/>
		Indexer,
		/// <seealso cref="IEvent"/>
		Event,
		/// <summary>
		/// The symbol is a method which is not an operator/constructor/destructor or accessor.
		/// </summary>
		/// <seealso cref="IMethod"/>
		Method,
		/// <summary>
		/// The symbol is a user-defined operator.
		/// </summary>
		/// <seealso cref="IMethod"/>
		Operator,
		/// <seealso cref="IMethod"/>
		Constructor,
		/// <seealso cref="IMethod"/>
		Destructor,
		/// <summary>
		/// The accessor method for a property getter/setter or event add/remove.
		/// </summary>
		/// <seealso cref="IMethod"/>
		Accessor,
		/// <seealso cref="INamespace"/>
		Namespace,
		/// <summary>
		/// The symbol is a variable, but not a parameter.
		/// </summary>
		/// <seealso cref="IVariable"/>
		Variable,
		/// <seealso cref="IParameter"/>
		Parameter,
		/// <seealso cref="ITypeParameter"/>
		TypeParameter,
	}
	
	/// <summary>
	/// Interface for type system symbols.
	/// </summary>
	public interface ISymbol
	{
		/// <summary>
		/// This property returns an enum specifying which kind of symbol this is
		/// (which derived interfaces of ISymbol are implemented)
		/// </summary>
		SymbolKind SymbolKind { get; }
		
		/// <summary>
		/// Gets the short name of the symbol.
		/// </summary>
		string Name { get; }
		
		/// <summary>
		/// Creates a symbol reference that can be used to rediscover this symbol in another compilation.
		/// </summary>
		ISymbolReference ToReference();
	}
	
	public interface ISymbolReference
	{
		ISymbol Resolve(ITypeResolveContext context);
	}
}
