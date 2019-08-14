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
using dnSpy.Contracts.Controls.ToolWindows;
using dnSpy.Contracts.Debugger.Breakpoints.Modules;
using dnSpy.Contracts.Debugger.Text;
using dnSpy.Contracts.MVVM;
using dnSpy.Contracts.Text.Classification;
using dnSpy.Contracts.Utilities;
using dnSpy.Debugger.UI;

namespace dnSpy.Debugger.ToolWindows.ModuleBreakpoints {
	sealed class ModuleBreakpointVM : ViewModelBase {
		public bool IsEnabled {
			get => settings.IsEnabled;
			set {
				if (settings.IsEnabled == value)
					return;
				ModuleBreakpoint.IsEnabled = value;
			}
		}

		public bool? IsDynamic {
			get => settings.IsDynamic;
			set {
				if (settings.IsDynamic == value)
					return;
				ModuleBreakpoint.IsDynamic = value;
			}
		}

		public bool? IsInMemory {
			get => settings.IsInMemory;
			set {
				if (settings.IsInMemory == value)
					return;
				ModuleBreakpoint.IsInMemory = value;
			}
		}

		public bool? IsLoaded {
			get => settings.IsLoaded;
			set {
				if (settings.IsLoaded == value)
					return;
				ModuleBreakpoint.IsLoaded = value;
			}
		}

		public IModuleBreakpointContext Context { get; }
		public DbgModuleBreakpoint ModuleBreakpoint { get; }
		public object ModuleNameObject => new FormatterObject<ModuleBreakpointVM>(this, PredefinedTextClassifierTags.ModuleBreakpointsWindowModuleName);
		public object OrderObject => new FormatterObject<ModuleBreakpointVM>(this, PredefinedTextClassifierTags.ModuleBreakpointsWindowOrder);
		public object ProcessNameObject => new FormatterObject<ModuleBreakpointVM>(this, PredefinedTextClassifierTags.ModuleBreakpointsWindowProcessName);
		public object AppDomainNameObject => new FormatterObject<ModuleBreakpointVM>(this, PredefinedTextClassifierTags.ModuleBreakpointsWindowModuleAppDomainName);
		internal int Order { get; }

		public IEditableValue ModuleNameEditableValue { get; }
		public IEditValueProvider ModuleNameEditValueProvider { get; }
		public IEditableValue OrderEditableValue { get; }
		public IEditValueProvider OrderEditValueProvider { get; }
		public IEditableValue ProcessNameEditableValue { get; }
		public IEditValueProvider ProcessNameEditValueProvider { get; }
		public IEditableValue AppDomainNameEditableValue { get; }
		public IEditValueProvider AppDomainNameEditValueProvider { get; }

		DbgModuleBreakpointSettings settings;

		public ModuleBreakpointVM(DbgModuleBreakpoint moduleBreakpoint, IModuleBreakpointContext context, int order, IEditValueProvider moduleNameEditValueProvider, IEditValueProvider orderEditValueProvider, IEditValueProvider processNameEditValueProvider, IEditValueProvider appDomainNameEditValueProvider) {
			ModuleBreakpoint = moduleBreakpoint ?? throw new ArgumentNullException(nameof(moduleBreakpoint));
			Context = context ?? throw new ArgumentNullException(nameof(context));
			Order = order;
			ModuleNameEditValueProvider = moduleNameEditValueProvider ?? throw new ArgumentNullException(nameof(moduleNameEditValueProvider));
			ModuleNameEditableValue = new EditableValueImpl(() => ModuleBreakpoint.ModuleName, s => ModuleBreakpoint.ModuleName = ConvertEditedString(s));
			OrderEditValueProvider = orderEditValueProvider ?? throw new ArgumentNullException(nameof(orderEditValueProvider));
			OrderEditableValue = new EditableValueImpl(() => OrderToNumber(), s => WriteOrder(s));
			ProcessNameEditValueProvider = processNameEditValueProvider ?? throw new ArgumentNullException(nameof(processNameEditValueProvider));
			ProcessNameEditableValue = new EditableValueImpl(() => ModuleBreakpoint.ProcessName, s => ModuleBreakpoint.ProcessName = ConvertEditedString(s));
			AppDomainNameEditValueProvider = appDomainNameEditValueProvider ?? throw new ArgumentNullException(nameof(appDomainNameEditValueProvider));
			AppDomainNameEditableValue = new EditableValueImpl(() => ModuleBreakpoint.AppDomainName, s => ModuleBreakpoint.AppDomainName = ConvertEditedString(s));
			settings = ModuleBreakpoint.Settings;
		}

