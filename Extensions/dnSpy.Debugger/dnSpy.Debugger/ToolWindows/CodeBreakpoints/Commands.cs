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

namespace dnSpy.Debugger.ToolWindows.CodeBreakpoints {
	[ExportAutoLoaded]
	sealed class CodeBreakpointsCommandsLoader : IAutoLoaded {
		[ImportingConstructor]
		CodeBreakpointsCommandsLoader(IWpfCommandService wpfCommandService, Lazy<ICodeBreakpointsContent> codeBreakpointsContent) {
			var cmds = wpfCommandService.GetCommands(ControlConstants.GUID_DEBUGGER_CODEBREAKPOINTS_LISTVIEW);
			cmds.Add(new RelayCommand(a => codeBreakpointsContent.Value.Operations.Copy(), a => codeBreakpointsContent.Value.Operations.CanCopy), ModifierKeys.Control, Key.C);
			cmds.Add(new RelayCommand(a => codeBreakpointsContent.Value.Operations.Copy(), a => codeBreakpointsContent.Value.Operations.CanCopy), ModifierKeys.Control, Key.Insert);
			cmds.Add(new RelayCommand(a => codeBreakpointsContent.Value.Operations.RemoveCodeBreakpoints(), a => codeBreakpointsContent.Value.Operations.CanRemoveCodeBreakpoints), ModifierKeys.None, Key.Delete);
			cmds.Add(new RelayCommand(a => codeBreakpointsContent.Value.Operations.GoToSourceCode(false), a => codeBreakpointsContent.Value.Operations.CanGoToSourceCode), ModifierKeys.None, Key.Enter);
			cmds.Add(new RelayCommand(a => codeBreakpointsContent.Value.Operations.EditSettings(), a => codeBreakpointsContent.Value.Operations.CanEditSettings), ModifierKeys.Alt, Key.Enter);
			cmds.Add(new RelayCommand(a => codeBreakpointsContent.Value.Operations.EditLabels(), a => codeBreakpointsContent.Value.Operations.CanEditLabels), ModifierKeys.None, Key.F2);

			cmds = wpfCommandService.GetCommands(ControlConstants.GUID_DEBUGGER_CODEBREAKPOINTS_CONTROL);
			cmds.Add(new RelayCommand(a => codeBreakpointsContent.Value.FocusSearchTextBox()), ModifierKeys.Control, Key.F);
			cmds.Add(new RelayCommand(a => codeBreakpointsContent.Value.FocusSearchTextBox()), ModifierKeys.Control, Key.E);
		}
	}

	sealed class CodeBreakpointsCtxMenuContext {
		public CodeBreakpointsOperations Operations { get; }
		public CodeBreakpointsCtxMenuContext(CodeBreakpointsOperations operations) => Operations = operations;
	}

	abstract class BreakpointsCtxMenuCommand : MenuItemBase<CodeBreakpointsCtxMenuContext> {
		protected sealed override object CachedContextKey => ContextKey;
		static readonly object ContextKey = new object();

		protected readonly Lazy<ICodeBreakpointsContent> codeBreakpointsContent;

		protected BreakpointsCtxMenuCommand(Lazy<ICodeBreakpointsContent> codeBreakpointsContent) => this.codeBreakpointsContent = codeBreakpointsContent;

		protected sealed override CodeBreakpointsCtxMenuContext? CreateContext(IMenuItemContext context) {
			if (!(context.CreatorObject.Object is ListView))
				return null;
			if (context.CreatorObject.Object != codeBreakpointsContent.Value.ListView)
				return null;
			return Create();
		}

		CodeBreakpointsCtxMenuContext Create() => new CodeBreakpointsCtxMenuContext(codeBreakpointsContent.Value.Operations);
	}

	[ExportMenuItem(Header = "res:CopyCommand", Icon = DsImagesAttribute.Copy, InputGestureText = "res:ShortCutKeyCtrlC", Group = MenuConstants.GROUP_CTX_DBG_CODEBPS_COPY, Order = 0)]
	sealed class CopyBreakpointsCtxMenuCommand : BreakpointsCtxMenuCommand {
		[ImportingConstructor]
		CopyBreakpointsCtxMenuCommand(Lazy<ICodeBreakpointsContent> codeBreakpointsContent)
			: base(codeBreakpointsContent) {
		}

