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

namespace dnSpy.Decompiler.Shared {
	public struct TextPosition : IEquatable<TextPosition>, IComparable<TextPosition> {
		public int Line {
			get { return line; }
		}
		readonly int line;

		public int Column {
			get { return column; }
		}
		readonly int column;

		public TextPosition(int line, int column) {
			this.line = line;
			this.column = column;
		}

		public static bool operator <(TextPosition a, TextPosition b) {
			return a.CompareTo(b) < 0;
		}

		public static bool operator <=(TextPosition a, TextPosition b) {
			return a.CompareTo(b) <= 0;
		}

		public static bool operator >(TextPosition a, TextPosition b) {
			return a.CompareTo(b) > 0;
		}

		public static bool operator >=(TextPosition a, TextPosition b) {
			return a.CompareTo(b) >= 0;
		}

		public static bool operator ==(TextPosition a, TextPosition b) {
			return a.Equals(b);
		}

		public static bool operator !=(TextPosition a, TextPosition b) {
			return !a.Equals(b);
		}

		public int CompareTo(TextPosition other) {
			int c = line.CompareTo(other.line);
			if (c != 0)
				return c;
			return column.CompareTo(other.column);
		}

		public bool Equals(TextPosition other) {
			return line == other.line &&
					column == other.column;
		}

		public override bool Equals(object obj) {
			var other = obj as TextPosition?;
			return other != null && Equals(other.Value);
		}

		public override int GetHashCode() {
			return (line << 10) ^ column;
		}

		public override string ToString() {
			return string.Format("({0},{1})", line, column);
		}
	}
}
