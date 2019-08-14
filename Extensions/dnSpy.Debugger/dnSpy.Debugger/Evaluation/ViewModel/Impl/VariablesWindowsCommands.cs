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
using System.Linq;
using dnSpy.Contracts.Debugger.Evaluation;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Menus;
using dnSpy.Contracts.Utilities;
using dnSpy.Debugger.Properties;

namespace dnSpy.Debugger.Evaluation.ViewModel.Impl {
	sealed class VariablesWindowCtxMenuContext {
		public VariablesWindowOperations Operations { get; }
		public IValueNodesVM VM { get; }
		public VariablesWindowCtxMenuContext(VariablesWindowOperations operations, IValueNodesVM vm) {
			Operations = operations;
			VM = vm;
		}
	}

	abstract class VariablesWindowCtxMenuCommand : MenuItemBase<VariablesWindowCtxMenuContext> {
		protected sealed override object CachedContextKey => ContextKey;
		static readonly object ContextKey = new object();

		protected readonly Lazy<VariablesWindowOperations> operations;

		protected VariablesWindowCtxMenuCommand(Lazy<VariablesWindowOperations> operations) => this.operations = operations;

		protected sealed override VariablesWindowCtxMenuContext? CreateContext(IMenuItemContext context) {
			var vm = context.Find<IValueNodesVM>();
			if (vm is null)
				return null;
			return new VariablesWindowCtxMenuContext(operations.Value, vm);
		}
	}

	[ExportMenuItem(Header = "res:CopyCommand", Icon = DsImagesAttribute.Copy, InputGestureText = "res:ShortCutKeyCtrlC", Group = MenuConstants.GROUP_CTX_DBG_VARIABLES_WINDOW_COPY, Order = 0)]
	sealed class CopyVariablesWindowCtxMenuCommand : VariablesWindowCtxMenuCommand {
		[ImportingConstructor]
		CopyVariablesWindowCtxMenuCommand(Lazy<VariablesWindowOperations> operations) : base(operations) { }
		public override void Execute(VariablesWindowCtxMenuContext context) => context.Operations.Copy(context.VM);
		public override bool IsEnabled(VariablesWindowCtxMenuContext context) => context.Operations.CanCopy(context.VM);
	}

	[ExportMenuItem(Header = "res:CopyExpressionCommand", Icon = DsImagesAttribute.Copy, Group = MenuConstants.GROUP_CTX_DBG_VARIABLES_WINDOW_COPY, Order = 10)]
	sealed class CopyExpressionVariablesWindowCtxMenuCommand : VariablesWindowCtxMenuCommand {
		[ImportingConstructor]
		CopyExpressionVariablesWindowCtxMenuCommand(Lazy<VariablesWindowOperations> operations) : base(operations) { }
		public override void Execute(VariablesWindowCtxMenuContext context) => context.Operations.CopyExpression(context.VM);
		public override bool IsEnabled(VariablesWindowCtxMenuContext context) => context.Operations.CanCopyExpression(context.VM);
	}

	[ExportMenuItem(Header = "res:PasteCommand", Icon = DsImagesAttribute.Paste, InputGestureText = "res:ShortCutKeyCtrlV", Group = MenuConstants.GROUP_CTX_DBG_VARIABLES_WINDOW_COPY, Order = 20)]
	sealed class PasteVariablesWindowCtxMenuCommand : VariablesWindowCtxMenuCommand {
		[ImportingConstructor]
		PasteVariablesWindowCtxMenuCommand(Lazy<VariablesWindowOperations> operations) : base(operations) { }
		public override void Execute(VariablesWindowCtxMenuContext context) => context.Operations.Paste(context.VM);
		public override bool IsEnabled(VariablesWindowCtxMenuContext context) => context.Operations.CanPaste(context.VM);
		public override bool IsVisible(VariablesWindowCtxMenuContext context) => context.Operations.SupportsPaste(context.VM);
	}

