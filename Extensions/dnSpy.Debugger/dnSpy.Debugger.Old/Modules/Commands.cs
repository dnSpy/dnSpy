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
using dnSpy.Contracts.Documents;
using dnSpy.Contracts.Documents.Tabs;
using dnSpy.Contracts.Extension;
using dnSpy.Contracts.Hex;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Menus;
using dnSpy.Contracts.MVVM;
using dnSpy.Contracts.MVVM.Dialogs;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.ToolWindows.App;
using dnSpy.Debugger.IMModules;
using dnSpy.Debugger.Memory;
using dnSpy.Debugger.Old.Properties;

namespace dnSpy.Debugger.Modules {
	//[ExportAutoLoaded]
	sealed class ModulesContentCommandLoader : IAutoLoaded {
		[ImportingConstructor]
		ModulesContentCommandLoader(IWpfCommandService wpfCommandService, Lazy<ITheDebugger> theDebugger, Lazy<IModulesContent> modulesContent, Lazy<IMemoryWindowService> memoryWindowService, CopyCallModulesCtxMenuCommand copyCmd, GoToModuleModulesCtxMenuCommand goToCmd, GoToModuleNewTabModulesCtxMenuCommand goToNewTabCmd, ShowInMemoryModulesCtxMenuCommand showInMemoryCmd) {
			var cmds = wpfCommandService.GetCommands(ControlConstants.GUID_DEBUGGER_MODULES_LISTVIEW);

			cmds.Add(ApplicationCommands.Copy, new ModulesCtxMenuCommandProxy(copyCmd));
			cmds.Add(new ModulesCtxMenuCommandProxy(goToCmd), ModifierKeys.None, Key.Enter);
			cmds.Add(new ModulesCtxMenuCommandProxy(goToNewTabCmd), ModifierKeys.Control, Key.Enter);
			cmds.Add(new ModulesCtxMenuCommandProxy(goToNewTabCmd), ModifierKeys.Shift, Key.Enter);
			cmds.Add(new ModulesCtxMenuCommandProxy(showInMemoryCmd), ModifierKeys.Control, Key.X);
			for (int i = 0; i < MemoryWindowsHelper.NUMBER_OF_MEMORY_WINDOWS && i < 10; i++)
				cmds.Add(new ModulesCtxMenuCommandProxy(new ShowInMemoryWindowModulesCtxMenuCommand(theDebugger, modulesContent, i, memoryWindowService)), ModifierKeys.Control, Key.D0 + (i + 1) % 10);
		}
	}

	//[ExportAutoLoaded]
	sealed class ModulesCommandLoader : IAutoLoaded {
		[ImportingConstructor]
		ModulesCommandLoader(IWpfCommandService wpfCommandService, IDsToolWindowService toolWindowService) {
			var cmds = wpfCommandService.GetCommands(ControlConstants.GUID_MAINWINDOW);

			cmds.Add(DebugRoutedCommands.ShowModules, new RelayCommand(a => toolWindowService.Show(ModulesToolWindowContent.THE_GUID)));
			cmds.Add(DebugRoutedCommands.ShowModules, ModifierKeys.Control | ModifierKeys.Alt, Key.U);
		}
	}

	sealed class ModulesCtxMenuContext {
		public IModulesVM VM { get; }
		public ModuleVM[] SelectedItems { get; }

		public ModulesCtxMenuContext(IModulesVM vm, ModuleVM[] selItems) {
			VM = vm;
			SelectedItems = selItems;
		}
	}

	sealed class ModulesCtxMenuCommandProxy : MenuItemCommandProxy<ModulesCtxMenuContext> {
		readonly ModulesCtxMenuCommand cmd;

		public ModulesCtxMenuCommandProxy(ModulesCtxMenuCommand cmd)
			: base(cmd) => this.cmd = cmd;

		protected override ModulesCtxMenuContext CreateContext() => cmd.Create();
	}

	abstract class ModulesCtxMenuCommand : MenuItemBase<ModulesCtxMenuContext> {
		protected sealed override object CachedContextKey => ContextKey;
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

	//[Export, ExportMenuItem(Header = "res:CopyCommand", Icon = DsImagesAttribute.Copy, InputGestureText = "res:ShortCutKeyCtrlC", Group = MenuConstants.GROUP_CTX_DBG_MODULES_COPY, Order = 0)]
	sealed class CopyCallModulesCtxMenuCommand : ModulesCtxMenuCommand {
		IDebuggerSettings debuggerSettings;

