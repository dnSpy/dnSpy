/*
    Copyright (C) 2014-2016 de4dot@gmail.com

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
using dnSpy.Contracts.Documents.Tabs;
using dnSpy.Contracts.Metadata;
using dnSpy.Contracts.Utilities;

namespace dnSpy.Debugger.CallStack {
	interface ICallStackContent : IUIObjectProvider {
		void OnShow();
		void OnClose();
		void OnVisible();
		void OnHidden();
		void Focus();
		ListView ListView { get; }
		ICallStackVM CallStackVM { get; }
	}

	[Export(typeof(ICallStackContent))]
	sealed class CallStackContent : ICallStackContent {
		public object UIObject => callStackControl;
		public IInputElement FocusedElement => callStackControl.ListView;
		public FrameworkElement ZoomElement => callStackControl;
		public ListView ListView => callStackControl.ListView;
		public ICallStackVM CallStackVM => vmCallStack;

		readonly CallStackControl callStackControl;
		readonly ICallStackVM vmCallStack;
		readonly Lazy<IStackFrameService> stackFrameService;
		readonly IDocumentTabService documentTabService;
		readonly Lazy<IModuleLoader> moduleLoader;
		readonly IModuleIdProvider moduleIdProvider;

		[ImportingConstructor]
		CallStackContent(IWpfCommandService wpfCommandService, ICallStackVM callStackVM, Lazy<IStackFrameService> stackFrameService, IDocumentTabService documentTabService, Lazy<IModuleLoader> moduleLoader, IModuleIdProvider moduleIdProvider) {
			callStackControl = new CallStackControl();
			vmCallStack = callStackVM;
			this.stackFrameService = stackFrameService;
			this.documentTabService = documentTabService;
			this.moduleLoader = moduleLoader;
			this.moduleIdProvider = moduleIdProvider;
			callStackControl.DataContext = vmCallStack;
			callStackControl.CallStackListViewDoubleClick += CallStackControl_CallStackListViewDoubleClick;

			wpfCommandService.Add(ControlConstants.GUID_DEBUGGER_CALLSTACK_CONTROL, callStackControl);
			wpfCommandService.Add(ControlConstants.GUID_DEBUGGER_CALLSTACK_LISTVIEW, callStackControl.ListView);
		}

		void CallStackControl_CallStackListViewDoubleClick(object sender, EventArgs e) {
			bool newTab = Keyboard.Modifiers == ModifierKeys.Shift || Keyboard.Modifiers == ModifierKeys.Control;
			SwitchToFrameCallStackCtxMenuCommand.Execute(moduleIdProvider, stackFrameService.Value, documentTabService, moduleLoader.Value, callStackControl.ListView.SelectedItem as CallStackFrameVM, newTab);
		}

		public void Focus() => UIUtilities.FocusSelector(callStackControl.ListView);
		public void OnClose() => vmCallStack.IsEnabled = false;
		public void OnShow() => vmCallStack.IsEnabled = true;
		public void OnHidden() => vmCallStack.IsVisible = false;
		public void OnVisible() => vmCallStack.IsVisible = true;
	}
}
