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

using System;
using dnSpy.Contracts.Hex;

namespace dnSpy.AsmEditor.Hex {
	static class HexBufferWriterHelper {
		public static void Write(IHexBufferService hexBufferService, string filename, HexPosition position, byte[] data) {
			if (string.IsNullOrEmpty(filename))
				throw new ArgumentException();
			var buffer = hexBufferService.GetOrCreate(filename);
			if (buffer is null)
				return;
			Write(buffer, position, data);
		}

		public static void Write(HexBuffer buffer, HexPosition position, byte[] data) {
			if (buffer is null)
				throw new ArgumentNullException(nameof(buffer));
			if (data is null || data.Length == 0)
				return;
			buffer.Replace(position, data);
		}
	}
}
