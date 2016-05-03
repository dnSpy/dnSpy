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

namespace dnSpy.Contracts.Files.TreeView {
	/// <summary>
	/// <see cref="IFileTreeNodeData"/> activated event args
	/// </summary>
	public sealed class FileTreeNodeActivatedEventArgs : EventArgs {
		/// <summary>
		/// Activated node
		/// </summary>
		public IFileTreeNodeData Node { get; }

		/// <summary>
		/// Set it to true if the event was handled
		/// </summary>
		public bool Handled { get; set; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="node">Node</param>
		public FileTreeNodeActivatedEventArgs(IFileTreeNodeData node) {
			if (node == null)
				throw new ArgumentNullException();
			this.Node = node;
		}
	}
}
