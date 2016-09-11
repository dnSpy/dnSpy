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

using dnSpy.Contracts.Command;

namespace dnSpy.Contracts.Language.Intellisense {
	/// <summary>
	/// Keyboard commands passed to an <see cref="IIntellisensePresenter"/>
	/// </summary>
	enum IntellisenseKeyboardCommand {
		/// <summary>
		/// Up (<see cref="TextEditorIds.UP"/>)
		/// </summary>
		Up,

		/// <summary>
		/// Down (<see cref="TextEditorIds.DOWN"/>)
		/// </summary>
		Down,

		/// <summary>
		/// Page Up (<see cref="TextEditorIds.PAGEUP"/>)
		/// </summary>
		PageUp,

		/// <summary>
		/// Page Down (<see cref="TextEditorIds.PAGEDN"/>)
		/// </summary>
		PageDown,

		/// <summary>
		/// Home (<see cref="TextEditorIds.BOL"/>)
		/// </summary>
		Home,

		/// <summary>
		/// End (<see cref="TextEditorIds.EOL"/>)
		/// </summary>
		End,

		/// <summary>
		/// Top Line (<see cref="TextEditorIds.TOPLINE"/>)
		/// </summary>
		TopLine,

		/// <summary>
		/// Bottom Line (<see cref="TextEditorIds.BOTTOMLINE"/>)
		/// </summary>
		BottomLine,

		/// <summary>
		/// Escape (<see cref="TextEditorIds.CANCEL"/>)
		/// </summary>
		Escape,

		/// <summary>
		/// Enter (<see cref="TextEditorIds.RETURN"/>)
		/// </summary>
		Enter,

		/// <summary>
		/// Increase filter level (<see cref="TextEditorIds.INCREASEFILTER"/>)
		/// </summary>
		IncreaseFilterLevel,

		/// <summary>
		/// Decrease filter level (<see cref="TextEditorIds.DECREASEFILTER"/>)
		/// </summary>
		DecreaseFilterLevel,
	}
}
