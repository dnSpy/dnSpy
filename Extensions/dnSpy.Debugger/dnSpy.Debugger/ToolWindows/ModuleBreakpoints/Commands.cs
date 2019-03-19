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
using System.ComponentModel.Composition;
using System.Windows.Controls;
using System.Windows.Input;
using dnSpy.Contracts.Controls;
using dnSpy.Contracts.Extension;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Menus;
using dnSpy.Contracts.MVVM;
using dnSpy.Debugger.Properties;

namespace dnSpy.Debugger.ToolWindows.ModuleBreakpoints {
	[ExportAutoLoaded]
	sealed class ModuleBreakpointsCommandsLoader : IAutoLoaded {
		[ImportingConstructor]
		ModuleBreakpointsCommandsLoader(IWpfCommandService wpfCommandService, Lazy<IModuleBreakpointsContent> moduleBreakpointsContent) {
			var cmds = wpfCommandService.GetCommands(ControlConstants.GUID_DEBUGGER_MODULEBREAKPOINTS_LISTVIEW);
			cmds.Add(new RelayCommand(a => moduleBreakpointsContent.Value.Operations.Copy(), a => moduleBreakpointsContent.Value.Operations.CanCopy), ModifierKeys.Control, Key.C);
			cmds.Add(new RelayCommand(a => moduleBreakpointsContent.Value.Operations.Copy(), a => moduleBreakpointsContent.Value.Operations.CanCopy), ModifierKeys.Control, Key.Insert);
			cmds.Add(new RelayCommand(a => moduleBreakpointsContent.Value.Operations.EditModuleName(), a => moduleBreakpointsContent.Value.Operations.CanEditModuleName), ModifierKeys.None, Key.F2);
			cmds.Add(new RelayCommand(a => moduleBreakpointsContent.Value.Operations.EditModuleName(), a => moduleBreakpointsContent.Value.Operations.CanEditModuleName), ModifierKeys.Control, Key.D1);
			cmds.Add(new RelayCommand(a => moduleBreakpointsContent.Value.Operations.EditOrder(), a => moduleBreakpointsContent.Value.Operations.CanEditOrder), ModifierKeys.Control, Key.D2);
			cmds.Add(new RelayCommand(a => moduleBreakpointsContent.Value.Operations.EditProcessName(), a => moduleBreakpointsContent.Value.Operations.CanEditProcessName), ModifierKeys.Control, Key.D3);
			cmds.Add(new RelayCommand(a => moduleBreakpointsContent.Value.Operations.EditAppDomainName(), a => moduleBreakpointsContent.Value.Operations.CanEditAppDomainName), ModifierKeys.Control, Key.D4);
			cmds.Add(new RelayCommand(a => moduleBreakpointsContent.Value.Operations.RemoveModuleBreakpoints(), a => moduleBreakpointsContent.Value.Operations.CanRemoveModuleBreakpoints), ModifierKeys.None, Key.Delete);
			cmds.Add(new RelayCommand(a => moduleBreakpointsContent.Value.Operations.AddModuleBreakpoint(), a => moduleBreakpointsContent.Value.Operations.CanAddModuleBreakpoint), ModifierKeys.None, Key.Insert);

			cmds = wpfCommandService.GetCommands(ControlConstants.GUID_DEBUGGER_MODULEBREAKPOINTS_CONTROL);
			cmds.Add(new RelayCommand(a => moduleBreakpointsContent.Value.FocusSearchTextBox()), ModifierKeys.Control, Key.F);
			cmds.Add(new RelayCommand(a => moduleBreakpointsContent.Value.FocusSearchTextBox()), ModifierKeys.Control, Key.E);
		}
	}

	sealed class ModuleBreakpointsCtxMenuContext {
		public ModuleBreakpointsOperations Operations { get; }
		public ModuleBreakpointsCtxMenuContext(ModuleBreakpointsOperations operations) => Operations = operations;
	}

