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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using ICSharpCode.NRefactory.TypeSystem.Implementation;
using ICSharpCode.NRefactory.CSharp.Resolver;
using ICSharpCode.NRefactory.Semantics;
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.Utils;

namespace ICSharpCode.NRefactory.CSharp.TypeSystem
{
	/// <summary>
	/// Resolved version of using scope.
	/// </summary>
	public class ResolvedUsingScope
	{
		readonly CSharpTypeResolveContext parentContext;
		readonly UsingScope usingScope;
		
		internal readonly ConcurrentDictionary<string, ResolveResult> ResolveCache = new ConcurrentDictionary<string, ResolveResult>();
		internal List<List<IMethod>> AllExtensionMethods;
		
		public ResolvedUsingScope(CSharpTypeResolveContext context, UsingScope usingScope)
		{
			if (context == null)
				throw new ArgumentNullException("context");
			if (usingScope == null)
				throw new ArgumentNullException("usingScope");
			this.parentContext = context;
			this.usingScope = usingScope;
			if (usingScope.Parent != null) {
				if (context.CurrentUsingScope == null)
					throw new InvalidOperationException();
			} else {
				if (context.CurrentUsingScope != null)
					throw new InvalidOperationException();
			}
		}
		
		public UsingScope UnresolvedUsingScope {
			get { return usingScope; }
		}
		
		INamespace @namespace;
		
		public INamespace Namespace {
			get {
				INamespace result = LazyInit.VolatileRead(ref this.@namespace);
				if (result != null) {
					return result;
				} else {
					if (parentContext.CurrentUsingScope != null) {
						result = parentContext.CurrentUsingScope.Namespace.GetChildNamespace(usingScope.ShortNamespaceName);
						if (result == null)
							result = new DummyNamespace(parentContext.CurrentUsingScope.Namespace, usingScope.ShortNamespaceName);
					} else {
						result = parentContext.Compilation.RootNamespace;
					}
					Debug.Assert(result != null);
					return LazyInit.GetOrSet(ref this.@namespace, result);
				}
			}
		}
		
		public ResolvedUsingScope Parent {
			get { return parentContext.CurrentUsingScope; }
		}
		
		IList<INamespace> usings;
		
		public IList<INamespace> Usings {
			get {
				var result = LazyInit.VolatileRead(ref this.usings);
				if (result != null) {
					return result;
				} else {
					result = new List<INamespace>();
					CSharpResolver resolver = new CSharpResolver(parentContext.WithUsingScope(this));
					foreach (var u in usingScope.Usings) {
						INamespace ns = u.ResolveNamespace(resolver);
						if (ns != null && !result.Contains(ns))
							result.Add(ns);
					}
					return LazyInit.GetOrSet(ref this.usings, new ReadOnlyCollection<INamespace>(result));
				}
			}
		}
		
		IList<KeyValuePair<string, ResolveResult>> usingAliases;
		
		public IList<KeyValuePair<string, ResolveResult>> UsingAliases {
			get {
				var result = LazyInit.VolatileRead(ref this.usingAliases);
				if (result != null) {
					return result;
				} else {
					CSharpResolver resolver = new CSharpResolver(parentContext.WithUsingScope(this));
					result = new KeyValuePair<string, ResolveResult>[usingScope.UsingAliases.Count];
					for (int i = 0; i < result.Count; i++) {
						var rr = usingScope.UsingAliases[i].Value.Resolve(resolver);
						if (rr is TypeResolveResult) {
							rr = new AliasTypeResolveResult (usingScope.UsingAliases[i].Key, (TypeResolveResult)rr);
						} else if (rr is NamespaceResolveResult) {
							rr = new AliasNamespaceResolveResult (usingScope.UsingAliases[i].Key, (NamespaceResolveResult)rr);
						}
						result[i] = new KeyValuePair<string, ResolveResult>(
							usingScope.UsingAliases[i].Key,
							rr
						);
					}
					return LazyInit.GetOrSet(ref this.usingAliases, result);
				}
			}
		}
		
		public IList<string> ExternAliases {
			get { return usingScope.ExternAliases; }
		}
		
		/// <summary>
		/// Gets whether this using scope has an alias (either using or extern)
		/// with the specified name.
		/// </summary>
		public bool HasAlias(string identifier)
		{
			return usingScope.HasAlias(identifier);
		}
		
		sealed class DummyNamespace : INamespace
		{
			readonly INamespace parentNamespace;
			readonly string name;
			
			public DummyNamespace(INamespace parentNamespace, string name)
			{
				this.parentNamespace = parentNamespace;
				this.name = name;
			}
			
			public string ExternAlias { get; set; }
			
			string INamespace.FullName {
				get { return NamespaceDeclaration.BuildQualifiedName(parentNamespace.FullName, name); }
			}
			
			public string Name {
				get { return name; }
			}
			
			SymbolKind ISymbol.SymbolKind {
				get { return SymbolKind.Namespace; }
			}
			
			INamespace INamespace.ParentNamespace {
				get { return parentNamespace; }
			}
			
			IEnumerable<INamespace> INamespace.ChildNamespaces {
				get { return EmptyList<INamespace>.Instance; }
			}
			
			IEnumerable<ITypeDefinition> INamespace.Types {
				get { return EmptyList<ITypeDefinition>.Instance; }
			}
			
			IEnumerable<IAssembly> INamespace.ContributingAssemblies {
				get { return EmptyList<IAssembly>.Instance; }
			}
			
			ICompilation ICompilationProvider.Compilation {
				get { return parentNamespace.Compilation; }
			}
			
			INamespace INamespace.GetChildNamespace(string name)
			{
				return null;
			}
			
			ITypeDefinition INamespace.GetTypeDefinition(string name, int typeParameterCount)
			{
				return null;
			}

			public ISymbolReference ToReference()
			{
				return new MergedNamespaceReference(ExternAlias, ((INamespace)this).FullName);
			}
		}
	}
}
