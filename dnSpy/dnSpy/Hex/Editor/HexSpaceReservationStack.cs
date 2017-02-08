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

using System;
using dnSpy.Contracts.Hex.Editor;

namespace dnSpy.Hex.Editor {
	/// <summary>
	/// Space reservation stack, used by Intellisense code to make sure the popups don't
	/// cover one another.
	/// </summary>
	abstract class HexSpaceReservationStack {
		/// <summary>
		/// Constructor
		/// </summary>
		protected HexSpaceReservationStack() { }

		/// <summary>
		/// true if the mouse is over any <see cref="HexSpaceReservationAgent"/>
		/// </summary>
		public abstract bool IsMouseOver { get; }

		/// <summary>
		/// true if any of the <see cref="HexSpaceReservationAgent"/>s has focus
		/// </summary>
		public abstract bool HasAggregateFocus { get; }

		/// <summary>
		/// Raised when it gets aggregate focus
		/// </summary>
		public abstract event EventHandler GotAggregateFocus;

		/// <summary>
		/// Raised when it loses aggregate focus
		/// </summary>
		public abstract event EventHandler LostAggregateFocus;

		/// <summary>
		/// Refreshes the space reservation stack which will force the agents to reposition themselves
		/// </summary>
		public abstract void Refresh();

		/// <summary>
		/// Creates a new or returns an existing <see cref="HexSpaceReservationManager"/> instance
		/// </summary>
		/// <param name="name">Name of the <see cref="HexSpaceReservationManager"/></param>
		/// <returns></returns>
		public abstract HexSpaceReservationManager GetSpaceReservationManager(string name);
	}
}
