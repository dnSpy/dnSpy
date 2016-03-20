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
using dnSpy.Debugger.IMModules;
using dnSpy.Shared.MVVM;

namespace dnSpy.Debugger.Modules {
	interface IModulesContent {
		object UIObject { get; }
		IInputElement FocusedElement { get; }
		FrameworkElement ScaleElement { get; }
		void OnShow();
		void OnClose();
		void OnVisible();
		void OnHidden();
		void Focus();
		ListView ListView { get; }
		IModulesVM ModulesVM { get; }
	}

	[Export, Export(typeof(IModulesContent)), PartCreationPolicy(CreationPolicy.Shared)]
	sealed class ModulesContent : IModulesContent {
		public object UIObject {
			get { return modulesControl; }
		}

		public IInputElement FocusedElement {
			get { return modulesControl.ListView; }
		}

		public FrameworkElement ScaleElement {
			get { return modulesControl; }
		}

		public ListView ListView {
			get { return modulesControl.ListView; }
		}

		public IModulesVM ModulesVM {
			get { return vmModules; }
		}

		readonly ModulesControl modulesControl;
		readonly IModulesVM vmModules;
		readonly IFileTabManager fileTabManager;
		readonly Lazy<ModuleLoader> moduleLoader;
		readonly Lazy<IInMemoryModuleManager> inMemoryModuleManager;

		[ImportingConstructor]
		ModulesContent(IWpfCommandManager wpfCommandManager, IThemeManager themeManager, IModulesVM modulesVM, IFileTabManager fileTabManager, Lazy<ModuleLoader> moduleLoader, Lazy<IInMemoryModuleManager> inMemoryModuleManager) {
			this.modulesControl = new ModulesControl();
			this.vmModules = modulesVM;
			this.fileTabManager = fileTabManager;
			this.moduleLoader = moduleLoader;
			this.inMemoryModuleManager = inMemoryModuleManager;
			this.modulesControl.DataContext = this.vmModules;
			this.modulesControl.ModulesListViewDoubleClick += ModulesControl_ModulesListViewDoubleClick;
			themeManager.ThemeChanged += ThemeManager_ThemeChanged;

			wpfCommandManager.Add(CommandConstants.GUID_DEBUGGER_MODULES_CONTROL, modulesControl);
			wpfCommandManager.Add(CommandConstants.GUID_DEBUGGER_MODULES_LISTVIEW, modulesControl.ListView);
		}

		void ModulesControl_ModulesListViewDoubleClick(object sender, EventArgs e) {
			bool newTab = Keyboard.Modifiers == ModifierKeys.Shift || Keyboard.Modifiers == ModifierKeys.Control;
			GoToModuleModulesCtxMenuCommand.ExecuteInternal(fileTabManager, inMemoryModuleManager, moduleLoader, modulesControl.ListView.SelectedItem as ModuleVM, newTab);
		}

		void ThemeManager_ThemeChanged(object sender, ThemeChangedEventArgs e) {
			vmModules.RefreshThemeFields();
		}

		public void Focus() {
			UIUtils.FocusSelector(modulesControl.ListView);
		}

		public void OnClose() {
			vmModules.IsEnabled = false;
		}

		public void OnShow() {
			vmModules.IsEnabled = true;
		}

		public void OnHidden() {
			vmModules.IsVisible = false;
		}

		public void OnVisible() {
			vmModules.IsVisible = true;
		}
	}
}
