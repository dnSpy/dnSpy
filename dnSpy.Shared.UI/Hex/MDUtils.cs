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

namespace dnSpy.Shared.UI.Hex {
	static class MDUtils {
		public static int GetCompressedUInt32Length(uint value) {
			if (value <= 0x7F)
				return 1;
			if (value <= 0x3FFF)
				return 2;
			if (value <= 0x1FFFFFFF)
				return 4;
			return -1;
		}

		public static void WriteCompressedUInt32(byte[] data, int index, uint value) {
			if (value <= 0x7F)
				data[index + 0] = (byte)value;
			else if (value <= 0x3FFF) {
				data[index + 0] = (byte)((value >> 8) | 0x80);
				data[index + 1] = (byte)value;
			}
			else if (value <= 0x1FFFFFFF) {
				data[index + 0] = (byte)((value >> 24) | 0xC0);
				data[index + 1] = (byte)(value >> 16);
				data[index + 2] = (byte)(value >> 8);
				data[index + 3] = (byte)value;
			}
			else
				throw new InvalidOperationException();
		}
	}
}
