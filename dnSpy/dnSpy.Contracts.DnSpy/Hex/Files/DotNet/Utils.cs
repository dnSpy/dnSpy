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
	}
}
