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

using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using dnSpy.Contracts;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Themes;
using ICSharpCode.TreeView;

namespace dnSpy.TreeView {
	sealed class DnSpySharpTreeNode : SharpTreeNode {
		public TreeNodeImpl TreeNodeImpl {
			get { return treeNodeImpl; }
		}
		readonly TreeNodeImpl treeNodeImpl;

		public DnSpySharpTreeNode(TreeNodeImpl treeNodeImpl) {
			this.treeNodeImpl = treeNodeImpl;
		}

		public override object ExpandedIcon {
			get { return GetIcon(treeNodeImpl.Data.ExpandedIcon ?? treeNodeImpl.Data.Icon); }
		}

		public override object Icon {
			get { return GetIcon(treeNodeImpl.Data.Icon); }
		}

		public override bool SingleClickExpandsChildren {
			get { return treeNodeImpl.Data.SingleClickExpandsChildren; }
		}

		public override object Text {
			get { return treeNodeImpl.Data.Text; }
		}

		public override object ToolTip {
			get { return treeNodeImpl.Data.ToolTip; }
		}

		protected override void LoadChildren() {
			treeNodeImpl.TreeView.AddChildren(treeNodeImpl);
		}

		public override bool ShowExpander {
			get { return treeNodeImpl.Data.ShowExpander(base.ShowExpander); }
		}

		object GetIcon(ImageReference imgRef) {
			bool b = imgRef.Assembly != null && imgRef.Name != null;
			Debug.Assert(b);
			if (!b)
				return null;
			return treeNodeImpl.TreeView.GetIcon(imgRef);
		}

		public override Brush Foreground {
			get { return DnSpy.App.ThemeManager.Theme.GetColor(ColorType.TreeViewNode).Foreground; }
		}

		public void RefreshUI() {
			RaisePropertyChanged("Icon");
			RaisePropertyChanged("ExpandedIcon");
			RaisePropertyChanged("ToolTip");
			RaisePropertyChanged("Text");
			RaisePropertyChanged("Foreground");
		}

		public override sealed bool IsCheckable {
			get { return false; }
		}

		public override sealed bool IsEditable {
			get { return false; }
		}

		public override void ActivateItem(RoutedEventArgs e) {
			e.Handled = treeNodeImpl.Data.Activate();
		}
	}
}
