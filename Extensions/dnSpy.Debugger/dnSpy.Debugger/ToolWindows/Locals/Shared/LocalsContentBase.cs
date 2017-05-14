/*
    Copyright (C) 2014-2017 de4dot@gmail.com

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
using System.Windows;
using dnSpy.Contracts.Controls;
using dnSpy.Contracts.Utilities;

namespace dnSpy.Debugger.ToolWindows.Locals.Shared {
	interface ILocalsContent : IUIObjectProvider {
		void OnShow();
		void OnClose();
		void OnVisible();
		void OnHidden();
		void Focus();
		ILocalsVM VM { get; }
	}

	abstract class LocalsContentBase : ILocalsContent {
		public object UIObject => localsControl;
		public IInputElement FocusedElement => localsControl.ListView as IInputElement ?? localsControl;
		public FrameworkElement ZoomElement => localsControl;
		public ILocalsVM VM => localsVM;

		readonly LocalsControl localsControl;
		readonly ILocalsVM localsVM;

		protected LocalsContentBase(IWpfCommandService wpfCommandService, LocalsVMFactory localsVMFactory) {
			localsControl = new LocalsControl();
			localsVM = localsVMFactory.Create(CreateLocalsVMOptions());
			localsVM.TreeViewChanged += LocalsVM_TreeViewChanged;
			localsControl.DataContext = localsVM;
		}

		protected abstract LocalsVMOptions CreateLocalsVMOptions();

		void LocalsVM_TreeViewChanged(object sender, EventArgs e) => localsControl.SetTreeView(localsVM.TreeView);

		public void Focus() {
			var listView = localsControl.ListView;
			if (listView != null)
				UIUtilities.FocusSelector(listView);
		}

		public void OnClose() => localsVM.IsOpen = false;
		public void OnShow() => localsVM.IsOpen = true;
		public void OnHidden() => localsVM.IsVisible = false;
		public void OnVisible() => localsVM.IsVisible = true;
	}
}
