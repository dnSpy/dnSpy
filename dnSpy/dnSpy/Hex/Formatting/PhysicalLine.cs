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
using dnSpy.Contracts.Hex;
using dnSpy.Contracts.Hex.Formatting;

namespace dnSpy.Hex.Formatting {
	/// <summary>
	/// Contains one or more visual lines. One physical line can have more than one visual line if
	/// word wrap is enabled or if the line was split up for some other reason, eg. it is too long.
	/// </summary>
	sealed class PhysicalLine {
		public HexFormattedLine[] Lines { get; }
		public HexBufferSpan BufferSpan => Lines[0].BufferLine.BufferSpan;
		public bool IsLastLine { get; private set; }
		public HexBufferLineProvider BufferLines => Lines[0].BufferLine.LineProvider;

		public PhysicalLine(HexFormattedLine[] lines) {
			if (lines == null)
				throw new ArgumentNullException(nameof(lines));
			if (lines.Length == 0)
				throw new ArgumentException();
			Lines = lines;
			IsLastLine = Lines[0].BufferLine.LineNumber + 1 == Lines[0].BufferLine.LineProvider.LineCount;
		}

		public int IndexOf(HexFormattedLine line) => Array.IndexOf(Lines, line);

		public bool Contains(HexBufferPoint point) {
			if (disposed)
				throw new ObjectDisposedException(nameof(PhysicalLine));
			if (point.Buffer != BufferSpan.Buffer)
				throw new ArgumentException();
			if (point < BufferSpan.Start)
				return false;
			if (IsLastLine)
				return point <= BufferSpan.End;
			return point < BufferSpan.End;
		}

		public HexFormattedLine FindFormattedLineByBufferPosition(HexBufferPoint point) {
			if (disposed)
				throw new ObjectDisposedException(nameof(PhysicalLine));
			if (point.Buffer != BufferSpan.Buffer)
				throw new ArgumentException();
			if (!Contains(point))
				return null;
			foreach (var line in Lines) {
				if (point <= line.BufferStart || line.ContainsBufferPosition(point))
					return line;
			}
			return Lines[Lines.Length - 1];
		}

		public bool OverlapsWith(NormalizedHexBufferSpanCollection regions) {
			if (disposed)
				throw new ObjectDisposedException(nameof(PhysicalLine));
			if (regions.Count == 0)
				return false;
			if (BufferSpan.Buffer != regions[0].Buffer)
				throw new ArgumentException();
			foreach (var r in regions) {
				if (r.OverlapsWith(BufferSpan))
					return true;
			}
			return false;
		}

		public void Dispose() {
			if (disposed)
				return;
			disposed = true;
			foreach (var l in Lines)
				l.Dispose();
		}
		bool disposed;

		public void UpdateIsLastLine() =>
			IsLastLine = Lines[0].BufferLine.LineNumber + 1 == Lines[0].BufferLine.LineProvider.LineCount;
	}
}
