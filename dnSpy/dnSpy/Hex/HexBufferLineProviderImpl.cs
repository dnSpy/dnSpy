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
using System.Linq;
using System.Text;
using dnSpy.Contracts.Hex;
using VST = Microsoft.VisualStudio.Text;

namespace dnSpy.Hex {
	sealed class HexBufferLineProviderImpl : HexBufferLineProvider {
		const int DEFAULT_GROUP_SIZE_IN_BYTES = 8;

		static HexValueFormatter[] valueFormatters = new HexValueFormatter[HexBufferLineProviderOptions.HexValuesDisplayFormat_Last - HexBufferLineProviderOptions.HexValuesDisplayFormat_First + 1] {
			new HexByteValueFormatter(),
			new HexUInt16ValueFormatter(),
			new HexUInt32ValueFormatter(),
			new HexUInt64ValueFormatter(),
			new HexSByteValueFormatter(),
			new HexInt16ValueFormatter(),
			new HexInt32ValueFormatter(),
			new HexInt64ValueFormatter(),
			new DecimalByteValueFormatter(),
			new DecimalUInt16ValueFormatter(),
			new DecimalUInt32ValueFormatter(),
			new DecimalUInt64ValueFormatter(),
			new DecimalSByteValueFormatter(),
			new DecimalInt16ValueFormatter(),
			new DecimalInt32ValueFormatter(),
			new DecimalInt64ValueFormatter(),
			new SingleValueFormatter(),
			new DoubleValueFormatter(),
			new Bit8ValueFormatter(),
			new HexUInt16BigEndianValueFormatter(),
			new HexUInt32BigEndianValueFormatter(),
			new HexUInt64BigEndianValueFormatter(),
			new HexInt16BigEndianValueFormatter(),
			new HexInt32BigEndianValueFormatter(),
			new HexInt64BigEndianValueFormatter(),
			new DecimalUInt16BigEndianValueFormatter(),
			new DecimalUInt32BigEndianValueFormatter(),
			new DecimalUInt64BigEndianValueFormatter(),
			new DecimalInt16BigEndianValueFormatter(),
			new DecimalInt32BigEndianValueFormatter(),
			new DecimalInt64BigEndianValueFormatter(),
			new SingleBigEndianValueFormatter(),
			new DoubleBigEndianValueFormatter(),
		};

		static HexBufferLineProviderImpl() {
			for (int i = 0; i < valueFormatters.Length; i++) {
				if (valueFormatters[i].Format != (HexValuesDisplayFormat)i)
					throw new InvalidOperationException();
			}
			foreach (var fi in typeof(HexValuesDisplayFormat).GetFields()) {
				if (!fi.IsLiteral)
					continue;
				var value = (HexValuesDisplayFormat)fi.GetValue(null);
				if (value < HexBufferLineProviderOptions.HexValuesDisplayFormat_First || value > HexBufferLineProviderOptions.HexValuesDisplayFormat_Last)
					throw new InvalidOperationException();
			}
		}

		public override HexBuffer Buffer => buffer;
		public override HexBufferSpan BufferSpan => HexBufferSpan.FromBounds(new HexBufferPoint(buffer, startPosition), new HexBufferPoint(buffer, endPosition));
		public override HexPosition LineCount { get; }

