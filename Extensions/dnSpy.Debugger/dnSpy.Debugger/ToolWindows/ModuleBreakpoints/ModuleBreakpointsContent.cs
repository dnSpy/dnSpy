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

using System.ComponentModel.Composition;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using dnSpy.Contracts.App;
using dnSpy.Contracts.Controls;
using dnSpy.Contracts.MVVM;
using dnSpy.Contracts.Utilities;
using dnSpy.Debugger.Properties;

namespace dnSpy.Debugger.ToolWindows.ModuleBreakpoints {
	interface IModuleBreakpointsContent : IUIObjectProvider {
		void OnShow();
		void OnClose();
		void OnVisible();
		void OnHidden();
		void Focus();
		void FocusSearchTextBox();
		ListView ListView { get; }
		ModuleBreakpointsOperations Operations { get; }
	}

	[Export(typeof(IModuleBreakpointsContent))]
	sealed class ModuleBreakpointsContent : IModuleBreakpointsContent {
		public object? UIObject => moduleBreakpointsControl;
		public IInputElement? FocusedElement => moduleBreakpointsControl.ListView;
		public FrameworkElement? ZoomElement => moduleBreakpointsControl;
		public ListView ListView => moduleBreakpointsControl.ListView;
		public ModuleBreakpointsOperations Operations { get; }

		readonly ModuleBreakpointsControl moduleBreakpointsControl;
		readonly IModuleBreakpointsVM moduleBreakpointsVM;

		sealed class ControlVM : ViewModelBase {
			public IModuleBreakpointsVM VM { get; }
			ModuleBreakpointsOperations Operations { get; }

			public string AddModuleBreakpointToolTip => ToolTipHelper.AddKeyboardShortcut(dnSpy_Debugger_Resources.Breakpoints_AddBreakpoint_ToolTip, null);
			public string RemoveModuleBreakpointToolTip => ToolTipHelper.AddKeyboardShortcut(dnSpy_Debugger_Resources.Breakpoints_RemoveBreakpoint_ToolTip, null);
			public string RemoveMatchingBreakpointsToolTip => ToolTipHelper.AddKeyboardShortcut(dnSpy_Debugger_Resources.Breakpoints_RemoveMatchingBreakpoints_ToolTip, null);
			public string ToggleMatchingBreakpointsToolTip => ToolTipHelper.AddKeyboardShortcut(dnSpy_Debugger_Resources.Breakpoints_ToggleMatchingBreakpoints_ToolTip, null);
			public string ExportMatchingBreakpointsToolTip => ToolTipHelper.AddKeyboardShortcut(dnSpy_Debugger_Resources.Breakpoints_ExportMatchingBreakpoints_ToolTip, null);
			public string ImportBreakpointsToolTip => ToolTipHelper.AddKeyboardShortcut(dnSpy_Debugger_Resources.Breakpoints_ImportBreakpoints_ToolTip, null);
			public string SearchToolTip => ToolTipHelper.AddKeyboardShortcut(dnSpy_Debugger_Resources.Breakpoints_Search_ToolTip, dnSpy_Debugger_Resources.ShortCutKeyCtrlF);
			public string ResetSearchSettingsToolTip => ToolTipHelper.AddKeyboardShortcut(dnSpy_Debugger_Resources.Breakpoints_ResetSearchSettings_ToolTip, null);
			public string SearchHelpToolTip => ToolTipHelper.AddKeyboardShortcut(dnSpy_Debugger_Resources.SearchHelp_ToolTip, null);

			public ICommand AddModuleBreakpointCommand => new RelayCommand(a => Operations.AddModuleBreakpoint(), a => Operations.CanAddModuleBreakpoint);
			public ICommand RemoveModuleBreakpointsCommand => new RelayCommand(a => Operations.RemoveModuleBreakpoints(), a => Operations.CanRemoveModuleBreakpoints);
			public ICommand RemoveMatchingBreakpointsCommand => new RelayCommand(a => Operations.RemoveMatchingModuleBreakpoints(), a => Operations.CanRemoveMatchingModuleBreakpoints);
			public ICommand ToggleMatchingBreakpointsCommand => new RelayCommand(a => Operations.ToggleMatchingBreakpoints(), a => Operations.CanToggleMatchingBreakpoints);
			public ICommand ExportMatchingBreakpointsCommand => new RelayCommand(a => Operations.ExportMatchingBreakpoints(), a => Operations.CanExportMatchingBreakpoints);
			public ICommand ResetSearchSettingsCommand => new RelayCommand(a => Operations.ResetSearchSettings(), a => Operations.CanResetSearchSettings);
			public ICommand ImportBreakpointsCommand => new RelayCommand(a => Operations.ImportBreakpoints(), a => Operations.CanImportBreakpoints);
			public ICommand SearchHelpCommand => new RelayCommand(a => SearchHelp());

			readonly IMessageBoxService messageBoxService;
			readonly DependencyObject control;

			public ControlVM(IModuleBreakpointsVM vm, ModuleBreakpointsOperations operations, IMessageBoxService messageBoxService, DependencyObject control) {
				VM = vm;
				Operations = operations;
				this.messageBoxService = messageBoxService;
				this.control = control;
			}

			void SearchHelp() => messageBoxService.Show(VM.GetSearchHelpText(), ownerWindow: Window.GetWindow(control));
		}

		[ImportingConstructor]
		ModuleBreakpointsContent(IWpfCommandService wpfCommandService, IModuleBreakpointsVM moduleBreakpointsVM, ModuleBreakpointsOperations moduleBreakpointsOperations, IMessageBoxService messageBoxService) {
			Operations = moduleBreakpointsOperations;
			moduleBreakpointsControl = new ModuleBreakpointsControl();
			this.moduleBreakpointsVM = moduleBreakpointsVM;
			moduleBreakpointsControl.DataContext = new ControlVM(moduleBreakpointsVM, moduleBreakpointsOperations, messageBoxService, moduleBreakpointsControl);

			wpfCommandService.Add(ControlConstants.GUID_DEBUGGER_MODULEBREAKPOINTS_CONTROL, moduleBreakpointsControl);
			wpfCommandService.Add(ControlConstants.GUID_DEBUGGER_MODULEBREAKPOINTS_LISTVIEW, moduleBreakpointsControl.ListView);

			moduleBreakpointsControl.ListView.PreviewKeyDown += ListView_PreviewKeyDown;
		}

		void ListView_PreviewKeyDown(object? sender, KeyEventArgs e) {
			if (!e.Handled) {
				// Use a KeyDown handler. If we add this as a key command to the listview, the textview
				// (used when editing eg. module name) won't see the space.
				if (e.Key == Key.Space && e.KeyboardDevice.Modifiers == ModifierKeys.None) {
					if (Operations.CanToggleEnabled) {
						Operations.ToggleEnabled();
						e.Handled = true;
						return;
					}
				}
			}
		}

		public void FocusSearchTextBox() => moduleBreakpointsControl.FocusSearchTextBox();
		public void Focus() => UIUtilities.FocusSelector(moduleBreakpointsControl.ListView);
		public void OnClose() => moduleBreakpointsVM.IsOpen = false;
		public void OnShow() => moduleBreakpointsVM.IsOpen = true;
		public void OnHidden() => moduleBreakpointsVM.IsVisible = false;
		public void OnVisible() => moduleBreakpointsVM.IsVisible = true;
	}
}