		[ImportingConstructor]
		CopyCallModulesCtxMenuCommand(Lazy<ITheDebugger> theDebugger, Lazy<IModulesContent> modulesContent, IDebuggerSettings debuggerSettings)
			: base(theDebugger, modulesContent) => this.debuggerSettings = debuggerSettings;

		public override void Execute(ModulesCtxMenuContext context) {
			var output = new StringBuilderTextColorOutput();
			foreach (var vm in context.SelectedItems) {
				var printer = new ModulePrinter(output, debuggerSettings.UseHexadecimal, theDebugger.Value.Debugger);
				printer.WriteName(vm);
				output.Write(BoxedTextColor.Text, "\t");
				printer.WriteOptimized(vm);
				output.Write(BoxedTextColor.Text, "\t");
				printer.WriteDynamic(vm);
				output.Write(BoxedTextColor.Text, "\t");
				printer.WriteInMemory(vm);
				output.Write(BoxedTextColor.Text, "\t");
				printer.WriteOrder(vm);
				output.Write(BoxedTextColor.Text, "\t");
				printer.WriteVersion(vm);
				output.Write(BoxedTextColor.Text, "\t");
				printer.WriteTimestamp(vm);
				output.Write(BoxedTextColor.Text, "\t");
				printer.WriteAddress(vm);
				output.Write(BoxedTextColor.Text, "\t");
				printer.WriteProcess(vm);
				output.Write(BoxedTextColor.Text, "\t");
				printer.WriteAppDomain(vm);
				output.Write(BoxedTextColor.Text, "\t");
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

		public override bool IsEnabled(ModulesCtxMenuContext context) => context.SelectedItems.Length > 0;
	}

	//[ExportMenuItem(Header = "res:SelectAllCommand", Icon = DsImagesAttribute.Select, InputGestureText = "res:ShortCutKeyCtrlA", Group = MenuConstants.GROUP_CTX_DBG_MODULES_COPY, Order = 10)]
	sealed class SelectAllModulesCtxMenuCommand : ModulesCtxMenuCommand {
		[ImportingConstructor]
		SelectAllModulesCtxMenuCommand(Lazy<ITheDebugger> theDebugger, Lazy<IModulesContent> modulesContent)
			: base(theDebugger, modulesContent) {
		}

		public override void Execute(ModulesCtxMenuContext context) => modulesContent.Value.ListView.SelectAll();
		public override bool IsEnabled(ModulesCtxMenuContext context) => context.SelectedItems.Length > 0;
	}

	//[Export, ExportMenuItem(Header = "res:GoToModuleCommand", Icon = DsImagesAttribute.ModulePublic, InputGestureText = "res:ShortCutKeyEnter", Group = MenuConstants.GROUP_CTX_DBG_MODULES_GOTO, Order = 0)]
	sealed class GoToModuleModulesCtxMenuCommand : ModulesCtxMenuCommand {
		readonly IDocumentTabService documentTabService;
		readonly Lazy<IModuleLoader> moduleLoader;
		readonly Lazy<IInMemoryModuleService> inMemoryModuleService;

		[ImportingConstructor]
		GoToModuleModulesCtxMenuCommand(Lazy<ITheDebugger> theDebugger, Lazy<IModulesContent> modulesContent, IDocumentTabService documentTabService, Lazy<IModuleLoader> moduleLoader, Lazy<IInMemoryModuleService> inMemoryModuleService)
			: base(theDebugger, modulesContent) {
			this.documentTabService = documentTabService;
			this.moduleLoader = moduleLoader;
			this.inMemoryModuleService = inMemoryModuleService;
		}

		public override void Execute(ModulesCtxMenuContext context) =>
			ExecuteInternal(documentTabService, inMemoryModuleService, moduleLoader, context, false);
		public override bool IsEnabled(ModulesCtxMenuContext context) => CanGoToModule(context);
		internal static bool CanGoToModule(ModulesCtxMenuContext context) => context != null && context.SelectedItems.Length != 0;

		internal static void ExecuteInternal(IDocumentTabService documentTabService, Lazy<IInMemoryModuleService> inMemoryModuleService, Lazy<IModuleLoader> moduleLoader, ModulesCtxMenuContext context, bool newTab) {
			if (context == null || context.SelectedItems.Length == 0)
				return;
			ExecuteInternal(documentTabService, inMemoryModuleService, moduleLoader, context.SelectedItems[0], newTab);
		}

		internal static void ExecuteInternal(IDocumentTabService documentTabService, Lazy<IInMemoryModuleService> inMemoryModuleService, Lazy<IModuleLoader> moduleLoader, ModuleVM vm, bool newTab) {
			if (vm == null)
				return;
			if (ShowErrorIfDynamic(inMemoryModuleService, vm.Module))
				GoToFile(documentTabService, moduleLoader.Value.LoadModule(vm.Module, canLoadDynFile: true, isAutoLoaded: false), newTab);
		}

		internal static bool ShowErrorIfDynamic(Lazy<IInMemoryModuleService> inMemoryModuleService, DnModule module, bool canShowDlgBox = true) {
			if (module.IsDynamic && module.Debugger.ProcessState != DebuggerProcessState.Paused) {
				if (inMemoryModuleService.Value.LoadDocument(module, false) == null) {
					if (canShowDlgBox)
						MsgBox.Instance.Show(dnSpy_Debugger_Resources.Module_BreakProcessBeforeLoadingDynamicModules);
					return false;
				}
			}
			return true;
		}

		internal static void GoToFile(IDocumentTabService documentTabService, IDsDocument document, bool newTab) {
			if (document == null)
				return;
			var obj = (object)document.ModuleDef ?? document;
			// The file could've been added lazily to the list so add a short delay before we select it
			Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => documentTabService.FollowReference(obj, newTab)));
		}
	}

