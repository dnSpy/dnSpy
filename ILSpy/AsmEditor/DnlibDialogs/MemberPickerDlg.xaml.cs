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
using System.Windows.Threading;
using dnSpy.AsmEditor.ViewHelpers;
using dnSpy.Images;
using ICSharpCode.ILSpy;
using ICSharpCode.TreeView;

namespace dnSpy.AsmEditor.DnlibDialogs {
	/// <summary>
	/// Interaction logic for MemberPickerDlg.xaml
	/// </summary>
	public partial class MemberPickerDlg : WindowBase, IMakeVisible {
		public MemberPickerDlg() {
			InitializeComponent();
			DataContextChanged += (s, e) => {
				var data = DataContext as MemberPickerVM;
				if (data != null) {
					data.OpenAssembly = new OpenAssembly();
					data.MakeVisible = this;
					data.AssemblyListTreeNode.OwnerTreeView = treeView;
				}
			};
			MainWindow.InitializeAssemblyTreeView(treeView);
			openImage.Source = ImageCache.Instance.GetImage("Open", BackgroundType.DialogWindow);
			privateInternalImage.Source = ImageCache.Instance.GetImage("PrivateInternal", BackgroundType.DialogWindow);
		}

		void IMakeVisible.ScrollIntoView(object item) {
			var node = item as SharpTreeNode;
			if (node != null) {
				// Calling ScrollIntoView() immediately won't always work so delay a little bit.
				// We must call ScrollIntoView() immediately or the selected item won't be
				// highlighted for some reason.
				treeView.ScrollIntoView(node);
				this.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(delegate {
					treeView.ScrollIntoView(node);
				}));
			}
		}
	}
}
