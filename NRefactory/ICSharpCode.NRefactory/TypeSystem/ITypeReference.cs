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
	/// <summary>
	/// Represents a reference to a type.
	/// Must be resolved before it can be used as type.
	/// </summary>
	public interface ITypeReference
	{
		// Keep this interface simple: I decided against having GetMethods/GetEvents etc. here,
		// so that the Resolve step is never hidden from the consumer.
		
		// I decided against implementing IFreezable here: IUnresolvedTypeDefinition can be used as ITypeReference,
		// but when freezing the reference, one wouldn't expect the definition to freeze.
		
		/// <summary>
		/// Resolves this type reference.
		/// </summary>
		/// <param name="context">
		/// Context to use for resolving this type reference.
		/// Which kind of context is required depends on the which kind of type reference this is;
		/// please consult the documentation of the method that was used to create this type reference,
		/// or that of the class implementing this method.
		/// </param>
		/// <returns>
		/// Returns the resolved type.
		/// In case of an error, returns an unknown type (<see cref="TypeKind.Unknown"/>).
		/// Never returns null.
		/// </returns>
		IType Resolve(ITypeResolveContext context);
	}
	
	public interface ITypeResolveContext : ICompilationProvider
	{
		/// <summary>
		/// Gets the current assembly.
		/// This property may return null if this context does not specify any assembly.
		/// </summary>
		IAssembly CurrentAssembly { get; }
		
		/// <summary>
		/// Gets the current type definition.
		/// </summary>
		ITypeDefinition CurrentTypeDefinition { get ;}
		
		/// <summary>
		/// Gets the current member.
		/// </summary>
		IMember CurrentMember { get; }
		
		ITypeResolveContext WithCurrentTypeDefinition(ITypeDefinition typeDefinition);
		ITypeResolveContext WithCurrentMember(IMember member);
	}
}