		public override void Execute(CodeBreakpointsCtxMenuContext context) => context.Operations.Copy();
		public override bool IsEnabled(CodeBreakpointsCtxMenuContext context) => context.Operations.CanCopy;
	}

	[ExportMenuItem(Header = "res:SelectAllCommand", Icon = DsImagesAttribute.Select, InputGestureText = "res:ShortCutKeyCtrlA", Group = MenuConstants.GROUP_CTX_DBG_CODEBPS_COPY, Order = 10)]
	sealed class SelectAllBreakpointsCtxMenuCommand : BreakpointsCtxMenuCommand {
		[ImportingConstructor]
		SelectAllBreakpointsCtxMenuCommand(Lazy<ICodeBreakpointsContent> codeBreakpointsContent)
			: base(codeBreakpointsContent) {
		}

		public override void Execute(CodeBreakpointsCtxMenuContext context) => context.Operations.SelectAll();
		public override bool IsEnabled(CodeBreakpointsCtxMenuContext context) => context.Operations.CanSelectAll;
	}

	[ExportMenuItem(Header = "res:GoToSourceCodeCommand", Icon = DsImagesAttribute.GoToSourceCode, InputGestureText = "res:ShortCutKeyEnter", Group = MenuConstants.GROUP_CTX_DBG_CODEBPS_CODE, Order = 0)]
	sealed class GoToSourceCodeBreakpointsCtxMenuCommand : BreakpointsCtxMenuCommand {
		[ImportingConstructor]
		GoToSourceCodeBreakpointsCtxMenuCommand(Lazy<ICodeBreakpointsContent> codeBreakpointesContent)
			: base(codeBreakpointesContent) {
		}

		public override void Execute(CodeBreakpointsCtxMenuContext context) => context.Operations.GoToSourceCode(false);
		public override bool IsEnabled(CodeBreakpointsCtxMenuContext context) => context.Operations.CanGoToSourceCode;
	}

	[ExportMenuItem(Header = "res:GoToDisassemblyCommand2", Icon = DsImagesAttribute.DisassemblyWindow, Group = MenuConstants.GROUP_CTX_DBG_CODEBPS_CODE, Order = 10)]
	sealed class GoToDisassemblyBreakpointsCtxMenuCommand : BreakpointsCtxMenuCommand {
		[ImportingConstructor]
		GoToDisassemblyBreakpointsCtxMenuCommand(Lazy<ICodeBreakpointsContent> codeBreakpointesContent)
			: base(codeBreakpointesContent) {
		}

		public override void Execute(CodeBreakpointsCtxMenuContext context) => context.Operations.GoToDisassembly();
		public override bool IsEnabled(CodeBreakpointsCtxMenuContext context) => context.Operations.CanGoToDisassembly;
	}

	[ExportMenuItem(Header = "res:SettingsCommand", InputGestureText = "res:ShortCutKeyAltEnter", Icon = DsImagesAttribute.Settings, Group = MenuConstants.GROUP_CTX_DBG_CODEBPS_SETTINGS, Order = 0)]
	sealed class EditSettingsBreakpointsCtxMenuCommand : BreakpointsCtxMenuCommand {
		[ImportingConstructor]
		EditSettingsBreakpointsCtxMenuCommand(Lazy<ICodeBreakpointsContent> codeBreakpointesContent)
			: base(codeBreakpointesContent) {
		}

		public override void Execute(CodeBreakpointsCtxMenuContext context) => context.Operations.EditSettings();
		public override bool IsEnabled(CodeBreakpointsCtxMenuContext context) => context.Operations.CanEditSettings;
	}

