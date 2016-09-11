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

using System.Collections.ObjectModel;

namespace dnSpy.Contracts.Language.Intellisense {
	/// <summary>
	/// Intellisense stack
	/// </summary>
	interface IIntellisenseSessionStack {
		/// <summary>
		/// Adds a new session to the top of the stack
		/// </summary>
		/// <param name="session">Session to add to the top of the stack</param>
		void PushSession(IIntellisenseSession session);

		/// <summary>
		/// Removes and returns the top session
		/// </summary>
		/// <returns></returns>
		IIntellisenseSession PopSession();

		/// <summary>
		/// Moves an existing session in the stack to the top of the stack
		/// </summary>
		/// <param name="session">Session to move to the top of the stack</param>
		void MoveSessionToTop(IIntellisenseSession session);

		/// <summary>
		/// Gets a read only observable collection of all available sessions
		/// </summary>
		ReadOnlyObservableCollection<IIntellisenseSession> Sessions { get; }

		/// <summary>
		/// Gets the top session or null if none
		/// </summary>
		IIntellisenseSession TopSession { get; }

		/// <summary>
		/// Collapses all sessions or dismisses them if they can't be collapsed
		/// </summary>
		void CollapseAllSessions();
	}
}
