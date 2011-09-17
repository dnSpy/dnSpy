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

namespace ICSharpCode.NRefactory.TypeSystem.Implementation
{
	/// <summary>
	/// Proxy that forwards calls to another TypeResolveContext.
	/// Useful as base class for decorators.
	/// </summary>
	[Serializable]
	public class ProxyTypeResolveContext : ITypeResolveContext
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
		
		/// <inheritdoc/>
		public virtual ITypeDefinition GetKnownTypeDefinition(TypeCode typeCode)
		{
			return target.GetKnownTypeDefinition(typeCode);
		}
	}
}
