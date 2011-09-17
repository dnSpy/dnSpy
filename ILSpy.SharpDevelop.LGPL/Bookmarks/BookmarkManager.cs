// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using ICSharpCode.Decompiler;
using ICSharpCode.NRefactory;
using ICSharpCode.NRefactory.CSharp;
using Mono.Cecil;
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
		
		public static List<BookmarkBase> GetBookmarks(string typeName)
		{
			if (typeName == null)
				throw new ArgumentNullException("typeName");
			
			List<BookmarkBase> marks = new List<BookmarkBase>();
			
			foreach (BookmarkBase mark in bookmarks) {
				if (typeName == mark.MemberReference.FullName) {
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
			if (a.MemberReference.FullName != b.MemberReference.FullName)
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
		                                  Func<TextLocation, BookmarkBase> bookmarkFactory)
		{
			foreach (BookmarkBase bookmark in GetBookmarks(typeName)) {
				if (canToggle(bookmark) && bookmark.LineNumber == line) {
					BookmarkManager.RemoveMark(bookmark);
					return;
				}
			}
			
			// no bookmark at that line: create a new bookmark
			BookmarkManager.AddMark(bookmarkFactory(new TextLocation(line, 0)));
		}
		
		public static event BookmarkEventHandler Removed;
		public static event BookmarkEventHandler Added;
	}
}