	//[Export]
	sealed class GoToModuleNewTabModulesCtxMenuCommand : ModulesCtxMenuCommand {
		readonly IDocumentTabService documentTabService;
		readonly Lazy<IModuleLoader> moduleLoader;
		readonly Lazy<IInMemoryModuleService> inMemoryModuleService;

		[ImportingConstructor]
		GoToModuleNewTabModulesCtxMenuCommand(Lazy<ITheDebugger> theDebugger, Lazy<IModulesContent> modulesContent, IDocumentTabService documentTabService, Lazy<IModuleLoader> moduleLoader, Lazy<IInMemoryModuleService> inMemoryModuleService)
			: base(theDebugger, modulesContent) {
			this.documentTabService = documentTabService;
			this.moduleLoader = moduleLoader;
			this.inMemoryModuleService = inMemoryModuleService;
		}

		public override void Execute(ModulesCtxMenuContext context) =>
			GoToModuleModulesCtxMenuCommand.ExecuteInternal(documentTabService, inMemoryModuleService, moduleLoader, context, true);
		public override bool IsEnabled(ModulesCtxMenuContext context) => GoToModuleModulesCtxMenuCommand.CanGoToModule(context);
	}

	//[ExportMenuItem(Icon = DsImagesAttribute.ModulePublic, Group = MenuConstants.GROUP_CTX_DBG_MODULES_GOTO, Order = 10)]
	sealed class LoadModulesCtxMenuCommand : ModulesCtxMenuCommand {
		readonly Lazy<IModuleLoader> moduleLoader;
		readonly Lazy<IInMemoryModuleService> inMemoryModuleService;

		[ImportingConstructor]
		LoadModulesCtxMenuCommand(Lazy<ITheDebugger> theDebugger, Lazy<IModulesContent> modulesContent, Lazy<IModuleLoader> moduleLoader, Lazy<IInMemoryModuleService> inMemoryModuleService)
			: base(theDebugger, modulesContent) {
			this.moduleLoader = moduleLoader;
			this.inMemoryModuleService = inMemoryModuleService;
		}

		public override bool IsEnabled(ModulesCtxMenuContext context) => context.SelectedItems.Length > 1;

		public override string GetHeader(ModulesCtxMenuContext context) {
			if (context.SelectedItems.Length <= 1)
				return dnSpy_Debugger_Resources.LoadModulesCommand;
			return string.Format(dnSpy_Debugger_Resources.LoadXModulesCommand, context.SelectedItems.Length);
		}

		public override void Execute(ModulesCtxMenuContext context) {
			bool canShowDlgBox = true;
			foreach (var vm in context.SelectedItems) {
				var mod = vm.Module;
				bool res = GoToModuleModulesCtxMenuCommand.ShowErrorIfDynamic(inMemoryModuleService, mod, canShowDlgBox);
				if (!res)
					canShowDlgBox = false;
				if (res)
					moduleLoader.Value.LoadModule(vm.Module, canLoadDynFile: true, isAutoLoaded: false);
			}
		}
	}

