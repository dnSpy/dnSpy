// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;

namespace ICSharpCode.ILSpy.Debugger.Bookmarks
{
	public class BreakpointBookmarkEventArgs : EventArgs
	{
		BreakpointBookmark breakpointBookmark;

		public BreakpointBookmark BreakpointBookmark {
			get {
				return breakpointBookmark;
			}
		}

		public BreakpointBookmarkEventArgs(BreakpointBookmark breakpointBookmark)
		{
			this.breakpointBookmark = breakpointBookmark;
		}
	}
}
