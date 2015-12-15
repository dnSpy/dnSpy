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
using System.Linq;
using dnSpy.Contracts.Images;

namespace dnSpy.Contracts.TreeView {
	/// <summary>
	/// User data stored in a <see cref="ITreeNode"/>
	/// </summary>
	public interface ITreeNodeData {
		/// <summary>
		/// Guid of this data
		/// </summary>
		Guid Guid { get; }

		/// <summary>
		/// Group or null
		/// </summary>
		ITreeNodeGroup TreeNodeGroup { get; }

		/// <summary>
		/// Gets the data shown in the UI
		/// </summary>
		object Text { get; }

		/// <summary>
		/// Gets the data shown in a tooltip
		/// </summary>
		object ToolTip { get; }

		/// <summary>
		/// Icon
		/// </summary>
		ImageReference Icon { get; }

		/// <summary>
		/// Expanded icon or null to use <see cref="Icon"/>
		/// </summary>
		ImageReference? ExpandedIcon { get; }

		/// <summary>
		/// true if single clicking on a node expands all its children
		/// </summary>
		bool SingleClickExpandsChildren { get; }

		/// <summary>
		/// Returns true if the expander should be shown
		/// </summary>
		/// <param name="defaultValue">Default value</param>
		/// <returns></returns>
		bool ShowExpander(bool defaultValue);

		/// <summary>
		/// Gets the <see cref="ITreeNode"/> owner instance. Only the tree view may write to this
		/// property.
		/// </summary>
		ITreeNode TreeNode { get; set; }

		/// <summary>
		/// Called when it's time to create its children
		/// </summary>
		/// <returns></returns>
		IEnumerable<ITreeNodeData> CreateChildren();

		/// <summary>
		/// Called after <see cref="TreeNode"/> has been set.
		/// </summary>
		void Initialize();

		/// <summary>
		/// Called by <see cref="ITreeNode.RefreshUI()"/> before it invalidates all UI properties
		/// </summary>
		void OnRefreshUI();

		/// <summary>
		/// Called when the item gets activated, eg. double clicked. Returns true if it was handled,
		/// false otherwise.
		/// </summary>
		/// <returns></returns>
		bool Activate();

		/// <summary>
		/// Called by <see cref="ITreeNode.EnsureChildrenLoaded()"/>
		/// </summary>
		void OnEnsureChildrenLoaded();

		/// <summary>
		/// Called when the children has been changed
		/// </summary>
		/// <param name="added">Added nodes</param>
		/// <param name="removed">Removed nodes</param>
		void OnChildrenChanged(ITreeNodeData[] added, ITreeNodeData[] removed);

		/// <summary>
		/// Called when <see cref="ITreeNode.IsVisible"/> has changed
		/// </summary>
		void OnIsVisibleChanged();

		/// <summary>
		/// Called when <see cref="ITreeNode.IsExpanded"/> has changed
		/// </summary>
		/// <param name="isExpanded">Value of <see cref="ITreeNode.IsExpanded"/></param>
		void OnIsExpandedChanged(bool isExpanded);
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
		public static IEnumerable<ITreeNodeData> Descendants(this ITreeNodeData self) {
			return self.TreeNode.Descendants().Select(a => a.Data);
		}

		/// <summary>
		/// Gets all descendants including itself
		/// </summary>
		/// <param name="self">This</param>
		/// <returns></returns>
		public static IEnumerable<ITreeNodeData> DescendantsAndSelf(this ITreeNodeData self) {
			return self.TreeNode.DescendantsAndSelf().Select(a => a.Data);
		}

		/// <summary>
		/// Gets the ancestor of a certain type
		/// </summary>
		/// <typeparam name="T">Desired type</typeparam>
		/// <param name="self">This</param>
		/// <returns></returns>
		public static T GetAncestorOrSelf<T>(this ITreeNodeData self) where T : class, ITreeNodeData {
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
