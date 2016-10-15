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
using System.Collections.ObjectModel;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.Text.Classification;
using Microsoft.VisualStudio.Text.Classification;

namespace dnSpy.Contracts.TreeView.Text {
	/// <summary>
	/// Treeview node classifier context passed to <see cref="ITextClassifier"/>s that classify treeview nodes
	/// </summary>
	public class TreeViewNodeClassifierContext : TextClassifierContext {
		/// <summary>
		/// Gets the treeview
		/// </summary>
		public ITreeView TreeView { get; }

		/// <summary>
		/// Gets the node to classify
		/// </summary>
		public ITreeNodeData Node { get; }

		/// <summary>
		/// Default colors, can be empty and there could be non-classified parts
		/// </summary>
		public ReadOnlyCollection<SpanData<object>> Colors { get; }

		/// <summary>
		/// true if the content will be shown in a tooltip
		/// </summary>
		public bool IsToolTip { get; }

		/// <summary>
		/// true if all <see cref="ITextClassifier"/>s can classify the text, false if only special
		/// colors (eg. search highlighting) will be shown. <see cref="Colors"/> won't be used if
		/// this property is false.
		/// </summary>
		public bool Colorize { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="text">Text to classify</param>
		/// <param name="treeView">Treeview</param>
		/// <param name="node">Node to classify</param>
		/// <param name="isToolTip">true if the content will be shown in a tooltip</param>
		/// <param name="colors">Default colors or null. It doesn't have to be sorted and elements can overlap. The colors
		/// must be <see cref="IClassificationType"/>s or <see cref="TextColor"/>s</param>
		/// <param name="colorize">true to colorize everything, false to remove most colors except special colors (depends on the text classifiers)</param>
		public TreeViewNodeClassifierContext(string text, ITreeView treeView, ITreeNodeData node, bool isToolTip, ReadOnlyCollection<SpanData<object>> colors = null, bool colorize = true)
			: base(text) {
			if (treeView == null)
				throw new ArgumentNullException(nameof(treeView));
			if (node == null)
				throw new ArgumentNullException(nameof(node));
			TreeView = treeView;
			Node = node;
			IsToolTip = isToolTip;
			Colors = colors ?? emptyColors;
			Colorize = colorize;
		}
		static readonly ReadOnlyCollection<SpanData<object>> emptyColors = new ReadOnlyCollection<SpanData<object>>(Array.Empty<SpanData<object>>());
	}
}
