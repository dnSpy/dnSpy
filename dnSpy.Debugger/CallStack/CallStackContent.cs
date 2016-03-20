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
using dnSpy.Contracts.Files.Tabs;
using dnSpy.Contracts.Themes;
using dnSpy.Shared.MVVM;

namespace dnSpy.Debugger.CallStack {
	interface ICallStackContent {
		object UIObject { get; }
		IInputElement FocusedElement { get; }
		FrameworkElement ScaleElement { get; }
		void OnShow();
		void OnClose();
		void OnVisible();
		void OnHidden();
		void Focus();
		ListView ListView { get; }
		ICallStackVM CallStackVM { get; }
	}

	[Export, Export(typeof(ICallStackContent)), PartCreationPolicy(CreationPolicy.Shared)]
	sealed class CallStackContent : ICallStackContent {
		public object UIObject {
			get { return callStackControl; }
		}

		public IInputElement FocusedElement {
			get { return callStackControl.ListView; }
		}

		public FrameworkElement ScaleElement {
			get { return callStackControl; }
		}

		public ListView ListView {
			get { return callStackControl.ListView; }
		}

		public ICallStackVM CallStackVM {
			get { return vmCallStack; }
		}

		readonly CallStackControl callStackControl;
		readonly ICallStackVM vmCallStack;
		readonly Lazy<IStackFrameManager> stackFrameManager;
		readonly IFileTabManager fileTabManager;
		readonly Lazy<IModuleLoader> moduleLoader;

		[ImportingConstructor]
		CallStackContent(IWpfCommandManager wpfCommandManager, IThemeManager themeManager, ICallStackVM callStackVM, Lazy<IStackFrameManager> stackFrameManager, IFileTabManager fileTabManager, Lazy<IModuleLoader> moduleLoader) {
			this.callStackControl = new CallStackControl();
			this.vmCallStack = callStackVM;
			this.stackFrameManager = stackFrameManager;
			this.fileTabManager = fileTabManager;
			this.moduleLoader = moduleLoader;
			this.callStackControl.DataContext = this.vmCallStack;
			this.callStackControl.CallStackListViewDoubleClick += CallStackControl_CallStackListViewDoubleClick;
			themeManager.ThemeChanged += ThemeManager_ThemeChanged;

			wpfCommandManager.Add(CommandConstants.GUID_DEBUGGER_CALLSTACK_CONTROL, callStackControl);
			wpfCommandManager.Add(CommandConstants.GUID_DEBUGGER_CALLSTACK_LISTVIEW, callStackControl.ListView);
		}

		void CallStackControl_CallStackListViewDoubleClick(object sender, EventArgs e) {
			bool newTab = Keyboard.Modifiers == ModifierKeys.Shift || Keyboard.Modifiers == ModifierKeys.Control;
			SwitchToFrameCallStackCtxMenuCommand.Execute(stackFrameManager.Value, fileTabManager, moduleLoader.Value, callStackControl.ListView.SelectedItem as CallStackFrameVM, newTab);
		}

		void ThemeManager_ThemeChanged(object sender, ThemeChangedEventArgs e) {
			vmCallStack.RefreshThemeFields();
		}

		public void Focus() {
			UIUtils.FocusSelector(callStackControl.ListView);
		}

		public void OnClose() {
			vmCallStack.IsEnabled = false;
		}

		public void OnShow() {
			vmCallStack.IsEnabled = true;
		}

		public void OnHidden() {
			vmCallStack.IsVisible = false;
		}

		public void OnVisible() {
			vmCallStack.IsVisible = true;
		}
	}
}
