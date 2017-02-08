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

using dnSpy.Contracts.Hex;
using dnSpy.Contracts.Hex.Files;
using dnSpy.Contracts.Hex.Files.PE;

namespace dnSpy.Hex.Files.PE {
	static class DataDirectoryDataUtils {
		public static HexSpan? TryGetSpan(HexBufferFile file, DataDirectoryData data, HexPosition position) {
			if (!data.VirtualAddress.Data.Span.Span.Contains(position))
				return null;
			uint rva = data.VirtualAddress.Data.ReadValue();
			if (rva == 0)
				return null;
			uint size = data.Size.Data.ReadValue();
			if (size == 0)
				return null;
			var peHeaders = file.GetHeaders<PeHeaders>();
			if (peHeaders == null)
				return null;
			var pos = peHeaders.RvaToBufferPosition(rva);
			if (pos + size > file.Span.End)
				return new HexSpan(pos, 0);
			return new HexSpan(pos, size);
		}
	}
}
