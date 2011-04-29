// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using ICSharpCode.Decompiler;
using ICSharpCode.NRefactory.CSharp;
using Mono.Cecil;
using Mono.CSharp;

namespace ICSharpCode.ILSpy.Debugger.Bookmarks
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
		                                  Func<AstLocation, BookmarkBase> bookmarkFactory)
		{
			foreach (BookmarkBase bookmark in GetBookmarks(typeName)) {
				if (canToggle(bookmark) && bookmark.LineNumber == line) {
					BookmarkManager.RemoveMark(bookmark);
					return;
				}
			}
			
			// no bookmark at that line: create a new bookmark
			BookmarkManager.AddMark(bookmarkFactory(new AstLocation(line, 0)));
		}
		
		public static event BookmarkEventHandler Removed;
		public static event BookmarkEventHandler Added;
	}
	
	// This is for the synchronize bookmarks logic
	public static partial class BookmarkManager
	{
		static BookmarkManager()
		{
			DebugData.LanguageChanged += OnLanguageChanged;
		}

		static void OnLanguageChanged(object sender, LanguageEventArgs e)
		{
			var oldLanguage = e.OldLanguage;
			var newLanguage = e.NewLanguage;
			
			SyncCurrentLineBookmark(oldLanguage, newLanguage);
			//SyncBreakpointBookmarks(oldLanguage, newLanguage);
		}
		
		/// <summary>
		/// Synchronize the IL<->C# current line marker.
		/// </summary>
		/// <param name="oldLanguage">Old language.</param>
		/// <param name="newLanguage">New language.</param>
		static void SyncCurrentLineBookmark(DecompiledLanguages oldLanguage, DecompiledLanguages newLanguage)
		{
			// checks
			if (CurrentLineBookmark.Instance == null)
				return;
			
			var oldMappings = DebugData.OldCodeMappings;
			var newMappings = DebugData.CodeMappings;

			if (oldMappings == null || newMappings == null)
				return;
			
			// 1. Save it's data
			int line = CurrentLineBookmark.Instance.LineNumber;
			var markerType = CurrentLineBookmark.Instance.MemberReference;
			
			if (!oldMappings.ContainsKey(markerType.FullName) || !newMappings.ContainsKey(markerType.FullName))
				return;
			
			// 2. Remove it
			CurrentLineBookmark.Remove();
			
			// 3. map the marker line
			uint token;
			var instruction = oldMappings[markerType.FullName].GetInstructionByLineNumber(line, out token);
			if (instruction == null)
				return;

			MemberReference memberReference;
			int newline;
			if (newMappings[markerType.FullName].GetSourceCodeFromMetadataTokenAndOffset(token, instruction.ILInstructionOffset.From, out memberReference, out newline)) {
				// 4. create breakpoint for new languages
				CurrentLineBookmark.SetPosition(memberReference, newline, 0, newline, 0);
			}
		}

		/// <summary>
		/// Synchronize the IL<->C# breakpoints bookmarks.
		/// </summary>
		/// <param name="oldLanguage">Old language.</param>
		/// <param name="newLanguage">New language.</param>
		static void SyncBreakpointBookmarks(DecompiledLanguages oldLanguage, DecompiledLanguages newLanguage)
		{
			// checks
			var oldMappings = DebugData.OldCodeMappings;
			var newMappings = DebugData.CodeMappings;

			if (oldMappings == null || newMappings == null)
				return;
			
			// 1. map the breakpoint lines
			var oldbps = bookmarks.FindAll(b => b is BreakpointBookmark &&
			                               ((BreakpointBookmark)b).Language == oldLanguage);
			if (oldbps == null || oldbps.Count == 0)
				return;

			foreach (var bp in oldbps) {
				uint token;
				string name = bp.MemberReference.FullName;
				if (!oldMappings.ContainsKey(name) || !newMappings.ContainsKey(name))
					continue;
				
				var instruction = oldMappings[name].GetInstructionByLineNumber(bp.LineNumber, out token);
				if (instruction == null)
					continue;

				MemberReference memberReference;
				int line;
				if (newMappings[name].GetSourceCodeFromMetadataTokenAndOffset(token, instruction.ILInstructionOffset.From, out memberReference, out line)) {
					// 2. create breakpoint for new languages
					var bookmark = new BreakpointBookmark(memberReference, new AstLocation(line, 0), instruction.ILInstructionOffset, BreakpointAction.Break, newLanguage);
					AddMark(bookmark);
				}
			}

			// 3. remove all breakpoints for the old language
			for (int i = bookmarks.Count - 1; i >= 0; --i) {
				var bm = bookmarks[i];
				if (bm is BreakpointBookmark && ((BreakpointBookmark)bm).Language == oldLanguage)
					RemoveMark(bm);
			}
		}
	}
}