		public override int CharsPerLine { get; }
		public override int BytesPerLine => (int)bytesPerLine;
		public override int GroupSizeInBytes => groupSizeInBytes;
		public override bool ShowOffset => showOffset;
		public override bool OffsetLowerCaseHex => offsetFormatter.LowerCaseHex;
		public override HexOffsetFormat OffsetFormat => offsetFormatter.Format;
		public override HexPosition StartPosition => startPosition;
		public override HexPosition EndPosition => endPosition;
		public override HexPosition BasePosition => basePosition;
		public override bool UseRelativePositions => useRelativePositions;
		public override bool ShowValues => showValues;
		public override bool ValuesLowerCaseHex => valuesLowerCaseHex;
		public override int OffsetBitSize => offsetFormatter.OffsetBitSize;
		public override HexValuesDisplayFormat ValuesFormat => valueFormatter.Format;
		public override int BytesPerValue => valueFormatter.ByteCount;
		public override bool ShowAscii => showAscii;
		public override ReadOnlyCollection<HexColumnType> ColumnOrder => columnOrder;
		public override VST.Span OffsetSpan { get; }
		public override VST.Span ValuesSpan { get; }
		public override VST.Span AsciiSpan { get; }
		public override ReadOnlyCollection<HexGroupInformation> ValuesGroup { get; }
		public override ReadOnlyCollection<HexGroupInformation> AsciiGroup { get; }

		readonly HexBuffer buffer;
		readonly StringBuilder stringBuilder;
		readonly List<HexCell> cellList;
		readonly bool useRelativePositions;
		readonly HexPosition startPosition;
		readonly HexPosition endPosition;
		readonly HexPosition basePosition;
		readonly HexOffsetFormatter offsetFormatter;
		readonly HexValueFormatter valueFormatter;
		readonly ulong bytesPerLine;
		readonly int groupSizeInBytes;
		readonly bool showOffset;
		readonly bool showValues;
		readonly bool showAscii;
		readonly bool valuesLowerCaseHex;

		readonly ReadOnlyCollection<HexColumnType> columnOrder;
		static readonly HexColumnType[] defaultColumnOrders = new HexColumnType[3] {
			HexColumnType.Offset,
			HexColumnType.Values,
			HexColumnType.Ascii,
		};

