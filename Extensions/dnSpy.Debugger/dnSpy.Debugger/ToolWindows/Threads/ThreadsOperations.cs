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
using dnSpy.Contracts.Debugger.CallStack;
using dnSpy.Contracts.Debugger.Text;
using dnSpy.Contracts.Documents;
using dnSpy.Debugger.UI;

namespace dnSpy.Debugger.ToolWindows.Threads {
	abstract class ThreadsOperations {
		public abstract bool CanCopy { get; }
		public abstract void Copy();
		public abstract bool CanSelectAll { get; }
		public abstract void SelectAll();
		public abstract bool CanToggleUseHexadecimal { get; }
		public abstract void ToggleUseHexadecimal();
		public abstract bool UseHexadecimal { get; set; }
		public abstract bool CanSwitchToThread { get; }
		public abstract void SwitchToThread(bool newTab);
		public abstract bool CanRenameThread { get; }
		public abstract void RenameThread();
		public abstract bool CanFreezeThread { get; }
		public abstract void FreezeThread();
		public abstract bool CanThawThread { get; }
		public abstract void ThawThread();
		public abstract bool CanResetSearchSettings { get; }
		public abstract void ResetSearchSettings();
		public abstract bool IsEditingValues { get; }
	}

	[Export(typeof(ThreadsOperations))]
	sealed class ThreadsOperationsImpl : ThreadsOperations {
		readonly IThreadsVM threadsVM;
		readonly DebuggerSettings debuggerSettings;
		readonly Lazy<ReferenceNavigatorService> referenceNavigatorService;
		readonly Lazy<DbgCallStackService> dbgCallStackService;

		BulkObservableCollection<ThreadVM> AllItems => threadsVM.AllItems;
		ObservableCollection<ThreadVM> SelectedItems => threadsVM.SelectedItems;
		IEnumerable<ThreadVM> SortedSelectedItems => threadsVM.Sort(SelectedItems);

		[ImportingConstructor]
		ThreadsOperationsImpl(IThreadsVM threadsVM, DebuggerSettings debuggerSettings, Lazy<ReferenceNavigatorService> referenceNavigatorService, Lazy<DbgCallStackService> dbgCallStackService) {
			this.threadsVM = threadsVM;
			this.debuggerSettings = debuggerSettings;
			this.referenceNavigatorService = referenceNavigatorService;
			this.dbgCallStackService = dbgCallStackService;
		}

		public override bool CanCopy => SelectedItems.Count != 0;
		public override void Copy() {
			var output = new DbgStringBuilderTextWriter();
			foreach (var vm in SortedSelectedItems) {
				var formatter = vm.Context.Formatter;
				bool needTab = false;
				foreach (var column in threadsVM.Descs.Columns) {
					if (!column.IsVisible)
						continue;
					if (column.Name == string.Empty)
						continue;

					if (needTab)
						output.Write(DbgTextColor.Text, "\t");
					switch (column.Id) {
					case ThreadsWindowColumnIds.Icon:
						formatter.WriteImage(output, vm);
						break;

					case ThreadsWindowColumnIds.ThreadID:
						formatter.WriteId(output, vm.Thread);
						break;

					case ThreadsWindowColumnIds.ThreadManagedId:
						formatter.WriteManagedId(output, vm.Thread);
						break;

					case ThreadsWindowColumnIds.ThreadCategory:
						formatter.WriteCategoryText(output, vm);
						break;

					case ThreadsWindowColumnIds.Name:
						formatter.WriteName(output, vm.Thread);
						break;

					case ThreadsWindowColumnIds.ThreadLocation:
						formatter.WriteLocation(output, vm);
						break;

					case ThreadsWindowColumnIds.ThreadPriority:
						formatter.WritePriority(output, vm);
						break;

					case ThreadsWindowColumnIds.ThreadAffinityMask:
						formatter.WriteAffinityMask(output, vm);
						break;

					case ThreadsWindowColumnIds.ThreadSuspendedCount:
						formatter.WriteSuspendedCount(output, vm.Thread);
						break;

					case ThreadsWindowColumnIds.ProcessName:
						formatter.WriteProcessName(output, vm.Thread);
						break;

					case ThreadsWindowColumnIds.AppDomain:
						formatter.WriteAppDomain(output, vm.Thread);
						break;

					case ThreadsWindowColumnIds.ThreadState:
						formatter.WriteState(output, vm.Thread);
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

		public override bool CanToggleUseHexadecimal => true;
		public override void ToggleUseHexadecimal() => UseHexadecimal = !UseHexadecimal;
		public override bool UseHexadecimal {
			get => debuggerSettings.UseHexadecimal;
			set => debuggerSettings.UseHexadecimal = value;
		}

		public override bool CanSwitchToThread => SelectedItems.Count == 1;
		public override void SwitchToThread(bool newTab) {
			if (!CanSwitchToThread)
				return;
			var thread = SelectedItems[0].Thread;
			thread.Process.DbgManager.CurrentThread.Current = thread;
			var info = ThreadUtilities.GetFirstFrameLocation(thread);
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

		public override bool CanRenameThread => SelectedItems.Count == 1 && !SelectedItems[0].NameEditableValue.IsEditingValue;
		public override void RenameThread() {
			if (!CanRenameThread)
				return;
			SelectedItems[0].ClearEditingValueProperties();
			SelectedItems[0].NameEditableValue.IsEditingValue = true;
		}

		public override bool CanFreezeThread => SelectedItems.Any(a => a.Thread.SuspendedCount == 0);
		public override void FreezeThread() {
			foreach (var vm in SelectedItems) {
				if (vm.Thread.SuspendedCount == 0)
					vm.Thread.Freeze();
			}
		}

		public override bool CanThawThread => SelectedItems.Any(a => a.Thread.SuspendedCount != 0);
		public override void ThawThread() {
			foreach (var vm in SelectedItems) {
				if (vm.Thread.SuspendedCount != 0)
					vm.Thread.Thaw();
			}
		}

		public override bool CanResetSearchSettings => true;
		public override void ResetSearchSettings() => threadsVM.ResetSearchSettings();

		public override bool IsEditingValues {
			get {
				foreach (var vm in SelectedItems) {
					if (vm.IsEditingValues)
						return true;
				}
				return false;
			}
		}
	}
}
