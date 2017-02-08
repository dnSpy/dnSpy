/*
    Copyright (C) 2014-2017 de4dot@gmail.com

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
using System.Windows.Controls;

namespace dnSpy.Contracts.TreeView {
	/// <summary>
	/// A treeview
	/// </summary>
	public interface ITreeView : IDisposable {
		/// <summary>
		/// Guid of this treeview
		/// </summary>
		Guid Guid { get; }

		/// <summary>
		/// Gets the invisible root node
		/// </summary>
		ITreeNode Root { get; }

		/// <summary>
		/// Creates a new <see cref="ITreeNode"/> instance that can be inserted into this, and only
		/// this, treeview.
		/// </summary>
		/// <param name="data">User data</param>
		/// <returns></returns>
		ITreeNode Create(TreeNodeData data);

		/// <summary>
		/// Gets the treeview UI object
		/// </summary>
		Control UIObject { get; }

		/// <summary>
		/// Select items
		/// </summary>
		/// <param name="items">Items to select</param>
		void SelectItems(IEnumerable<TreeNodeData> items);

		/// <summary>
		/// Raised when selection has changed
		/// </summary>
		event EventHandler<TreeViewSelectionChangedEventArgs> SelectionChanged;

		/// <summary>
		/// Raised when a node has been removed
		/// </summary>
		event EventHandler<TreeViewNodeRemovedEventArgs> NodeRemoved;

		/// <summary>
		/// Gets the selected node or null
		/// </summary>
		TreeNodeData SelectedItem { get; }

		/// <summary>
		/// Gets all selected items
		/// </summary>
		TreeNodeData[] SelectedItems { get; }

		/// <summary>
		/// Gets the selected items which don't have any of their ancestors selected
		/// </summary>
		TreeNodeData[] TopLevelSelection { get; }

		/// <summary>
		/// Focuses the treeview, possibly getting keyboard focus
		/// </summary>
		void Focus();

		/// <summary>
		/// Scrolls the current node into view
		/// </summary>
		void ScrollIntoView();

		/// <summary>
		/// Calls all nodes' <see cref="ITreeNode.RefreshUI()"/> method
		/// </summary>
		void RefreshAllNodes();

		/// <summary>
		/// Converts the selected item to a <see cref="TreeNodeData"/>. Should rarely be called.
		/// </summary>
		/// <param name="selectedItem">Selected item</param>
		/// <returns></returns>
		TreeNodeData FromImplNode(object selectedItem);

		/// <summary>
		/// Converts <paramref name="node"/> to the real tree node
		/// </summary>
		/// <param name="node">Node</param>
		/// <returns></returns>
		object ToImplNode(TreeNodeData node);

		/// <summary>
		/// Collapses all unselected nodes
		/// </summary>
		void CollapseUnusedNodes();
	}
}
