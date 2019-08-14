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
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using dnSpy.Contracts.Extension;
using dnSpy.Contracts.MVVM;
using dnSpy.Contracts.TreeView;
using dnSpy.Debugger.Evaluation.ViewModel;

namespace dnSpy.Debugger.Evaluation.UI {
	sealed partial class VariablesWindowControl : UserControl {
		static /*readonly*/ Lazy<VariablesWindowOperations>? variablesWindowOperations;

		[ExportAutoLoaded]
		sealed class Loader : IAutoLoaded {
			[ImportingConstructor]
			Loader(Lazy<VariablesWindowOperations> variablesWindowOperations) => VariablesWindowControl.variablesWindowOperations = variablesWindowOperations;
		}

		public ListView? ListView => treeViewContentPresenter.Content as ListView;
		public VariablesWindowControl() => InitializeComponent();
		public void SetTreeView(ITreeView treeView, VariablesWindowKind windowKind) {
			var listView = (ListView?)treeView?.UIObject;
			if (treeViewContentPresenter.Content == listView)
				return;
			if (treeViewContentPresenter.Content is UIElement oldElem)
				oldElem.PreviewTextInput -= TreeView_PreviewTextInput;
			if (!(listView is null))
				listView.PreviewTextInput += TreeView_PreviewTextInput;
			treeViewContentPresenter.Content = listView;
			if (!(listView is null)) {
				AutomationPeerMemoryLeakWorkaround.SetEmptyCount(listView, GetEmptyCount(windowKind));
				var gridView = (GridView)FindResource("GridView");
				listView.View = gridView;
			}
		}

		static int GetEmptyCount(VariablesWindowKind windowKind) {
			switch (windowKind) {
			case VariablesWindowKind.Watch:
				// The edit text box is always present when the window is open
				return 1;

			case VariablesWindowKind.None:
			case VariablesWindowKind.Locals:
			case VariablesWindowKind.Autos:
				return 0;

			default:
				Debug.Fail($"Unknown vars window kind: {windowKind}");
				return 0;
			}
		}

		void TreeView_PreviewTextInput(object? sender, TextCompositionEventArgs e) {
			Debug2.Assert(!(variablesWindowOperations is null));
			if (variablesWindowOperations is null)
				return;
			if (!(treeViewContentPresenter.Content is ListView listView) || listView.SelectedItems.Count != 1)
				return;
			if (!(listView.DataContext is IVariablesWindowVM vm))
				return;
			if (!(e.OriginalSource is ListViewItem))
				return;
			var text = e.Text;
			if (text.Length == 0 || (text.Length == 1 && (text[0] == '\u001B' || text[0] == '\b')))
				return;
			if (variablesWindowOperations.Value.CanEdit(vm.VM)) {
				e.Handled = true;
				variablesWindowOperations.Value.Edit(vm.VM, text);
			}
		}
	}
}
