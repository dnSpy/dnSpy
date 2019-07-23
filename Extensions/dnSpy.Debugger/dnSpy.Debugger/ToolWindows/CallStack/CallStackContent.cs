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
using dnSpy.Contracts.Controls;
using dnSpy.Contracts.MVVM;
using dnSpy.Contracts.Utilities;

namespace dnSpy.Debugger.ToolWindows.CallStack {
	interface ICallStackContent : IUIObjectProvider {
		void OnShow();
		void OnClose();
		void OnVisible();
		void OnHidden();
		void Focus();
		ListView ListView { get; }
		CallStackOperations Operations { get; }
	}

	[Export(typeof(ICallStackContent))]
	sealed class CallStackContent : ICallStackContent {
		public object? UIObject => callStackControl;
		public IInputElement? FocusedElement => callStackControl.ListView;
		public FrameworkElement? ZoomElement => callStackControl;
		public ListView ListView => callStackControl.ListView;
		public CallStackOperations Operations { get; }

		readonly CallStackControl callStackControl;
		readonly ICallStackVM callStackVM;

		sealed class ControlVM : ViewModelBase {
			public ICallStackVM VM { get; }
			CallStackOperations Operations { get; }

			public ControlVM(ICallStackVM vm, CallStackOperations operations) {
				VM = vm;
				Operations = operations;
			}
		}

		[ImportingConstructor]
		CallStackContent(IWpfCommandService wpfCommandService, ICallStackVM callStackVM, CallStackOperations callStackOperations) {
			Operations = callStackOperations;
			callStackControl = new CallStackControl();
			this.callStackVM = callStackVM;
			callStackControl.DataContext = new ControlVM(callStackVM, callStackOperations);
			callStackControl.CallStackListViewDoubleClick += CallStackControl_CallStackListViewDoubleClick;

			wpfCommandService.Add(ControlConstants.GUID_DEBUGGER_CALLSTACK_CONTROL, callStackControl);
			wpfCommandService.Add(ControlConstants.GUID_DEBUGGER_CALLSTACK_LISTVIEW, callStackControl.ListView);
		}

		void CallStackControl_CallStackListViewDoubleClick(object? sender, EventArgs e) {
			bool newTab = Keyboard.Modifiers == ModifierKeys.Shift || Keyboard.Modifiers == ModifierKeys.Control;
			if (Operations.CanSwitchToFrame)
				Operations.SwitchToFrame(newTab);
		}

		public void Focus() => UIUtilities.FocusSelector(callStackControl.ListView);
		public void OnClose() => callStackVM.IsOpen = false;
		public void OnShow() => callStackVM.IsOpen = true;
		public void OnHidden() => callStackVM.IsVisible = false;
		public void OnVisible() => callStackVM.IsVisible = true;
	}
}
