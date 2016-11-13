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

using dnSpy.Contracts.Hex.Editor;

namespace dnSpy.Contracts.Hex {
	/// <summary>
	/// Creates <see cref="HexBufferLine"/>s
	/// </summary>
	public abstract class HexBufferLineProvider {
		/// <summary>
		/// Constructor
		/// </summary>
		protected HexBufferLineProvider() { }

		/// <summary>
		/// Number of lines (0-2^64)
		/// </summary>
		public abstract HexPosition LineCount { get; }

		/// <summary>
		/// Creates a line
		/// </summary>
		/// <param name="position">Position (0-2^64-1)</param>
		/// <returns></returns>
		public abstract HexBufferLine CreateLine(HexPosition position);

		/// <summary>
		/// Converts a physical (stream) position to a logical position
		/// </summary>
		/// <param name="physicalPosition">Physical (stream) position</param>
		/// <returns></returns>
		public abstract HexPosition ToLogicalPosition(HexPosition physicalPosition);

		/// <summary>
		/// Converts a logical position to a physical (stream) position
		/// </summary>
		/// <param name="logicalPosition">Logical position</param>
		/// <returns></returns>
		public abstract HexPosition ToPhysicalPosition(HexPosition logicalPosition);
	}
}
