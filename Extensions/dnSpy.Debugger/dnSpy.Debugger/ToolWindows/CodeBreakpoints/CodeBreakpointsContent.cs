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

namespace dnSpy.Debugger.ToolWindows.CodeBreakpoints {
	interface ICodeBreakpointsContent : IUIObjectProvider {
		void OnShow();
		void OnClose();
		void OnVisible();
		void OnHidden();
		void Focus();
		void FocusSearchTextBox();
		ListView ListView { get; }
		CodeBreakpointsOperations Operations { get; }
	}

	[Export(typeof(ICodeBreakpointsContent))]
	sealed class CodeBreakpointsContent : ICodeBreakpointsContent {
		public object? UIObject => codeBreakpointsControl;
		public IInputElement? FocusedElement => codeBreakpointsControl.ListView;
		public FrameworkElement? ZoomElement => codeBreakpointsControl;
		public ListView ListView => codeBreakpointsControl.ListView;
		public CodeBreakpointsOperations Operations { get; }

		readonly CodeBreakpointsControl codeBreakpointsControl;
		readonly ICodeBreakpointsVM codeBreakpointsVM;

		sealed class ControlVM : ViewModelBase {
			public ICodeBreakpointsVM VM { get; }
			CodeBreakpointsOperations Operations { get; }

			public string RemoveCodeBreakpointToolTip => ToolTipHelper.AddKeyboardShortcut(dnSpy_Debugger_Resources.Breakpoints_RemoveBreakpoint_ToolTip, null);
			public string RemoveMatchingBreakpointsToolTip => ToolTipHelper.AddKeyboardShortcut(dnSpy_Debugger_Resources.Breakpoints_RemoveMatchingBreakpoints_ToolTip, null);
			public string ToggleMatchingBreakpointsToolTip => ToolTipHelper.AddKeyboardShortcut(dnSpy_Debugger_Resources.Breakpoints_ToggleMatchingBreakpoints_ToolTip, null);
			public string ExportMatchingBreakpointsToolTip => ToolTipHelper.AddKeyboardShortcut(dnSpy_Debugger_Resources.Breakpoints_ExportMatchingBreakpoints_ToolTip, null);
			public string ImportBreakpointsToolTip => ToolTipHelper.AddKeyboardShortcut(dnSpy_Debugger_Resources.Breakpoints_ImportBreakpoints_ToolTip, null);
			public string GoToSourceCodeToolTip => ToolTipHelper.AddKeyboardShortcut(dnSpy_Debugger_Resources.Breakpoints_GoToSourceCode_ToolTip, null);
			public string GoToDisassemblyToolTip => ToolTipHelper.AddKeyboardShortcut(dnSpy_Debugger_Resources.Breakpoints_GoToDisassembly_ToolTip, null);
			public string SearchToolTip => ToolTipHelper.AddKeyboardShortcut(dnSpy_Debugger_Resources.Breakpoints_Search_ToolTip, dnSpy_Debugger_Resources.ShortCutKeyCtrlF);
			public string ResetSearchSettingsToolTip => ToolTipHelper.AddKeyboardShortcut(dnSpy_Debugger_Resources.Breakpoints_ResetSearchSettings_ToolTip, null);
			public string SearchHelpToolTip => ToolTipHelper.AddKeyboardShortcut(dnSpy_Debugger_Resources.SearchHelp_ToolTip, null);

