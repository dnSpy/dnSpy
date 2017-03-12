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
using System.ComponentModel.Composition;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using dnSpy.Contracts.Controls;
using dnSpy.Contracts.MVVM;
using dnSpy.Contracts.Utilities;
using dnSpy.Debugger.Properties;

namespace dnSpy.Debugger.ToolWindows.Exceptions {
	interface IExceptionsContent : IUIObjectProvider {
		void OnShow();
		void OnClose();
		void OnVisible();
		void OnHidden();
		void Focus();
		void FocusSearchTextBox();
		ExceptionsOperations Operations { get; }
		ListView ListView { get; }
	}

	[Export(typeof(IExceptionsContent))]
	sealed class ExceptionsContent : IExceptionsContent {
		public object UIObject => exceptionsControl;
		public IInputElement FocusedElement => exceptionsControl.ListView;
		public FrameworkElement ZoomElement => exceptionsControl;
		public ListView ListView => exceptionsControl.ListView;
		public ExceptionsOperations Operations { get; }

		readonly ExceptionsControl exceptionsControl;
		readonly IExceptionsVM exceptionsVM;

		sealed class ControlVM {
			public IExceptionsVM VM { get; }
			public ExceptionsOperations Operations { get; }

			public string ShowOnlyEnabledExceptionsToolTip => ToolTipHelper.AddKeyboardShortcut(dnSpy_Debugger_Resources.Exceptions_ShowOnlyEnabledExceptions_ToolTip, null);
			public string AddExceptionToolTip => ToolTipHelper.AddKeyboardShortcut(dnSpy_Debugger_Resources.Exceptions_Add_ToolTip, null);
			public string RemoveExceptionsToolTip => ToolTipHelper.AddKeyboardShortcut(dnSpy_Debugger_Resources.Exceptions_Remove_ToolTip, null);
			public string EditConditionsToolTip => ToolTipHelper.AddKeyboardShortcut(dnSpy_Debugger_Resources.Exceptions_EditConditions_ToolTip, null);
			public string RestoreSettingsToolTip => ToolTipHelper.AddKeyboardShortcut(dnSpy_Debugger_Resources.Exceptions_RestoreSettings_ToolTip, null);
			public string ResetSearchSettingsToolTip => ToolTipHelper.AddKeyboardShortcut(dnSpy_Debugger_Resources.Exceptions_ResetSearchSettings_ToolTip, null);
			public string SearchToolTip => ToolTipHelper.AddKeyboardShortcut(dnSpy_Debugger_Resources.Exceptions_Search_ToolTip, dnSpy_Debugger_Resources.ShortCutKeyCtrlF);

			public ICommand AddExceptionCommand => new RelayCommand(a => Operations.AddException(), a => Operations.CanAddException);
			public ICommand RemoveExceptionsCommand => new RelayCommand(a => Operations.RemoveExceptions(), a => Operations.CanRemoveExceptions);
			public ICommand EditConditionsCommand => new RelayCommand(a => Operations.EditConditions(), a => Operations.CanEditConditions);
			public ICommand RestoreSettingsCommand => new RelayCommand(a => Operations.RestoreSettings(), a => Operations.CanRestoreSettings);
			public ICommand ResetSearchSettingsCommand => new RelayCommand(a => Operations.ResetSearchSettings(), a => Operations.CanResetSearchSettings);

			public ControlVM(IExceptionsVM vm, ExceptionsOperations operations) {
				VM = vm;
				Operations = operations;
			}
		}

		[ImportingConstructor]
		ExceptionsContent(IWpfCommandService wpfCommandService, IExceptionsVM exceptionsVM, ExceptionsOperations exceptionsOperations) {
			Operations = exceptionsOperations;
			exceptionsControl = new ExceptionsControl();
			this.exceptionsVM = exceptionsVM;
			exceptionsControl.DataContext = new ControlVM(exceptionsVM, exceptionsOperations);
			exceptionsControl.ExceptionsListViewDoubleClick += ExceptionsControl_ExceptionsListViewDoubleClick;

			wpfCommandService.Add(ControlConstants.GUID_DEBUGGER_EXCEPTIONS_CONTROL, exceptionsControl);
			wpfCommandService.Add(ControlConstants.GUID_DEBUGGER_EXCEPTIONS_LISTVIEW, exceptionsControl.ListView);
		}

		void ExceptionsControl_ExceptionsListViewDoubleClick(object sender, EventArgs e) {
			//TODO: Show edit conditions dlg box
		}

		public void FocusSearchTextBox() => exceptionsControl.FocusSearchTextBox();
		public void Focus() => UIUtilities.FocusSelector(exceptionsControl.ListView);
		public void OnClose() => exceptionsVM.IsEnabled = false;
		public void OnShow() => exceptionsVM.IsEnabled = true;
		public void OnHidden() => exceptionsVM.IsVisible = false;
		public void OnVisible() => exceptionsVM.IsVisible = true;
	}
}
