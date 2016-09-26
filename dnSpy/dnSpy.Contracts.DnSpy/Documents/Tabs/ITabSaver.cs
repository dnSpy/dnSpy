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

namespace dnSpy.Contracts.Documents.Tabs {
	/// <summary>
	/// Saves tabs
	/// </summary>
	public interface ITabSaver {
		/// <summary>
		/// true if it can be saved
		/// </summary>
		bool CanSave { get; }

		/// <summary>
		/// Gets the menu header, eg. "_Save..." or null to use the default "_Save..." string
		/// </summary>
		string MenuHeader { get; }

		/// <summary>
		/// Saves the tab
		/// </summary>
		void Save();
	}
}
