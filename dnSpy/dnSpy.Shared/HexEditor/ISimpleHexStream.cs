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

namespace dnSpy.Shared.HexEditor {
	public interface ISimpleHexStream {
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
		/// Gets the page size of the underlying data store or 0 if it's unknown. Eg. if it's
		/// memory in some process, <see cref="System.Environment.SystemPageSize"/> can be returned
		/// here. The returned value must be 0 or a power of 2.
		/// </summary>
		ulong PageSize { get; }

		/// <summary>
		/// Reads bytes. Returns number of bytes read.
		/// </summary>
		/// <param name="offset">Offset</param>
		/// <param name="array">Array</param>
		/// <param name="index">Index in <paramref name="array"/></param>
		/// <param name="count">Size of data to read</param>
		/// <returns></returns>
		int Read(ulong offset, byte[] array, long index, int count);

		/// <summary>
		/// Writes bytes. Returns number of bytes written.
		/// </summary>
		/// <param name="offset">Offset</param>
		/// <param name="array">Data to write</param>
		/// <param name="index">Index in <paramref name="array"/></param>
		/// <param name="count">Number of bytes to write</param>
		/// <returns></returns>
		int Write(ulong offset, byte[] array, long index, int count);
	}
}