	[ExportMenuItem(Header = "res:LocalsEditValueCommand", Icon = DsImagesAttribute.Edit, Group = MenuConstants.GROUP_CTX_DBG_VARIABLES_WINDOW_VALUES, Order = 0)]
	sealed class EditValueVariablesWindowCtxMenuCommand : VariablesWindowCtxMenuCommand {
		[ImportingConstructor]
		EditValueVariablesWindowCtxMenuCommand(Lazy<VariablesWindowOperations> operations) : base(operations) { }
		public override void Execute(VariablesWindowCtxMenuContext context) => context.Operations.EditValue(context.VM);
		public override bool IsEnabled(VariablesWindowCtxMenuContext context) => context.Operations.CanEditValue(context.VM);
		public override string? GetInputGestureText(VariablesWindowCtxMenuContext context) {
			switch (context.VM.VariablesWindowKind) {
			case VariablesWindowKind.Locals: return dnSpy_Debugger_Resources.ShortCutKeyF2;
			case VariablesWindowKind.Autos: return dnSpy_Debugger_Resources.ShortCutKeyF2;
			case VariablesWindowKind.Watch: return null;
			default: throw new InvalidOperationException();
			}
		}
	}

	[ExportMenuItem(Header = "res:LocalsCopyValueCommand", InputGestureText = "res:ShortCutKeyCtrlShiftC", Group = MenuConstants.GROUP_CTX_DBG_VARIABLES_WINDOW_VALUES, Order = 10)]
	sealed class CopyValueVariablesWindowCtxMenuCommand : VariablesWindowCtxMenuCommand {
		[ImportingConstructor]
		CopyValueVariablesWindowCtxMenuCommand(Lazy<VariablesWindowOperations> operations) : base(operations) { }
		public override void Execute(VariablesWindowCtxMenuContext context) => context.Operations.CopyValue(context.VM);
		public override bool IsEnabled(VariablesWindowCtxMenuContext context) => context.Operations.CanCopyValue(context.VM);
	}

	[ExportMenuItem(Header = "res:LocalsAddWatchCommand", Icon = DsImagesAttribute.Watch, Group = MenuConstants.GROUP_CTX_DBG_VARIABLES_WINDOW_VALUES, Order = 20)]
	sealed class AddWatchVariablesWindowCtxMenuCommand : VariablesWindowCtxMenuCommand {
		[ImportingConstructor]
		AddWatchVariablesWindowCtxMenuCommand(Lazy<VariablesWindowOperations> operations) : base(operations) { }
		public override void Execute(VariablesWindowCtxMenuContext context) => context.Operations.AddWatch(context.VM);
		public override bool IsEnabled(VariablesWindowCtxMenuContext context) => context.Operations.CanAddWatch(context.VM);
	}

	// Order = 30: Add _Parallel Watch

	[ExportMenuItem(Header = "res:DeleteWatchCommand", Icon = DsImagesAttribute.DeleteWatch, InputGestureText = "res:ShortCutKeyDelete", Group = MenuConstants.GROUP_CTX_DBG_VARIABLES_WINDOW_VALUES, Order = 40)]
	sealed class DeleteWatchVariablesWindowCtxMenuCommand : VariablesWindowCtxMenuCommand {
		[ImportingConstructor]
		DeleteWatchVariablesWindowCtxMenuCommand(Lazy<VariablesWindowOperations> operations) : base(operations) { }
		public override void Execute(VariablesWindowCtxMenuContext context) => context.Operations.DeleteWatch(context.VM);
		public override bool IsEnabled(VariablesWindowCtxMenuContext context) => context.Operations.CanDeleteWatch(context.VM);
		public override bool IsVisible(VariablesWindowCtxMenuContext context) => context.Operations.SupportsDeleteWatch(context.VM);
	}

	[ExportMenuItem(Header = "res:MakeObjectIdCommand", Group = MenuConstants.GROUP_CTX_DBG_VARIABLES_WINDOW_VALUES, Order = 50)]
	sealed class MakeObjectIdVariablesWindowCtxMenuCommand : VariablesWindowCtxMenuCommand {
		[ImportingConstructor]
		MakeObjectIdVariablesWindowCtxMenuCommand(Lazy<VariablesWindowOperations> operations) : base(operations) { }
		public override void Execute(VariablesWindowCtxMenuContext context) => context.Operations.MakeObjectId(context.VM);
		public override bool IsEnabled(VariablesWindowCtxMenuContext context) => context.Operations.CanMakeObjectId(context.VM);
		public override bool IsVisible(VariablesWindowCtxMenuContext context) => context.Operations.IsMakeObjectIdVisible(context.VM);
	}