		public HexBufferLineProviderImpl(HexBuffer buffer, HexBufferLineProviderOptions options) {
			if (buffer == null)
				throw new ArgumentNullException(nameof(buffer));
			if (options == null)
				throw new ArgumentNullException(nameof(options));
			if (options.CharsPerLine < 0)
				throw new ArgumentOutOfRangeException(nameof(options));
			if (options.BytesPerLine < 0)
				throw new ArgumentOutOfRangeException(nameof(options));
			if (options.GroupSizeInBytes < 0)
				throw new ArgumentOutOfRangeException(nameof(options));
			if (options.OffsetFormat < HexBufferLineProviderOptions.HexOffsetFormat_First || options.OffsetFormat > HexBufferLineProviderOptions.HexOffsetFormat_Last)
				throw new ArgumentOutOfRangeException(nameof(options));
			if (options.StartPosition >= HexPosition.MaxEndPosition)
				throw new ArgumentOutOfRangeException(nameof(options));
			if (options.EndPosition > HexPosition.MaxEndPosition)
				throw new ArgumentOutOfRangeException(nameof(options));
			if (options.StartPosition > options.EndPosition)
				throw new ArgumentOutOfRangeException(nameof(options));
			if (options.BasePosition >= HexPosition.MaxEndPosition)
				throw new ArgumentOutOfRangeException(nameof(options));
			if (options.OffsetBitSize < HexBufferLineProviderOptions.MinOffsetBitSize || options.OffsetBitSize > HexBufferLineProviderOptions.MaxOffsetBitSize)
				throw new ArgumentOutOfRangeException(nameof(options));
			if (options.ValuesFormat < HexBufferLineProviderOptions.HexValuesDisplayFormat_First || options.ValuesFormat > HexBufferLineProviderOptions.HexValuesDisplayFormat_Last)
				throw new ArgumentOutOfRangeException(nameof(options));

			this.buffer = buffer;
			columnOrder = TryCreateColumns(options.ColumnOrder ?? defaultColumnOrders);
			if (columnOrder == null)
				throw new ArgumentOutOfRangeException(nameof(options));
			cellList = new List<HexCell>();
			useRelativePositions = options.UseRelativePositions;
			startPosition = options.StartPosition;
			endPosition = options.EndPosition;
			basePosition = options.BasePosition;
			showOffset = options.ShowOffset;
			showValues = options.ShowValues;
			showAscii = options.ShowAscii;
			valuesLowerCaseHex = options.ValuesLowerCaseHex;

			int bitSize = options.OffsetBitSize;
			if (bitSize == 0)
				bitSize = CalculateOffsetBitSize();
			offsetFormatter = CreateOffsetFormatter(options.OffsetFormat, bitSize, options.OffsetLowerCaseHex);
			valueFormatter = valueFormatters[(int)options.ValuesFormat];

			bytesPerLine = (ulong)options.BytesPerLine;
			if (bytesPerLine == 0)
				bytesPerLine = (ulong)CalculateBytesPerLine(options.CharsPerLine);
			if (bytesPerLine % (ulong)valueFormatter.ByteCount != 0)
				bytesPerLine = bytesPerLine - bytesPerLine % (ulong)valueFormatter.ByteCount + (ulong)valueFormatter.ByteCount;
			if (bytesPerLine > (ulong)HexBufferLineProviderOptions.MaxBytesPerLine)
				bytesPerLine = (ulong)(HexBufferLineProviderOptions.MaxBytesPerLine / valueFormatter.ByteCount * valueFormatter.ByteCount);
			if (bytesPerLine <= 0)
				throw new InvalidOperationException();
			groupSizeInBytes = options.GroupSizeInBytes;
			if (groupSizeInBytes == 0)
				groupSizeInBytes = DEFAULT_GROUP_SIZE_IN_BYTES;
			if ((ulong)groupSizeInBytes > bytesPerLine)
				groupSizeInBytes = (int)bytesPerLine;
			groupSizeInBytes = (groupSizeInBytes + valueFormatter.ByteCount - 1) / valueFormatter.ByteCount * valueFormatter.ByteCount;
			if (groupSizeInBytes <= 0)
				throw new InvalidOperationException();
			CharsPerLine = CalculateCharsPerLine();
			stringBuilder = new StringBuilder(CharsPerLine);

			var totalBytes = options.EndPosition - options.StartPosition;
			Debug.Assert(totalBytes <= new HexPosition(1, 0));
			if (totalBytes == 0)
				LineCount = 1;
			else if (bytesPerLine == 1)
				LineCount = totalBytes;
			else if (totalBytes == new HexPosition(1, 0)) {
				// 2^64 / x => (2^64 - x + x) / x => (2^64 - x) / x + 1
				Debug.Assert(bytesPerLine > 1);
				ulong d = (0UL - bytesPerLine) / bytesPerLine + 1UL;
				ulong r = 0UL - (d * bytesPerLine);
				LineCount = d + (r == 0UL ? 0UL : 1UL);
			}
			else {
				ulong v = totalBytes.ToUInt64();
				LineCount = v / bytesPerLine + (v % bytesPerLine == 0 ? 0UL : 1UL);
			}
			if (LineCount == 0)
				throw new InvalidOperationException();

			VST.Span offsetSpan, valuesSpan, asciiSpan;
			CalculateColumnSpans(out offsetSpan, out valuesSpan, out asciiSpan);
			OffsetSpan = offsetSpan;
			ValuesSpan = valuesSpan;
			AsciiSpan = asciiSpan;

			var group = new List<HexGroupInformation>();
			CalculateValuesGroupSpans(group);
			ValuesGroup = new ReadOnlyCollection<HexGroupInformation>(group.ToArray());
			CalculateAsciiGroupSpans(group);
			AsciiGroup = new ReadOnlyCollection<HexGroupInformation>(group.ToArray());
			if (ShowValues == ShowAscii && ValuesGroup.Count != AsciiGroup.Count)
				throw new InvalidOperationException();
		}

