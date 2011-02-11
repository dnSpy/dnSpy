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
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows.Input;
using System.Windows.Media;

namespace ILSpy.Debugger.Bookmarks
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
