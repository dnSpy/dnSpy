// Copyright (c) 2010 AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using ICSharpCode.NRefactory.Utils;

namespace ICSharpCode.NRefactory.TypeSystem.Implementation
{
	/// <summary>
	/// Represents multiple type resolve contexts.
	/// </summary>
	public class CompositeTypeResolveContext : ITypeResolveContext
	{
		/// <summary>
		/// Creates a <see cref="CompositeTypeResolveContext"/> that combines the given resolve contexts.
		/// If one of the input parameters is null, the other input parameter is returned directly.
		/// If both input parameters are null, the function returns null.
		/// </summary>
		public static ITypeResolveContext Combine(ITypeResolveContext a, ITypeResolveContext b)
		{
			if (a == null)
				return b;
			if (b == null)
				return a;
			return new CompositeTypeResolveContext(new [] { a, b });
		}
		
		readonly ITypeResolveContext[] children;
		
		/// <summary>
		/// Creates a new <see cref="CompositeTypeResolveContext"/>
		/// </summary>
		public CompositeTypeResolveContext(IEnumerable<ITypeResolveContext> children)
		{
			if (children == null)
				throw new ArgumentNullException("children");
			this.children = children.ToArray();
			foreach (ITypeResolveContext c in this.children) {
				if (c == null)
					throw new ArgumentException("children enumeration contains nulls");
			}
		}
		
		private CompositeTypeResolveContext(ITypeResolveContext[] children)
		{
			Debug.Assert(children != null);
			this.children = children;
		}
		
		/// <inheritdoc/>
		public ITypeDefinition GetTypeDefinition(string nameSpace, string name, int typeParameterCount, StringComparer nameComparer)
		{
			foreach (ITypeResolveContext context in children) {
				ITypeDefinition d = context.GetTypeDefinition(nameSpace, name, typeParameterCount, nameComparer);
				if (d != null)
					return d;
			}
			return null;
		}
		
		/// <inheritdoc/>
		public IEnumerable<ITypeDefinition> GetTypes()
		{
			return children.SelectMany(c => c.GetTypes());
		}
		
		/// <inheritdoc/>
		public IEnumerable<ITypeDefinition> GetTypes(string nameSpace, StringComparer nameComparer)
		{
			return children.SelectMany(c => c.GetTypes(nameSpace, nameComparer));
		}
		
		/// <inheritdoc/>
		public IEnumerable<string> GetNamespaces()
		{
			return children.SelectMany(c => c.GetNamespaces()).Distinct();
		}
		
		/// <inheritdoc/>
		public string GetNamespace(string nameSpace, StringComparer nameComparer)
		{
			foreach (ITypeResolveContext context in children) {
				string r = context.GetNamespace(nameSpace, nameComparer);
				if (r != null)
					return r;
			}
			return null;
		}
		
		/// <inheritdoc/>
		public virtual ISynchronizedTypeResolveContext Synchronize()
		{
			ISynchronizedTypeResolveContext[] sync = new ISynchronizedTypeResolveContext[children.Length];
			bool success = false;
			try {
				for (int i = 0; i < sync.Length; i++) {
					sync[i] = children[i].Synchronize();
					if (sync[i] == null)
						throw new InvalidOperationException(children[i] + ".ToString() returned null");
				}
				ISynchronizedTypeResolveContext r = new CompositeSynchronizedTypeResolveContext(sync, new CacheManager(), true);
				success = true;
				return r;
			} finally {
				if (!success) {
					// something went wrong, so immediately dispose the contexts we acquired
					for (int i = 0; i < sync.Length; i++) {
						if (sync[i] != null)
							sync[i].Dispose();
					}
				}
			}
		}
		
		public virtual CacheManager CacheManager {
			// We don't know if our input contexts are mutable, so, to be on the safe side,
			// we don't implement caching here.
			get { return null; }
		}
		
		sealed class CompositeSynchronizedTypeResolveContext : CompositeTypeResolveContext, ISynchronizedTypeResolveContext
		{
			readonly CacheManager cacheManager;
			readonly bool isTopLevel;
			
			public CompositeSynchronizedTypeResolveContext(ITypeResolveContext[] children, CacheManager cacheManager, bool isTopLevel)
				: base(children)
			{
				Debug.Assert(cacheManager != null);
				this.cacheManager = cacheManager;
				this.isTopLevel = isTopLevel;
			}
			
			public void Dispose()
			{
				if (isTopLevel) {
					foreach (ISynchronizedTypeResolveContext element in children) {
						element.Dispose();
					}
					// When the top-level synchronized block is closed, clear any cached data
					cacheManager.Dispose();
				}
			}
			
			public override CacheManager CacheManager {
				// I expect CompositeTypeResolveContext to be used for almost all resolver operations,
				// so this is the only place where implementing CacheManager is really important.
				get { return cacheManager; }
			}
			
			public override ISynchronizedTypeResolveContext Synchronize()
			{
				// re-use the same cache manager for nested synchronized contexts
				if (isTopLevel)
					return new CompositeSynchronizedTypeResolveContext(children, cacheManager, false);
				else
					return this;
			}
		}
	}
}