		void CalculateValuesGroupSpans(List<HexGroupInformation> list) {
			list.Clear();
			if (!ShowValues)
				return;

			int bytePos = 0;
			int pos = 0;
			bool isGroup0 = true;
			while (bytePos < BytesPerLine) {
				int groupByteEnd = bytePos + groupSizeInBytes;
				int groupStart = pos;
				bool needSep = false;
				while (bytePos < BytesPerLine && bytePos < groupByteEnd) {
					if (needSep)
						pos++;
					needSep = true;
					pos += valueFormatter.FormattedLength;
					bytePos = bytePos + valueFormatter.ByteCount;
				}
				int groupIndex = isGroup0 ? 0 : 1;
				var span = VST.Span.FromBounds(groupStart, pos);
				VST.Span fullSpan;
				if (bytePos < BytesPerLine) {
					fullSpan = new VST.Span(span.Start, span.Length + 1);
					pos++;
				}
				else
					fullSpan = span;
				list.Add(new HexGroupInformation(groupIndex, fullSpan, span));
				isGroup0 = !isGroup0;
			}
			if (pos != ValuesSpan.Length)
				throw new InvalidOperationException();
		}

		void CalculateAsciiGroupSpans(List<HexGroupInformation> list) {
			list.Clear();
			if (!ShowAscii)
				return;

			int pos = 0;
			bool isGroup0 = true;
			while (pos < BytesPerLine) {
				int groupStart = pos;
				pos += Math.Min(BytesPerLine - pos, groupSizeInBytes);
				int groupIndex = isGroup0 ? 0 : 1;
				var span = VST.Span.FromBounds(groupStart, pos);
				list.Add(new HexGroupInformation(groupIndex, span, span));
				isGroup0 = !isGroup0;
			}
			if (pos != AsciiSpan.Length)
				throw new InvalidOperationException();
		}

		void CalculateColumnSpans(out VST.Span offsetSpan, out VST.Span valuesSpan, out VST.Span asciiSpan) {
			offsetSpan = default(VST.Span);
			valuesSpan = default(VST.Span);
			asciiSpan = default(VST.Span);

			bool needSep = false;
			int position = 0;
			foreach (var column in columnOrder) {
				switch (column) {
				case HexColumnType.Offset:
					if (showOffset) {
						if (needSep)
							position++;
						needSep = true;
						offsetSpan = new VST.Span(position, offsetFormatter.FormattedLength);
						position = offsetSpan.End;
					}
					break;

				case HexColumnType.Values:
					if (showValues) {
						if (needSep)
							position++;
						needSep = true;
						int cellCount = (int)(bytesPerLine / (ulong)valueFormatter.ByteCount);
						valuesSpan = new VST.Span(position, valueFormatter.FormattedLength * cellCount + cellCount - 1);
						position = valuesSpan.End;
					}
					break;

				case HexColumnType.Ascii:
					if (showAscii) {
						if (needSep)
							position++;
						needSep = true;
						asciiSpan = new VST.Span(position, (int)bytesPerLine);
						position = asciiSpan.End;
					}
					break;

				default:
					throw new InvalidOperationException();
				}
			}
			if (position != CharsPerLine)
				throw new InvalidOperationException();
		}

		int CalculateCharsPerLine() {
			int columnCount = (showOffset ? 1 : 0) + (showValues ? 1 : 0) + (showAscii ? 1 : 0);
			if (columnCount == 0)
				return 0;

			// Add column separators
			int length = columnCount - 1;

			if (showOffset)
				length += offsetFormatter.FormattedLength;
			if (showValues) {
				Debug.Assert(bytesPerLine % (ulong)valueFormatter.ByteCount == 0);
				int vc = (int)(bytesPerLine / (ulong)valueFormatter.ByteCount);
				Debug.Assert(vc > 0);
				length += valueFormatter.FormattedLength * vc + vc - 1;
			}
			if (showAscii)
				length += (int)bytesPerLine;

			return length;
		}

