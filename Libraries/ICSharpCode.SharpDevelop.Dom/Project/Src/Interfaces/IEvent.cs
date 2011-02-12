// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;

namespace ICSharpCode.SharpDevelop.Dom
{
	public interface IEvent : IMember
	{
		IMethod AddMethod {
			get;
		}
		
		IMethod RemoveMethod {
			get;
		}
		
		IMethod RaiseMethod {
			get;
		}
	}
}