	abstract class ModuleBreakpointsCtxMenuCommand : MenuItemBase<ModuleBreakpointsCtxMenuContext> {
		protected sealed override object CachedContextKey => ContextKey;
		static readonly object ContextKey = new object();

		protected readonly Lazy<IModuleBreakpointsContent> moduleBreakpointsContent;

		protected ModuleBreakpointsCtxMenuCommand(Lazy<IModuleBreakpointsContent> moduleBreakpointsContent) => this.moduleBreakpointsContent = moduleBreakpointsContent;

		protected sealed override ModuleBreakpointsCtxMenuContext CreateContext(IMenuItemContext context) {
			if (!(context.CreatorObject.Object is ListView))
				return null;
			if (context.CreatorObject.Object != moduleBreakpointsContent.Value.ListView)
				return null;
			return Create();
		}

		ModuleBreakpointsCtxMenuContext Create() => new ModuleBreakpointsCtxMenuContext(moduleBreakpointsContent.Value.Operations);
	}

	[ExportMenuItem(Header = "res:CopyCommand", Icon = DsImagesAttribute.Copy, InputGestureText = "res:ShortCutKeyCtrlC", Group = MenuConstants.GROUP_CTX_DBG_MODULEBPS_COPY, Order = 0)]
	sealed class CopyModuleBreakpointsCtxMenuCommand : ModuleBreakpointsCtxMenuCommand {
		[ImportingConstructor]
		CopyModuleBreakpointsCtxMenuCommand(Lazy<IModuleBreakpointsContent> moduleBreakpointsContent)
			: base(moduleBreakpointsContent) {
		}

		public override void Execute(ModuleBreakpointsCtxMenuContext context) => context.Operations.Copy();
		public override bool IsEnabled(ModuleBreakpointsCtxMenuContext context) => context.Operations.CanCopy;
	}

	[ExportMenuItem(Header = "res:SelectAllCommand", Icon = DsImagesAttribute.Select, InputGestureText = "res:ShortCutKeyCtrlA", Group = MenuConstants.GROUP_CTX_DBG_MODULEBPS_COPY, Order = 10)]
	sealed class SelectAllModuleBreakpointsCtxMenuCommand : ModuleBreakpointsCtxMenuCommand {
		[ImportingConstructor]
		SelectAllModuleBreakpointsCtxMenuCommand(Lazy<IModuleBreakpointsContent> moduleBreakpointsContent)
			: base(moduleBreakpointsContent) {
		}

		public override void Execute(ModuleBreakpointsCtxMenuContext context) => context.Operations.SelectAll();
		public override bool IsEnabled(ModuleBreakpointsCtxMenuContext context) => context.Operations.CanSelectAll;
	}

	[ExportMenuItem(Header = "res:AddBreakpointCommand", InputGestureText = "res:ShortCutKeyInsert", Icon = DsImagesAttribute.Add, Group = MenuConstants.GROUP_CTX_DBG_MODULEBPS_CMDS1, Order = 0)]
	sealed class AddBreakpointBreakpointCtxMenuCommand : ModuleBreakpointsCtxMenuCommand {
		[ImportingConstructor]
		AddBreakpointBreakpointCtxMenuCommand(Lazy<IModuleBreakpointsContent> moduleBreakpointesContent)
			: base(moduleBreakpointesContent) {
		}

		public override void Execute(ModuleBreakpointsCtxMenuContext context) => context.Operations.AddModuleBreakpoint();
		public override bool IsEnabled(ModuleBreakpointsCtxMenuContext context) => context.Operations.CanAddModuleBreakpoint;
	}

