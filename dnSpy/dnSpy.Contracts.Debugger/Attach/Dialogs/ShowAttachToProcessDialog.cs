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

namespace dnSpy.Contracts.Debugger.Attach.Dialogs {
	/// <summary>
	/// Shows the Attach to Process dialog box
	/// </summary>
	public abstract class ShowAttachToProcessDialog {
		/// <summary>
		/// Shows the dialog box and returns the selected processes or an empty list
		/// </summary>
		/// <param name="options">Options or null to use the default options</param>
		/// <returns></returns>
		public abstract AttachToProgramOptions[] Show(ShowAttachToProcessDialogOptions options = null);

		/// <summary>
		/// Shows the dialog box and attaches to the selected processes
		/// </summary>
		/// <param name="options">Options or null to use the default options</param>
		public abstract void Attach(ShowAttachToProcessDialogOptions options = null);
	}
}
