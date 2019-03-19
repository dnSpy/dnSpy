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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Linq;
using dnSpy.Contracts.MVVM;
using dnSpy.Contracts.Settings.Dialog;
using dnSpy.Debugger.Evaluation;
using dnSpy.Debugger.Properties;

namespace dnSpy.Debugger.Settings {
	[Export(typeof(IAppSettingsPageProvider))]
	sealed class DebuggerAppSettingsPageProvider : IAppSettingsPageProvider {
		readonly DebuggerSettingsImpl debuggerSettingsImpl;
		readonly Lazy<DbgLanguageService2> dbgLanguageService;

		[ImportingConstructor]
		DebuggerAppSettingsPageProvider(DebuggerSettingsImpl debuggerSettingsImpl, Lazy<DbgLanguageService2> dbgLanguageService) {
			this.debuggerSettingsImpl = debuggerSettingsImpl;
			this.dbgLanguageService = dbgLanguageService;
		}

		public IEnumerable<AppSettingsPage> Create() {
			yield return new DebuggerAppSettingsPage(debuggerSettingsImpl, dbgLanguageService);
		}
	}

	sealed class DebuggerAppSettingsPage : AppSettingsPage {
		readonly DebuggerSettingsImpl _global_settings;

		internal static readonly Guid PageGuid = new Guid("8D2BC2FB-5CA4-4907-84C7-F4F705327AC8");
		public override Guid Guid => PageGuid;
		public DebuggerSettingsBase Settings { get; }
		public override double Order => AppSettingsConstants.ORDER_DEBUGGER;
		public override string Title => dnSpy_Debugger_Resources.DebuggerOptDlgTab;
		public override object UIObject => this;

		public object Runtimes {
			get {
				if (runtimesVM == null)
					runtimesVM = new RuntimesVM(dbgLanguageService.Value.GetLanguageInfos());
				return runtimesVM;
			}
		}
		RuntimesVM runtimesVM;

		readonly Lazy<DbgLanguageService2> dbgLanguageService;

		public DebuggerAppSettingsPage(DebuggerSettingsImpl debuggerSettingsImpl, Lazy<DbgLanguageService2> dbgLanguageService) {
			_global_settings = debuggerSettingsImpl;
			this.dbgLanguageService = dbgLanguageService;
			Settings = debuggerSettingsImpl.Clone();
		}

		public override void OnApply() {
			Settings.CopyTo(_global_settings);
			if (runtimesVM != null) {
				foreach (var info in runtimesVM.GetSettings()) {
					var language = dbgLanguageService.Value.GetLanguages(info.runtimeKindGuid).First(a => a.Name == info.languageName);
					dbgLanguageService.Value.SetCurrentLanguage(info.runtimeKindGuid, language);
				}
			}
		}
	}

	sealed class RuntimesVM : ViewModelBase {
		public object Items => runtimes;
		readonly ObservableCollection<RuntimeVM> runtimes;
		public object SelectedItem {
			get => selectedItem;
			set {
				if (selectedItem == value)
					return;
				selectedItem = (RuntimeVM)value;
				OnPropertyChanged(nameof(SelectedItem));
			}
		}
		RuntimeVM selectedItem;

		public RuntimesVM(RuntimeLanguageInfo[] infos) {
			runtimes = new ObservableCollection<RuntimeVM>(infos.OrderBy(a => a.RuntimeDisplayName, StringComparer.CurrentCultureIgnoreCase).Select(a => new RuntimeVM(a)));
			selectedItem = runtimes.FirstOrDefault();
		}

		public IEnumerable<(Guid runtimeKindGuid, string languageName)> GetSettings() {
			foreach (var runtime in runtimes)
				yield return runtime.GetSettings();
		}
	}

	sealed class RuntimeVM : ViewModelBase {
		public string Name { get; }
		public object Languages => languages;
		readonly ObservableCollection<LanguageVM> languages;
		public object SelectedItem {
			get => selectedItem;
			set {
				if (selectedItem == value)
					return;
				selectedItem = (LanguageVM)value;
				OnPropertyChanged(nameof(SelectedItem));
			}
		}
		LanguageVM selectedItem;

		readonly Guid runtimeKindGuid;

		public RuntimeVM(RuntimeLanguageInfo info) {
			Name = info.RuntimeDisplayName;
			runtimeKindGuid = info.RuntimeKindGuid;
			languages = new ObservableCollection<LanguageVM>(info.Languages.OrderBy(a => a.LanguageDisplayName, StringComparer.CurrentCultureIgnoreCase).Select(a => new LanguageVM(a)));
			selectedItem = languages.FirstOrDefault(a => a.ID == info.CurrentLanguage) ?? languages.FirstOrDefault();
		}

		public (Guid runtimeKindGuid, string languageName) GetSettings() => (runtimeKindGuid, selectedItem.ID);
	}

	sealed class LanguageVM : ViewModelBase {
		public string Name => info.LanguageDisplayName;
		public string ID => info.LanguageName;
		readonly LanguageInfo info;
		public LanguageVM(LanguageInfo info) => this.info = info;
	}
}
