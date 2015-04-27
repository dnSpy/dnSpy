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
	/// Represents an unresolved assembly.
	/// </summary>
	public interface IUnresolvedAssembly : IAssemblyReference
	{
		/// <summary>
		/// Gets the assembly name (short name).
		/// </summary>
		string AssemblyName { get; }
		
		/// <summary>
		/// Gets the full assembly name (including public key token etc.)
		/// </summary>
		string FullAssemblyName { get; }
		
		/// <summary>
		/// Gets the path to the assembly location. 
		/// For projects it is the same as the output path.
		/// </summary>
		string Location { get; }

		/// <summary>
		/// Gets the list of all assembly attributes in the project.
		/// </summary>
		IEnumerable<IUnresolvedAttribute> AssemblyAttributes { get; }
		
		/// <summary>
		/// Gets the list of all module attributes in the project.
		/// </summary>
		IEnumerable<IUnresolvedAttribute> ModuleAttributes { get; }
		
		/// <summary>
		/// Gets all non-nested types in the assembly.
		/// </summary>
		IEnumerable<IUnresolvedTypeDefinition> TopLevelTypeDefinitions { get; }
	}
	
	public interface IAssemblyReference
	{
		/// <summary>
		/// Resolves this assembly.
		/// </summary>
		IAssembly Resolve(ITypeResolveContext context);
	}
	
	/// <summary>
	/// Represents an assembly.
	/// </summary>
	public interface IAssembly : ICompilationProvider
	{
		/// <summary>
		/// Gets the original unresolved assembly.
		/// </summary>
		IUnresolvedAssembly UnresolvedAssembly { get; }
		
		/// <summary>
		/// Gets whether this assembly is the main assembly of the compilation.
		/// </summary>
		bool IsMainAssembly { get; }
		
		/// <summary>
		/// Gets the assembly name (short name).
		/// </summary>
		string AssemblyName { get; }
		
		/// <summary>
		/// Gets the full assembly name (including public key token etc.)
		/// </summary>
		string FullAssemblyName { get; }
		
		/// <summary>
		/// Gets the list of all assembly attributes in the project.
		/// </summary>
		IList<IAttribute> AssemblyAttributes { get; }
		
		/// <summary>
		/// Gets the list of all module attributes in the project.
		/// </summary>
		IList<IAttribute> ModuleAttributes { get; }
		
		/// <summary>
		/// Gets whether the internals of this assembly are visible in the specified assembly.
		/// </summary>
		bool InternalsVisibleTo(IAssembly assembly);
		
		/// <summary>
		/// Gets the root namespace for this assembly.
		/// </summary>
		/// <remarks>
		/// This always is the namespace without a name - it's unrelated to the 'root namespace' project setting.
		/// </remarks>
		INamespace RootNamespace { get; }
		
		/// <summary>
		/// Gets the type definition for a top-level type.
		/// </summary>
		/// <remarks>This method uses ordinal name comparison, not the compilation's name comparer.</remarks>
		ITypeDefinition GetTypeDefinition(TopLevelTypeName topLevelTypeName);
		
		/// <summary>
		/// Gets all non-nested types in the assembly.
		/// </summary>
		IEnumerable<ITypeDefinition> TopLevelTypeDefinitions { get; }
	}
}
