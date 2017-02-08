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
using dnSpy.Contracts.Utilities;
using dnSpy.Debugger.IMModules;

namespace dnSpy.Debugger.Modules {
	interface IModulesContent : IUIObjectProvider {
		void OnShow();
		void OnClose();
		void OnVisible();
		void OnHidden();
		void Focus();
		ListView ListView { get; }
		IModulesVM ModulesVM { get; }
	}

	//[Export(typeof(IModulesContent))]
	sealed class ModulesContent : IModulesContent {
		public object UIObject => modulesControl;
		public IInputElement FocusedElement => modulesControl.ListView;
		public FrameworkElement ZoomElement => modulesControl;
		public ListView ListView => modulesControl.ListView;
		public IModulesVM ModulesVM => vmModules;

		readonly ModulesControl modulesControl;
		readonly IModulesVM vmModules;
		readonly IDocumentTabService documentTabService;
		readonly Lazy<IModuleLoader> moduleLoader;
		readonly Lazy<IInMemoryModuleService> inMemoryModuleService;

		[ImportingConstructor]
		ModulesContent(IWpfCommandService wpfCommandService, IModulesVM modulesVM, IDocumentTabService documentTabService, Lazy<IModuleLoader> moduleLoader, Lazy<IInMemoryModuleService> inMemoryModuleService) {
			modulesControl = new ModulesControl();
			vmModules = modulesVM;
			this.documentTabService = documentTabService;
			this.moduleLoader = moduleLoader;
			this.inMemoryModuleService = inMemoryModuleService;
			modulesControl.DataContext = vmModules;
			modulesControl.ModulesListViewDoubleClick += ModulesControl_ModulesListViewDoubleClick;

			wpfCommandService.Add(ControlConstants.GUID_DEBUGGER_MODULES_CONTROL, modulesControl);
			wpfCommandService.Add(ControlConstants.GUID_DEBUGGER_MODULES_LISTVIEW, modulesControl.ListView);
		}

		void ModulesControl_ModulesListViewDoubleClick(object sender, EventArgs e) {
			bool newTab = Keyboard.Modifiers == ModifierKeys.Shift || Keyboard.Modifiers == ModifierKeys.Control;
			GoToModuleModulesCtxMenuCommand.ExecuteInternal(documentTabService, inMemoryModuleService, moduleLoader, modulesControl.ListView.SelectedItem as ModuleVM, newTab);
		}

		public void Focus() => UIUtilities.FocusSelector(modulesControl.ListView);
		public void OnClose() => vmModules.IsEnabled = false;
		public void OnShow() => vmModules.IsEnabled = true;
		public void OnHidden() => vmModules.IsVisible = false;
		public void OnVisible() => vmModules.IsVisible = true;
	}
}
