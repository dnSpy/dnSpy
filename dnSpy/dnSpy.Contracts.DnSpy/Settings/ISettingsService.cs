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

namespace dnSpy.Contracts.Settings {
	/// <summary>
	/// Adds/removes settings
	/// </summary>
	public interface ISettingsService {
		/// <summary>
		/// Gets all sections
		/// </summary>
		ISettingsSection[] Sections { get; }

		/// <summary>
		/// Gets an existing section or creates a new one if one doesn't exist
		/// </summary>
		/// <param name="guid">Guid of section</param>
		/// <returns></returns>
		ISettingsSection GetOrCreateSection(Guid guid);

		/// <summary>
		/// Removes a section
		/// </summary>
		/// <param name="guid">Guid of section</param>
		void RemoveSection(Guid guid);

		/// <summary>
		/// Removes a section
		/// </summary>
		/// <param name="section">Section</param>
		void RemoveSection(ISettingsSection section);

		/// <summary>
		/// Removes an existing section and re-creates it
		/// </summary>
		/// <param name="guid">Guid of section</param>
		/// <returns></returns>
		ISettingsSection RecreateSection(Guid guid);
	}
}