	[ExportMenuItem(Header = "res:RemoveBreakpointCommand", InputGestureText = "res:ShortCutKeyDelete", Icon = DsImagesAttribute.RemoveCommand, Group = MenuConstants.GROUP_CTX_DBG_MODULEBPS_CMDS1, Order = 10)]
	sealed class RemoveBreakpointBreakpointCtxMenuCommand : ModuleBreakpointsCtxMenuCommand {
		[ImportingConstructor]
		RemoveBreakpointBreakpointCtxMenuCommand(Lazy<IModuleBreakpointsContent> moduleBreakpointesContent)
			: base(moduleBreakpointesContent) {
		}

		public override void Execute(ModuleBreakpointsCtxMenuContext context) => context.Operations.RemoveModuleBreakpoints();
		public override bool IsEnabled(ModuleBreakpointsCtxMenuContext context) => context.Operations.CanRemoveModuleBreakpoints;
	}

	[ExportMenuItem(Header = "res:RemoveAllBreakpointsCommand", Icon = DsImagesAttribute.ClearBreakpointGroup, Group = MenuConstants.GROUP_CTX_DBG_MODULEBPS_CMDS1, Order = 20)]
	sealed class RemoveAllBreakpointsBreakpointCtxMenuCommand : ModuleBreakpointsCtxMenuCommand {
		[ImportingConstructor]
		RemoveAllBreakpointsBreakpointCtxMenuCommand(Lazy<IModuleBreakpointsContent> moduleBreakpointesContent)
			: base(moduleBreakpointesContent) {
		}

		public override void Execute(ModuleBreakpointsCtxMenuContext context) => context.Operations.RemoveMatchingModuleBreakpoints();
		public override bool IsEnabled(ModuleBreakpointsCtxMenuContext context) => context.Operations.CanRemoveMatchingModuleBreakpoints;
	}

	[ExportMenuItem(Header = "res:EnableBreakpointCommand3", Group = MenuConstants.GROUP_CTX_DBG_MODULEBPS_CMDS1, Order = 30)]
	sealed class EnableBreakpointBreakpointCtxMenuCommand : ModuleBreakpointsCtxMenuCommand {
		[ImportingConstructor]
		EnableBreakpointBreakpointCtxMenuCommand(Lazy<IModuleBreakpointsContent> moduleBreakpointesContent)
			: base(moduleBreakpointesContent) {
		}

		public override void Execute(ModuleBreakpointsCtxMenuContext context) => context.Operations.EnableBreakpoints();
		public override bool IsVisible(ModuleBreakpointsCtxMenuContext context) => context.Operations.CanEnableBreakpoints;
	}

	[ExportMenuItem(Header = "res:DisableBreakpointCommand3", Group = MenuConstants.GROUP_CTX_DBG_MODULEBPS_CMDS1, Order = 40)]
	sealed class DisableBreakpointBreakpointCtxMenuCommand : ModuleBreakpointsCtxMenuCommand {
		[ImportingConstructor]
		DisableBreakpointBreakpointCtxMenuCommand(Lazy<IModuleBreakpointsContent> moduleBreakpointesContent)
			: base(moduleBreakpointesContent) {
		}

		public override void Execute(ModuleBreakpointsCtxMenuContext context) => context.Operations.DisableBreakpoints();
		public override bool IsVisible(ModuleBreakpointsCtxMenuContext context) => context.Operations.CanDisableBreakpoints;
	}

	[ExportMenuItem(Header = "res:EditModuleNameCommand", InputGestureText = "res:ShortCutKeyF2", Icon = DsImagesAttribute.Edit, Group = MenuConstants.GROUP_CTX_DBG_MODULEBPS_CMDS2, Order = 0)]
	sealed class EditModuleNameBreakpointCtxMenuCommand : ModuleBreakpointsCtxMenuCommand {
		[ImportingConstructor]
		EditModuleNameBreakpointCtxMenuCommand(Lazy<IModuleBreakpointsContent> moduleBreakpointesContent)
			: base(moduleBreakpointesContent) {
		}

