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

namespace dnSpy.Debugger.Modules {
	[Export(typeof(IPaneCreator))]
	sealed class ModulesControlCreator : IPaneCreator {
		ModulesControlCreator() {
		}

		public IPane Create(string name) {
			if (name == ModulesControl.PANE_TYPE_NAME)
				return ModulesControlInstance;
			return null;
		}

		internal static ModulesControl ModulesControlInstance {
			get {
				if (modulesControl == null) {
					modulesControl = new ModulesControl();
					var vm = new ModulesVM();
					modulesControl.DataContext = vm;
					InitializeCommandShortcuts(modulesControl.listView);
				}
				return modulesControl;
			}
		}

		static ModulesControl modulesControl;

		static void InitializeCommandShortcuts(ListView listView) {
			listView.AddCommandBinding(ApplicationCommands.Copy, new ModulesCtxMenuCommandProxy(new CopyCallModulesCtxMenuCommand()));
			listView.InputBindings.Add(new KeyBinding(new ModulesCtxMenuCommandProxy(new GoToModuleModulesCtxMenuCommand()), Key.Enter, ModifierKeys.None));
			listView.InputBindings.Add(new KeyBinding(new ModulesCtxMenuCommandProxy(new GoToModuleNewTabModulesCtxMenuCommand()), Key.Enter, ModifierKeys.Control));
			listView.InputBindings.Add(new KeyBinding(new ModulesCtxMenuCommandProxy(new GoToModuleNewTabModulesCtxMenuCommand()), Key.Enter, ModifierKeys.Shift));
		}
	}

	public partial class ModulesControl : UserControl, IPane {
		public static readonly string PANE_TYPE_NAME = "modules window";

		public ModulesControl() {
			InitializeComponent();
			dntheme.Themes.ThemeChanged += Themes_ThemeChanged;
		}

		public ICommand ShowCommand {
			get { return new RelayCommand(a => Show(), a => CanShow); }
		}

		void Themes_ThemeChanged(object sender, EventArgs e) {
			var vm = DataContext as ModulesVM;
			if (vm != null)
				vm.RefreshThemeFields();
		}

		string IPane.PaneName {
			get { return PANE_TYPE_NAME; }
		}

		string IPane.PaneTitle {
			get { return "Modules"; }
		}

		void IPane.Closed() {
		}

		void IPane.Opened() {
		}

		bool CanShow {
			get { return DebugManager.Instance.IsDebugging; }
		}

		void Show() {
			if (!MainWindow.Instance.IsBottomPaneVisible(this))
				MainWindow.Instance.ShowInBottomPane(((IPane)this).PaneTitle, this);
			UIUtils.FocusSelector(listView);
		}

		void listView_MouseDoubleClick(object sender, MouseButtonEventArgs e) {
			if (!UIUtils.IsLeftDoubleClick<ListViewItem>(listView, e))
				return;
			bool newTab = Keyboard.Modifiers == ModifierKeys.Shift || Keyboard.Modifiers == ModifierKeys.Control;
			GoToModuleModulesCtxMenuCommand.ExecuteInternal(listView.SelectedItem as ModuleVM, newTab);
		}
	}
}