			public ICommand RemoveCodeBreakpointsCommand => new RelayCommand(a => Operations.RemoveCodeBreakpoints(), a => Operations.CanRemoveCodeBreakpoints);
			public ICommand RemoveMatchingBreakpointsCommand => new RelayCommand(a => Operations.RemoveMatchingCodeBreakpoints(), a => Operations.CanRemoveMatchingCodeBreakpoints);
			public ICommand ToggleMatchingBreakpointsCommand => new RelayCommand(a => Operations.ToggleMatchingBreakpoints(), a => Operations.CanToggleMatchingBreakpoints);
			public ICommand ExportMatchingBreakpointsCommand => new RelayCommand(a => Operations.ExportMatchingBreakpoints(), a => Operations.CanExportMatchingBreakpoints);
			public ICommand ResetSearchSettingsCommand => new RelayCommand(a => Operations.ResetSearchSettings(), a => Operations.CanResetSearchSettings);
			public ICommand ImportBreakpointsCommand => new RelayCommand(a => Operations.ImportBreakpoints(), a => Operations.CanImportBreakpoints);
			public ICommand GoToSourceCodeCommand => new RelayCommand(a => Operations.GoToSourceCode(false), a => Operations.CanGoToSourceCode);
			public ICommand GoToDisassemblyCommand => new RelayCommand(a => Operations.GoToDisassembly(), a => Operations.CanGoToDisassembly);
			public ICommand SearchHelpCommand => new RelayCommand(a => SearchHelp());

			readonly IMessageBoxService messageBoxService;
			readonly DependencyObject control;

			public ControlVM(ICodeBreakpointsVM vm, CodeBreakpointsOperations operations, IMessageBoxService messageBoxService, DependencyObject control) {
				VM = vm;
				Operations = operations;
				this.messageBoxService = messageBoxService;
				this.control = control;
			}

			void SearchHelp() => messageBoxService.Show(VM.GetSearchHelpText(), ownerWindow: Window.GetWindow(control));
		}

		[ImportingConstructor]
		CodeBreakpointsContent(IWpfCommandService wpfCommandService, ICodeBreakpointsVM codeBreakpointsVM, CodeBreakpointsOperations codeBreakpointsOperations, IMessageBoxService messageBoxService) {
			Operations = codeBreakpointsOperations;
			codeBreakpointsControl = new CodeBreakpointsControl();
			this.codeBreakpointsVM = codeBreakpointsVM;
			codeBreakpointsControl.DataContext = new ControlVM(codeBreakpointsVM, codeBreakpointsOperations, messageBoxService, codeBreakpointsControl);
			codeBreakpointsControl.CodeBreakpointsListViewDoubleClick += CodeBreakpointsControl_CodeBreakpointsListViewDoubleClick;

			wpfCommandService.Add(ControlConstants.GUID_DEBUGGER_CODEBREAKPOINTS_CONTROL, codeBreakpointsControl);
			wpfCommandService.Add(ControlConstants.GUID_DEBUGGER_CODEBREAKPOINTS_LISTVIEW, codeBreakpointsControl.ListView);

			codeBreakpointsControl.ListView.PreviewKeyDown += ListView_PreviewKeyDown;
		}

		void CodeBreakpointsControl_CodeBreakpointsListViewDoubleClick(object? sender, EventArgs e) {
			bool newTab = Keyboard.Modifiers == ModifierKeys.Shift || Keyboard.Modifiers == ModifierKeys.Control;
			if (!Operations.IsEditingValues && Operations.CanGoToSourceCode)
				Operations.GoToSourceCode(newTab);
		}

		void ListView_PreviewKeyDown(object? sender, KeyEventArgs e) {
			if (!e.Handled) {
				// Use a KeyDown handler. If we add this as a key command to the listview, the textview
				// (used when editing eg. labels) won't see the space.
				if (e.Key == Key.Space && e.KeyboardDevice.Modifiers == ModifierKeys.None) {
					if (Operations.CanToggleEnabled) {
						Operations.ToggleEnabled();
						e.Handled = true;
						return;
					}
				}
			}
		}

		public void FocusSearchTextBox() => codeBreakpointsControl.FocusSearchTextBox();
		public void Focus() => UIUtilities.FocusSelector(codeBreakpointsControl.ListView);
		public void OnClose() => codeBreakpointsVM.IsOpen = false;
		public void OnShow() => codeBreakpointsVM.IsOpen = true;
		public void OnHidden() => codeBreakpointsVM.IsVisible = false;
		public void OnVisible() => codeBreakpointsVM.IsVisible = true;
	}
}
