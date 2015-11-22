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

namespace dnSpy.Contracts.Files.Tabs {
	/// <summary>
	/// Manages the file tabs and treeview
	/// </summary>
	public interface IFileTabManager {
		/// <summary>
		/// Gets the active tab or null if none, see also <see cref="GetOrCreateActiveTab()"/>
		/// </summary>
		IFileTab ActiveTab { get; }

		/// <summary>
		/// Gets the active tab or creates a new one if <see cref="ActiveTab"/> is null
		/// </summary>
		/// <returns></returns>
		IFileTab GetOrCreateActiveTab();

		/// <summary>
		/// Opens a new empty tab and sets it as the active tab (<see cref="ActiveTab"/>)
		/// </summary>
		/// <returns></returns>
		IFileTab OpenEmptyTab();
	}
}
