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
using System.Windows;
using System.Windows.Media;
using ICSharpCode.AvalonEdit.Highlighting;

namespace dnSpy.Contracts.Files.Tabs.DocViewer {
	/// <summary>
	/// Represents a text marker.
	/// </summary>
	public interface ITextMarker {
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
		/// Gets the highlighting color
		/// </summary>
		Func<HighlightingColor> HighlightingColor { get; set; }

		/// <summary>
		/// Gets/Sets the background color.
		/// </summary>
		Color? BackgroundColor { get; }

		/// <summary>
		/// Gets/Sets the foreground color.
		/// </summary>
		Color? ForegroundColor { get; }

		/// <summary>
		/// Gets/Sets the font weight.
		/// </summary>
		FontWeight? FontWeight { get; }

		/// <summary>
		/// Gets/Sets the font style.
		/// </summary>
		FontStyle? FontStyle { get; }

		/// <summary>
		/// Gets/Sets the type of the marker. Use TextMarkerType.None for normal markers.
		/// </summary>
		TextMarkerTypes MarkerTypes { get; set; }

		/// <summary>
		/// Gets/Sets the color of the marker.
		/// </summary>
		Color MarkerColor { get; set; }

		/// <summary>
		/// Gets or sets if the marker is visible or not.
		/// </summary>
		Predicate<object> IsVisible { get; set; }

		/// <summary>
		/// Gets or sets the text obj
		/// </summary>
		ITextMarkerObject TextMarkerObject { get; set; }

		/// <summary>
		/// Gets or sets the Z-order
		/// </summary>
		double ZOrder { get; set; }

		/// <summary>
		/// Forces a redraw
		/// </summary>
		void Redraw();
	}

	/// <summary>
	/// Text marker types
	/// </summary>
	[Flags]
	public enum TextMarkerTypes {
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

	/// <summary>
	/// Handles the text markers for a code editor.
	/// </summary>
	public interface ITextMarkerService {
		/// <summary>
		/// Gets the text view
		/// </summary>
		ICSharpCode.AvalonEdit.Rendering.TextView TextView { get; }

		/// <summary>
		/// Creates a new text marker. The text marker will be invisible at first,
		/// you need to set one of the Color properties to make it visible.
		/// </summary>
		ITextMarker Create(int startOffset, int length);

		/// <summary>
		/// Removes the specified text marker.
		/// </summary>
		void Remove(ITextMarker marker);
	}
}
