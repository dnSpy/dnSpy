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
using System.Diagnostics;
using VST = Microsoft.VisualStudio.Text;

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
		/// Buffer span
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
		public VST.Span TextSpan => new VST.Span(0, Text.Length);

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
		/// Gets the span of a column
		/// </summary>
		/// <param name="column">Colum</param>
		/// <param name="onlyVisibleCells">true to only include visible values, false to include the full column</param>
		/// <returns></returns>
		public VST.Span GetSpan(HexColumnType column, bool onlyVisibleCells) {
			switch (column) {
			case HexColumnType.Offset:	return GetOffsetSpan();
			case HexColumnType.Values:	return GetValuesSpan(onlyVisibleCells);
			case HexColumnType.Ascii:	return GetAsciiSpan(onlyVisibleCells);
			default: throw new ArgumentOutOfRangeException(nameof(column));
			}
		}

		/// <summary>
		/// Gets the span of the offset in <see cref="Text"/>. This can be an empty span if
		/// the offset column isn't shown.
		/// </summary>
		/// <returns></returns>
		public abstract VST.Span GetOffsetSpan();

		/// <summary>
		/// Gets the span of the values column
		/// </summary>
		/// <param name="onlyVisibleCells">true to only include visible values, false to include the full column</param>
		/// <returns></returns>
		public abstract VST.Span GetValuesSpan(bool onlyVisibleCells);

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
		public abstract VST.Span GetAsciiSpan(bool onlyVisibleCells);

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
		public abstract HexCellCollection ValueCells { get; }

		/// <summary>
		/// Gets the ASCII cell collection
		/// </summary>
		public abstract HexCellCollection AsciiCells { get; }

		TextAndHexSpan Create(HexCellCollection collection, HexCell first, HexCell last, HexBufferSpan bufferSpan) {
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
			return new TextAndHexSpan(VST.Span.FromBounds(firstCellSpan.Start, lastCellSpan.End), resultBufferSpan);
		}

		IEnumerable<TextAndHexSpan> GetTextAndHexSpans(bool isColumnPresent, HexCellCollection collection, HexBufferSpan span, HexSpanSelectionFlags flags, VST.Span visibleSpan, VST.Span fullSpan) {
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

				IEnumerable<HexCell> cells;
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
				HexCell firstCell = null;
				HexCell lastCell = null;
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
				yield return new TextAndHexSpan(VST.Span.FromBounds(textStart, textEnd), new HexBufferSpan(Buffer, HexSpan.FromBounds(posStart, posEnd)));
			}
		}

		/// <summary>
		/// Gets a text line position or null
		/// </summary>
		/// <param name="position">Position</param>
		/// <returns></returns>
		public int? GetLinePosition(HexCellPosition position) {
			if (position.IsDefault)
				throw new ArgumentException();

			HexCellCollection collection;
			switch (position.Column) {
			case HexColumnType.Values:		collection = ValueCells; break;
			case HexColumnType.Ascii:		collection = AsciiCells; break;
			case HexColumnType.Offset:
			default:
				throw new ArgumentOutOfRangeException(nameof(position));
			}

			var cell = collection.GetCell(position.BufferPosition);
			if (cell == null)
				return null;
			if (position.CellPosition >= cell.CellSpan.Length)
				return null;
			return cell.CellSpan.Start + position.CellPosition;
		}

		/// <summary>
		/// Creates a <see cref="HexLinePositionInfo"/>
		/// </summary>
		/// <param name="linePosition">Line position</param>
		/// <returns></returns>
		public HexLinePositionInfo GetLinePositionInfo(int linePosition) {
			if (linePosition >= Text.Length)
				return HexLinePositionInfo.CreateVirtualSpace(linePosition, Text.Length);

			if (IsOffsetColumnPresent) {
				var span = GetOffsetSpan();
				if (span.Contains(linePosition))
					return HexLinePositionInfo.CreateOffset(linePosition, linePosition - span.Start);
			}

			if (IsValuesColumnPresent) {
				var valuesSpan = GetValuesSpan(onlyVisibleCells: false);
				if (valuesSpan.Contains(linePosition)) {
					int cellIndex = (linePosition - valuesSpan.Start) / LineProvider.GetCharsPerCellIncludingSeparator(HexColumnType.Values);
					var cell = AsciiCells[cellIndex];
					if (cell.SeparatorSpan.Contains(linePosition))
						return HexLinePositionInfo.CreateValueCellSeparator(linePosition, cell);
					return HexLinePositionInfo.CreateValue(linePosition, cell);
				}
			}

			if (IsAsciiColumnPresent) {
				var asciiSpan = GetAsciiSpan(onlyVisibleCells: false);
				if (asciiSpan.Contains(linePosition)) {
					int cellIndex = linePosition - asciiSpan.Start;
					var cell = AsciiCells[cellIndex];
					return HexLinePositionInfo.CreateAscii(linePosition, cell);
				}
			}

			foreach (var column in ColumnOrder) {
				var span = GetSpan(column, onlyVisibleCells: false);
				if (span.End == linePosition)
					return HexLinePositionInfo.CreateColumnSeparator(linePosition);
			}

			throw new InvalidOperationException();
		}

		/// <summary>
		/// Gets the closest cell position
		/// </summary>
		/// <param name="position">Position</param>
		/// <param name="onlyVisibleCells">true to only return cells with data</param>
		/// <returns></returns>
		public HexCellPosition? GetClosestCellPosition(HexLinePositionInfo position, bool onlyVisibleCells) {
			switch (position.Type) {
			case HexLinePositionInfoType.ValueCell:
			case HexLinePositionInfoType.AsciiCell:
				break;

			case HexLinePositionInfoType.Offset:
			case HexLinePositionInfoType.ValueCellSeparator:
			case HexLinePositionInfoType.ColumnSeparator:
			case HexLinePositionInfoType.VirtualSpace:
				var closestPos = GetClosestCellPosition(position.Position);
				if (closestPos == null)
					return null;
				Debug.Assert(closestPos.Value.IsAsciiCell || closestPos.Value.IsValueCell);
				position = closestPos.Value;
				break;

			default:
				throw new InvalidOperationException();
			}

			var cell = position.Cell;
			int cellPosition = position.CellPosition;
			switch (position.Type) {
			case HexLinePositionInfoType.AsciiCell:
				if (!IsAsciiColumnPresent)
					return null;
				if (onlyVisibleCells && !cell.HasData) {
					var visible = GetVisible(AsciiCells, cell);
					if (visible == null)
						return null;
					cell = visible.Value.Key;
					cellPosition = visible.Value.Value;
				}
				return new HexCellPosition(HexColumnType.Ascii, cell.BufferStart, cellPosition);

			case HexLinePositionInfoType.ValueCell:
				if (!IsValuesColumnPresent)
					return null;
				if (onlyVisibleCells && !cell.HasData) {
					var visible = GetVisible(ValueCells, cell);
					if (visible == null)
						return null;
					cell = visible.Value.Key;
					cellPosition = visible.Value.Value;
				}
				return new HexCellPosition(HexColumnType.Values, LineProvider.GetValueBufferSpan(cell, cellPosition).Start, cellPosition);

			case HexLinePositionInfoType.Offset:
			case HexLinePositionInfoType.ValueCellSeparator:
			case HexLinePositionInfoType.ColumnSeparator:
			case HexLinePositionInfoType.VirtualSpace:
			default:
				throw new InvalidOperationException();
			}
		}

		static KeyValuePair<HexCell, int>? GetVisible(HexCellCollection collection, HexCell cell) {
			if (cell.HasData)
				throw new ArgumentException();
			for (int i = cell.Index + 1; i < collection.Count; i++) {
				var c = collection[i];
				if (!c.HasData)
					continue;
				return new KeyValuePair<HexCell, int>(c, 0);
			}
			for (int i = cell.Index - 1; i >= 0; i--) {
				var c = collection[i];
				if (!c.HasData)
					continue;
				return new KeyValuePair<HexCell, int>(c, c.CellSpan.Length - 1);
			}
			return null;
		}

		HexLinePositionInfo? GetClosestCellPosition(int linePosition) {
			KeyValuePair<HexColumnType, HexCell>? closest = null;
			int cellPosition = -1;
			foreach (var info in GetCells()) {
				var cell = info.Value;
				if (closest == null || Compare(linePosition, cell, closest.Value.Value) < 0) {
					closest = info;
					cellPosition = linePosition - info.Value.CellSpan.Start;
					if (cellPosition < 0)
						cellPosition = 0;
					else if (cellPosition >= info.Value.CellSpan.Length)
						cellPosition = info.Value.CellSpan.Length - 1;
				}
			}
			if (closest == null)
				return null;
			if (cellPosition < 0 || cellPosition >= closest.Value.Value.CellSpan.Length)
				throw new InvalidOperationException();
			int pos = closest.Value.Value.CellSpan.Start + cellPosition;
			if (closest.Value.Key == HexColumnType.Values)
				return HexLinePositionInfo.CreateValue(pos, closest.Value.Value);
			if (closest.Value.Key == HexColumnType.Ascii)
				return HexLinePositionInfo.CreateAscii(pos, closest.Value.Value);
			throw new InvalidOperationException();
		}

		static int Compare(int linePosition, HexCell a, HexCell b) {
			int da = GetLength(linePosition, a);
			int db = GetLength(linePosition, a);
			return da - db;
		}

		static int GetLength(int linePosition, HexCell a) {
			int sl = Math.Abs(linePosition - a.FullSpan.Start);
			int el = Math.Abs(linePosition - (a.FullSpan.End - 1));
			return Math.Min(sl, el);
		}

		IEnumerable<KeyValuePair<HexColumnType, HexCell>> GetCells() {
			foreach (var column in ColumnOrder) {
				switch (column) {
				case HexColumnType.Offset:
					break;

				case HexColumnType.Values:
					if (IsValuesColumnPresent) {
						foreach (var cell in ValueCells.GetCells())
							yield return new KeyValuePair<HexColumnType, HexCell>(HexColumnType.Values, cell);
					}
					break;

				case HexColumnType.Ascii:
					if (IsAsciiColumnPresent) {
						foreach (var cell in AsciiCells.GetCells())
							yield return new KeyValuePair<HexColumnType, HexCell>(HexColumnType.Ascii, cell);
					}
					break;

				default:
					throw new InvalidOperationException();
				}
			}
		}

		/// <summary>
		/// Returns <see cref="Text"/>
		/// </summary>
		/// <returns></returns>
		public override string ToString() => Text;
	}
}
