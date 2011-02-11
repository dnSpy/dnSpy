// Copyright (c) 2010 AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using ICSharpCode.NRefactory.TypeSystem;

namespace ICSharpCode.NRefactory.CSharp.Resolver
{
	/// <summary>
	/// Represents a resolve error.
	/// </summary>
	public class ErrorResolveResult : ResolveResult
	{
		public ErrorResolveResult(IType type) : base(type)
		{
		}
		
		public override bool IsError {
			get { return true; }
		}
	}
}
