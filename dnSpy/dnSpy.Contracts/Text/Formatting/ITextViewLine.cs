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

using System.Collections.ObjectModel;
using dnSpy.Contracts.Text.Editor;

namespace dnSpy.Contracts.Text.Formatting {
	/// <summary>
	/// <see cref="ITextView"/> line
	/// </summary>
	public interface ITextViewLine {
		/// <summary>
		/// Gets the position of the bottom edge of this line in the text rendering coordinate system.
		/// </summary>
		double Bottom { get; }

		/// <summary>
		/// Gets the distance between the top and bottom edge of this line.
		/// </summary>
		double Height { get; }

		/// <summary>
		/// Gets the position of the left edge of this line in the text rendering coordinate system.
		/// </summary>
		double Left { get; }

		/// <summary>
		/// Gets the position of the right edge of this line in the text rendering coordinate system.
		/// </summary>
		double Right { get; }

		/// <summary>
		/// Gets the position of the top edge of this line in the text rendering coordinate system.
		/// </summary>
		double Top { get; }

		/// <summary>
		/// Gets the distance between the left and right edges of this line.
		/// </summary>
		double Width { get; }

		/// <summary>
		/// Gets the y-coordinate of the bottom of the text in the rendered line.
		/// </summary>
		double TextBottom { get; }

		/// <summary>
		/// Gets the y-coordinate of the top of the text in the rendered line.
		/// </summary>
		double TextTop { get; }

		/// <summary>
		/// Gets the vertical distance between the top and bottom of the text in the rendered line.
		/// </summary>
		double TextHeight { get; }

		/// <summary>
		/// Gets the horizontal distance between <see cref="TextRight"/> and <see cref="TextLeft"/>.
		/// </summary>
		double TextWidth { get; }

		/// <summary>
		/// Gets the x-coordinate of the left edge of the text in the rendered line.
		/// </summary>
		double TextLeft { get; }

		/// <summary>
		/// Gets the x-coordinate of the right edge of the text in the rendered line.
		/// </summary>
		double TextRight { get; }

		/// <summary>
		/// Get the width of the virtual spaces at the end of this line.
		/// </summary>
		double VirtualSpaceWidth { get; }

		/// <summary>
		/// Gets the distance from the top of the text to the baseline text on the line.
		/// </summary>
		double Baseline { get; }

		/// <summary>
		/// Gets the change in the top of this rendered text line between the value of <see cref="Top"/> in the current layout and the value of <see cref="Top"/> in the previous layout.
		/// </summary>
		double DeltaY { get; }

		/// <summary>
		/// Gets the distance from the right edge of the last character in this line to the end of the space of this line.
		/// </summary>
		double EndOfLineWidth { get; }

		/// <summary>
		/// Gets a tag that can be used to track the identity of an <see cref="ITextViewLine"/> across layouts in the view.
		/// </summary>
		object IdentityTag { get; }

		/// <summary>
		/// Determines whether this <see cref="ITextViewLine"/> is the first line in the list of lines formatted for a particular <see cref="ITextSnapshotLine"/>.
		/// </summary>
		bool IsFirstTextViewLineForSnapshotLine { get; }

		/// <summary>
		/// Determines whether this <see cref="ITextViewLine"/> is the last line in the list of lines formatted for a particular <see cref="ITextSnapshotLine"/>.
		/// </summary>
		bool IsLastTextViewLineForSnapshotLine { get; }

		/// <summary>
		/// Determines whether this text view line is still valid.
		/// </summary>
		bool IsValid { get; }

		/// <summary>
		/// Gets the length of the line, excluding any line break characters.
		/// </summary>
		int Length { get; }

		/// <summary>
		/// Gets the length of the line, including any line break characters.
		/// </summary>
		int LengthIncludingLineBreak { get; }

		/// <summary>
		/// Gets the length of the line break sequence (for example, "\r\n") that appears at the end of this line.
		/// </summary>
		int LineBreakLength { get; }

		/// <summary>
		/// Gets the <see cref="Formatting.LineTransform"/> used to render this line.
		/// </summary>
		LineTransform LineTransform { get; }

		/// <summary>
		/// Gets the <see cref="ITextSnapshot"/> on which this map is based.
		/// </summary>
		ITextSnapshot Snapshot { get; }

		/// <summary>
		/// Gets the visibility state of this rendered text line with respect to the top and bottom of the view.
		/// </summary>
		VisibilityState VisibilityState { get; }

		/// <summary>
		/// Gets the position in <see cref="Snapshot"/> of the first character in the line.
		/// </summary>
		SnapshotPoint Start { get; }

		/// <summary>
		/// Gets the position of the first character past the end of the line, excluding any line break characters.
		/// </summary>
		SnapshotPoint End { get; }

		/// <summary>
		/// Gets the position of the first character past the end of the line, including any line break characters.
		/// </summary>
		SnapshotPoint EndIncludingLineBreak { get; }

		/// <summary>
		/// Gets the extent of the line, excluding any line break characters.
		/// </summary>
		SnapshotSpan Extent { get; }

		/// <summary>
		/// Gets the extent of the line, including any line break characters.
		/// </summary>
		SnapshotSpan ExtentIncludingLineBreak { get; }

