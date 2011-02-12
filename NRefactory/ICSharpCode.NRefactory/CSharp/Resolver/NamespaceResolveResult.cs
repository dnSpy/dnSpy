// Copyright (c) 2010 AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using ICSharpCode.NRefactory.TypeSystem;

namespace ICSharpCode.NRefactory.CSharp.Resolver
{
	/// <summary>
	/// Represents that an expression resolved to a namespace.
	/// </summary>
	public class NamespaceResolveResult : ResolveResult
	{
		readonly string namespaceName;
		
		public NamespaceResolveResult(string namespaceName) : base(SharedTypes.UnknownType)
		{
			this.namespaceName = namespaceName;
		}
		
		public string NamespaceName {
			get { return namespaceName; }
		}
		
		public override string ToString()
		{
			return string.Format("[{0} {1}]", GetType().Name, namespaceName);
		}
	}
}