	[ExportMenuItem(Header = "res:DeleteObjectIdCommand", Group = MenuConstants.GROUP_CTX_DBG_VARIABLES_WINDOW_VALUES, Order = 60)]
	sealed class DeleteObjectIdVariablesWindowCtxMenuCommand : VariablesWindowCtxMenuCommand {
		[ImportingConstructor]
		DeleteObjectIdVariablesWindowCtxMenuCommand(Lazy<VariablesWindowOperations> operations) : base(operations) { }
		public override void Execute(VariablesWindowCtxMenuContext context) => context.Operations.DeleteObjectId(context.VM);
		public override bool IsVisible(VariablesWindowCtxMenuContext context) => context.Operations.CanDeleteObjectId(context.VM);
	}

	[ExportMenuItem(Header = "res:LocalsSaveCommand", Icon = DsImagesAttribute.Save, Group = MenuConstants.GROUP_CTX_DBG_VARIABLES_WINDOW_VALUES, Order = 70)]
	sealed class SaveVariablesWindowCtxMenuCommand : VariablesWindowCtxMenuCommand {
		[ImportingConstructor]
		SaveVariablesWindowCtxMenuCommand(Lazy<VariablesWindowOperations> operations) : base(operations) { }
		public override void Execute(VariablesWindowCtxMenuContext context) => context.Operations.Save(context.VM);
		public override bool IsEnabled(VariablesWindowCtxMenuContext context) => context.Operations.CanSave(context.VM);
	}

	// There's no F5 shortcut key since F5 is used to continue the debugged process
	[ExportMenuItem(Header = "res:Refresh", Icon = DsImagesAttribute.Refresh, Group = MenuConstants.GROUP_CTX_DBG_VARIABLES_WINDOW_VALUES, Order = 80)]
	sealed class RefreshVariablesWindowCtxMenuCommand : VariablesWindowCtxMenuCommand {
		[ImportingConstructor]
		RefreshVariablesWindowCtxMenuCommand(Lazy<VariablesWindowOperations> operations) : base(operations) { }
		public override void Execute(VariablesWindowCtxMenuContext context) => context.Operations.Refresh(context.VM);
		public override bool IsEnabled(VariablesWindowCtxMenuContext context) => context.Operations.CanRefresh(context.VM);
	}

	static class Constants {
		public const string SHOW_IN_MEMORY_WINDOW_GUID = "10E1F865-8531-486F-86E2-071FB1B9E1B1";
		public const string GROUP_SHOW_IN_MEMORY_WINDOW = "0,CFAF7CC1-2289-436D-8EB6-C5F6E32DE253";
		public const string LANGUAGE_GUID = "5CFF54C9-B6A8-45CD-B70A-7406FF33794A";
		public const string GROUP_LANGUAGE = "0,81ADE85F-2CA6-4C29-AF32-9300CEAFB584";
	}

	[ExportMenuItem(Header = "res:ShowInMemoryWindowCommand", Icon = DsImagesAttribute.MemoryWindow, Guid = Constants.SHOW_IN_MEMORY_WINDOW_GUID, Group = MenuConstants.GROUP_CTX_DBG_VARIABLES_WINDOW_VALUES, Order = 90)]
	sealed class ShowInMemoryWindowVariablesWindowCtxMenuCommand : VariablesWindowCtxMenuCommand {
		[ImportingConstructor]
		ShowInMemoryWindowVariablesWindowCtxMenuCommand(Lazy<VariablesWindowOperations> operations) : base(operations) { }
		public override void Execute(VariablesWindowCtxMenuContext context) { }
		public override bool IsEnabled(VariablesWindowCtxMenuContext context) => context.Operations.CanShowInMemoryWindow(context.VM);
	}

	[ExportMenuItem(OwnerGuid = Constants.SHOW_IN_MEMORY_WINDOW_GUID, Group = Constants.GROUP_SHOW_IN_MEMORY_WINDOW, Order = 0)]
	sealed class ShowInMemoryXVariablesWindowCtxMenuCommand : VariablesWindowCtxMenuCommand, IMenuItemProvider {
		readonly (IMenuItem command, string header, string inputGestureText)[] subCmds;

		[ImportingConstructor]
		ShowInMemoryXVariablesWindowCtxMenuCommand(Lazy<VariablesWindowOperations> operations)
			: base(operations) {
			subCmds = new(IMenuItem, string, string)[ToolWindows.Memory.MemoryWindowsHelper.NUMBER_OF_MEMORY_WINDOWS];
			for (int i = 0; i < subCmds.Length; i++) {
				var header = ToolWindows.Memory.MemoryWindowsHelper.GetHeaderText(i);
				var inputGestureText = ToolWindows.Memory.MemoryWindowsHelper.GetCtrlInputGestureText(i);
				subCmds[i] = (new ShowInMemoryWindowModulesCtxMenuCommand(operations, i), header, inputGestureText);
			}
		}

