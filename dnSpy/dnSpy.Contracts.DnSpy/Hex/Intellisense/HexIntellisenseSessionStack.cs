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

namespace dnSpy.Contracts.Hex.Intellisense {
	/// <summary>
	/// Intellisense session stack
	/// </summary>
	public abstract class HexIntellisenseSessionStack {
		/// <summary>
		/// Constructor
		/// </summary>
		protected HexIntellisenseSessionStack() { }

		/// <summary>
		/// Adds a session to the top of the stack
		/// </summary>
		/// <param name="session">Session</param>
		public abstract void PushSession(HexIntellisenseSession session);

		/// <summary>
		/// Removes the session from the top of the stack
		/// </summary>
		/// <returns></returns>
		public abstract HexIntellisenseSession PopSession();

		/// <summary>
		/// Moves a session to the top of the stack
		/// </summary>
		/// <param name="session">Session</param>
		public abstract void MoveSessionToTop(HexIntellisenseSession session);

		/// <summary>
		/// Gets all sessions
		/// </summary>
		public abstract ReadOnlyObservableCollection<HexIntellisenseSession> Sessions { get; }

		/// <summary>
		/// Gets the session at the top of the stack or null if none
		/// </summary>
		public abstract HexIntellisenseSession TopSession { get; }

		/// <summary>
		/// Collapses all sessions
		/// </summary>
		public abstract void CollapseAllSessions();
	}
}
