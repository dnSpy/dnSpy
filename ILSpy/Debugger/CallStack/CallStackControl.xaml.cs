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
using System.ComponentModel.Composition;
using System.Windows.Controls;
using System.Windows.Input;
using dnSpy.MVVM;
using ICSharpCode.ILSpy;

namespace dnSpy.Debugger.CallStack {
	[Export(typeof(IPaneCreator))]
	sealed class CallStackPaneCreator : IPaneCreator {
		CallStackPaneCreator() {
		}

		public IPane Create(string name) {
			if (name == CallStackControl.PANE_TYPE_NAME)
				return CallStackControlInstance;
			return null;
		}

		internal static CallStackControl CallStackControlInstance {
			get {
				if (callStackControl == null) {
					callStackControl = new CallStackControl();
					callStackControl.DataContext = new CallStackVM();
					InitializeCommandShortcuts(callStackControl.listView);
				}
				return callStackControl;
			}
		}
		static CallStackControl callStackControl;

		static void InitializeCommandShortcuts(ListView listView) {
			listView.AddCommandBinding(ApplicationCommands.Copy, new CallStackCtxMenuCommandProxy(new CopyCallStackCtxMenuCommand()));
			listView.AddCommandBinding(new CallStackCtxMenuCommandProxy(new RunToCursorCallStackCtxMenuCommand()), ModifierKeys.Control, Key.F10);
		}
	}

	public partial class CallStackControl : UserControl, IPane {
		public static readonly string PANE_TYPE_NAME = "call stack window";

		public CallStackControl() {
			InitializeComponent();
			dntheme.Themes.ThemeChanged += Themes_ThemeChanged;
		}

		public ICommand ShowCommand {
			get { return new RelayCommand(a => Show(), a => CanShow); }
		}

		void Themes_ThemeChanged(object sender, EventArgs e) {
			var vm = DataContext as CallStackVM;
			if (vm != null)
				vm.RefreshIconFields();
		}

		string IPane.PaneName {
			get { return PANE_TYPE_NAME; }
		}

		string IPane.PaneTitle {
			get { return "Call Stack"; }
		}

		void IPane.Closed() {
			var vm = DataContext as CallStackVM;
			if (vm != null)
				vm.IsEnabled = false;
		}

		void IPane.Opened() {
			var vm = DataContext as CallStackVM;
			if (vm != null)
				vm.IsEnabled = true;
		}

		bool CanShow {
			get { return DebugManager.Instance.IsDebugging && !MainWindow.Instance.IsBottomPaneContent(this); }
		}

		void Show() {
			if (!MainWindow.Instance.IsBottomPaneVisible(this))
				MainWindow.Instance.ShowInBottomPane(((IPane)this).PaneTitle, this);
			UIUtils.FocusSelector(listView);
		}

		void listView_MouseDoubleClick(object sender, MouseButtonEventArgs e) {
			if (!UIUtils.IsLeftDoubleClick<ListViewItem>(listView, e))
				return;
			SwitchToFrameCallStackCtxMenuCommand.Execute(listView.SelectedItem as CallStackFrameVM);
		}
	}
}
