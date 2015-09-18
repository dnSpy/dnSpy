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
using dndbg.Engine;
using dnSpy.MVVM;
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

	[ExportContextMenuEntry(Header = "_Hexadecimal Display", Order = 200, Category = "MODMiscOptions")]
	sealed class HexadecimalDisplayModulesCtxMenuCommand : ModulesCtxMenuCommand {
		protected override void Execute(ModulesCtxMenuContext context) {
			DebuggerSettings.Instance.UseHexadecimal = !DebuggerSettings.Instance.UseHexadecimal;
		}

		protected override void Initialize(ModulesCtxMenuContext context, MenuItem menuItem) {
			menuItem.IsChecked = DebuggerSettings.Instance.UseHexadecimal;
		}
	}

	[ExportContextMenuEntry(Header = "_Open Containing Folder", Order = 300, Category = "MOD1")]
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

	[ExportContextMenuEntry(Header = "Copy Filename", Order = 310, Category = "MOD1")]
	sealed class CopyFilenameModulesCtxMenuCommand : ModulesCtxMenuCommand {
		protected override void Execute(ModulesCtxMenuContext context) {
			if (context.SelectedItems.Length > 0)
				Clipboard.SetText(context.SelectedItems[0].Module.Name);
		}

		protected override bool IsEnabled(ModulesCtxMenuContext context) {
			return context.SelectedItems.Length == 1;
		}
	}

	[ExportContextMenuEntry(Order = 400, Category = "MOD2")]
	sealed class SaveModuleToDiskModulesCtxMenuCommand : ModulesCtxMenuCommand {
		protected override void Execute(ModulesCtxMenuContext context) {
			Save(GetSavableFiles(context.SelectedItems));
		}

		static void Save(ModuleVM[] files) {
			if (files.Length == 0)
				return;
			var buffer = new byte[0x10000];
			if (files.Length == 1) {
				var vm = files[0];
				var filename = new PickSaveFilename().GetFilename(vm.Module.Name, GetDefaultExtension(vm.Module.Name, vm.IsExe), PickFilenameConstants.DotNetAssemblyOrModuleFilter);
				if (string.IsNullOrEmpty(filename))
					return;
				Save(vm.Module, filename, buffer);
			}
			else {
				var dir = new PickDirectory().GetDirectory(null);
				if (string.IsNullOrEmpty(dir))
					return;
				foreach (var file in files) {
					var filename = DebugOutputUtils.GetFilename(file.Module.Name);
					if (filename.IndexOf('.') < 0)
						filename += file.IsExe ? ".exe" : ".dll";
					bool saved = Save(file.Module, Path.Combine(dir, filename), buffer);
					if (!saved)
						return;
				}
			}
		}

		static bool Save(DnModule module, string filename, byte[] buffer) {
			bool createdFile = false;
			try {
				using (var file = File.Create(filename)) {
					createdFile = true;
					ulong addr = module.CorModule.Address;
					ulong sizeLeft = module.CorModule.Size;
					while (sizeLeft > 0) {
						int bytesToRead = sizeLeft <= (ulong)buffer.Length ? (int)sizeLeft : buffer.Length;
						int bytesRead;
						int hr = module.Process.CorProcess.ReadMemory(addr, buffer, 0, bytesToRead, out bytesRead);
						if (hr < 0) {
							MainWindow.Instance.ShowMessageBox(string.Format("Failed to save '{0}'\nERROR: {1:X8}", filename, hr));
							return false;
						}
						if (bytesRead == 0) {
							MainWindow.Instance.ShowMessageBox(string.Format("Failed to save '{0}'\nERROR: Could not read any data", filename));
							return false;
						}

						file.Write(buffer, 0, bytesRead);
						addr += (ulong)bytesRead;
						sizeLeft -= (ulong)bytesRead;
					}

					return true;
				}
			}
			catch (Exception ex) {
				MainWindow.Instance.ShowMessageBox(string.Format("Failed to save '{0}'\nERROR: {1}", filename, ex.Message));
				if (createdFile && File.Exists(filename)) {
					try {
						File.Delete(filename);
					} catch { }
				}
				return false;
			}
		}

		protected override bool IsEnabled(ModulesCtxMenuContext context) {
			return GetSavableFiles(context.SelectedItems).Length > 0;
		}

		protected override void Initialize(ModulesCtxMenuContext context, MenuItem menuItem) {
			var files = GetSavableFiles(context.SelectedItems);
			menuItem.Header = files.Length > 1 ? string.Format("Save {0} Modules...", files.Length) : "Save Module...";
		}

		static ModuleVM[] GetSavableFiles(ModuleVM[] files) {
			//TODO: Support dynamic modules
			return files.Where(a => a.Module.CorModule.Address != 0 && a.Module.CorModule.Size > 0 && !a.Module.CorModule.IsDynamic && a.Module.CorModule.IsInMemory).ToArray();
		}

		static string GetDefaultExtension(string name, bool isExe) {
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
