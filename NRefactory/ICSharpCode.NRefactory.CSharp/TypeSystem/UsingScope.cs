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
using System.Diagnostics;
using System.Linq;
using ICSharpCode.NRefactory.CSharp.TypeSystem;
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.TypeSystem.Implementation;
using ICSharpCode.NRefactory.Utils;

namespace ICSharpCode.NRefactory.CSharp.TypeSystem
{
	/// <summary>
	/// Represents a scope that contains "using" statements.
	/// This is either the file itself, or a namespace declaration.
	/// </summary>
	[Serializable]
	public class UsingScope : AbstractFreezable
	{
		readonly UsingScope parent;
		DomRegion region;
		string shortName = "";
		IList<TypeOrNamespaceReference> usings;
		IList<KeyValuePair<string, TypeOrNamespaceReference>> usingAliases;
		IList<string> externAliases;
		
		protected override void FreezeInternal()
		{
			usings = FreezableHelper.FreezeList(usings);
			usingAliases = FreezableHelper.FreezeList(usingAliases);
			externAliases = FreezableHelper.FreezeList(externAliases);
			
			// In current model (no child scopes), it makes sense to freeze the parent as well
			// to ensure the whole lookup chain is immutable.
			if (parent != null)
				parent.Freeze();
			
			base.FreezeInternal();
		}
		
		/// <summary>
		/// Creates a new root using scope.
		/// </summary>
		public UsingScope()
		{
		}
		
		/// <summary>
		/// Creates a new nested using scope.
		/// </summary>
		/// <param name="parent">The parent using scope.</param>
		/// <param name="shortName">The short namespace name.</param>
		public UsingScope(UsingScope parent, string shortName)
		{
			if (parent == null)
				throw new ArgumentNullException("parent");
			if (shortName == null)
				throw new ArgumentNullException("shortName");
			this.parent = parent;
			this.shortName = shortName;
		}
		
		public UsingScope Parent {
			get { return parent; }
		}
		
		public DomRegion Region {
			get { return region; }
			set {
				FreezableHelper.ThrowIfFrozen(this);
				region = value;
			}
		}
		
		public string ShortNamespaceName {
			get {
				return shortName;
			}
		}
		
		public string NamespaceName {
			get {
				if (parent != null)
					return NamespaceDeclaration.BuildQualifiedName(parent.NamespaceName, shortName);
				else
					return shortName;
			}
//			set {
//				if (value == null)
//					throw new ArgumentNullException("NamespaceName");
//				FreezableHelper.ThrowIfFrozen(this);
//				namespaceName = value;
//			}
		}
		
		public IList<TypeOrNamespaceReference> Usings {
			get {
				if (usings == null)
					usings = new List<TypeOrNamespaceReference>();
				return usings;
			}
		}
		
		public IList<KeyValuePair<string, TypeOrNamespaceReference>> UsingAliases {
			get {
				if (usingAliases == null)
					usingAliases = new List<KeyValuePair<string, TypeOrNamespaceReference>>();
				return usingAliases;
			}
		}
		
		public IList<string> ExternAliases {
			get {
				if (externAliases == null)
					externAliases = new List<string>();
				return externAliases;
			}
		}
		
//		public IList<UsingScope> ChildScopes {
//			get {
//				if (childScopes == null)
//					childScopes = new List<UsingScope>();
//				return childScopes;
//			}
//		}
		
		/// <summary>
		/// Gets whether this using scope has an alias (either using or extern)
		/// with the specified name.
		/// </summary>
		public bool HasAlias(string identifier)
		{
			if (usingAliases != null) {
				foreach (var pair in usingAliases) {
					if (pair.Key == identifier)
						return true;
				}
			}
			return externAliases != null && externAliases.Contains(identifier);
		}
		
		/// <summary>
		/// Resolves the namespace represented by this using scope.
		/// </summary>
		public ResolvedUsingScope Resolve(ICompilation compilation)
		{
			CacheManager cache = compilation.CacheManager;
			ResolvedUsingScope resolved = cache.GetShared(this) as ResolvedUsingScope;
			if (resolved == null) {
				var csContext = new CSharpTypeResolveContext(compilation.MainAssembly, parent != null ? parent.Resolve(compilation) : null);
				resolved = (ResolvedUsingScope)cache.GetOrAddShared(this, new ResolvedUsingScope(csContext, this));
			}
			return resolved;
		}
	}
}
