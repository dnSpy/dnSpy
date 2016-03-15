/*
    Copyright (C) 2014-2016 de4dot@gmail.com

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
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using dndbg.Engine;
using dnlib.DotNet;
using dnSpy.Contracts.App;
using dnSpy.Contracts.Controls;
using dnSpy.Contracts.Files;
using dnSpy.Contracts.Files.Tabs;
using dnSpy.Contracts.Menus;
using dnSpy.Contracts.Plugin;
using dnSpy.Contracts.ToolWindows.App;
using dnSpy.Debugger.IMModules;
using dnSpy.Debugger.Memory;
using dnSpy.Debugger.Properties;
using dnSpy.Decompiler.Shared;
using dnSpy.Shared.Highlighting;
using dnSpy.Shared.Menus;
using dnSpy.Shared.MVVM;
using dnSpy.Shared.MVVM.Dialogs;

namespace dnSpy.Debugger.Modules {
	[ExportAutoLoaded]
	sealed class ModulesContentCommandLoader : IAutoLoaded {
		[ImportingConstructor]
		ModulesContentCommandLoader(IWpfCommandManager wpfCommandManager, Lazy<ITheDebugger> theDebugger, Lazy<IModulesContent> modulesContent, Lazy<IMemoryWindowManager> memoryWindowManager, CopyCallModulesCtxMenuCommand copyCmd, GoToModuleModulesCtxMenuCommand goToCmd, GoToModuleNewTabModulesCtxMenuCommand goToNewTabCmd, ShowInMemoryModulesCtxMenuCommand showInMemoryCmd) {
			var cmds = wpfCommandManager.GetCommands(CommandConstants.GUID_DEBUGGER_MODULES_LISTVIEW);

			cmds.Add(ApplicationCommands.Copy, new ModulesCtxMenuCommandProxy(copyCmd));
			cmds.Add(new ModulesCtxMenuCommandProxy(goToCmd), ModifierKeys.None, Key.Enter);
			cmds.Add(new ModulesCtxMenuCommandProxy(goToNewTabCmd), ModifierKeys.Control, Key.Enter);
			cmds.Add(new ModulesCtxMenuCommandProxy(goToNewTabCmd), ModifierKeys.Shift, Key.Enter);
			cmds.Add(new ModulesCtxMenuCommandProxy(showInMemoryCmd), ModifierKeys.Control, Key.X);
			for (int i = 0; i < MemoryWindowsHelper.NUMBER_OF_MEMORY_WINDOWS && i < 10; i++)
				cmds.Add(new ModulesCtxMenuCommandProxy(new ShowInMemoryWindowModulesCtxMenuCommand(theDebugger, modulesContent, i, memoryWindowManager)), ModifierKeys.Control, Key.D0 + (i + 1) % 10);
		}
	}

	[ExportAutoLoaded]
	sealed class ModulesCommandLoader : IAutoLoaded {
		[ImportingConstructor]
		ModulesCommandLoader(IWpfCommandManager wpfCommandManager, IMainToolWindowManager mainToolWindowManager) {
			var cmds = wpfCommandManager.GetCommands(CommandConstants.GUID_MAINWINDOW);

			cmds.Add(DebugRoutedCommands.ShowModules, new RelayCommand(a => mainToolWindowManager.Show(ModulesToolWindowContent.THE_GUID)));
			cmds.Add(DebugRoutedCommands.ShowModules, ModifierKeys.Control | ModifierKeys.Alt, Key.U);
		}
	}

	sealed class ModulesCtxMenuContext {
		public readonly IModulesVM VM;
		public readonly ModuleVM[] SelectedItems;

		public ModulesCtxMenuContext(IModulesVM vm, ModuleVM[] selItems) {
			this.VM = vm;
			this.SelectedItems = selItems;
		}
	}

	sealed class ModulesCtxMenuCommandProxy : MenuItemCommandProxy<ModulesCtxMenuContext> {
		readonly ModulesCtxMenuCommand cmd;

		public ModulesCtxMenuCommandProxy(ModulesCtxMenuCommand cmd)
			: base(cmd) {
			this.cmd = cmd;
		}

		protected override ModulesCtxMenuContext CreateContext() {
			return cmd.Create();
		}
	}

	abstract class ModulesCtxMenuCommand : MenuItemBase<ModulesCtxMenuContext> {
		protected sealed override object CachedContextKey {
			get { return ContextKey; }
		}
		static readonly object ContextKey = new object();

		protected readonly Lazy<ITheDebugger> theDebugger;
		protected readonly Lazy<IModulesContent> modulesContent;

		protected ModulesCtxMenuCommand(Lazy<ITheDebugger> theDebugger, Lazy<IModulesContent> modulesContent) {
			this.theDebugger = theDebugger;
			this.modulesContent = modulesContent;
		}

		protected sealed override ModulesCtxMenuContext CreateContext(IMenuItemContext context) {
			if (!(context.CreatorObject.Object is ListView))
				return null;
			if (theDebugger.Value.ProcessState == DebuggerProcessState.Terminated)
				return null;
			if (context.CreatorObject.Object != modulesContent.Value.ListView)
				return null;
			return Create();
		}

		internal ModulesCtxMenuContext Create() {
			var vm = modulesContent.Value.ModulesVM;
			var elems = modulesContent.Value.ListView.SelectedItems.OfType<ModuleVM>().ToArray();
			Array.Sort(elems, (a, b) => a.Module.UniqueId.CompareTo(b.Module.UniqueId));

			return new ModulesCtxMenuContext(vm, elems);
		}
	}

	[Export, ExportMenuItem(Header = "res:CopyCommand", Icon = "Copy", InputGestureText = "res:ShortCutKeyCtrlC", Group = MenuConstants.GROUP_CTX_DBG_MODULES_COPY, Order = 0)]
	sealed class CopyCallModulesCtxMenuCommand : ModulesCtxMenuCommand {
		IDebuggerSettings debuggerSettings;

		[ImportingConstructor]
		CopyCallModulesCtxMenuCommand(Lazy<ITheDebugger> theDebugger, Lazy<IModulesContent> modulesContent, IDebuggerSettings debuggerSettings)
			: base(theDebugger, modulesContent) {
			this.debuggerSettings = debuggerSettings;
		}

		public override void Execute(ModulesCtxMenuContext context) {
			var output = new NoSyntaxHighlightOutput();
			foreach (var vm in context.SelectedItems) {
				var printer = new ModulePrinter(output, debuggerSettings.UseHexadecimal, theDebugger.Value.Debugger);
				printer.WriteName(vm);
				output.Write("\t", TextTokenKind.Text);
				printer.WriteOptimized(vm);
				output.Write("\t", TextTokenKind.Text);
				printer.WriteDynamic(vm);
				output.Write("\t", TextTokenKind.Text);
				printer.WriteInMemory(vm);
				output.Write("\t", TextTokenKind.Text);
				printer.WriteOrder(vm);
				output.Write("\t", TextTokenKind.Text);
				printer.WriteVersion(vm);
				output.Write("\t", TextTokenKind.Text);
				printer.WriteTimestamp(vm);
				output.Write("\t", TextTokenKind.Text);
				printer.WriteAddress(vm);
				output.Write("\t", TextTokenKind.Text);
				printer.WriteProcess(vm);
				output.Write("\t", TextTokenKind.Text);
				printer.WriteAppDomain(vm);
				output.Write("\t", TextTokenKind.Text);
				printer.WritePath(vm);
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

		public override bool IsEnabled(ModulesCtxMenuContext context) {
			return context.SelectedItems.Length > 0;
		}
	}

	[ExportMenuItem(Header = "res:SelectAllCommand", Icon = "Select", InputGestureText = "res:ShortCutKeyCtrlA", Group = MenuConstants.GROUP_CTX_DBG_MODULES_COPY, Order = 10)]
	sealed class SelectAllModulesCtxMenuCommand : ModulesCtxMenuCommand {
		[ImportingConstructor]
		SelectAllModulesCtxMenuCommand(Lazy<ITheDebugger> theDebugger, Lazy<IModulesContent> modulesContent)
			: base(theDebugger, modulesContent) {
		}

		public override void Execute(ModulesCtxMenuContext context) {
			modulesContent.Value.ListView.SelectAll();
		}

		public override bool IsEnabled(ModulesCtxMenuContext context) {
			return context.SelectedItems.Length > 0;
		}
	}

	[Export, ExportMenuItem(Header = "res:GoToModuleCommand", Icon = "AssemblyModule", InputGestureText = "res:ShortCutKeyEnter", Group = MenuConstants.GROUP_CTX_DBG_MODULES_GOTO, Order = 0)]
	sealed class GoToModuleModulesCtxMenuCommand : ModulesCtxMenuCommand {
		readonly IFileTabManager fileTabManager;
		readonly Lazy<ModuleLoader> moduleLoader;
		readonly Lazy<IInMemoryModuleManager> inMemoryModuleManager;

		[ImportingConstructor]
		GoToModuleModulesCtxMenuCommand(Lazy<ITheDebugger> theDebugger, Lazy<IModulesContent> modulesContent, IFileTabManager fileTabManager, Lazy<ModuleLoader> moduleLoader, Lazy<IInMemoryModuleManager> inMemoryModuleManager)
			: base(theDebugger, modulesContent) {
			this.fileTabManager = fileTabManager;
			this.moduleLoader = moduleLoader;
			this.inMemoryModuleManager = inMemoryModuleManager;
		}

		public override void Execute(ModulesCtxMenuContext context) {
			ExecuteInternal(fileTabManager, inMemoryModuleManager, moduleLoader, context, false);
		}

		public override bool IsEnabled(ModulesCtxMenuContext context) {
			return CanGoToModule(context);
		}

		internal static bool CanGoToModule(ModulesCtxMenuContext context) {
			return context != null && context.SelectedItems.Length != 0;
		}

		internal static void ExecuteInternal(IFileTabManager fileTabManager, Lazy<IInMemoryModuleManager> inMemoryModuleManager, Lazy<ModuleLoader> moduleLoader, ModulesCtxMenuContext context, bool newTab) {
			if (context == null || context.SelectedItems.Length == 0)
				return;
			ExecuteInternal(fileTabManager, inMemoryModuleManager, moduleLoader, context.SelectedItems[0], newTab);
		}

		internal static void ExecuteInternal(IFileTabManager fileTabManager, Lazy<IInMemoryModuleManager> inMemoryModuleManager, Lazy<ModuleLoader> moduleLoader, ModuleVM vm, bool newTab) {
			if (vm == null)
				return;
			if (ShowErrorIfDynamic(inMemoryModuleManager, vm.Module))
				GoToFile(fileTabManager, moduleLoader.Value.LoadModule(vm.Module, true), newTab);
		}

		internal static bool ShowErrorIfDynamic(Lazy<IInMemoryModuleManager> inMemoryModuleManager, DnModule module, bool canShowDlgBox = true) {
			if (module.IsDynamic && module.Debugger.ProcessState != DebuggerProcessState.Paused) {
				if (inMemoryModuleManager.Value.LoadFile(module, false) == null) {
					if (canShowDlgBox)
						Shared.App.MsgBox.Instance.Show(dnSpy_Debugger_Resources.Module_BreakProcessBeforeLoadingDynamicModules);
					return false;
				}
			}
			return true;
		}

		internal static void GoToFile(IFileTabManager fileTabManager, IDnSpyFile file, bool newTab) {
			if (file == null)
				return;
			var obj = (object)file.ModuleDef ?? file;
			// The file could've been added lazily to the list so add a short delay before we select it
			Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => fileTabManager.FollowReference(obj, newTab)));
		}
	}

	[Export]
	sealed class GoToModuleNewTabModulesCtxMenuCommand : ModulesCtxMenuCommand {
		readonly IFileTabManager fileTabManager;
		readonly Lazy<ModuleLoader> moduleLoader;
		readonly Lazy<IInMemoryModuleManager> inMemoryModuleManager;

		[ImportingConstructor]
		GoToModuleNewTabModulesCtxMenuCommand(Lazy<ITheDebugger> theDebugger, Lazy<IModulesContent> modulesContent, IFileTabManager fileTabManager, Lazy<ModuleLoader> moduleLoader, Lazy<IInMemoryModuleManager> inMemoryModuleManager)
			: base(theDebugger, modulesContent) {
			this.fileTabManager = fileTabManager;
			this.moduleLoader = moduleLoader;
			this.inMemoryModuleManager = inMemoryModuleManager;
		}

		public override void Execute(ModulesCtxMenuContext context) {
			GoToModuleModulesCtxMenuCommand.ExecuteInternal(fileTabManager, inMemoryModuleManager, moduleLoader, context, true);
		}

		public override bool IsEnabled(ModulesCtxMenuContext context) {
			return GoToModuleModulesCtxMenuCommand.CanGoToModule(context);
		}
	}

	[ExportMenuItem(Icon = "AssemblyModule", Group = MenuConstants.GROUP_CTX_DBG_MODULES_GOTO, Order = 10)]
	sealed class LoadModulesCtxMenuCommand : ModulesCtxMenuCommand {
		readonly Lazy<ModuleLoader> moduleLoader;
		readonly Lazy<IInMemoryModuleManager> inMemoryModuleManager;

		[ImportingConstructor]
		LoadModulesCtxMenuCommand(Lazy<ITheDebugger> theDebugger, Lazy<IModulesContent> modulesContent, Lazy<ModuleLoader> moduleLoader, Lazy<IInMemoryModuleManager> inMemoryModuleManager)
			: base(theDebugger, modulesContent) {
			this.moduleLoader = moduleLoader;
			this.inMemoryModuleManager = inMemoryModuleManager;
		}

		public override bool IsEnabled(ModulesCtxMenuContext context) {
			return context.SelectedItems.Length > 1;
		}

		public override string GetHeader(ModulesCtxMenuContext context) {
			if (context.SelectedItems.Length <= 1)
				return dnSpy_Debugger_Resources.LoadModulesCommand;
			return string.Format(dnSpy_Debugger_Resources.LoadXModulesCommand, context.SelectedItems.Length);
		}

		public override void Execute(ModulesCtxMenuContext context) {
			bool canShowDlgBox = true;
			foreach (var vm in context.SelectedItems) {
				var mod = vm.Module;
				bool res = GoToModuleModulesCtxMenuCommand.ShowErrorIfDynamic(inMemoryModuleManager, mod, canShowDlgBox);
				if (!res)
					canShowDlgBox = false;
				if (res)
					moduleLoader.Value.LoadModule(vm.Module, true);
			}
		}
	}

	[ExportMenuItem(Header = "res:OpenModuleFromMemoryCommand", Icon = "AssemblyModule", Group = MenuConstants.GROUP_CTX_DBG_MODULES_GOTO, Order = 20)]
	sealed class OpenModuleFromMemoryModulesCtxMenuCommand : ModulesCtxMenuCommand {
		readonly Lazy<IInMemoryModuleManager> inMemoryModuleManager;
		readonly IFileTabManager fileTabManager;

		[ImportingConstructor]
		OpenModuleFromMemoryModulesCtxMenuCommand(Lazy<ITheDebugger> theDebugger, Lazy<IModulesContent> modulesContent, Lazy<IInMemoryModuleManager> inMemoryModuleManager, IFileTabManager fileTabManager)
			: base(theDebugger, modulesContent) {
			this.inMemoryModuleManager = inMemoryModuleManager;
			this.fileTabManager = fileTabManager;
		}

		public override void Execute(ModulesCtxMenuContext context) {
			ExecuteInternal(fileTabManager, inMemoryModuleManager, context, false);
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

		static void ExecuteInternal(IFileTabManager fileTabManager, Lazy<IInMemoryModuleManager> inMemoryModuleManager, ModulesCtxMenuContext context, bool newTab) {
			if (context == null || context.SelectedItems.Length == 0)
				return;
			ExecuteInternal(fileTabManager, inMemoryModuleManager, context.SelectedItems[0], newTab);
		}

		static void ExecuteInternal(IFileTabManager fileTabManager, Lazy<IInMemoryModuleManager> inMemoryModuleManager, ModuleVM vm, bool newTab) {
			if (vm == null)
				return;

			if (GoToModuleModulesCtxMenuCommand.ShowErrorIfDynamic(inMemoryModuleManager, vm.Module))
				GoToModuleModulesCtxMenuCommand.GoToFile(fileTabManager, inMemoryModuleManager.Value.LoadFile(vm.Module, true), newTab);
		}
	}

	static class Constants {
		public const string SHOW_IN_MEMORY_WINDOW_GUID = "517AC97D-2619-477E-961E-B5519BB7FCE3";
		public const string GROUP_SHOW_IN_MEMORY_WINDOW = "0,E1F6906B-64C8-4411-B8B7-07C331197BFE";
	}

	[ExportMenuItem(Header = "res:ShowInMemoryWindowCommand", Icon = "MemoryWindow", Guid = Constants.SHOW_IN_MEMORY_WINDOW_GUID, Group = MenuConstants.GROUP_CTX_DBG_MODULES_GOTO, Order = 30)]
	sealed class ShowInMemoryXModulesCtxMenuCommand : ModulesCtxMenuCommand {
		[ImportingConstructor]
		ShowInMemoryXModulesCtxMenuCommand(Lazy<ITheDebugger> theDebugger, Lazy<IModulesContent> modulesContent)
			: base(theDebugger, modulesContent) {
		}

		public override void Execute(ModulesCtxMenuContext context) {
		}
	}

	[ExportMenuItem(OwnerGuid = Constants.SHOW_IN_MEMORY_WINDOW_GUID, Group = Constants.GROUP_SHOW_IN_MEMORY_WINDOW, Order = 0)]
	sealed class ShowInMemoryXModulesSubCtxMenuCommand : ModulesCtxMenuCommand, IMenuItemCreator {
		readonly Tuple<IMenuItem, string, string>[] subCmds;

		[ImportingConstructor]
		ShowInMemoryXModulesSubCtxMenuCommand(Lazy<ITheDebugger> theDebugger, Lazy<IModulesContent> modulesContent, Lazy<IMemoryWindowManager> memoryWindowManager)
			: base(theDebugger, modulesContent) {
			subCmds = new Tuple<IMenuItem, string, string>[MemoryWindowsHelper.NUMBER_OF_MEMORY_WINDOWS];
			for (int i = 0; i < subCmds.Length; i++) {
				var header = MemoryWindowsHelper.GetHeaderText(i);
				var inputGestureText = MemoryWindowsHelper.GetCtrlInputGestureText(i);
				subCmds[i] = Tuple.Create((IMenuItem)new ShowInMemoryWindowModulesCtxMenuCommand(theDebugger, modulesContent, i, memoryWindowManager), header, inputGestureText);
			}
		}

		public override void Execute(ModulesCtxMenuContext context) {
		}

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

	[Export]
	sealed class ShowInMemoryModulesCtxMenuCommand : ModulesCtxMenuCommand {
		readonly Lazy<IMemoryWindowManager> memoryWindowManager;

		[ImportingConstructor]
		ShowInMemoryModulesCtxMenuCommand(Lazy<ITheDebugger> theDebugger, Lazy<IModulesContent> modulesContent, Lazy<IMemoryWindowManager> memoryWindowManager)
			: base(theDebugger, modulesContent) {
			this.memoryWindowManager = memoryWindowManager;
		}

		public override void Execute(ModulesCtxMenuContext context) {
			var vm = ShowInMemoryWindowModulesCtxMenuCommand.GetModule(context);
			if (vm != null)
				memoryWindowManager.Value.Show(vm.Module.Address, vm.Module.Size);
		}

		public override bool IsEnabled(ModulesCtxMenuContext context) {
			return ShowInMemoryWindowModulesCtxMenuCommand.GetModule(context) != null;
		}
	}

	sealed class ShowInMemoryWindowModulesCtxMenuCommand : ModulesCtxMenuCommand {
		readonly int windowIndex;
		readonly Lazy<IMemoryWindowManager> memoryWindowManager;

		public ShowInMemoryWindowModulesCtxMenuCommand(Lazy<ITheDebugger> theDebugger, Lazy<IModulesContent> modulesContent, int windowIndex, Lazy<IMemoryWindowManager> memoryWindowManager)
			: base(theDebugger, modulesContent) {
			this.windowIndex = windowIndex;
			this.memoryWindowManager = memoryWindowManager;
		}

		public override void Execute(ModulesCtxMenuContext context) {
			var vm = GetModule(context);
			if (vm != null)
				memoryWindowManager.Value.Show(vm.Module.Address, vm.Module.Size, windowIndex);
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

	[ExportMenuItem(Header = "res:HexDisplayCommand", Group = MenuConstants.GROUP_CTX_DBG_MODULES_HEXOPTS, Order = 0)]
	sealed class HexadecimalDisplayModulesCtxMenuCommand : ModulesCtxMenuCommand {
		readonly DebuggerSettingsImpl debuggerSettings;

		[ImportingConstructor]
		HexadecimalDisplayModulesCtxMenuCommand(Lazy<ITheDebugger> theDebugger, Lazy<IModulesContent> modulesContent, DebuggerSettingsImpl debuggerSettings)
			: base(theDebugger, modulesContent) {
			this.debuggerSettings = debuggerSettings;
		}

		public override void Execute(ModulesCtxMenuContext context) {
			debuggerSettings.UseHexadecimal = !debuggerSettings.UseHexadecimal;
		}

		public override bool IsChecked(ModulesCtxMenuContext context) {
			return debuggerSettings.UseHexadecimal;
		}
	}

	[ExportMenuItem(Header = "res:OpenContainingFolderCommand", Group = MenuConstants.GROUP_CTX_DBG_MODULES_DIRS, Order = 0)]
	sealed class OpenContainingFolderModulesCtxMenuCommand : ModulesCtxMenuCommand {
		[ImportingConstructor]
		OpenContainingFolderModulesCtxMenuCommand(Lazy<ITheDebugger> theDebugger, Lazy<IModulesContent> modulesContent)
			: base(theDebugger, modulesContent) {
		}

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

	[ExportMenuItem(Header = "res:ModuleCopyFilenameCommand", Group = MenuConstants.GROUP_CTX_DBG_MODULES_DIRS, Order = 10)]
	sealed class CopyFilenameModulesCtxMenuCommand : ModulesCtxMenuCommand {
		[ImportingConstructor]
		CopyFilenameModulesCtxMenuCommand(Lazy<ITheDebugger> theDebugger, Lazy<IModulesContent> modulesContent)
			: base(theDebugger, modulesContent) {
		}

		public override void Execute(ModulesCtxMenuContext context) {
			if (context.SelectedItems.Length > 0) {
				try {
					Clipboard.SetText(context.SelectedItems[0].Module.Name);
				}
				catch (ExternalException) { }
			}
		}

		public override bool IsEnabled(ModulesCtxMenuContext context) {
			return context.SelectedItems.Length == 1;
		}
	}

	[ExportMenuItem(Group = MenuConstants.GROUP_CTX_DBG_MODULES_SAVE, Order = 0)]
	sealed class SaveModuleToDiskModulesCtxMenuCommand : ModulesCtxMenuCommand {
		readonly IAppWindow appWindow;
		readonly IMessageBoxManager messageBoxManager;

		[ImportingConstructor]
		SaveModuleToDiskModulesCtxMenuCommand(Lazy<ITheDebugger> theDebugger, Lazy<IModulesContent> modulesContent, IAppWindow appWindow, IMessageBoxManager messageBoxManager)
			: base(theDebugger, modulesContent) {
			this.appWindow = appWindow;
			this.messageBoxManager = messageBoxManager;
		}

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

		void Save(ModuleVM[] files) {
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

			var data = new ProgressVM(Dispatcher.CurrentDispatcher, new PEFilesSaver(list));
			var win = new ProgressDlg();
			win.DataContext = data;
			win.Owner = appWindow.MainWindow;
			win.Title = list.Length == 1 ? dnSpy_Debugger_Resources.ModuleSaveModuleTitle :
						dnSpy_Debugger_Resources.ModuleSaveModulesTitle;
			var res = win.ShowDialog();
			if (res != true)
				return;
			if (!data.WasError)
				return;
			messageBoxManager.Show(string.Format(dnSpy_Debugger_Resources.ErrorOccurredX, data.ErrorMessage));
		}

		public override bool IsEnabled(ModulesCtxMenuContext context) {
			return GetSavableFiles(context.SelectedItems).Length > 0;
		}

		public override string GetHeader(ModulesCtxMenuContext context) {
			var files = GetSavableFiles(context.SelectedItems);
			return files.Length > 1 ? string.Format(dnSpy_Debugger_Resources.SaveModulesCommand, files.Length) :
						dnSpy_Debugger_Resources.SaveModuleCommand;
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
