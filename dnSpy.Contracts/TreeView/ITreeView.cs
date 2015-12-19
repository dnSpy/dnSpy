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

namespace dnSpy.Contracts.TreeView {
	/// <summary>
	/// A tree view
	/// </summary>
	public interface ITreeView {
		/// <summary>
		/// Guid of this tree view
		/// </summary>
		Guid Guid { get; }

		/// <summary>
		/// Gets the invisible root node
		/// </summary>
		ITreeNode Root { get; }

		/// <summary>
		/// Creates a new <see cref="ITreeNode"/> instance that can be inserted into this, and only
		/// this, tree view.
		/// </summary>
		/// <param name="data">User data</param>
		/// <returns></returns>
		ITreeNode Create(ITreeNodeData data);

		/// <summary>
		/// Gets the tree view UI object
		/// </summary>
		object UIObject { get; }

		/// <summary>
		/// Select items
		/// </summary>
		/// <param name="items">Items to select</param>
		void SelectItems(IEnumerable<ITreeNodeData> items);

		/// <summary>
		/// Raised when selection has changed
		/// </summary>
		event EventHandler<TVSelectionChangedEventArgs> SelectionChanged;

		/// <summary>
		/// Gets the selected node or null
		/// </summary>
		ITreeNodeData SelectedItem { get; }

		/// <summary>
		/// Gets all selected items
		/// </summary>
		ITreeNodeData[] SelectedItems { get; }

		/// <summary>
		/// Gets the selected items which don't have any of their ancestors selected
		/// </summary>
		ITreeNodeData[] TopLevelSelection { get; }

		/// <summary>
		/// Focuses the treeview, possibly getting keyboard focus
		/// </summary>
		void Focus();

		/// <summary>
		/// Calls all nodes' <see cref="ITreeNode.RefreshUI()"/> method
		/// </summary>
		void RefreshAllNodes();
	}
}
