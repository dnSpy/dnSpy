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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Documents;
using dnSpy.Contracts.Text;
using dnSpy.Debugger.CallStack;

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
		public abstract bool CanToggleUseHexadecimal { get; }
		public abstract void ToggleUseHexadecimal();
		public abstract bool UseHexadecimal { get; set; }
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
		readonly Lazy<ReferenceNavigatorService> referenceNavigatorService;
		readonly Lazy<CallStackService> callStackService;

		ObservableCollection<StackFrameVM> AllItems => callStackVM.AllItems;
		ObservableCollection<StackFrameVM> SelectedItems => callStackVM.SelectedItems;
		IEnumerable<StackFrameVM> SortedSelectedItems => SelectedItems.OrderBy(a => a.Index);

		[ImportingConstructor]
		CallStackOperationsImpl(ICallStackVM callStackVM, DebuggerSettings debuggerSettings, CallStackDisplaySettings callStackDisplaySettings, Lazy<ReferenceNavigatorService> referenceNavigatorService, Lazy<CallStackService> callStackService) {
			this.callStackVM = callStackVM;
			this.debuggerSettings = debuggerSettings;
			this.callStackDisplaySettings = callStackDisplaySettings;
			this.referenceNavigatorService = referenceNavigatorService;
			this.callStackService = callStackService;
		}

		public override bool CanCopy => SelectedItems.Count != 0;
		public override void Copy() {
			var output = new StringBuilderTextColorOutput();
			foreach (var vm in SortedSelectedItems) {
				var formatter = vm.Context.Formatter;
				formatter.WriteImage(output, vm);
				output.Write(BoxedTextColor.Text, "\t");
				formatter.WriteName(output, vm);
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
			callStackService.Value.ActiveFrameIndex = vm.Index;
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

		public override bool CanGoToDisassembly => false;
		public override void GoToDisassembly() {
			if (!CanGoToDisassembly)
				return;
			var vm = (NormalStackFrameVM)SelectedItems[0];
			//TODO:
		}

		public override bool CanRunToCursor => SelectedItems.Count == 1 && SelectedItems[0] is NormalStackFrameVM;
		public override void RunToCursor() {
			if (!CanRunToCursor)
				return;
			var vm = (NormalStackFrameVM)SelectedItems[0];
			//TODO:
		}

		public override bool CanUnwindToFrame => false;
		public override void UnwindToFrame() {
			if (!CanUnwindToFrame)
				return;
			var vm = (NormalStackFrameVM)SelectedItems[0];
			//TODO:
		}

		public override BreakpointsCommandKind BreakpointsCommandKind => BreakpointsCommandKind.Add;//TODO:
		public override bool IsBreakpointEnabled => true;//TODO:

		public override bool CanAddBreakpoint => CanAddBreakpointOrTracepoint;
		public override void AddBreakpoint() => AddBreakpointOrTracepoint(true);
		public override bool CanAddTracepoint => CanAddBreakpointOrTracepoint;
		public override void AddTracepoint() => AddBreakpointOrTracepoint(false);
		bool CanAddBreakpointOrTracepoint => SelectedItems.Count == 1 && SelectedItems[0] is NormalStackFrameVM;
		void AddBreakpointOrTracepoint(bool addBreakpoint) {
			if (!CanAddBreakpointOrTracepoint)
				return;
			var vm = (NormalStackFrameVM)SelectedItems[0];
			//TODO: If BP/TP exists, remove it
		}

		public override bool CanEnableBreakpoint => SelectedItems.Count == 1 && SelectedItems[0] is NormalStackFrameVM;
		public override void EnableBreakpoint() {
			if (!CanEnableBreakpoint)
				return;
			var vm = (NormalStackFrameVM)SelectedItems[0];
			//TODO:
		}

		public override bool CanRemoveBreakpoint => SelectedItems.Count == 1 && SelectedItems[0] is NormalStackFrameVM;
		public override void RemoveBreakpoint() {
			if (!CanRemoveBreakpoint)
				return;
			var vm = (NormalStackFrameVM)SelectedItems[0];
			//TODO:
		}

		public override bool CanEditBreakpointSettings => SelectedItems.Count == 1 && SelectedItems[0] is NormalStackFrameVM;
		public override void EditBreakpointSettings() {
			if (!CanEditBreakpointSettings)
				return;
			var vm = (NormalStackFrameVM)SelectedItems[0];
			//TODO:
		}

		public override bool CanExportBreakpoint => SelectedItems.Count == 1 && SelectedItems[0] is NormalStackFrameVM;
		public override void ExportBreakpoint() {
			if (!CanExportBreakpoint)
				return;
			var vm = (NormalStackFrameVM)SelectedItems[0];
			//TODO:
		}

		public override bool CanToggleUseHexadecimal => true;
		public override void ToggleUseHexadecimal() => UseHexadecimal = !UseHexadecimal;
		public override bool UseHexadecimal {
			get => debuggerSettings.UseHexadecimal;
			set => debuggerSettings.UseHexadecimal = value;
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
