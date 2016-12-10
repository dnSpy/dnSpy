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
using System.ComponentModel.Composition;
using System.Text;
using dnSpy.Contracts.Hex.Operations;

namespace dnSpy.Hex.Operations {
	[Export(typeof(HexSearchServiceProvider))]
	sealed class HexSearchServiceProviderImpl : HexSearchServiceProvider {
		public override HexSearchService CreateByteSearchService(byte[] pattern, byte[] mask) =>
			new ByteHexSearchService(pattern, mask);

		public override HexSearchService CreateUtf8StringSearchService(string pattern, bool isCaseSensitive) {
			if (pattern == null)
				throw new ArgumentNullException(nameof(pattern));
			if (pattern.Length == 0)
				throw new ArgumentOutOfRangeException(nameof(pattern));
			if (isCaseSensitive)
				return CreateByteSearchService(Encoding.UTF8.GetBytes(pattern));
			return new Utf8StringHexSearchService(pattern);
		}

		public override HexSearchService CreateUtf16StringSearchService(string pattern, bool isCaseSensitive, bool isBigEndian) {
			if (pattern == null)
				throw new ArgumentNullException(nameof(pattern));
			if (pattern.Length == 0)
				throw new ArgumentOutOfRangeException(nameof(pattern));
			if (isCaseSensitive)
				return CreateByteSearchService(Encoding.Unicode.GetBytes(pattern));
			if (isBigEndian)
				return new BigEndianUtf16StringHexSearchService(pattern);
			return new Utf16StringHexSearchService(pattern);
		}
	}
}
