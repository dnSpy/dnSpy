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
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Input;
using dnSpy.Contracts.Controls;
using dnSpy.Contracts.Extension;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Menus;
using dnSpy.Contracts.MVVM;
using dnSpy.Debugger.Properties;
using dnSpy.Debugger.ToolWindows.Memory;

namespace dnSpy.Debugger.ToolWindows.Modules {
	[ExportAutoLoaded]
	sealed class ModulesCommandsLoader : IAutoLoaded {
		[ImportingConstructor]
		ModulesCommandsLoader(IWpfCommandService wpfCommandService, Lazy<IModulesContent> modulesContent) {
			var cmds = wpfCommandService.GetCommands(ControlConstants.GUID_DEBUGGER_MODULES_LISTVIEW);
			cmds.Add(new RelayCommand(a => modulesContent.Value.Operations.Copy(), a => modulesContent.Value.Operations.CanCopy), ModifierKeys.Control, Key.C);
			cmds.Add(new RelayCommand(a => modulesContent.Value.Operations.Copy(), a => modulesContent.Value.Operations.CanCopy), ModifierKeys.Control, Key.Insert);
			cmds.Add(new RelayCommand(a => modulesContent.Value.Operations.GoToModule(newTab: false), a => modulesContent.Value.Operations.CanGoToModule), ModifierKeys.None, Key.Enter);
			cmds.Add(new RelayCommand(a => modulesContent.Value.Operations.GoToModule(newTab: true), a => modulesContent.Value.Operations.CanGoToModule), ModifierKeys.Control, Key.Enter);
			cmds.Add(new RelayCommand(a => modulesContent.Value.Operations.GoToModule(newTab: true), a => modulesContent.Value.Operations.CanGoToModule), ModifierKeys.Shift, Key.Enter);
			cmds.Add(new RelayCommand(a => modulesContent.Value.Operations.ShowInMemoryWindow(), a => modulesContent.Value.Operations.CanShowInMemoryWindow), ModifierKeys.Control, Key.X);
			for (int i = 0; i < MemoryWindowsHelper.NUMBER_OF_MEMORY_WINDOWS && i < 10; i++) {
				var windowIndex = i;
				cmds.Add(new RelayCommand(a => modulesContent.Value.Operations.ShowInMemoryWindow(windowIndex), a => modulesContent.Value.Operations.CanShowInMemoryWindow), ModifierKeys.Control, Key.D0 + (i + 1) % 10);
			}

			cmds = wpfCommandService.GetCommands(ControlConstants.GUID_DEBUGGER_MODULES_CONTROL);
			cmds.Add(new RelayCommand(a => modulesContent.Value.FocusSearchTextBox()), ModifierKeys.Control, Key.F);
			cmds.Add(new RelayCommand(a => modulesContent.Value.FocusSearchTextBox()), ModifierKeys.Control, Key.E);
		}
	}

	sealed class ModulesCtxMenuContext {
		public ModulesOperations Operations { get; }
		public ModulesCtxMenuContext(ModulesOperations operations) => Operations = operations;
	}

	abstract class ModulesCtxMenuCommand : MenuItemBase<ModulesCtxMenuContext> {
		protected sealed override object CachedContextKey => ContextKey;
		static readonly object ContextKey = new object();

		protected readonly Lazy<IModulesContent> modulesContent;

		protected ModulesCtxMenuCommand(Lazy<IModulesContent> modulesContent) => this.modulesContent = modulesContent;

		protected sealed override ModulesCtxMenuContext CreateContext(IMenuItemContext context) {
			if (!(context.CreatorObject.Object is ListView))
				return null;
			if (context.CreatorObject.Object != modulesContent.Value.ListView)
				return null;
			return Create();
		}

		ModulesCtxMenuContext Create() => new ModulesCtxMenuContext(modulesContent.Value.Operations);
	}

	[ExportMenuItem(Header = "res:CopyCommand", Icon = DsImagesAttribute.Copy, InputGestureText = "res:ShortCutKeyCtrlC", Group = MenuConstants.GROUP_CTX_DBG_MODULES_COPY, Order = 0)]
	sealed class CopyModulesCtxMenuCommand : ModulesCtxMenuCommand {
		[ImportingConstructor]
		CopyModulesCtxMenuCommand(Lazy<IModulesContent> modulesContent)
			: base(modulesContent) {
		}

		public override void Execute(ModulesCtxMenuContext context) => context.Operations.Copy();
		public override bool IsEnabled(ModulesCtxMenuContext context) => context.Operations.CanCopy;
	}

	[ExportMenuItem(Header = "res:SelectAllCommand", Icon = DsImagesAttribute.Select, InputGestureText = "res:ShortCutKeyCtrlA", Group = MenuConstants.GROUP_CTX_DBG_MODULES_COPY, Order = 10)]
	sealed class SelectAllModulesCtxMenuCommand : ModulesCtxMenuCommand {
		[ImportingConstructor]
		SelectAllModulesCtxMenuCommand(Lazy<IModulesContent> modulesContent)
			: base(modulesContent) {
		}

