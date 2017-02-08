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

namespace dnSpy.Contracts.Hex.Files.DotNet {
	static class Utils {
		public static int? Read7BitEncodedInt32(HexBuffer buffer, ref HexPosition position) {
			uint val = 0;
			int bits = 0;
			for (int i = 0; i < 5; i++) {
				byte b = buffer.ReadByte(position++);
				val |= (uint)(b & 0x7F) << bits;
				if ((b & 0x80) == 0)
					return (int)val;
				bits += 7;
			}
			return null;
		}

		// #US / #Blob
		public static uint ReadCompressedUInt32(HexBuffer buffer, ref HexPosition position) {
			byte b = buffer.ReadByte(position++);
			if ((b & 0x80) == 0)
				return b;

			if ((b & 0xC0) == 0x80)
				return (uint)((b & 0x3F) << 8) | buffer.ReadByte(position++);

			// The encoding 111x isn't allowed but the CLR sometimes doesn't verify this
			// and just assumes it's 110x. Don't fail if it's 111x, just assume it's 110x.

			return (uint)(((b & 0x1F) << 24) | (buffer.ReadByte(position++) << 16) |
					(buffer.ReadByte(position++) << 8) | buffer.ReadByte(position++));
		}
	}
}
