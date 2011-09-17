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

using ICSharpCode.NRefactory.Utils;

namespace ICSharpCode.NRefactory.TypeSystem
{
	/// <summary>
	/// Context representing the set of assemblies in which a type is being searched.
	/// </summary>
	#if WITH_CONTRACTS
	[ContractClass(typeof(ITypeResolveContextContract))]
	#endif
	public interface ITypeResolveContext
	{
		/// <summary>
		/// Gets the definition for a known type.
		/// </summary>
		/// <returns>Returns the type definition; or null if the type was not found</returns>
		/// <remarks>
		/// This method will may only be called with the 'known types' (see members of KnownTypeReference).
		/// As a special case, TypeCode.Empty is used to represent System.Void.
		/// </remarks>
		ITypeDefinition GetKnownTypeDefinition(TypeCode typeCode);
		
		/// <summary>
		/// Retrieves a type.
		/// </summary>
		/// <param name="nameSpace">Namespace that contains the type</param>
		/// <param name="name">Name of the type</param>
		/// <param name="typeParameterCount">Number of type parameters</param>
		/// <param name="nameComparer">Language-specific rules for how class names are compared</param>
		/// <returns>The type definition for the class; or null if no such type exists.</returns>
		/// <remarks>This method never returns inner types; it can be used only with top-level types.</remarks>
		ITypeDefinition GetTypeDefinition(string nameSpace, string name, int typeParameterCount, StringComparer nameComparer);
		
		/// <summary>
		/// Retrieves all top-level types.
		/// </summary>
		/// <remarks>
		/// If this method is called within <c>using (pc.Synchronize())</c>, then the returned enumerable is valid
		/// only until the end of the synchronize block.
		/// </remarks>
		IEnumerable<ITypeDefinition> GetTypes();
		
		/// <summary>
		/// Retrieves all types in the specified namespace.
		/// </summary>
		/// <param name="nameSpace">Namespace in which types are being retrieved. Use <c>string.Empty</c> for the root namespace.</param>
		/// <param name="nameComparer">Language-specific rules for how namespace names are compared</param>
		/// <returns>List of types within that namespace.</returns>
		/// <remarks>
		/// If this method is called within <c>using (var spc = pc.Synchronize())</c>, then the returned enumerable is valid
		/// only until the end of the synchronize block.
		/// </remarks>
		IEnumerable<ITypeDefinition> GetTypes(string nameSpace, StringComparer nameComparer);
		
		/// <summary>
		/// Retrieves all namespaces.
		/// </summary>
		/// <remarks>
		/// If this method is called within <c>using (var spc = pc.Synchronize())</c>, then the returned enumerable is valid
		/// only until the end of the synchronize block.
		/// </remarks>
		IEnumerable<string> GetNamespaces();
		
		/// <summary>
		/// Gets a namespace.
		/// </summary>
		/// <param name="nameSpace">The full name of the namespace.</param>
		/// <param name="nameComparer">The comparer to use.</param>
		/// <returns>The full name of the namespace, if it exists; or null if the namespace does not exist.</returns>
		/// <remarks>
		/// For StringComparer.Ordinal, the return value is either null or the input namespace.
		/// For other name comparers, this method returns the declared name of the namespace.
		/// </remarks>
		string GetNamespace(string nameSpace, StringComparer nameComparer);
		
		/// <summary>
		/// Returns a <see cref="ISynchronizedTypeResolveContext"/> that
		/// represents the same context as this instance, but cannot be modified
		/// by other threads.
		/// The ISynchronizedTypeResolveContext must be disposed from the same thread
		/// that called this method when it is no longer used.
		/// </summary>
		/// <remarks>
		/// A simple implementation might enter a ReaderWriterLock when the synchronized context
		/// is created, and releases the lock when Dispose() is called.
		/// However, implementations based on immutable data structures are also possible.
		/// </remarks>
		ISynchronizedTypeResolveContext Synchronize();
		
		/// <summary>
		/// Returns the cache manager associated with this resolve context,
		/// or null if caching is not allowed.
		/// Whenever the resolve context changes in some way, this property must return a new object to
		/// ensure that old caches are cleared.
		/// </summary>
		CacheManager CacheManager { get; }
	}
	
	#if WITH_CONTRACTS
	[ContractClassFor(typeof(ITypeResolveContext))]
	abstract class ITypeResolveContextContract : ITypeResolveContext
	{
		ITypeDefinition ITypeResolveContext.GetClass(string nameSpace, string name, int typeParameterCount, StringComparer nameComparer)
		{
			Contract.Requires(nameSpace != null);
			Contract.Requires(name != null);
			Contract.Requires(typeParameterCount >= 0);
			Contract.Requires(nameComparer != null);
			return null;
		}
		
		ISynchronizedTypeResolveContext ITypeResolveContext.Synchronize()
		{
			Contract.Ensures(Contract.Result<ISynchronizedTypeResolveContext>() != null);
			return null;
		}
		
		IEnumerable<ITypeDefinition> ITypeResolveContext.GetTypes()
		{
			Contract.Ensures(Contract.Result<IEnumerable<ITypeDefinition>>() != null);
			return null;
		}
		
		IEnumerable<ITypeDefinition> ITypeResolveContext.GetTypes(string nameSpace, StringComparer nameComparer)
		{
			Contract.Requires(nameSpace != null);
			Contract.Requires(nameComparer != null);
			Contract.Ensures(Contract.Result<IEnumerable<ITypeDefinition>>() != null);
			return null;
		}
		
		IEnumerable<string> ITypeResolveContext.GetNamespaces()
		{
			Contract.Ensures(Contract.Result<IEnumerable<ITypeDefinition>>() != null);
			return null;
		}
		
		Utils.CacheManager ITypeResolveContext.CacheManager {
			get { return null; }
		}
		
		string ITypeResolveContext.GetNamespace(string nameSpace, StringComparer nameComparer)
		{
			Contract.Requires(nameSpace != null);
			Contract.Requires(nameComparer != null);
			return null;
		}
	}
	#endif
}
