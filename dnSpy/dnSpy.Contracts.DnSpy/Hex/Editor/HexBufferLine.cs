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
using Microsoft.VisualStudio.Text;

namespace dnSpy.Contracts.Hex.Editor {
	/// <summary>
	/// Line information
	/// </summary>
	public abstract class HexBufferLine {
		/// <summary>
		/// Line span, some of the bytes could be hidden
		/// </summary>
		public abstract HexBufferSpan LineSpan { get; }

		/// <summary>
		/// The visible bytes shown in the UI
		/// </summary>
		public abstract HexBufferSpan VisibleBytesSpan { get; }

		/// <summary>
		/// All raw visible bytes
		/// </summary>
		public abstract HexBytes VisibleHexBytes { get; }

		/// <summary>
		/// Text shown in the UI. The positions of the offset column, values column
		/// and ASCII column are not fixed, use one of the GetXXX methods to get
		/// the spans.
		/// </summary>
		public abstract string Text { get; }

		/// <summary>
		/// true if the offset column is present
		/// </summary>
		public abstract bool IsOffsetColumnPresent { get; }

		/// <summary>
		/// true if the values column is present
		/// </summary>
		public abstract bool IsValuesColumnPresent { get; }

		/// <summary>
		/// true if the ASCII column is present
		/// </summary>
		public abstract bool IsAsciiColumnPresent { get; }

		/// <summary>
		/// Gets the value of the offset shown in <see cref="Text"/>. The real offset
		/// is stored in <see cref="LineSpan"/>
		/// </summary>
		public abstract HexPosition LogicalOffset { get; }

		/// <summary>
		/// Gets the span of the offset in <see cref="Text"/>. This can be an empty span if
		/// the offset column isn't shown.
		/// </summary>
		/// <returns></returns>
		public abstract Span GetOffsetSpan();

		/// <summary>
		/// Gets the span of the values column
		/// </summary>
		/// <param name="onlyVisibleCells">true to only include visible values, false to include the full column</param>
		/// <returns></returns>
		public abstract Span GetValuesSpan(bool onlyVisibleCells);

		/// <summary>
		/// Gets the span of values in <see cref="Text"/>. This can be an empty span
		/// if the values column isn't shown.
		/// </summary>
		/// <param name="span">Span</param>
		/// <param name="flags">Flags</param>
		/// <returns></returns>
		public virtual TextAndHexSpan GetValuesSpan(HexBufferSpan span, HexCellSpanFlags flags) =>
			GetTextAndHexSpan(ValueCells, span, flags);

		/// <summary>
		/// Gets the span of the ASCII column. This can be an empty span
		/// if the ASCII column isn't shown.
		/// </summary>
		/// <param name="onlyVisibleCells">true to only include visible characters, false to include the full column</param>
		/// <returns></returns>
		public abstract Span GetAsciiSpan(bool onlyVisibleCells);

		/// <summary>
		/// Gets the span of ASCII characters in <see cref="Text"/>. This can be an empty span
		/// if the ASCII column isn't shown.
		/// </summary>
		/// <param name="span">Span</param>
		/// <param name="flags">Flags</param>
		/// <returns></returns>
		public virtual TextAndHexSpan GetAsciiSpan(HexBufferSpan span, HexCellSpanFlags flags) =>
			GetTextAndHexSpan(AsciiCells, span, flags);

		/// <summary>
		/// Gets the value cell collection
		/// </summary>
		public abstract HexCellInformationCollection ValueCells { get; }

		/// <summary>
		/// Gets the ASCII cell collection
		/// </summary>
		public abstract HexCellInformationCollection AsciiCells { get; }

		TextAndHexSpan GetTextAndHexSpan(HexCellInformationCollection collection, HexBufferSpan span, HexCellSpanFlags flags) {
			if (span.IsDefault)
				throw new ArgumentException();

			int textStart = int.MaxValue;
			int textEnd = int.MinValue;
			var posStart = HexPosition.MaxValue;
			var posEnd = HexPosition.MinValue;
			foreach (var cell in collection.GetCells(span)) {
				if (!cell.HasData)
					continue;
				var cellSpan = cell.GetSpan(flags);
				textStart = Math.Min(textStart, cellSpan.Start);
				textEnd = Math.Max(textEnd, cellSpan.End);

				posStart = HexPosition.Min(posStart, cell.HexSpan.Start);
				posEnd = HexPosition.Max(posEnd, cell.HexSpan.End);
			}

			if (textStart > textEnd)
				textStart = textEnd = 0;
			if (posStart > posEnd)
				posStart = posEnd = HexPosition.Zero;
			return new TextAndHexSpan(Span.FromBounds(textStart, textEnd), new HexBufferSpan(span.Buffer, HexSpan.FromBounds(posStart, posEnd)));
		}

		/// <summary>
		/// Returns <see cref="Text"/>
		/// </summary>
		/// <returns></returns>
		public override string ToString() => Text;
	}

	/// <summary>
	/// Flags passed to <see cref="HexBufferLine.GetValuesSpan(HexBufferSpan, HexCellSpanFlags)"/>
	/// and <see cref="HexBufferLine.GetAsciiSpan(HexBufferSpan, HexCellSpanFlags)"/>
	/// </summary>
	[Flags]
	public enum HexCellSpanFlags {
		/// <summary>
		/// No bit is set
		/// </summary>
		None					= 0,

		/// <summary>
		/// Include cell whitespace, if any
		/// </summary>
		Cell					= 0x00000001,

		/// <summary>
		/// Include cell separator, if any
		/// </summary>
		Separator				= 0x00000002,
	}
}
