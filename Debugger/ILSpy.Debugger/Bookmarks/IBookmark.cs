// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows.Input;
using System.Windows.Media;

namespace ICSharpCode.ILSpy.Debugger.Bookmarks
{
	/// <summary>
	/// The bookmark margin.
	/// </summary>
	public interface IBookmarkMargin
	{
		/// <summary>
		/// Gets the list of bookmarks.
		/// </summary>
		IList<BookmarkBase> Bookmarks { get; }
		
		/// <summary>
		/// Redraws the bookmark margin. Bookmarks need to call this method when the Image changes.
		/// </summary>
		void Redraw();
	}
	
	/// <summary>
	/// Represents a bookmark in the bookmark margin.
	/// </summary>
	public interface IBookmark
	{
		/// <summary>
		/// Gets the line number of the bookmark.
		/// </summary>
		int LineNumber { get; }
		
		/// <summary>
		/// Gets the image.
		/// </summary>
		ImageSource Image { get; }
		
		/// <summary>
		/// Gets the Z-Order of the bookmark icon.
		/// </summary>
		int ZOrder { get; }
		
		/// <summary>
		/// Handles the mouse down event.
		/// </summary>
		void MouseDown(MouseButtonEventArgs e);
		
		/// <summary>
		/// Handles the mouse up event.
		/// </summary>
		void MouseUp(MouseButtonEventArgs e);
		
		/// <summary>
		/// Gets whether this bookmark can be dragged around.
		/// </summary>
		bool CanDragDrop { get; }
		
		/// <summary>
		/// Notifies the bookmark that it was dropped on the specified line.
		/// </summary>
		void Drop(int lineNumber);
	}
}