		public override void Execute(ModulesCtxMenuContext context) => context.Operations.SelectAll();
		public override bool IsEnabled(ModulesCtxMenuContext context) => context.Operations.CanSelectAll;
	}

	[Export, ExportMenuItem(Header = "res:GoToModuleCommand", Icon = DsImagesAttribute.ModulePublic, InputGestureText = "res:ShortCutKeyEnter", Group = MenuConstants.GROUP_CTX_DBG_MODULES_GOTO, Order = 0)]
	sealed class GoToModuleModulesCtxMenuCommand : ModulesCtxMenuCommand {
		[ImportingConstructor]
		GoToModuleModulesCtxMenuCommand(Lazy<IModulesContent> modulesContent)
			: base(modulesContent) {
		}

		public override void Execute(ModulesCtxMenuContext context) => context.Operations.GoToModule(newTab: false);
		public override bool IsEnabled(ModulesCtxMenuContext context) => context.Operations.CanGoToModule;
	}

	[ExportMenuItem(Icon = DsImagesAttribute.ModulePublic, Group = MenuConstants.GROUP_CTX_DBG_MODULES_GOTO, Order = 10)]
	sealed class LoadModulesModulesCtxMenuCommand : ModulesCtxMenuCommand {
		[ImportingConstructor]
		LoadModulesModulesCtxMenuCommand(Lazy<IModulesContent> modulesContent)
			: base(modulesContent) {
		}

		public override void Execute(ModulesCtxMenuContext context) => context.Operations.LoadModules();
		public override bool IsEnabled(ModulesCtxMenuContext context) => context.Operations.CanLoadModules;
		public override string GetHeader(ModulesCtxMenuContext context) {
			if (context.Operations.LoadModulesCount <= 1)
				return dnSpy_Debugger_Resources.LoadModulesCommand;
			return string.Format(dnSpy_Debugger_Resources.LoadXModulesCommand, context.Operations.LoadModulesCount);
		}
	}

	[ExportMenuItem(Header = "res:LoadAllModulesCommand", Icon = DsImagesAttribute.ModulePublic, Group = MenuConstants.GROUP_CTX_DBG_MODULES_GOTO, Order = 20)]
	sealed class LoadAllModulesModulesCtxMenuCommand : ModulesCtxMenuCommand {
		[ImportingConstructor]
		LoadAllModulesModulesCtxMenuCommand(Lazy<IModulesContent> modulesContent)
			: base(modulesContent) {
		}

		public override void Execute(ModulesCtxMenuContext context) => context.Operations.LoadAllModules();
		public override bool IsEnabled(ModulesCtxMenuContext context) => context.Operations.CanLoadAllModules;
		public override bool IsVisible(ModulesCtxMenuContext context) => IsEnabled(context);
	}

	[ExportMenuItem(Header = "res:OpenModuleFromMemoryCommand", Icon = DsImagesAttribute.ModulePublic, Group = MenuConstants.GROUP_CTX_DBG_MODULES_GOTO, Order = 30)]
	sealed class OpemModuleFromMemoryModulesCtxMenuCommand : ModulesCtxMenuCommand {
		[ImportingConstructor]
		OpemModuleFromMemoryModulesCtxMenuCommand(Lazy<IModulesContent> modulesContent)
			: base(modulesContent) {
		}

		public override void Execute(ModulesCtxMenuContext context) => context.Operations.OpenModuleFromMemory(newTab: false);
		public override bool IsEnabled(ModulesCtxMenuContext context) => context.Operations.CanOpenModuleFromMemory;
		public override bool IsVisible(ModulesCtxMenuContext context) => IsEnabled(context);
	}

	static class Constants {
		public const string SHOW_IN_MEMORY_WINDOW_GUID = "517AC97D-2619-477E-961E-B5519BB7FCE3";
		public const string GROUP_SHOW_IN_MEMORY_WINDOW = "0,E1F6906B-64C8-4411-B8B7-07C331197BFE";
	}

	[ExportMenuItem(Header = "res:ShowInMemoryWindowCommand", Icon = DsImagesAttribute.MemoryWindow, Guid = Constants.SHOW_IN_MEMORY_WINDOW_GUID, Group = MenuConstants.GROUP_CTX_DBG_MODULES_GOTO, Order = 30)]
	sealed class ShowInMemoryWindowXModulesCtxMenuCommand : ModulesCtxMenuCommand {
		[ImportingConstructor]
		ShowInMemoryWindowXModulesCtxMenuCommand(Lazy<IModulesContent> modulesContent)
			: base(modulesContent) {
		}

		public override void Execute(ModulesCtxMenuContext context) { }
		public override bool IsEnabled(ModulesCtxMenuContext context) => context.Operations.CanShowInMemoryWindow;
	}

