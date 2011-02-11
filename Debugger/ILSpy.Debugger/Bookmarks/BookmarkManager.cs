// Copyright (c) 2011 AlphaSierraPapa for the SharpDevelop Team
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

using System;
using System.Collections.Generic;
using ICSharpCode.NRefactory;

namespace ILSpy.Debugger.Bookmarks
{
	/// <summary>
	/// Static class that maintains the list of bookmarks and breakpoints.
	/// </summary>
	public static class BookmarkManager
	{
		static List<BookmarkBase> bookmarks = new List<BookmarkBase>();
		
		public static List<BookmarkBase> Bookmarks {
			get {
				return bookmarks;
			}
		}
		
		public static List<BookmarkBase> GetBookmarks(string typeName)
		{
			if (typeName == null)
				throw new ArgumentNullException("typeName");
			
			List<BookmarkBase> marks = new List<BookmarkBase>();
			
			foreach (BookmarkBase mark in bookmarks) {
				if (typeName == mark.TypeName) {
					marks.Add(mark);
				}
			}
			
			return marks;
		}
		
		public static void AddMark(BookmarkBase bookmark)
		{
			if (bookmark == null) return;
			if (bookmarks.Contains(bookmark)) return;
			if (bookmarks.Exists(b => IsEqualBookmark(b, bookmark))) return;
			bookmarks.Add(bookmark);
			OnAdded(new BookmarkEventArgs(bookmark));
		}
		
		static bool IsEqualBookmark(BookmarkBase a, BookmarkBase b)
		{
			if (a == b)
				return true;
			if (a == null || b == null)
				return false;
			if (a.GetType() != b.GetType())
				return false;
			if (a.TypeName != b.TypeName)
				return false;
			return a.LineNumber == b.LineNumber;
		}
		
		public static void RemoveMark(BookmarkBase bookmark)
		{
			bookmarks.Remove(bookmark);
			OnRemoved(new BookmarkEventArgs(bookmark));
		}
		
		public static void Clear()
		{
			while (bookmarks.Count > 0) {
				var b = bookmarks[bookmarks.Count - 1];
				bookmarks.RemoveAt(bookmarks.Count - 1);
				OnRemoved(new BookmarkEventArgs(b));
			}
		}
		
		internal static void Initialize()
		{
			
		}
		
		static void OnRemoved(BookmarkEventArgs e)
		{
			if (Removed != null) {
				Removed(null, e);
			}
		}
		
		static void OnAdded(BookmarkEventArgs e)
		{
			if (Added != null) {
				 Added(null, e);
			}
		}
		
		public static void ToggleBookmark(string typeName, int line,
		                                  Predicate<BookmarkBase> canToggle,
		                                  Func<Location, BookmarkBase> bookmarkFactory)
		{
			foreach (BookmarkBase bookmark in GetBookmarks(typeName)) {
				if (canToggle(bookmark) && bookmark.LineNumber == line) {
					BookmarkManager.RemoveMark(bookmark);
					return;
				}
			}
			// no bookmark at that line: create a new bookmark			
			BookmarkManager.AddMark(bookmarkFactory(new Location(0, line)));
		}
		
		public static event BookmarkEventHandler Removed;
		public static event BookmarkEventHandler Added;
	}
}