	[ExportMenuItem(Header = "res:EditLabelsCommand", InputGestureText = "res:ShortCutKeyF2", Group = MenuConstants.GROUP_CTX_DBG_CODEBPS_SETTINGS, Order = 10)]
	sealed class EditLabelsBreakpointsCtxMenuCommand : BreakpointsCtxMenuCommand {
		[ImportingConstructor]
		EditLabelsBreakpointsCtxMenuCommand(Lazy<ICodeBreakpointsContent> codeBreakpointesContent)
			: base(codeBreakpointesContent) {
		}

		public override void Execute(CodeBreakpointsCtxMenuContext context) => context.Operations.EditLabels();
		public override bool IsEnabled(CodeBreakpointsCtxMenuContext context) => context.Operations.CanEditLabels;
	}

	[ExportMenuItem(Header = "res:ResetBreakpointHitCountCommand", Group = MenuConstants.GROUP_CTX_DBG_CODEBPS_SETTINGS, Order = 20)]
	sealed class ResetHitCountBreakpointsCtxMenuCommand : BreakpointsCtxMenuCommand {
		[ImportingConstructor]
		ResetHitCountBreakpointsCtxMenuCommand(Lazy<ICodeBreakpointsContent> codeBreakpointesContent)
			: base(codeBreakpointesContent) {
		}

		public override void Execute(CodeBreakpointsCtxMenuContext context) => context.Operations.ResetHitCount();
		public override bool IsEnabled(CodeBreakpointsCtxMenuContext context) => context.Operations.CanResetHitCount;
	}

	[ExportMenuItem(Header = "res:RemoveBreakpointCommand", InputGestureText = "res:ShortCutKeyDelete", Icon = DsImagesAttribute.Cancel, Group = MenuConstants.GROUP_CTX_DBG_CODEBPS_CMDS1, Order = 0)]
	sealed class RemoveBreakpointBreakpointsCtxMenuCommand : BreakpointsCtxMenuCommand {
		[ImportingConstructor]
		RemoveBreakpointBreakpointsCtxMenuCommand(Lazy<ICodeBreakpointsContent> codeBreakpointesContent)
			: base(codeBreakpointesContent) {
		}

		public override void Execute(CodeBreakpointsCtxMenuContext context) => context.Operations.RemoveCodeBreakpoints();
		public override bool IsEnabled(CodeBreakpointsCtxMenuContext context) => context.Operations.CanRemoveCodeBreakpoints;
	}

	[ExportMenuItem(Header = "res:RemoveAllBreakpointsCommand", Icon = DsImagesAttribute.ClearBreakpointGroup, Group = MenuConstants.GROUP_CTX_DBG_CODEBPS_CMDS1, Order = 10)]
	sealed class RemoveAllBreakpointsBreakpointsCtxMenuCommand : BreakpointsCtxMenuCommand {
		[ImportingConstructor]
		RemoveAllBreakpointsBreakpointsCtxMenuCommand(Lazy<ICodeBreakpointsContent> codeBreakpointesContent)
			: base(codeBreakpointesContent) {
		}

		public override void Execute(CodeBreakpointsCtxMenuContext context) => context.Operations.RemoveMatchingCodeBreakpoints();
		public override bool IsEnabled(CodeBreakpointsCtxMenuContext context) => context.Operations.CanRemoveMatchingCodeBreakpoints;
	}

	[ExportMenuItem(Header = "res:EnableBreakpointCommand3", Group = MenuConstants.GROUP_CTX_DBG_CODEBPS_CMDS1, Order = 20)]
	sealed class EnableBreakpointBreakpointsCtxMenuCommand : BreakpointsCtxMenuCommand {
		[ImportingConstructor]
		EnableBreakpointBreakpointsCtxMenuCommand(Lazy<ICodeBreakpointsContent> codeBreakpointesContent)
			: base(codeBreakpointesContent) {
		}

		public override void Execute(CodeBreakpointsCtxMenuContext context) => context.Operations.EnableBreakpoints();
		public override bool IsVisible(CodeBreakpointsCtxMenuContext context) => context.Operations.CanEnableBreakpoints;
	}

