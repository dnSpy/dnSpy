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
using ICSharpCode.NRefactory.Utils;

namespace ICSharpCode.NRefactory.TypeSystem
{
	public interface ICompilation
	{
		/// <summary>
		/// Gets the current assembly.
		/// </summary>
		IAssembly MainAssembly { get; }
		
		/// <summary>
		/// Gets the type resolve context that specifies this compilation and no current assembly or entity.
		/// </summary>
		ITypeResolveContext TypeResolveContext { get; }
		
		/// <summary>
		/// Gets the list of all assemblies in the compilation.
		/// </summary>
		/// <remarks>
		/// This main assembly is the first entry in the list.
		/// </remarks>
		IList<IAssembly> Assemblies { get; }
		
		/// <summary>
		/// Gets the referenced assemblies.
		/// This list does not include the main assembly.
		/// </summary>
		IList<IAssembly> ReferencedAssemblies { get; }
		
		/// <summary>
		/// Gets the root namespace of this compilation.
		/// This is a merged version of the root namespaces of all assemblies.
		/// </summary>
		/// <remarks>
		/// This always is the namespace without a name - it's unrelated to the 'root namespace' project setting.
		/// </remarks>
		INamespace RootNamespace { get; }
		
		/// <summary>
		/// Gets the root namespace for a given extern alias.
		/// </summary>
		/// <remarks>
		/// If <paramref name="alias"/> is <c>null</c> or an empty string, this method
		/// returns the global root namespace.
		/// If no alias with the specified name exists, this method returns null.
		/// </remarks>
		INamespace GetNamespaceForExternAlias(string alias);
		
		IType FindType(KnownTypeCode typeCode);
		
		/// <summary>
		/// Gets the name comparer for the language being compiled.
		/// This is the string comparer used for the INamespace.GetTypeDefinition method.
		/// </summary>
		StringComparer NameComparer { get; }
		
		ISolutionSnapshot SolutionSnapshot { get; }
		
		CacheManager CacheManager { get; }
	}
	
	public interface ICompilationProvider
	{
		/// <summary>
		/// Gets the parent compilation.
		/// This property never returns null.
		/// </summary>
		ICompilation Compilation { get; }
	}
}
