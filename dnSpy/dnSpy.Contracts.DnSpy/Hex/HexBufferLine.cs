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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.VisualStudio.Text;

namespace dnSpy.Contracts.Hex {
	/// <summary>
	/// Line information
	/// </summary>
	public abstract class HexBufferLine {
		/// <summary>
		/// Constructor
		/// </summary>
		protected HexBufferLine() { }

		/// <summary>
		/// Gets the <see cref="HexBufferLineProvider"/> instance that created this line
		/// </summary>
		public abstract HexBufferLineProvider LineProvider { get; }

		/// <summary>
		/// Gets the buffer
		/// </summary>
		public HexBuffer Buffer => LineProvider.Buffer;

		/// <summary>
		/// Gets the line number
		/// </summary>
		public abstract HexPosition LineNumber { get; }

		/// <summary>
		/// Gets the column order
		/// </summary>
		public abstract ReadOnlyCollection<HexColumnType> ColumnOrder { get; }

		/// <summary>
		/// Line span
		/// </summary>
		public abstract HexBufferSpan BufferSpan { get; }

		/// <summary>
		/// Gets the start position
		/// </summary>
		public HexBufferPoint BufferStart => BufferSpan.Start;

		/// <summary>
		/// Gets the end position
		/// </summary>
		public HexBufferPoint BufferEnd => BufferSpan.End;

		/// <summary>
		/// All raw bytes
		/// </summary>
		public abstract HexBytes HexBytes { get; }

		/// <summary>
		/// Text shown in the UI. The positions of the offset column, values column
		/// and ASCII column are not fixed, use one of the GetXXX methods to get
		/// the spans.
		/// </summary>
		public abstract string Text { get; }

		/// <summary>
		/// Gets a span covering <see cref="Text"/>
		/// </summary>
		public Span TextSpan => new Span(0, Text.Length);

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
		/// Returns true if a column is present
		/// </summary>
		/// <param name="column">Column</param>
		/// <returns></returns>
		public bool IsColumnPresent(HexColumnType column) {
			switch (column) {
			case HexColumnType.Offset:	return IsOffsetColumnPresent;
			case HexColumnType.Values:	return IsValuesColumnPresent;
			case HexColumnType.Ascii:	return IsAsciiColumnPresent;
			default: throw new ArgumentOutOfRangeException(nameof(column));
			}
		}

		/// <summary>
		/// Gets the value of the offset shown in <see cref="Text"/>. The real offset
		/// is stored in <see cref="BufferSpan"/>
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
		/// Gets values spans
		/// </summary>
		/// <param name="span">Buffer span</param>
		/// <param name="flags">Flags</param>
		/// <returns></returns>
		public IEnumerable<TextAndHexSpan> GetValuesSpans(HexBufferSpan span, HexSpanSelectionFlags flags) =>
			GetTextAndHexSpans(IsValuesColumnPresent, ValueCells, span, flags, GetValuesSpan(onlyVisibleCells: true), GetValuesSpan(onlyVisibleCells: false));

		/// <summary>
		/// Gets the span of the ASCII column. This can be an empty span
		/// if the ASCII column isn't shown.
		/// </summary>
		/// <param name="onlyVisibleCells">true to only include visible characters, false to include the full column</param>
		/// <returns></returns>
		public abstract Span GetAsciiSpan(bool onlyVisibleCells);

		/// <summary>
		/// Gets ASCII spans
		/// </summary>
		/// <param name="span">Buffer span</param>
		/// <param name="flags">Flags</param>
		/// <returns></returns>
		public IEnumerable<TextAndHexSpan> GetAsciiSpans(HexBufferSpan span, HexSpanSelectionFlags flags) =>
			GetTextAndHexSpans(IsAsciiColumnPresent, AsciiCells, span, flags, GetAsciiSpan(onlyVisibleCells: true), GetAsciiSpan(onlyVisibleCells: false));

		/// <summary>
		/// Gets column spans in column order
		/// </summary>
		/// <param name="span">Buffer span</param>
		/// <param name="flags">Flags</param>
		/// <returns></returns>
		public IEnumerable<TextAndHexSpan> GetSpans(HexBufferSpan span, HexSpanSelectionFlags flags) {
			if (span.IsDefault)
				throw new ArgumentException();
			if (span.Buffer != Buffer)
				throw new ArgumentException();

			var overlapSpan = BufferSpan.Overlap(span);
			if (overlapSpan == null)
				yield break;

			foreach (var column in ColumnOrder) {
				switch (column) {
				case HexColumnType.Offset:
					if ((flags & HexSpanSelectionFlags.Offset) != 0 && IsOffsetColumnPresent) {
						if (BufferSpan.Contains(overlapSpan.Value))
							yield return new TextAndHexSpan(GetOffsetSpan(), BufferSpan);
					}
					break;

				case HexColumnType.Values:
					if ((flags & HexSpanSelectionFlags.Values) != 0) {
						foreach (var info in GetValuesSpans(overlapSpan.Value, flags))
							yield return info;
					}
					break;

				case HexColumnType.Ascii:
					if ((flags & HexSpanSelectionFlags.Ascii) != 0) {
						foreach (var info in GetAsciiSpans(overlapSpan.Value, flags))
							yield return info;
					}
					break;

				default:
					throw new InvalidOperationException();
				}
			}
		}

