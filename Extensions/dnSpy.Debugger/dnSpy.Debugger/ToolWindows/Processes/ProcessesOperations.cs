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
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.Attach.Dialogs;
using dnSpy.Contracts.Debugger.CallStack;
using dnSpy.Contracts.Debugger.Steppers;
using dnSpy.Contracts.Debugger.Text;
using dnSpy.Contracts.Documents;
using dnSpy.Debugger.UI;

namespace dnSpy.Debugger.ToolWindows.Processes {
	abstract class ProcessesOperations {
		public abstract bool CanCopy { get; }
		public abstract void Copy();
		public abstract bool CanSelectAll { get; }
		public abstract void SelectAll();
		public abstract bool CanContinueProcess { get; }
		public abstract void ContinueProcess();
		public abstract bool CanBreakProcess { get; }
		public abstract void BreakProcess();
		public abstract bool CanStepIntoProcess { get; }
		public abstract void StepIntoProcess();
		public abstract bool CanStepOverProcess { get; }
		public abstract void StepOverProcess();
		public abstract bool CanStepOutProcess { get; }
		public abstract void StepOutProcess();
		public abstract bool CanDetachProcess { get; }
		public abstract void DetachProcess();
		public abstract bool CanTerminateProcess { get; }
		public abstract void TerminateProcess();
		public abstract bool CanAttachToProcess { get; }
		public abstract void AttachToProcess();
		public abstract bool CanToggleDetachWhenDebuggingStopped { get; }
		public abstract void ToggleDetachWhenDebuggingStopped();
		public abstract bool DetachWhenDebuggingStopped { get; set; }
		public abstract bool CanToggleUseHexadecimal { get; }
		public abstract void ToggleUseHexadecimal();
		public abstract bool UseHexadecimal { get; set; }
		public abstract bool CanSetCurrentProcess { get; }
		public abstract void SetCurrentProcess(bool newTab);
		public abstract bool CanResetSearchSettings { get; }
		public abstract void ResetSearchSettings();
	}

	[Export(typeof(ProcessesOperations))]
	sealed class ProcessesOperationsImpl : ProcessesOperations {
		readonly UIDispatcher uiDispatcher;
		readonly IProcessesVM processesVM;
		readonly DebuggerSettings debuggerSettings;
		readonly Lazy<DbgManager> dbgManager;
		readonly Lazy<ShowAttachToProcessDialog> showAttachToProcessDialog;
		readonly Lazy<ReferenceNavigatorService> referenceNavigatorService;
		readonly Lazy<DbgCallStackService> dbgCallStackService;

		BulkObservableCollection<ProcessVM> AllItems => processesVM.AllItems;
		ObservableCollection<ProcessVM> SelectedItems => processesVM.SelectedItems;
		IEnumerable<ProcessVM> SortedSelectedItems => processesVM.Sort(SelectedItems);

		[ImportingConstructor]
		ProcessesOperationsImpl(UIDispatcher uiDispatcher, IProcessesVM processesVM, DebuggerSettings debuggerSettings, Lazy<DbgManager> dbgManager, Lazy<ShowAttachToProcessDialog> showAttachToProcessDialog, Lazy<ReferenceNavigatorService> referenceNavigatorService, Lazy<DbgCallStackService> dbgCallStackService) {
			this.uiDispatcher = uiDispatcher;
			this.processesVM = processesVM;
			this.debuggerSettings = debuggerSettings;
			this.dbgManager = dbgManager;
			this.showAttachToProcessDialog = showAttachToProcessDialog;
			this.referenceNavigatorService = referenceNavigatorService;
			this.dbgCallStackService = dbgCallStackService;
		}

