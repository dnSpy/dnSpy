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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Hex;
using dnSpy.Contracts.Text;
using dnSpy.Debugger.ToolWindows.Memory;

namespace dnSpy.Debugger.ToolWindows.Modules {
	abstract class ModulesOperations {
		public abstract bool CanCopy { get; }
		public abstract void Copy();
		public abstract bool CanSelectAll { get; }
		public abstract void SelectAll();
		public abstract bool CanGoToModule { get; }
		public abstract void GoToModule(bool newTab);
		public abstract int LoadModulesCount { get; }
		public abstract bool CanLoadModules { get; }
		public abstract void LoadModules();
		public abstract bool CanOpenModuleFromMemory { get; }
		public abstract void OpenModuleFromMemory(bool newTab);
		public abstract bool CanShowInMemoryWindow { get; }
		public abstract void ShowInMemoryWindow(int windowIndex);
		public abstract void ShowInMemoryWindow();
		public abstract bool CanToggleUseHexadecimal { get; }
		public abstract void ToggleUseHexadecimal();
		public abstract bool UseHexadecimal { get; set; }
		public abstract bool CanOpenContainingFolder { get; }
		public abstract void OpenContainingFolder();
		public abstract bool CanCopyFilename { get; }
		public abstract void CopyFilename();
		public abstract int GetSaveModuleCount();
		public abstract bool CanSave { get; }
		public abstract void Save();
	}

	[Export(typeof(ModulesOperations))]
	sealed class ModulesOperationsImpl : ModulesOperations {
		readonly IModulesVM modulesVM;
		readonly DebuggerSettings debuggerSettings;
		readonly Lazy<ModulesSaver> modulesSaver;
		readonly Lazy<MemoryWindowService> memoryWindowService;

		ObservableCollection<ModuleVM> AllItems => modulesVM.AllItems;
		ObservableCollection<ModuleVM> SelectedItems => modulesVM.SelectedItems;
		//TODO: This should be view order
		IEnumerable<ModuleVM> SortedSelectedItems => SelectedItems.OrderBy(a => a.Order);

		[ImportingConstructor]
		ModulesOperationsImpl(IModulesVM modulesVM, DebuggerSettings debuggerSettings, Lazy<ModulesSaver> modulesSaver, Lazy<MemoryWindowService> memoryWindowService) {
			this.modulesVM = modulesVM;
			this.debuggerSettings = debuggerSettings;
			this.modulesSaver = modulesSaver;
			this.memoryWindowService = memoryWindowService;
		}

		public override bool CanCopy => SelectedItems.Count != 0;
		public override void Copy() {
			var output = new StringBuilderTextColorOutput();
			foreach (var vm in SortedSelectedItems) {
				var formatter = vm.Context.Formatter;
				formatter.WriteName(output, vm.Module);
				output.Write(BoxedTextColor.Text, "\t");
				formatter.WriteOptimized(output, vm.Module);
				output.Write(BoxedTextColor.Text, "\t");
				formatter.WriteDynamic(output, vm.Module);
				output.Write(BoxedTextColor.Text, "\t");
				formatter.WriteInMemory(output, vm.Module);
				output.Write(BoxedTextColor.Text, "\t");
				formatter.WriteOrder(output, vm.Module);
				output.Write(BoxedTextColor.Text, "\t");
				formatter.WriteVersion(output, vm.Module);
				output.Write(BoxedTextColor.Text, "\t");
				formatter.WriteTimestamp(output, vm.Module);
				output.Write(BoxedTextColor.Text, "\t");
				formatter.WriteAddress(output, vm.Module);
				output.Write(BoxedTextColor.Text, "\t");
				formatter.WriteProcess(output, vm.Module);
				output.Write(BoxedTextColor.Text, "\t");
				formatter.WriteAppDomain(output, vm.Module);
				output.Write(BoxedTextColor.Text, "\t");
				formatter.WritePath(output, vm.Module);
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

		public override bool CanGoToModule => SelectedItems.Count == 1;
		public override void GoToModule(bool newTab) {
			//TODO:
		}

		public override int LoadModulesCount => SelectedItems.Count;
		public override bool CanLoadModules => SelectedItems.Count > 1;
		public override void LoadModules() {
			//TODO:
		}

		public override bool CanOpenModuleFromMemory => SelectedItems.Count == 1 && !SelectedItems[0].Module.IsDynamic && !SelectedItems[0].Module.IsInMemory;
		public override void OpenModuleFromMemory(bool newTab) {
			//TODO:
		}

		public override bool CanShowInMemoryWindow => GetShowInMemoryWindowModule() != null;
		public override void ShowInMemoryWindow(int windowIndex) {
			if ((uint)windowIndex >= (uint)MemoryWindowsHelper.NUMBER_OF_MEMORY_WINDOWS)
				throw new ArgumentOutOfRangeException(nameof(windowIndex));
			ShowInMemoryWindowCore(windowIndex);
		}
		ModuleVM GetShowInMemoryWindowModule() {
			if (SelectedItems.Count != 1)
				return null;
			var vm = SelectedItems[0];
			if (!vm.Module.HasAddress)
				return null;
			return vm;
		}
		public override void ShowInMemoryWindow() => ShowInMemoryWindowCore(null);
		void ShowInMemoryWindowCore(int? windowIndex) {
			var vm = GetShowInMemoryWindowModule();
			if (vm != null) {
				var start = new HexPosition(vm.Module.Address);
				var end = start + vm.Module.Size;
				Debug.Assert(end <= HexPosition.MaxEndPosition);
				if (end <= HexPosition.MaxEndPosition) {
					if (windowIndex != null)
						memoryWindowService.Value.Show(vm.Module.Process.Id, HexSpan.FromBounds(start, end), windowIndex.Value);
					else
						memoryWindowService.Value.Show(vm.Module.Process.Id, HexSpan.FromBounds(start, end));
				}
			}
		}

		public override bool CanToggleUseHexadecimal => true;
		public override void ToggleUseHexadecimal() => UseHexadecimal = !UseHexadecimal;
		public override bool UseHexadecimal {
			get => debuggerSettings.UseHexadecimal;
			set => debuggerSettings.UseHexadecimal = value;
		}

		public override bool CanOpenContainingFolder => GetFilename() != null;
		public override void OpenContainingFolder() {
			var filename = GetFilename();
			if (filename == null)
				return;
			// Known problem: explorer can't show files in the .NET 2.0 GAC.
			var args = string.Format("/select,{0}", filename);
			try {
				Process.Start(new ProcessStartInfo("explorer.exe", args));
			}
			catch {
			}
		}
		string GetFilename() {
			if (SelectedItems.Count != 1)
				return null;
			var path = SelectedItems[0].Module.Filename;
			if (!File.Exists(path))
				return null;
			return path;
		}

		public override bool CanCopyFilename => SelectedItems.Count == 1;
		public override void CopyFilename() {
			if (SelectedItems.Count == 0)
				return;
			try {
				Clipboard.SetText(SelectedItems[0].Module.Filename);
			}
			catch (ExternalException) { }
		}

		public override int GetSaveModuleCount() => GetModulesToSave().Length;
		public override bool CanSave => GetModulesToSave().Length != 0;
		public override void Save() => modulesSaver.Value.Save(GetModulesToSave());
		ModuleVM[] GetModulesToSave() => modulesSaver.Value.FilterModules(SelectedItems);
	}
}
