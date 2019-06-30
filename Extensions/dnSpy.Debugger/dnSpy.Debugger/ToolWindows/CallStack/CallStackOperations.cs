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
using System.Runtime.InteropServices;
using System.Windows;
using dnSpy.Contracts.App;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.Breakpoints.Code;
using dnSpy.Contracts.Debugger.Breakpoints.Code.Dialogs;
using dnSpy.Contracts.Debugger.CallStack;
using dnSpy.Contracts.Debugger.Code;
using dnSpy.Contracts.Debugger.Evaluation;
using dnSpy.Contracts.Debugger.Text;
using dnSpy.Contracts.Documents;
using dnSpy.Debugger.Breakpoints.Code;
using dnSpy.Debugger.Disassembly;
using dnSpy.Debugger.Properties;

namespace dnSpy.Debugger.ToolWindows.CallStack {
	abstract class CallStackOperations {
		public abstract bool CanCopy { get; }
		public abstract void Copy();
		public abstract bool CanSelectAll { get; }
		public abstract void SelectAll();
		public abstract bool CanSwitchToFrame { get; }
		public abstract void SwitchToFrame(bool newTab);
		public abstract bool CanGoToSourceCode { get; }
		public abstract void GoToSourceCode(bool newTab);
		public abstract bool CanGoToDisassembly { get; }
		public abstract void GoToDisassembly();
		public abstract bool CanRunToCursor { get; }
		public abstract void RunToCursor();
		public abstract bool CanUnwindToFrame { get; }
		public abstract void UnwindToFrame();
		public abstract BreakpointsCommandKind BreakpointsCommandKind { get; }
		public abstract bool IsBreakpointEnabled { get; }
		public abstract bool CanAddBreakpoint { get; }
		public abstract void AddBreakpoint();
		public abstract bool CanAddTracepoint { get; }
		public abstract void AddTracepoint();
		public abstract bool CanEnableBreakpoint { get; }
		public abstract void EnableBreakpoint();
		public abstract bool CanRemoveBreakpoint { get; }
		public abstract void RemoveBreakpoint();
		public abstract bool CanEditBreakpointSettings { get; }
		public abstract void EditBreakpointSettings();
		public abstract bool CanExportBreakpoint { get; }
		public abstract void ExportBreakpoint();
		public abstract IList<DbgLanguage> GetLanguages();
		public abstract DbgLanguage? GetCurrentLanguage();
		public abstract void SetCurrentLanguage(DbgLanguage language);
		public abstract bool CanToggleUseHexadecimal { get; }
		public abstract void ToggleUseHexadecimal();
		public abstract bool UseHexadecimal { get; set; }
		public abstract bool CanToggleUseDigitSeparators { get; }
		public abstract void ToggleUseDigitSeparators();
		public abstract bool UseDigitSeparators { get; set; }
		public abstract bool ShowReturnTypes { get; set; }
		public abstract bool ShowParameterTypes { get; set; }
		public abstract bool ShowParameterNames { get; set; }
		public abstract bool ShowParameterValues { get; set; }
		public abstract bool ShowFunctionOffset { get; set; }
		public abstract bool ShowModuleNames { get; set; }
		public abstract bool ShowDeclaringTypes { get; set; }
		public abstract bool ShowNamespaces { get; set; }
		public abstract bool ShowIntrinsicTypeKeywords { get; set; }
		public abstract bool ShowTokens { get; set; }
	}

	enum BreakpointsCommandKind {
		None,
		Add,
		Edit,
	}

	[Export(typeof(CallStackOperations))]
	sealed class CallStackOperationsImpl : CallStackOperations {
		readonly ICallStackVM callStackVM;
		readonly DebuggerSettings debuggerSettings;
		readonly CallStackDisplaySettings callStackDisplaySettings;
		readonly IMessageBoxService messageBoxService;
		readonly Lazy<ReferenceNavigatorService> referenceNavigatorService;
		readonly Lazy<DbgCallStackService> dbgCallStackService;
		readonly Lazy<ShowCodeBreakpointSettingsService> showCodeBreakpointSettingsService;
		readonly Lazy<DbgCodeBreakpointSerializerService> dbgCodeBreakpointSerializerService;
		readonly Lazy<DbgCodeBreakpointsService> dbgCodeBreakpointsService;
		readonly Lazy<DbgManager> dbgManager;
		readonly Lazy<DbgLanguageService> dbgLanguageService;
		readonly Lazy<DbgShowNativeCodeService> dbgShowNativeCodeService;

		ObservableCollection<StackFrameVM> AllItems => callStackVM.AllItems;
		ObservableCollection<StackFrameVM> SelectedItems => callStackVM.SelectedItems;
		IEnumerable<StackFrameVM> SortedSelectedItems => SelectedItems.OrderBy(a => a.Index);