		static ReadOnlyCollection<HexColumnType> TryCreateColumns(HexColumnType[] columnOrders) {
			var columns = columnOrders.ToList();
			if (!columns.Contains(HexColumnType.Offset))
				columns.Add(HexColumnType.Offset);
			if (!columns.Contains(HexColumnType.Values))
				columns.Add(HexColumnType.Values);
			if (!columns.Contains(HexColumnType.Ascii))
				columns.Add(HexColumnType.Ascii);
			if (columns.Count != 3)
				return null;
			return new ReadOnlyCollection<HexColumnType>(columns.ToArray());
		}

		int CalculateBytesPerLine(int charsPerLine) {
			int columnCount = (showOffset ? 1 : 0) + (showValues ? 1 : 0) + (showAscii ? 1 : 0);
			if (columnCount == 0)
				return 1;
			int fixedLength = columnCount - 1;// N-1 separators
			if (showOffset)
				fixedLength += offsetFormatter.FormattedLength;

			if (fixedLength >= charsPerLine)
				return showValues ? valueFormatter.ByteCount : 1;
			int remainingChars = charsPerLine - fixedLength;
			if (showValues && showAscii) {
				// VL = value length in chars
				// VC = number of values
				// VB = number of bytes per value
				// Total size required by values and ASCII, not counting the separator between the
				// two columns, it's included in fixedLength above.
				// ___VALUES_COLUMN____   _ASCII_
				// (VC * VL + (VC - 1)) + VC * VB <= remainingChars
				// or
				// VC <= (remainingChars + 1) / (VL + 1 + VB)
				int vl = valueFormatter.FormattedLength;
				int vb = valueFormatter.ByteCount;
				int vc = (remainingChars + 1) / (vl + 1 + vb);
				if (vc <= 0)
					vc = 1;
				return vc * vb;
			}
			else if (showValues) {
				// VL = value length in chars
				// VC = number of values
				// VC * VL + (VC - 1) <= remainingChars
				// or
				// VC <= (remainingChars + 1) / (VL + 1)
				int vl = valueFormatter.FormattedLength;
				int vb = valueFormatter.ByteCount;
				int vc = (remainingChars + 1) / (vl + 1);
				if (vc <= 0)
					vc = 1;
				return vc * vb;
			}
			else if (showAscii)
				return Math.Max(1, remainingChars);

			// No data is shown
			return 1;
		}

		int CalculateOffsetBitSize() {
			var end = ToLogicalPosition(endPosition > 0 ? endPosition - 1 : 0).ToUInt64();
			if (end <= byte.MaxValue) return 8;
			if (end <= ushort.MaxValue) return 16;
			if (end <= uint.MaxValue) return 32;
			return 64;
		}

		static HexOffsetFormatter CreateOffsetFormatter(HexOffsetFormat format, int bitSize, bool lowerCaseHex) {
			bitSize = (bitSize + 3) / 4 * 4;
			switch (format) {
			case HexOffsetFormat.Hex:				return new OnlyHexOffsetFormatter(bitSize, lowerCaseHex);
			case HexOffsetFormat.HexCSharp:			return new HexCSharpOffsetFormatter(bitSize, lowerCaseHex);
			case HexOffsetFormat.HexVisualBasic:	return new HexVisualBasicOffsetFormatter(bitSize, lowerCaseHex);
			case HexOffsetFormat.HexAssembly:		return new HexAssemblyOffsetFormatter(bitSize, lowerCaseHex);
			default: throw new ArgumentOutOfRangeException(nameof(bitSize));
			}
		}

		public override HexPosition GetLineNumberFromPosition(HexBufferPoint position) {
			position = FilterAndVerify(position);
			return (position.Position - startPosition).ToUInt64() / bytesPerLine;
		}

		int CurrentTextIndex => stringBuilder.Length;

		public override HexBufferPoint GetBufferPositionFromLineNumber(HexPosition lineNumber) {
			if (lineNumber >= LineCount)
				throw new ArgumentOutOfRangeException(nameof(lineNumber));
			return new HexBufferPoint(Buffer, startPosition + checked(lineNumber.ToUInt64() * bytesPerLine));
		}