		/// <summary>
		/// Gets the <see cref="IMappingSpan"/> that corresponds to the <see cref="Extent"/> of the line.
		/// </summary>
		IMappingSpan ExtentAsMappingSpan { get; }

		/// <summary>
		/// Gets the <see cref="IMappingSpan"/> that corresponds to <see cref="ExtentIncludingLineBreak"/>.
		/// </summary>s
		IMappingSpan ExtentIncludingLineBreakAsMappingSpan { get; }

		/// <summary>
		/// Gets the default <see cref="LineTransform"/> used to render this line.
		/// </summary>
		LineTransform DefaultLineTransform { get; }

		/// <summary>
		/// Gets the change to this rendered textline between the current layout and the previous layout.
		/// </summary>
		TextViewLineChange Change { get; }

		/// <summary>
		/// Determines whether the specified buffer position lies within this text line.
		/// </summary>
		/// <param name="bufferPosition">The buffer position</param>
		/// <returns></returns>
		bool ContainsBufferPosition(SnapshotPoint bufferPosition);

		/// <summary>
		/// Calculates the bounds of the specified adornment.
		/// </summary>
		/// <param name="identityTag">The IAdornmentElement.IdentityTag of the adornment whose bounds should be calculated</param>
		/// <returns></returns>
		TextBounds? GetAdornmentBounds(object identityTag);

		/// <summary>
		/// Gets the adornments positioned on the line.
		/// </summary>
		/// <param name="providerTag">The identity tag of the provider.</param>
		/// <returns></returns>
		ReadOnlyCollection<object> GetAdornmentTags(object providerTag);

		/// <summary>
		/// Gets the buffer position of the character whose character bounds contains the given x-coordinate.
		/// </summary>
		/// <param name="xCoordinate">The x-coordinate of the desired character</param>
		/// <returns></returns>
		SnapshotPoint? GetBufferPositionFromXCoordinate(double xCoordinate);

		/// <summary>
		/// Gets the buffer position of the character whose character bounds contains the given x-coordinate.
		/// </summary>
		/// <param name="xCoordinate">The x-coordinate of the desired character</param>
		/// <param name="textOnly">If true, then this method will return null if xCoordinate is over an adornment.</param>
		/// <returns></returns>
		SnapshotPoint? GetBufferPositionFromXCoordinate(double xCoordinate, bool textOnly);

		/// <summary>
		/// Calculates the bounds of the character at the specified buffer position.
		/// </summary>
		/// <param name="bufferPosition">The text buffer-based index of the character</param>
		/// <returns></returns>
		TextBounds GetCharacterBounds(SnapshotPoint bufferPosition);

		/// <summary>
		/// Calculates the bounds of the character at the specified buffer position.
		/// </summary>
		/// <param name="bufferPosition">The text buffer-based index of the character</param>
		/// <returns></returns>
		TextBounds GetCharacterBounds(VirtualSnapshotPoint bufferPosition);

		/// <summary>
		/// Calculates the bounds of the character at the specified buffer position, including any adjacent space-negotiating adornments.
		/// </summary>
		/// <param name="bufferPosition">The text buffer-based index of the character</param>
		/// <returns></returns>
		TextBounds GetExtendedCharacterBounds(SnapshotPoint bufferPosition);

		/// <summary>
		/// Calculates the bounds of the character at the specified virtual buffer position, including any adjacent space-negotiating adornments.
		/// </summary>
		/// <param name="bufferPosition">The text buffer-based index of the character</param>
		/// <returns></returns>
		TextBounds GetExtendedCharacterBounds(VirtualSnapshotPoint bufferPosition);

		/// <summary>
		/// Gets the buffer position used if new data were to be inserted at the given x-coordinate.
		/// </summary>
		/// <param name="xCoordinate">The x-coordinate of the desired point</param>
		/// <returns></returns>
		VirtualSnapshotPoint GetInsertionBufferPositionFromXCoordinate(double xCoordinate);

		/// <summary>
		/// Gets a collection of <see cref="TextBounds"/> structures for the text that corresponds to the given span.
		/// </summary>
		/// <param name="bufferSpan">The <see cref="SnapshotSpan"/> representing the text for which to compute the text bounds</param>
		/// <returns></returns>
		Collection<TextBounds> GetNormalizedTextBounds(SnapshotSpan bufferSpan);

		/// <summary>
		/// Gets the span whose text element index corresponds to the given buffer position.
		/// </summary>
		/// <param name="bufferPosition">The buffer position</param>
		/// <returns></returns>
		SnapshotSpan GetTextElementSpan(SnapshotPoint bufferPosition);

		/// <summary>
		/// Gets the buffer position of the character whose character bounds contains the given x-coordinate.
		/// </summary>
		/// <param name="xCoordinate">The x-coordinate of the desired character</param>
		/// <returns></returns>
		VirtualSnapshotPoint GetVirtualBufferPositionFromXCoordinate(double xCoordinate);

		/// <summary>
		/// Determines whether a <paramref name="bufferSpan"/> intersects this text line.
		/// </summary>
		/// <param name="bufferSpan">The buffer span</param>
		/// <returns></returns>
		bool IntersectsBufferSpan(SnapshotSpan bufferSpan);
	}
}
