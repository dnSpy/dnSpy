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

namespace dnSpy.Debugger.ToolWindows.Threads {
	interface IThreadsContent : IUIObjectProvider {
		void OnShow();
		void OnClose();
		void OnVisible();
		void OnHidden();
		void Focus();
		void FocusSearchTextBox();
		ListView ListView { get; }
		ThreadsOperations Operations { get; }
	}

	[Export(typeof(IThreadsContent))]
	sealed class ThreadsContent : IThreadsContent {
		public object? UIObject => threadsControl;
		public IInputElement? FocusedElement => threadsControl.ListView;
		public FrameworkElement? ZoomElement => threadsControl;
		public ListView ListView => threadsControl.ListView;
		public ThreadsOperations Operations { get; }

		readonly ThreadsControl threadsControl;
		readonly IThreadsVM threadsVM;

		sealed class ControlVM : ViewModelBase {
			public IThreadsVM VM { get; }
			ThreadsOperations Operations { get; }

			public string FreezeThreadsToolTip => ToolTipHelper.AddKeyboardShortcut(dnSpy_Debugger_Resources.Threads_FreezeThreads_ToolTip, null);
			public string ThawThreadsToolTip => ToolTipHelper.AddKeyboardShortcut(dnSpy_Debugger_Resources.Threads_ThawThreads_ToolTip, null);
			public string SearchToolTip => ToolTipHelper.AddKeyboardShortcut(dnSpy_Debugger_Resources.Threads_Search_ToolTip, dnSpy_Debugger_Resources.ShortCutKeyCtrlF);
			public string ResetSearchSettingsToolTip => ToolTipHelper.AddKeyboardShortcut(dnSpy_Debugger_Resources.Threads_ResetSearchSettings_ToolTip, null);
			public string SearchHelpToolTip => ToolTipHelper.AddKeyboardShortcut(dnSpy_Debugger_Resources.SearchHelp_ToolTip, null);

			public ICommand FreezeThreadsCommand => new RelayCommand(a => Operations.FreezeThread(), a => Operations.CanFreezeThread);
			public ICommand ThawThreadsCommand => new RelayCommand(a => Operations.ThawThread(), a => Operations.CanThawThread);
			public ICommand ResetSearchSettingsCommand => new RelayCommand(a => Operations.ResetSearchSettings(), a => Operations.CanResetSearchSettings);
			public ICommand SearchHelpCommand => new RelayCommand(a => SearchHelp());

			readonly IMessageBoxService messageBoxService;
			readonly DependencyObject control;

			public ControlVM(IThreadsVM vm, ThreadsOperations operations, IMessageBoxService messageBoxService, DependencyObject control) {
				VM = vm;
				Operations = operations;
				this.messageBoxService = messageBoxService;
				this.control = control;
			}

			void SearchHelp() => messageBoxService.Show(VM.GetSearchHelpText(), ownerWindow: Window.GetWindow(control));
		}

		[ImportingConstructor]
		ThreadsContent(IWpfCommandService wpfCommandService, IThreadsVM threadsVM, ThreadsOperations threadsOperations, IMessageBoxService messageBoxService) {
			Operations = threadsOperations;
			threadsControl = new ThreadsControl();
			this.threadsVM = threadsVM;
			threadsControl.DataContext = new ControlVM(threadsVM, threadsOperations, messageBoxService, threadsControl);
			threadsControl.ThreadsListViewDoubleClick += ThreadsControl_ThreadsListViewDoubleClick;

			wpfCommandService.Add(ControlConstants.GUID_DEBUGGER_THREADS_CONTROL, threadsControl);
			wpfCommandService.Add(ControlConstants.GUID_DEBUGGER_THREADS_LISTVIEW, threadsControl.ListView);
		}

		void ThreadsControl_ThreadsListViewDoubleClick(object? sender, EventArgs e) {
			bool newTab = Keyboard.Modifiers == ModifierKeys.Shift || Keyboard.Modifiers == ModifierKeys.Control;
			if (!Operations.IsEditingValues && Operations.CanSwitchToThread)
				Operations.SwitchToThread(newTab);
		}

		public void FocusSearchTextBox() => threadsControl.FocusSearchTextBox();
		public void Focus() => UIUtilities.FocusSelector(threadsControl.ListView);
		public void OnClose() => threadsVM.IsOpen = false;
		public void OnShow() => threadsVM.IsOpen = true;
		public void OnHidden() => threadsVM.IsVisible = false;
		public void OnVisible() => threadsVM.IsVisible = true;
	}
}
