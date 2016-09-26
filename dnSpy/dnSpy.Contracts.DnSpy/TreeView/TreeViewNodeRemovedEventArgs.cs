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

namespace dnSpy.Contracts.TreeView {
	/// <summary>
	/// Node removed event args
	/// </summary>
	public sealed class TreeViewNodeRemovedEventArgs : EventArgs {
		/// <summary>
		/// The node
		/// </summary>
		public ITreeNodeData Node { get; }

		/// <summary>
		/// true if <see cref="Node"/> was removed
		/// </summary>
		public bool Removed { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="node">Node</param>
		/// <param name="removed">true if it was removed</param>
		public TreeViewNodeRemovedEventArgs(ITreeNodeData node, bool removed) {
			this.Node = node;
			this.Removed = removed;
		}
	}
}
