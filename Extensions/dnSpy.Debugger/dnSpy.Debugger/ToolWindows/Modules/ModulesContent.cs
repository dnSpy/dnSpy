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
using dnSpy.Contracts.Utilities;

namespace dnSpy.Debugger.ToolWindows.Modules {
	interface IModulesContent : IUIObjectProvider {
		void Focus();
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
		readonly IDocumentTabService documentTabService;

		sealed class ControlVM {
			public IModulesVM VM { get; }
			ModulesOperations Operations { get; }

			public ControlVM(IModulesVM vm, ModulesOperations operations) {
				VM = vm;
				Operations = operations;
			}
		}

		[ImportingConstructor]
		ModulesContent(IWpfCommandService wpfCommandService, IModulesVM modulesVM, ModulesOperations modulesOperations, IDocumentTabService documentTabService) {
			Operations = modulesOperations;
			modulesControl = new ModulesControl();
			this.documentTabService = documentTabService;
			modulesControl.DataContext = new ControlVM(modulesVM, modulesOperations);
			modulesControl.ModulesListViewDoubleClick += ModulesControl_ModulesListViewDoubleClick;

			wpfCommandService.Add(ControlConstants.GUID_DEBUGGER_MODULES_CONTROL, modulesControl);
			wpfCommandService.Add(ControlConstants.GUID_DEBUGGER_MODULES_LISTVIEW, modulesControl.ListView);
		}

		void ModulesControl_ModulesListViewDoubleClick(object sender, EventArgs e) {
			bool newTab = Keyboard.Modifiers == ModifierKeys.Shift || Keyboard.Modifiers == ModifierKeys.Control;
			//TODO:
		}

		public void Focus() => UIUtilities.FocusSelector(modulesControl.ListView);
	}
}
