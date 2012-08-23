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
using System.Diagnostics.Contracts;
using ICSharpCode.NRefactory.TypeSystem;

namespace ICSharpCode.NRefactory.TypeSystem
{
	public interface IUnresolvedMethod : IUnresolvedParameterizedMember
	{
		/// <summary>
		/// Gets the attributes associated with the return type. (e.g. [return: MarshalAs(...)])
		/// </summary>
		IList<IUnresolvedAttribute> ReturnTypeAttributes { get; }
		
		IList<IUnresolvedTypeParameter> TypeParameters { get; }
		
		bool IsConstructor { get; }
		bool IsDestructor { get; }
		bool IsOperator { get; }
		
		/// <summary>
		/// Gets whether the method is a C#-style partial method.
		/// Check <see cref="HasBody"/> to test if it is a partial method declaration or implementation.
		/// </summary>
		bool IsPartial { get; }
		
		[Obsolete("Use IsPartial && !HasBody instead")]
		bool IsPartialMethodDeclaration { get; }
		
		[Obsolete("Use IsPartial && HasBody instead")]
		bool IsPartialMethodImplementation { get; }
		
		/// <summary>
		/// Gets whether the method has a body.
		/// This property returns <c>false</c> for <c>abstract</c> or <c>extern</c> methods,
		/// or for <c>partial</c> methods without implementation.
		/// </summary>
		bool HasBody { get; }
		
		/// <summary>
		/// If this method is an accessor, returns a reference to the corresponding property/event.
		/// Otherwise, returns null.
		/// </summary>
		IUnresolvedMember AccessorOwner { get; }
		
		/// <summary>
		/// Resolves the member.
		/// </summary>
		/// <param name="context">
		/// Context for looking up the member. The context must specify the current assembly.
		/// A <see cref="SimpleTypeResolveContext"/> that specifies the current assembly is sufficient.
		/// </param>
		/// <returns>
		/// Returns the resolved member, or <c>null</c> if the member could not be found.
		/// </returns>
		new IMethod Resolve(ITypeResolveContext context);
	}
	
	/// <summary>
	/// Represents a method, constructor, destructor or operator.
	/// </summary>
	public interface IMethod : IParameterizedMember
	{
		/// <summary>
		/// Gets the unresolved method parts.
		/// For partial methods, this returns all parts.
		/// Otherwise, this returns an array with a single element (new[] { UnresolvedMember }).
		/// </summary>
		IList<IUnresolvedMethod> Parts { get; }
		
		/// <summary>
		/// Gets the attributes associated with the return type. (e.g. [return: MarshalAs(...)])
		/// </summary>
		IList<IAttribute> ReturnTypeAttributes { get; }
		
		IList<ITypeParameter> TypeParameters { get; }
		
		bool IsExtensionMethod { get; }
		bool IsConstructor { get; }
		bool IsDestructor { get; }
		bool IsOperator { get; }
		
		/// <summary>
		/// Gets whether the method is a C#-style partial method.
		/// A call to such a method is ignored by the compiler if the partial method has no body.
		/// </summary>
		/// <seealso cref="HasBody"/>
		bool IsPartial { get; }
		
		/// <summary>
		/// Gets whether the method has a body.
		/// This property returns <c>false</c> for <c>abstract</c> or <c>extern</c> methods,
		/// or for <c>partial</c> methods without implementation.
		/// </summary>
		bool HasBody { get; }
		
		/// <summary>
		/// Gets whether the method is a property/event accessor.
		/// </summary>
		bool IsAccessor { get; }
		
		/// <summary>
		/// If this method is an accessor, returns the corresponding property/event.
		/// Otherwise, returns null.
		/// </summary>
		IMember AccessorOwner { get; }
	}
}
