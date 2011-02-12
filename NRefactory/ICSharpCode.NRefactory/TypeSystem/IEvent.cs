// Copyright (c) 2010 AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Diagnostics.Contracts;

namespace ICSharpCode.NRefactory.TypeSystem
{
	public interface IEvent : IMember
	{
		bool CanAdd { get; }
		bool CanRemove { get; }
		bool CanInvoke { get; }
		
		IAccessor AddAccessor { get; }
		IAccessor RemoveAccessor { get; }
		IAccessor InvokeAccessor { get; }
	}
}