		public override HexBufferLine GetLineFromLineNumber(HexPosition lineNumber) {
			if (lineNumber >= LineCount)
				throw new ArgumentOutOfRangeException(nameof(lineNumber));
			var position = startPosition + checked(lineNumber.ToUInt64() * bytesPerLine);
			return GetLineFromPosition(new HexBufferPoint(Buffer, position));
		}

		public override HexBufferLine GetLineFromPosition(HexBufferPoint position) {
			position = FilterAndVerify(position);
			ResetBuilderFields();

			var linePosition = startPosition + (position.Position - startPosition).ToUInt64() / bytesPerLine * bytesPerLine;
			var lineEnd = HexPosition.Min(linePosition + bytesPerLine, endPosition);
			var lineSpan = HexSpan.FromBounds(linePosition, lineEnd);
			var logicalOffset = ToLogicalPosition(lineSpan.Start);

			var hexBytes = buffer.ReadHexBytes(lineSpan.Start, (long)lineSpan.Length.ToUInt64());

			var offsetSpan = default(VST.Span);
			var fullValuesSpan = default(VST.Span);
			var visibleValuesSpan = default(VST.Span);
			var fullAsciiSpan = default(VST.Span);
			var visibleAsciiSpan = default(VST.Span);

			var valueCells = Array.Empty<HexCell>();
			var asciiCells = Array.Empty<HexCell>();

			bool needSep = false;
			foreach (var column in columnOrder) {
				switch (column) {
				case HexColumnType.Offset:
					if (showOffset) {
						if (needSep)
							stringBuilder.Append(' ');
						needSep = true;
						WriteOffset(logicalOffset, out offsetSpan);
					}
					break;

				case HexColumnType.Values:
					if (showValues) {
						if (needSep)
							stringBuilder.Append(' ');
						needSep = true;
						valueCells = WriteValues(hexBytes, lineSpan, out fullValuesSpan, out visibleValuesSpan);
					}
					break;

				case HexColumnType.Ascii:
					if (showAscii) {
						if (needSep)
							stringBuilder.Append(' ');
						needSep = true;
						asciiCells = WriteAscii(hexBytes, lineSpan, out fullAsciiSpan, out visibleAsciiSpan);
					}
					break;

				default:
					throw new InvalidOperationException();
				}
			}

			var text = stringBuilder.ToString();
			if (text.Length != CharsPerLine)
				throw new InvalidOperationException();
			var valueCellColl = valueCells.Length == 0 ? HexCellCollection.Empty : new HexCellCollection(valueCells);
			var asciiCellColl = asciiCells.Length == 0 ? HexCellCollection.Empty : new HexCellCollection(asciiCells);
			var lineNumber = GetLineNumberFromPosition(new HexBufferPoint(Buffer, lineSpan.Start));
			var res = new HexBufferLineImpl(this, lineNumber, columnOrder, new HexBufferSpan(buffer, lineSpan), hexBytes, text, showOffset, showValues, showAscii, logicalOffset, valueCellColl, asciiCellColl, offsetSpan, fullValuesSpan, visibleValuesSpan, fullAsciiSpan, visibleAsciiSpan);
			ResetBuilderFields();
			return res;
		}

		void ResetBuilderFields() {
			stringBuilder.Clear();
			cellList.Clear();
		}

		void WriteOffset(HexPosition logicalPosition, out VST.Span offsetSpan) {
			Debug.Assert(showOffset);
			int start = CurrentTextIndex;
			offsetFormatter.FormatOffset(stringBuilder, logicalPosition);
			offsetSpan = VST.Span.FromBounds(start, CurrentTextIndex);
			if (OffsetSpan != offsetSpan)
				throw new InvalidOperationException();
		}

