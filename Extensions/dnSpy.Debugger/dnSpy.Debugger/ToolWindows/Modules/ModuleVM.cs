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
using System.ComponentModel;
using System.Windows.Threading;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.MVVM;
using dnSpy.Contracts.Text.Classification;

namespace dnSpy.Debugger.ToolWindows.Modules {
	sealed class ModuleVM : ViewModelBase {
		public ImageReference ImageReference => Module.IsExe ? DsImages.AssemblyExe : DsImages.ModulePublic;
		public object NameObject => new FormatterObject<ModuleVM>(this, PredefinedTextClassifierTags.ModulesWindowName);
		public object PathObject => new FormatterObject<ModuleVM>(this, PredefinedTextClassifierTags.ModulesWindowPath);
		public object OptimizedObject => new FormatterObject<ModuleVM>(this, PredefinedTextClassifierTags.ModulesWindowOptimized);
		public object DynamicObject => new FormatterObject<ModuleVM>(this, PredefinedTextClassifierTags.ModulesWindowDynamic);
		public object InMemoryObject => new FormatterObject<ModuleVM>(this, PredefinedTextClassifierTags.ModulesWindowInMemory);
		public object OrderObject => new FormatterObject<ModuleVM>(this, PredefinedTextClassifierTags.ModulesWindowOrder);
		public object VersionObject => new FormatterObject<ModuleVM>(this, PredefinedTextClassifierTags.ModulesWindowVersion);
		public object TimestampObject => new FormatterObject<ModuleVM>(this, PredefinedTextClassifierTags.ModulesWindowTimestamp);
		public object AddressObject => new FormatterObject<ModuleVM>(this, PredefinedTextClassifierTags.ModulesWindowAddress);
		public object ProcessObject => new FormatterObject<ModuleVM>(this, PredefinedTextClassifierTags.ModulesWindowProcess);
		public object AppDomainObject => new FormatterObject<ModuleVM>(this, PredefinedTextClassifierTags.ModulesWindowAppDomain);
		public DbgModule Module { get; }
		public IModuleContext Context { get; }
		internal int Order { get; }

		public ModuleVM(DbgModule module, IModuleContext context, int order) {
			Module = module ?? throw new ArgumentNullException(nameof(module));
			Context = context ?? throw new ArgumentNullException(nameof(context));
			Order = order;
			module.PropertyChanged += DbgModule_PropertyChanged;
		}

		// UI thread
		internal void RefreshThemeFields() {
			OnPropertyChanged(nameof(NameObject));
			OnPropertyChanged(nameof(PathObject));
			OnPropertyChanged(nameof(OptimizedObject));
			OnPropertyChanged(nameof(DynamicObject));
			OnPropertyChanged(nameof(InMemoryObject));
			OnPropertyChanged(nameof(OrderObject));
			OnPropertyChanged(nameof(VersionObject));
			OnPropertyChanged(nameof(TimestampObject));
			OnPropertyChanged(nameof(AddressObject));
			OnPropertyChanged(nameof(ProcessObject));
			OnPropertyChanged(nameof(AppDomainObject));
		}

		// UI thread
		internal void RefreshHexFields() => OnPropertyChanged(nameof(ProcessObject));

		// UI thread
		internal void RefreshAppDomainNames(DbgAppDomain appDomain) {
			if (Module.AppDomain == appDomain)
				OnPropertyChanged(nameof(AppDomainObject));
		}

		// DbgManager thread
		void DbgModule_PropertyChanged(object sender, PropertyChangedEventArgs e) =>
			Context.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => DbgModule_PropertyChanged_UI(e.PropertyName)));

		// UI thread
		void DbgModule_PropertyChanged_UI(string propertyName) {
			switch (propertyName) {
			case nameof(Module.IsExe):
				OnPropertyChanged(nameof(ImageReference));
				break;

			case nameof(Module.Timestamp):
				OnPropertyChanged(nameof(TimestampObject));
				break;

			case nameof(Module.Version):
				OnPropertyChanged(nameof(VersionObject));
				break;

			case nameof(Module.IsDynamic):
				OnPropertyChanged(nameof(DynamicObject));
				OnPropertyChanged(nameof(NameObject));
				OnPropertyChanged(nameof(PathObject));
				break;

			case nameof(Module.IsInMemory):
				OnPropertyChanged(nameof(InMemoryObject));
				OnPropertyChanged(nameof(NameObject));
				OnPropertyChanged(nameof(PathObject));
				break;

			case nameof(Module.Name):
				OnPropertyChanged(nameof(NameObject));
				break;

			case nameof(Module.IsOptimized):
				OnPropertyChanged(nameof(OptimizedObject));
				break;

			case nameof(Module.Order):
				OnPropertyChanged(nameof(OrderObject));
				break;

			case nameof(Module.Address):
			case nameof(Module.Size):
				OnPropertyChanged(nameof(AddressObject));
				break;
			}
		}

		// UI thread
		public void Dispose() => Module.PropertyChanged -= DbgModule_PropertyChanged;
	}
}
