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
using dnSpy.Contracts.Utilities;

namespace dnSpy.Debugger.ToolWindows.Threads {
	interface IThreadsContent : IUIObjectProvider {
		void OnShow();
		void OnClose();
		void OnVisible();
		void OnHidden();
		void Focus();
		ListView ListView { get; }
		ThreadsOperations Operations { get; }
	}

	[Export(typeof(IThreadsContent))]
	sealed class ThreadsContent : IThreadsContent {
		public object UIObject => threadsControl;
		public IInputElement FocusedElement => threadsControl.ListView;
		public FrameworkElement ZoomElement => threadsControl;
		public ListView ListView => threadsControl.ListView;
		public ThreadsOperations Operations { get; }

		readonly ThreadsControl threadsControl;
		readonly IThreadsVM threadsVM;

		sealed class ControlVM {
			public IThreadsVM VM { get; }
			ThreadsOperations Operations { get; }

			public ControlVM(IThreadsVM vm, ThreadsOperations operations) {
				VM = vm;
				Operations = operations;
			}
		}

		[ImportingConstructor]
		ThreadsContent(IWpfCommandService wpfCommandService, IThreadsVM threadsVM, ThreadsOperations threadsOperations) {
			Operations = threadsOperations;
			threadsControl = new ThreadsControl();
			this.threadsVM = threadsVM;
			threadsControl.DataContext = new ControlVM(threadsVM, threadsOperations);
			threadsControl.ThreadsListViewDoubleClick += ThreadsControl_ThreadsListViewDoubleClick;

			wpfCommandService.Add(ControlConstants.GUID_DEBUGGER_THREADS_CONTROL, threadsControl);
			wpfCommandService.Add(ControlConstants.GUID_DEBUGGER_THREADS_LISTVIEW, threadsControl.ListView);
		}

		void ThreadsControl_ThreadsListViewDoubleClick(object sender, EventArgs e) {
			bool newTab = Keyboard.Modifiers == ModifierKeys.Shift || Keyboard.Modifiers == ModifierKeys.Control;
			//TODO:
		}

		public void Focus() => UIUtilities.FocusSelector(threadsControl.ListView);
		public void OnClose() => threadsVM.IsEnabled = false;
		public void OnShow() => threadsVM.IsEnabled = true;
		public void OnHidden() => threadsVM.IsVisible = false;
		public void OnVisible() => threadsVM.IsVisible = true;
	}
}
