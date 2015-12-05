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
using System.Linq;
using System.Windows;
using dnlib.DotNet;
using dnSpy.Contracts.Menus;
using dnSpy.Shared.UI.Menus;
using ICSharpCode.Decompiler;
using ICSharpCode.ILSpy.TreeNodes;
using ICSharpCode.TreeView;

namespace dnSpy.Debugger.Exceptions {
	sealed class ExceptionsCtxMenuContext {
		public readonly ExceptionsVM VM;
		public readonly ExceptionVM[] SelectedItems;

		public ExceptionsCtxMenuContext(ExceptionsVM vm, ExceptionVM[] selItems) {
			this.VM = vm;
			this.SelectedItems = selItems;
		}
	}

	sealed class ExceptionsCtxMenuCommandProxy : MenuItemCommandProxy<ExceptionsCtxMenuContext> {
		public ExceptionsCtxMenuCommandProxy(ExceptionsCtxMenuCommand cmd)
			: base(cmd) {
		}

		protected override ExceptionsCtxMenuContext CreateContext() {
			return ExceptionsCtxMenuCommand.Create();
		}
	}

	abstract class ExceptionsCtxMenuCommand : MenuItemBase<ExceptionsCtxMenuContext> {
		protected sealed override object CachedContextKey {
			get { return ContextKey; }
		}
		static readonly object ContextKey = new object();

		protected sealed override ExceptionsCtxMenuContext CreateContext(IMenuItemContext context) {
			var ui = ExceptionsControlCreator.ExceptionsControlInstance;
			if (context.CreatorObject.Object != ui.listBox)
				return null;
			return Create();
		}

		internal static ExceptionsCtxMenuContext Create() {
			var ui = ExceptionsControlCreator.ExceptionsControlInstance;
			var vm = ui.DataContext as ExceptionsVM;
			if (vm == null)
				return null;

			var dict = new Dictionary<object, int>(ui.listBox.Items.Count);
			for (int i = 0; i < ui.listBox.Items.Count; i++)
				dict[ui.listBox.Items[i]] = i;
			var elems = ui.listBox.SelectedItems.OfType<ExceptionVM>().ToArray();
			Array.Sort(elems, (a, b) => dict[a].CompareTo(dict[b]));

			return new ExceptionsCtxMenuContext(vm, elems);
		}
	}

	[ExportMenuItem(Header = "Cop_y", Icon = "Copy", InputGestureText = "Ctrl+C", Group = MenuConstants.GROUP_CTX_DBG_EXCEPTIONS_COPY, Order = 0)]
	sealed class CopyCallExceptionsCtxMenuCommand : ExceptionsCtxMenuCommand {
		public override void Execute(ExceptionsCtxMenuContext context) {
			var output = new PlainTextOutput();
			foreach (var vm in context.SelectedItems) {
				var printer = new ExceptionPrinter(output);
				printer.WriteName(vm);
				output.WriteLine();
			}
			var s = output.ToString();
			if (s.Length > 0)
				Clipboard.SetText(s);
		}

		public override bool IsEnabled(ExceptionsCtxMenuContext context) {
			return context.SelectedItems.Length > 0;
		}
	}

	[ExportMenuItem(Header = "Select _All", Icon = "Select", InputGestureText = "Ctrl+A", Group = MenuConstants.GROUP_CTX_DBG_EXCEPTIONS_COPY, Order = 10)]
	sealed class SelectAllExceptionsCtxMenuCommand : ExceptionsCtxMenuCommand {
		public override void Execute(ExceptionsCtxMenuContext context) {
			ExceptionsControlCreator.ExceptionsControlInstance.listBox.SelectAll();
		}

		public override bool IsEnabled(ExceptionsCtxMenuContext context) {
			return ExceptionsControlCreator.ExceptionsControlInstance.listBox.Items.Count > 0;
		}
	}

	[ExportMenuItem(Header = "Add E_xception", Icon = "Add", InputGestureText = "Ins", Group = MenuConstants.GROUP_CTX_DBG_EXCEPTIONS_ADD, Order = 0)]
	sealed class AddExceptionsCtxMenuCommand : ExceptionsCtxMenuCommand {
		public override void Execute(ExceptionsCtxMenuContext context) {
			context.VM.AddException();
		}

		public override bool IsEnabled(ExceptionsCtxMenuContext context) {
			return context.VM.CanAddException;
		}
	}

	[ExportMenuItem(Header = "_Remove", Icon = "RemoveCommand", InputGestureText = "Del", Group = MenuConstants.GROUP_CTX_DBG_EXCEPTIONS_ADD, Order = 10)]
	sealed class RemoveExceptionsCtxMenuCommand : ExceptionsCtxMenuCommand {
		public override void Execute(ExceptionsCtxMenuContext context) {
			context.VM.RemoveExceptions();
		}

		public override bool IsEnabled(ExceptionsCtxMenuContext context) {
			return context.VM.CanRemoveExceptions;
		}
	}

	[ExportMenuItem(Header = "Restore Defaults", Icon = "UndoCheckBoxList", Group = MenuConstants.GROUP_CTX_DBG_EXCEPTIONS_ADD, Order = 20)]
	sealed class RestoreDefaultsExceptionsCtxMenuCommand : ExceptionsCtxMenuCommand {
		public override void Execute(ExceptionsCtxMenuContext context) {
			context.VM.RestoreDefaults();
		}

