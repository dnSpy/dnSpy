/*
    Copyright (C) 2014-2015 de4dot@gmail.com

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

namespace dnSpy.Contracts.Settings.Dialog {
	/// <summary>
	/// A tab shown in the main settings dialog box
	/// </summary>
	public interface IAppSettingsTab {
		/// <summary>
		/// Gets the title
		/// </summary>
		string Title { get; }

		/// <summary>
		/// Gets the UI object
		/// </summary>
		object UIObject { get; }

		/// <summary>
		/// Called when the dialog box has closed
		/// </summary>
		/// <param name="saveSettings">true to save the settings, false to cancel any changes</param>
		/// <param name="appRefreshSettings">Used if <paramref name="saveSettings"/> is true. Add
		/// anything that needs to be refreshed, eg. re-decompile code</param>
		void OnClosed(bool saveSettings, IAppRefreshSettings appRefreshSettings);
	}
}
