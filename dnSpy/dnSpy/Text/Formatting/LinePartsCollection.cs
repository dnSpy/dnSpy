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
using System.Diagnostics;
using Microsoft.VisualStudio.Text;

namespace dnSpy.Text.Formatting {
	sealed class LinePartsCollection {
		public List<LinePart> LineParts { get; }
		public int Length { get; }
		public SnapshotSpan Span { get; private set; }
		int linePartsIndex;

		public LinePartsCollection(List<LinePart> lineParts, SnapshotSpan span) {
			if (lineParts == null)
				throw new ArgumentNullException(nameof(lineParts));
			if (span.Snapshot == null)
				throw new ArgumentException();
			Span = span;
			LineParts = lineParts;
			if (lineParts.Count == 0)
				this.Length = 0;
			else {
				var last = lineParts[lineParts.Count - 1];
				this.Length = last.Column + last.Span.Length;
			}
		}

		public LinePart? GetLinePartFromColumn(int column) {
			if (LineParts.Count == 0)
				return null;
			for (int i = 0; i < LineParts.Count; i++) {
				var part = LineParts[linePartsIndex];
				if (part.Column <= column && column < part.Column + part.Span.Length)
					return part;
				linePartsIndex++;
				if (linePartsIndex >= LineParts.Count)
					linePartsIndex = 0;
			}
			return null;
		}

		public LinePart? GetLinePartFromBufferPosition(SnapshotPoint bufferPosition) {
			if (bufferPosition.Snapshot != Span.Snapshot)
				throw new ArgumentException();
			if (LineParts.Count == 0)
				return null;
			int lineIndex = bufferPosition - Span.Start;
			for (int i = 0; i < LineParts.Count; i++) {
				var part = LineParts[linePartsIndex];
				if (part.Span.Start <= lineIndex && lineIndex < part.Span.End)
					return part;
				linePartsIndex++;
				if (linePartsIndex >= LineParts.Count)
					linePartsIndex = 0;
			}
			return null;
		}

		public int ConvertBufferPositionToColumn(SnapshotPoint bufferPosition) {
			if (bufferPosition.Snapshot != Span.Snapshot)
				throw new ArgumentException();
			var linePart = GetLinePartFromBufferPosition(bufferPosition);
			if (linePart == null && bufferPosition == Span.End && LineParts.Count != 0)
				linePart = LineParts[LineParts.Count - 1];
			if (linePart == null)
				return 0;
			return linePart.Value.Column + ((bufferPosition.Position - Span.Span.Start) - linePart.Value.Span.Start);
		}

		public SnapshotPoint ConvertColumnToBufferPosition(int column) {
			var linePart = GetLinePartFromColumn(column);
			if (linePart == null && column == Length && LineParts.Count != 0)
				linePart = LineParts[LineParts.Count - 1];
			return Span.Start + (linePart == null ? 0 : linePart.Value.Span.Start + (column - linePart.Value.Column));
		}

		public void SetSnapshot(ITextSnapshot visualSnapshot, ITextSnapshot editSnapshot) {
			int oldLength = Span.Length;
			Span = Span.TranslateTo(editSnapshot, SpanTrackingMode.EdgeExclusive);
			// This line should've been invalidated if there were any changes to it
			if (oldLength != Span.Length)
				throw new InvalidOperationException();
			Debug.Assert(Span.Start.GetContainingLine().ExtentIncludingLineBreak == Span);
		}
	}
}