		public override void Execute(VariablesWindowCtxMenuContext context) { }

		IEnumerable<CreatedMenuItem> IMenuItemProvider.Create(IMenuItemContext context) {
			var ctx = CreateContext(context);
			Debug2.Assert(!(ctx is null));
			if (ctx is null)
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

	sealed class ShowInMemoryWindowModulesCtxMenuCommand : VariablesWindowCtxMenuCommand {
		readonly int windowIndex;
		public ShowInMemoryWindowModulesCtxMenuCommand(Lazy<VariablesWindowOperations> operations, int windowIndex) : base(operations) => this.windowIndex = windowIndex;
		public override void Execute(VariablesWindowCtxMenuContext context) => context.Operations.ShowInMemoryWindow(context.VM, windowIndex);
		public override bool IsEnabled(VariablesWindowCtxMenuContext context) => context.Operations.CanShowInMemoryWindow(context.VM);
	}

	[ExportMenuItem(Header = "res:LanguageCommand", Guid = Constants.LANGUAGE_GUID, Group = MenuConstants.GROUP_CTX_DBG_VARIABLES_WINDOW_VALUES, Order = 100)]
	sealed class LanguageVariablesWindowCtxMenuCommand : VariablesWindowCtxMenuCommand {
		[ImportingConstructor]
		LanguageVariablesWindowCtxMenuCommand(Lazy<VariablesWindowOperations> operations) : base(operations) { }
		public override void Execute(VariablesWindowCtxMenuContext context) { }
		public override bool IsEnabled(VariablesWindowCtxMenuContext context) => context.Operations.GetLanguages(context.VM).Any(a => a.Name != PredefinedDbgLanguageNames.None);
	}

	[ExportMenuItem(OwnerGuid = Constants.LANGUAGE_GUID, Group = Constants.GROUP_LANGUAGE, Order = 0)]
	sealed class LanguageXVariablesWindowCtxMenuCommand : VariablesWindowCtxMenuCommand, IMenuItemProvider {
		[ImportingConstructor]
		LanguageXVariablesWindowCtxMenuCommand(Lazy<VariablesWindowOperations> operations) : base(operations) { }

		public override void Execute(VariablesWindowCtxMenuContext context) { }

		IEnumerable<CreatedMenuItem> IMenuItemProvider.Create(IMenuItemContext context) {
			var ctx = CreateContext(context);
			Debug2.Assert(!(ctx is null));
			if (ctx is null)
				yield break;

			var languages = ctx.Operations.GetLanguages(ctx.VM);
			if (languages.Count == 0)
				yield break;

			var currentLanguage = ctx.Operations.GetCurrentLanguage(ctx.VM);
			foreach (var language in languages.OrderBy(a => a.DisplayName, StringComparer.CurrentCultureIgnoreCase)) {
				var attr = new ExportMenuItemAttribute { Header = UIUtilities.EscapeMenuItemHeader(language.DisplayName) };
				var cmd = new SetLanguageWindowModulesCtxMenuCommand(operations, language, language == currentLanguage);
				yield return new CreatedMenuItem(attr, cmd);
			}
		}
	}

	sealed class SetLanguageWindowModulesCtxMenuCommand : VariablesWindowCtxMenuCommand {
		readonly DbgLanguage language;
		readonly bool isChecked;
		public SetLanguageWindowModulesCtxMenuCommand(Lazy<VariablesWindowOperations> operations, DbgLanguage language, bool isChecked)
			: base(operations) {
			this.language = language;
			this.isChecked = isChecked;
		}
		public override void Execute(VariablesWindowCtxMenuContext context) => context.Operations.SetCurrentLanguage(context.VM, language);
		public override bool IsChecked(VariablesWindowCtxMenuContext context) => isChecked;
	}

	[ExportMenuItem(Header = "res:SelectAllCommand", Icon = DsImagesAttribute.Select, InputGestureText = "res:ShortCutKeyCtrlA", Group = MenuConstants.GROUP_CTX_DBG_VARIABLES_WINDOW_VALUES, Order = 10000)]
	sealed class SelectAllVariablesWindowCtxMenuCommand : VariablesWindowCtxMenuCommand {
		[ImportingConstructor]
		SelectAllVariablesWindowCtxMenuCommand(Lazy<VariablesWindowOperations> operations) : base(operations) { }
		public override void Execute(VariablesWindowCtxMenuContext context) => context.Operations.SelectAll(context.VM);
		public override bool IsEnabled(VariablesWindowCtxMenuContext context) => context.Operations.CanSelectAll(context.VM);
	}

	[ExportMenuItem(Header = "res:ClearAllCommand", Icon = DsImagesAttribute.ClearWindowContent, Group = MenuConstants.GROUP_CTX_DBG_VARIABLES_WINDOW_VALUES, Order = 10010)]
	sealed class ClearAllVariablesWindowCtxMenuCommand : VariablesWindowCtxMenuCommand {
		[ImportingConstructor]
		ClearAllVariablesWindowCtxMenuCommand(Lazy<VariablesWindowOperations> operations) : base(operations) { }
		public override void Execute(VariablesWindowCtxMenuContext context) => context.Operations.ClearAll(context.VM);
		public override bool IsEnabled(VariablesWindowCtxMenuContext context) => context.Operations.CanClearAll(context.VM);
		public override bool IsVisible(VariablesWindowCtxMenuContext context) => context.Operations.SupportsClearAll(context.VM);
	}

	[ExportMenuItem(Header = "res:HexDisplayCommand", Group = MenuConstants.GROUP_CTX_DBG_VARIABLES_WINDOW_HEXOPTS, Order = 0)]
	sealed class UseHexadecimalCallStackCtxMenuCommand : VariablesWindowCtxMenuCommand {
		[ImportingConstructor]
		UseHexadecimalCallStackCtxMenuCommand(Lazy<VariablesWindowOperations> operations) : base(operations) { }
		public override void Execute(VariablesWindowCtxMenuContext context) => context.Operations.ToggleUseHexadecimal();
		public override bool IsEnabled(VariablesWindowCtxMenuContext context) => context.Operations.CanToggleUseHexadecimal;
		public override bool IsChecked(VariablesWindowCtxMenuContext context) => context.Operations.UseHexadecimal;
	}

	[ExportMenuItem(Header = "res:DigitSeparatorsCommand", Group = MenuConstants.GROUP_CTX_DBG_VARIABLES_WINDOW_HEXOPTS, Order = 10)]
	sealed class UseDigitSeparatorsCallStackCtxMenuCommand : VariablesWindowCtxMenuCommand {
		[ImportingConstructor]
		UseDigitSeparatorsCallStackCtxMenuCommand(Lazy<VariablesWindowOperations> operations) : base(operations) { }
		public override void Execute(VariablesWindowCtxMenuContext context) => context.Operations.ToggleUseDigitSeparators();
		public override bool IsEnabled(VariablesWindowCtxMenuContext context) => context.Operations.CanToggleUseDigitSeparators;
		public override bool IsChecked(VariablesWindowCtxMenuContext context) => context.Operations.UseDigitSeparators;
	}

	[ExportMenuItem(Header = "res:LocalsCollapseParentNodeCommand", Icon = DsImagesAttribute.OneLevelUp, Group = MenuConstants.GROUP_CTX_DBG_VARIABLES_WINDOW_TREE, Order = 0)]
	sealed class CollapseParentVariablesWindowCtxMenuCommand : VariablesWindowCtxMenuCommand {
		[ImportingConstructor]
		CollapseParentVariablesWindowCtxMenuCommand(Lazy<VariablesWindowOperations> operations) : base(operations) { }
		public override void Execute(VariablesWindowCtxMenuContext context) => context.Operations.CollapseParent(context.VM);
		public override bool IsEnabled(VariablesWindowCtxMenuContext context) => context.Operations.CanCollapseParent(context.VM);
	}

	[ExportMenuItem(Header = "res:LocalsExpandChildrenNodesCommand", Icon = DsImagesAttribute.FolderOpened, Group = MenuConstants.GROUP_CTX_DBG_VARIABLES_WINDOW_TREE, Order = 10)]
	sealed class ExpandChildrenVariablesWindowCtxMenuCommand : VariablesWindowCtxMenuCommand {
		[ImportingConstructor]
		ExpandChildrenVariablesWindowCtxMenuCommand(Lazy<VariablesWindowOperations> operations) : base(operations) { }
		public override void Execute(VariablesWindowCtxMenuContext context) => context.Operations.ExpandChildren(context.VM);
		public override bool IsEnabled(VariablesWindowCtxMenuContext context) => context.Operations.CanExpandChildren(context.VM);
	}

	[ExportMenuItem(Header = "res:LocalsCollapseChildrenNodesCommand", Icon = DsImagesAttribute.FolderClosed, Group = MenuConstants.GROUP_CTX_DBG_VARIABLES_WINDOW_TREE, Order = 20)]
	sealed class CollapseChildrenVariablesWindowCtxMenuCommand : VariablesWindowCtxMenuCommand {
		[ImportingConstructor]
		CollapseChildrenVariablesWindowCtxMenuCommand(Lazy<VariablesWindowOperations> operations) : base(operations) { }
		public override void Execute(VariablesWindowCtxMenuContext context) => context.Operations.CollapseChildren(context.VM);
		public override bool IsEnabled(VariablesWindowCtxMenuContext context) => context.Operations.CanCollapseChildren(context.VM);
	}

	[ExportMenuItem(Header = "res:ShowOnlyPublicMembersCommand", Group = MenuConstants.GROUP_CTX_DBG_VARIABLES_WINDOW_OPTS, Order = 0)]
	sealed class ShowOnlyPublicMembersVariablesWindowCtxMenuCommand : VariablesWindowCtxMenuCommand {
		[ImportingConstructor]
		ShowOnlyPublicMembersVariablesWindowCtxMenuCommand(Lazy<VariablesWindowOperations> operations) : base(operations) { }
		public override void Execute(VariablesWindowCtxMenuContext context) => context.Operations.ShowOnlyPublicMembers = !context.Operations.ShowOnlyPublicMembers;
		public override bool IsEnabled(VariablesWindowCtxMenuContext context) => true;
		public override bool IsChecked(VariablesWindowCtxMenuContext context) => context.Operations.ShowOnlyPublicMembers;
	}

	[ExportMenuItem(Header = "res:ShowNamespacesCommand", Group = MenuConstants.GROUP_CTX_DBG_VARIABLES_WINDOW_OPTS, Order = 10)]
	sealed class ShowNamespacesVariablesWindowCtxMenuCommand : VariablesWindowCtxMenuCommand {
		[ImportingConstructor]
		ShowNamespacesVariablesWindowCtxMenuCommand(Lazy<VariablesWindowOperations> operations) : base(operations) { }
		public override void Execute(VariablesWindowCtxMenuContext context) => context.Operations.ShowNamespaces = !context.Operations.ShowNamespaces;
		public override bool IsEnabled(VariablesWindowCtxMenuContext context) => true;
		public override bool IsChecked(VariablesWindowCtxMenuContext context) => context.Operations.ShowNamespaces;
	}

	[ExportMenuItem(Header = "res:ShowIntrinsicTypeKeywordsCommand", Group = MenuConstants.GROUP_CTX_DBG_VARIABLES_WINDOW_OPTS, Order = 20)]
	sealed class ShowIntrinsicTypeKeywordsVariablesWindowCtxMenuCommand : VariablesWindowCtxMenuCommand {
		[ImportingConstructor]
		ShowIntrinsicTypeKeywordsVariablesWindowCtxMenuCommand(Lazy<VariablesWindowOperations> operations) : base(operations) { }
		public override void Execute(VariablesWindowCtxMenuContext context) => context.Operations.ShowIntrinsicTypeKeywords = !context.Operations.ShowIntrinsicTypeKeywords;
		public override bool IsEnabled(VariablesWindowCtxMenuContext context) => true;
		public override bool IsChecked(VariablesWindowCtxMenuContext context) => context.Operations.ShowIntrinsicTypeKeywords;
	}

	[ExportMenuItem(Header = "res:ShowTokensCommand", Group = MenuConstants.GROUP_CTX_DBG_VARIABLES_WINDOW_OPTS, Order = 30)]
	sealed class ShowTokensVariablesWindowCtxMenuCommand : VariablesWindowCtxMenuCommand {
		[ImportingConstructor]
		ShowTokensVariablesWindowCtxMenuCommand(Lazy<VariablesWindowOperations> operations) : base(operations) { }
		public override void Execute(VariablesWindowCtxMenuContext context) => context.Operations.ShowTokens = !context.Operations.ShowTokens;
		public override bool IsEnabled(VariablesWindowCtxMenuContext context) => true;
		public override bool IsChecked(VariablesWindowCtxMenuContext context) => context.Operations.ShowTokens;
	}
}
