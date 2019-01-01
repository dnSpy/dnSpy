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

namespace dnSpy.Contracts.Hex.Editor {
	/// <summary>
	/// Maps between byte positions and fractions of the total vertical extent of a <see cref="HexView"/>
	/// </summary>
	public abstract class HexVerticalFractionMap {
		/// <summary>
		/// Constructor
		/// </summary>
		protected HexVerticalFractionMap() { }

		/// <summary>
		/// Gets the hex view
		/// </summary>
		public abstract HexView HexView { get; }

		/// <summary>
		/// Gets the fraction of the vertical extent of the view that corresponds to the specified buffer position
		/// </summary>
		/// <param name="bufferPosition">Buffer position</param>
		/// <returns></returns>
		public abstract double GetFractionAtBufferPosition(HexBufferPoint bufferPosition);

		/// <summary>
		/// Gets the buffer position that corresponds to a fraction of the vertical extent of the view, if it exists
		/// </summary>
		/// <param name="fraction">The fraction of the vertical extent of the view</param>
		/// <returns></returns>
		public abstract HexBufferPoint GetBufferPositionAtFraction(double fraction);

		/// <summary>
		/// Raised when the mapping is changed
		/// </summary>
		public abstract event EventHandler MappingChanged;
	}
}
