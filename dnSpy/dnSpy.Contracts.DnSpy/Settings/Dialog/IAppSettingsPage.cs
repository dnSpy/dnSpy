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
using dnSpy.Contracts.Images;

namespace dnSpy.Contracts.Settings.Dialog {
	/// <summary>
	/// Content shown in the application settings
	/// </summary>
	public interface IAppSettingsPage {
		/// <summary>
		/// Parent <see cref="System.Guid"/> or <see cref="System.Guid.Empty"/> if the root element is the parent
		/// </summary>
		Guid ParentGuid { get; }

		/// <summary>
		/// Gets the <see cref="System.Guid"/>
		/// </summary>
		Guid Guid { get; }

		/// <summary>
		/// Gets the order, eg. <see cref="AppSettingsConstants.ORDER_DECOMPILER"/>
		/// </summary>
		double Order { get; }

		/// <summary>
		/// Gets the title shown in the UI
		/// </summary>
		string Title { get; }

		/// <summary>
		/// Gets the icon shown in the UI (eg. <see cref="DsImages.Assembly"/>) or <see cref="ImageReference.None"/>
		/// </summary>
		ImageReference Icon { get; }

		/// <summary>
		/// Gets the UI object
		/// </summary>
		object UIObject { get; }

		/// <summary>
		/// Called when the dialog box has been closed
		/// </summary>
		/// <param name="saveSettings">true to save the settings, false to cancel any changes</param>
		/// <param name="appRefreshSettings">Used if <paramref name="saveSettings"/> is true. Add
		/// anything that needs to be refreshed, eg. re-decompile code</param>
		void OnClosed(bool saveSettings, IAppRefreshSettings appRefreshSettings);
	}
}
