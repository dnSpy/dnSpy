// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Windows.Media;

using ICSharpCode.ILSpy.Bookmarks;

namespace ICSharpCode.ILSpy.AvalonEdit
{
	/// <summary>
	/// Represents a text marker.
	/// </summary>
	public interface ITextMarker
	{
		/// <summary>
		/// Gets the start offset of the marked text region.
		/// </summary>
		int StartOffset { get; }
		
		/// <summary>
		/// Gets the end offset of the marked text region.
		/// </summary>
		int EndOffset { get; }
		
		/// <summary>
		/// Gets the length of the marked region.
		/// </summary>
		int Length { get; }
		
		/// <summary>
		/// Deletes the text marker.
		/// </summary>
		void Delete();
		
		/// <summary>
		/// Gets whether the text marker was deleted.
		/// </summary>
		bool IsDeleted { get; }
		
		/// <summary>
		/// Event that occurs when the text marker is deleted.
		/// </summary>
		event EventHandler Deleted;
		
		/// <summary>
		/// Gets/Sets the background color.
		/// </summary>
		Color? BackgroundColor { get; set; }
		
		/// <summary>
		/// Gets/Sets the foreground color.
		/// </summary>
		Color? ForegroundColor { get; set; }
		
		/// <summary>
		/// Gets/Sets the type of the marker. Use TextMarkerType.None for normal markers.
		/// </summary>
		TextMarkerType MarkerType { get; set; }
		
		/// <summary>
		/// Gets/Sets the color of the marker.
		/// </summary>
		Color MarkerColor { get; set; }
		
		/// <summary>
		/// Gets/Sets an object with additional data for this text marker.
		/// </summary>
		object Tag { get; set; }
		
		/// <summary>
		/// Gets/Sets an object that will be displayed as tooltip in the text editor.
		/// </summary>
		object ToolTip { get; set; }
		
		/// <summary>
		/// Gets or sets if the marker is visible or not.
		/// </summary>
		Predicate<object> IsVisible { get; set; }
		
		/// <summary>
		/// Gets or sets the bookmark.
		/// </summary>
		IBookmark Bookmark { get; set; }
	}
	
	public enum TextMarkerType
	{
		/// <summary>
		/// Use no marker
		/// </summary>
		None,
		/// <summary>
		/// Use squiggly underline marker
		/// </summary>
		SquigglyUnderline
	}
	
	public interface ITextMarkerService
	{
		/// <summary>
		/// Creates a new text marker. The text marker will be invisible at first,
		/// you need to set one of the Color properties to make it visible.
		/// </summary>
		ITextMarker Create(int startOffset, int length);
		
		/// <summary>
		/// Gets the list of text markers.
		/// </summary>
		IEnumerable<ITextMarker> TextMarkers { get; }
		
		/// <summary>
		/// Removes the specified text marker.
		/// </summary>
		void Remove(ITextMarker marker);
		
		/// <summary>
		/// Removes all text markers that match the condition.
		/// </summary>
		void RemoveAll(Predicate<ITextMarker> predicate);
	}
}