		[ImportingConstructor]
		CallStackOperationsImpl(ICallStackVM callStackVM, DebuggerSettings debuggerSettings, CallStackDisplaySettings callStackDisplaySettings, IMessageBoxService messageBoxService, Lazy<ReferenceNavigatorService> referenceNavigatorService, Lazy<DbgCallStackService> dbgCallStackService, Lazy<ShowCodeBreakpointSettingsService> showCodeBreakpointSettingsService, Lazy<DbgCodeBreakpointSerializerService> dbgCodeBreakpointSerializerService, Lazy<DbgCodeBreakpointsService> dbgCodeBreakpointsService, Lazy<DbgManager> dbgManager, Lazy<DbgLanguageService> dbgLanguageService, Lazy<DbgShowNativeCodeService> dbgShowNativeCodeService) {
			this.callStackVM = callStackVM;
			this.debuggerSettings = debuggerSettings;
			this.callStackDisplaySettings = callStackDisplaySettings;
			this.messageBoxService = messageBoxService;
			this.referenceNavigatorService = referenceNavigatorService;
			this.dbgCallStackService = dbgCallStackService;
			this.showCodeBreakpointSettingsService = showCodeBreakpointSettingsService;
			this.dbgCodeBreakpointSerializerService = dbgCodeBreakpointSerializerService;
			this.dbgCodeBreakpointsService = dbgCodeBreakpointsService;
			this.dbgManager = dbgManager;
			this.dbgLanguageService = dbgLanguageService;
			this.dbgShowNativeCodeService = dbgShowNativeCodeService;
		}

		public override bool CanCopy => SelectedItems.Count != 0;
		public override void Copy() {
			var output = new DbgStringBuilderTextWriter();
			foreach (var vm in SortedSelectedItems) {
				var formatter = vm.Context.Formatter;
				bool needTab = false;
				foreach (var column in callStackVM.Descs.Columns) {
					if (!column.IsVisible)
						continue;

					if (needTab)
						output.Write(DbgTextColor.Text, "\t");
					switch (column.Id) {
					case CallStackWindowColumnIds.Icon:
						formatter.WriteImage(output, vm);
						break;

					case CallStackWindowColumnIds.Name:
						formatter.WriteName(output, vm);
						break;

					default:
						throw new InvalidOperationException();
					}

					needTab = true;
				}
				output.WriteLine();
			}
			var s = output.ToString();
			if (s.Length > 0) {
				try {
					Clipboard.SetText(s);
				}
				catch (ExternalException) { }
			}
		}

		public override bool CanSelectAll => SelectedItems.Count != AllItems.Count;
		public override void SelectAll() {
			SelectedItems.Clear();
			foreach (var vm in AllItems)
				SelectedItems.Add(vm);
		}

		public override bool CanSwitchToFrame => SelectedItems.Count == 1 && SelectedItems[0] is NormalStackFrameVM;
		public override void SwitchToFrame(bool newTab) {
			if (!CanSwitchToFrame)
				return;
			var vm = (NormalStackFrameVM)SelectedItems[0];
			dbgCallStackService.Value.ActiveFrameIndex = vm.Index;
			var options = newTab ? new object[] { PredefinedReferenceNavigatorOptions.NewTab } : Array.Empty<object>();
			referenceNavigatorService.Value.GoTo(vm.Frame.Location, options);
		}

		public override bool CanGoToSourceCode => SelectedItems.Count == 1 && SelectedItems[0] is NormalStackFrameVM;
		public override void GoToSourceCode(bool newTab) {
			if (!CanGoToSourceCode)
				return;
			var vm = (NormalStackFrameVM)SelectedItems[0];
			var options = newTab ? new object[] { PredefinedReferenceNavigatorOptions.NewTab } : Array.Empty<object>();
			referenceNavigatorService.Value.GoTo(vm.Frame.Location, options);
		}

		public override bool CanGoToDisassembly {
			get {
				if (SelectedItems.Count != 1)
					return false;
				if (!(SelectedItems[0] is NormalStackFrameVM vm))
					return false;
				return dbgShowNativeCodeService.Value.CanShowNativeCode(vm.Frame);
			}
		}
		public override void GoToDisassembly() {
			if (!CanGoToDisassembly)
				return;
			var vm = (NormalStackFrameVM)SelectedItems[0];
			if (!dbgShowNativeCodeService.Value.ShowNativeCode(vm.Frame))
				messageBoxService.Show(dnSpy_Debugger_Resources.Error_CouldNotShowDisassembly);
		}

