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

namespace dndbg.DotNet {
	/// <summary>
	/// Reads memory from some process
	/// </summary>
	public interface IProcessReader {
		/// <summary>
		/// Reads bytes from the process and returns number of bytes read. This can be less than
		/// <paramref name="count"/> if not all memory is readable
		/// </summary>
		/// <param name="address">Address</param>
		/// <param name="data">Destination buffer</param>
		/// <param name="index">Index in <paramref name="data"/></param>
		/// <param name="count">Number of bytes to read</param>
		/// <returns></returns>
		int ReadBytes(ulong address, byte[] data, int index, int count);
	}
}
