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
	static class UnicodeUtilities {
		public static bool IsWord(ITextSnapshotLine line, int lineIndex, int length) {
			if (lineIndex < 0 || length > line.Length)
				return false;
			if (lineIndex == 0 && length == line.Length)
				return true;
			if (length <= 0)
				return false;
			if (lineIndex != 0 && !IsWordBreak(line, lineIndex))
				return false;
			if (length != line.Length && !IsWordBreak(line, lineIndex + length))
				return false;
			return true;
		}

		static bool IsWordBreak(ITextSnapshotLine line, int lineIndex) {
			if (lineIndex <= 0 || lineIndex >= line.Length)
				return true;

			// TODO: This code only supports 16-bit chars

			var snapshot = line.Snapshot;
			var position = line.Start.Position + lineIndex;
			var cr = snapshot[position];
			if (IsNoBreak(cr))
				return false;
			var cl = snapshot[position - 1];
			if (IsNoBreak(cl))
				return false;

			bool wr = IsWordChar(cr);
			bool wl = IsWordChar(cl);
			if (wr != wl || (!wr && !wl))
				return true;

			return false;
		}

		static bool IsWordChar(char c) => char.IsLetterOrDigit(c) || c == '_' || c == '$';// Same as VS

		static bool IsNoBreak(char c) =>
			c == '\u00A0' ||// Unicode Character 'NO-BREAK SPACE' (U+00A0)
			c == '\u2011' ||// Unicode Character 'NON-BREAKING HYPHEN' (U+2011)
			c == '\u202F' ||// Unicode Character 'NARROW NO-BREAK SPACE' (U+202F)
			c == '\u30FC' ||// Unicode Character 'KATAKANA-HIRAGANA PROLONGED SOUND MARK' (U+30FC)
			c == '\uFEFF';	// Unicode Character 'ZERO WIDTH NO-BREAK SPACE' (U+FEFF)
	}
}
