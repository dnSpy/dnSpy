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

using System.Collections.Generic;
using dnSpy.Contracts.Files.TreeView;
using dnSpy.Contracts.Tabs;

namespace dnSpy.Contracts.Files.Tabs {
	/// <summary>
	/// Manages the file tabs and treeview
	/// </summary>
	public interface IFileTabManager {
		/// <summary>
		/// Gets the settings
		/// </summary>
		IFileTabManagerSettings Settings { get; }

		/// <summary>
		/// Gets the <see cref="IFileTreeView"/> instance
		/// </summary>
		IFileTreeView FileTreeView { get; }

		/// <summary>
		/// Gets the <see cref="ITabGroupManager"/> instance
		/// </summary>
		ITabGroupManager TabGroupManager { get; }

		/// <summary>
		/// Gets all <see cref="IFileTab"/> instances
		/// </summary>
		IEnumerable<IFileTab> SortedTabs { get; }

		/// <summary>
		/// Gets the active tab or null if none, see also <see cref="GetOrCreateActiveTab()"/>
		/// </summary>
		IFileTab ActiveTab { get; set; }

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

		/// <summary>
		/// Gives <paramref name="tab"/> keyboard focus
		/// </summary>
		/// <param name="tab">Tab</param>
		void SetFocus(IFileTab tab);

		/// <summary>
		/// Refreshes those tabs that need to be refreshed
		/// </summary>
		void CheckRefresh();

		/// <summary>
		/// Refreshes those tabs that need to be refreshed
		/// </summary>
		/// <param name="tabs">Tabs to check</param>
		void CheckRefresh(IEnumerable<IFileTab> tabs);

		/// <summary>
		/// Returns true if <paramref name="tabGroup"/> is owned by this instance
		/// </summary>
		/// <param name="tabGroup">Tab group</param>
		/// <returns></returns>
		bool Owns(ITabGroup tabGroup);

		/// <summary>
		/// Closes the tab
		/// </summary>
		/// <param name="tab"></param>
		void Close(IFileTab tab);

		/// <summary>
		/// Tries to get the <see cref="IFileTab"/>
		/// </summary>
		/// <param name="content">Tab content</param>
		/// <returns></returns>
		IFileTab TryGetFileTab(ITabContent content);
	}
}
