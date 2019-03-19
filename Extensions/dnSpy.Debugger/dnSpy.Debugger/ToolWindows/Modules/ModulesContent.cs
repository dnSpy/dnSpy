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
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using dnSpy.Contracts.App;
using dnSpy.Contracts.Controls;
using dnSpy.Contracts.MVVM;
using dnSpy.Contracts.Utilities;
using dnSpy.Debugger.Properties;

namespace dnSpy.Debugger.ToolWindows.Modules {
	interface IModulesContent : IUIObjectProvider {
		void OnShow();
		void OnClose();
		void OnVisible();
		void OnHidden();
		void Focus();
		void FocusSearchTextBox();
		ListView ListView { get; }
		ModulesOperations Operations { get; }
	}

	[Export(typeof(IModulesContent))]
	sealed class ModulesContent : IModulesContent {
		public object UIObject => modulesControl;
		public IInputElement FocusedElement => modulesControl.ListView;
		public FrameworkElement ZoomElement => modulesControl;
		public ListView ListView => modulesControl.ListView;
		public ModulesOperations Operations { get; }

		readonly ModulesControl modulesControl;
		readonly IModulesVM modulesVM;

		sealed class ControlVM : ViewModelBase {
			public IModulesVM VM { get; }
			ModulesOperations Operations { get; }

			public string SearchToolTip => ToolTipHelper.AddKeyboardShortcut(dnSpy_Debugger_Resources.Modules_Search_ToolTip, dnSpy_Debugger_Resources.ShortCutKeyCtrlF);
			public string ResetSearchSettingsToolTip => ToolTipHelper.AddKeyboardShortcut(dnSpy_Debugger_Resources.Modules_ResetSearchSettings_ToolTip, null);
			public string SearchHelpToolTip => ToolTipHelper.AddKeyboardShortcut(dnSpy_Debugger_Resources.SearchHelp_ToolTip, null);

			public ICommand ResetSearchSettingsCommand => new RelayCommand(a => Operations.ResetSearchSettings(), a => Operations.CanResetSearchSettings);
			public ICommand SearchHelpCommand => new RelayCommand(a => SearchHelp());

			readonly IMessageBoxService messageBoxService;
			readonly DependencyObject control;

			public ControlVM(IModulesVM vm, ModulesOperations operations, IMessageBoxService messageBoxService, DependencyObject control) {
				VM = vm;
				Operations = operations;
				this.messageBoxService = messageBoxService;
				this.control = control;
			}

			void SearchHelp() => messageBoxService.Show(VM.GetSearchHelpText(), ownerWindow: Window.GetWindow(control));
		}

		[ImportingConstructor]
		ModulesContent(IWpfCommandService wpfCommandService, IModulesVM modulesVM, ModulesOperations modulesOperations, IMessageBoxService messageBoxService) {
			Operations = modulesOperations;
			modulesControl = new ModulesControl();
			this.modulesVM = modulesVM;
			modulesControl.DataContext = new ControlVM(modulesVM, modulesOperations, messageBoxService, modulesControl);
			modulesControl.ModulesListViewDoubleClick += ModulesControl_ModulesListViewDoubleClick;

			wpfCommandService.Add(ControlConstants.GUID_DEBUGGER_MODULES_CONTROL, modulesControl);
			wpfCommandService.Add(ControlConstants.GUID_DEBUGGER_MODULES_LISTVIEW, modulesControl.ListView);
		}

		void ModulesControl_ModulesListViewDoubleClick(object sender, EventArgs e) {
			bool newTab = Keyboard.Modifiers == ModifierKeys.Shift || Keyboard.Modifiers == ModifierKeys.Control;
			if (Operations.CanGoToModule)
				Operations.GoToModule(newTab);
		}

		public void FocusSearchTextBox() => modulesControl.FocusSearchTextBox();
		public void Focus() => UIUtilities.FocusSelector(modulesControl.ListView);
		public void OnClose() => modulesVM.IsOpen = false;
		public void OnShow() => modulesVM.IsOpen = true;
		public void OnHidden() => modulesVM.IsVisible = false;
		public void OnVisible() => modulesVM.IsVisible = true;
	}
}
