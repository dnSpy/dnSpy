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
using dnSpy.Contracts.Images;
using dnSpy.Contracts.TreeView;

namespace dnSpy.Shared.UI.TreeView {
	public abstract class TreeNodeData : ITreeNodeData {
		public abstract Guid Guid { get; }
		public abstract object Text { get; }
		public abstract object ToolTip { get; }
		public abstract ImageReference Icon { get; }

		public virtual ITreeNodeGroup TreeNodeGroup {
			get { return null; }
		}

		public virtual ImageReference? ExpandedIcon {
			get { return null; }
		}

		public virtual bool SingleClickExpandsChildren {
			get { return false; }
		}

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

		public virtual bool ShowExpander(bool defaultValue) {
			return defaultValue;
		}

		public virtual IEnumerable<ITreeNodeData> CreateChildren() {
			yield break;
		}

		public virtual void Initialize() {
		}

		public abstract void OnRefreshUI();

		public virtual bool Activate() {
			return false;
		}

		public virtual void OnEnsureChildrenLoaded() {
		}

		public virtual void OnChildrenChanged(ITreeNodeData[] added, ITreeNodeData[] removed) {
		}

		public virtual void OnIsVisibleChanged() {
		}

		public virtual void OnIsExpandedChanged(bool isExpanded) {
		}
	}
}