	//[ExportMenuItem(Header = "res:OpenModuleFromMemoryCommand", Icon = DsImagesAttribute.ModulePublic, Group = MenuConstants.GROUP_CTX_DBG_MODULES_GOTO, Order = 20)]
	sealed class OpenModuleFromMemoryModulesCtxMenuCommand : ModulesCtxMenuCommand {
		readonly Lazy<IInMemoryModuleService> inMemoryModuleService;
		readonly IDocumentTabService documentTabService;

		[ImportingConstructor]
		OpenModuleFromMemoryModulesCtxMenuCommand(Lazy<ITheDebugger> theDebugger, Lazy<IModulesContent> modulesContent, Lazy<IInMemoryModuleService> inMemoryModuleService, IDocumentTabService documentTabService)
			: base(theDebugger, modulesContent) {
			this.inMemoryModuleService = inMemoryModuleService;
			this.documentTabService = documentTabService;
		}

		public override void Execute(ModulesCtxMenuContext context) =>
			ExecuteInternal(documentTabService, inMemoryModuleService, context, false);
		public override bool IsVisible(ModulesCtxMenuContext context) => IsEnabled(context);
		public override bool IsEnabled(ModulesCtxMenuContext context) => CanGoToModule(context);

		static bool CanGoToModule(ModulesCtxMenuContext context) {
			if (context == null || context.SelectedItems.Length == 0)
				return false;
			var vm = context.SelectedItems[0];
			return !vm.Module.IsDynamic && !vm.Module.IsInMemory;
		}

		static void ExecuteInternal(IDocumentTabService documentTabService, Lazy<IInMemoryModuleService> inMemoryModuleService, ModulesCtxMenuContext context, bool newTab) {
			if (context == null || context.SelectedItems.Length == 0)
				return;
			ExecuteInternal(documentTabService, inMemoryModuleService, context.SelectedItems[0], newTab);
		}

		static void ExecuteInternal(IDocumentTabService documentTabService, Lazy<IInMemoryModuleService> inMemoryModuleService, ModuleVM vm, bool newTab) {
			if (vm == null)
				return;

			if (GoToModuleModulesCtxMenuCommand.ShowErrorIfDynamic(inMemoryModuleService, vm.Module))
				GoToModuleModulesCtxMenuCommand.GoToFile(documentTabService, inMemoryModuleService.Value.LoadDocument(vm.Module, true), newTab);
		}
	}

	static class Constants {
		public const string SHOW_IN_MEMORY_WINDOW_GUID = "517AC97D-2619-477E-961E-B5519BB7FCE3";
		public const string GROUP_SHOW_IN_MEMORY_WINDOW = "0,E1F6906B-64C8-4411-B8B7-07C331197BFE";
	}

	//[ExportMenuItem(Header = "res:ShowInMemoryWindowCommand", Icon = DsImagesAttribute.MemoryWindow, Guid = Constants.SHOW_IN_MEMORY_WINDOW_GUID, Group = MenuConstants.GROUP_CTX_DBG_MODULES_GOTO, Order = 30)]
	sealed class ShowInMemoryXModulesCtxMenuCommand : ModulesCtxMenuCommand {
		[ImportingConstructor]
		ShowInMemoryXModulesCtxMenuCommand(Lazy<ITheDebugger> theDebugger, Lazy<IModulesContent> modulesContent)
			: base(theDebugger, modulesContent) {
		}

		public override void Execute(ModulesCtxMenuContext context) { }
	}

	//[ExportMenuItem(OwnerGuid = Constants.SHOW_IN_MEMORY_WINDOW_GUID, Group = Constants.GROUP_SHOW_IN_MEMORY_WINDOW, Order = 0)]
	sealed class ShowInMemoryXModulesSubCtxMenuCommand : ModulesCtxMenuCommand, IMenuItemProvider {
		readonly (IMenuItem command, string header, string inputGestureText)[] subCmds;

