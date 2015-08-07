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
		/// Size of stream. This could be <see cref="ulong.MaxValue"/>, one byte too small, if the
		/// whole 64-bit address space is used. See also <see cref="EndOffset"/>.
		/// </summary>
		ulong Size { get; }

		/// <summary>
		/// First valid offset
		/// </summary>
		ulong StartOffset { get; }

		/// <summary>
		/// Last valid offset. See also <see cref="Size"/>
		/// </summary>
		ulong EndOffset { get; }

		/// <summary>
		/// Reads a byte. Returns -1 if <paramref name="offset"/> is invalid or if the memory isn't
		/// readable.
		/// </summary>
		/// <param name="offset">Offset of byte</param>
		/// <returns></returns>
		int ReadByte(ulong offset);

		/// <summary>
		/// Reads bytes
		/// </summary>
		/// <param name="offset">Offset</param>
		/// <param name="array">Array</param>
		/// <param name="index">Index in <paramref name="array"/></param>
		/// <param name="count">Size of data to read</param>
		void Read(ulong offset, byte[] array, long index, int count);

		/// <summary>
		/// Writes a byte
		/// </summary>
		/// <param name="offset">Offset</param>
		/// <param name="b">Value</param>
		void Write(ulong offset, byte b);

		/// <summary>
		/// Writes bytes
		/// </summary>
		/// <param name="offset">Offset</param>
		/// <param name="array">Data to write</param>
		/// <param name="index">Index in <paramref name="array"/></param>
		/// <param name="count">Number of bytes to write</param>
		void Write(ulong offset, byte[] array, long index, int count);
	}
}
