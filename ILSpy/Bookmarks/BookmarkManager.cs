// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel.Composition;
using System.Linq;
using ICSharpCode.Decompiler;
using ICSharpCode.NRefactory;
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.ILSpy.TreeNodes;
using dnlib.DotNet;

namespace ICSharpCode.ILSpy.Bookmarks
{
	[Export(typeof(IPlugin))]
	class BookmarkManagerPlugin : IPlugin
	{
		public void OnLoaded()
		{
			BookmarkManager.Initialize();
		}
	}

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

		internal static void Initialize()
		{
			MainWindow.Instance.CurrentAssemblyListChanged += MainWindow_CurrentAssemblyListChanged;
			MainWindow.Instance.OnModuleModified += MainWindow_OnModuleModified;
		}

		static void MainWindow_CurrentAssemblyListChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			if (e.OldItems != null) {
				foreach (var bm in bookmarks.ToArray()) {
					if (bm.MemberReference.Module == null || e.OldItems.Cast<LoadedAssembly>().Any(n => n.ModuleDefinition == bm.MemberReference.Module))
						RemoveMark(bm);
				}
			}
		}

		static void MainWindow_OnModuleModified(object sender, MainWindow.ModuleModifiedEventArgs e)
		{
			foreach (var bm in bookmarks.ToArray()) {
				if (MustDelete(bm.MemberReference))
					RemoveMark(bm);
			}
		}

		static bool MustDelete(IMemberRef mr)
		{
			if (mr == null || mr.Module == null)
				return true;
			if (!(mr is IType) && mr.DeclaringType == null)
				return true;
			var td = mr as TypeDef;
			if (td != null) {
				for (int i = 0; i < 100 && td.DeclaringType != null; i++)
					td = td.DeclaringType;
				if (td.DeclaringType == null && td.Module.Types.IndexOf(td) < 0)
					return true;
			}

			return false;
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

		public static void RemoveMarks<T>() where T : BookmarkBase
		{
			for (int i = Bookmarks.Count - 1; i >= 0; i--) {
				if (BookmarkManager.Bookmarks[i] is T)
					BookmarkManager.RemoveMark(BookmarkManager.Bookmarks[i]);
			}
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
		
		public static event BookmarkEventHandler Removed;
		public static event BookmarkEventHandler Added;
	}
}
