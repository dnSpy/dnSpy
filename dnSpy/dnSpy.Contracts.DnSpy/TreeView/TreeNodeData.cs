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
using System.Linq;
using System.Windows;
using dnSpy.Contracts.Images;

namespace dnSpy.Contracts.TreeView {
	/// <summary>
	/// Treenode data base class
	/// </summary>
	public abstract class TreeNodeData {
		/// <summary>
		/// Guid of this node
		/// </summary>
		public abstract Guid Guid { get; }

		/// <summary>
		/// Gets the data shown in the UI
		/// </summary>
		public abstract object Text { get; }

		/// <summary>
		/// Gets the data shown in a tooltip
		/// </summary>
		public abstract object ToolTip { get; }

		/// <summary>
		/// Icon
		/// </summary>
		public abstract ImageReference Icon { get; }

		/// <summary>
		/// Group or null
		/// </summary>
		public virtual ITreeNodeGroup TreeNodeGroup => null;

		/// <summary>
		/// Expanded icon or null to use <see cref="Icon"/>
		/// </summary>
		public virtual ImageReference? ExpandedIcon => null;

		/// <summary>
		/// true if single clicking on a node expands all its children
		/// </summary>
		public virtual bool SingleClickExpandsChildren => false;

		/// <summary>
		/// Gets the <see cref="ITreeNode"/> owner instance. Only the treeview may write to this
		/// property.
		/// </summary>
		public ITreeNode TreeNode {
			get { return treeNode; }
			set {
				if (value == null)
					throw new ArgumentNullException(nameof(value));
				if (treeNode != null)
					throw new InvalidOperationException();
				treeNode = value;
			}
		}
		ITreeNode treeNode;

		/// <summary>
		/// Constructor
		/// </summary>
		protected TreeNodeData() {
		}

		/// <summary>
		/// Returns true if the expander should be shown
		/// </summary>
		/// <param name="defaultValue">Default value</param>
		/// <returns></returns>
		public virtual bool ShowExpander(bool defaultValue) => defaultValue;

		/// <summary>
		/// Called when it's time to create its children
		/// </summary>
		/// <returns></returns>
		public virtual IEnumerable<TreeNodeData> CreateChildren() {
			yield break;
		}

		/// <summary>
		/// Called after <see cref="TreeNode"/> has been set.
		/// </summary>
		public virtual void Initialize() { }

		/// <summary>
		/// Called by <see cref="ITreeNode.RefreshUI()"/> before it invalidates all UI properties
		/// </summary>
		public abstract void OnRefreshUI();

		/// <summary>
		/// Called when the item gets activated, eg. double clicked. Returns true if it was handled,
		/// false otherwise.
		/// </summary>
		/// <returns></returns>
		public virtual bool Activate() => false;

		/// <summary>
		/// Called by <see cref="ITreeNode.EnsureChildrenLoaded()"/>
		/// </summary>
		public virtual void OnEnsureChildrenLoaded() { }

		/// <summary>
		/// Called when the children has changed
		/// </summary>
		/// <param name="added">Added nodes</param>
		/// <param name="removed">Removed nodes</param>
		public virtual void OnChildrenChanged(TreeNodeData[] added, TreeNodeData[] removed) { }

		/// <summary>
		/// Called when <see cref="ITreeNode.IsVisible"/> has changed
		/// </summary>
		public virtual void OnIsVisibleChanged() { }

		/// <summary>
		/// Called when <see cref="ITreeNode.IsExpanded"/> has changed
		/// </summary>
		/// <param name="isExpanded">Value of <see cref="ITreeNode.IsExpanded"/></param>
		public virtual void OnIsExpandedChanged(bool isExpanded) { }

		/// <summary>
		/// Returns true if the nodes can be dragged
		/// </summary>
		/// <param name="nodes">Nodes</param>
		/// <returns></returns>
		public virtual bool CanDrag(TreeNodeData[] nodes) => false;

		/// <summary>
		/// Starts the drag and drop operation
		/// </summary>
		/// <param name="dragSource">Drag source</param>
		/// <param name="nodes">Nodes</param>
		public virtual void StartDrag(DependencyObject dragSource, TreeNodeData[] nodes) { }

		/// <summary>
		/// Copies nodes
		/// </summary>
		/// <param name="nodes">Nodes</param>
		/// <returns></returns>
		public virtual IDataObject Copy(TreeNodeData[] nodes) {
			throw new NotSupportedException();
		}

		/// <summary>
		/// Returns true if drop can execute
		/// </summary>
		/// <param name="e">Event args</param>
		/// <param name="index">Index</param>
		/// <returns></returns>
		public virtual bool CanDrop(DragEventArgs e, int index) => false;

		/// <summary>
		/// Drops data
		/// </summary>
		/// <param name="e">Event args</param>
		/// <param name="index">Index</param>
		public virtual void Drop(DragEventArgs e, int index) {
			throw new NotSupportedException();
		}
	}

	/// <summary>
	/// Extension methods
	/// </summary>
	public static class TreeNodeDataExtensionMethods {
		/// <summary>
		/// Gets all descendants
		/// </summary>
		/// <param name="self">This</param>
		/// <returns></returns>
		public static IEnumerable<TreeNodeData> Descendants(this TreeNodeData self) => self.TreeNode.Descendants().Select(a => a.Data);

		/// <summary>
		/// Gets all descendants including itself
		/// </summary>
		/// <param name="self">This</param>
		/// <returns></returns>
		public static IEnumerable<TreeNodeData> DescendantsAndSelf(this TreeNodeData self) => self.TreeNode.DescendantsAndSelf().Select(a => a.Data);

		/// <summary>
		/// Gets the ancestor of a certain type
		/// </summary>
		/// <typeparam name="T">Desired type</typeparam>
		/// <param name="self">This</param>
		/// <returns></returns>
		public static T GetAncestorOrSelf<T>(this TreeNodeData self) where T : TreeNodeData {
			while (self != null) {
				var found = self as T;
				if (found != null)
					return found;
				var parent = self.TreeNode.Parent;
				if (parent == null)
					break;
				self = parent.Data;
			}
			return null;
		}
	}
}
