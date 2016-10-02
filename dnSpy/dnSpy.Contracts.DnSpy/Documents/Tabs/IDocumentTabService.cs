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
using System.Collections.Generic;
using dnSpy.Contracts.Documents.TreeView;
using dnSpy.Contracts.Tabs;

namespace dnSpy.Contracts.Documents.Tabs {
	/// <summary>
	/// Manages the document tabs and treeview
	/// </summary>
	public interface IDocumentTabService {
		/// <summary>
		/// Gets the <see cref="IDocumentTreeView"/> instance
		/// </summary>
		IDocumentTreeView DocumentTreeView { get; }

		/// <summary>
		/// Gets the <see cref="ITabGroupService"/> instance
		/// </summary>
		ITabGroupService TabGroupService { get; }

		/// <summary>
		/// Gets all <see cref="IDocumentTab"/> instances
		/// </summary>
		IEnumerable<IDocumentTab> SortedTabs { get; }

		/// <summary>
		/// Same as <see cref="SortedTabs"/> except that visible tabs are returned first
		/// </summary>
		IEnumerable<IDocumentTab> VisibleFirstTabs { get; }

		/// <summary>
		/// Gets the active tab or null if none, see also <see cref="GetOrCreateActiveTab()"/>
		/// </summary>
		IDocumentTab ActiveTab { get; set; }

		/// <summary>
		/// Gets the active tab or creates a new one if <see cref="ActiveTab"/> is null
		/// </summary>
		/// <returns></returns>
		IDocumentTab GetOrCreateActiveTab();

		/// <summary>
		/// Opens a new empty tab and sets it as the active tab (<see cref="ActiveTab"/>)
		/// </summary>
		/// <returns></returns>
		IDocumentTab OpenEmptyTab();

		/// <summary>
		/// Gives <paramref name="tab"/> keyboard focus
		/// </summary>
		/// <param name="tab">Tab</param>
		void SetFocus(IDocumentTab tab);

		/// <summary>
		/// Forces a refresh of the selected tabs
		/// </summary>
		/// <param name="tabs">Tabs to refresh</param>
		void Refresh(IEnumerable<IDocumentTab> tabs);

		/// <summary>
		/// Refreshes all tabs that contain nodes of type <typeparamref name="T"/>
		/// </summary>
		/// <typeparam name="T">Node type</typeparam>
		void Refresh<T>() where T : IDocumentTreeNodeData;

		/// <summary>
		/// Refreshes all tabs that contain certain nodes
		/// </summary>
		/// <param name="pred">Returns true if the node should be included</param>
		void Refresh(Predicate<IDocumentTreeNodeData> pred);

		/// <summary>
		/// Refreshes all tabs that use <paramref name="document"/>
		/// </summary>
		/// <param name="document">Modified document</param>
		void RefreshModifiedDocument(IDsDocument document);

		/// <summary>
		/// Raised when <see cref="RefreshModifiedDocument(IDsDocument)"/> gets called
		/// </summary>
		event EventHandler<DocumentModifiedEventArgs> DocumentModified;

		/// <summary>
		/// Notified when the document collection gets changed
		/// </summary>
		event EventHandler<NotifyDocumentCollectionChangedEventArgs> DocumentCollectionChanged;

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
		void Close(IDocumentTab tab);

		/// <summary>
		/// Tries to get the <see cref="IDocumentTab"/>
		/// </summary>
		/// <param name="content">Tab content</param>
		/// <returns></returns>
		IDocumentTab TryGetDocumentTab(ITabContent content);

		/// <summary>
		/// Closes all tabs
		/// </summary>
		void CloseAll();

		/// <summary>
		/// Follows the reference in the active tab or a new tab
		/// </summary>
		/// <param name="ref">Reference</param>
		/// <param name="newTab">true to open a new tab</param>
		/// <param name="setFocus">true to give the tab keyboard focus</param>
		/// <param name="onShown">Called after the content has been shown. Can be null.</param>
		void FollowReference(object @ref, bool newTab = false, bool setFocus = true, Action<ShowTabContentEventArgs> onShown = null);

		/// <summary>
		/// Creates a new <see cref="IDocumentTabContent"/> instance. Returns null if it couldn't be created
		/// </summary>
		/// <param name="nodes">Nodes</param>
		/// <returns></returns>
		IDocumentTabContent TryCreateContent(IDocumentTreeNodeData[] nodes);
	}
}
