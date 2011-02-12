// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Text;

namespace ICSharpCode.SharpDevelop.Dom
{
	public class DefaultUsing : AbstractFreezable, IUsing
	{
		DomRegion region;
		IProjectContent projectContent;
		
		public DefaultUsing(IProjectContent projectContent)
		{
			this.projectContent = projectContent;
		}
		
		public DefaultUsing(IProjectContent projectContent, DomRegion region) : this(projectContent)
		{
			this.region = region;
		}
		
		IList<string> usings  = new List<string>();
		IDictionary<string, IReturnType> aliases = null;
		
		protected override void FreezeInternal()
		{
			usings = FreezeList(usings);
			if (aliases != null)
				aliases = new ReadOnlyDictionary<string, IReturnType>(aliases);
			base.FreezeInternal();
		}
		
		public DomRegion Region {
			get {
				return region;
			}
		}
		
		public IList<string> Usings {
			get {
				return usings;
			}
		}
		
		public IDictionary<string, IReturnType> Aliases {
			get {
				return aliases;
			}
		}
		
		public bool HasAliases {
			get {
				return aliases != null && aliases.Count > 0;
			}
		}
		
		public void AddAlias(string alias, IReturnType type)
		{
			CheckBeforeMutation();
			if (aliases == null) aliases = new SortedList<string, IReturnType>();
			aliases.Add(alias, type);
		}
		
		public string SearchNamespace(string partialNamespaceName)
		{
			if (HasAliases) {
				foreach (KeyValuePair<string, IReturnType> entry in aliases) {
					string aliasString = entry.Key;
					string nsName;
					if (projectContent.Language.NameComparer.Equals(partialNamespaceName, aliasString)) {
						nsName = entry.Value.FullyQualifiedName;
						if (projectContent.NamespaceExists(nsName))
							return nsName;
					}
					if (partialNamespaceName.Length > aliasString.Length) {
						if (projectContent.Language.NameComparer.Equals(partialNamespaceName.Substring(0, aliasString.Length + 1), aliasString + ".")) {
							nsName = String.Concat(entry.Value.FullyQualifiedName, partialNamespaceName.Remove(0, aliasString.Length));
							if (projectContent.NamespaceExists(nsName)) {
								return nsName;
							}
						}
					}
				}
			}
			if (projectContent.Language.ImportNamespaces) {
				foreach (string str in usings) {
					string possibleNamespace = String.Concat(str, ".", partialNamespaceName);
					if (projectContent.NamespaceExists(possibleNamespace))
						return possibleNamespace;
				}
			}
			return null;
		}
		
		/// <summary>
		/// Returns a collection of possible types that could be meant when using this Import
		/// to search the type.
		/// Types with the incorrect type parameter count might be returned, but for each
		/// same using entry or alias entry at most one (the best matching) type should be returned.
		/// </summary>
		public IEnumerable<IReturnType> SearchType(string partialTypeName, int typeParameterCount)
		{
			if (HasAliases) {
				foreach (KeyValuePair<string, IReturnType> entry in aliases) {
					string aliasString = entry.Key;
					if (projectContent.Language.NameComparer.Equals(partialTypeName, aliasString)) {
						if (entry.Value.GetUnderlyingClass() == null)
							continue; // type not found, maybe entry was a namespace
						yield return entry.Value;
					}
					if (partialTypeName.Length > aliasString.Length) {
						if (projectContent.Language.NameComparer.Equals(partialTypeName.Substring(0, aliasString.Length + 1), aliasString + ".")) {
							string className = entry.Value.FullyQualifiedName + partialTypeName.Remove(0, aliasString.Length);
							IClass c = projectContent.GetClass(className, typeParameterCount);
							if (c != null) {
								yield return c.DefaultReturnType;
							}
						}
					}
				}
			}
			if (projectContent.Language.ImportNamespaces) {
				foreach (string str in usings) {
					IClass c = projectContent.GetClass(str + "." + partialTypeName, typeParameterCount);
					if (c != null) {
						yield return c.DefaultReturnType;
					}
				}
			} else {
				int pos = partialTypeName.IndexOf('.');
				string className, subClassName;
				if (pos < 0) {
					className = partialTypeName;
					subClassName = null;
				} else {
					className = partialTypeName.Substring(0, pos);
					subClassName = partialTypeName.Substring(pos + 1);
				}
				foreach (string str in usings) {
					IClass c = projectContent.GetClass(str + "." + className, typeParameterCount);
					if (c != null) {
						c = projectContent.GetClass(str + "." + partialTypeName, typeParameterCount);
						if (c != null) {
							yield return c.DefaultReturnType;
						}
					}
				}
			}
		}
		
		public override string ToString()
		{
			StringBuilder builder = new StringBuilder("[DefaultUsing: ");
			foreach (string str in usings) {
				builder.Append(str);
				builder.Append(", ");
			}
			if (HasAliases) {
				foreach (KeyValuePair<string, IReturnType> p in aliases) {
					builder.Append(p.Key);
					builder.Append("=");
					builder.Append(p.Value.ToString());
					builder.Append(", ");
				}
			}
			builder.Length -= 2; // remove last ", "
			builder.Append("]");
			return builder.ToString();
		}
	}
}
