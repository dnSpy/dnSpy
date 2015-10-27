/*
    Copyright (C) 2014-2015 de4dot@gmail.com

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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using dndbg.Engine;
using dnlib.DotNet;
using dnSpy.Debugger.IMModules;
using dnSpy.Debugger.Memory;
using dnSpy.Files;
using dnSpy.Images;
using dnSpy.MVVM;
using dnSpy.MVVM.Dialogs;
using dnSpy.NRefactory;
using ICSharpCode.Decompiler;
using ICSharpCode.ILSpy;

namespace dnSpy.Debugger.Modules {
	sealed class ModulesCtxMenuContext {
		public readonly ModulesVM VM;
		public readonly ModuleVM[] SelectedItems;

		public ModulesCtxMenuContext(ModulesVM vm, ModuleVM[] selItems) {
			this.VM = vm;
			this.SelectedItems = selItems;
		}
	}

	sealed class ModulesCtxMenuCommandProxy : ContextMenuEntryCommandProxy {
		public ModulesCtxMenuCommandProxy(ModulesCtxMenuCommand cmd)
			: base(cmd) {
		}

		protected override ContextMenuEntryContext CreateContext() {
			return ContextMenuEntryContext.Create(ModulesControlCreator.ModulesControlInstance.listView);
		}
	}

	abstract class ModulesCtxMenuCommand : ContextMenuEntryBase<ModulesCtxMenuContext> {
		protected override ModulesCtxMenuContext CreateContext(ContextMenuEntryContext context) {
			if (DebugManager.Instance.ProcessState == DebuggerProcessState.Terminated)
				return null;
			var ui = ModulesControlCreator.ModulesControlInstance;
			if (context.Element != ui.listView)
				return null;
			var vm = ui.DataContext as ModulesVM;
			if (vm == null)
				return null;

			var elems = ui.listView.SelectedItems.OfType<ModuleVM>().ToArray();
			Array.Sort(elems, (a, b) => a.Module.ModuleOrder.CompareTo(b.Module.ModuleOrder));

			return new ModulesCtxMenuContext(vm, elems);
		}
	}

	[ExportContextMenuEntry(Header = "Cop_y", Order = 100, Category = "CopyMOD", Icon = "Copy", InputGestureText = "Ctrl+C")]
	sealed class CopyCallModulesCtxMenuCommand : ModulesCtxMenuCommand {
		protected override void Execute(ModulesCtxMenuContext context) {
			var output = new PlainTextOutput();
			foreach (var vm in context.SelectedItems) {
				var printer = new ModulePrinter(output, DebuggerSettings.Instance.UseHexadecimal);
				printer.WriteName(vm);
				output.Write('\t', TextTokenType.Text);
				printer.WriteOptimized(vm);
				output.Write('\t', TextTokenType.Text);
				printer.WriteDynamic(vm);
				output.Write('\t', TextTokenType.Text);
				printer.WriteInMemory(vm);
				output.Write('\t', TextTokenType.Text);
				printer.WriteOrder(vm);
				output.Write('\t', TextTokenType.Text);
				printer.WriteVersion(vm);
				output.Write('\t', TextTokenType.Text);
				printer.WriteTimestamp(vm);
				output.Write('\t', TextTokenType.Text);
				printer.WriteAddress(vm);
				output.Write('\t', TextTokenType.Text);
				printer.WriteProcess(vm);
				output.Write('\t', TextTokenType.Text);
				printer.WriteAppDomain(vm);
				output.Write('\t', TextTokenType.Text);
				printer.WritePath(vm);
				output.WriteLine();
			}
			var s = output.ToString();
			if (s.Length > 0)
				Clipboard.SetText(s);
		}

		protected override bool IsEnabled(ModulesCtxMenuContext context) {
			return context.SelectedItems.Length > 0;
		}
	}

	[ExportContextMenuEntry(Header = "Select _All", Order = 110, Category = "CopyMOD", Icon = "Select", InputGestureText = "Ctrl+A")]
	sealed class SelectAllModulesCtxMenuCommand : ModulesCtxMenuCommand {
		protected override void Execute(ModulesCtxMenuContext context) {
			ModulesControlCreator.ModulesControlInstance.listView.SelectAll();
		}

		protected override bool IsEnabled(ModulesCtxMenuContext context) {
			return ModulesControlCreator.ModulesControlInstance.listView.Items.Count > 0;
		}
	}

	[ExportContextMenuEntry(Header = "_Go To Module", Order = 200, Category = "MODGoTo", Icon = "AssemblyModule", InputGestureText = "Enter")]
	sealed class GoToModuleModulesCtxMenuCommand : ModulesCtxMenuCommand {
		protected override void Execute(ModulesCtxMenuContext context) {
			ExecuteInternal(context, false);
		}

		protected override bool IsEnabled(ModulesCtxMenuContext context) {
			return CanGoToModule(context);
		}

		internal static bool CanGoToModule(ModulesCtxMenuContext context) {
			return context != null && context.SelectedItems.Length != 0;
		}

		internal static void ExecuteInternal(ModulesCtxMenuContext context, bool newTab) {
			if (context == null || context.SelectedItems.Length == 0)
				return;
			ExecuteInternal(context.SelectedItems[0], newTab);
		}

		internal static void ExecuteInternal(ModuleVM vm, bool newTab) {
			if (vm == null)
				return;
			if (ShowErrorIfDynamic(vm.Module))
				GoToFile(ModuleLoader.Instance.LoadModule(vm.Module, true), newTab);
		}

		internal static bool ShowErrorIfDynamic(DnModule module) {
			if (module.IsDynamic && DebugManager.Instance.ProcessState != DebuggerProcessState.Stopped) {
				if (InMemoryModuleManager.Instance.LoadFile(module, false) == null) {
					MainWindow.Instance.ShowMessageBox("You must break the process before dynamic modules can be loaded.");
					return false;
				}
			}
			return true;
		}

		internal static void GoToFile(DnSpyFile file, bool newTab) {
			if (file == null)
				return;
			var mod = file.ModuleDef;
			if (mod == null)
				return;
			// The file could've been added lazily to the list so add a short delay before we select it
			MainWindow.Instance.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => {
				if (newTab)
					MainWindow.Instance.OpenNewEmptyTab();
				MainWindow.Instance.JumpToReference(mod);
			}));
		}
	}

	sealed class GoToModuleNewTabModulesCtxMenuCommand : ModulesCtxMenuCommand {
		protected override void Execute(ModulesCtxMenuContext context) {
			GoToModuleModulesCtxMenuCommand.ExecuteInternal(context, true);
		}

		protected override bool IsEnabled(ModulesCtxMenuContext context) {
			return GoToModuleModulesCtxMenuCommand.CanGoToModule(context);
		}
	}

	[ExportContextMenuEntry(Header = "Open Module from Memory", Order = 210, Category = "MODGoTo", Icon = "AssemblyModule")]
	sealed class OpenModuleFromMemoryModulesCtxMenuCommand : ModulesCtxMenuCommand {
		protected override void Execute(ModulesCtxMenuContext context) {
			ExecuteInternal(context, false);
		}

		protected override bool IsVisible(ModulesCtxMenuContext context) {
			return IsEnabled(context);
		}

		protected override bool IsEnabled(ModulesCtxMenuContext context) {
			return CanGoToModule(context);
		}

		static bool CanGoToModule(ModulesCtxMenuContext context) {
			if (context == null || context.SelectedItems.Length == 0)
				return false;
			var vm = context.SelectedItems[0];
			return !vm.Module.IsDynamic && !vm.Module.IsInMemory;
		}

		static void ExecuteInternal(ModulesCtxMenuContext context, bool newTab) {
			if (context == null || context.SelectedItems.Length == 0)
				return;
			ExecuteInternal(context.SelectedItems[0], newTab);
		}

		static void ExecuteInternal(ModuleVM vm, bool newTab) {
			if (vm == null)
				return;

			if (GoToModuleModulesCtxMenuCommand.ShowErrorIfDynamic(vm.Module))
				GoToModuleModulesCtxMenuCommand.GoToFile(InMemoryModuleManager.Instance.LoadFile(vm.Module, true), newTab);
		}
	}

	[ExportContextMenuEntry(Header = "Show in Memory Window", Order = 220, Category = "MODGoTo", Icon = "MemoryWindow")]
	sealed class ShowInMemoryXModulesCtxMenuCommand : ModulesCtxMenuCommand {
		protected override void Execute(ModulesCtxMenuContext context) {
		}

		static ShowInMemoryXModulesCtxMenuCommand() {
			subCmds = new Tuple<ICommand, string, string>[MemoryControlCreator.NUMBER_OF_MEMORY_WINDOWS];
			for (int i = 0; i < subCmds.Length; i++)
				subCmds[i] = Tuple.Create((ICommand)new ModulesCtxMenuCommandProxy(new ShowInMemoryWindowModulesCtxMenuCommand(i + 1)), MemoryControlCreator.GetHeaderText(i), MemoryControlCreator.GetCtrlInputGestureText(i));
		}

		static readonly Tuple<ICommand, string, string>[] subCmds;

		protected override void Initialize(ModulesCtxMenuContext context, MenuItem menuItem) {
			foreach (var tuple in subCmds) {
				var mi = new MenuItem {
					Command = tuple.Item1,
					Header = tuple.Item2,
				};
				if (!string.IsNullOrEmpty(tuple.Item3))
					mi.InputGestureText = tuple.Item3;
				MainWindow.CreateMenuItemImage(mi, this, "MemoryWindow", BackgroundType.ContextMenuItem);
				menuItem.Items.Add(mi);
			}
		}
	}

	sealed class ShowInMemoryModulesCtxMenuCommand : ModulesCtxMenuCommand {
		protected override void Execute(ModulesCtxMenuContext context) {
			var vm = ShowInMemoryWindowModulesCtxMenuCommand.GetModule(context);
			if (vm != null)
				MemoryUtils.ShowInMemoryWindow(vm.Module.Address, vm.Module.Size);
		}

		protected override bool IsEnabled(ModulesCtxMenuContext context) {
			return ShowInMemoryWindowModulesCtxMenuCommand.GetModule(context) != null;
		}
	}

	sealed class ShowInMemoryWindowModulesCtxMenuCommand : ModulesCtxMenuCommand {
		readonly int windowNumber;

		public ShowInMemoryWindowModulesCtxMenuCommand(int windowNumber) {
			this.windowNumber = windowNumber;
		}

		protected override void Execute(ModulesCtxMenuContext context) {
			var vm = GetModule(context);
			if (vm != null)
				MemoryUtils.ShowInMemoryWindow(windowNumber, vm.Module.Address, vm.Module.Size);
		}

		protected override bool IsEnabled(ModulesCtxMenuContext context) {
			return GetModule(context) != null;
		}

		internal static ModuleVM GetModule(ModulesCtxMenuContext context) {
			if (context.SelectedItems.Length != 1)
				return null;
			var vm = context.SelectedItems[0];
			if (vm.Module.Address == 0 || vm.Module.Size == 0)
				return null;
			return vm;
		}
	}

	[ExportContextMenuEntry(Header = "_Hexadecimal Display", Order = 300, Category = "MODMiscOptions")]
	sealed class HexadecimalDisplayModulesCtxMenuCommand : ModulesCtxMenuCommand {
		protected override void Execute(ModulesCtxMenuContext context) {
			DebuggerSettings.Instance.UseHexadecimal = !DebuggerSettings.Instance.UseHexadecimal;
		}

		protected override void Initialize(ModulesCtxMenuContext context, MenuItem menuItem) {
			menuItem.IsChecked = DebuggerSettings.Instance.UseHexadecimal;
		}
	}

	[ExportContextMenuEntry(Header = "_Open Containing Folder", Order = 400, Category = "MOD1")]
	sealed class OpenContainingFolderModulesCtxMenuCommand : ModulesCtxMenuCommand {
		protected override void Execute(ModulesCtxMenuContext context) {
			if (context.SelectedItems.Length > 0)
				OpenContainingFolder(context.SelectedItems[0].Module.Name);
		}

		protected override bool IsEnabled(ModulesCtxMenuContext context) {
			return context.SelectedItems.Length == 1 &&
				!context.SelectedItems[0].Module.IsDynamic &&
				!context.SelectedItems[0].Module.IsInMemory;
		}

		static void OpenContainingFolder(string filename) {
			// Known problem: explorer can't show files in the .NET 2.0 GAC.
			var args = string.Format("/select,{0}", filename);
			try {
				Process.Start(new ProcessStartInfo("explorer.exe", args));
			}
			catch {
			}
		}
	}

	[ExportContextMenuEntry(Header = "Copy Filename", Order = 410, Category = "MOD1")]
	sealed class CopyFilenameModulesCtxMenuCommand : ModulesCtxMenuCommand {
		protected override void Execute(ModulesCtxMenuContext context) {
			if (context.SelectedItems.Length > 0)
				Clipboard.SetText(context.SelectedItems[0].Module.Name);
		}

		protected override bool IsEnabled(ModulesCtxMenuContext context) {
			return context.SelectedItems.Length == 1;
		}
	}

	[ExportContextMenuEntry(Order = 500, Category = "MOD2")]
	sealed class SaveModuleToDiskModulesCtxMenuCommand : ModulesCtxMenuCommand {
		protected override void Execute(ModulesCtxMenuContext context) {
			Save(GetSavableFiles(context.SelectedItems));
		}

		static string GetModuleFilename(DnModule module) {
			if (module.IsDynamic)
				return null;
			if (!module.IsInMemory)
				return DebugOutputUtils.GetFilename(module.Name);
			if (module.CorModule.IsManifestModule)
				return DebugOutputUtils.GetFilename(new AssemblyNameInfo(module.Assembly.FullName).Name);
			return DebugOutputUtils.GetFilename(module.DnlibName);
		}

		static void Save(ModuleVM[] files) {
			var list = new Tuple<DnModule, string>[files.Length];
			if (files.Length == 1) {
				var vm = files[0];
				var filename = new PickSaveFilename().GetFilename(GetModuleFilename(vm.Module), GetDefaultExtension(GetModuleFilename(vm.Module), vm.IsExe, vm.Module.CorModule.IsManifestModule), PickFilenameConstants.DotNetAssemblyOrModuleFilter);
				if (string.IsNullOrEmpty(filename))
					return;
				list[0] = Tuple.Create(vm.Module, filename);
			}
			else {
				var dir = new PickDirectory().GetDirectory(null);
				if (!Directory.Exists(dir))
					return;
				for (int i = 0; i < files.Length; i++) {
					var file = files[i];
					var filename = DebugOutputUtils.GetFilename(file.Module.Name);
					var lf = filename.ToUpperInvariant();
					if (lf.EndsWith(".EXE") || lf.EndsWith(".DLL") || lf.EndsWith(".NETMODULE")) {
					}
					else if (file.Module.CorModule.IsManifestModule)
						filename += file.IsExe ? ".exe" : ".dll";
					else
						filename += ".netmodule";
					list[i] = Tuple.Create(file.Module, Path.Combine(dir, filename));
				}
			}

			var data = new ProgressVM(MainWindow.Instance.Dispatcher, new PEFilesSaver(list));
			var win = new ProgressDlg();
			win.DataContext = data;
			win.Owner = MainWindow.Instance;
			win.Title = list.Length == 1 ? "Save Module" : "Save Modules";
			var res = win.ShowDialog();
			if (res != true)
				return;
			if (!data.WasError)
				return;
			MainWindow.Instance.ShowMessageBox(string.Format("An error occurred:\n\n{0}", data.ErrorMessage));
		}

		protected override bool IsEnabled(ModulesCtxMenuContext context) {
			return GetSavableFiles(context.SelectedItems).Length > 0;
		}

		protected override void Initialize(ModulesCtxMenuContext context, MenuItem menuItem) {
			var files = GetSavableFiles(context.SelectedItems);
			menuItem.Header = files.Length > 1 ? string.Format("Save {0} Modules...", files.Length) : "Save Module...";
		}

		static ModuleVM[] GetSavableFiles(ModuleVM[] files) {
			return files.Where(a => a.Module.CorModule.Address != 0 && a.Module.CorModule.Size > 0 && !a.Module.CorModule.IsDynamic).ToArray();
		}

		static string GetDefaultExtension(string name, bool isExe, bool isManifestModule) {
			if (!isManifestModule)
				return ".netmodule";
			try {
				var ext = Path.GetExtension(name);
				if (ext.Length > 0 && ext[0] == '.')
					return ext.Substring(1);
			}
			catch {
			}
			return isExe ? "exe" : "dll";
		}
	}
}
