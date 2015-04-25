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

namespace ICSharpCode.NRefactory.TypeSystem
{
	/// <summary>
	/// Represents a resolved namespace.
	/// </summary>
	public interface INamespace : ISymbol, ICompilationProvider
	{
		// No pointer back to unresolved namespace:
		// multiple unresolved namespaces (from different assemblies) get
		// merged into one INamespace.
		
		/// <summary>
		/// Gets the extern alias for this namespace.
		/// Returns an empty string for normal namespaces.
		/// </summary>
		string ExternAlias { get; }
		
		/// <summary>
		/// Gets the full name of this namespace. (e.g. "System.Collections")
		/// </summary>
		string FullName { get; }
		
		/// <summary>
		/// Gets the short name of this namespace (e.g. "Collections").
		/// </summary>
		new string Name { get; }
		
		/// <summary>
		/// Gets the parent namespace.
		/// Returns null if this is the root namespace.
		/// </summary>
		INamespace ParentNamespace { get; }
		
		/// <summary>
		/// Gets the child namespaces in this namespace.
		/// </summary>
		IEnumerable<INamespace> ChildNamespaces { get; }
		
		/// <summary>
		/// Gets the types in this namespace.
		/// </summary>
		IEnumerable<ITypeDefinition> Types { get; }
		
		/// <summary>
		/// Gets the assemblies that contribute types to this namespace (or to child namespaces).
		/// </summary>
		IEnumerable<IAssembly> ContributingAssemblies { get; }
		
		/// <summary>
		/// Gets a direct child namespace by its short name.
		/// Returns null when the namespace cannot be found.
		/// </summary>
		/// <remarks>
		/// This method uses the compilation's current string comparer.
		/// </remarks>
		INamespace GetChildNamespace(string name);
		
		/// <summary>
		/// Gets the type with the specified short name and type parameter count.
		/// Returns null if the type cannot be found.
		/// </summary>
		/// <remarks>
		/// This method uses the compilation's current string comparer.
		/// </remarks>
		ITypeDefinition GetTypeDefinition(string name, int typeParameterCount);
	}
}
