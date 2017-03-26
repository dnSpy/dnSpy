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
using System.ComponentModel.Composition;
using System.Windows.Controls;
using System.Windows.Input;
using dnSpy.Contracts.Controls;
using dnSpy.Contracts.Extension;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Menus;
using dnSpy.Contracts.MVVM;

namespace dnSpy.Debugger.ToolWindows.CodeBreakpoints {
	[ExportAutoLoaded]
	sealed class CodeBreakpointsCommandsLoader : IAutoLoaded {
		[ImportingConstructor]
		CodeBreakpointsCommandsLoader(IWpfCommandService wpfCommandService, Lazy<ICodeBreakpointsContent> codeBreakpointsContent) {
			var cmds = wpfCommandService.GetCommands(ControlConstants.GUID_DEBUGGER_CODEBREAKPOINTS_LISTVIEW);
			cmds.Add(new RelayCommand(a => codeBreakpointsContent.Value.Operations.Copy(), a => codeBreakpointsContent.Value.Operations.CanCopy), ModifierKeys.Control, Key.C);
			cmds.Add(new RelayCommand(a => codeBreakpointsContent.Value.Operations.Copy(), a => codeBreakpointsContent.Value.Operations.CanCopy), ModifierKeys.Control, Key.Insert);
			cmds.Add(new RelayCommand(a => codeBreakpointsContent.Value.Operations.RemoveCodeBreakpoints(), a => codeBreakpointsContent.Value.Operations.CanRemoveCodeBreakpoints), ModifierKeys.None, Key.Delete);
			cmds.Add(new RelayCommand(a => codeBreakpointsContent.Value.Operations.ToggleEnabled(), a => codeBreakpointsContent.Value.Operations.CanToggleEnabled), ModifierKeys.None, Key.Space);
			cmds.Add(new RelayCommand(a => codeBreakpointsContent.Value.Operations.GoToSourceCode(), a => codeBreakpointsContent.Value.Operations.CanGoToSourceCode), ModifierKeys.None, Key.Enter);

			cmds = wpfCommandService.GetCommands(ControlConstants.GUID_DEBUGGER_CODEBREAKPOINTS_CONTROL);
			cmds.Add(new RelayCommand(a => codeBreakpointsContent.Value.FocusSearchTextBox()), ModifierKeys.Control, Key.F);
			cmds.Add(new RelayCommand(a => codeBreakpointsContent.Value.FocusSearchTextBox()), ModifierKeys.Control, Key.E);
		}
	}

	sealed class CodeBreakpointsCtxMenuContext {
		public CodeBreakpointsOperations Operations { get; }
		public CodeBreakpointsCtxMenuContext(CodeBreakpointsOperations operations) => Operations = operations;
	}

	abstract class CodeBreakpointsCtxMenuCommand : MenuItemBase<CodeBreakpointsCtxMenuContext> {
		protected sealed override object CachedContextKey => ContextKey;
		static readonly object ContextKey = new object();

		protected readonly Lazy<ICodeBreakpointsContent> codeBreakpointsContent;

		protected CodeBreakpointsCtxMenuCommand(Lazy<ICodeBreakpointsContent> codeBreakpointsContent) => this.codeBreakpointsContent = codeBreakpointsContent;

		protected sealed override CodeBreakpointsCtxMenuContext CreateContext(IMenuItemContext context) {
			if (!(context.CreatorObject.Object is ListView))
				return null;
			if (context.CreatorObject.Object != codeBreakpointsContent.Value.ListView)
				return null;
			return Create();
		}

		CodeBreakpointsCtxMenuContext Create() => new CodeBreakpointsCtxMenuContext(codeBreakpointsContent.Value.Operations);
	}

	[ExportMenuItem(Header = "res:CopyCommand", Icon = DsImagesAttribute.Copy, InputGestureText = "res:ShortCutKeyCtrlC", Group = MenuConstants.GROUP_CTX_DBG_CODEBPS_COPY, Order = 0)]
	sealed class CopyCodeBreakpointsCtxMenuCommand : CodeBreakpointsCtxMenuCommand {
		[ImportingConstructor]
		CopyCodeBreakpointsCtxMenuCommand(Lazy<ICodeBreakpointsContent> codeBreakpointsContent)
			: base(codeBreakpointsContent) {
		}

		public override void Execute(CodeBreakpointsCtxMenuContext context) => context.Operations.Copy();
		public override bool IsEnabled(CodeBreakpointsCtxMenuContext context) => context.Operations.CanCopy;
	}

	[ExportMenuItem(Header = "res:SelectAllCommand", Icon = DsImagesAttribute.Select, InputGestureText = "res:ShortCutKeyCtrlA", Group = MenuConstants.GROUP_CTX_DBG_CODEBPS_COPY, Order = 10)]
	sealed class SelectAllCodeBreakpointsCtxMenuCommand : CodeBreakpointsCtxMenuCommand {
		[ImportingConstructor]
		SelectAllCodeBreakpointsCtxMenuCommand(Lazy<ICodeBreakpointsContent> codeBreakpointsContent)
			: base(codeBreakpointsContent) {
		}

		public override void Execute(CodeBreakpointsCtxMenuContext context) => context.Operations.SelectAll();
		public override bool IsEnabled(CodeBreakpointsCtxMenuContext context) => context.Operations.CanSelectAll;
	}

