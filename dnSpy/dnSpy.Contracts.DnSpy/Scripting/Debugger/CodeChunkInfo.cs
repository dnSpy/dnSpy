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

namespace dnSpy.Contracts.Scripting.Debugger {
	/// <summary>
	/// Code chunk info
	/// </summary>
	public struct CodeChunkInfo {
		/// <summary>
		/// Start address
		/// </summary>
		public readonly ulong StartAddr;

		/// <summary>
		/// Length
		/// </summary>
		public readonly uint Length;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="addr">Address</param>
		/// <param name="length">Length</param>
		public CodeChunkInfo(ulong addr, uint length) {
			StartAddr = addr;
			Length = length;
		}

		/// <summary>
		/// ToString()
		/// </summary>
		/// <returns></returns>
		public override string ToString() => string.Format("{0:X8}-{1:X8} (0x{2:X})", StartAddr, Length == 0 ? StartAddr : StartAddr + Length - 1, Length);
	}
}
