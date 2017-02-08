/*
    Copyright (C) 2014-2017 de4dot@gmail.com

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

using System.Diagnostics;
using Microsoft.VisualStudio.Text;

namespace dnSpy.Text.Operations {
	struct WordParser {
		public enum WordKind {
			Word,
			Whitespace,
			Other,
		}

		public static SnapshotSpan GetWordSpan(SnapshotPoint currentPosition, out WordKind kind) {
			kind = GetWordKind(currentPosition);
			return GetWordSpan(currentPosition, kind);
		}

		static SnapshotPoint GetStartSpanBefore(ITextSnapshotLine line, int column, WordKind kind) {
			int position = line.Start.Position + column;
			var snapshot = line.Snapshot;
			for (;;) {
				if (position == line.Start.Position)
					return line.Start;
				position--;
				if (GetWordKind(snapshot[position]) != kind)
					return new SnapshotPoint(snapshot, position + 1);
			}
		}

		static SnapshotPoint GetEndSpanAfter(ITextSnapshotLine line, int column, WordKind kind) {
			int position = line.Start.Position + column;
			var snapshot = line.Snapshot;
			for (;;) {
				if (position + 1 >= line.End.Position)
					return new SnapshotPoint(snapshot, line.End.Position);
				position++;
				if (GetWordKind(snapshot[position]) != kind)
					return new SnapshotPoint(snapshot, position);
			}
		}

		static SnapshotSpan GetWordSpan(SnapshotPoint currentPosition, WordKind kind) {
			Debug.Assert(GetWordKind(currentPosition) == kind);
			var line = currentPosition.GetContainingLine();
			int column = currentPosition.Position - line.Start.Position;
			var start = GetStartSpanBefore(line, column, kind);
			var end = GetEndSpanAfter(line, column, kind);
			return new SnapshotSpan(start, end);
		}

		static WordKind GetWordKind(SnapshotPoint currentPosition) {
			if (currentPosition.Position >= currentPosition.Snapshot.Length)
				return WordKind.Whitespace;
			return GetWordKind(currentPosition.GetChar());
		}

		static WordKind GetWordKind(char c) {
			if (char.IsLetterOrDigit(c) || c == '_')
				return WordKind.Word;
			if (char.IsWhiteSpace(c))
				return WordKind.Whitespace;
			return WordKind.Other;
		}
	}
}