		HexCell[] WriteValues(HexBytes hexBytes, HexSpan visibleBytesSpan, out VST.Span fullSpan, out VST.Span visibleSpan) {
			Debug.Assert(showValues);
			cellList.Clear();
			int fullStart = CurrentTextIndex;

			ulong cellCount = bytesPerLine / (ulong)valueFormatter.ByteCount;
			var flags = valuesLowerCaseHex ? HexValueFormatterFlags.LowerCaseHex : HexValueFormatterFlags.None;
			var pos = visibleBytesSpan.Start;
			var end = visibleBytesSpan.Start + bytesPerLine;
			int? visStart = null;
			int? visEnd = null;
			int cellPos = 0;
			for (ulong i = 0; i < cellCount; i++) {
				if (i != 0)
					stringBuilder.Append(' ');
				int groupIndex = (cellPos / groupSizeInBytes) & 1;

				HexBufferSpan bufferSpan;
				int cellStart = CurrentTextIndex;
				int spaces;
				if (visibleBytesSpan.Contains(pos)) {
					if (visStart == null)
						visStart = CurrentTextIndex;
					long valueIndex = (long)(pos - visibleBytesSpan.Start).ToUInt64();
					spaces = valueFormatter.FormatValue(stringBuilder, hexBytes, valueIndex, flags);
					var endPos = HexPosition.Min(endPosition, pos + (ulong)valueFormatter.ByteCount);
					bufferSpan = new HexBufferSpan(new HexBufferPoint(buffer, pos), new HexBufferPoint(buffer, endPos));
				}
				else {
					if (visStart != null && visEnd == null)
						visEnd = CurrentTextIndex;
					stringBuilder.Append(' ', valueFormatter.FormattedLength);
					spaces = valueFormatter.FormattedLength;
					bufferSpan = default(HexBufferSpan);
				}
				if (cellStart + valueFormatter.FormattedLength != CurrentTextIndex)
					throw new InvalidOperationException();
				var textSpan = VST.Span.FromBounds(cellStart + spaces, CurrentTextIndex);
				var cellSpan = VST.Span.FromBounds(cellStart, CurrentTextIndex);
				VST.Span separatorSpan;
				if (i + 1 < cellCount)
					separatorSpan = new VST.Span(CurrentTextIndex, 1);
				else
					separatorSpan = new VST.Span(CurrentTextIndex, 0);
				var cellFullSpan = VST.Span.FromBounds(cellStart, separatorSpan.End);
				cellList.Add(new HexCell((int)i, groupIndex, bufferSpan, textSpan, cellSpan, separatorSpan, cellFullSpan));

				pos += (ulong)valueFormatter.ByteCount;
				cellPos += valueFormatter.ByteCount;
			}
			if (pos != end)
				throw new InvalidOperationException();
			if (visStart != null && visEnd == null)
				visEnd = CurrentTextIndex;
			visibleSpan = visStart == null ? default(VST.Span) : VST.Span.FromBounds(visStart.Value, visEnd.Value);
			fullSpan = VST.Span.FromBounds(fullStart, CurrentTextIndex);
			if (ValuesSpan != fullSpan)
				throw new InvalidOperationException();
			return cellList.ToArray();
		}

