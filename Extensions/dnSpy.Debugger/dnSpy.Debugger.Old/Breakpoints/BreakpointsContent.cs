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
using dnSpy.Contracts.Documents.Tabs;
using dnSpy.Contracts.Extension;
using dnSpy.Contracts.Metadata;
using dnSpy.Contracts.Utilities;

namespace dnSpy.Debugger.Breakpoints {
	//[ExportAutoLoaded]
	sealed class BreakpointsContentCommandLoader : IAutoLoaded {
		[ImportingConstructor]
		BreakpointsContentCommandLoader(IWpfCommandService wpfCommandService, CopyBreakpointCtxMenuCommand copyCmd, DeleteBreakpointCtxMenuCommand deleteCmd, GoToSourceBreakpointCtxMenuCommand gotoSrcCmd, GoToSourceNewTabBreakpointCtxMenuCommand gotoSrcNewTabCmd, ToggleEnableBreakpointCtxMenuCommand toggleBpCmd) {
			var cmds = wpfCommandService.GetCommands(ControlConstants.GUID_DEBUGGER_BREAKPOINTS_LISTVIEW);
			cmds.Add(ApplicationCommands.Copy, new BreakpointCtxMenuCommandProxy(copyCmd));
			cmds.Add(ApplicationCommands.Delete, new BreakpointCtxMenuCommandProxy(deleteCmd));
			cmds.Add(new BreakpointCtxMenuCommandProxy(gotoSrcCmd), ModifierKeys.None, Key.Enter);
			cmds.Add(new BreakpointCtxMenuCommandProxy(gotoSrcNewTabCmd), ModifierKeys.Control, Key.Enter);
			cmds.Add(new BreakpointCtxMenuCommandProxy(gotoSrcNewTabCmd), ModifierKeys.Shift, Key.Enter);
			cmds.Add(new BreakpointCtxMenuCommandProxy(toggleBpCmd), ModifierKeys.None, Key.Space);
		}
	}

	interface IBreakpointsContent : IUIObjectProvider {
		void OnShow();
		void OnClose();
		void OnVisible();
		void OnHidden();
		void Focus();
		IBreakpointsVM BreakpointsVM { get; }
		ListView ListView { get; }
	}

	//[Export(typeof(IBreakpointsContent))]
	sealed class BreakpointsContent : IBreakpointsContent {
		public object UIObject => BreakpointsControl;
		public IInputElement FocusedElement => BreakpointsControl.ListView;
		public FrameworkElement ZoomElement => BreakpointsControl;
		public ListView ListView => BreakpointsControl.ListView;

		BreakpointsControl BreakpointsControl {
			get {
				if (breakpointsControl.DataContext == null) {
					breakpointsControl.DataContext = BreakpointsVM;
					breakpointsControl.BreakpointsListViewDoubleClick += BreakpointsControl_BreakpointsListViewDoubleClick;
				}
				return breakpointsControl;
			}
		}
		readonly BreakpointsControl breakpointsControl;

		public IBreakpointsVM BreakpointsVM => vmBreakpoints.Value;

		readonly Lazy<IModuleLoader> moduleLoader;
		readonly IDocumentTabService documentTabService;
		readonly Lazy<IBreakpointsVM> vmBreakpoints;
		readonly IModuleIdProvider moduleIdProvider;

		[ImportingConstructor]
		BreakpointsContent(IWpfCommandService wpfCommandService, Lazy<IBreakpointsVM> breakpointsVM, Lazy<IModuleLoader> moduleLoader, IDocumentTabService documentTabService, IModuleIdProvider moduleIdProvider) {
			breakpointsControl = new BreakpointsControl();
			this.moduleLoader = moduleLoader;
			this.documentTabService = documentTabService;
			vmBreakpoints = breakpointsVM;
			this.moduleIdProvider = moduleIdProvider;

			wpfCommandService.Add(ControlConstants.GUID_DEBUGGER_BREAKPOINTS_CONTROL, breakpointsControl);
			wpfCommandService.Add(ControlConstants.GUID_DEBUGGER_BREAKPOINTS_LISTVIEW, breakpointsControl.ListView);
		}

		void BreakpointsControl_BreakpointsListViewDoubleClick(object sender, EventArgs e) {
			bool newTab = Keyboard.Modifiers == ModifierKeys.Shift || Keyboard.Modifiers == ModifierKeys.Control;
			GoToSourceBreakpointCtxMenuCommand.GoTo(moduleIdProvider, documentTabService, moduleLoader, BreakpointsControl.ListView.SelectedItem as BreakpointVM, newTab);
		}

		public void Focus() => UIUtilities.FocusSelector(BreakpointsControl.ListView);
		public void OnClose() { }
		public void OnHidden() { }
		public void OnShow() { }
		public void OnVisible() { }
	}
}
