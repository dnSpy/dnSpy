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

using System.Text;

namespace dnSpy.Hex.Operations {
	sealed class Utf16StringHexSearchService : StringHexSearchService {
		public Utf16StringHexSearchService(string pattern)
			: base(pattern) {
		}

		protected override bool Initialize(string pattern, out byte[] lowerBytes, out byte[] upperBytes, out byte[] charLengths) =>
			Initialize(Encoding.Unicode, pattern, out lowerBytes, out upperBytes, out charLengths);
	}

	sealed class BigEndianUtf16StringHexSearchService : StringHexSearchService {
		public BigEndianUtf16StringHexSearchService(string pattern)
			: base(pattern) {
		}

		protected override bool Initialize(string pattern, out byte[] lowerBytes, out byte[] upperBytes, out byte[] charLengths) =>
			Initialize(Encoding.BigEndianUnicode, pattern, out lowerBytes, out upperBytes, out charLengths);
	}
}