		public override void Execute(ModuleBreakpointsCtxMenuContext context) => context.Operations.EditModuleName();
		public override bool IsEnabled(ModuleBreakpointsCtxMenuContext context) => context.Operations.CanEditModuleName;
	}

	[ExportMenuItem(Header = "res:EditOrderCommand", Group = MenuConstants.GROUP_CTX_DBG_MODULEBPS_CMDS2, Order = 10)]
	sealed class EditOrderBreakpointCtxMenuCommand : ModuleBreakpointsCtxMenuCommand {
		[ImportingConstructor]
		EditOrderBreakpointCtxMenuCommand(Lazy<IModuleBreakpointsContent> moduleBreakpointesContent)
			: base(moduleBreakpointesContent) {
		}

		public override void Execute(ModuleBreakpointsCtxMenuContext context) => context.Operations.EditOrder();
		public override bool IsEnabled(ModuleBreakpointsCtxMenuContext context) => context.Operations.CanEditOrder;
		public override string GetInputGestureText(ModuleBreakpointsCtxMenuContext context) => string.Format(dnSpy_Debugger_Resources.ShortCutKeyCtrl_DIGIT, "2");
	}

	[ExportMenuItem(Header = "res:EditProcessNameCommand", Group = MenuConstants.GROUP_CTX_DBG_MODULEBPS_CMDS2, Order = 20)]
	sealed class EditProcessNameBreakpointCtxMenuCommand : ModuleBreakpointsCtxMenuCommand {
		[ImportingConstructor]
		EditProcessNameBreakpointCtxMenuCommand(Lazy<IModuleBreakpointsContent> moduleBreakpointesContent)
			: base(moduleBreakpointesContent) {
		}

		public override void Execute(ModuleBreakpointsCtxMenuContext context) => context.Operations.EditProcessName();
		public override bool IsEnabled(ModuleBreakpointsCtxMenuContext context) => context.Operations.CanEditProcessName;
		public override string GetInputGestureText(ModuleBreakpointsCtxMenuContext context) => string.Format(dnSpy_Debugger_Resources.ShortCutKeyCtrl_DIGIT, "3");
	}

	[ExportMenuItem(Header = "res:EditAppDomainNameCommand", Group = MenuConstants.GROUP_CTX_DBG_MODULEBPS_CMDS2, Order = 30)]
	sealed class EditAppDomainNameBreakpointCtxMenuCommand : ModuleBreakpointsCtxMenuCommand {
		[ImportingConstructor]
		EditAppDomainNameBreakpointCtxMenuCommand(Lazy<IModuleBreakpointsContent> moduleBreakpointesContent)
			: base(moduleBreakpointesContent) {
		}

		public override void Execute(ModuleBreakpointsCtxMenuContext context) => context.Operations.EditAppDomainName();
		public override bool IsEnabled(ModuleBreakpointsCtxMenuContext context) => context.Operations.CanEditAppDomainName;
		public override string GetInputGestureText(ModuleBreakpointsCtxMenuContext context) => string.Format(dnSpy_Debugger_Resources.ShortCutKeyCtrl_DIGIT, "4");
	}

	[ExportMenuItem(Header = "res:ExportSelectedCommand", Icon = DsImagesAttribute.Open, Group = MenuConstants.GROUP_CTX_DBG_MODULEBPS_EXPORT, Order = 0)]
	sealed class ExportSelectedBreakpointCtxMenuCommand : ModuleBreakpointsCtxMenuCommand {
		[ImportingConstructor]
		ExportSelectedBreakpointCtxMenuCommand(Lazy<IModuleBreakpointsContent> moduleBreakpointesContent)
			: base(moduleBreakpointesContent) {
		}

		public override void Execute(ModuleBreakpointsCtxMenuContext context) => context.Operations.ExportSelectedBreakpoints();
		public override bool IsEnabled(ModuleBreakpointsCtxMenuContext context) => context.Operations.CanExportSelectedBreakpoints;
	}
}