	[ExportMenuItem(Header = "res:DisableBreakpointCommand3", Group = MenuConstants.GROUP_CTX_DBG_CODEBPS_CMDS1, Order = 30)]
	sealed class DisableBreakpointBreakpointsCtxMenuCommand : BreakpointsCtxMenuCommand {
		[ImportingConstructor]
		DisableBreakpointBreakpointsCtxMenuCommand(Lazy<ICodeBreakpointsContent> codeBreakpointesContent)
			: base(codeBreakpointesContent) {
		}

		public override void Execute(CodeBreakpointsCtxMenuContext context) => context.Operations.DisableBreakpoints();
		public override bool IsVisible(CodeBreakpointsCtxMenuContext context) => context.Operations.CanDisableBreakpoints;
	}

	[ExportMenuItem(Header = "res:ExportSelectedCommand", Icon = DsImagesAttribute.Open, Group = MenuConstants.GROUP_CTX_DBG_CODEBPS_EXPORT, Order = 0)]
	sealed class ExportSelectedBreakpointsCtxMenuCommand : BreakpointsCtxMenuCommand {
		[ImportingConstructor]
		ExportSelectedBreakpointsCtxMenuCommand(Lazy<ICodeBreakpointsContent> codeBreakpointesContent)
			: base(codeBreakpointesContent) {
		}

		public override void Execute(CodeBreakpointsCtxMenuContext context) => context.Operations.ExportSelectedBreakpoints();
		public override bool IsEnabled(CodeBreakpointsCtxMenuContext context) => context.Operations.CanExportSelectedBreakpoints;
	}

	[ExportMenuItem(Header = "res:ShowTokensCommand", Group = MenuConstants.GROUP_CTX_DBG_CODEBPS_OPTS, Order = 0)]
	sealed class ShowTokensBreakpointsCtxMenuCommand : BreakpointsCtxMenuCommand {
		[ImportingConstructor]
		ShowTokensBreakpointsCtxMenuCommand(Lazy<ICodeBreakpointsContent> codeBreakpointesContent)
			: base(codeBreakpointesContent) {
		}

		public override void Execute(CodeBreakpointsCtxMenuContext context) => context.Operations.ShowTokens = !context.Operations.ShowTokens;
		public override bool IsChecked(CodeBreakpointsCtxMenuContext context) => context.Operations.ShowTokens;
	}

	[ExportMenuItem(Header = "res:ShowModuleNamesCommand", Group = MenuConstants.GROUP_CTX_DBG_CODEBPS_OPTS, Order = 10)]
	sealed class ShowModuleNamesBreakpointsCtxMenuCommand : BreakpointsCtxMenuCommand {
		[ImportingConstructor]
		ShowModuleNamesBreakpointsCtxMenuCommand(Lazy<ICodeBreakpointsContent> codeBreakpointesContent)
			: base(codeBreakpointesContent) {
		}

		public override void Execute(CodeBreakpointsCtxMenuContext context) => context.Operations.ShowModuleNames = !context.Operations.ShowModuleNames;
		public override bool IsChecked(CodeBreakpointsCtxMenuContext context) => context.Operations.ShowModuleNames;
	}

	[ExportMenuItem(Header = "res:ShowParameterTypesCommand2", Group = MenuConstants.GROUP_CTX_DBG_CODEBPS_OPTS, Order = 20)]
	sealed class ShowParameterTypesBreakpointsCtxMenuCommand : BreakpointsCtxMenuCommand {
		[ImportingConstructor]
		ShowParameterTypesBreakpointsCtxMenuCommand(Lazy<ICodeBreakpointsContent> codeBreakpointesContent)
			: base(codeBreakpointesContent) {
		}

		public override void Execute(CodeBreakpointsCtxMenuContext context) => context.Operations.ShowParameterTypes = !context.Operations.ShowParameterTypes;
		public override bool IsChecked(CodeBreakpointsCtxMenuContext context) => context.Operations.ShowParameterTypes;
	}

