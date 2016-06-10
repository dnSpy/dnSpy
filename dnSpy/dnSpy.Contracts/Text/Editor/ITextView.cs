/*
    Copyright (C) 2014-2016 de4dot@gmail.com

    This file is part of dnSpy

    dnSpy is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    dnSpy is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with dnSpy.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using dnSpy.Contracts.Command;
using dnSpy.Contracts.Text.Editor.Operations;
using dnSpy.Contracts.Text.Formatting;

namespace dnSpy.Contracts.Text.Editor {
	/// <summary>
	/// Text view
	/// </summary>
	public interface ITextView : IPropertyOwner {
		/// <summary>
		/// Closes the text view
		/// </summary>
		void Close();

		/// <summary>
		/// true if it's been closed
		/// </summary>
		bool IsClosed { get; }

		/// <summary>
		/// Raised when the text view has been closed
		/// </summary>
		event EventHandler Closed;

		/// <summary>
		/// Raised when it or any of its adornments got the keyboard focus
		/// </summary>
		event EventHandler GotAggregateFocus;

		/// <summary>
		/// Raised when it and all its adornments lost the keyboard focus
		/// </summary>
		event EventHandler LostAggregateFocus;

		/// <summary>
		/// Raised when layout has changed
		/// </summary>
		event EventHandler<TextViewLayoutChangedEventArgs> LayoutChanged;

		/// <summary>
		/// Raised when <see cref="ViewportHeight"/> has changed
		/// </summary>
		event EventHandler ViewportHeightChanged;

		/// <summary>
		/// Raised when <see cref="ViewportLeft"/> has changed
		/// </summary>
		event EventHandler ViewportLeftChanged;

		/// <summary>
		/// Raised when <see cref="ViewportWidth"/> has changed
		/// </summary>
		event EventHandler ViewportWidthChanged;

		/// <summary>
		/// Raised when the mouse has hovered over a character
		/// </summary>
		event EventHandler<MouseHoverEventArgs> MouseHover;

		/// <summary>
		/// true if it or any of its adornments has focus
		/// </summary>
		bool HasAggregateFocus { get; }

		/// <summary>
		/// true if the mouse is over it or any of its adornments
		/// </summary>
		bool IsMouseOverViewOrAdornments { get; }

		/// <summary>
		/// true if it's being laid out
		/// </summary>
		bool InLayout { get; }

		/// <summary>
		/// View port top
		/// </summary>
		double ViewportTop { get; }

		/// <summary>
		/// View port bottom
		/// </summary>
		double ViewportBottom { get; }

		/// <summary>
		/// View port left
		/// </summary>
		double ViewportLeft { get; set; }

		/// <summary>
		/// View port right
		/// </summary>
		double ViewportRight { get; }

		/// <summary>
		/// View port width (includes margin)
		/// </summary>
		double ViewportWidth { get; }

		/// <summary>
		/// View port height (includes margin)
		/// </summary>
		double ViewportHeight { get; }

		/// <summary>
		/// Gets the nominal height of a line of text in the view
		/// </summary>
		double LineHeight { get; }

		/// <summary>
		/// Gets the right coordinate of the longest line, whether or not that line is currently visible, in logical pixels
		/// </summary>
		double MaxTextRightCoordinate { get; }

		/// <summary>
		/// Provisional text highlight span or null if none
		/// </summary>
		ITrackingSpan ProvisionalTextHighlight { get; set; }

		/// <summary>
		/// Gets the caret
		/// </summary>
		ITextCaret Caret { get; }

		/// <summary>
		/// Gets the selection
		/// </summary>
		ITextSelection Selection { get; }

		/// <summary>
		/// Text buffer shown in this text view
		/// </summary>
		ITextBuffer TextBuffer { get; }

		/// <summary>
		/// <see cref="ITextSnapshot"/> of <see cref="TextBuffer"/>
		/// </summary>
		ITextSnapshot TextSnapshot { get; }

		/// <summary>
		/// <see cref="ITextSnapshot"/> of the visual buffer
		/// </summary>
		ITextSnapshot VisualSnapshot { get; }

		/// <summary>
		/// Gets the text data model
		/// </summary>
		ITextDataModel TextDataModel { get; }

		/// <summary>
		/// Gets the text view model
		/// </summary>
		ITextViewModel TextViewModel { get; }

		/// <summary>
		/// Gets the roles
		/// </summary>
		ITextViewRoleSet Roles { get; }

		/// <summary>
		/// Gets the options
		/// </summary>
		IEditorOptions Options { get; }

		/// <summary>
		/// Gets the command target
		/// </summary>
		ICommandTargetCollection CommandTarget { get; }

		/// <summary>
		/// Gets the editor operations
		/// </summary>
		IEditorOperations2 EditorOperations { get; }

		/// <summary>
		/// Gets the text view lines that have been rendered, some of them could be hidden.
		/// </summary>
		ITextViewLineCollection TextViewLines { get; }

		/// <summary>
		/// Gets the view scroller
		/// </summary>
		IViewScroller ViewScroller { get; }

		/// <summary>
		/// Formats and displays the contents of the text buffer so that the <see cref="ITextViewLine"/> containing the buffer position is displayed at the desired position
		/// </summary>
		/// <param name="bufferPosition">Buffer position</param>
		/// <param name="verticalDistance">The distance (in pixels) between the ITextViewLine and the edge of the view</param>
		/// <param name="relativeTo">Relative to top or bottom</param>
		void DisplayTextLineContainingBufferPosition(SnapshotPoint bufferPosition, double verticalDistance, ViewRelativePosition relativeTo);

		/// <summary>
		/// Formats and displays the contents of the text buffer so that the <see cref="ITextViewLine"/> containing the specified buffer position is displayed at the desired position
		/// </summary>
		/// <param name="bufferPosition">Buffer position</param>
		/// <param name="verticalDistance">The distance (in pixels) between the ITextViewLine and the edge of the view</param>
		/// <param name="relativeTo">Relative to top or bottom</param>
		/// <param name="viewportWidthOverride">If specified, the text is formatted as if the viewport had the specified width</param>
		/// <param name="viewportHeightOverride">If specified, the text is formatted as if the viewport had the specified height</param>
		void DisplayTextLineContainingBufferPosition(SnapshotPoint bufferPosition, double verticalDistance, ViewRelativePosition relativeTo, double? viewportWidthOverride, double? viewportHeightOverride);

		/// <summary>
		/// Gets the <see cref="SnapshotSpan"/> of text that constitutes a text element (a single visual representation) at the given <see cref="SnapshotPoint"/>
		/// </summary>
		/// <param name="point">Position in the text snapshot</param>
		/// <returns></returns>
		SnapshotSpan GetTextElementSpan(SnapshotPoint point);

		/// <summary>
		/// Gets the <see cref="IWpfTextViewLine"/> that contains the specified text buffer position
		/// </summary>
		/// <param name="bufferPosition">Buffer position</param>
		/// <returns></returns>
		ITextViewLine GetTextViewLineContainingBufferPosition(SnapshotPoint bufferPosition);
	}
}
