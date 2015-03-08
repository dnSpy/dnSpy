// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;

namespace ICSharpCode.ILSpy.Bookmarks
{
	public delegate void BookmarkEventHandler(object sender, BookmarkEventArgs e);
	
	/// <summary>
	/// Description of BookmarkEventHandler.
	/// </summary>
	public class BookmarkEventArgs : EventArgs
	{
		BookmarkBase bookmark;
		
		public BookmarkBase Bookmark {
			get {
				return bookmark;
			}
		}
		
		public BookmarkEventArgs(BookmarkBase bookmark)
		{
			this.bookmark = bookmark;
		}
	}
}
