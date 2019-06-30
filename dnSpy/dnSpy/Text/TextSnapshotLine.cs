/*
    Copyright (C) 2014-2019 de4dot@gmail.com

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

using Microsoft.VisualStudio.Text;

namespace dnSpy.Text {
	sealed class TextSnapshotLine : ITextSnapshotLine {
		public SnapshotPoint Start => new SnapshotPoint(Snapshot, position);
		public SnapshotPoint End => new SnapshotPoint(Snapshot, position + Length);
		public SnapshotPoint EndIncludingLineBreak => new SnapshotPoint(Snapshot, position + LengthIncludingLineBreak);
		public SnapshotSpan Extent => new SnapshotSpan(Snapshot, position, Length);
		public SnapshotSpan ExtentIncludingLineBreak => new SnapshotSpan(Snapshot, position, LengthIncludingLineBreak);

		public int Length { get; }
		public int LengthIncludingLineBreak => Length + LineBreakLength;
		public int LineBreakLength { get; }
		public int LineNumber { get; }
		public ITextSnapshot Snapshot { get; }

		readonly int position;

		public TextSnapshotLine(ITextSnapshot snapshot, int lineNumber, int position, int length, int lineBreakLength) {
			Snapshot = snapshot;
			LineNumber = lineNumber;
			Length = length;
			LineBreakLength = lineBreakLength;
			this.position = position;
		}

		public string GetLineBreakText() => Snapshot.GetText(position + Length, LineBreakLength);
		public string GetText() => Snapshot.GetText(position, Length);
		public string GetTextIncludingLineBreak() => Snapshot.GetText(position, LengthIncludingLineBreak);

		// VS doesn't override ToString() so we also don't
	}
}