		[ImportingConstructor]
		ShowInMemoryXModulesSubCtxMenuCommand(Lazy<ITheDebugger> theDebugger, Lazy<IModulesContent> modulesContent, Lazy<IMemoryWindowService> memoryWindowService)
			: base(theDebugger, modulesContent) {
			subCmds = new (IMenuItem, string, string)[MemoryWindowsHelper.NUMBER_OF_MEMORY_WINDOWS];
			for (int i = 0; i < subCmds.Length; i++) {
				var header = MemoryWindowsHelper.GetHeaderText(i);
				var inputGestureText = MemoryWindowsHelper.GetCtrlInputGestureText(i);
				subCmds[i] = (new ShowInMemoryWindowModulesCtxMenuCommand(theDebugger, modulesContent, i, memoryWindowService), header, inputGestureText);
			}
		}

		public override void Execute(ModulesCtxMenuContext context) { }

		public IEnumerable<CreatedMenuItem> Create(IMenuItemContext context) {
			var ctx = CreateContext(context);
			Debug.Assert(ctx != null);
			if (ctx == null)
				yield break;

			for (int i = 0; i < subCmds.Length; i++) {
				var info = subCmds[i];
				var attr = new ExportMenuItemAttribute { Header = info.header, Icon = DsImagesAttribute.MemoryWindow };
				if (!string.IsNullOrEmpty(info.inputGestureText))
					attr.InputGestureText = info.inputGestureText;
				yield return new CreatedMenuItem(attr, info.command);
			}
		}
	}

	//[Export]
	sealed class ShowInMemoryModulesCtxMenuCommand : ModulesCtxMenuCommand {
		readonly Lazy<IMemoryWindowService> memoryWindowService;

		[ImportingConstructor]
		ShowInMemoryModulesCtxMenuCommand(Lazy<ITheDebugger> theDebugger, Lazy<IModulesContent> modulesContent, Lazy<IMemoryWindowService> memoryWindowService)
			: base(theDebugger, modulesContent) => this.memoryWindowService = memoryWindowService;

		public override void Execute(ModulesCtxMenuContext context) {
			var vm = ShowInMemoryWindowModulesCtxMenuCommand.GetModule(context);
			if (vm != null) {
				var start = new HexPosition(vm.Module.Address);
				var end = start + vm.Module.Size;
				Debug.Assert(end <= HexPosition.MaxEndPosition);
				if (end <= HexPosition.MaxEndPosition)
					memoryWindowService.Value.Show(HexSpan.FromBounds(start, end));
			}
		}

		public override bool IsEnabled(ModulesCtxMenuContext context) => ShowInMemoryWindowModulesCtxMenuCommand.GetModule(context) != null;
	}

	sealed class ShowInMemoryWindowModulesCtxMenuCommand : ModulesCtxMenuCommand {
		readonly int windowIndex;
		readonly Lazy<IMemoryWindowService> memoryWindowService;

		public ShowInMemoryWindowModulesCtxMenuCommand(Lazy<ITheDebugger> theDebugger, Lazy<IModulesContent> modulesContent, int windowIndex, Lazy<IMemoryWindowService> memoryWindowService)
			: base(theDebugger, modulesContent) {
			this.windowIndex = windowIndex;
			this.memoryWindowService = memoryWindowService;
		}

		public override void Execute(ModulesCtxMenuContext context) {
			var vm = GetModule(context);
			if (vm != null) {
				var start = new HexPosition(vm.Module.Address);
				var end = start + vm.Module.Size;
				Debug.Assert(end <= HexPosition.MaxEndPosition);
				if (end <= HexPosition.MaxEndPosition)
					memoryWindowService.Value.Show(HexSpan.FromBounds(start, end), windowIndex);
			}
		}

		public override bool IsEnabled(ModulesCtxMenuContext context) => GetModule(context) != null;

		internal static ModuleVM GetModule(ModulesCtxMenuContext context) {
			if (context.SelectedItems.Length != 1)
				return null;
			var vm = context.SelectedItems[0];
			if (vm.Module.Address == 0 || vm.Module.Size == 0)
				return null;
			return vm;
		}
	}

	//[ExportMenuItem(Header = "res:HexDisplayCommand", Group = MenuConstants.GROUP_CTX_DBG_MODULES_HEXOPTS, Order = 0)]
	sealed class HexadecimalDisplayModulesCtxMenuCommand : ModulesCtxMenuCommand {
		readonly DebuggerSettingsImpl debuggerSettings;

