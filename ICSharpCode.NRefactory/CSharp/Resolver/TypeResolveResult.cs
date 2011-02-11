// Copyright (c) 2010 AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using ICSharpCode.NRefactory.TypeSystem;

namespace ICSharpCode.NRefactory.CSharp.Resolver
{
	/// <summary>
	/// The resolved expression refers to a type name.
	/// </summary>
	public class TypeResolveResult : ResolveResult
	{
		public TypeResolveResult(IType type)
			: base(type)
		{
		}
	}
}
