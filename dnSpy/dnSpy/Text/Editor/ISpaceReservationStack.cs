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

using System;
using Microsoft.VisualStudio.Text.Editor;

namespace dnSpy.Text.Editor {
	/// <summary>
	/// Space reservation stack, used by Intellisense code to make sure the popups don't
	/// cover one another.
	/// </summary>
	interface ISpaceReservationStack {
		/// <summary>
		/// true if the mouse is over any <see cref="ISpaceReservationAgent"/>
		/// </summary>
		bool IsMouseOver { get; }

		/// <summary>
		/// true if any of the <see cref="ISpaceReservationAgent"/>s has focus
		/// </summary>
		bool HasAggregateFocus { get; }

		/// <summary>
		/// Raised when it gets aggregate focus
		/// </summary>
		event EventHandler GotAggregateFocus;

		/// <summary>
		/// Raised when it loses aggregate focus
		/// </summary>
		event EventHandler LostAggregateFocus;

		/// <summary>
		/// Refreshes the space reservation stack which will force the agents to reposition themselves
		/// </summary>
		void Refresh();

		/// <summary>
		/// Creates a new or returns an existing <see cref="ISpaceReservationManager"/> instance
		/// </summary>
		/// <param name="name">Name of the <see cref="ISpaceReservationManager"/>, eg. <see cref="Microsoft.VisualStudio.Language.Intellisense.IntellisenseSpaceReservationManagerNames.CompletionSpaceReservationManagerName"/></param>
		/// <returns></returns>
		ISpaceReservationManager GetSpaceReservationManager(string name);
	}
}
