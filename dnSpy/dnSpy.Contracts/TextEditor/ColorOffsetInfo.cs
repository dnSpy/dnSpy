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

using System.Diagnostics;

namespace dnSpy.Contracts.TextEditor {
	/// <summary>
	/// Color info
	/// </summary>
	public struct ColorOffsetInfo {
		/// <summary>
		/// Offset of text
		/// </summary>
		public int Offset { get; }

		/// <summary>
		/// Length of text
		/// </summary>
		public int Length { get; }

		/// <summary>
		/// Kind
		/// </summary>
		public OutputColor Color { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="offset">Offset</param>
		/// <param name="length">Length</param>
		/// <param name="color">Color</param>
		public ColorOffsetInfo(int offset, int length, OutputColor color) {
			Debug.Assert(offset + length >= offset && length >= 0);
			Offset = offset;
			Length = length;
			Color = color;
		}
	}
}
