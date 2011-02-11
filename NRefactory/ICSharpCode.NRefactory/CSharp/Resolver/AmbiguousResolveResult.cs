// Copyright (c) 2010 AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using ICSharpCode.NRefactory.TypeSystem;

namespace ICSharpCode.NRefactory.CSharp.Resolver
{
	/// <summary>
	/// Represents an ambiguous type resolve result.
	/// </summary>
	public class AmbiguousTypeResolveResult : TypeResolveResult
	{
		public AmbiguousTypeResolveResult(IType type) : base(type)
		{
		}
		
		public override bool IsError {
			get { return true; }
		}
	}
	
	public class AmbiguousMemberResultResult : MemberResolveResult
	{
		public AmbiguousMemberResultResult(IMember member, IType returnType) : base(member, returnType)
		{
		}
		
		public override bool IsError {
			get { return true; }
		}
	}
}