		public override bool IsEnabled(ExceptionsCtxMenuContext context) {
			return context.VM.CanRestoreDefaults;
		}
	}

	sealed class ToggleEnableExceptionsCtxMenuCommand : ExceptionsCtxMenuCommand {
		public override void Execute(ExceptionsCtxMenuContext context) {
			using (ExceptionListSettings.Instance.TemporarilyDisableSave()) {
				foreach (var vm in context.SelectedItems)
					vm.BreakOnFirstChance = !vm.BreakOnFirstChance;
			}
		}
	}

	[ExportMenuItem(Header = "_Enable All Filtered Exceptions", Group = MenuConstants.GROUP_CTX_DBG_EXCEPTIONS_ADD, Order = 30)]
	sealed class EnableAllExceptionsCtxMenuCommand : ExceptionsCtxMenuCommand {
		public override void Execute(ExceptionsCtxMenuContext context) {
			context.VM.EnableAllFilteredExceptions();
		}

		public override bool IsEnabled(ExceptionsCtxMenuContext context) {
			return context.VM.CanEnableAllFilteredExceptions;
		}
	}

	[ExportMenuItem(Header = "_Disable All Filtered Exceptions", Group = MenuConstants.GROUP_CTX_DBG_EXCEPTIONS_ADD, Order = 40)]
	sealed class DisableAllExceptionsCtxMenuCommand : ExceptionsCtxMenuCommand {
		public override void Execute(ExceptionsCtxMenuContext context) {
			context.VM.DisableAllFilteredExceptions();
		}

		public override bool IsEnabled(ExceptionsCtxMenuContext context) {
			return context.VM.CanDisableAllFilteredExceptions;
		}
	}

	static class BreakWhenThrownExceptionCommand {
		abstract class CommandBase : MenuItemBase<string> {
			protected sealed override string CreateContext(IMenuItemContext context) {
				return GetExceptionTypeName(context);
			}

			public override bool IsVisible(string context) {
				if (context == null)
					return false;
				return !ExceptionsControlCreator.ExceptionsVM.Exists(ExceptionType.DotNet, context);
			}

			public override void Execute(string context) {
				if (context == null)
					return;
				ExceptionsControlCreator.ExceptionsVM.AddException(ExceptionType.DotNet, context);
			}

			string GetExceptionTypeName(IMenuItemContext context) {
				var vm = ExceptionsControlCreator.ExceptionsVM;
				if (vm == null)
					return null;
				var td = GetTypeDef(context);
				if (td == null)
					return null;
				if (!TypeTreeNode.IsException(td))
					return null;
				var name = GetExceptionString(td);
				if (vm.Exists(ExceptionType.DotNet, name))
					return null;
				return name;
			}

			static string GetExceptionString(TypeDef td) {
				//TODO: HACK: do a proper replacement since a namespace/name could contain slashes
				return td.FullName.Replace('/', '.');
			}

			protected abstract TypeDef GetTypeDef(IMenuItemContext context);

			protected TypeDef GetTypeDefFromTreeNodes(IMenuItemContext context, string guid) {
				if (context.CreatorObject.Guid != new Guid(guid))
					return null;
				var nodes = context.FindByType<SharpTreeNode[]>();
				if (nodes == null || nodes.Length != 1)
					return null;
				var node = nodes[0] as IMemberTreeNode;
				if (node == null)
					return null;
				return (node.Member as ITypeDefOrRef).ResolveTypeDef();
			}

			protected TypeDef GetTypeDefFromReference(IMenuItemContext context, string guid) {
				if (context.CreatorObject.Guid != new Guid(guid))
					return null;

				var @ref = context.FindByType<CodeReferenceSegment>();
				if (@ref == null || @ref.Reference == null)
					return null;

				return (@ref.Reference as ITypeDefOrRef).ResolveTypeDef();
			}
		}

		[ExportMenuItem(Header = "Break When Thrown", Icon = "Add", Group = MenuConstants.GROUP_CTX_FILES_DEBUG, Order = 0)]
		sealed class FilesCommand : CommandBase {
			protected sealed override object CachedContextKey {
				get { return ContextKey; }
			}
			static readonly object ContextKey = new object();

			protected override TypeDef GetTypeDef(IMenuItemContext context) {
				return GetTypeDefFromTreeNodes(context, MenuConstants.GUIDOBJ_FILES_TREEVIEW_GUID);
			}
		}

		[ExportMenuItem(Header = "Break When Thrown", Icon = "Add", Group = MenuConstants.GROUP_CTX_CODE_DEBUG, Order = 1000)]
		sealed class CodeCommand : CommandBase {
			protected sealed override object CachedContextKey {
				get { return ContextKey; }
			}
			static readonly object ContextKey = new object();

			protected override TypeDef GetTypeDef(IMenuItemContext context) {
				return GetTypeDefFromReference(context, MenuConstants.GUIDOBJ_TEXTEDITORCONTROL_GUID);
			}
		}
	}
}
