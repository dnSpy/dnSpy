// Copyright (c) 2010 AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;

namespace ICSharpCode.NRefactory.TypeSystem
{
	/// <summary>
	/// Represents a property or indexer.
	/// </summary>
	public interface IProperty : IParameterizedMember
	{
		bool CanGet { get; }
		bool CanSet { get; }
		
		IAccessor Getter { get; }
		IAccessor Setter { get; }
		
		bool IsIndexer { get; }
	}
}
