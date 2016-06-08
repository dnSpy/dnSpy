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

using dnSpy.Contracts.Text;

namespace dnSpy.Text.Editor {
	static class TextSnapshotLineExtensions {
		public static bool IsLineEmpty(this ITextSnapshotLine line) {
			if (line.Length == 0)
				return true;
			// Check the end first, it's rarely whitespace if there's non-whitespace on the line
			if (!char.IsWhiteSpace((line.End - 1).GetChar()))
				return false;

			// Don't check the end, we already checked it above
			int end = line.End.Position - 1;
			for (int offset = line.Start.Position; offset < end; offset++) {
				if (!char.IsWhiteSpace(line.Snapshot[offset]))
					return false;
			}
			return true;
		}
	}
}
