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
using System.Text;
using dnSpy.Contracts.Hex;

namespace dnSpy.Hex {
	abstract class HexOffsetFormatter {
		public HexOffsetFormat Format { get; }
		public int FormattedLength { get; }

		readonly string prefix;
		readonly string suffix;
		readonly int bitSize;
		readonly bool lowerCaseHex;

		protected HexOffsetFormatter(int bitSize, bool lowerCaseHex, string prefix, string suffix, HexOffsetFormat format) {
			if (bitSize <= 0 || (bitSize % 4) != 0)
				throw new ArgumentOutOfRangeException(nameof(bitSize));
			if (prefix == null)
				throw new ArgumentNullException(nameof(prefix));
			if (suffix == null)
				throw new ArgumentNullException(nameof(suffix));
			FormattedLength = prefix.Length + bitSize / 4 + suffix.Length;
			this.prefix = prefix;
			this.suffix = suffix;
			this.bitSize = bitSize;
			this.lowerCaseHex = lowerCaseHex;
			Format = format;
		}

		public void FormatOffset(StringBuilder dest, HexPosition position) {
			var offset = position.ToUInt64() << (64 - bitSize);
			dest.Append(prefix);
			for (int i = 0; i < bitSize; i += 4, offset <<= 4) {
				var nibble = (offset >> 60) & 0x0F;
				if (nibble < 10)
					dest.Append((char)('0' + nibble));
				else
					dest.Append((char)((lowerCaseHex ? 'a' : 'A') + nibble - 10));
			}
			dest.Append(suffix);
		}
	}

	sealed class OnlyHexOffsetFormatter : HexOffsetFormatter {
		public OnlyHexOffsetFormatter(int bitSize, bool lowerCaseHex)
			: base(bitSize, lowerCaseHex, string.Empty, string.Empty, HexOffsetFormat.Hex) {
		}
	}

	sealed class HexCSharpOffsetFormatter : HexOffsetFormatter {
		public HexCSharpOffsetFormatter(int bitSize, bool lowerCaseHex)
			: base(bitSize, lowerCaseHex, "0x", string.Empty, HexOffsetFormat.HexCSharp) {
		}
	}
}
