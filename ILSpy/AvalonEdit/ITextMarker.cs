// Copyright (c) 2014 AlphaSierraPapa for the SharpDevelop Team
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
using System.Windows;
using System.Windows.Media;

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
		/// Gets/Sets the font weight.
		/// </summary>
		FontWeight? FontWeight { get; set; }
		
		/// <summary>
		/// Gets/Sets the font style.
		/// </summary>
		FontStyle? FontStyle { get; set; }
		
		/// <summary>
		/// Gets/Sets the type of the marker. Use TextMarkerType.None for normal markers.
		/// </summary>
		TextMarkerTypes MarkerTypes { get; set; }
		
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
	}
	
	[Flags]
	public enum TextMarkerTypes
	{
		/// <summary>
		/// Use no marker
		/// </summary>
		None = 0x0000,
		/// <summary>
		/// Use squiggly underline marker
		/// </summary>
		SquigglyUnderline = 0x001,
		/// <summary>
		/// Normal underline.
		/// </summary>
		NormalUnderline = 0x002,
		/// <summary>
		/// Dotted underline.
		/// </summary>
		DottedUnderline = 0x004,
		
		/// <summary>
		/// Horizontal line in the scroll bar.
		/// </summary>
		LineInScrollBar = 0x0100,
		/// <summary>
		/// Small triangle in the scroll bar, pointing to the right.
		/// </summary>
		ScrollBarRightTriangle = 0x0400,
		/// <summary>
		/// Small triangle in the scroll bar, pointing to the left.
		/// </summary>
		ScrollBarLeftTriangle = 0x0800,
		/// <summary>
		/// Small circle in the scroll bar.
		/// </summary>
		CircleInScrollBar = 0x1000
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
		
		/// <summary>
		/// Finds all text markers at the specified offset.
		/// </summary>
		IEnumerable<ITextMarker> GetMarkersAtOffset(int offset);
	}
}
