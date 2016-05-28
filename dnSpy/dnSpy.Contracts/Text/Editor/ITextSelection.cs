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
using System.Collections.ObjectModel;
using dnSpy.Contracts.Text.Formatting;

namespace dnSpy.Contracts.Text.Editor {
	/// <summary>
	/// Text selection
	/// </summary>
	public interface ITextSelection {
		/// <summary>
		/// Gets the <see cref="ITextView"/> owner
		/// </summary>
		ITextView TextView { get; }

		/// <summary>
		/// Text selection mode
		/// </summary>
		TextSelectionMode Mode { get; set; }

		/// <summary>
		/// Determines whether <see cref="IsActive"/> should track when the <see cref="ITextView"/> gains and loses aggregate focus
		/// </summary>
		bool ActivationTracksFocus { get; set; }

		/// <summary>
		/// Gets/sets whether or not this selection is active. Gets automatically updated if
		/// <see cref="ActivationTracksFocus"/> is true
		/// </summary>
		bool IsActive { get; set; }

		/// <summary>
		/// true if the selection is empty
		/// </summary>
		bool IsEmpty { get; }

		/// <summary>
		/// true if the anchor point is <see cref="Span.End"/>, false if it's <see cref="Span.Start"/>
		/// </summary>
		bool IsReversed { get; }

		/// <summary>
		/// The selected spans. This collection is never empty, but the spans in the collection may be empty.
		/// </summary>
		NormalizedSnapshotSpanCollection SelectedSpans { get; }

		/// <summary>
		/// Gets the anchor point, this is normally the start of the selection unless <see cref="IsReversed"/> is true
		/// </summary>
		VirtualSnapshotPoint AnchorPoint { get; }

		/// <summary>
		/// Gets the active point, this is normally the end of the selection unless <see cref="IsReversed"/> is true
		/// </summary>
		VirtualSnapshotPoint ActivePoint { get; }

		/// <summary>
		/// This is the start of the selection, and is either <see cref="AnchorPoint"/> or <see cref="ActivePoint"/>
		/// </summary>
		VirtualSnapshotPoint Start { get; }

		/// <summary>
		/// This is the end of the selection, and is either <see cref="AnchorPoint"/> or <see cref="ActivePoint"/>
		/// </summary>
		VirtualSnapshotPoint End { get; }

		/// <summary>
		/// Gets the current selection as a stream selection even if the current selection mode is box selection
		/// </summary>
		VirtualSnapshotSpan StreamSelectionSpan { get; }

		/// <summary>
		/// Current selection. This collection is never empty, but the spans in the collection may be empty.
		/// </summary>
		ReadOnlyCollection<VirtualSnapshotSpan> VirtualSelectedSpans { get; }

		/// <summary>
		/// Raised when the selection has changed
		/// </summary>
		event EventHandler SelectionChanged;

		/// <summary>
		/// Clears the selection
		/// </summary>
		void Clear();

		/// <summary>
		/// Gets the selection of <paramref name="line"/>
		/// </summary>
		/// <param name="line">Line</param>
		/// <returns></returns>
		VirtualSnapshotSpan? GetSelectionOnTextViewLine(ITextViewLine line);

		/// <summary>
		/// Selects text
		/// </summary>
		/// <param name="selectionSpan">Span</param>
		/// <param name="isReversed">true if it's reversed, see <see cref="IsReversed"/></param>
		void Select(SnapshotSpan selectionSpan, bool isReversed);

		/// <summary>
		/// Selects text
		/// </summary>
		/// <param name="anchorPoint">Anchor point</param>
		/// <param name="activePoint">Active point</param>
		void Select(VirtualSnapshotPoint anchorPoint, VirtualSnapshotPoint activePoint);

		/// <summary>
		/// Selects text
		/// </summary>
		/// <param name="startLine">Start line, 0-based</param>
		/// <param name="startColumn">Start column, 0-based</param>
		/// <param name="endLine">End line, 0-based</param>
		/// <param name="endColumn">End column, 0-based</param>
		void Select(int startLine, int startColumn, int endLine, int endColumn);
	}
}