		public override bool CanRunToCursor =>
			SelectedItems.Count == 1 &&
			SelectedItems[0] is NormalStackFrameVM vm &&
			(vm.Frame.Flags & DbgStackFrameFlags.LocationIsNextStatement) != 0 &&
			vm.Index != 0 &&
			vm.Frame.Location is DbgCodeLocation location &&
			dbgCodeBreakpointsService.Value.TryGetBreakpoint(location) is null;
		public override void RunToCursor() {
			if (!CanRunToCursor)
				return;
			var vm = (NormalStackFrameVM)SelectedItems[0];
			var process = vm.Frame.Process;
			var bp = dbgCodeBreakpointsService.Value.Add(new DbgCodeBreakpointInfo(vm.Frame.Location!.Clone(), new DbgCodeBreakpointSettings { IsEnabled = true }, DbgCodeBreakpointOptions.Hidden | DbgCodeBreakpointOptions.Temporary | DbgCodeBreakpointOptions.OneShot));
			if (!(bp is null))
				dbgManager.Value.Run(process);
		}

		public override bool CanUnwindToFrame => false;
		public override void UnwindToFrame() {
			if (!CanUnwindToFrame)
				return;
			var vm = (NormalStackFrameVM)SelectedItems[0];
			//TODO:
		}

		DbgCodeBreakpoint? TryGetBreakpoint(DbgCodeLocation location) {
			if (location is null)
				return null;
			var bp = dbgCodeBreakpointsService.Value.TryGetBreakpoint(location);
			return bp is null || bp.IsHidden ? null : bp;
		}

		(DbgCodeBreakpoint? breakpoint, DbgCodeLocation location)? GetBreakpoint() {
			if (SelectedItems.Count != 1)
				return null;
			var vm = SelectedItems[0] as NormalStackFrameVM;
			if (vm is null)
				return null;
			if ((vm.Frame.Flags & DbgStackFrameFlags.LocationIsNextStatement) == 0)
				return null;
			var location = vm.Frame.Location;
			if (location is null)
				return null;
			var bp = TryGetBreakpoint(location);
			return (bp, location);
		}

		public override BreakpointsCommandKind BreakpointsCommandKind {
			get {
				if (SelectedItems.Count != 1)
					return BreakpointsCommandKind.None;
				var vm = SelectedItems[0] as NormalStackFrameVM;
				if (vm is null)
					return BreakpointsCommandKind.None;
				if ((vm.Frame.Flags & DbgStackFrameFlags.LocationIsNextStatement) == 0)
					return BreakpointsCommandKind.None;
				var location = vm.Frame.Location;
				if (location is null)
					return BreakpointsCommandKind.None;
				var bp = TryGetBreakpoint(location);
				return bp is null ? BreakpointsCommandKind.Add : BreakpointsCommandKind.Edit;
			}
		}

		public override bool IsBreakpointEnabled => GetBreakpoint()?.breakpoint?.IsEnabled == true;

		public override bool CanAddBreakpoint => CanAddBreakpointOrTracepoint;
		public override void AddBreakpoint() => AddBreakpointOrTracepoint(true);
		public override bool CanAddTracepoint => CanAddBreakpointOrTracepoint;
		public override void AddTracepoint() => AddBreakpointOrTracepoint(false);
		bool CanAddBreakpointOrTracepoint => SelectedItems.Count == 1 && SelectedItems[0] is NormalStackFrameVM;
		void AddBreakpointOrTracepoint(bool addBreakpoint) {
			if (!CanAddBreakpointOrTracepoint)
				return;
			var info = GetBreakpoint();
			if (info is null)
				return;
			if (!(info.Value.breakpoint is null)) {
				if (info.Value.breakpoint.IsEnabled)
					info.Value.breakpoint.Remove();
				else
					info.Value.breakpoint.IsEnabled = true;
			}
			else {
				var bp = dbgCodeBreakpointsService.Value.Add(new DbgCodeBreakpointInfo(info.Value.location.Clone(), new DbgCodeBreakpointSettings { IsEnabled = true }, DbgCodeBreakpointOptions.Temporary));
				if (bp is null)
					return;
				if (!addBreakpoint) {
					var settings = bp.Settings;
					settings.Trace = new DbgCodeBreakpointTrace(string.Empty, @continue: true);
					var newSettings = showCodeBreakpointSettingsService.Value.Show(settings);
					if (!(newSettings is null))
						bp.Settings = newSettings.Value;
				}
			}
		}

		public override bool CanEnableBreakpoint => !(GetBreakpoint()?.breakpoint is null);
		public override void EnableBreakpoint() {
			if (!CanEnableBreakpoint)
				return;
			var info = GetBreakpoint();
			if (info?.breakpoint is DbgCodeBreakpoint bp)
				bp.IsEnabled = !bp.IsEnabled;
		}

