// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Threading;
using Mono.Cecil;

namespace ICSharpCode.Decompiler
{
	public class DecompilerContext
	{
		public CancellationToken CancellationToken;
		public TypeDefinition CurrentType;
		public MethodDefinition CurrentMethod;
		
		public DecompilerContext Clone()
		{
			return (DecompilerContext)MemberwiseClone();
		}
	}
}
