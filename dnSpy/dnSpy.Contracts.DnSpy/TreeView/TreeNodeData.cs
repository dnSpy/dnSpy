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
using System.Windows;
using dnSpy.Contracts.Images;

namespace dnSpy.Contracts.TreeView {
	/// <summary>
	/// Treenode data base class
	/// </summary>
	public abstract class TreeNodeData : ITreeNodeData {
		/// <inheritdoc/>
		public abstract Guid Guid { get; }
		/// <inheritdoc/>
		public abstract object Text { get; }
		/// <inheritdoc/>
		public abstract object ToolTip { get; }
		/// <inheritdoc/>
		public abstract ImageReference Icon { get; }
		/// <inheritdoc/>
		public virtual ITreeNodeGroup TreeNodeGroup => null;
		/// <inheritdoc/>
		public virtual ImageReference? ExpandedIcon => null;
		/// <inheritdoc/>
		public virtual bool SingleClickExpandsChildren => false;

		/// <inheritdoc/>
		public ITreeNode TreeNode {
			get { return treeNode; }
			set {
				if (value == null)
					throw new ArgumentNullException();
				if (treeNode != null)
					throw new InvalidOperationException();
				treeNode = value;
			}
		}
		ITreeNode treeNode;

		/// <inheritdoc/>
		public virtual bool ShowExpander(bool defaultValue) => defaultValue;

		/// <inheritdoc/>
		public virtual IEnumerable<ITreeNodeData> CreateChildren() {
			yield break;
		}

		/// <inheritdoc/>
		public virtual void Initialize() { }
		/// <inheritdoc/>
		public abstract void OnRefreshUI();
		/// <inheritdoc/>
		public virtual bool Activate() => false;
		/// <inheritdoc/>
		public virtual void OnEnsureChildrenLoaded() { }
		/// <inheritdoc/>
		public virtual void OnChildrenChanged(ITreeNodeData[] added, ITreeNodeData[] removed) { }
		/// <inheritdoc/>
		public virtual void OnIsVisibleChanged() { }
		/// <inheritdoc/>
		public virtual void OnIsExpandedChanged(bool isExpanded) { }
		/// <inheritdoc/>
		public virtual bool CanDrag(ITreeNodeData[] nodes) => false;
		/// <inheritdoc/>
		public virtual void StartDrag(DependencyObject dragSource, ITreeNodeData[] nodes) { }

		/// <inheritdoc/>
		public virtual IDataObject Copy(ITreeNodeData[] nodes) {
			throw new NotSupportedException();
		}

		/// <inheritdoc/>
		public virtual bool CanDrop(DragEventArgs e, int index) => false;

		/// <inheritdoc/>
		public virtual void Drop(DragEventArgs e, int index) {
			throw new NotSupportedException();
		}
	}
}
