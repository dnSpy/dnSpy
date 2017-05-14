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
using System.Linq;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Menus;

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

		protected sealed override VariablesWindowCtxMenuContext CreateContext(IMenuItemContext context) {
			var vm = context.Find<IValueNodesVM>();
			if (vm == null)
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

	// Order = 10: Paste (watch window)

	[ExportMenuItem(Header = "res:LocalsEditValueCommand", InputGestureText = "res:ShortCutKeyF2", Group = MenuConstants.GROUP_CTX_DBG_VARIABLES_WINDOW_VALUES, Order = 0)]
	sealed class EditValueVariablesWindowCtxMenuCommand : VariablesWindowCtxMenuCommand {
		[ImportingConstructor]
		EditValueVariablesWindowCtxMenuCommand(Lazy<VariablesWindowOperations> operations) : base(operations) { }
		public override void Execute(VariablesWindowCtxMenuContext context) => context.Operations.EditValue(context.VM);
		public override bool IsEnabled(VariablesWindowCtxMenuContext context) => context.Operations.CanEditValue(context.VM);
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
	// Order = 40: _Delete Watch (watch window)

	[ExportMenuItem(Header = "res:MakeObjectIdCommand", Group = MenuConstants.GROUP_CTX_DBG_VARIABLES_WINDOW_VALUES, Order = 50)]
	sealed class MakeObjectIdVariablesWindowCtxMenuCommand : VariablesWindowCtxMenuCommand {
		[ImportingConstructor]
		MakeObjectIdVariablesWindowCtxMenuCommand(Lazy<VariablesWindowOperations> operations) : base(operations) { }
		public override void Execute(VariablesWindowCtxMenuContext context) => context.Operations.MakeObjectId(context.VM);
		public override bool IsEnabled(VariablesWindowCtxMenuContext context) => context.Operations.CanMakeObjectId(context.VM);
	}

	[ExportMenuItem(Header = "res:LocalsSaveCommand", Icon = DsImagesAttribute.Save, Group = MenuConstants.GROUP_CTX_DBG_VARIABLES_WINDOW_VALUES, Order = 60)]
	sealed class SaveVariablesWindowCtxMenuCommand : VariablesWindowCtxMenuCommand {
		[ImportingConstructor]
		SaveVariablesWindowCtxMenuCommand(Lazy<VariablesWindowOperations> operations) : base(operations) { }
		public override void Execute(VariablesWindowCtxMenuContext context) => context.Operations.Save(context.VM);
		public override bool IsEnabled(VariablesWindowCtxMenuContext context) => context.Operations.CanSave(context.VM);
	}

	static class Constants {
		public const string SHOW_IN_MEMORY_WINDOW_GUID = "10E1F865-8531-486F-86E2-071FB1B9E1B1";
		public const string GROUP_SHOW_IN_MEMORY_WINDOW = "0,CFAF7CC1-2289-436D-8EB6-C5F6E32DE253";
	}

	[ExportMenuItem(Header = "res:ShowInMemoryWindowCommand", Icon = DsImagesAttribute.MemoryWindow, Guid = Constants.SHOW_IN_MEMORY_WINDOW_GUID, Group = MenuConstants.GROUP_CTX_DBG_VARIABLES_WINDOW_VALUES, Order = 70)]
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

	sealed class ShowInMemoryWindowModulesCtxMenuCommand : VariablesWindowCtxMenuCommand {
		readonly int windowIndex;
		public ShowInMemoryWindowModulesCtxMenuCommand(Lazy<VariablesWindowOperations> operations, int windowIndex) : base(operations) => this.windowIndex = windowIndex;
		public override void Execute(VariablesWindowCtxMenuContext context) => context.Operations.ShowInMemoryWindow(context.VM, windowIndex);
		public override bool IsEnabled(VariablesWindowCtxMenuContext context) => context.Operations.CanShowInMemoryWindow(context.VM);
	}

	[ExportMenuItem(Header = "res:SelectAllCommand", Icon = DsImagesAttribute.Select, InputGestureText = "res:ShortCutKeyCtrlA", Group = MenuConstants.GROUP_CTX_DBG_VARIABLES_WINDOW_VALUES, Order = 10000)]
	sealed class SelectAllVariablesWindowCtxMenuCommand : VariablesWindowCtxMenuCommand {
		[ImportingConstructor]
		SelectAllVariablesWindowCtxMenuCommand(Lazy<VariablesWindowOperations> operations) : base(operations) { }
		public override void Execute(VariablesWindowCtxMenuContext context) => context.Operations.SelectAll(context.VM);
		public override bool IsEnabled(VariablesWindowCtxMenuContext context) => context.Operations.CanSelectAll(context.VM);
	}

	// Order = 10010: Clear Al_l (watch window)

	[ExportMenuItem(Header = "res:HexDisplayCommand", Group = MenuConstants.GROUP_CTX_DBG_VARIABLES_WINDOW_HEXOPTS, Order = 0)]
	sealed class UseHexadecimalCallStackCtxMenuCommand : VariablesWindowCtxMenuCommand {
		[ImportingConstructor]
		UseHexadecimalCallStackCtxMenuCommand(Lazy<VariablesWindowOperations> operations) : base(operations) { }
		public override void Execute(VariablesWindowCtxMenuContext context) => context.Operations.ToggleUseHexadecimal();
		public override bool IsEnabled(VariablesWindowCtxMenuContext context) => context.Operations.CanToggleUseHexadecimal;
		public override bool IsChecked(VariablesWindowCtxMenuContext context) => context.Operations.UseHexadecimal;
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

	[ExportMenuItem(Header = "res:ShowNamespacesCommand", Group = MenuConstants.GROUP_CTX_DBG_VARIABLES_WINDOW_OPTS, Order = 0)]
	sealed class ShowNamespacesVariablesWindowCtxMenuCommand : VariablesWindowCtxMenuCommand {
		[ImportingConstructor]
		ShowNamespacesVariablesWindowCtxMenuCommand(Lazy<VariablesWindowOperations> operations) : base(operations) { }
		public override void Execute(VariablesWindowCtxMenuContext context) => context.Operations.ShowNamespaces = !context.Operations.ShowNamespaces;
		public override bool IsEnabled(VariablesWindowCtxMenuContext context) => true;
		public override bool IsChecked(VariablesWindowCtxMenuContext context) => context.Operations.ShowNamespaces;
	}

	[ExportMenuItem(Header = "res:ShowIntrinsicTypeKeywordsCommand", Group = MenuConstants.GROUP_CTX_DBG_VARIABLES_WINDOW_OPTS, Order = 10)]
	sealed class ShowIntrinsicTypeKeywordsVariablesWindowCtxMenuCommand : VariablesWindowCtxMenuCommand {
		[ImportingConstructor]
		ShowIntrinsicTypeKeywordsVariablesWindowCtxMenuCommand(Lazy<VariablesWindowOperations> operations) : base(operations) { }
		public override void Execute(VariablesWindowCtxMenuContext context) => context.Operations.ShowIntrinsicTypeKeywords = !context.Operations.ShowIntrinsicTypeKeywords;
		public override bool IsEnabled(VariablesWindowCtxMenuContext context) => true;
		public override bool IsChecked(VariablesWindowCtxMenuContext context) => context.Operations.ShowIntrinsicTypeKeywords;
	}

	[ExportMenuItem(Header = "res:ShowTokensCommand", Group = MenuConstants.GROUP_CTX_DBG_VARIABLES_WINDOW_OPTS, Order = 20)]
	sealed class ShowTokensVariablesWindowCtxMenuCommand : VariablesWindowCtxMenuCommand {
		[ImportingConstructor]
		ShowTokensVariablesWindowCtxMenuCommand(Lazy<VariablesWindowOperations> operations) : base(operations) { }
		public override void Execute(VariablesWindowCtxMenuContext context) => context.Operations.ShowTokens = !context.Operations.ShowTokens;
		public override bool IsEnabled(VariablesWindowCtxMenuContext context) => true;
		public override bool IsChecked(VariablesWindowCtxMenuContext context) => context.Operations.ShowTokens;
	}
}
