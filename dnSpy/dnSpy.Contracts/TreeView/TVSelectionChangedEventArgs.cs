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
	/// Selection changed event args
	/// </summary>
	public sealed class TVSelectionChangedEventArgs : EventArgs {
		/// <summary>
		/// Added nodes
		/// </summary>
		public ITreeNodeData[] Added { get; }

		/// <summary>
		/// Removed nodes
		/// </summary>
		public ITreeNodeData[] Removed { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="added">Added nodes or null</param>
		/// <param name="removed">Removed nodes or null</param>
		public TVSelectionChangedEventArgs(ITreeNodeData[] added, ITreeNodeData[] removed) {
			this.Added = added ?? Array.Empty<ITreeNodeData>();
			this.Removed = removed ?? Array.Empty<ITreeNodeData>();
		}
	}
}