	[ExportMenuItem(OwnerGuid = Constants.SHOW_IN_MEMORY_WINDOW_GUID, Group = Constants.GROUP_SHOW_IN_MEMORY_WINDOW, Order = 0)]
	sealed class ShowInMemoryWindowXSubModulesCtxMenuCommand : ModulesCtxMenuCommand, IMenuItemProvider {
		readonly (IMenuItem command, string header, string inputGestureText)[] subCmds;

		[ImportingConstructor]
		ShowInMemoryWindowXSubModulesCtxMenuCommand(Lazy<IModulesContent> modulesContent)
			: base(modulesContent) {
			subCmds = new(IMenuItem, string, string)[MemoryWindowsHelper.NUMBER_OF_MEMORY_WINDOWS];
			for (int i = 0; i < subCmds.Length; i++) {
				var header = MemoryWindowsHelper.GetHeaderText(i);
				var inputGestureText = MemoryWindowsHelper.GetCtrlInputGestureText(i);
				subCmds[i] = (new ShowInMemoryWindowModulesCtxMenuCommand(modulesContent, i), header, inputGestureText);
			}
		}

		public override void Execute(ModulesCtxMenuContext context) { }

		IEnumerable<CreatedMenuItem> IMenuItemProvider.Create(IMenuItemContext context) {
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

	sealed class ShowInMemoryWindowModulesCtxMenuCommand : ModulesCtxMenuCommand {
		readonly int windowIndex;
		public ShowInMemoryWindowModulesCtxMenuCommand(Lazy<IModulesContent> modulesContent, int windowIndex)
			: base(modulesContent) => this.windowIndex = windowIndex;

		public override void Execute(ModulesCtxMenuContext context) => context.Operations.ShowInMemoryWindow(windowIndex);
		public override bool IsEnabled(ModulesCtxMenuContext context) => context.Operations.CanShowInMemoryWindow;
	}

	[ExportMenuItem(Header = "res:HexDisplayCommand", Group = MenuConstants.GROUP_CTX_DBG_MODULES_HEXOPTS, Order = 0)]
	sealed class UseHexadecimalModulesCtxMenuCommand : ModulesCtxMenuCommand {
		[ImportingConstructor]
		UseHexadecimalModulesCtxMenuCommand(Lazy<IModulesContent> modulesContent)
			: base(modulesContent) {
		}

		public override void Execute(ModulesCtxMenuContext context) => context.Operations.ToggleUseHexadecimal();
		public override bool IsEnabled(ModulesCtxMenuContext context) => context.Operations.CanToggleUseHexadecimal;
		public override bool IsChecked(ModulesCtxMenuContext context) => context.Operations.UseHexadecimal;
	}

	[ExportMenuItem(Header = "res:OpenContainingFolderCommand", Group = MenuConstants.GROUP_CTX_DBG_MODULES_DIRS, Order = 0)]
	sealed class OpenContainingFolderModulesCtxMenuCommand : ModulesCtxMenuCommand {
		[ImportingConstructor]
		OpenContainingFolderModulesCtxMenuCommand(Lazy<IModulesContent> modulesContent)
			: base(modulesContent) {
		}

		public override void Execute(ModulesCtxMenuContext context) => context.Operations.OpenContainingFolder();
		public override bool IsEnabled(ModulesCtxMenuContext context) => context.Operations.CanOpenContainingFolder;
	}

	[ExportMenuItem(Header = "res:ModuleCopyFilenameCommand", Group = MenuConstants.GROUP_CTX_DBG_MODULES_DIRS, Order = 10)]
	sealed class CopyFilenameModulesCtxMenuCommand : ModulesCtxMenuCommand {
		[ImportingConstructor]
		CopyFilenameModulesCtxMenuCommand(Lazy<IModulesContent> modulesContent)
			: base(modulesContent) {
		}

		public override void Execute(ModulesCtxMenuContext context) => context.Operations.CopyFilename();
		public override bool IsEnabled(ModulesCtxMenuContext context) => context.Operations.CanCopyFilename;
	}

	[ExportMenuItem(Icon = DsImagesAttribute.Save, Group = MenuConstants.GROUP_CTX_DBG_MODULES_SAVE, Order = 0)]
	sealed class SaveModulesCtxMenuCommand : ModulesCtxMenuCommand {
		[ImportingConstructor]
		SaveModulesCtxMenuCommand(Lazy<IModulesContent> modulesContent)
			: base(modulesContent) {
		}

		public override void Execute(ModulesCtxMenuContext context) => context.Operations.Save();
		public override bool IsEnabled(ModulesCtxMenuContext context) => context.Operations.CanSave;

		public override string GetHeader(ModulesCtxMenuContext context) {
			var count = context.Operations.GetSaveModuleCount();
			return count > 1 ?
				string.Format(dnSpy_Debugger_Resources.SaveModulesCommand, count) :
				dnSpy_Debugger_Resources.SaveModuleCommand;
		}
	}
}
