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
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Threading;
using dndbg.Engine;
using dnlib.DotNet;
using dnSpy.Contracts.Menus;
using dnSpy.Debugger.IMModules;
using dnSpy.Debugger.Memory;
using dnSpy.Files;
using dnSpy.MVVM;
using dnSpy.MVVM.Dialogs;
using dnSpy.NRefactory;
using dnSpy.Shared.UI.Menus;
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

	sealed class ModulesCtxMenuCommandProxy : MenuItemCommandProxy<ModulesCtxMenuContext> {
		public ModulesCtxMenuCommandProxy(ModulesCtxMenuCommand cmd)
			: base(cmd) {
		}

		protected override ModulesCtxMenuContext CreateContext() {
			return ModulesCtxMenuCommand.Create();
		}
	}

	abstract class ModulesCtxMenuCommand : MenuItemBase<ModulesCtxMenuContext> {
		protected sealed override object CachedContextKey {
			get { return ContextKey; }
		}
		static readonly object ContextKey = new object();

		protected sealed override ModulesCtxMenuContext CreateContext(IMenuItemContext context) {
			if (DebugManager.Instance.ProcessState == DebuggerProcessState.Terminated)
				return null;
			var ui = ModulesControlCreator.ModulesControlInstance;
			if (context.CreatorObject.Object != ui.listView)
				return null;
			return Create();
		}

		internal static ModulesCtxMenuContext Create() {
			var ui = ModulesControlCreator.ModulesControlInstance;
			var vm = ui.DataContext as ModulesVM;
			if (vm == null)
				return null;

			var elems = ui.listView.SelectedItems.OfType<ModuleVM>().ToArray();
			Array.Sort(elems, (a, b) => a.Module.ModuleOrder.CompareTo(b.Module.ModuleOrder));

			return new ModulesCtxMenuContext(vm, elems);
		}
	}

	[ExportMenuItem(Header = "Cop_y", Icon = "Copy", InputGestureText = "Ctrl+C", Group = MenuConstants.GROUP_CTX_DBG_MODULES_COPY, Order = 0)]
	sealed class CopyCallModulesCtxMenuCommand : ModulesCtxMenuCommand {
		public override void Execute(ModulesCtxMenuContext context) {
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

		public override bool IsEnabled(ModulesCtxMenuContext context) {
			return context.SelectedItems.Length > 0;
		}
	}

	[ExportMenuItem(Header = "Select _All", Icon = "Select", InputGestureText = "Ctrl+A", Group = MenuConstants.GROUP_CTX_DBG_MODULES_COPY, Order = 10)]
	sealed class SelectAllModulesCtxMenuCommand : ModulesCtxMenuCommand {
		public override void Execute(ModulesCtxMenuContext context) {
			ModulesControlCreator.ModulesControlInstance.listView.SelectAll();
		}

		public override bool IsEnabled(ModulesCtxMenuContext context) {
			return ModulesControlCreator.ModulesControlInstance.listView.Items.Count > 0;
		}
	}

	[ExportMenuItem(Header = "_Go To Module", Icon = "AssemblyModule", InputGestureText = "Enter", Group = MenuConstants.GROUP_CTX_DBG_MODULES_GOTO, Order = 0)]
	sealed class GoToModuleModulesCtxMenuCommand : ModulesCtxMenuCommand {
		public override void Execute(ModulesCtxMenuContext context) {
			ExecuteInternal(context, false);
		}

		public override bool IsEnabled(ModulesCtxMenuContext context) {
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
		public override void Execute(ModulesCtxMenuContext context) {
			GoToModuleModulesCtxMenuCommand.ExecuteInternal(context, true);
		}

		public override bool IsEnabled(ModulesCtxMenuContext context) {
			return GoToModuleModulesCtxMenuCommand.CanGoToModule(context);
		}
	}

	[ExportMenuItem(Header = "Open Module from Memory", Icon = "AssemblyModule", Group = MenuConstants.GROUP_CTX_DBG_MODULES_GOTO, Order = 10)]
	sealed class OpenModuleFromMemoryModulesCtxMenuCommand : ModulesCtxMenuCommand {
		public override void Execute(ModulesCtxMenuContext context) {
			ExecuteInternal(context, false);
		}

		public override bool IsVisible(ModulesCtxMenuContext context) {
			return IsEnabled(context);
		}

		public override bool IsEnabled(ModulesCtxMenuContext context) {
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

	static class Constants {
		public const string SHOW_IN_MEMORY_WINDOW_GUID = "517AC97D-2619-477E-961E-B5519BB7FCE3";
		public const string GROUP_SHOW_IN_MEMORY_WINDOW = "0,E1F6906B-64C8-4411-B8B7-07C331197BFE";
	}

	[ExportMenuItem(Header = "Show in Memory Window", Icon = "MemoryWindow", Guid = Constants.SHOW_IN_MEMORY_WINDOW_GUID, Group = MenuConstants.GROUP_CTX_DBG_MODULES_GOTO, Order = 20)]
	sealed class ShowInMemoryXModulesCtxMenuCommand : ModulesCtxMenuCommand {
		public override void Execute(ModulesCtxMenuContext context) {
		}
	}

	[ExportMenuItem(OwnerGuid = Constants.SHOW_IN_MEMORY_WINDOW_GUID, Group = Constants.GROUP_SHOW_IN_MEMORY_WINDOW, Order = 0)]
	sealed class ShowInMemoryXModulesSubCtxMenuCommand : ModulesCtxMenuCommand, IMenuItemCreator {
		public override void Execute(ModulesCtxMenuContext context) {
		}

		static ShowInMemoryXModulesSubCtxMenuCommand() {
			subCmds = new Tuple<IMenuItem, string, string>[MemoryControlCreator.NUMBER_OF_MEMORY_WINDOWS];
			for (int i = 0; i < subCmds.Length; i++)
				subCmds[i] = Tuple.Create((IMenuItem)new ShowInMemoryWindowModulesCtxMenuCommand(i + 1), MemoryControlCreator.GetHeaderText(i), MemoryControlCreator.GetCtrlInputGestureText(i));
		}

		static readonly Tuple<IMenuItem, string, string>[] subCmds;

		public IEnumerable<CreatedMenuItem> Create(IMenuItemContext context) {
			var ctx = CreateContext(context);
			Debug.Assert(ctx != null);
			if (ctx == null)
				yield break;

			for (int i = 0; i < subCmds.Length; i++) {
				var info = subCmds[i];
				var attr = new ExportMenuItemAttribute { Header = info.Item2, Icon = "MemoryWindow" };
				if (!string.IsNullOrEmpty(info.Item3))
					attr.InputGestureText = info.Item3;
				yield return new CreatedMenuItem(attr, info.Item1);
			}
		}
	}

	sealed class ShowInMemoryModulesCtxMenuCommand : ModulesCtxMenuCommand {
		public override void Execute(ModulesCtxMenuContext context) {
			var vm = ShowInMemoryWindowModulesCtxMenuCommand.GetModule(context);
			if (vm != null)
				MemoryUtils.ShowInMemoryWindow(vm.Module.Address, vm.Module.Size);
		}

		public override bool IsEnabled(ModulesCtxMenuContext context) {
			return ShowInMemoryWindowModulesCtxMenuCommand.GetModule(context) != null;
		}
	}

	sealed class ShowInMemoryWindowModulesCtxMenuCommand : ModulesCtxMenuCommand {
		readonly int windowNumber;

		public ShowInMemoryWindowModulesCtxMenuCommand(int windowNumber) {
			this.windowNumber = windowNumber;
		}

		public override void Execute(ModulesCtxMenuContext context) {
			var vm = GetModule(context);
			if (vm != null)
				MemoryUtils.ShowInMemoryWindow(windowNumber, vm.Module.Address, vm.Module.Size);
		}

		public override bool IsEnabled(ModulesCtxMenuContext context) {
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

	[ExportMenuItem(Header = "_Hexadecimal Display", Group = MenuConstants.GROUP_CTX_DBG_MODULES_HEXOPTS, Order = 0)]
	sealed class HexadecimalDisplayModulesCtxMenuCommand : ModulesCtxMenuCommand {
		public override void Execute(ModulesCtxMenuContext context) {
			DebuggerSettings.Instance.UseHexadecimal = !DebuggerSettings.Instance.UseHexadecimal;
		}

		public override bool IsChecked(ModulesCtxMenuContext context) {
			return DebuggerSettings.Instance.UseHexadecimal;
		}
	}

	[ExportMenuItem(Header = "_Open Containing Folder", Group = MenuConstants.GROUP_CTX_DBG_MODULES_DIRS, Order = 0)]
	sealed class OpenContainingFolderModulesCtxMenuCommand : ModulesCtxMenuCommand {
		public override void Execute(ModulesCtxMenuContext context) {
			if (context.SelectedItems.Length > 0)
				OpenContainingFolder(context.SelectedItems[0].Module.Name);
		}

		public override bool IsEnabled(ModulesCtxMenuContext context) {
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

	[ExportMenuItem(Header = "Copy Filename", Group = MenuConstants.GROUP_CTX_DBG_MODULES_DIRS, Order = 10)]
	sealed class CopyFilenameModulesCtxMenuCommand : ModulesCtxMenuCommand {
		public override void Execute(ModulesCtxMenuContext context) {
			if (context.SelectedItems.Length > 0)
				Clipboard.SetText(context.SelectedItems[0].Module.Name);
		}

		public override bool IsEnabled(ModulesCtxMenuContext context) {
			return context.SelectedItems.Length == 1;
		}
	}

	[ExportMenuItem(Group = MenuConstants.GROUP_CTX_DBG_MODULES_SAVE, Order = 0)]
	sealed class SaveModuleToDiskModulesCtxMenuCommand : ModulesCtxMenuCommand {
		public override void Execute(ModulesCtxMenuContext context) {
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

		public override bool IsEnabled(ModulesCtxMenuContext context) {
			return GetSavableFiles(context.SelectedItems).Length > 0;
		}

		public override string GetHeader(ModulesCtxMenuContext context) {
			var files = GetSavableFiles(context.SelectedItems);
			return files.Length > 1 ? string.Format("Save {0} Modules...", files.Length) : "Save Module...";
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
