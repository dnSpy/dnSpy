/*
    Copyright (C) 2014-2015 de4dot@gmail.com

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

namespace dnSpy.HexEditor {
	public interface IHexStream {
		/// <summary>
		/// Size of stream
		/// </summary>
		ulong Size { get; }

		/// <summary>
		/// Reads a byte. Returns -1 if <paramref name="offset"/> is invalid or if the memory isn't
		/// readable.
		/// </summary>
		/// <param name="offset">Offset of byte</param>
		/// <returns></returns>
		int ReadByte(ulong offset);
	}
}
