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
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using dnSpy.Contracts.Languages;
using dnSpy.Contracts.Menus;
using dnSpy.Contracts.ToolBars;

namespace dnSpy.MainApp {
	[ExportToolBarObject(OwnerGuid = ToolBarConstants.APP_TB_GUID, Group = ToolBarConstants.GROUP_APP_TB_MAIN_MENU, Order = 0)]
	sealed class MainMenuToolbarCommand : ToolBarObjectBase {
		readonly IMenuManager menuManager;
		Menu menu;

		[ImportingConstructor]
		MainMenuToolbarCommand(IMenuManager menuManager) {
			this.menuManager = menuManager;
		}

		public override object GetUIObject(IToolBarItemContext context, IInputElement commandTarget) {
			if (menu == null)
				menu = menuManager.CreateMenu(new Guid(MenuConstants.APP_MENU_GUID), commandTarget);
			return menu;
		}
	}

	[ExportToolBarObject(OwnerGuid = ToolBarConstants.APP_TB_GUID, Group = ToolBarConstants.GROUP_APP_TB_MAIN_LANGUAGE, Order = 0)]
	sealed class LanguageComboBoxToolbarCommand : ToolBarObjectBase, INotifyPropertyChanged {
		readonly ComboBox comboBox;
		readonly List<LanguageInfo> infos;
		readonly ILanguageManager languageManager;

		public event PropertyChangedEventHandler PropertyChanged;

		public object SelectedItem {
			get { return selectedItem; }
			set {
				if (selectedItem != value) {
					selectedItem = value;
					languageManager.Language = ((LanguageInfo)value).Language;
					PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedItem)));
				}
			}
		}
		object selectedItem;

		sealed class LanguageInfo {
			public ILanguage Language;
			public string Name => Language.UniqueNameUI;
			public override string ToString() => Name;
		}

		[ImportingConstructor]
		LanguageComboBoxToolbarCommand(ILanguageManager languageManager) {
			this.languageManager = languageManager;
			this.infos = languageManager.AllLanguages.OrderBy(a => a.OrderUI).Select(a => new LanguageInfo { Language = a }).ToList();
			UpdateSelectedItem();
			this.comboBox = new ComboBox {
				DisplayMemberPath = "Name",
				Width = 90,
				ItemsSource = infos,
			};
			this.comboBox.SetBinding(Selector.SelectedItemProperty, new Binding(nameof(SelectedItem)) {
				Source = this,
			});
			languageManager.LanguageChanged += LanguageManager_LanguageChanged;
		}

		void UpdateSelectedItem() => SelectedItem = infos.First(a => a.Language == languageManager.Language);
		void LanguageManager_LanguageChanged(object sender, EventArgs e) => UpdateSelectedItem();
		public override object GetUIObject(IToolBarItemContext context, IInputElement commandTarget) => comboBox;
	}

	[ExportToolBarButton(OwnerGuid = ToolBarConstants.APP_TB_GUID, Icon = "FullScreen", Header = "res:FullScreenToolBarCommand", IsToggleButton = true, Group = ToolBarConstants.GROUP_APP_TB_MAIN_FULLSCREEN, Order = 0)]
	sealed class FullScreenToolbarCommand : ToolBarButtonBase, IToolBarToggleButton {
		[Import]
		AppWindow appWindow = null;

		public Binding GetBinding(IToolBarItemContext context) {
			Debug.Assert(appWindow != null && appWindow.MainWindow != null);
			return new Binding(nameof(appWindow.MainWindow.IsFullScreen)) {
				Source = appWindow.MainWindow,
			};
		}

		public override void Execute(IToolBarItemContext context) {
		}

		bool initd = false;
		public override bool IsVisible(IToolBarItemContext context) {
			Debug.Assert(appWindow != null && appWindow.MainWindow != null);
			if (!initd) {
				appWindow.MainWindow.IsFullScreenChanged += (s, e) => appWindow.RefreshToolBar();
				initd = true;
			}
			return appWindow.MainWindow.IsFullScreen;
		}
	}
}