		HexCell[] WriteAscii(HexBytes hexBytes, HexSpan visibleBytesSpan, out VST.Span fullSpan, out VST.Span visibleSpan) {
			Debug.Assert(showAscii);
			cellList.Clear();
			int fullStart = CurrentTextIndex;

			int? visStart = null;
			int? visEnd = null;
			var pos = visibleBytesSpan.Start;
			int cellPos = 0;
			for (ulong i = 0; i < bytesPerLine; i++, pos++) {
				int groupIndex = (cellPos / groupSizeInBytes) & 1;

				HexBufferSpan bufferSpan;
				int cellStart = CurrentTextIndex;
				if (visibleBytesSpan.Contains(pos)) {
					if (visStart == null)
						visStart = CurrentTextIndex;
					long index = (long)(pos - visibleBytesSpan.Start).ToUInt64();
					int b = hexBytes.TryReadByte(index);
					if (b < 0)
						stringBuilder.Append('?');
					else if (b < 0x20 || b > 0x7E)
						stringBuilder.Append('.');
					else
						stringBuilder.Append((char)b);
					bufferSpan = new HexBufferSpan(buffer, new HexSpan(pos, 1));
				}
				else {
					if (visStart != null && visEnd == null)
						visEnd = CurrentTextIndex;
					stringBuilder.Append(' ');
					bufferSpan = default(HexBufferSpan);
				}
				var cellSpan = VST.Span.FromBounds(cellStart, CurrentTextIndex);
				var separatorSpan = new VST.Span(cellSpan.End, 0);
				cellList.Add(new HexCell((int)i, groupIndex, bufferSpan, cellSpan, cellSpan, separatorSpan, cellSpan));

				cellPos++;
			}
			if ((ulong)fullStart + bytesPerLine != (ulong)CurrentTextIndex)
				throw new InvalidOperationException();
			if (visStart != null && visEnd == null)
				visEnd = CurrentTextIndex;
			visibleSpan = visStart == null ? default(VST.Span) : VST.Span.FromBounds(visStart.Value, visEnd.Value);
			fullSpan = VST.Span.FromBounds(fullStart, CurrentTextIndex);
			if (AsciiSpan != fullSpan)
				throw new InvalidOperationException();
			return cellList.ToArray();
		}

		public override HexPosition ToLogicalPosition(HexPosition physicalPosition) =>
			(physicalPosition - (useRelativePositions ? startPosition : 0) + basePosition).ToUInt64();

		public override HexPosition ToPhysicalPosition(HexPosition logicalPosition) =>
			(logicalPosition - basePosition + (useRelativePositions ? startPosition : 0)).ToUInt64();

		public override int GetCharsPerCell(HexColumnType column) {
			switch (column) {
			case HexColumnType.Offset:	return offsetFormatter.FormattedLength;
			case HexColumnType.Values:	return valueFormatter.FormattedLength;
			case HexColumnType.Ascii:	return 1;
			default: throw new ArgumentOutOfRangeException(nameof(column));
			}
		}

		public override int GetCharsPerCellIncludingSeparator(HexColumnType column) {
			switch (column) {
			case HexColumnType.Offset:	return offsetFormatter.FormattedLength;
			case HexColumnType.Values:	return valueFormatter.FormattedLength + 1;
			case HexColumnType.Ascii:	return 1;
			default: throw new ArgumentOutOfRangeException(nameof(column));
			}
		}

		public override HexBufferSpan GetValueBufferSpan(HexCell cell, int cellPosition) {
			if (cell == null)
				throw new ArgumentNullException(nameof(cell));
			if (cellPosition < 0 || cellPosition >= cell.CellSpan.Length)
				throw new ArgumentOutOfRangeException(nameof(cellPosition));
			return valueFormatter.GetBufferSpan(cell.BufferSpan, cell.CellSpan.Start + cellPosition);
		}

		public override bool CanEditValueCell => valueFormatter.CanEdit;

		public override PositionAndData? EditValueCell(HexCell cell, int cellPosition, char c) {
			if (cell == null)
				throw new ArgumentNullException(nameof(cell));
			if (cell.BufferSpan.Buffer != Buffer)
				throw new ArgumentException();
			if ((uint)cellPosition >= (uint)cell.CellSpan.Length)
				throw new ArgumentOutOfRangeException(nameof(cellPosition));
			if (!cell.HasData)
				return null;
			return valueFormatter.Edit(cell.BufferStart, cellPosition, c);
		}

		public override string GetFormattedOffset(HexPosition position) {
			ResetBuilderFields();
			offsetFormatter.FormatOffset(stringBuilder, position);
			var result = stringBuilder.ToString();
			ResetBuilderFields();
			return result;
		}
	}
}
