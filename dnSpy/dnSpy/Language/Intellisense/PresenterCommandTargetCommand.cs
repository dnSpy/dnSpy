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

namespace dnSpy.Language.Intellisense {
	enum PresenterCommandTargetCommand {
		/// <summary>
		/// Up (<see cref="dnSpy.Contracts.Command.TextEditorIds.UP"/>)
		/// </summary>
		Up,

		/// <summary>
		/// Down (<see cref="dnSpy.Contracts.Command.TextEditorIds.DOWN"/>)
		/// </summary>
		Down,

		/// <summary>
		/// Page Up (<see cref="dnSpy.Contracts.Command.TextEditorIds.PAGEUP"/>)
		/// </summary>
		PageUp,

		/// <summary>
		/// Page Down (<see cref="dnSpy.Contracts.Command.TextEditorIds.PAGEDN"/>)
		/// </summary>
		PageDown,

		/// <summary>
		/// Home (<see cref="dnSpy.Contracts.Command.TextEditorIds.BOL"/>)
		/// </summary>
		Home,

		/// <summary>
		/// End (<see cref="dnSpy.Contracts.Command.TextEditorIds.EOL"/>)
		/// </summary>
		End,

		/// <summary>
		/// Top Line (<see cref="dnSpy.Contracts.Command.TextEditorIds.TOPLINE"/>)
		/// </summary>
		TopLine,

		/// <summary>
		/// Bottom Line (<see cref="dnSpy.Contracts.Command.TextEditorIds.BOTTOMLINE"/>)
		/// </summary>
		BottomLine,

		/// <summary>
		/// Escape (<see cref="dnSpy.Contracts.Command.TextEditorIds.CANCEL"/>)
		/// </summary>
		Escape,

		/// <summary>
		/// Enter (<see cref="dnSpy.Contracts.Command.TextEditorIds.RETURN"/>)
		/// </summary>
		Enter,

		/// <summary>
		/// Increase filter level
		/// </summary>
		IncreaseFilterLevel,

		/// <summary>
		/// Decrease filter level
		/// </summary>
		DecreaseFilterLevel,
	}
}
