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

using System;

namespace dnSpy.Contracts.Settings.Dialog {
	/// <summary>
	/// Stores info on what needs to be refreshed when the settings dialog box has closed
	/// </summary>
	public interface IAppRefreshSettings {
		/// <summary>
		/// Adds something that must be refreshed, eg. <see cref="AppSettingsConstants.REFRESH_LANGUAGE_SHOWMEMBER"/>
		/// </summary>
		/// <param name="guid">Guid, eg. <see cref="AppSettingsConstants.REFRESH_LANGUAGE_SHOWMEMBER"/></param>
		/// <param name="value">Value or null</param>
		void Add(Guid guid, object? value = null);

		/// <summary>
		/// Returns true if <paramref name="guid"/> has been added by <see cref="Add(Guid, object)"/>
		/// </summary>
		/// <param name="guid">Guid, eg. <see cref="AppSettingsConstants.REFRESH_LANGUAGE_SHOWMEMBER"/></param>
		/// <returns></returns>
		bool Has(Guid guid);

		/// <summary>
		/// Gets the value or null if it's not present
		/// </summary>
		/// <param name="guid">Guid, eg. <see cref="AppSettingsConstants.REFRESH_LANGUAGE_SHOWMEMBER"/></param>
		/// <returns></returns>
		object? GetValue(Guid guid);
	}
}
