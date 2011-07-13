// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;

using ICSharpCode.ILSpy.Bookmarks;
using ICSharpCode.NRefactory.CSharp;

namespace ICSharpCode.ILSpy.AvalonEdit
{
	/// <summary>
	/// Stores the entries in the icon bar margin. Multiple icon bar margins
	/// can use the same manager if split view is used.
	/// </summary>
	public class IconBarManager : IBookmarkMargin
	{
		ObservableCollection<IBookmark> bookmarks = new ObservableCollection<IBookmark>();
		
		public IconBarManager()
		{
			bookmarks.CollectionChanged += bookmarks_CollectionChanged;
		}
		
		public IList<IBookmark> Bookmarks {
			get { return bookmarks; }
		}
		
		void bookmarks_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			Redraw();
		}
		
		public void Redraw()
		{
			if (RedrawRequested != null)
				RedrawRequested(this, EventArgs.Empty);
		}
		
		public event EventHandler RedrawRequested;
		
		public void UpdateClassMemberBookmarks(IEnumerable<AstNode> nodes, Type bookmarkType, Type memberType)
		{
			this.bookmarks.Clear();
			
			if (nodes == null || nodes.Count() == 0)
				return;
			
			foreach (var n in nodes) {
				switch (n.NodeType) {
					case NodeType.TypeDeclaration:
					case NodeType.TypeReference:
						this.bookmarks.Add(Activator.CreateInstance(bookmarkType, n) as IBookmark);
						break;
					case NodeType.Member:
						this.bookmarks.Add(Activator.CreateInstance(memberType, n) as IBookmark);
						break;
					default:
						// do nothing
						break;
				}
			}
		}
	}
}
