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
using System.Windows.Media;

namespace dnSpy.Contracts.Hex.Editor {
	/// <summary>
	/// Space reservation agent
	/// </summary>
	public abstract class HexSpaceReservationAgent {
		/// <summary>
		/// Constructor
		/// </summary>
		protected HexSpaceReservationAgent() { }

		/// <summary>
		/// true if its adornment has keyboard focus
		/// </summary>
		public abstract bool HasFocus { get; }

		/// <summary>
		/// true if the mouse is over its adornment
		/// </summary>
		public abstract bool IsMouseOver { get; }

		/// <summary>
		/// Raised after its adornment got keyboard focus
		/// </summary>
		public abstract event EventHandler GotFocus;

		/// <summary>
		/// Raised after its adornment lost keyboard focus
		/// </summary>
		public abstract event EventHandler LostFocus;

		/// <summary>
		/// Called to hide the adornment
		/// </summary>
		public abstract void Hide();

		/// <summary>
		/// Positions and displays the adornment. Returns null if it should be removed.
		/// </summary>
		/// <param name="reservedSpace">Reserved space</param>
		/// <returns></returns>
		public abstract Geometry PositionAndDisplay(Geometry reservedSpace);
	}
}
