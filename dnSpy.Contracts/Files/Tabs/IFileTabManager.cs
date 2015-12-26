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

using System;
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
		/// Same as <see cref="SortedTabs"/> except that visible tabs are returned first
		/// </summary>
		IEnumerable<IFileTab> VisibleFirstTabs { get; }

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
		/// Forces a refresh of the selected tabs
		/// </summary>
		/// <param name="tabs">Tabs to refresh</param>
		void Refresh(IEnumerable<IFileTab> tabs);

		/// <summary>
		/// Refreshes all tabs that contain nodes of type <typeparamref name="T"/>
		/// </summary>
		/// <typeparam name="T">Node type</typeparam>
		void Refresh<T>() where T : IFileTreeNodeData;

		/// <summary>
		/// Refreshes all tabs that contain certain nodes
		/// </summary>
		/// <param name="pred">Returns true if the node should be included</param>
		void Refresh(Predicate<IFileTreeNodeData> pred);

		/// <summary>
		/// Refreshes all tabs that use <paramref name="file"/>
		/// </summary>
		/// <param name="file">Modified file</param>
		void RefreshModifiedFile(IDnSpyFile file);

		/// <summary>
		/// Raised when <see cref="RefreshModifiedFile(IDnSpyFile)"/> gets called
		/// </summary>
		event EventHandler<FileModifiedEventArgs> FileModified;

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

		/// <summary>
		/// Closes all tabs
		/// </summary>
		void CloseAll();

		/// <summary>
		/// Follows the reference in the active tab or a new tab
		/// </summary>
		/// <param name="ref">Reference</param>
		/// <param name="newTab">true to open a new tab</param>
		/// <param name="onShown">Called after the content has been shown. Can be null.</param>
		void FollowReference(object @ref, bool newTab = false, Action<ShowTabContentEventArgs> onShown = null);

		/// <summary>
		/// Creates a new <see cref="IFileTabContent"/> instance. Returns null if it couldn't be created
		/// </summary>
		/// <param name="nodes">Nodes</param>
		/// <returns></returns>
		IFileTabContent TryCreateContent(IFileTreeNodeData[] nodes);
	}
}