		string? ConvertEditedString(string? s) {
			if (string2.IsNullOrWhiteSpace(s))
				return null;
			return s.Trim();
		}

		string OrderToNumber() {
			var writer = new DbgStringBuilderTextWriter();
			Context.Formatter.WriteOrder(writer, ModuleBreakpoint);
			return writer.ToString();
		}

		void WriteOrder(string? value) {
			if (string2.IsNullOrWhiteSpace(value))
				ModuleBreakpoint.Order = null;
			else {
				var order = SimpleTypeConverter.ParseInt32(value, int.MinValue, int.MaxValue, out var error);
				if (error is null)
					ModuleBreakpoint.Order = order;
				else {
					// Keep original value
				}
			}
		}

		// UI thread
		internal void RefreshThemeFields_UI() {
			Context.UIDispatcher.VerifyAccess();
			OnPropertyChanged(nameof(ModuleNameObject));
			OnPropertyChanged(nameof(OrderObject));
			OnPropertyChanged(nameof(ProcessNameObject));
			OnPropertyChanged(nameof(AppDomainNameObject));
		}

		// UI thread
		internal void UpdateSettings_UI(DbgModuleBreakpointSettings newSettings) {
			Context.UIDispatcher.VerifyAccess();
			var oldSettings = settings;
			settings = newSettings;
			if (oldSettings.IsEnabled != newSettings.IsEnabled)
				OnPropertyChanged(nameof(IsEnabled));
			if (oldSettings.ModuleName != newSettings.ModuleName)
				OnPropertyChanged(nameof(ModuleNameObject));
			if (oldSettings.IsDynamic != newSettings.IsDynamic)
				OnPropertyChanged(nameof(IsDynamic));
			if (oldSettings.IsInMemory != newSettings.IsInMemory)
				OnPropertyChanged(nameof(IsInMemory));
			if (oldSettings.IsLoaded != newSettings.IsLoaded)
				OnPropertyChanged(nameof(IsLoaded));
			if (oldSettings.Order != newSettings.Order)
				OnPropertyChanged(nameof(OrderObject));
			if (oldSettings.ProcessName != newSettings.ProcessName)
				OnPropertyChanged(nameof(ProcessNameObject));
			if (oldSettings.AppDomainName != newSettings.AppDomainName)
				OnPropertyChanged(nameof(AppDomainNameObject));
		}

		// UI thread
		internal void ClearEditingValueProperties() {
			Context.UIDispatcher.VerifyAccess();
			ModuleNameEditableValue.IsEditingValue = false;
			OrderEditableValue.IsEditingValue = false;
			ProcessNameEditableValue.IsEditingValue = false;
			AppDomainNameEditableValue.IsEditingValue = false;
		}

		// UI thread
		internal void Dispose() {
			Context.UIDispatcher.VerifyAccess();
			ClearEditingValueProperties();
		}

		// UI thread
		internal bool IsEditingValues {
			get {
				Context.UIDispatcher.VerifyAccess();
				return ModuleNameEditableValue.IsEditingValue ||
					OrderEditableValue.IsEditingValue ||
					ProcessNameEditableValue.IsEditingValue ||
					AppDomainNameEditableValue.IsEditingValue;
			}
		}
	}
}
