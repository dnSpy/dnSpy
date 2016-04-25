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

namespace dnSpy.Contracts.Settings {
	/// <summary>
	/// Creates <see cref="ISettingsSection"/>s
	/// </summary>
	public interface ISettingsSectionCreator {
		/// <summary>
		/// Gets all sections
		/// </summary>
		ISettingsSection[] Sections { get; }

		/// <summary>
		/// Creates a new section, even if a section with the same name already exists
		/// </summary>
		/// <param name="name">Name of section</param>
		/// <returns></returns>
		ISettingsSection CreateSection(string name);

		/// <summary>
		/// Gets an existing section or creates a new one if one doesn't exist
		/// </summary>
		/// <param name="name">Name of section</param>
		/// <returns></returns>
		ISettingsSection GetOrCreateSection(string name);

		/// <summary>
		/// Removes a section
		/// </summary>
		/// <param name="name">Name of section</param>
		void RemoveSection(string name);

		/// <summary>
		/// Removes a section
		/// </summary>
		/// <param name="section">Section</param>
		void RemoveSection(ISettingsSection section);

		/// <summary>
		/// Gets all sections
		/// </summary>
		/// <param name="name">Name of section</param>
		/// <returns></returns>
		ISettingsSection[] SectionsWithName(string name);

		/// <summary>
		/// Gets a section or null if it doesn't exist
		/// </summary>
		/// <param name="name">Name of section</param>
		/// <returns></returns>
		ISettingsSection TryGetSection(string name);
	}
}