	[ExportMenuItem(Header = "res:GoToSourceCodeCommand", Icon = DsImagesAttribute.GoToSourceCode, InputGestureText = "res:ShortCutKeyEnter", Group = MenuConstants.GROUP_CTX_DBG_CODEBPS_CODE, Order = 0)]
	sealed class GoToSourceCodeBreakpointCtxMenuCommand : CodeBreakpointsCtxMenuCommand {
		[ImportingConstructor]
		GoToSourceCodeBreakpointCtxMenuCommand(Lazy<ICodeBreakpointsContent> codeBreakpointesContent)
			: base(codeBreakpointesContent) {
		}

		public override void Execute(CodeBreakpointsCtxMenuContext context) => context.Operations.GoToSourceCode();
		public override bool IsEnabled(CodeBreakpointsCtxMenuContext context) => context.Operations.CanGoToSourceCode;
	}

	[ExportMenuItem(Header = "res:GoToDisassemblyCommand", Icon = DsImagesAttribute.DisassemblyWindow, Group = MenuConstants.GROUP_CTX_DBG_CODEBPS_CODE, Order = 10)]
	sealed class GoToDisassemblyBreakpointCtxMenuCommand : CodeBreakpointsCtxMenuCommand {
		[ImportingConstructor]
		GoToDisassemblyBreakpointCtxMenuCommand(Lazy<ICodeBreakpointsContent> codeBreakpointesContent)
			: base(codeBreakpointesContent) {
		}

		public override void Execute(CodeBreakpointsCtxMenuContext context) => context.Operations.GoToDisassembly();
		public override bool IsEnabled(CodeBreakpointsCtxMenuContext context) => context.Operations.CanGoToDisassembly;
	}

	[ExportMenuItem(Header = "res:RemoveBreakpointCommand", InputGestureText = "res:ShortCutKeyDelete", Icon = DsImagesAttribute.Cancel, Group = MenuConstants.GROUP_CTX_DBG_CODEBPS_CMDS1, Order = 0)]
	sealed class RemoveBreakpointBreakpointCtxMenuCommand : CodeBreakpointsCtxMenuCommand {
		[ImportingConstructor]
		RemoveBreakpointBreakpointCtxMenuCommand(Lazy<ICodeBreakpointsContent> codeBreakpointesContent)
			: base(codeBreakpointesContent) {
		}

		public override void Execute(CodeBreakpointsCtxMenuContext context) => context.Operations.RemoveCodeBreakpoints();
		public override bool IsEnabled(CodeBreakpointsCtxMenuContext context) => context.Operations.CanRemoveCodeBreakpoints;
	}

	[ExportMenuItem(Header = "res:RemoveAllBreakpointsCommand", Icon = DsImagesAttribute.ClearBreakpointGroup, Group = MenuConstants.GROUP_CTX_DBG_CODEBPS_CMDS1, Order = 10)]
	sealed class RemoveAllBreakpointsBreakpointCtxMenuCommand : CodeBreakpointsCtxMenuCommand {
		[ImportingConstructor]
		RemoveAllBreakpointsBreakpointCtxMenuCommand(Lazy<ICodeBreakpointsContent> codeBreakpointesContent)
			: base(codeBreakpointesContent) {
		}

		public override void Execute(CodeBreakpointsCtxMenuContext context) => context.Operations.RemoveMatchingCodeBreakpoints();
		public override bool IsEnabled(CodeBreakpointsCtxMenuContext context) => context.Operations.CanRemoveMatchingCodeBreakpoints;
	}

	[ExportMenuItem(Header = "res:EnableBreakpointCommand3", Group = MenuConstants.GROUP_CTX_DBG_CODEBPS_CMDS1, Order = 20)]
	sealed class EnableBreakpointBreakpointCtxMenuCommand : CodeBreakpointsCtxMenuCommand {
		[ImportingConstructor]
		EnableBreakpointBreakpointCtxMenuCommand(Lazy<ICodeBreakpointsContent> codeBreakpointesContent)
			: base(codeBreakpointesContent) {
		}

		public override void Execute(CodeBreakpointsCtxMenuContext context) => context.Operations.EnableBreakpoints();
		public override bool IsVisible(CodeBreakpointsCtxMenuContext context) => context.Operations.CanEnableBreakpoints;
	}

	[ExportMenuItem(Header = "res:DisableBreakpointCommand3", Group = MenuConstants.GROUP_CTX_DBG_CODEBPS_CMDS1, Order = 30)]
	sealed class DisableBreakpointBreakpointCtxMenuCommand : CodeBreakpointsCtxMenuCommand {
		[ImportingConstructor]
		DisableBreakpointBreakpointCtxMenuCommand(Lazy<ICodeBreakpointsContent> codeBreakpointesContent)
			: base(codeBreakpointesContent) {
		}

		public override void Execute(CodeBreakpointsCtxMenuContext context) => context.Operations.DisableBreakpoints();
		public override bool IsVisible(CodeBreakpointsCtxMenuContext context) => context.Operations.CanDisableBreakpoints;
	}

	[ExportMenuItem(Header = "res:ExportSelectedCommand", Icon = DsImagesAttribute.Open, Group = MenuConstants.GROUP_CTX_DBG_CODEBPS_EXPORT, Order = 0)]
	sealed class ExportSelectedBreakpointCtxMenuCommand : CodeBreakpointsCtxMenuCommand {
		[ImportingConstructor]
		ExportSelectedBreakpointCtxMenuCommand(Lazy<ICodeBreakpointsContent> codeBreakpointesContent)
			: base(codeBreakpointesContent) {
		}

		public override void Execute(CodeBreakpointsCtxMenuContext context) => context.Operations.ExportSelectedBreakpoints();
		public override bool IsEnabled(CodeBreakpointsCtxMenuContext context) => context.Operations.CanExportSelectedBreakpoints;
	}
}
