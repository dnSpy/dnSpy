// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

namespace Debugger
{
	public enum ExceptionType
	{
		FirstChance = 1,
		UserFirstChance = 2,
		CatchHandlerFound = 3,
		Unhandled = 4,
	}
}
