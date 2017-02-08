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

using System.Collections.Generic;

namespace dnSpy.Contracts.TreeView {
	/// <summary>
	/// A tree node
	/// </summary>
	public interface ITreeNode {
		/// <summary>
		/// Gets the treeview
		/// </summary>
		ITreeView TreeView { get; }

		/// <summary>
		/// Gets the parent or null if it is the root node or if it hasn't been inserted into the
		/// treeview yet.
		/// </summary>
		ITreeNode Parent { get; }

		/// <summary>
		/// Gets all children or an empty list if <see cref="LazyLoading"/> is true. See also
		/// <see cref="EnsureChildrenLoaded()"/>
		/// </summary>
		IList<ITreeNode> Children { get; }

		/// <summary>
		/// Gets all <see cref="TreeNodeData"/> children in <see cref="Children"/>. See also
		/// <see cref="EnsureChildrenLoaded()"/>
		/// </summary>
		IEnumerable<TreeNodeData> DataChildren { get; }

		/// <summary>
		/// Tree node data
		/// </summary>
		TreeNodeData Data { get; }

		/// <summary>
		/// Gets/sets lazy loading of children. When true, <see cref="TreeNodeData.CreateChildren()"/>
		/// will get called to load the children. Should only be used by <see cref="Data"/>
		/// </summary>
		bool LazyLoading { get; set; }

		/// <summary>
		/// true if it's expanded
		/// </summary>
		bool IsExpanded { get; set; }

		/// <summary>
		/// true if it's hidden
		/// </summary>
		bool IsHidden { get; set; }

		/// <summary>
		/// true when this node is not hidden and all parent nodes are expanded and not hidden
		/// </summary>
		bool IsVisible { get; }

		/// <summary>
		/// Forces loading of <see cref="Children"/>
		/// </summary>
		void EnsureChildrenLoaded();

		/// <summary>
		/// Adds a new node to <see cref="Children"/>
		/// </summary>
		/// <param name="node">Node to insert</param>
		void AddChild(ITreeNode node);

		/// <summary>
		/// Gets all descendants
		/// </summary>
		/// <returns></returns>
		IEnumerable<ITreeNode> Descendants();

		/// <summary>
		/// Gets all descendants including itself
		/// </summary>
		/// <returns></returns>
		IEnumerable<ITreeNode> DescendantsAndSelf();

		/// <summary>
		/// Refreshes the UI
		/// </summary>
		void RefreshUI();
	}
}
