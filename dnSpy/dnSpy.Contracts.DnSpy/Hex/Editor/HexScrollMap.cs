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

namespace dnSpy.Contracts.Hex.Editor {
	/// <summary>
	/// Hex view scroll map
	/// </summary>
	public abstract class HexScrollMap : HexVerticalFractionMap {
		/// <summary>
		/// Constructor
		/// </summary>
		protected HexScrollMap() { }

		/// <summary>
		/// Gets the scrollmap coordinates of a buffer position
		/// </summary>
		/// <param name="bufferPosition">Buffer position</param>
		/// <returns></returns>
		public abstract double GetCoordinateAtBufferPosition(HexBufferPoint bufferPosition);

		/// <summary>
		/// Gets the buffer position that corresponds to a scrollmap coordinate
		/// </summary>
		/// <param name="coordinate">Scrollbar coordinate</param>
		/// <returns></returns>
		public abstract HexBufferPoint GetBufferPositionAtCoordinate(double coordinate);

		/// <summary>
		/// Gets the scrollmap coordinate of the start of the buffer
		/// </summary>
		public abstract double Start { get; }

		/// <summary>
		/// Gets the scrollmap coordinate of the end of the buffer
		/// </summary>
		public abstract double End { get; }

		/// <summary>
		/// Gets the size of the text visible in the view (in scrollmap coordinates)
		/// </summary>
		public abstract double ThumbSize { get; }
	}
}