		public override bool CanCopy => SelectedItems.Count != 0;
		public override void Copy() {
			var output = new DbgStringBuilderTextWriter();
			foreach (var vm in SortedSelectedItems) {
				var formatter = vm.Context.Formatter;
				bool needTab = false;
				foreach (var column in processesVM.Descs.Columns) {
					if (!column.IsVisible)
						continue;

					if (needTab)
						output.Write(DbgTextColor.Text, "\t");
					switch (column.Id) {
					case ProcessesWindowColumnIds.Icon:
						formatter.WriteImage(output, vm);
						break;

					case ProcessesWindowColumnIds.Name:
						formatter.WriteName(output, vm.Process);
						break;

					case ProcessesWindowColumnIds.ID:
						formatter.WriteId(output, vm.Process);
						break;

					case ProcessesWindowColumnIds.Title:
						formatter.WriteTitle(output, vm);
						break;

					case ProcessesWindowColumnIds.State:
						formatter.WriteState(output, vm);
						break;

					case ProcessesWindowColumnIds.Debugging:
						formatter.WriteDebugging(output, vm.Process);
						break;

					case ProcessesWindowColumnIds.ProcessArchitecture:
						formatter.WriteArchitecture(output, vm.Process.Architecture);
						break;

					case ProcessesWindowColumnIds.Path:
						formatter.WritePath(output, vm.Process);
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

		public override bool CanContinueProcess => SelectedItems.Any(a => a.Process.State == DbgProcessState.Paused);
		public override void ContinueProcess() {
			foreach (var vm in SelectedItems)
				vm.Process.Run();
		}

		public override bool CanBreakProcess => SelectedItems.Any(a => a.Process.State == DbgProcessState.Running);
		public override void BreakProcess() {
			foreach (var vm in SelectedItems)
				vm.Process.Break();
		}

		void Step(DbgStepKind step) {
			if (!CanStepProcess)
				return;
			var thread = dbgManager.Value.CurrentThread.Current;
			if (thread is null)
				return;
			thread.CreateStepper().Step(step, autoClose: true);
		}

		bool CanStepProcess => dbgManager.Value.CurrentProcess.Current?.State == DbgProcessState.Paused;

		public override bool CanStepIntoProcess => CanStepProcess;
		public override void StepIntoProcess() => Step(DbgStepKind.StepIntoProcess);

		public override bool CanStepOverProcess => CanStepProcess;
		public override void StepOverProcess() => Step(DbgStepKind.StepOverProcess);

		public override bool CanStepOutProcess => CanStepProcess;
		public override void StepOutProcess() => Step(DbgStepKind.StepOutProcess);

		public override bool CanDetachProcess => SelectedItems.Count != 0;
		public override void DetachProcess() {
			foreach (var vm in SelectedItems)
				vm.Process.Detach();
		}

		public override bool CanTerminateProcess => SelectedItems.Count != 0;
		public override void TerminateProcess() {
			foreach (var vm in SelectedItems)
				vm.Process.Terminate();
		}

		public override bool CanAttachToProcess => true;
		public override void AttachToProcess() => showAttachToProcessDialog.Value.Attach();

		public override bool CanToggleDetachWhenDebuggingStopped => SelectedItems.Count != 0;
		public override void ToggleDetachWhenDebuggingStopped() => DetachWhenDebuggingStopped = !DetachWhenDebuggingStopped;
		public override bool DetachWhenDebuggingStopped {
			get => SelectedItems.Count != 0 && !SelectedItems.Any(a => !a.Process.ShouldDetach);
			set {
				foreach (var vm in SelectedItems)
					vm.Process.ShouldDetach = value;
			}
		}

		public override bool CanToggleUseHexadecimal => true;
		public override void ToggleUseHexadecimal() => UseHexadecimal = !UseHexadecimal;
		public override bool UseHexadecimal {
			get => debuggerSettings.UseHexadecimal;
			set => debuggerSettings.UseHexadecimal = value;
		}

		public override bool CanSetCurrentProcess => SelectedItems.Count == 1 && SelectedItems[0].Process.State == DbgProcessState.Paused;
		public override void SetCurrentProcess(bool newTab) {
			if (!CanSetCurrentProcess)
				return;
			var process = SelectedItems[0].Process;
			if (process.State == DbgProcessState.Paused) {
				process.DbgManager.CurrentProcess.Current = process;
				process.DbgManager.Dispatcher.BeginInvoke(() => {
					if (process.DbgManager.CurrentProcess.Current == process) {
						var thread = process.DbgManager.CurrentThread.Current;
						if (!(thread is null))
							uiDispatcher.UI(() => GoToThread(thread, newTab: newTab));
					}
				});
			}
		}

		void GoToThread(DbgThread thread, bool newTab) {
			var info = Threads.ThreadUtilities.GetFirstFrameLocation(thread);
			if (!(info.location is null)) {
				try {
					var options = newTab ? new object[] { PredefinedReferenceNavigatorOptions.NewTab } : Array.Empty<object>();
					referenceNavigatorService.Value.GoTo(info.location, options);
					dbgCallStackService.Value.ActiveFrameIndex = info.frameIndex;
				}
				finally {
					info.location.Close();
				}
			}
		}

		public override bool CanResetSearchSettings => true;
		public override void ResetSearchSettings() => processesVM.ResetSearchSettings();
	}
}
