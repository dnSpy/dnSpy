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
using System.Linq;
using System.Threading;

namespace ICSharpCode.NRefactory.TypeSystem.Implementation
{
	/// <summary>
	/// Type Reference used when the fully qualified type name is known.
	/// </summary>
	[Serializable]
	public sealed class GetClassTypeReference : ITypeReference, ISymbolReference, ISupportsInterning
	{
		readonly IAssemblyReference assembly;
		readonly FullTypeName fullTypeName;
		
		/// <summary>
		/// Creates a new GetClassTypeReference that searches a type definition.
		/// </summary>
		/// <param name="fullTypeName">The full name of the type.</param>
		/// <param name="assembly">A reference to the assembly containing this type.
		/// If this parameter is null, the GetClassTypeReference will search in all
		/// assemblies belonging to the compilation.
		/// </param>
		public GetClassTypeReference(FullTypeName fullTypeName, IAssemblyReference assembly = null)
		{
			this.fullTypeName = fullTypeName;
			this.assembly = assembly;
		}
		
		/// <summary>
		/// Creates a new GetClassTypeReference that searches a top-level type in all assemblies.
		/// </summary>
		/// <param name="namespaceName">The namespace name containing the type, e.g. "System.Collections.Generic".</param>
		/// <param name="name">The name of the type, e.g. "List".</param>
		/// <param name="typeParameterCount">The number of type parameters, (e.g. 1 for List&lt;T&gt;).</param>
		public GetClassTypeReference(string namespaceName, string name, int typeParameterCount = 0)
		{
			this.fullTypeName = new TopLevelTypeName(namespaceName, name, typeParameterCount);
		}
		
		/// <summary>
		/// Creates a new GetClassTypeReference that searches a top-level type in the specified assembly.
		/// </summary>
		/// <param name="assembly">A reference to the assembly containing this type.
		/// If this parameter is null, the GetClassTypeReference will search in all assemblies belonging to the ICompilation.</param>
		/// <param name="namespaceName">The namespace name containing the type, e.g. "System.Collections.Generic".</param>
		/// <param name="name">The name of the type, e.g. "List".</param>
		/// <param name="typeParameterCount">The number of type parameters, (e.g. 1 for List&lt;T&gt;).</param>
		public GetClassTypeReference(IAssemblyReference assembly, string namespaceName, string name, int typeParameterCount = 0)
		{
			this.assembly = assembly;
			this.fullTypeName = new TopLevelTypeName(namespaceName, name, typeParameterCount);
		}
		
		/// <summary>
		/// Gets the assembly reference.
		/// This property returns null if the GetClassTypeReference is searching in all assemblies
		/// of the compilation.
		/// </summary>
		public IAssemblyReference Assembly { get { return assembly; } }
		
		/// <summary>
		/// Gets the full name of the type this reference is searching for.
		/// </summary>
		public FullTypeName FullTypeName { get { return fullTypeName; } }
		
		[Obsolete("Use the FullTypeName property instead. GetClassTypeReference now supports nested types, where the Namespace/Name/TPC tripel isn't sufficient for identifying the type.")]
		public string Namespace { get { return fullTypeName.TopLevelTypeName.Namespace; } }
		[Obsolete("Use the FullTypeName property instead. GetClassTypeReference now supports nested types, where the Namespace/Name/TPC tripel isn't sufficient for identifying the type.")]
		public string Name { get { return fullTypeName.Name; } }
		[Obsolete("Use the FullTypeName property instead. GetClassTypeReference now supports nested types, where the Namespace/Name/TPC tripel isn't sufficient for identifying the type.")]
		public int TypeParameterCount { get { return fullTypeName.TypeParameterCount; } }

		IType ResolveInAllAssemblies(ITypeResolveContext context)
		{
			var compilation = context.Compilation;
			foreach (var asm in compilation.Assemblies) {
				IType type = asm.GetTypeDefinition(fullTypeName);
				if (type != null)
					return type;
			}
			return null;
		}

		public IType Resolve(ITypeResolveContext context)
		{
			if (context == null)
				throw new ArgumentNullException("context");
			
			IType type = null;
			if (assembly == null) {
				// No assembly specified: look in all assemblies, but prefer the current assembly
				if (context.CurrentAssembly != null) {
					type = context.CurrentAssembly.GetTypeDefinition(fullTypeName);
				}
				if (type == null) {
					type = ResolveInAllAssemblies(context);
				}
			} else {
				// Assembly specified: only look in the specified assembly.
				// But if that's not loaded in the compilation, allow fall back to other assemblies.
				// (the non-loaded assembly might be a facade containing type forwarders -
				//  for example, when referencing a portable library from a non-portable project)
				IAssembly asm = assembly.Resolve(context);
				if (asm != null) {
					type = asm.GetTypeDefinition(fullTypeName);
				} else {
					type = ResolveInAllAssemblies(context);
				}
			}
			return type ?? new UnknownType(fullTypeName);
		}
		
		ISymbol ISymbolReference.Resolve(ITypeResolveContext context)
		{
			var type = Resolve(context);
			if (type is ITypeDefinition)
				return (ISymbol)type;
			return null;
		}
		
		public override string ToString()
		{
			return fullTypeName.ToString() + (assembly != null ? ", " + assembly.ToString() : null);
		}
		
		int ISupportsInterning.GetHashCodeForInterning()
		{
			unchecked {
				return 33 * assembly.GetHashCode() + fullTypeName.GetHashCode();
			}
		}
		
		bool ISupportsInterning.EqualsForInterning(ISupportsInterning other)
		{
			GetClassTypeReference o = other as GetClassTypeReference;
			return o != null && assembly == o.assembly && fullTypeName == o.fullTypeName;
		}
	}
}
