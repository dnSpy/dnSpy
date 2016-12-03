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

using System.Windows;
using System.Windows.Controls;

namespace dnSpy.Contracts.TreeView {
	/// <summary>
	/// Treeview options
	/// </summary>
	public sealed class TreeViewOptions {
		/// <summary>
		/// true if drag and drop is possible
		/// </summary>
		public bool CanDragAndDrop { get; set; }

		/// <summary>
		/// See <see cref="UIElement.AllowDrop"/>
		/// </summary>
		public bool AllowDrop { get; set; }

		/// <summary>
		/// See <see cref="VirtualizingStackPanel.IsVirtualizingProperty"/>
		/// </summary>
		public bool IsVirtualizing { get; set; }

		/// <summary>
		/// See <see cref="VirtualizingStackPanel.VirtualizationModeProperty"/>
		/// </summary>
		public VirtualizationMode VirtualizationMode { get; set; }

		/// <summary>
		/// See <see cref="ListBox.SelectionMode"/>
		/// </summary>
		public SelectionMode SelectionMode { get; set; }

		/// <summary>
		/// true if it's a grid view
		/// </summary>
		public bool IsGridView { get; set; }

		/// <summary>
		/// <see cref="ITreeView"/> listener
		/// </summary>
		public ITreeViewListener TreeViewListener { get; set; }

		/// <summary>
		/// The root node or null
		/// </summary>
		public TreeNodeData RootNode { get; set; }

		/// <summary>
		/// Foreground brush resource key or null to use the default color
		/// </summary>
		public object ForegroundBrushResourceKey { get; set; }

		/// <summary>
		/// Constructor
		/// </summary>
		public TreeViewOptions() {
			CanDragAndDrop = true;
			AllowDrop = false;
			IsVirtualizing = true;
			VirtualizationMode = VirtualizationMode.Recycling;
			SelectionMode = SelectionMode.Extended;
		}
	}
}