	[ExportMenuItem(Header = "res:ShowParameterNamesCommand", Group = MenuConstants.GROUP_CTX_DBG_CODEBPS_OPTS, Order = 30)]
	sealed class ShowParameterNamesBreakpointsCtxMenuCommand : BreakpointsCtxMenuCommand {
		[ImportingConstructor]
		ShowParameterNamesBreakpointsCtxMenuCommand(Lazy<ICodeBreakpointsContent> codeBreakpointesContent)
			: base(codeBreakpointesContent) {
		}

		public override void Execute(CodeBreakpointsCtxMenuContext context) => context.Operations.ShowParameterNames = !context.Operations.ShowParameterNames;
		public override bool IsChecked(CodeBreakpointsCtxMenuContext context) => context.Operations.ShowParameterNames;
	}

	[ExportMenuItem(Header = "res:ShowDeclaringTypesCommand", Group = MenuConstants.GROUP_CTX_DBG_CODEBPS_OPTS, Order = 40)]
	sealed class ShowDeclaringTypesBreakpointsCtxMenuCommand : BreakpointsCtxMenuCommand {
		[ImportingConstructor]
		ShowDeclaringTypesBreakpointsCtxMenuCommand(Lazy<ICodeBreakpointsContent> codeBreakpointesContent)
			: base(codeBreakpointesContent) {
		}

		public override void Execute(CodeBreakpointsCtxMenuContext context) => context.Operations.ShowDeclaringTypes = !context.Operations.ShowDeclaringTypes;
		public override bool IsChecked(CodeBreakpointsCtxMenuContext context) => context.Operations.ShowDeclaringTypes;
	}

	[ExportMenuItem(Header = "res:ShowNamespacesCommand", Group = MenuConstants.GROUP_CTX_DBG_CODEBPS_OPTS, Order = 50)]
	sealed class ShowNamespacesBreakpointsCtxMenuCommand : BreakpointsCtxMenuCommand {
		[ImportingConstructor]
		ShowNamespacesBreakpointsCtxMenuCommand(Lazy<ICodeBreakpointsContent> codeBreakpointesContent)
			: base(codeBreakpointesContent) {
		}

		public override void Execute(CodeBreakpointsCtxMenuContext context) => context.Operations.ShowNamespaces = !context.Operations.ShowNamespaces;
		public override bool IsChecked(CodeBreakpointsCtxMenuContext context) => context.Operations.ShowNamespaces;
	}

	[ExportMenuItem(Header = "res:ShowReturnTypesCommand", Group = MenuConstants.GROUP_CTX_DBG_CODEBPS_OPTS, Order = 60)]
	sealed class ShowReturnTypesBreakpointsCtxMenuCommand : BreakpointsCtxMenuCommand {
		[ImportingConstructor]
		ShowReturnTypesBreakpointsCtxMenuCommand(Lazy<ICodeBreakpointsContent> codeBreakpointesContent)
			: base(codeBreakpointesContent) {
		}

		public override void Execute(CodeBreakpointsCtxMenuContext context) => context.Operations.ShowReturnTypes = !context.Operations.ShowReturnTypes;
		public override bool IsChecked(CodeBreakpointsCtxMenuContext context) => context.Operations.ShowReturnTypes;
	}

	[ExportMenuItem(Header = "res:ShowIntrinsicTypeKeywordsCommand", Group = MenuConstants.GROUP_CTX_DBG_CODEBPS_OPTS, Order = 70)]
	sealed class ShowTypeKeywordsBreakpointsCtxMenuCommand : BreakpointsCtxMenuCommand {
		[ImportingConstructor]
		ShowTypeKeywordsBreakpointsCtxMenuCommand(Lazy<ICodeBreakpointsContent> codeBreakpointesContent)
			: base(codeBreakpointesContent) {
		}

		public override void Execute(CodeBreakpointsCtxMenuContext context) => context.Operations.ShowIntrinsicTypeKeywords = !context.Operations.ShowIntrinsicTypeKeywords;
		public override bool IsChecked(CodeBreakpointsCtxMenuContext context) => context.Operations.ShowIntrinsicTypeKeywords;
	}
}
