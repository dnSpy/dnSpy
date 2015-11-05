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

using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Windows.Controls;
using System.Windows.Input;
using dnSpy.Contracts;
using dnSpy.Contracts.Themes;
using dnSpy.Shared.UI.MVVM;
using ICSharpCode.ILSpy;

namespace dnSpy.Debugger.Breakpoints {
	[Export(typeof(IPaneCreator))]
	sealed class BreakpointsControlCreator : IPaneCreator {
		BreakpointsControlCreator() {
		}

		public IPane Create(string name) {
			if (name == BreakpointsControl.PANE_TYPE_NAME)
				return BreakpointsControlInstance;
			return null;
		}

		internal static BreakpointsControl BreakpointsControlInstance {
			get {
				if (breakpointsControl == null) {
					breakpointsControl = new BreakpointsControl();
					var vm = new BreakpointsVM();
					breakpointsControl.DataContext = vm;
					MainWindow.Instance.SessionSettings.FilterSettings.PropertyChanged += FilterSettings_PropertyChanged;
					InitializeCommandShortcuts(breakpointsControl.listView);
				}
				return breakpointsControl;
			}
		}

		static void FilterSettings_PropertyChanged(object sender, PropertyChangedEventArgs e) {
			if (e.PropertyName == "Language") {
				var vm = BreakpointsControlInstance.DataContext as BreakpointsVM;
				if (vm != null)
					vm.RefreshLanguageFields();
			}
		}

		static BreakpointsControl breakpointsControl;

		static void InitializeCommandShortcuts(ListView listView) {
			listView.AddCommandBinding(ApplicationCommands.Copy, new BreakpointCtxMenuCommandProxy(new CopyBreakpointCtxMenuCommand()));
			listView.AddCommandBinding(ApplicationCommands.Delete, new BreakpointCtxMenuCommandProxy(new DeleteBreakpointCtxMenuCommand()));
			listView.InputBindings.Add(new KeyBinding(new BreakpointCtxMenuCommandProxy(new GoToSourceBreakpointCtxMenuCommand()), Key.Enter, ModifierKeys.None));
			listView.InputBindings.Add(new KeyBinding(new BreakpointCtxMenuCommandProxy(new GoToSourceNewTabBreakpointCtxMenuCommand()), Key.Enter, ModifierKeys.Control));
			listView.InputBindings.Add(new KeyBinding(new BreakpointCtxMenuCommandProxy(new GoToSourceNewTabBreakpointCtxMenuCommand()), Key.Enter, ModifierKeys.Shift));
			listView.InputBindings.Add(new KeyBinding(new BreakpointCtxMenuCommandProxy(new ToggleEnableBreakpointCtxMenuCommand()), Key.Space, ModifierKeys.None));
		}
	}

	public partial class BreakpointsControl : UserControl, IPane {
		public static readonly string PANE_TYPE_NAME = "breakpoints window";

		public BreakpointsControl() {
			InitializeComponent();
			Globals.App.ThemesManager.ThemeChanged += ThemesManager_ThemeChanged;
		}

		public ICommand ShowCommand {
			get { return new RelayCommand(a => Show(), a => CanShow); }
		}

		void ThemesManager_ThemeChanged(object sender, ThemeChangedEventArgs e) {
			var vm = DataContext as BreakpointsVM;
			if (vm != null)
				vm.RefreshThemeFields();
		}

		string IPane.PaneName {
			get { return PANE_TYPE_NAME; }
		}

		string IPane.PaneTitle {
			get { return "Breakpoints"; }
		}

		void IPane.Closed() {
		}

		void IPane.Opened() {
		}

		bool CanShow {
			get { return true; }
		}

		void Show() {
			if (!MainWindow.Instance.IsBottomPaneVisible(this))
				MainWindow.Instance.ShowInBottomPane(this);
			FocusPane();
		}

		public void FocusPane() {
			UIUtils.FocusSelector(listView);
		}

		void listView_MouseDoubleClick(object sender, MouseButtonEventArgs e) {
			if (!UIUtils.IsLeftDoubleClick<ListViewItem>(listView, e))
				return;
			bool newTab = Keyboard.Modifiers == ModifierKeys.Shift || Keyboard.Modifiers == ModifierKeys.Control;
			GoToSourceBreakpointCtxMenuCommand.GoTo(listView.SelectedItem as BreakpointVM, newTab);
		}
	}
}
