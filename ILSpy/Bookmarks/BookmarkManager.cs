// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using ICSharpCode.Decompiler;
using ICSharpCode.NRefactory;
using ICSharpCode.NRefactory.CSharp;
using dnlib.DotNet;
using Mono.CSharp;

namespace ICSharpCode.ILSpy.Bookmarks
{
	/// <summary>
	/// Static class that maintains the list of bookmarks and breakpoints.
	/// </summary>
	public static partial class BookmarkManager
	{
		static List<BookmarkBase> bookmarks = new List<BookmarkBase>();
		
		public static List<BookmarkBase> Bookmarks {
			get {
				return bookmarks;
			}
		}
		
		public static void AddMark(BookmarkBase bookmark)
		{
			if (bookmark == null) return;
			if (bookmarks.Contains(bookmark)) return;
			bookmarks.Add(bookmark);
			OnAdded(new BookmarkEventArgs(bookmark));
		}

		public static void ReplaceMark(int index, BookmarkBase bookmark)
		{
			var removedBookmark = bookmarks[index];
			bookmarks.RemoveAt(index);
			OnRemoved(new BookmarkEventArgs(removedBookmark));

			bookmarks.Insert(index, bookmark);
			OnAdded(new BookmarkEventArgs(bookmark));
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
		
		public static bool ToggleBookmark(TextLocation location, TextLocation endLocation,
		                                  Predicate<BookmarkBase> canToggle,
										  Func<TextLocation, TextLocation, BookmarkBase> bookmarkFactory)
		{
			foreach (BookmarkBase bookmark in Bookmarks) {
				if (canToggle(bookmark) && bookmark.Location == location && bookmark.EndLocation == endLocation) {
					BookmarkManager.RemoveMark(bookmark);
					return false;
				}
			}
			
			// no bookmark at that line: create a new bookmark
			BookmarkManager.AddMark(bookmarkFactory(location, endLocation));
			return true;
		}
		
		public static event BookmarkEventHandler Removed;
		public static event BookmarkEventHandler Added;
	}
}
