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
	/// Represents a property or indexer.
	/// </summary>
	public interface IUnresolvedProperty : IUnresolvedParameterizedMember
	{
		bool CanGet { get; }
		bool CanSet { get; }
		
		IUnresolvedMethod Getter { get; }
		IUnresolvedMethod Setter { get; }
		
		bool IsIndexer { get; }
		
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
		new IProperty Resolve(ITypeResolveContext context);
	}
	
	/// <summary>
	/// Represents a property or indexer.
	/// </summary>
	public interface IProperty : IParameterizedMember
	{
		bool CanGet { get; }
		bool CanSet { get; }
		
		IMethod Getter { get; }
		IMethod Setter { get; }
		
		bool IsIndexer { get; }
	}
}
