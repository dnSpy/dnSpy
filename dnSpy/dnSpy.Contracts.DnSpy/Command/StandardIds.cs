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

namespace dnSpy.Contracts.Command {
	/// <summary>
	/// Standard command IDs (group = <see cref="CommandConstants.StandardGroup"/>)
	/// </summary>
	public enum StandardIds {
		/// <summary>
		/// Unknown command, if no other command is found
		/// </summary>
		Unknown,

		/// <summary>
		/// Copy
		/// </summary>
		Copy,

		/// <summary>
		/// Cut
		/// </summary>
		Cut,

		/// <summary>
		/// Paste
		/// </summary>
		Paste,

		/// <summary>
		/// Undo
		/// </summary>
		Undo,

		/// <summary>
		/// Redo
		/// </summary>
		Redo,

		/// <summary>
		/// Find (eg. Ctrl+F)
		/// </summary>
		Find,

		/// <summary>
		/// Replace (eg. Ctrl+H)
		/// </summary>
		Replace,

		/// <summary>
		/// Incremental search (eg. Ctrl+I)
		/// </summary>
		IncrementalSearch,

		/// <summary>
		/// Backward incremental search (eg. Ctrl+Shift+I)
		/// </summary>
		IncrementalSearchBackward,

		/// <summary>
		/// Find next (eg. F3)
		/// </summary>
		FindNext,

		/// <summary>
		/// Find previous (eg. Shift+F3)
		/// </summary>
		FindPrevious,

		/// <summary>
		/// Find next selected (eg. Ctrl+F3)
		/// </summary>
		FindNextSelected,

		/// <summary>
		/// Find previous selected (eg. Ctrl+Shift+F3)
		/// </summary>
		FindPreviousSelected,
	}
}
