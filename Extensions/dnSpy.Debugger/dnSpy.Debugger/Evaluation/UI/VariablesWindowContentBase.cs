/*
    Copyright (C) 2014-2019 de4dot@gmail.com

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

namespace dnSpy.Debugger.Evaluation.UI {
	interface IVariablesWindowContent : IUIObjectProvider {
		void OnShow();
		void OnClose();
		void OnVisible();
		void OnHidden();
		void Focus();
		IVariablesWindowVM VM { get; }
	}

	abstract class VariablesWindowContentBase : IVariablesWindowContent {
		public object UIObject => variablesWindowControl;
		public IInputElement FocusedElement => variablesWindowControl.ListView as IInputElement ?? variablesWindowControl;
		public FrameworkElement ZoomElement => variablesWindowControl;
		public IVariablesWindowVM VM => variablesWindowVM;

		internal VariablesWindowControl VariablesWindowControl => variablesWindowControl;

		readonly VariablesWindowControl variablesWindowControl;
		IVariablesWindowVM variablesWindowVM;

		protected VariablesWindowContentBase() => variablesWindowControl = new VariablesWindowControl();

		protected void Initialize(IWpfCommandService wpfCommandService, VariablesWindowVMFactory variablesWindowVMFactory, VariablesWindowVMOptions options) {
			variablesWindowVM = variablesWindowVMFactory.Create(options);
			variablesWindowVM.TreeViewChanged += VariablesWindowVM_TreeViewChanged;
			variablesWindowControl.DataContext = variablesWindowVM;
		}

		void VariablesWindowVM_TreeViewChanged(object sender, EventArgs e) => variablesWindowControl.SetTreeView(variablesWindowVM.TreeView, variablesWindowVM.VM.VariablesWindowKind);

		public void Focus() {
			var listView = variablesWindowControl.ListView;
			if (listView != null)
				UIUtilities.FocusSelector(listView);
		}

		public void OnClose() => variablesWindowVM.IsOpen = false;
		public void OnShow() => variablesWindowVM.IsOpen = true;
		public void OnHidden() => variablesWindowVM.IsVisible = false;
		public void OnVisible() => variablesWindowVM.IsVisible = true;
	}
}