		[ImportingConstructor]
		HexadecimalDisplayModulesCtxMenuCommand(Lazy<ITheDebugger> theDebugger, Lazy<IModulesContent> modulesContent, DebuggerSettingsImpl debuggerSettings)
			: base(theDebugger, modulesContent) => this.debuggerSettings = debuggerSettings;

		public override void Execute(ModulesCtxMenuContext context) => debuggerSettings.UseHexadecimal = !debuggerSettings.UseHexadecimal;
		public override bool IsChecked(ModulesCtxMenuContext context) => debuggerSettings.UseHexadecimal;
	}

	//[ExportMenuItem(Header = "res:OpenContainingFolderCommand", Group = MenuConstants.GROUP_CTX_DBG_MODULES_DIRS, Order = 0)]
	sealed class OpenContainingFolderModulesCtxMenuCommand : ModulesCtxMenuCommand {
		[ImportingConstructor]
		OpenContainingFolderModulesCtxMenuCommand(Lazy<ITheDebugger> theDebugger, Lazy<IModulesContent> modulesContent)
			: base(theDebugger, modulesContent) {
		}

		public override void Execute(ModulesCtxMenuContext context) {
			if (context.SelectedItems.Length > 0)
				OpenContainingFolder(context.SelectedItems[0].Module.Name);
		}

		public override bool IsEnabled(ModulesCtxMenuContext context) => context.SelectedItems.Length == 1 &&
				!context.SelectedItems[0].Module.IsDynamic &&
				!context.SelectedItems[0].Module.IsInMemory;

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

	//[ExportMenuItem(Header = "res:ModuleCopyFilenameCommand", Group = MenuConstants.GROUP_CTX_DBG_MODULES_DIRS, Order = 10)]
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

		public override bool IsEnabled(ModulesCtxMenuContext context) => context.SelectedItems.Length == 1;
	}

	//[ExportMenuItem(Icon = DsImagesAttribute.Save, Group = MenuConstants.GROUP_CTX_DBG_MODULES_SAVE, Order = 0)]
	sealed class SaveModuleToDiskModulesCtxMenuCommand : ModulesCtxMenuCommand {
		readonly IAppWindow appWindow;
		readonly IMessageBoxService messageBoxService;
		readonly SimpleProcessReader simpleProcessReader;

		[ImportingConstructor]
		SaveModuleToDiskModulesCtxMenuCommand(Lazy<ITheDebugger> theDebugger, Lazy<IModulesContent> modulesContent, IAppWindow appWindow, IMessageBoxService messageBoxService, SimpleProcessReader simpleProcessReader)
			: base(theDebugger, modulesContent) {
			this.appWindow = appWindow;
			this.messageBoxService = messageBoxService;
			this.simpleProcessReader = simpleProcessReader;
		}

		public override void Execute(ModulesCtxMenuContext context) => Save(GetSavableFiles(context.SelectedItems));

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
			var list = new (DnModule module, string filename)[files.Length];
			if (files.Length == 1) {
				var vm = files[0];
				var filename = new PickSaveFilename().GetFilename(GetModuleFilename(vm.Module), GetDefaultExtension(GetModuleFilename(vm.Module), vm.IsExe, vm.Module.CorModule.IsManifestModule), PickFilenameConstants.DotNetAssemblyOrModuleFilter);
				if (string.IsNullOrEmpty(filename))
					return;
				list[0] = (vm.Module, filename);
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
					list[i] = (file.Module, Path.Combine(dir, filename));
				}
			}

			var data = new ProgressVM(Dispatcher.CurrentDispatcher, new PEFilesSaver(simpleProcessReader, list));
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
			messageBoxService.Show(string.Format(dnSpy_Debugger_Resources.ErrorOccurredX, data.ErrorMessage));
		}

		public override bool IsEnabled(ModulesCtxMenuContext context) => GetSavableFiles(context.SelectedItems).Length > 0;

		public override string GetHeader(ModulesCtxMenuContext context) {
			var files = GetSavableFiles(context.SelectedItems);
			return files.Length > 1 ? string.Format(dnSpy_Debugger_Resources.SaveModulesCommand, files.Length) :
						dnSpy_Debugger_Resources.SaveModuleCommand;
		}

		static ModuleVM[] GetSavableFiles(ModuleVM[] files) => files.Where(a => a.Module.CorModule.Address != 0 && a.Module.CorModule.Size > 0 && !a.Module.CorModule.IsDynamic).ToArray();

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
