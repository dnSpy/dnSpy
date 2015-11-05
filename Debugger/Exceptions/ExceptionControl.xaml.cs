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

using System.ComponentModel.Composition;
using System.Windows.Controls;
using System.Windows.Input;
using dnSpy.Contracts;
using dnSpy.Contracts.Themes;
using dnSpy.MVVM;
using dnSpy.Shared.UI.MVVM;
using ICSharpCode.ILSpy;

namespace dnSpy.Debugger.Exceptions {
	[Export(typeof(IPaneCreator))]
	sealed class ExceptionsControlCreator : IPaneCreator {
		ExceptionsControlCreator() {
		}

		public IPane Create(string name) {
			if (name == ExceptionsControl.PANE_TYPE_NAME)
				return ExceptionsControlInstance;
			return null;
		}

		internal static ExceptionsVM ExceptionsVM {
			get { return ExceptionsControlInstance.DataContext as ExceptionsVM; }
		}

		internal static ExceptionsControl ExceptionsControlInstance {
			get {
				if (exceptionsControl == null) {
					exceptionsControl = new ExceptionsControl();
					var vm = new ExceptionsVM(new SelectedItemsProvider<ExceptionVM>(exceptionsControl.listBox), new GetNewExceptionName(MainWindow.Instance));
					exceptionsControl.DataContext = vm;
					exceptionsControl.InputBindings.Add(new KeyBinding(new RelayCommand(a => FocusSearchTextBox()), Key.F, ModifierKeys.Control));
					InitializeCommandShortcuts(exceptionsControl.listBox);
				}
				return exceptionsControl;
			}
		}

		static ExceptionsControl exceptionsControl;

		static void InitializeCommandShortcuts(ListBox listBox) {
			listBox.AddCommandBinding(ApplicationCommands.Copy, new ExceptionsCtxMenuCommandProxy(new CopyCallExceptionsCtxMenuCommand()));
			listBox.InputBindings.Add(new KeyBinding(new ExceptionsCtxMenuCommandProxy(new AddExceptionsCtxMenuCommand()), Key.Insert, ModifierKeys.None));
			listBox.InputBindings.Add(new KeyBinding(new ExceptionsCtxMenuCommandProxy(new RemoveExceptionsCtxMenuCommand()), Key.Delete, ModifierKeys.None));
			listBox.InputBindings.Add(new KeyBinding(new ExceptionsCtxMenuCommandProxy(new ToggleEnableExceptionsCtxMenuCommand()), Key.Space, ModifierKeys.None));
		}

		static void FocusSearchTextBox() {
			ExceptionsControlInstance.searchTextBox.Focus();
			ExceptionsControlInstance.searchTextBox.SelectAll();
		}
	}

	public partial class ExceptionsControl : UserControl, IPane {
		public static readonly string PANE_TYPE_NAME = "exceptions window";

		public ExceptionsControl() {
			InitializeComponent();
			Globals.App.ThemesManager.ThemeChanged += ThemesManager_ThemeChanged;
		}

		public ICommand ShowCommand {
			get { return new RelayCommand(a => Show(), a => CanShow); }
		}

		void ThemesManager_ThemeChanged(object sender, ThemeChangedEventArgs e) {
			var vm = DataContext as ExceptionsVM;
			if (vm != null)
				vm.RefreshThemeFields();
		}

		string IPane.PaneName {
			get { return PANE_TYPE_NAME; }
		}

		string IPane.PaneTitle {
			get { return "Exception Settings"; }
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
			UIUtils.FocusSelector(listBox);
		}
	}
}