		/// <summary>
		/// Gets the value cell collection
		/// </summary>
		public abstract HexCellInformationCollection ValueCells { get; }

		/// <summary>
		/// Gets the ASCII cell collection
		/// </summary>
		public abstract HexCellInformationCollection AsciiCells { get; }

		TextAndHexSpan Create(HexCellInformationCollection collection, HexCellInformation first, HexCellInformation last, HexBufferSpan bufferSpan) {
			var firstCellSpan = first.FullSpan;
			var lastCellSpan = last.FullSpan;
			var startPos = HexPosition.MaxEndPosition;
			var endPos = HexPosition.Zero;
			for (int i = first.Index; i <= last.Index; i++) {
				var cell = collection[i];
				if (!cell.HasData)
					continue;
				startPos = HexPosition.Min(startPos, cell.BufferStart);
				endPos = HexPosition.Max(endPos, cell.BufferEnd);
			}
			var resultBufferSpan = startPos <= endPos ?
				new HexBufferSpan(new HexBufferPoint(Buffer, startPos), new HexBufferPoint(Buffer, endPos)) :
				bufferSpan;
			return new TextAndHexSpan(Span.FromBounds(firstCellSpan.Start, lastCellSpan.End), resultBufferSpan);
		}

		IEnumerable<TextAndHexSpan> GetTextAndHexSpans(bool isColumnPresent, HexCellInformationCollection collection, HexBufferSpan span, HexSpanSelectionFlags flags, Span visibleSpan, Span fullSpan) {
			if (span.IsDefault)
				throw new ArgumentException();
			if (span.Buffer != Buffer)
				throw new ArgumentException();

			if (!isColumnPresent)
				yield break;

			var overlapSpan = BufferSpan.Overlap(span);
			if (overlapSpan == null)
				yield break;

			if ((flags & (HexSpanSelectionFlags.Group0 | HexSpanSelectionFlags.Group1)) != 0) {
				bool group0 = (flags & HexSpanSelectionFlags.Group0) != 0;
				bool group1 = (flags & HexSpanSelectionFlags.Group1) != 0;

				IEnumerable<HexCellInformation> cells;
				if ((flags & HexSpanSelectionFlags.AllCells) != 0) {
					cells = collection.GetCells();
					overlapSpan = BufferSpan;
				}
				else if ((flags & HexSpanSelectionFlags.AllVisibleCells) != 0) {
					cells = collection.GetVisibleCells();
					overlapSpan = BufferSpan;
				}
				else
					cells = collection.GetCells(overlapSpan.Value);
				HexCellInformation firstCell = null;
				HexCellInformation lastCell = null;
				foreach (var cell in cells) {
					if (!((cell.GroupIndex == 0 && group0) || (cell.GroupIndex == 1 && group1)))
						continue;
					if (firstCell == null) {
						firstCell = cell;
						lastCell = cell;
					}
					else if (lastCell.Index + 1 == cell.Index && lastCell.GroupIndex == cell.GroupIndex)
						lastCell = cell;
					else {
						yield return Create(collection, firstCell, lastCell, overlapSpan.Value);
						firstCell = lastCell = cell;
					}
				}
				if (firstCell != null)
					yield return Create(collection, firstCell, lastCell, overlapSpan.Value);
				yield break;
			}
			if ((flags & HexSpanSelectionFlags.AllVisibleCells) != 0) {
				yield return new TextAndHexSpan(visibleSpan, BufferSpan);
				yield break;
			}
			if ((flags & HexSpanSelectionFlags.AllCells) != 0) {
				yield return new TextAndHexSpan(fullSpan, BufferSpan);
				yield break;
			}

			if ((flags & HexSpanSelectionFlags.OneValue) != 0) {
				foreach (var cell in collection.GetCells(overlapSpan.Value)) {
					if (!cell.HasData)
						continue;
					var cellSpan = cell.GetSpan(flags);
					yield return new TextAndHexSpan(cellSpan, new HexBufferSpan(Buffer, cell.BufferSpan));
				}
			}
			else {
				int textStart = int.MaxValue;
				int textEnd = int.MinValue;
				var posStart = HexPosition.MaxValue;
				var posEnd = HexPosition.MinValue;
				foreach (var cell in collection.GetCells(overlapSpan.Value)) {
					if (!cell.HasData)
						continue;
					var cellSpan = cell.GetSpan(flags);
					textStart = Math.Min(textStart, cellSpan.Start);
					textEnd = Math.Max(textEnd, cellSpan.End);

					posStart = HexPosition.Min(posStart, cell.BufferStart);
					posEnd = HexPosition.Max(posEnd, cell.BufferEnd);
				}

				if (textStart > textEnd || posStart > posEnd)
					yield break;
				yield return new TextAndHexSpan(Span.FromBounds(textStart, textEnd), new HexBufferSpan(Buffer, HexSpan.FromBounds(posStart, posEnd)));
			}
		}

		/// <summary>
		/// Returns <see cref="Text"/>
		/// </summary>
		/// <returns></returns>
		public override string ToString() => Text;
	}
}
