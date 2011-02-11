// Copyright (c) 2010 AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;

namespace ICSharpCode.NRefactory.TypeSystem
{
	public enum EntityType : byte
	{
		None,
		TypeDefinition,
		Field,
		Property,
		Indexer,
		Event,
		Method,
		Operator,
		Constructor,
		Destructor
	}
}
