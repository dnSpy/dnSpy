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
using dnSpy.Contracts.Hex.Tagging;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Formatting;

namespace dnSpy.Contracts.Hex.Formatting {
	/// <summary>
	/// Hex view line
	/// </summary>
	public abstract class HexViewLine {
		/// <summary>
		/// Constructor
		/// </summary>
		protected HexViewLine() { }

		/// <summary>
		/// Gets a tag that can be used to track the instance across layouts
		/// </summary>
		public abstract object IdentityTag { get; }

		/// <summary>
		/// true if this line is valid, false if it has been disposed
		/// </summary>
		public abstract bool IsValid { get; }

		/// <summary>
		/// Gets the base line
		/// </summary>
		public abstract double Baseline { get; }

		/// <summary>
		/// Gets the position of the top edge of this line
		/// </summary>
		public abstract double Top { get; }

		/// <summary>
		/// Gets the position of the bottom edge of this line
		/// </summary>
		public abstract double Bottom { get; }

		/// <summary>
		/// Gets the position of the left edge of this line
		/// </summary>
		public abstract double Left { get; }

		/// <summary>
		/// Gets the position of the right edge of this line
		/// </summary>
		public abstract double Right { get; }

		/// <summary>
		/// Gets the width of the line
		/// </summary>
		public abstract double Width { get; }

		/// <summary>
		/// Gets the height of the line
		/// </summary>
		public abstract double Height { get; }

		/// <summary>
		/// Gets the position of the top edge of the text
		/// </summary>
		public abstract double TextTop { get; }

		/// <summary>
		/// Gets the position of the bottom edge of the text
		/// </summary>
		public abstract double TextBottom { get; }

		/// <summary>
		/// Gets the position of the left edge of the text
		/// </summary>
		public abstract double TextLeft { get; }

		/// <summary>
		/// Gets the position of the right edge of the text
		/// </summary>
		public abstract double TextRight { get; }

		/// <summary>
		/// Gets the text width
		/// </summary>
		public abstract double TextWidth { get; }

		/// <summary>
		/// Gets the text height
		/// </summary>
		public abstract double TextHeight { get; }

		/// <summary>
		/// Get the width of the virtual spaces at the end of this line
		/// </summary>
		public abstract double VirtualSpaceWidth { get; }

		/// <summary>
		/// Gets the delta Y between current layout and previous layout
		/// </summary>
		public abstract double DeltaY { get; }

		/// <summary>
		/// Gets the width of the end of line character
		/// </summary>
		public abstract double EndOfLineWidth { get; }

		/// <summary>
		/// Gets the visibility
		/// </summary>
		public abstract VisibilityState VisibilityState { get; }

		/// <summary>
		/// Gets the change
		/// </summary>
		public abstract TextViewLineChange Change { get; }

		/// <summary>
		/// Gets the default line transform
		/// </summary>
		public abstract LineTransform DefaultLineTransform { get; }

		/// <summary>
		/// Gets the line transform
		/// </summary>
		public abstract LineTransform LineTransform { get; }

		/// <summary>
		/// Gets the buffer
		/// </summary>
		public HexBuffer Buffer => BufferLine.LineProvider.Buffer;

		/// <summary>
		/// Gets the buffer line
		/// </summary>
		public abstract HexBufferLine BufferLine { get; }

		/// <summary>
		/// Gets the text shown in this line
		/// </summary>
		public string Text => BufferLine.Text;

		/// <summary>
		/// Gets the text span
		/// </summary>
		public Span TextSpan => BufferLine.TextSpan;

		/// <summary>
		/// Returns true if <paramref name="bufferPosition"/> lies within this line
		/// </summary>
		/// <param name="bufferPosition">Position</param>
		/// <returns></returns>
		public abstract bool ContainsBufferPosition(HexBufferPoint bufferPosition);

		/// <summary>
		/// Gets the bounds of an adornment
		/// </summary>
		/// <param name="identityTag">Identity tag (<see cref="HexAdornmentElement.IdentityTag"/>)</param>
		/// <returns></returns>
		public abstract TextBounds? GetAdornmentBounds(object identityTag);

		/// <summary>
		/// Gets all adornment tags
		/// </summary>
		/// <param name="providerTag">Provider tag (<see cref="HexSpaceNegotiatingAdornmentTag.ProviderTag"/>)</param>
		/// <returns></returns>
		public abstract ReadOnlyCollection<object> GetAdornmentTags(object providerTag);

		/// <summary>
		/// Gets the line position
		/// </summary>
		/// <param name="xCoordinate">x coordinate</param>
		/// <returns></returns>
		public abstract int? GetLinePositionFromXCoordinate(double xCoordinate);

		/// <summary>
		/// Gets the line position
		/// </summary>
		/// <param name="xCoordinate">x coordinate</param>
		/// <param name="textOnly">true to return null if it's over an adornment</param>
		/// <returns></returns>
		public abstract int? GetLinePositionFromXCoordinate(double xCoordinate, bool textOnly);

		/// <summary>
		/// Gets character bounds
		/// </summary>
		/// <param name="linePosition">Position</param>
		/// <returns></returns>
		public abstract TextBounds GetCharacterBounds(int linePosition);

		/// <summary>
		/// Gets extended character bounds, including any adornments
		/// </summary>
		/// <param name="linePosition">Position</param>
		/// <returns></returns>
		public abstract TextBounds GetExtendedCharacterBounds(int linePosition);

		/// <summary>
		/// Gets the line position
		/// </summary>
		/// <param name="xCoordinate">x coordinate</param>
		/// <returns></returns>
		public abstract int GetVirtualLinePositionFromXCoordinate(double xCoordinate);

		/// <summary>
		/// Gets the insertion line position
		/// </summary>
		/// <param name="xCoordinate">x coordinate</param>
		/// <returns></returns>
		public abstract int GetInsertionLinePositionFromXCoordinate(double xCoordinate);

		/// <summary>
		/// Gets normalized text bounds
		/// </summary>
		/// <param name="lineSpan">Line span</param>
		/// <returns></returns>
		public abstract Collection<TextBounds> GetNormalizedTextBounds(Span lineSpan);

		/// <summary>
		/// Gets normalized text bounds
		/// </summary>
		/// <param name="bufferPosition">Position</param>
		/// <param name="flags">Flags</param>
		/// <returns></returns>
		public abstract Collection<TextBounds> GetNormalizedTextBounds(HexBufferSpan bufferPosition, TextBoundsFlags flags);

		/// <summary>
		/// Gets the span whose text element index corresponds to the given line position
		/// </summary>
		/// <param name="linePosition">Position</param>
		/// <returns></returns>
		public abstract Span GetTextElementSpan(int linePosition);

		/// <summary>
		/// Returns true if the line intersects with <paramref name="bufferSpan"/>
		/// </summary>
		/// <param name="bufferSpan">Span</param>
		/// <returns></returns>
		public abstract bool IntersectsBufferSpan(HexBufferSpan bufferSpan);
	}
}
