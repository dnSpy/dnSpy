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
using System.Linq;
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.TypeSystem.Implementation;

namespace ICSharpCode.NRefactory.CSharp.Resolver
{
	/// <summary>
	/// Represents a scope that contains "using" statements.
	/// This is either the file itself, or a namespace declaration.
	/// </summary>
	[Serializable]
	public class UsingScope : AbstractFreezable
	{
		readonly IProjectContent projectContent;
		readonly UsingScope parent;
		DomRegion region;
		string namespaceName = "";
		IList<ITypeOrNamespaceReference> usings;
		IList<KeyValuePair<string, ITypeOrNamespaceReference>> usingAliases;
		IList<string> externAliases;
		//IList<UsingScope> childScopes;
		
		protected override void FreezeInternal()
		{
			if (usings == null || usings.Count == 0)
				usings = EmptyList<ITypeOrNamespaceReference>.Instance;
			else
				usings = Array.AsReadOnly(usings.ToArray());
			
			if (usingAliases == null || usingAliases.Count == 0)
				usingAliases = EmptyList<KeyValuePair<string, ITypeOrNamespaceReference>>.Instance;
			else
				usingAliases = Array.AsReadOnly(usingAliases.ToArray());
			
			externAliases = FreezeList(externAliases);
			//childScopes = FreezeList(childScopes);
			
			// In current model (no child scopes), it makes sense to freeze the parent as well
			// to ensure the whole lookup chain is immutable.
			// But we probably shouldn't do this if we add back childScopes.
			if (parent != null)
				parent.Freeze();
			
			base.FreezeInternal();
		}
		
		/// <summary>
		/// Creates a new root using scope.
		/// </summary>
		public UsingScope(IProjectContent projectContent)
		{
			if (projectContent == null)
				throw new ArgumentNullException("projectContent");
			this.projectContent = projectContent;
		}
		
		/// <summary>
		/// Creates a new nested using scope.
		/// </summary>
		/// <param name="parent">The parent using scope.</param>
		/// <param name="namespaceName">The full namespace name.</param>
		public UsingScope(UsingScope parent, string namespaceName)
		{
			if (parent == null)
				throw new ArgumentNullException("parent");
			if (namespaceName == null)
				throw new ArgumentNullException("namespaceName");
			this.parent = parent;
			this.projectContent = parent.projectContent;
			this.namespaceName = namespaceName;
		}
		
		public UsingScope Parent {
			get { return parent; }
		}
		
		public IProjectContent ProjectContent {
			get { return projectContent; }
		}
		
		public DomRegion Region {
			get { return region; }
			set {
				CheckBeforeMutation();
				region = value;
			}
		}
		
		public string NamespaceName {
			get { return namespaceName; }
			set {
				if (value == null)
					throw new ArgumentNullException("NamespaceName");
				CheckBeforeMutation();
				namespaceName = value;
			}
		}
		
		public IList<ITypeOrNamespaceReference> Usings {
			get {
				if (usings == null)
					usings = new List<ITypeOrNamespaceReference>();
				return usings;
			}
		}
		
		public IList<KeyValuePair<string, ITypeOrNamespaceReference>> UsingAliases {
			get {
				if (usingAliases == null)
					usingAliases = new List<KeyValuePair<string, ITypeOrNamespaceReference>>();
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
	}
}
