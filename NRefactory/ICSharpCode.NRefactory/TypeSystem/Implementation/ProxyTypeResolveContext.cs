// Copyright (c) 2010 AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;

namespace ICSharpCode.NRefactory.TypeSystem.Implementation
{
	/// <summary>
	/// Proxy that forwards calls to another TypeResolveContext.
	/// Useful as base class for decorators.
	/// </summary>
	public class ProxyTypeResolveContext : AbstractAnnotatable, ITypeResolveContext
	{
		protected readonly ITypeResolveContext target;
		
		/// <summary>
		/// Creates a new ProxyTypeResolveContext.
		/// </summary>
		public ProxyTypeResolveContext(ITypeResolveContext target)
		{
			if (target == null)
				throw new ArgumentNullException("target");
			this.target = target;
		}
		
		/// <inheritdoc/>
		public virtual ITypeDefinition GetTypeDefinition(string nameSpace, string name, int typeParameterCount, StringComparer nameComparer)
		{
			return target.GetTypeDefinition(nameSpace, name, typeParameterCount, nameComparer);
		}
		
		/// <inheritdoc/>
		public virtual IEnumerable<ITypeDefinition> GetTypes()
		{
			return target.GetTypes();
		}
		
		/// <inheritdoc/>
		public virtual IEnumerable<ITypeDefinition> GetTypes(string nameSpace, StringComparer nameComparer)
		{
			return target.GetTypes(nameSpace, nameComparer);
		}
		
		/// <inheritdoc/>
		public virtual IEnumerable<string> GetNamespaces()
		{
			return target.GetNamespaces();
		}
		
		/// <inheritdoc/>
		public virtual string GetNamespace(string nameSpace, StringComparer nameComparer)
		{
			return target.GetNamespace(nameSpace, nameComparer);
		}
		
		/// <inheritdoc/>
		public virtual ISynchronizedTypeResolveContext Synchronize()
		{
			return target.Synchronize();
		}
		
		/// <inheritdoc/>
		public virtual Utils.CacheManager CacheManager {
			// Don't forward this by default; we don't know what derived classes are doing;
			// it might not be cache-safe.
			get { return null; }
		}
	}
}