		public override bool CanRemoveBreakpoint => !(GetBreakpoint()?.breakpoint is null);
		public override void RemoveBreakpoint() {
			if (!CanRemoveBreakpoint)
				return;
			var info = GetBreakpoint();
			if (info?.breakpoint is DbgCodeBreakpoint bp)
				bp.Remove();
		}

		public override bool CanEditBreakpointSettings => !(GetBreakpoint()?.breakpoint is null);
		public override void EditBreakpointSettings() {
			if (!CanEditBreakpointSettings)
				return;
			var info = GetBreakpoint();
			if (info?.breakpoint is DbgCodeBreakpoint bp) {
				var settings = bp.Settings;
				var newSettings = showCodeBreakpointSettingsService.Value.Show(settings);
				if (!(newSettings is null))
					bp.Settings = newSettings.Value;
			}
		}

		public override bool CanExportBreakpoint => !(GetBreakpoint()?.breakpoint is null);
		public override void ExportBreakpoint() {
			if (!CanExportBreakpoint)
				return;
			var info = GetBreakpoint();
			if (info?.breakpoint is DbgCodeBreakpoint bp)
				dbgCodeBreakpointSerializerService.Value.Save(new[] { bp });
		}

		DbgRuntime? CurrentRuntime => dbgManager.Value.CurrentThread.Current?.Runtime;

		public override IList<DbgLanguage> GetLanguages() {
			var runtime = CurrentRuntime;
			if (runtime is null)
				return Array.Empty<DbgLanguage>();
			return dbgLanguageService.Value.GetLanguages(runtime.RuntimeKindGuid);
		}

		public override DbgLanguage? GetCurrentLanguage() {
			var runtime = CurrentRuntime;
			if (runtime is null)
				return null;
			return dbgLanguageService.Value.GetCurrentLanguage(runtime.RuntimeKindGuid);
		}

		public override void SetCurrentLanguage(DbgLanguage language) {
			var runtime = CurrentRuntime;
			if (runtime is null)
				return;
			dbgLanguageService.Value.SetCurrentLanguage(runtime.RuntimeKindGuid, language);
		}

		public override bool CanToggleUseHexadecimal => true;
		public override void ToggleUseHexadecimal() => UseHexadecimal = !UseHexadecimal;
		public override bool UseHexadecimal {
			get => debuggerSettings.UseHexadecimal;
			set => debuggerSettings.UseHexadecimal = value;
		}

		public override bool CanToggleUseDigitSeparators => true;
		public override void ToggleUseDigitSeparators() => UseDigitSeparators = !UseDigitSeparators;
		public override bool UseDigitSeparators {
			get => debuggerSettings.UseDigitSeparators;
			set => debuggerSettings.UseDigitSeparators = value;
		}

		public override bool ShowReturnTypes {
			get => callStackDisplaySettings.ShowReturnTypes;
			set => callStackDisplaySettings.ShowReturnTypes = value;
		}

		public override bool ShowParameterTypes {
			get => callStackDisplaySettings.ShowParameterTypes;
			set => callStackDisplaySettings.ShowParameterTypes = value;
		}

		public override bool ShowParameterNames {
			get => callStackDisplaySettings.ShowParameterNames;
			set => callStackDisplaySettings.ShowParameterNames = value;
		}

		public override bool ShowParameterValues {
			get => callStackDisplaySettings.ShowParameterValues;
			set => callStackDisplaySettings.ShowParameterValues = value;
		}

		public override bool ShowFunctionOffset {
			get => callStackDisplaySettings.ShowFunctionOffset;
			set => callStackDisplaySettings.ShowFunctionOffset = value;
		}

		public override bool ShowModuleNames {
			get => callStackDisplaySettings.ShowModuleNames;
			set => callStackDisplaySettings.ShowModuleNames = value;
		}

		public override bool ShowDeclaringTypes {
			get => callStackDisplaySettings.ShowDeclaringTypes;
			set => callStackDisplaySettings.ShowDeclaringTypes = value;
		}

		public override bool ShowNamespaces {
			get => callStackDisplaySettings.ShowNamespaces;
			set => callStackDisplaySettings.ShowNamespaces = value;
		}

		public override bool ShowIntrinsicTypeKeywords {
			get => callStackDisplaySettings.ShowIntrinsicTypeKeywords;
			set => callStackDisplaySettings.ShowIntrinsicTypeKeywords = value;
		}

		public override bool ShowTokens {
			get => callStackDisplaySettings.ShowTokens;
			set => callStackDisplaySettings.ShowTokens = value;
		}
	}
}
