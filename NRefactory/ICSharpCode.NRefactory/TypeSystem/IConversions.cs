// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;

namespace ICSharpCode.NRefactory.TypeSystem
{
	/// <summary>
	/// Interface used to check whether types are convertible.
	/// </summary>
	public interface IConversions
	{
		bool ImplicitConversion(IType fromType, IType toType);
		bool ImplicitReferenceConversion(IType fromType, IType toType);
	}
}
