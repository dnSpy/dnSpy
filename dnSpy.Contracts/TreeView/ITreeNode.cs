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

namespace dnSpy.Contracts.TreeView {
	/// <summary>
	/// A tree node
	/// </summary>
	public interface ITreeNode {
		/// <summary>
		/// Gets the tree view
		/// </summary>
		ITreeView TreeView { get; }

		/// <summary>
		/// Gets the parent or null if it is the root node or if it hasn't been inserted into the
		/// tree view yet.
		/// </summary>
		ITreeNode Parent { get; }

		/// <summary>
		/// Gets all children
		/// </summary>
		IList<ITreeNode> Children { get; }

		/// <summary>
		/// Tree node data
		/// </summary>
		ITreeNodeData Data { get; }

		/// <summary>
		/// Gets/sets lazy loading of children. When true, <see cref="ITreeNodeData.CreateChildren()"/>
		/// will get called to load the children. Should only be used by <see cref="Data"/>
		/// </summary>
		bool LazyLoading { get; set; }

		/// <summary>
		/// Adds a new node to <see cref="Children"/>
		/// </summary>
		/// <param name="node">Node to insert</param>
		void AddChild(ITreeNode node);
	}
}
